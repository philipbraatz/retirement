
using static RetirementPlanner.TaxBrackets;

namespace RetirementPlanner;

public class Person
{
    public DateTime BirthDate { get; set; }
    public int CurrentAge => DateTime.Now.Year - BirthDate.Year;
    public bool GenderMale { get; set; }
    public int FullRetirementAge { get; set; }
    public int PartTimeAge { get; set; }
    public int PartTimeEndAge { get; set; }
    public double CurrentSalary { get; set; }
    public double SalaryGrowthRate { get; set; }

    public double InflationRate { get; set; } = 0.02; // Default 2%
    public FileType FileType { get; set; } = FileType.Single;

    public double IncomeYearly { get; set; }
    public double TaxRate => TaxBrackets.CalculateTaxes(FileType, IncomeYearly);

    // Part-Time Job Details
    public double PartTimeSalary { get; set; }

    // Contributions
    public double Personal401kContributionRate { get; set; }
    public double EmployerMatchPercentage { get; set; }

    // Expenses
    public double EssentialExpenses { get; set; }  // Must be paid (rent, food, healthcare)
    public double DiscretionarySpending { get; set; }  // Wants (travel, hobbies)

    public List<InvestmentAccount> Accounts { get; set; } = new List<InvestmentAccount>();

    public InvestmentAccount? GetAccount(AccountType type)
    {
        return Accounts.FirstOrDefault(acc => acc.Type == type);
    }

    // Social Security
    public int SocialSecurityClaimingAge { get; set; }
    public double SocialSecurityIncome { get; set; }
}
