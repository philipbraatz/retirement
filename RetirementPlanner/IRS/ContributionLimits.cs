namespace RetirementPlanner.IRS;

public static class ContributionLimits
{
    private static TaxYearConfig LoadYear(int year)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "IRS", "tax-data.json");
        using var stream = File.OpenRead(path);
        var data = System.Text.Json.JsonSerializer.Deserialize<TaxData>(stream, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TaxData();
        return data.Years.FirstOrDefault(y => y.Year == year) ?? new TaxYearConfig { Year = year };
    }

    private static double GetLimit(int year, string key, double fallback)
    {
        var cfg = LoadYear(year);
        return cfg.ContributionLimits != null && cfg.ContributionLimits.TryGetValue(key, out var v) ? v : fallback;
    }

    public static double Get401kPersonalLimit(int year, int age)
    {
        double baseLimit = GetLimit(year, "401kPersonal", 23000);
        double catchUp50 = GetLimit(year, "401kCatchUp50", 7500);
        double catchUp6063 = GetLimit(year, "401kCatchUp60to63", 11250);
        double catchUp = 0;
        if (age >= 50 && age < 60) catchUp = catchUp50;
        else if (age >= 60 && age <= 63) catchUp = catchUp6063;
        else if (age >= 64) catchUp = catchUp50;
        return baseLimit + catchUp;
    }

    public static double Get401kTotalLimit(int year, int age)
    {
        double total = GetLimit(year, "401kTotal", 69000);
        double totalCatchUp = GetLimit(year, "401kTotalWithCatchUp", 76500);
        return age >= 50 ? totalCatchUp : total;
    }

    public static double GetIRALimit(int year, int age)
    {
        double baseLimit = GetLimit(year, "iraBase", 7000);
        double catchUp = age >= 50 ? GetLimit(year, "iraCatchUp", 1000) : 0;
        return baseLimit + catchUp;
    }

    public static double GetRothIRALimit(int year, int age, double modifiedAGI, TaxBrackets.FileType filingStatus)
    {
        var cfg = LoadYear(year);
        double baseLimit = GetIRALimit(year, age);
        var map = cfg.RothIraPhaseOut;
        double start = filingStatus switch
        {
            TaxBrackets.FileType.Single => map?.GetValueOrDefault("Single_Start") ?? 146000,
            TaxBrackets.FileType.HeadOfHousehold => map?.GetValueOrDefault("HeadOfHousehold_Start") ?? 146000,
            TaxBrackets.FileType.MarriedFilingJointly => map?.GetValueOrDefault("MarriedFilingJointly_Start") ?? 230000,
            TaxBrackets.FileType.MarriedFilingSeparately => map?.GetValueOrDefault("MarriedFilingSeparately_Start") ?? 0,
            _ => 146000
        };
        double end = filingStatus switch
        {
            TaxBrackets.FileType.Single => map?.GetValueOrDefault("Single_End") ?? 161000,
            TaxBrackets.FileType.HeadOfHousehold => map?.GetValueOrDefault("HeadOfHousehold_End") ?? 161000,
            TaxBrackets.FileType.MarriedFilingJointly => map?.GetValueOrDefault("MarriedFilingJointly_End") ?? 240000,
            TaxBrackets.FileType.MarriedFilingSeparately => map?.GetValueOrDefault("MarriedFilingSeparately_End") ?? 10000,
            _ => 161000
        };
        if (modifiedAGI <= start) return baseLimit;
        if (modifiedAGI >= end) return 0;
        double pct = (modifiedAGI - start) / (end - start);
        return Math.Max(0, baseLimit * (1 - pct));
    }

    public static double GetTraditionalIRADeductibleLimit(int year, int age, double modifiedAGI, TaxBrackets.FileType filingStatus, bool taxpayerCoveredByPlan, bool spouseCoveredByPlan)
    {
        var cfg = LoadYear(year);
        double baseLimit = GetIRALimit(year, age);
        var covered = cfg.TraditionalIraDeductPhaseOutCovered;
        var spousal = cfg.TraditionalIraDeductPhaseOutSpousal;
        double start; double end;
        if (filingStatus == TaxBrackets.FileType.MarriedFilingSeparately)
        {
            start = covered?.GetValueOrDefault("MarriedFilingSeparately_Start") ?? 0;
            end = covered?.GetValueOrDefault("MarriedFilingSeparately_End") ?? 10000;
        }
        else if (filingStatus == TaxBrackets.FileType.MarriedFilingJointly)
        {
            if (taxpayerCoveredByPlan)
            {
                start = covered?.GetValueOrDefault("MarriedFilingJointly_Start") ?? 123000;
                end = covered?.GetValueOrDefault("MarriedFilingJointly_End") ?? 143000;
            }
            else if (!taxpayerCoveredByPlan && spouseCoveredByPlan)
            {
                start = spousal?.GetValueOrDefault("MarriedFilingJointly_Start") ?? 230000;
                end = spousal?.GetValueOrDefault("MarriedFilingJointly_End") ?? 240000;
            }
            else
            {
                return baseLimit;
            }
        }
        else // Single / HOH
        {
            if (taxpayerCoveredByPlan)
            {
                start = covered?.GetValueOrDefault("Single_Start") ?? 77000;
                end = covered?.GetValueOrDefault("Single_End") ?? 87000;
            }
            else return baseLimit;
        }
        if (modifiedAGI <= start) return baseLimit;
        if (modifiedAGI >= end) return 0;
        double pct = (modifiedAGI - start) / (end - start);
        return Math.Max(0, baseLimit * (1 - pct));
    }

    public static double GetSEPLimit(int year, double compensation)
    {
        double dollarLimit = GetLimit(year, "sepLimit", year >= 2025 ? 70000 : 69000);
        double percentageLimit = compensation * 0.25;
        return Math.Min(dollarLimit, percentageLimit);
    }

    public static double GetSIMPLELimit(int year, int age)
    {
        double baseLimit = GetLimit(year, "simpleBase", year >= 2025 ? 16500 : 16000);
        double catchUp = age >= 50 ? GetLimit(year, "simpleCatchUp", 3500) : 0;
        return baseLimit + catchUp;
    }

    public static double GetCompensationLimit(int year) => GetLimit(year, "compensationLimit", year >= 2025 ? 350000 : 345000);

    public static double GetHSALimit(int year, bool isFamilyCoverage = false, int age = 0)
    {
        string baseKey = isFamilyCoverage ? "hsaFamily" : "hsaIndividual";
        double baseLimit = GetLimit(year, baseKey, isFamilyCoverage ? (year >= 2025 ? 8550 : 8300) : (year >= 2025 ? 4300 : 4150));
        double catchUp = age >= 55 ? GetLimit(year, "hsaCatchUp", 1000) : 0;
        return baseLimit + catchUp;
    }

    public static double GetHSALimit(int year) => GetHSALimit(year, false, 0);

    public static double CalculateMaxEmployer401k(int year, int age, double employeeContribution, double compensation)
    {
        double totalLimit = Get401kTotalLimit(year, age);
        double limitedCompensation = Math.Min(compensation, GetCompensationLimit(year));
        double maxEmployerFromComp = limitedCompensation * 0.25;
        return Math.Min(totalLimit - employeeContribution, maxEmployerFromComp);
    }

    public static double Limit401kPersonal(double personalContributions, double personalRate, double age)
    {
        int currentYear = DateTime.Now.Year;
        // Return full allowable limit for the year (personalRate parameter reserved for future salary-based calc)
        return Get401kPersonalLimit(currentYear, (int)age);
    }

    public static double Limit401kEmployer(double currentPersonal, double employerContribution, int age)
    {
        int currentYear = DateTime.Now.Year;
        double totalLimit = Get401kTotalLimit(currentYear, age);
        return Math.Max(0, totalLimit - currentPersonal - employerContribution);
    }

    public static double LimitCompensation(double salary) => Math.Min(salary, GetCompensationLimit(DateTime.Now.Year));

    public static double Calculate401kContributionYearly(double salary, double personalRate, double employerRate, double age)
    {
        int currentYear = DateTime.Now.Year;
        double personalLimit = Get401kPersonalLimit(currentYear, (int)age);
        double salaryForMatch = LimitCompensation(salary);
        double potentialPersonal = Math.Min(salary * personalRate, personalLimit);
        double remainingSpace = Math.Max(0, Get401kTotalLimit(currentYear, (int)age) - potentialPersonal);
        double employerContribution = Math.Min(salaryForMatch * employerRate, remainingSpace);
        return potentialPersonal + employerContribution;
    }
}