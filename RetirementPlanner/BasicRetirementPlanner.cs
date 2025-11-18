using RetirementPlanner.Event;
using static RetirementPlanner.RetirementPlanner;

namespace RetirementPlanner;

public class RetirementPlanner(Person person, Options? options = null)
{
    public class Options
    {
        public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddYears(110));
        public TimeSpan ReportGranularity { get; set; } = TimeSpan.FromDays(30);
        public TimeSpan TimeStep { get; set; } = TimeSpan.FromDays(1);
        public bool AutoSubscribeLifeEvents { get; set; } = true;
    }

    private readonly Options _Options = options ?? new();
    public DateOnly CurrentDate { get; private set; } = (options ?? new()).StartDate;
    public DateOnly LastReportDate { get; private set; } = (options ?? new()).StartDate.AddDays(-1);

    // Collection of smart milestone events that know when to trigger themselves
    private readonly List<MilestoneEvent> _milestones = CreateMilestoneEvents();

    public static event EventHandler<DatedEventArgs>? NewYear;
    public static event EventHandler<PayTaxesEventArgs>? PayTaxes;
    public static event EventHandler<DatedEventArgs>? Birthday;
    public static event EventHandler<DatedEventArgs>? OnRetired;
    public static event EventHandler<DatedEventArgs>? OnFullRetirementAge;
    public static event EventHandler<DatedEventArgs>? OnRMDAgeHit;
    public static event EventHandler<DatedEventArgs>? OnSocialSecurityClaimingAgeHit;
    public static event EventHandler<DatedEventArgs>? OnCatchUpContributionsEligible;
    public static event EventHandler<DatedEventArgs>? OnRuleOf55Eligible;
    public static event EventHandler<DatedEventArgs>? OnEarlyWithdrawalPenaltyEnds;
    public static event EventHandler<DatedEventArgs>? OnSocialSecurityEarlyEligible;
    public static event EventHandler<DatedEventArgs>? OnMedicareEligible;
    public static event EventHandler<MoneyShortfallEventArgs>? OnMoneyShortfall;
    public static event EventHandler<DatedEventArgs>? OnBroke;
    public static event EventHandler<JobPayEventArgs>? OnJobPay;
    public static event EventHandler<DatedEventArgs>? OnNewMonth;
    public static event EventHandler<SpendingEventArgs>? OnSpending;

    // Reset all static event handlers to prevent duplicate subscriptions across runs
    public static void ResetAllEventHandlers()
    {
        NewYear = null;
        PayTaxes = null;
        Birthday = null;
        OnRetired = null;
        OnFullRetirementAge = null;
        OnRMDAgeHit = null;
        OnSocialSecurityClaimingAgeHit = null;
        OnCatchUpContributionsEligible = null;
        OnRuleOf55Eligible = null;
        OnEarlyWithdrawalPenaltyEnds = null;
        OnSocialSecurityEarlyEligible = null;
        OnMedicareEligible = null;
        OnMoneyShortfall = null;
        OnBroke = null;
        OnJobPay = null;
        OnNewMonth = null;
        OnSpending = null;
    }

    public double CalculateSEPP(DateTime date)
    {
        //double lifeExpectancy = LifeExpectancyTable.GetLifeExpectancy(person.CurrentAge(date), person.GenderMale);
        return 0; //return person.GetAccount(AccountType.Traditional401k).Balance / lifeExpectancy;
    }

    public double GetTaxableSocialSecurity(double totalIncome)
    {
        double provisionalIncome = totalIncome + (person.SocialSecurityIncome * 0.5);

        if (provisionalIncome <= 25000) return 0; // No SS taxation (single)
        if (provisionalIncome <= 34000) return person.SocialSecurityIncome * 0.50; // 50% taxable
        return person.SocialSecurityIncome * 0.85; // 85% taxable
    }

    public async Task RunRetirementSimulation()
    {
        if (_Options.ReportGranularity < TimeSpan.FromDays(1))
            throw new ArgumentException("Granularity must be at least one day");

        // Auto-subscribe to default LifeEvents if requested
        if (_Options.AutoSubscribeLifeEvents)
            LifeEvents.Subscribe(this);

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"Started simulation for {person.BirthDate} with {person.Investments.Accounts.Count} accounts");
        Console.ResetColor();

        List<MonthlyAccountSummary> history = [];
        double yearlyTaxesOwed = 0;

        while (CurrentDate < _Options.EndDate)
        {
            await Task.Run(() =>
            {
                yearlyTaxesOwed += Simulate(CurrentDate);

                if (CurrentDate.ToDateTime(TimeOnly.MinValue) - LastReportDate.ToDateTime(TimeOnly.MinValue) >= _Options.ReportGranularity)
                    LastReportDate = Report(CurrentDate, history);

                if (person.Investments.Accounts.All(a => a.Balance(CurrentDate) <= 0))
                {
                    LowBalanceSimulationEnd(CurrentDate);
                    CurrentDate = _Options.EndDate;
                    return;
                }

            });

            CurrentDate = CurrentDate.AddDays((int)_Options.TimeStep.TotalDays);
        }

        Export(history);
    }

    private void Export(List<MonthlyAccountSummary> history)
    {
        Helpers.ExportToCSV(history, $"retirement_simulation_{person.PartTimeEndAge}_{person.BirthDate.Year}.csv");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("Exported");
        Console.ResetColor();
    }

    private void LowBalanceSimulationEnd(DateOnly currentDate)//, (TransactionCategory, double)[] paymentsMissed)
    {
        OnBroke?.Invoke(this, new DatedEventArgs { Date = currentDate });
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"All account balances are zero at age {currentDate.Year - person.BirthDate.Year}. Goal of {person.PartTimeEndAge}/{person.FullRetirementAge}. Ending simulation.");
        //Console.WriteLine($"\tPayments missed: {string.Join(", ", paymentsMissed.Select(p => $"{p.Item1}={p.Item2:C}"))}");
        Console.ResetColor();
    }

    private DateOnly Report(DateOnly currentDate, List<MonthlyAccountSummary> history)
    {
        DateOnly lastReported = currentDate;

        // Collect data for each account using LINQ Select and AddRange
        var accountSummaries = person.Investments.Accounts
            .Select(account =>
            {
                Func<(double Amount, DateOnly Date, TransactionCategory Category), bool> CurrentMonth = d => d.Date.Year == currentDate.Year && d.Date.Month == currentDate.Month;
                return new MonthlyAccountSummary
                {
                    Date = currentDate,
                    AccountName = account.Name,
                    Deposits = account.DepositHistory
                                    .Where(CurrentMonth)
                                    .Sum(d => d.Amount),
                    Withdrawals = account.WithdrawalHistory
                                    .Where(CurrentMonth)
                                    .Sum(w => w.Amount),
                    TotalBalance = account.Balance(currentDate)
                };
            });

        history.AddRange(accountSummaries);

        PrintColoredLine($"Age {person.CurrentAge(currentDate)}\t", person.Investments.Accounts.Select(a => (a.Balance(currentDate), $"{a.Name}={a.Balance(currentDate):C}")).ToArray(), (person.EssentialExpenses + person.DiscretionarySpending) / 12);
        return lastReported;
    }

    /// <summary>
    /// Creates the collection of smart milestone events with their trigger conditions
    /// Each event knows when it should fire based on its predicate
    /// </summary>
    private static List<MilestoneEvent> CreateMilestoneEvents()
    {
        return
        [
            new(
                "Catch-up Contributions Eligible",
                (p, age) => age >= 50,
                OnCatchUpContributionsEligible
            ),
            new(
                "Rule of 55 Eligible",
                (p, age) => age >= 55,
                OnRuleOf55Eligible
            ),
            new(
                "Early Withdrawal Penalty Ends",
                (p, age) => age >= 59.5,
                OnEarlyWithdrawalPenaltyEnds
            ),
            new(
                "Social Security Early Eligible",
                (p, age) => age >= 62,
                OnSocialSecurityEarlyEligible
            ),
            new(
                "Medicare Eligible",
                (p, age) => age >= 65,
                OnMedicareEligible
            ),
            new(
                "Full Retirement Age",
                (p, age) => age == p.FullRetirementAge,
                OnFullRetirementAge
            ),
            new(
                "Social Security Claiming Age",
                (p, age) => age == p.SocialSecurityClaimingAge,
                OnSocialSecurityClaimingAgeHit
            ),
            new(
                "RMD Age Hit",
                (p, age) => age >= 73,
                OnRMDAgeHit
            ),
            new(
                "Early Retirement",
                (p, age) => (p.RetirementAge > 0 && age == p.RetirementAge) || (p.RetirementAge == 0 && p.PartTimeAge > 0 && age == p.PartTimeAge),
                OnRetired,
                (date, age) => Console.WriteLine($"🎯 Early Retirement at age {age}!")
            )
        ];
    }

    public double Simulate(DateOnly currentDate)
    {
        double yearlyTaxesOwed = 0;
        int age = currentDate.Year - person.BirthDate.Year;

        switch (currentDate)
        {
            case { Month: 1, Day: 1 }:
                NewYear?.Invoke(person, new() { Date = currentDate });
                // Also process monthly events on the 1st of January
                OnNewMonth?.Invoke(person, new() { Date = currentDate });
                break;
            case { Month: 12, Day: 1 }:
                yearlyTaxesOwed = ApplyYearlyTaxes();
                PayTaxes?.Invoke(person, new() { Date = currentDate, TaxesOwed = yearlyTaxesOwed });
                // Also process monthly events on the 1st of December
                OnNewMonth?.Invoke(person, new() { Date = currentDate });
                break;

            case { Day: 1 }:
                // Handles: Investment growth and spending monthly expenses
                OnNewMonth?.Invoke(person, new() { Date = currentDate });
                break;

            case { Month: var month, Day: var day } when month == person.BirthDate.Month && day == person.BirthDate.Day:
                Birthday?.Invoke(person, new() { Date = currentDate, Age = age });
                break;
        }

        // Check and trigger smart milestone events - they know when to fire
        foreach (var milestone in _milestones)
        {
            milestone.CheckAndTrigger(person, age, currentDate, person);
        }

        bool isRetired = person.RetirementAge > 0 ? age >= person.RetirementAge : age >= person.PartTimeEndAge;

        // Social Security, RMD withdrawals handled as special jobs
        foreach (var j in person.Jobs.Where(j => j.IsPayday(currentDate) && !isRetired
            ).ToList())
        {
            OnJobPay?.Invoke(person, new()
            {
                Date = currentDate,
                Job = j,
                GrossIncome = j.CalculateMonthlyIncome(j.HoursWorkedWeekly)
            });
        }

        return yearlyTaxesOwed;
    }

    private double ApplyYearlyTaxes()
    {
        double totalTaxableIncome = person.Jobs.Sum(s => s.CalculateTaxableIncome());

        // Include taxable portion of Social Security
        totalTaxableIncome += GetTaxableSocialSecurity(person.IncomeYearly);

        // Sum taxable withdrawals from tax-deferred accounts (401k, Traditional IRA)
        totalTaxableIncome += person.Investments.Accounts.Select(a => a as Traditional401kAccount)
            .Sum(s => s?.WithdrawalHistory.Where(w => w.Date.Year == DateTime.Now.Year)
            .Select(w => w.Amount).Sum() ?? 0);

        return TaxBrackets.CalculateTaxes(person.FileType, Math.Max(0, totalTaxableIncome));
    }

    private static void PrintColoredLine(string text, (double, string)[] moneyValues, double monthlyExpenses)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(text);
        foreach (var moneyValue in moneyValues)
        {
            Console.ForegroundColor = moneyValue.Item1 > 1000 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write($"{moneyValue.Item2}\t");
        }
        Console.ResetColor();
    }

    /// <summary>
    /// Processes age-based retirement milestones by checking each smart milestone event
    /// The events themselves know when to trigger based on their predicates
    /// </summary>
    private void ProcessAgeMilestones(DateOnly currentDate, int age)
    {
        // Each milestone checks itself and triggers if conditions are met
        foreach (var milestone in _milestones)
        {
            milestone.CheckAndTrigger(person, age, currentDate, person);
        }
    }
}