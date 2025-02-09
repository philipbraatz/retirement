public class Person
{
    public DateTime BirthDate { get; set; }
    public int FullRetirementAge { get; set; }
    public int PartTimeAge { get; set; }
    public int PartTimeEndAge { get; set; }
    public double CurrentSalary { get; set; }
    public double SalaryGrowthRate { get; set; }

    // Part-Time Job Details
    public double PartTimeSalary { get; set; }

    // Contributions
    public double Personal401kContributionRate { get; set; }
    public double EmployerMatchPercentage { get; set; }

    // Expenses
    public double EssentialExpenses { get; set; }  // Must be paid (rent, food, healthcare)
    public double DiscretionarySpending { get; set; }  // Wants (travel, hobbies)

    // Retirement Accounts
    public InvestmentAccount Account401k { get; set; } = new InvestmentAccount(0.05, "401k");
    public InvestmentAccount RothIRA { get; set; } = new InvestmentAccount(0.05, "Roth IRA");
    public InvestmentAccount TaxableAccount { get; set; } = new InvestmentAccount(0.05, "Taxable");
    public InvestmentAccount SurplusAccount { get; set; } = new InvestmentAccount(0.02, "Surplus");  // Stores excess withdrawals

    // Social Security
    public int SocialSecurityClaimingAge { get; set; }
    public double SocialSecurityIncome { get; set; }
}
