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
        double provisionalIncome = Person.IncomeYearly + Person.SocialSecurityIncome * 0.5;

        if (provisionalIncome <= 25000) return 0; // No SS taxation (single)
        if (provisionalIncome <= 34000) return Person.SocialSecurityIncome * 0.50; // 50% taxable
        return Person.SocialSecurityIncome * 0.85; // 85% taxable
    }
}