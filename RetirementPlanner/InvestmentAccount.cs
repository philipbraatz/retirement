using RetirementPlanner.Calculators;
using RetirementPlanner.IRS;

namespace RetirementPlanner;

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
    CapitalGainShortTerm,
    CapitalGainLongTerm
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

    private readonly HashSet<DateOnly> _growthAppliedDates = [];

    private static readonly MoneySourceAccount _externalSource = new();
    private static readonly MoneySinkAccount _externalSink = new();
    private static CashAccount? _globalCash;
    public static void SetGlobalCashAccount(CashAccount cash) => _globalCash = cash;

    public class MoneySourceAccount() : InvestmentAccount(0, "External Funding", AccountType.Savings)
    {
        public override double TransferTo(InvestmentAccount destination, double amount, DateOnly date, TransactionCategory category)
        {
            if (amount <= 0) return 0;
            if (!destination.YearlyStartingBalances.Any()) destination.YearlyStartingBalances[date.Year] = 0;
            if (!destination.YearlyStartingBalances.ContainsKey(date.Year)) destination.YearlyStartingBalances[date.Year] = destination.CalcStartOfYearBalance(date.Year);
            destination.DepositHistory.Add((amount, date, category));
            return amount;
        }
    }
    public class MoneySinkAccount() : InvestmentAccount(0, "External Spending", AccountType.Savings)
    {
        public override double TransferTo(InvestmentAccount destination, double amount, DateOnly date, TransactionCategory category) => 0;
        public override double Deposit(double amount, DateOnly date, TransactionCategory category) => amount;
    }
    public class CashAccount(double annualGrowthRate = 0) : InvestmentAccount(annualGrowthRate, "Cash", AccountType.Savings)
    {
        public override double Deposit(double amount, DateOnly date, TransactionCategory category)
        {
            // Cash receives external income sources only
            if (amount <= 0) return 0;
            if (category is TransactionCategory.Income or TransactionCategory.SocialSecurity)
            {
                if (!YearlyStartingBalances.Any()) YearlyStartingBalances[date.Year] = 0;
                if (!YearlyStartingBalances.ContainsKey(date.Year)) YearlyStartingBalances[date.Year] = CalcStartOfYearBalance(date.Year);
                DepositHistory.Add((amount, date, category));
                return amount;
            }
            // Other deposits to cash must come via transfer
            return 0;
        }
    }

    public InvestmentAccount(double annualGrowthRate, string name, double startingBalance, AccountType type, DateOnly? creationDate = null) : this(annualGrowthRate, name, type)
    {
        int startYear = creationDate?.Year ?? 1;
        YearlyStartingBalances[startYear] = startingBalance;
    }

    public double Balance(DateOnly date)
    {
        int targetYear = date.Year;
        if (!YearlyStartingBalances.Any()) return 0;
        if (!YearlyStartingBalances.TryGetValue(targetYear, out double startingBalance))
        {
            var candidate = YearlyStartingBalances.Where(y => y.Key <= targetYear).OrderByDescending(y => y.Key).FirstOrDefault();
            if (!candidate.Equals(default(KeyValuePair<int, double>))) { targetYear = candidate.Key; startingBalance = candidate.Value; }
            else { var earliest = YearlyStartingBalances.OrderBy(y => y.Key).First(); targetYear = earliest.Key; startingBalance = earliest.Value; }
        }
        double deposits = DepositHistory.Where(d => d.Date.Year >= targetYear && d.Date <= date).Sum(d => d.Amount);
        double withdrawals = WithdrawalHistory.Where(w => w.Date.Year >= targetYear && w.Date <= date).Sum(w => w.Amount);
        return startingBalance + deposits - withdrawals;
    }

    private double CalcStartOfYearBalance(int year)
    {
        var prev = YearlyStartingBalances.Where(m => m.Key < year);
        if (!prev.Any()) return 0;
        var previousYear = prev.Max(k => k.Key);
        return Balance(new DateOnly(previousYear, 12, 31));
    }

    public void ApplyMonthlyGrowth(DateOnly date)
    {
        if (_growthAppliedDates.Contains(date)) return;
        double currentBalance = Balance(date);
        if (currentBalance <= 0) { _growthAppliedDates.Add(date); return; }
        double monthlyRate = Math.Pow(1 + AnnualGrowthRate, 1.0 / 12) - 1;
        double growthAmount = currentBalance * monthlyRate;
        if (growthAmount > 0) { Deposit(growthAmount, date, TransactionCategory.Intrest); _growthAppliedDates.Add(date); }
    }

    public virtual double TransferTo(InvestmentAccount destination, double amount, DateOnly date, TransactionCategory category)
    {
        if (amount <= 0) return 0;
        double available = this is MoneySourceAccount ? amount : Math.Min(amount, Balance(date));
        if (available <= 0) return 0;
        if (this is not MoneySourceAccount) WithdrawalHistory.Add((available, date, category));
        if (destination is not MoneySinkAccount)
        {
            if (!destination.YearlyStartingBalances.Any()) destination.YearlyStartingBalances[date.Year] = 0;
            if (!destination.YearlyStartingBalances.ContainsKey(date.Year)) destination.YearlyStartingBalances[date.Year] = destination.CalcStartOfYearBalance(date.Year);
            destination.DepositHistory.Add((available, date, category));
        }
        return available;
    }

    public double Spend(double amount, DateOnly date, TransactionCategory category = TransactionCategory.Expenses) => TransferTo(_externalSink, amount, date, category);

    public virtual double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        if (category == TransactionCategory.Intrest)
        {
            if (amount <= 0) return 0;
            if (!YearlyStartingBalances.Any()) YearlyStartingBalances[date.Year] = 0;
            if (!YearlyStartingBalances.ContainsKey(date.Year)) YearlyStartingBalances[date.Year] = CalcStartOfYearBalance(date.Year);
            DepositHistory.Add((amount, date, category));
            return amount;
        }
        // Route non-interest funding through global cash (realistic source of dollars)
        if (category is TransactionCategory.ContributionPersonal or TransactionCategory.ContributionEmployer or TransactionCategory.InternalTransfer or TransactionCategory.Expenses or TransactionCategory.MedicalExpense or TransactionCategory.Taxes)
        {
            if (_globalCash is null) return 0; // no cash source available
            double transferred = _globalCash.TransferTo(this, amount, date, category);
            return transferred;
        }
        if (category is TransactionCategory.Income or TransactionCategory.SocialSecurity)
        {
            // Income should land in cash account not directly in investment accounts
            if (_globalCash is not null && this != _globalCash)
            {
                return _globalCash.Deposit(amount, date, category);
            }
        }
        return 0; // Ignore other categories for direct deposit
    }

    public virtual double Withdraw(double amount, DateOnly date, TransactionCategory category) => TransferTo(_externalSink, amount, date, category);
}

public class RothIRAAccount(double annualGrowthRate, string name, Person person, double startingBalance = 0, DateOnly? creationDate = null) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.RothIRA, creationDate)
{
    public Person Owner { get; } = person;
    public override double TransferTo(InvestmentAccount destination, double amount, DateOnly date, TransactionCategory category)
    {
        bool isSpending = destination.Name == "External Spending" || category == TransactionCategory.Expenses;
        double available = Math.Min(amount, Balance(date));
        if (available <= 0) return 0;
        if (isSpending)
        {
            var startingBalance = YearlyStartingBalances.TryGetValue(date.Year, out var bal)
                ? bal
                : YearlyStartingBalances.TryGetValue(date.Year - 1, out bal)
                    ? bal
                    : Balance(date);
            var rothLimit = startingBalance * 0.25;
            available = Math.Min(available, rothLimit);
            double penaltyFreeBalance = DepositHistory.Where(c => date >= c.Date.AddYears(5)).Sum(c => c.Amount);
            if (available > penaltyFreeBalance)
            {
                double taxableAmount = available - penaltyFreeBalance;
                double penalty = taxableAmount * 0.10;
                if (penalty < Balance(date)) WithdrawalHistory.Add((penalty, date, TransactionCategory.EarlyWithdrawalPenality));
                available -= penalty;
            }
        }
        return base.TransferTo(destination, available, date, category);
    }
}

public class Traditional401kAccount(double annualGrowthRate, string name, DateOnly birthdate, double startingBalance = 0, DateOnly? creationDate = null) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.Traditional401k, creationDate)
{
    private readonly DateOnly birthdate = birthdate;

    public override double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        if (category == TransactionCategory.Intrest) return base.Deposit(amount, date, category);
        double personalContributions = DepositHistory.Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionPersonal).Sum(d => d.Amount);
        double employerContributions = DepositHistory.Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionEmployer).Sum(d => d.Amount);
        double totalContributions = personalContributions + employerContributions;
        double limit = category switch
        {
            TransactionCategory.ContributionPersonal => ContributionLimits.Limit401kPersonal(personalContributions, 1, date.Year - birthdate.Year),
            TransactionCategory.ContributionEmployer => ContributionLimits.Limit401kEmployer(personalContributions, employerContributions, date.Year - birthdate.Year),
            _ => double.MaxValue
        };
        double remaining = Math.Max(0, limit - (category == TransactionCategory.ContributionPersonal ? personalContributions : totalContributions));
        return base.Deposit(Math.Min(amount, remaining), date, category);
    }

    public override double TransferTo(InvestmentAccount destination, double amount, DateOnly date, TransactionCategory category)
    {
        double age = date.Year - birthdate.Year + (date.Month - birthdate.Month) / 12.0;
        bool isSpending = destination.Name == "External Spending" || category == TransactionCategory.Expenses;
        double available = Math.Min(amount, Balance(date));
        if (available <= 0) return 0;
        if (isSpending)
        {
            double penalty = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(Type, available, age, withdrawalReason: WithdrawalReason.GeneralDistribution);
            if (penalty > 0 && penalty < Balance(date)) { WithdrawalHistory.Add((penalty, date, TransactionCategory.EarlyWithdrawalPenality)); available -= penalty; }
        }
        return base.TransferTo(destination, available, date, category);
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
    private double contributionBasis = startingBalance;

    public override double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        if (category == TransactionCategory.Intrest) return base.Deposit(amount, date, category);
        double personalContributions = DepositHistory.Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionPersonal).Sum(d => d.Amount);
        double employerContributions = DepositHistory.Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionEmployer).Sum(d => d.Amount);
        double totalContributions = personalContributions + employerContributions;
        double limit = category switch
        {
            TransactionCategory.ContributionPersonal => ContributionLimits.Limit401kPersonal(personalContributions, 1, date.Year - birthdate.Year),
            TransactionCategory.ContributionEmployer => ContributionLimits.Limit401kEmployer(personalContributions, employerContributions, date.Year - birthdate.Year),
            _ => double.MaxValue
        };
        double remaining = Math.Max(0, limit - (category == TransactionCategory.ContributionPersonal ? personalContributions : totalContributions));
        double deposited = base.Deposit(Math.Min(amount, remaining), date, category);
        if (category == TransactionCategory.ContributionPersonal || category == TransactionCategory.ContributionEmployer) contributionBasis += deposited;
        return deposited;
    }

    public override double TransferTo(InvestmentAccount destination, double amount, DateOnly date, TransactionCategory category)
    {
        bool isSpending = destination.Name == "External Spending" || category == TransactionCategory.Expenses;
        double available = Math.Min(amount, Balance(date));
        if (available <= 0) return 0;
        double age = date.Year - birthdate.Year + (date.Month - birthdate.Month) / 12.0;
        if (isSpending)
        {
            double contributionAmount = Math.Min(available, contributionBasis);
            double penalty = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(Type, available, age, rothContributionAmount: contributionAmount, withdrawalReason: WithdrawalReason.GeneralDistribution);
            if (penalty > 0 && penalty < Balance(date)) { WithdrawalHistory.Add((penalty, date, TransactionCategory.EarlyWithdrawalPenality)); available -= penalty; }
            contributionBasis = Math.Max(0, contributionBasis - available);
        }
        return base.TransferTo(destination, available, date, category);
    }

    public double RequiredMinimalDistributions(DateOnly date, bool isStillWorking = false)
    {
        int currentAge = date.Year - birthdate.Year;
        double priorYearBalance = Balance(new DateOnly(date.Year - 1, 12, 31));
        return RMDCalculator.CalculateRMD(this, currentAge, priorYearBalance, isStillWorking);
    }
}

public class TraditionalIRAAccount(double annualGrowthRate, string name, Person person, double startingBalance = 0, DateOnly? creationDate = null) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.TraditionalIRA, creationDate)
{
    public Person Owner { get; } = person;
    private readonly DateOnly birthdate = DateOnly.FromDateTime(person.BirthDate);
    public override double TransferTo(InvestmentAccount destination, double amount, DateOnly date, TransactionCategory category)
    {
        bool isSpending = destination.Name == "External Spending" || category == TransactionCategory.Expenses;
        double available = Math.Min(amount, Balance(date));
        if (available <= 0) return 0;
        if (isSpending)
        {
            double age = date.Year - birthdate.Year + (date.Month - birthdate.Month) / 12.0;
            double penalty = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(Type, available, age, withdrawalReason: WithdrawalReason.GeneralDistribution);
            if (penalty > 0 && penalty < Balance(date)) { WithdrawalHistory.Add((penalty, date, TransactionCategory.EarlyWithdrawalPenality)); available -= penalty; }
        }
        return base.TransferTo(destination, available, date, category);
    }
}

public class TaxLot { public DateOnly Acquired { get; set; } public double Quantity { get; set; } public double CostBasisPerUnit { get; set; } }

public class TaxableAccount(double annualGrowthRate, string name, double startingBalance = 0, DateOnly? creationDate = null) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.Taxable, creationDate)
{
    private readonly List<TaxLot> _lots = [];
    public Func<DateOnly, double>? MarketPriceProvider { get; set; }

    public override double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        if (category == TransactionCategory.ContributionPersonal || category == TransactionCategory.Income || category == TransactionCategory.InternalTransfer)
        {
            double price = MarketPriceProvider?.Invoke(date) ?? 1.0;
            double qty = price > 0 ? amount / price : 0;
            _lots.Add(new TaxLot { Acquired = date, Quantity = qty, CostBasisPerUnit = price });
        }
        return base.Deposit(amount, date, category);
    }

    public override double TransferTo(InvestmentAccount destination, double amount, DateOnly date, TransactionCategory category)
    {
        bool isSpending = destination.Name == "External Spending" || category == TransactionCategory.Expenses;
        double available = Math.Min(amount, Balance(date));
        if (available <= 0) return 0;
        if (isSpending)
        {
            var (stGain, ltGain, _, _) = RealizeGains(available, date);
            if (stGain > 0) DepositHistory.Add((stGain, date, TransactionCategory.CapitalGainShortTerm));
            if (ltGain > 0) DepositHistory.Add((ltGain, date, TransactionCategory.CapitalGainLongTerm));
        }
        return base.TransferTo(destination, available, date, category);
    }

    private (double st, double lt, double proceeds, double basis) RealizeGains(double amountToSell, DateOnly date)
    {
        double price = MarketPriceProvider?.Invoke(date) ?? 1.0;
        if (price <= 0) price = 1.0;
        double qtyTarget = amountToSell / price;
        double remaining = qtyTarget; double st = 0; double lt = 0; double proceeds = 0; double basis = 0;
        for (int i = 0; i < _lots.Count && remaining > 0; i++)
        {
            var lot = _lots[i]; if (lot.Quantity <= 0) continue;
            double sellQty = Math.Min(lot.Quantity, remaining);
            double lotProceeds = sellQty * price; double lotBasis = sellQty * lot.CostBasisPerUnit; double gain = lotProceeds - lotBasis;
            bool longTerm = (date.ToDateTime(TimeOnly.MinValue) - lot.Acquired.ToDateTime(TimeOnly.MinValue)).TotalDays >= 365;
            if (longTerm) lt += gain; else st += gain;
            lot.Quantity -= sellQty; remaining -= sellQty; proceeds += lotProceeds; basis += lotBasis;
        }
        return (st, lt, proceeds, basis);
    }
}

public class HSAAccount(double annualGrowthRate, string name, Person owner, double startingBalance = 0, DateOnly? creationDate = null) : InvestmentAccount(annualGrowthRate, name, startingBalance, AccountType.HSA, creationDate)
{
    public Person Owner { get; } = owner;
    public override double TransferTo(InvestmentAccount destination, double amount, DateOnly date, TransactionCategory category)
    {
        bool isSpending = destination.Name == "External Spending" || category == TransactionCategory.MedicalExpense || category == TransactionCategory.Expenses;
        double available = Math.Min(amount, Balance(date));
        if (available <= 0) return 0;
        double distributed = available;
        if (isSpending && category != TransactionCategory.MedicalExpense)
        {
            int age = Owner.CurrentAge(date);
            if (age < 65)
            {
                double penalty = distributed * 0.20;
                if (penalty < Balance(date)) WithdrawalHistory.Add((penalty, date, TransactionCategory.EarlyWithdrawalPenality));
            }
        }
        return base.TransferTo(destination, distributed, date, category);
    }

    public override double Deposit(double amount, DateOnly date, TransactionCategory category)
    {
        if (amount <= 0) return 0;
        if (category != TransactionCategory.ContributionPersonal && category != TransactionCategory.Intrest) return base.Deposit(amount, date, category);
        double personalContributionsYtd = DepositHistory.Where(d => d.Date.Year == date.Year && d.Category == TransactionCategory.ContributionPersonal).Sum(d => d.Amount);
        double limit = ContributionLimits.GetHSALimit(date.Year, false, Owner.CurrentAge(date));
        double remaining = Math.Max(0, limit - personalContributionsYtd);
        return base.Deposit(Math.Min(amount, remaining), date, category);
    }
}
