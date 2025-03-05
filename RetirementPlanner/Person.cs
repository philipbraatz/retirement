using RetirementPlanner.IRS;
using static RetirementPlanner.TaxBrackets;

namespace RetirementPlanner;

public class Person(params InvestmentAccount[] accounts)
{
    public DateTime BirthDate { get; set; }
    public int CurrentAge(DateOnly date) => date.Year - BirthDate.Year;
    public bool GenderMale { get; set; }
    public int FullRetirementAge { get; set; }
    public int PartTimeAge { get; set; }
    public int PartTimeEndAge { get; set; }
    public double SalaryGrowthRate { get; set; }
    public double InflationRate { get; set; } = 0.02;
    public FileType FileType { get; set; } = FileType.Single;

    public List<Job> Jobs { get; set; } = new();
    public double IncomeYearly => Jobs.Sum(job => job.CalculateAnnualIncome());
    public double TaxRate => TaxBrackets.CalculateTaxes(FileType, IncomeYearly);

    public InvestmentManager Investments { get; set; } = new(accounts);

    // Expenses
    public double EssentialExpenses { get; set; }  // Must be paid (rent, food, healthcare)
    public double DiscretionarySpending { get; set; }  // Wants (travel, hobbies)

    // Social Security
    public int SocialSecurityClaimingAge { get; set; }
    public double SocialSecurityIncome { get; set; }

    public void ApplyYearlyPayRaises() => Jobs.ForEach(j => j.ApplyYearlyPayRaise());
    public double CalculateTotalNetPay() => Jobs.Sum(job => job.CalculateNetPay(TaxRate));
    public double CalculateTotal401kContributions() => Jobs.Sum(job => job.CalculatePersonal401kContribution() + job.CalculateCompanyMatchContribution());

    public double CalculateCurrentSocialSecurityBenefits(DateTime retirementDate)
    {
        int retirementAge = retirementDate.Year - BirthDate.Year;
        return SocialSecurity.CalculateSocialSecurityBenefit(BirthDate.Year, retirementAge, IncomeYearly) / 12;
    }
}

