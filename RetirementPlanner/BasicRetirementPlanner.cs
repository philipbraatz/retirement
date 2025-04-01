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
    }

    private readonly Options _Options = options ?? new();
    public DateOnly CurrentDate { get; private set; } = (options ?? new()).StartDate;
    public DateOnly LastReportDate { get; private set; } = (options ?? new()).StartDate.AddDays(-1);

    public static event EventHandler<DatedEventArgs> NewYear;
    public static event EventHandler<PayTaxesEventArgs> PayTaxes;
    public static event EventHandler<DatedEventArgs> Birthday;
    public static event EventHandler<DatedEventArgs> OnRetired;
    public static event EventHandler<DatedEventArgs> OnFullRetirementAge;
    public static event EventHandler<DatedEventArgs> OnRMDAgeHit;
    public static event EventHandler<DatedEventArgs> OnSocialSecurityClaimingAgeHit;
    public static event EventHandler<MoneyShortfallEventArgs> OnMoneyShortfall;
    public static event EventHandler<DatedEventArgs> OnBroke;
    public static event EventHandler<JobPayEventArgs> OnJobPay;
    public static event EventHandler<DatedEventArgs> OnNewMonth;
    public static event EventHandler<SpendingEventArgs> OnSpending;

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

    private bool isRetired;
    private bool claimedSocialSecurity;
    private bool rmdTriggered;

    public async Task RunRetirementSimulation()
    {
        if (_Options.ReportGranularity < TimeSpan.FromDays(1))
            throw new ArgumentException("Granularity must be at least one day");

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

        // Collect data for each account
        foreach (var account in person.Investments.Accounts)
        {
            double deposits = account.DepositHistory.Where(d => d.Date.Year == currentDate.Year && d.Date.Month == currentDate.Month).Sum(d => d.Amount);
            double withdrawals = account.WithdrawalHistory.Where(w => w.Date.Year == currentDate.Year && w.Date.Month == currentDate.Month).Sum(w => w.Amount);
            double balance = account.Balance(currentDate);

            history.Add(new()
            {
                Date = currentDate,
                AccountName = account.Name,
                Deposits = deposits,
                Withdrawals = withdrawals,
                TotalBalance = balance
            });
        }

        //PrintColoredLine($"Age {person.CurrentAge(currentDate)}\t", person.Investments.Accounts.Select(a => (a.Balance(currentDate), $"{a.Name}={a.Balance(currentDate):C}")).ToArray(), (person.EssentialExpenses + person.DiscretionarySpending) / 12);
        return lastReported;
    }

    public double Simulate(DateOnly currentDate)
    {
        double yearlyTaxesOwed = 0;
        int age = currentDate.Year - person.BirthDate.Year;

        switch (currentDate)
        {
            case { Month: 1, Day: 1 }:
                NewYear?.Invoke(person, new() { Date = currentDate });
                break;
            case { Month: 12, Day: 1 }:
                yearlyTaxesOwed = ApplyYearlyTaxes();
                PayTaxes?.Invoke(person, new() { Date = currentDate, TaxesOwed = yearlyTaxesOwed });
                break;

            case { Day: 1 }:
                // Currently handles: Investment growth, spending monthly expenses
                OnNewMonth?.Invoke(person, new() { Date = currentDate });
                break;

            case { Month: var month, Day: var day } when month == person.BirthDate.Month && day == person.BirthDate.Day:
                Birthday?.Invoke(person, new() { Date = currentDate, Age = age });
                break;
        }

        if (!isRetired && age == person.FullRetirementAge)
        {
            OnFullRetirementAge?.Invoke(person, new() { Date = currentDate, Age = age });
            isRetired = true;
        }
        if (!claimedSocialSecurity && age == person.SocialSecurityClaimingAge)
        {
            OnSocialSecurityClaimingAgeHit?.Invoke(person, new() { Date = currentDate, Age = age });
            claimedSocialSecurity = true;
        }
        if (!rmdTriggered && age == 73)
        {
            OnRMDAgeHit?.Invoke(person, new() { Date = currentDate, Age = age });
            rmdTriggered = true;
        }

        // Social Security, RMD withdrawals are handled as special kind of jobs (More like income sources)
        foreach (var j in person.Jobs.Where(j => j.IsPayday(currentDate) &&
                (person.PartTimeEndAge > age || j.Type == JobType.Unemployed)
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
                .Sum(s => s.Amount) ?? 0);

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
}