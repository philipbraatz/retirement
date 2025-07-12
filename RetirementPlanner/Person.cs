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

    public List<IncomeSource> Jobs { get; set; } = [];
    public double IncomeYearly => Jobs.Sum(job => job.GrossAnnualIncome());

    public InvestmentManager Investments { get; set; } = new(accounts);

    public double EssentialExpenses { get; set; }
    public double DiscretionarySpending { get; set; }

    public int SocialSecurityClaimingAge { get; set; }
    public double SocialSecurityIncome { get; set; }
    public double TaxableIncome { get; set; }

    public void ApplyYearlyPayRaises() => Jobs.ForEach(j => j.ApplyYearlyPayRaise());
    public double CalculateTotalRetirementContributions() => Jobs.Sum(job => job.CalculateRetirementContribution() + job.CalculateCompanyMatchContribution());

    public double CalculateCurrentSocialSecurityBenefits(DateTime retirementDate)
    {
        int retirementAge = retirementDate.Year - BirthDate.Year;
        return SocialSecurity.CalculateSocialSecurityBenefit(BirthDate.Year, retirementAge, IncomeYearly) / 12;
    }

    /// <summary>
    /// Creates a deep copy of the Person object for scenario analysis
    /// </summary>
    public Person Clone()
    {
        var clonedAccounts = Investments.Accounts.Select(account => account switch
        {
            Traditional401kAccount t401k => new Traditional401kAccount(
                t401k.AnnualGrowthRate, 
                t401k.Name, 
                DateOnly.FromDateTime(BirthDate), 
                t401k.Balance(DateOnly.FromDateTime(DateTime.Now))),
            RothIRAAccount rothIRA => new RothIRAAccount(
                rothIRA.AnnualGrowthRate, 
                rothIRA.Name, 
                this, 
                rothIRA.Balance(DateOnly.FromDateTime(DateTime.Now))),
            Roth401kAccount roth401k => new Roth401kAccount(
                roth401k.AnnualGrowthRate, 
                roth401k.Name, 
                DateOnly.FromDateTime(BirthDate), 
                roth401k.Balance(DateOnly.FromDateTime(DateTime.Now))),
            TraditionalIRAAccount tradIRA => new TraditionalIRAAccount(
                tradIRA.AnnualGrowthRate, 
                tradIRA.Name, 
                this, 
                tradIRA.Balance(DateOnly.FromDateTime(DateTime.Now))),
            TaxableAccount taxable => new TaxableAccount(
                taxable.AnnualGrowthRate, 
                taxable.Name, 
                taxable.Balance(DateOnly.FromDateTime(DateTime.Now))),
            HSAAccount hsa => new HSAAccount(
                hsa.AnnualGrowthRate, 
                hsa.Name, 
                hsa.Balance(DateOnly.FromDateTime(DateTime.Now))),
            _ => account
        }).ToArray();

        var clonedJobs = Jobs.Select(job => new IncomeSource
        {
            Title = job.Title,
            Salary = job.Salary,
            RetirementContributionPercent = job.RetirementContributionPercent,
            CompanyMatchContributionPercent = job.CompanyMatchContributionPercent,
            StartDate = job.StartDate
        }).ToList();

        return new Person(clonedAccounts)
        {
            BirthDate = BirthDate,
            GenderMale = GenderMale,
            FullRetirementAge = FullRetirementAge,
            PartTimeAge = PartTimeAge,
            PartTimeEndAge = PartTimeEndAge,
            SalaryGrowthRate = SalaryGrowthRate,
            InflationRate = InflationRate,
            FileType = FileType,
            Jobs = clonedJobs,
            EssentialExpenses = EssentialExpenses,
            DiscretionarySpending = DiscretionarySpending,
            SocialSecurityClaimingAge = SocialSecurityClaimingAge,
            SocialSecurityIncome = SocialSecurityIncome,
            TaxableIncome = TaxableIncome
        };
    }


}