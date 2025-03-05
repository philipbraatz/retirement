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

public class InvestmentAccount(double annualGrowthRate, string name, double startingBalance = 0)
{
    public string Name { get; } = name;
    public double AnnualGrowthRate { get; } = annualGrowthRate;
    public double MonthlyContribution { get; set; }

    public Dictionary<int, double> YearlyStartingBalances { get; } = [];
    public List<(double Amount, DateOnly Date, TransactionCategory Category)> DepositHistory { get; } = [];
    public List<(double Amount, DateOnly Date, TransactionCategory Category)> WithdrawalHistory { get; } = [];

    public double Balance(DateOnly date)
    {
        int year = date.Year;
        if (!YearlyStartingBalances.ContainsKey(year))
            return 0;

        double startingBalance = YearlyStartingBalances[year];
        double deposits = DepositHistory.Where(d => d.Date.Year == year).Sum(d => d.Amount);
        double withdrawals = WithdrawalHistory.Where(w => w.Date.Year == year).Sum(w => w.Amount);

        return startingBalance + deposits - withdrawals;
    }

    private double CalcStartOfYearBalance(int year)
    {
        if (year == DateTime.Now.Year)
        {
            return YearlyStartingBalances.ContainsKey(year - 1) ? YearlyStartingBalances[year - 1] : 0;
        }

        double previousYearBalance = YearlyStartingBalances.ContainsKey(year - 1) ? YearlyStartingBalances[year - 1] : 0;
        double previousYearDeposits = DepositHistory.Where(d => d.Date.Year == year - 1).Sum(d => d.Amount);
        double previousYearWithdrawals = WithdrawalHistory.Where(w => w.Date.Year == year - 1).Sum(w => w.Amount);

        return previousYearBalance + previousYearDeposits - previousYearWithdrawals;
    }

    public void ApplyMonthlyGrowth(DateOnly date)
    {
        double monthlyRate = Math.Pow(1 + AnnualGrowthRate, 1.0 / 12) - 1;
        double growthAmount = Balance(date) * monthlyRate;
        Deposit(growthAmount, date, TransactionCategory.Intrest);
    }

    public virtual double Withdraw(double amount, DateOnly date, TransactionCategory category)
    {
        double currentBalance = Balance(date);
        if (currentBalance <= 0) return 0;

        double amountWithdrawn = Math.Min(currentBalance, amount);

        // Withdraw from balance
        WithdrawalHistory.Add((amountWithdrawn, date, category));
        return amountWithdrawn;
    }

    public virtual double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        DepositHistory.Add((amount, date, category));

        return amount;
    }
}

public class RothIRAAccount(double annualGrowthRate, string name, double startingBalance = 0) : InvestmentAccount(annualGrowthRate, name, startingBalance)
{

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
            .Where(c => date >= c.Date.AddYears(5)) // Only conversions older than 5 years are penalty-free
            .Sum(c => c.Amount);

        // If withdrawal exceeds penalty-free balance, apply 10% penalty
        if (amountWithdrawn > penaltyFreeBalance)
        {
            double taxableAmount = amountWithdrawn - penaltyFreeBalance;
            penalty = taxableAmount * 0.10;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\t Roth IRA Penalty Applied: {penalty:C} on {taxableAmount:C} of withdrawal");
            Console.ResetColor();

            if (penalty >= Balance(date))
                return 0;

            base.Withdraw(penalty, date, TransactionCategory.EarlyWithdrawalPenality);
        }

        var withdrawalAmount = base.Withdraw(amount, date, category);
        return withdrawalAmount;
    }
}

public class TraditionalAccount(double annualGrowthRate, string name, DateOnly birthdate, double startingBalance = 0) : InvestmentAccount(annualGrowthRate, name, startingBalance)
{
    private readonly DateOnly birthdate = birthdate;

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
            limit = ContributionLimits.Max401kTotal - personalContributions;
        }

        double amountToDeposit = Math.Min(amount, limit - totalContributions);
        base.Deposit(amountToDeposit, date, category);
        return amountToDeposit;
    }

    public override double Withdraw(double amount, DateOnly date, TransactionCategory category)
    {
        var amountWithdrawn = Math.Min(amount, Balance(date));
        var penalty = 0.0;
        if ((date.Year + date.Month / 12 - birthdate.Year - birthdate.Month / 12) < 59.5)
        {
            penalty = amountWithdrawn * 0.10; // 10% early withdrawal penalty
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\t 401(k)/IRA Early Withdrawal Penalty Applied: {penalty:C}");
            Console.ResetColor();

            base.Withdraw(penalty, date, TransactionCategory.EarlyWithdrawalPenality);
        }

        return base.Withdraw(amountWithdrawn, date, category);
    }

    public double RequiredMinimalDistributions(DateOnly date)
    {
        var currentAge = (date.Year + date.Month / 12 - birthdate.Year - birthdate.Month / 12);
        if (currentAge < 73)
            return 0;

        return Balance(new(date.Year, 1, 1)) / LifeExpectancyTable.GetLifeExpectancy(currentAge, true);
    }
}

public enum TransactionCategory
{
    Income,
    ContributionPersonal,
    ContributionEmployer,
    Intrest,
    Withdrawal,
    Taxes,
    EarlyWithdrawalPenality,
    InternalTransfer,
    SocialSecurity,
}

public enum AccountType
{
    Traditional401k,
    TraditionalIRA,
    RothIRA,
    Savings
}
