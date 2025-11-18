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
 public int RetirementAge { get; set; } // Actual work stop age (0 = not set)
 public double SalaryGrowthRate { get; set; }
 public double InflationRate { get; set; } =0.02;
 public FileType FileType { get; set; } = FileType.Single;

 public List<IncomeSource> Jobs { get; set; } = [];
 public double IncomeYearly => Jobs.Sum(job => job.GrossAnnualIncome());

 public InvestmentManager Investments { get; set; } = new(accounts);

 public double EssentialExpenses { get; set; }
 public double DiscretionarySpending { get; set; }

 public int SocialSecurityClaimingAge { get; set; }
 public double SocialSecurityIncome { get; set; }
 public double TaxableIncome { get; set; }

 #region Emergency Fund Properties
 
 public double PreRetirementEmergencyFundMinimum { get; set; } =0;
 public double EarlyRetirementEmergencyFundMinimum { get; set; } =0;
 public double PostRetirementEmergencyFundMinimum { get; set; } =0;
 public bool AutoCalculateEmergencyFunds { get; set; } = true;
 public double PreRetirementEmergencyMonths { get; set; } =6;
 public double EarlyRetirementEmergencyMonths { get; set; } =18;
 public double PostRetirementEmergencyMonths { get; set; } =12;
 
 #endregion

 public void ApplyYearlyPayRaises() => Jobs.ForEach(j => j.ApplyYearlyPayRaise());
 public double CalculateTotalRetirementContributions() => Jobs.Sum(job => job.CalculateRetirementContribution() + job.CalculateCompanyMatchContribution());

 public double CalculateCurrentSocialSecurityBenefits(DateTime retirementDate)
 {
 int retirementAge = retirementDate.Year - BirthDate.Year;
 return SocialSecurity.CalculateSocialSecurityBenefit(BirthDate.Year, retirementAge, IncomeYearly) /12;
 }
 
 #region Emergency Fund Methods
 
 public double GetRequiredEmergencyFundMinimum(DateOnly date)
 {
 int age = CurrentAge(date);
 double totalAnnualExpenses = EssentialExpenses + DiscretionarySpending;
 double monthlyExpenses = totalAnnualExpenses /12;
 
 if (AutoCalculateEmergencyFunds)
 {
 bool passedEarlyRetirementAge = RetirementAge > 0 ? age >= RetirementAge : age >= PartTimeAge;
 bool isActuallyRetired = passedEarlyRetirementAge && !Jobs.Any(j => j.Type == JobType.FullTime);
 
 if (!passedEarlyRetirementAge && Jobs.Any(j => j.Type == JobType.FullTime))
 return monthlyExpenses * PreRetirementEmergencyMonths;
 else if (isActuallyRetired && age <59.5)
 return monthlyExpenses * EarlyRetirementEmergencyMonths;
 else
 return monthlyExpenses * PostRetirementEmergencyMonths;
 }
 else
 {
 bool passedEarlyRetirementAge = RetirementAge > 0 ? age >= RetirementAge : age >= PartTimeAge;
 bool isActuallyRetired = passedEarlyRetirementAge && !Jobs.Any(j => j.Type == JobType.FullTime);
 
 if (!passedEarlyRetirementAge && Jobs.Any(j => j.Type == JobType.FullTime))
 return PreRetirementEmergencyFundMinimum;
 else if (isActuallyRetired && age <59.5)
 return EarlyRetirementEmergencyFundMinimum;
 else
 return PostRetirementEmergencyFundMinimum;
 }
 }
 
 public double GetCurrentEmergencyFundBalance(DateOnly date)
 {
 return Investments.Accounts
 .Where(a => a.Type == AccountType.Savings || a.Type == AccountType.Taxable)
 .Sum(a => a.Balance(date));
 }
 
 public bool IsEmergencyFundLow(DateOnly date)
 {
 return GetCurrentEmergencyFundBalance(date) < GetRequiredEmergencyFundMinimum(date);
 }
 
 public double GetEmergencyFundShortfall(DateOnly date)
 {
 double current = GetCurrentEmergencyFundBalance(date);
 double required = GetRequiredEmergencyFundMinimum(date);
 return Math.Max(0, required - current);
 }
 
 public double GetAvailableForWithdrawal(DateOnly date, AccountType accountType)
 {
 if (accountType != AccountType.Savings && accountType != AccountType.Taxable)
 {
 // Non-emergency accounts can be withdrawn from normally
 return Investments.Accounts
 .Where(a => a.Type == accountType)
 .Sum(a => a.Balance(date));
 }
 
 double requiredMinimum = GetRequiredEmergencyFundMinimum(date);
 double savingsBalance = Investments.Accounts.Where(a => a.Type == AccountType.Savings).Sum(a => a.Balance(date));
 double taxableBalance = Investments.Accounts.Where(a => a.Type == AccountType.Taxable).Sum(a => a.Balance(date));
 
 if (accountType == AccountType.Savings)
 {
 // Savings must preserve required minimum first
 return Math.Max(0, savingsBalance - requiredMinimum);
 }
 else // AccountType.Taxable
 {
 // Apply required minimum against savings first, then protect remaining from taxable
 double remainingRequired = Math.Max(0, requiredMinimum - savingsBalance);
 return Math.Max(0, taxableBalance - remainingRequired);
 }
 }
 
 public bool WouldIncurEarlyWithdrawalPenalty(DateOnly date)
 {
 int age = CurrentAge(date);
 return age <59.5;
 }
 
 public bool ShouldAvoidEarlyWithdrawalPenalties(DateOnly date)
 {
 bool wouldIncurPenalty = WouldIncurEarlyWithdrawalPenalty(date);
 bool hasStableIncome = Jobs.Any(j => j.Type == JobType.FullTime);
 bool hasEmergencyFundBuffer = GetCurrentEmergencyFundBalance(date) > (EssentialExpenses + DiscretionarySpending) /12;
 return wouldIncurPenalty && hasStableIncome && hasEmergencyFundBuffer;
 }
 
 public double GetAvailableWithdrawalExcludingPenalties(DateOnly date)
 {
 if (!ShouldAvoidEarlyWithdrawalPenalties(date))
 {
 return Investments.Accounts.Sum(a => a.Balance(date));
 }
 
 var penaltyFreeAccountTypes = new[] { 
 AccountType.Savings, 
 AccountType.Taxable, 
 AccountType.HSA 
 };
 
 double penaltyFreeTotal =0;
 
 foreach (var accountType in penaltyFreeAccountTypes)
 {
 if (accountType == AccountType.Savings || accountType == AccountType.Taxable)
 {
 penaltyFreeTotal += GetAvailableForWithdrawal(date, accountType);
 }
 else
 {
 penaltyFreeTotal += Investments.Accounts
 .Where(a => a.Type == accountType)
 .Sum(a => a.Balance(date));
 }
 }
 
 var rothAccounts = Investments.Accounts.Where(a => a.Type == AccountType.RothIRA || a.Type == AccountType.Roth401k);
 foreach (var account in rothAccounts)
 {
 double estimatedContributions = account.Balance(date) *0.6; // simplified
 penaltyFreeTotal += estimatedContributions;
 }
 
 return penaltyFreeTotal;
 }
 
 #endregion

 public double PocketCashTarget(DateOnly date)
 {
  double monthlyNeeds = (EssentialExpenses + DiscretionarySpending) / 12.0;
  return monthlyNeeds * 3.0; // 3 months of needs
 }

 public double CurrentPocketCash(DateOnly date) => Investments.Accounts.Where(a => a.Name == "Pocket Cash").Sum(a => a.Balance(date));

 public double PocketCashShortfall(DateOnly date)
 {
  return Math.Max(0, PocketCashTarget(date) - CurrentPocketCash(date));
 }
 
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

 var clonedPerson = new Person(clonedAccounts)
 {
 BirthDate = BirthDate,
 GenderMale = GenderMale,
 FullRetirementAge = FullRetirementAge,
 PartTimeAge = PartTimeAge,
 PartTimeEndAge = PartTimeEndAge,
 RetirementAge = RetirementAge,
 SalaryGrowthRate = SalaryGrowthRate,
 InflationRate = InflationRate,
 FileType = FileType,
 Jobs = clonedJobs,
 EssentialExpenses = EssentialExpenses,
 DiscretionarySpending = DiscretionarySpending,
 SocialSecurityClaimingAge = SocialSecurityClaimingAge,
 SocialSecurityIncome = SocialSecurityIncome,
 TaxableIncome = TaxableIncome,
 PreRetirementEmergencyFundMinimum = PreRetirementEmergencyFundMinimum,
 EarlyRetirementEmergencyFundMinimum = EarlyRetirementEmergencyFundMinimum,
 PostRetirementEmergencyFundMinimum = PostRetirementEmergencyFundMinimum,
 AutoCalculateEmergencyFunds = AutoCalculateEmergencyFunds,
 PreRetirementEmergencyMonths = PreRetirementEmergencyMonths,
 EarlyRetirementEmergencyMonths = EarlyRetirementEmergencyMonths,
 PostRetirementEmergencyMonths = PostRetirementEmergencyMonths
 };

 return clonedPerson;
 }
}