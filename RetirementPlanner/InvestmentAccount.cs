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

    public InvestmentAccount(double annualGrowthRate, string name, double startingBalance, AccountType type) : this(annualGrowthRate, name, type)
    {
        Deposit(startingBalance, DateOnly.FromDateTime(DateTime.MinValue), TransactionCategory.InternalTransfer);
        Console.WriteLine();
    }

    public double Balance(DateOnly date)
    {
        int year = date.Year;

        // Get the desired year or the latest year before the desired year
        if (!YearlyStartingBalances.TryGetValue(date.Year, out var startingBalance))
        {
            if (!YearlyStartingBalances.Any())
                YearlyStartingBalances.Add(date.Year - 1, 0);

            var yearBalance = YearlyStartingBalances.Where(y => y.Key < date.Year).OrderByDescending(o => o.Key).First();
            year = yearBalance.Key;
            startingBalance = yearBalance.Value;
        }

        double deposits = DepositHistory.Where(d => d.Date.Year >= year && d.Date <= date).Sum(d => d.Amount);
        double withdrawals = WithdrawalHistory.Where(w => w.Date.Year >= year && w.Date <= date).Sum(w => w.Amount);

        return startingBalance + deposits - withdrawals;
    }

    private double CalcStartOfYearBalance(int year)
    {
        var previousYear = YearlyStartingBalances.Where(m => m.Key < year).Max(k => k.Key);
        double previousYearBalance = YearlyStartingBalances[previousYear];
        double previousYearDeposits = DepositHistory.Where(d => d.Date.Year >= previousYear && year > d.Date.Year).Sum(d => d.Amount);
        double previousYearWithdrawals = WithdrawalHistory.Where(w => w.Date.Year >= previousYear && year > w.Date.Year).Sum(w => w.Amount);

        return previousYearBalance + previousYearDeposits - previousYearWithdrawals;
    }

    public void ApplyMonthlyGrowth(DateOnly date)
    {
        double monthlyRate = Math.Pow(1 + AnnualGrowthRate, 1.0 / 12) - 1;
        double growthAmount = Balance(date) * monthlyRate;
        Deposit(growthAmount, date, TransactionCategory.Intrest);
        Console.WriteLine();
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
        if (amount <= 0) return 0;

        if (!YearlyStartingBalances.Any())
        {   // Ensure a balance exists.
            YearlyStartingBalances.Add(date.Year, 0);
        }

        if (!YearlyStartingBalances.ContainsKey(date.Year))
        {   // Set this years balance to the previously found years balance.
            YearlyStartingBalances[date.Year] = CalcStartOfYearBalance(date.Year);
        }

        DepositHistory.Add((amount, date, category));

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"\t=> {amount:C} Into {Name} ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"[ {Balance(date):C0} ]");
        Console.ResetColor();
        return amount;
    }
}

public class RothIRAAccount(double annualGrowthRate, string name, Person person, double startingBalance = 0) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.RothIRA)
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

    public override double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        double personalContributions = DepositHistory
            .Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionPersonal)
            .Sum(d => d.Amount);

        // Use TaxCalculator to determine taxable income
        TaxCalculator taxCalculator = new(Owner, date.Year);

        // Apply Roth IRA contribution limits based on taxable income
        double limit = LimitRothIRA(personalContributions, person.TaxableIncome, Owner.CurrentAge(date));

        double amountToDeposit = Math.Min(amount, limit - personalContributions);
        return base.Deposit(amountToDeposit, date, category);
    }


    public static double LimitRothIRA(double currentContribution, double income, int age)
    {
        double limit = (age >= 50) ? 8000 : 7000; // Catch-up at age 50
        if (income > 161000) return 0; // Above income limit, no Roth contributions
        if (income > 146000) return Math.Max(0, limit - ((income - 146000) / (161000 - 146000) * limit)); // Phase-out

        return Math.Max(0, limit - currentContribution);
    }
}

public class Traditional401kAccount(double annualGrowthRate, string name, DateOnly birthdate, double startingBalance = 0) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.Traditional401k)
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
            limit = ContributionLimits.Limit401kEmployer(personalContributions, employerContributions, date.Year - birthdate.Year);
        }

        double amountToDeposit = amount + totalContributions > limit ? limit : amount;
        amountToDeposit = base.Deposit(amountToDeposit, date, category);
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
    Expenses,
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
