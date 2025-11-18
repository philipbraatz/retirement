using RetirementPlanner.IRS;

namespace RetirementPlanner;

public class FinancialSnapshot
{
    public DateOnly Date { get; set; }
    public double Salary { get; set; }
    public double SocialSecurityIncome { get; set; }
    public double TotalIncome { get; set; }
    public double EssentialExpenses { get; set; }
    public double DiscretionarySpending { get; set; }
    public double TotalExpenses { get; set; }
    public double Withdrawals { get; set; }
    public double SurplusSaved { get; set; }
    public double SurplusBalance { get; set; }
    public double Account401kBalance { get; set; }
    public double RothIRABalance { get; set; }
    public double TaxableBalance { get; set; }
}

public class InvestmentAccount(double annualGrowthRate, string name, AccountType type)
{
    public string Name { get; } = name;
    public double AnnualGrowthRate { get; } = annualGrowthRate;
    public double MonthlyContribution { get; set; }
    public readonly AccountType Type = type;

    public Dictionary<int, double> YearlyStartingBalances { get; } = [];
    public List<(double Amount, DateOnly Date, TransactionCategory Category)> DepositHistory { get; } = [];
    public List<(double Amount, DateOnly Date, TransactionCategory Category)> WithdrawalHistory { get; } = [];

    // Track which dates have already had growth applied to prevent duplicates on the same day
    private readonly HashSet<DateOnly> _growthAppliedDates = [];

    public InvestmentAccount(double annualGrowthRate, string name, double startingBalance, AccountType type, DateOnly? creationDate = null) : this(annualGrowthRate, name, type)
    {
        // If a creation date is not provided, assume the balance existed "since the beginning"
        // so that queries for any historical date include withdrawals/deposits correctly.
        int startYear = creationDate?.Year ?? 1;
        YearlyStartingBalances[startYear] = startingBalance;
    }

    public double Balance(DateOnly date)
    {
        int targetYear = date.Year;

        if (!YearlyStartingBalances.Any())
            return 0;

        // Try to get a starting balance for the requested year
        if (!YearlyStartingBalances.TryGetValue(targetYear, out double startingBalance))
        {
            // Find the latest starting balance not after the target year
            var candidate = YearlyStartingBalances
            .Where(y => y.Key <= targetYear)
            .OrderByDescending(y => y.Key)
            .FirstOrDefault();

            if (!candidate.Equals(default(KeyValuePair<int, double>)))
            {
                targetYear = candidate.Key;
                startingBalance = candidate.Value;
            }
            else
            {
                // Requested date is earlier than any recorded starting balance.
                // Treat the earliest recorded starting balance as already existing.
                var earliest = YearlyStartingBalances.OrderBy(y => y.Key).First();
                targetYear = earliest.Key;
                startingBalance = earliest.Value;
            }
        }

        double deposits = DepositHistory.Where(d => d.Date.Year >= targetYear && d.Date <= date).Sum(d => d.Amount);
        double withdrawals = WithdrawalHistory.Where(w => w.Date.Year >= targetYear && w.Date <= date).Sum(w => w.Amount);

        return startingBalance + deposits - withdrawals;
    }

    private double CalcStartOfYearBalance(int year)
    {
        // Find the most recent year before the target year that has a starting balance
        var previousYearEntries = YearlyStartingBalances.Where(m => m.Key < year);
        if (!previousYearEntries.Any())
        {
            return 0; // No previous years, start with0
        }

        var previousYear = previousYearEntries.Max(k => k.Key);

        // Calculate the end-of-year balance for the previous year
        // This becomes the starting balance for the current year
        double endOfPreviousYearBalance = Balance(new DateOnly(previousYear, 12, 31));

        return endOfPreviousYearBalance;
    }

    public void ApplyMonthlyGrowth(DateOnly date)
    {
        // Prevent applying growth multiple times for the same day
        if (_growthAppliedDates.Contains(date))
        {
            return;
        }

        double currentBalance = Balance(date);
        if (currentBalance <= 0)
        {
            // Mark as processed even if no growth to apply
            _growthAppliedDates.Add(date);
            return;
        }

        double monthlyRate = Math.Pow(1 + AnnualGrowthRate, 1.0 / 12) - 1;
        double growthAmount = currentBalance * monthlyRate;

        if (growthAmount > 0)
        {
            Deposit(growthAmount, date, TransactionCategory.Intrest);
            // Mark this day as having growth applied
            _growthAppliedDates.Add(date);
        }
    }

    public virtual double Withdraw(double amount, DateOnly date, TransactionCategory category)
    {
        double currentBalance = Balance(date);
        if (currentBalance <= 0) return 0;

        double amountWithdrawn = Math.Min(currentBalance, amount);

        // Withdraw from balance
        WithdrawalHistory.Add((amountWithdrawn, date, category));

        // Console output: -$ {category} from {account type}
        Console.WriteLine($"-${amountWithdrawn:C} {category} from {Type}");

        return amountWithdrawn;
    }

    public virtual double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        if (amount <= 0) return 0;

        if (!YearlyStartingBalances.Any())
        { // Ensure a balance exists.
            YearlyStartingBalances.Add(date.Year, 0);
        }

        if (!YearlyStartingBalances.ContainsKey(date.Year))
        { // Set this years balance to the previously found years balance.
            YearlyStartingBalances[date.Year] = CalcStartOfYearBalance(date.Year);
        }

        DepositHistory.Add((amount, date, category));

        // Console output: +$ {category} to {account type}
        Console.WriteLine($"+${amount:C} {category} to {Type}");

        return amount;
    }
}

public class RothIRAAccount(double annualGrowthRate, string name, Person person, double startingBalance = 0, DateOnly? creationDate = null) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.RothIRA, creationDate)
{
    public Person Owner { get; } = person;

    public override double Withdraw(double amount, DateOnly date, TransactionCategory category)
    {
        var startingBalance = YearlyStartingBalances.TryGetValue(date.Year, out var balance)
        ? balance
        : YearlyStartingBalances.TryGetValue(date.Year - 1, out balance)
        ? balance
        : Balance(date);

        var rothLimit = startingBalance * 0.25;
        var penalty = 0.0;
        var amountWithdrawn = Math.Min(amount, Math.Min(Balance(date), rothLimit));

        // Check for penalty-free amount
        double penaltyFreeBalance = DepositHistory
        .Where(c => date >= c.Date.AddYears(5)) // Only conversions older than5 years are penalty-free
        .Sum(c => c.Amount);

        // If withdrawal exceeds penalty-free balance, apply10% penalty
        if (amountWithdrawn > penaltyFreeBalance)
        {
            double taxableAmount = amountWithdrawn - penaltyFreeBalance;
            penalty = taxableAmount * 0.10;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"💸 FEE: Roth IRA Early Withdrawal Penalty {penalty:C} → IRS");
            Console.ResetColor();

            if (penalty >= Balance(date))
                return 0;

            base.Withdraw(penalty, date, TransactionCategory.EarlyWithdrawalPenality);
        }

        var withdrawalAmount = base.Withdraw(amount, date, category);
        return withdrawalAmount;
    }

    public override double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        double personalContributions = DepositHistory
        .Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionPersonal)
        .Sum(d => d.Amount);

        // Apply Roth IRA contribution limits based on taxable income
        double limit = LimitRothIRA(personalContributions, Owner.TaxableIncome, Owner.CurrentAge(date));

        double amountToDeposit = Math.Min(amount, limit - personalContributions);
        return base.Deposit(amountToDeposit, date, category);
    }


    public static double LimitRothIRA(double currentContribution, double income, int age)
    {
        double limit = (age >= 50) ? 8000 : 7000; // Catch-up at age50
        if (income > 161000) return 0; // Above income limit, no Roth contributions
        if (income > 146000) return Math.Max(0, limit - ((income - 146000) / (161000 - 146000) * limit)); // Phase-out

        return Math.Max(0, limit - currentContribution);
    }
}

public class Traditional401kAccount(double annualGrowthRate, string name, DateOnly birthdate, double startingBalance = 0, DateOnly? creationDate = null) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.Traditional401k, creationDate)
{
    private readonly DateOnly birthdate = birthdate;

    public override double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        // For interest/growth, don't apply contribution limits
        if (category == TransactionCategory.Intrest)
        {
            return base.Deposit(amount, date, category);
        }

        double personalContributions = DepositHistory
        .Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionPersonal)
        .Sum(d => d.Amount);

        double employerContributions = DepositHistory
        .Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionEmployer)
        .Sum(d => d.Amount);

        double totalContributions = personalContributions + employerContributions;

        double limit = 0;
        if (category == TransactionCategory.ContributionPersonal)
        {
            limit = ContributionLimits.Limit401kPersonal(personalContributions, 1, date.Year - birthdate.Year);
        }
        else if (category == TransactionCategory.ContributionEmployer)
        {
            limit = ContributionLimits.Limit401kEmployer(personalContributions, employerContributions, date.Year - birthdate.Year);
        }

        // Only allow the remaining amount up to the limit for this year/category
        double remaining = Math.Max(0, limit - (category == TransactionCategory.ContributionPersonal ? personalContributions : totalContributions));
        double amountToDeposit = Math.Min(amount, remaining);
        double actualDeposited = base.Deposit(amountToDeposit, date, category);
        return actualDeposited;
    }

    public override double Withdraw(double amount, DateOnly date, TransactionCategory category)
    {
        var amountWithdrawn = Math.Min(amount, Balance(date));
        double age = date.Year - birthdate.Year + (date.Month - birthdate.Month) / 12.0;

        // Calculate early withdrawal penalty using the new calculator
        var penalty = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(
        Type,
        amountWithdrawn,
        age,
        withdrawalReason: WithdrawalReason.GeneralDistribution);

        if (penalty > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"💸 FEE:401(k) Early Withdrawal Penalty {penalty:C} → IRS");
            Console.ResetColor();

            base.Withdraw(penalty, date, TransactionCategory.EarlyWithdrawalPenality);
        }

        return base.Withdraw(amountWithdrawn, date, category);
    }

    public double RequiredMinimalDistributions(DateOnly date, bool isStillWorking = false)
    {
        int currentAge = date.Year - birthdate.Year;
        double priorYearBalance = Balance(new DateOnly(date.Year - 1, 12, 31));

        return RMDCalculator.CalculateRMD(this, currentAge, priorYearBalance, isStillWorking);
    }
}

public class Roth401kAccount(double annualGrowthRate, string name, DateOnly birthdate, double startingBalance = 0, DateOnly? creationDate = null) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.Roth401k, creationDate)
{
    private readonly DateOnly birthdate = birthdate;
    private double contributionBasis = startingBalance; // Track contributions vs earnings

    public override double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        double personalContributions = DepositHistory
        .Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionPersonal)
        .Sum(d => d.Amount);

        double employerContributions = DepositHistory
        .Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionEmployer)
        .Sum(d => d.Amount);

        double totalContributions = personalContributions + employerContributions;

        double limit = 0;
        if (category == TransactionCategory.ContributionPersonal)
        {
            limit = ContributionLimits.Limit401kPersonal(personalContributions, 1, date.Year - birthdate.Year);
        }
        else if (category == TransactionCategory.ContributionEmployer)
        {
            limit = ContributionLimits.Limit401kEmployer(personalContributions, employerContributions, date.Year - birthdate.Year);
        }

        // Only allow the remaining amount up to the limit for this year/category
        double remaining = Math.Max(0, limit - (category == TransactionCategory.ContributionPersonal ? personalContributions : totalContributions));
        double amountToDeposit = Math.Min(amount, remaining);
        double actualDeposit = base.Deposit(amountToDeposit, date, category);

        // Track contribution basis (contributions are after-tax)
        if (category == TransactionCategory.ContributionPersonal || category == TransactionCategory.ContributionEmployer)
        {
            contributionBasis += actualDeposit;
        }

        return actualDeposit;
    }

    public override double Withdraw(double amount, DateOnly date, TransactionCategory category)
    {
        var amountWithdrawn = Math.Min(amount, Balance(date));
        double age = date.Year - birthdate.Year + (date.Month - birthdate.Month) / 12.0;

        // For Roth accounts, determine how much is contributions vs earnings
        double contributionAmount = Math.Min(amountWithdrawn, contributionBasis);

        // Calculate early withdrawal penalty using the new calculator
        var penalty = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(
        Type,
        amountWithdrawn,
        age,
        rothContributionAmount: contributionAmount,
        withdrawalReason: WithdrawalReason.GeneralDistribution);

        if (penalty > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"💸 FEE: Roth401(k) Early Withdrawal Penalty {penalty:C} → IRS");
            Console.ResetColor();

            base.Withdraw(penalty, date, TransactionCategory.EarlyWithdrawalPenality);
        }

        // Update contribution basis
        contributionBasis = Math.Max(0, contributionBasis - amountWithdrawn);

        return base.Withdraw(amountWithdrawn, date, category);
    }

    public double RequiredMinimalDistributions(DateOnly date, bool isStillWorking = false)
    {
        int currentAge = date.Year - birthdate.Year;
        double priorYearBalance = Balance(new DateOnly(date.Year - 1, 12, 31));

        // Roth401(k) requires RMDs unlike Roth IRA
        return RMDCalculator.CalculateRMD(this, currentAge, priorYearBalance, isStillWorking);
    }
}

public class TraditionalIRAAccount(double annualGrowthRate, string name, Person person, double startingBalance = 0, DateOnly? creationDate = null) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.TraditionalIRA, creationDate)
{
    public Person Owner { get; } = person;
    private readonly DateOnly birthdate = DateOnly.FromDateTime(person.BirthDate);

    public override double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        double personalContributions = DepositHistory
        .Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionPersonal)
        .Sum(d => d.Amount);

        double limit = ContributionLimits.GetIRALimit(date.Year, Owner.CurrentAge(date));
        double amountToDeposit = Math.Min(amount, limit - personalContributions);
        return base.Deposit(amountToDeposit, date, category);
    }

    public override double Withdraw(double amount, DateOnly date, TransactionCategory category)
    {
        var amountWithdrawn = Math.Min(amount, Balance(date));
        double age = date.Year - birthdate.Year + (date.Month - birthdate.Month) / 12.0;

        // Calculate early withdrawal penalty using the new calculator
        var penalty = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(
        Type,
        amountWithdrawn,
        age,
        withdrawalReason: WithdrawalReason.GeneralDistribution);

        if (penalty > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"💸 FEE: Traditional IRA Early Withdrawal Penalty {penalty:C} → IRS");
            Console.ResetColor();

            base.Withdraw(penalty, date, TransactionCategory.EarlyWithdrawalPenality);
        }

        return base.Withdraw(amountWithdrawn, date, category);
    }

    public double RequiredMinimalDistributions(DateOnly date)
    {
        int currentAge = date.Year - birthdate.Year;
        double priorYearBalance = Balance(new DateOnly(date.Year - 1, 12, 31));

        return RMDCalculator.CalculateRMD(this, currentAge, priorYearBalance, false);
    }
}

public class TaxableAccount(double annualGrowthRate, string name, double startingBalance = 0, DateOnly? creationDate = null) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.Taxable, creationDate)
{
    public override double Withdraw(double amount, DateOnly date, TransactionCategory category)
    {
        // No early withdrawal penalties for taxable accounts
        double currentBalance = Balance(date);
        if (currentBalance <= 0) return 0;

        double amountWithdrawn = Math.Min(currentBalance, amount);
        WithdrawalHistory.Add((amountWithdrawn, date, category));
        return amountWithdrawn;
    }

    public override double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        // No contribution limits for taxable accounts
        return base.Deposit(amount, date, category);
    }
}

public class HSAAccount(double annualGrowthRate, string name, double startingBalance = 0, DateOnly? creationDate = null) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.HSA, creationDate)
{
    // HSA annual personal contribution limit handling
    public override double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        if (amount <= 0) return 0;
        if (category != TransactionCategory.ContributionPersonal && category != TransactionCategory.Intrest)
            return base.Deposit(amount, date, category); // Allow non-contribution deposits untouched

        double personalContributionsYtd = DepositHistory
            .Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionPersonal)
            .Sum(d => d.Amount);

        double limit = ContributionLimits.GetHSALimit(date.Year);
        double remaining = Math.Max(0, limit - personalContributionsYtd);
        double toDeposit = Math.Min(amount, remaining);
        return base.Deposit(toDeposit, date, category);
    }

    public override double Withdraw(double amount, DateOnly date, TransactionCategory category)
    {
        // Qualified medical expense withdrawals are tax/penalty free
        bool isQualifiedMedical = category == TransactionCategory.MedicalExpense;
        double currentBalance = Balance(date);
        if (currentBalance <= 0) return 0;
        double amountWithdrawn = Math.Min(currentBalance, amount);

        if (!isQualifiedMedical)
        {
            // Apply penalty (simplified: 20%) before age 65 for non-qualified withdrawals
            int age = date.Year - DateOnly.FromDateTime(DateTime.Now).Year + (date.Month / 12); // Simplified, ideally use owner birthdate
            if (age < 65)
            {
                double penalty = amountWithdrawn * 0.20;
                base.Withdraw(penalty, date, TransactionCategory.EarlyWithdrawalPenality);
            }
        }

        return base.Withdraw(amountWithdrawn, date, category);
    }
}

public enum TransactionCategory
{
    Income,
    ContributionPersonal,
    ContributionEmployer,
    Intrest,
    Expenses,
    Taxes,
    EarlyWithdrawalPenality,
    InternalTransfer,
    SocialSecurity,
    MedicalExpense,
    InitialBalance,
}

public enum AccountType
{
    Traditional401k,
    Roth401k,
    Traditional403b,
    Roth403b,
    TraditionalIRA,
    RothIRA,
    SEPIRA,
    SIMPLEIRA,
    Savings,
    Taxable,
    HSA
}
