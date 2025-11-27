using RetirementPlanner.IRS;
using static RetirementPlanner.TaxBrackets;

namespace RetirementPlanner.Calculators;

public class TaxCalculator(Person person, int taxYear)
{
    private readonly TaxYearConfig _config = LoadYearConfig(taxYear);

    private static TaxYearConfig LoadYearConfig(int year)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "IRS", "tax-data.json");
        using var stream = File.OpenRead(path);
        var data = System.Text.Json.JsonSerializer.Deserialize<TaxData>(stream, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TaxData();
        return data.Years.FirstOrDefault(y => y.Year == year) ?? new TaxYearConfig { Year = year };
    }

    public Person Person { get; } = person;
    public int TaxYear { get; } = taxYear;

    private double GetThreshold(Dictionary<string,double>? map, string key, double fallback) => map != null && map.TryGetValue(key, out var v) ? v : fallback;

    public double GetGrossIncome() => Person.IncomeYearly + GetTaxableInvestmentIncome() + GetTaxableSocialSecurity();

    public double GetTaxableIncome(double grossIncome) => Math.Max(0, grossIncome - GetTotalDeductions());

    public double GetTotalDeductions()
    {
        int age = EstimateAgeAtYearEnd(TaxYear);
        int spouseAge = Person.SpouseBirthDate.HasValue ? TaxYear - Person.SpouseBirthDate.Value.Year : 0;
        return GetStandardDeduction(TaxYear, age, Person.IsBlind, Person.FileType, spouseAge, Person.SpouseIsBlind);
    }

    public double GetTaxesOwed(double grossIncome) => CalculateTaxes(Person.FileType, GetTaxableIncome(grossIncome), TaxYear);

    public double GetTotalTaxLiability(double netInvestmentIncome, double wages, double modifiedAGI) 
        => GetTaxesOwed(GetGrossIncome()) +
            CalculateNetInvestmentIncomeTax(netInvestmentIncome, modifiedAGI) +
            CalculateAdditionalMedicareTax(wages, Person.FileType);

    public double CalculateNetInvestmentIncomeTax(double netInvestmentIncome, double modifiedAGI)
    {
        double threshold = Person.FileType switch
        {
            FileType.Single => GetThreshold(_config.NiitThresholds, nameof(FileType.Single), 200000),
            FileType.MarriedFilingJointly => GetThreshold(_config.NiitThresholds, nameof(FileType.MarriedFilingJointly), 250000),
            FileType.MarriedFilingSeparately => GetThreshold(_config.NiitThresholds, nameof(FileType.MarriedFilingSeparately), 125000),
            FileType.HeadOfHousehold => GetThreshold(_config.NiitThresholds, nameof(FileType.HeadOfHousehold), 200000),
            _ => 200000
        };
        double rate = _config.NiitRate ?? 0.038;
        if (modifiedAGI <= threshold) return 0;
        double excess = modifiedAGI - threshold;
        double taxable = Math.Min(netInvestmentIncome, excess);
        return taxable * rate;
    }

    public static double CalculateAdditionalMedicareTax(double wages, FileType filingStatus)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "IRS", "tax-data.json");
        using var stream = File.OpenRead(path);
        var data = System.Text.Json.JsonSerializer.Deserialize<TaxData>(stream, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TaxData();
        var yearCfg = data.Years.FirstOrDefault(y => y.Year == DateTime.Now.Year);
        var map = yearCfg?.AdditionalMedicareThresholds;
        double threshold = filingStatus switch
        {
            FileType.Single => map != null && map.TryGetValue(nameof(FileType.Single), out var v1) ? v1 : 200000,
            FileType.MarriedFilingJointly => map != null && map.TryGetValue(nameof(FileType.MarriedFilingJointly), out var v2) ? v2 : 250000,
            FileType.MarriedFilingSeparately => map != null && map.TryGetValue(nameof(FileType.MarriedFilingSeparately), out var v3) ? v3 : 125000,
            FileType.HeadOfHousehold => map != null && map.TryGetValue(nameof(FileType.HeadOfHousehold), out var v4) ? v4 : 200000,
            _ => 200000
        };
        double rate = yearCfg?.AdditionalMedicareRate ?? 0.009;
        if (wages <= threshold) return 0;
        return (wages - threshold) * rate;
    }

    public double GetStandardDeduction(int year, int age, bool isBlind = false)
        => GetStandardDeduction(year, age, isBlind, Person.FileType, Person.SpouseBirthDate.HasValue ? year - Person.SpouseBirthDate.Value.Year : 0, Person.SpouseIsBlind);

    public static double GetStandardDeduction(int year, int age, bool isBlind, FileType filingStatus, int spouseAge = 0, bool spouseIsBlind = false)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "IRS", "tax-data.json");
        using var stream = File.OpenRead(path);
        var data = System.Text.Json.JsonSerializer.Deserialize<TaxData>(stream, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TaxData();
        var cfg = data.Years.FirstOrDefault(y => y.Year == year);
        double baseDeduction = TaxYearDataProvider.GetBaseStandardDeductionStatic(year, filingStatus) ?? 0;
        int qualifiers = 0;
        int seniorAge = (int)(cfg?.AdditionalDeductionAmounts?.GetValueOrDefault("SeniorAge") ?? 65);
        if (age >= seniorAge) qualifiers++;
        if (isBlind) qualifiers++;
        bool isMFJ = filingStatus == FileType.MarriedFilingJointly;
        if (isMFJ)
        {
            if (spouseAge >= seniorAge) qualifiers++;
            if (spouseIsBlind) qualifiers++;
        }
        double addSingle = cfg?.AdditionalDeductionAmounts?.GetValueOrDefault("SingleOrHOH") ?? 1950;
        double addMFJ = cfg?.AdditionalDeductionAmounts?.GetValueOrDefault("MFJOrMFS") ?? 1550;
        double addPer = (filingStatus == FileType.Single || filingStatus == FileType.HeadOfHousehold || filingStatus == FileType.MarriedFilingSeparately) ? addSingle : addMFJ;
        return baseDeduction + qualifiers * addPer;
    }

    private double GetTaxableInvestmentIncome() => Person.Investments.Accounts.Where(acc => acc is Traditional401kAccount)
        .Aggregate(0.0, (tax, account) => tax + account.WithdrawalHistory.Where(w => w.Date.Year == TaxYear).Sum(w => w.Amount));

    private double GetTaxableSocialSecurity()
    {
        double annualSS = Person.SocialSecurityIncome * 12.0;
        double provisionalIncome = Person.IncomeYearly + Person.TaxExemptInterest + (annualSS * 0.5);
        double singleLower = _config.SocialSecurityThresholds?.GetValueOrDefault("SingleLower") ?? 25000;
        double singleUpper = _config.SocialSecurityThresholds?.GetValueOrDefault("SingleUpper") ?? 34000;
        double mfjLower = _config.SocialSecurityThresholds?.GetValueOrDefault("MFJLower") ?? 32000;
        double mfjUpper = _config.SocialSecurityThresholds?.GetValueOrDefault("MFJUpper") ?? 44000;
        double midPct = _config.SocialSecurityThresholds?.GetValueOrDefault("MidPct") ?? 0.50;
        double highPct = _config.SocialSecurityThresholds?.GetValueOrDefault("HighPct") ?? 0.85;
        return Person.FileType switch
        {
            FileType.Single or FileType.HeadOfHousehold => provisionalIncome <= singleLower ? 0 : provisionalIncome <= singleUpper ? annualSS * midPct : annualSS * highPct,
            FileType.MarriedFilingJointly => provisionalIncome <= mfjLower ? 0 : provisionalIncome <= mfjUpper ? annualSS * midPct : annualSS * highPct,
            FileType.MarriedFilingSeparately => provisionalIncome <= 0 ? 0 : annualSS * highPct,
            _ => 0
        };
    }

    private int EstimateAgeAtYearEnd(int year)
    {
        if (Person.BirthDate == default) return 0;
        var yearEnd = new DateOnly(year, 12, 31);
        return Person.CurrentAge(yearEnd);
    }
}