using static RetirementPlanner.TaxBrackets;

namespace RetirementPlanner.IRS;

public class TaxCalculator(Person person, int taxYear)
{
    public Person Person { get; } = person;
    public int TaxYear { get; } = taxYear;

    public double GetGrossIncome()
    {
        double earnedIncome = Person.IncomeYearly;
        double taxableInvestments = GetTaxableInvestmentIncome();
        double taxableSocialSecurity = GetTaxableSocialSecurity();

        return earnedIncome + taxableInvestments + taxableSocialSecurity;
    }

    public double GetTaxableIncome(double grossIncome)
    {
        //double deductions = GetTotalDeductions();
        return Math.Max(0, grossIncome);
    }

    public double GetTotalDeductions()
    {
        double standardDeduction = GetStandardDeduction();
        return standardDeduction;
    }

    public double GetTaxesOwed(double grossIncome)
    {
        double taxableIncome = GetTaxableIncome(grossIncome);
        return CalculateTaxes(Person.FileType, taxableIncome);
    }

    public double GetStandardDeduction() => (int)Person.FileType;

    private double GetTaxableInvestmentIncome() => Person.Investments.Accounts.Where(acc => acc is Traditional401kAccount)
            .Aggregate(0.0, (tax, account) => tax + account.WithdrawalHistory
                    .Where(w => w.Date.Year == TaxYear)
                    .Sum(w => w.Amount));

    private double GetTaxableSocialSecurity()
    {
        // Calculate provisional income: AGI + Non-taxable interest + 50% of Social Security
        double provisionalIncome = Person.IncomeYearly + (Person.SocialSecurityIncome * 0.5);
        
        // Determine taxable portion based on filing status and provisional income
        return Person.FileType switch
        {
            FileType.Single => CalculateTaxableSocialSecuritySingle(provisionalIncome, Person.SocialSecurityIncome),
            FileType.MarriedFilingJointly => CalculateTaxableSocialSecurityMFJ(provisionalIncome, Person.SocialSecurityIncome),
            FileType.MarriedFilingSeparately => CalculateTaxableSocialSecurityMFS(provisionalIncome, Person.SocialSecurityIncome),
            _ => 0
        };
    }

    private static double CalculateTaxableSocialSecuritySingle(double provisionalIncome, double socialSecurityIncome)
    {
        if (provisionalIncome <= 25000) return 0; // No SS taxation
        if (provisionalIncome <= 34000) return Math.Min(socialSecurityIncome * 0.50, (provisionalIncome - 25000) * 0.50); // Up to 50% taxable
        
        // Up to 85% taxable
        double tier1Tax = Math.Min(socialSecurityIncome * 0.50, 4500); // 50% of (34000-25000)/2
        double tier2Tax = (provisionalIncome - 34000) * 0.85;
        return Math.Min(socialSecurityIncome * 0.85, tier1Tax + tier2Tax);
    }

    private static double CalculateTaxableSocialSecurityMFJ(double provisionalIncome, double socialSecurityIncome)
    {
        if (provisionalIncome <= 32000) return 0; // No SS taxation
        if (provisionalIncome <= 44000) return Math.Min(socialSecurityIncome * 0.50, (provisionalIncome - 32000) * 0.50); // Up to 50% taxable
        
        // Up to 85% taxable
        double tier1Tax = Math.Min(socialSecurityIncome * 0.50, 6000); // 50% of (44000-32000)/2
        double tier2Tax = (provisionalIncome - 44000) * 0.85;
        return Math.Min(socialSecurityIncome * 0.85, tier1Tax + tier2Tax);
    }

    private static double CalculateTaxableSocialSecurityMFS(double provisionalIncome, double socialSecurityIncome)
    {
        // Married filing separately has $0 thresholds - most SS income is taxable
        if (provisionalIncome <= 0) return 0;
        return Math.Min(socialSecurityIncome * 0.85, provisionalIncome * 0.85);
    }
}