using RetirementPlanner.IRS;

namespace RetirementPlanner.Event;

public static class LifeEvents
{
    public static void Subscribe(RetirementPlanner planner)
    {
        RetirementPlanner.NewYear += OnNewYear;
        RetirementPlanner.PayTaxes += OnPayTaxes;
        RetirementPlanner.Birthday += OnBirthday;
        RetirementPlanner.OnRetired += OnRetired;
        RetirementPlanner.OnFullRetirementAge += OnFullRetirementAge;
        RetirementPlanner.OnRMDAgeHit += OnRMDAgeHit;
        RetirementPlanner.OnSocialSecurityClaimingAgeHit += ClaimSocialSecurity;
        RetirementPlanner.OnMoneyShortfall += OnMoneyShortfall;
        RetirementPlanner.OnBroke += OnBroke;
        RetirementPlanner.OnJobPay += OnJobPay;
        RetirementPlanner.OnNewMonth += OnNewMonth;
        RetirementPlanner.OnSpending += OnSpending;
    }

    private static void OnNewYear(object sender, DatedEventArgs e)
    {
        Console.WriteLine($"New Year: {e.Date}\t----------------------------------");

        var person = (Person)sender;
        person.ApplyYearlyPayRaises();
        person.EssentialExpenses *= (1 + person.InflationRate);
        person.DiscretionarySpending *= (1 + person.InflationRate);

        var grossIncome = person.Jobs.Sum(s => s.GrossAnnualIncome());
        person.TaxableIncome = grossIncome;

        // Calculate retirement contributions
        double personal401kContribution = grossIncome * (person.Jobs[0].Personal401kContributionPercent.HasValue ? person.Jobs[0].Personal401kContributionPercent.Value : 0);
        double companyMatchContribution = grossIncome * (person.Jobs[0].CompanyMatchContributionPercent.HasValue ? person.Jobs[0].CompanyMatchContributionPercent.Value * person.Jobs[0].Personal401kContributionPercent.Value : 0);

        person.TaxableIncome -= personal401kContribution;

        // Update RMD IncomeSource every new year
        var rmdIncomeSource = person.Jobs.FirstOrDefault(j => j.Title == "RMD");
        if (rmdIncomeSource != null)
        {
            rmdIncomeSource.Salary = CalculateRMD(person, e.Date) / 12;
        }
        // Print the change in balances for each account from the previous year to now
        foreach (var account in person.Investments.Accounts)
        {
            var previousYear = e.Date.Year - 1;
            var currentYear = e.Date.Year;

            var previousYearBalance = account.Balance(new DateOnly(previousYear, 1, 1));
            var currentYearBalance = account.Balance(new DateOnly(currentYear, 1, 1));

            var balanceChange = currentYearBalance - previousYearBalance;

            Console.WriteLine($"Account: {account.Name}, Previous Year Balance: {previousYearBalance:C}, Current Year Balance: {currentYearBalance:C}, Change: {balanceChange:C}");
        }
    }

    private static void OnPayTaxes(object sender, PayTaxesEventArgs e)
    {
        Console.WriteLine($"Pay Taxes: {e.Date}, Taxes Owed: {e.TaxesOwed:C}");
    }

    private static void OnBirthday(object sender, DatedEventArgs e)
    {
        var person = (Person)sender;
        Console.WriteLine($"Birthday: {e.Date}, Age: {e.Age}");

        if (e.Age == person.SocialSecurityClaimingAge)
        {
            var socialSecurityJob = new IncomeSource
            {
                Title = "Social Security",
                StartDate = e.Date,
                Salary = person.CalculateCurrentSocialSecurityBenefits(e.Date.ToDateTime(TimeOnly.MinValue)),
                PaymentType = PaymentType.Salaried,
                PayFrequency = PayFrequency.Monthly
            };
            person.Jobs.Add(socialSecurityJob);
        }
    }

    private static void OnRetired(object sender, DatedEventArgs e)
    {
        Console.WriteLine($"Retired: {e.Date}, Age: {e.Age}");
    }

    private static void OnFullRetirementAge(object sender, DatedEventArgs e)
    {
        Console.WriteLine($"Full Retirement Age: {e.Date}, Age: {e.Age}");
    }

    private static void OnRMDAgeHit(object sender, DatedEventArgs e)
    {
        var person = (Person)sender;
        Console.WriteLine($"RMD Age Hit: {e.Date}, Age: {e.Age}");

        person.Jobs.Add(new()
        {
            Title = "RMD",
            StartDate = e.Date,
            Salary = CalculateRMD(person, e.Date) / 12,
            PaymentType = PaymentType.Salaried,
            PayFrequency = PayFrequency.Monthly,
            Type = JobType.Unemployed
        });
    }

    private static void ClaimSocialSecurity(object sender, DatedEventArgs e)
    {
        Console.WriteLine($"Social Security Claiming Age Hit: {e.Date}, Age: {e.Age}");
    }

    private static void OnBroke(object sender, DatedEventArgs e)
    {
        Console.WriteLine($"Broke: {e.Date}");
    }

    private static void OnJobPay(object sender, JobPayEventArgs e)
    {
        var person = (Person)sender;
        var job = e.Job;
        var grossIncome = job.GrossAnnualIncome();
        double netIncome = grossIncome;

        // Calculate retirement contributions
        double personal401kContribution = grossIncome / (int)job.PayFrequency * (job.Personal401kContributionPercent.HasValue ? job.Personal401kContributionPercent.Value : 0);
        double companyMatchContribution = grossIncome / (int)job.PayFrequency * (job.CompanyMatchContributionPercent.HasValue ? job.CompanyMatchContributionPercent.Value * job.Personal401kContributionPercent.Value : 0);

        netIncome -= personal401kContribution;

        // Deposit contributions into retirement accounts
        var traditional401k = person.Investments.Accounts.FirstOrDefault(w => w is Traditional401kAccount);
        personal401kContribution = traditional401k?.Deposit(personal401kContribution, e.Date, TransactionCategory.ContributionPersonal) ?? 0;
        companyMatchContribution = traditional401k?.Deposit(companyMatchContribution, e.Date, TransactionCategory.ContributionEmployer) ?? 0;

        // Calculate taxes
        TaxCalculator taxCalculator = new(person, e.Date.Year);
        netIncome -= taxCalculator.GetTaxesOwed(netIncome);// Double counts if you have more than one job.
        netIncome /= (int)job.PayFrequency;

        // Deposit net income into taxable account
        var taxableAccount = person.Investments.Accounts.First(w => w.Name == nameof(AccountType.Savings));
        netIncome = taxableAccount.Deposit(netIncome, e.Date, TransactionCategory.Income);
        Console.WriteLine();

        if (netIncome > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"{e.Date}\t+ {netIncome:C} '{job.Title}'");
            Console.ResetColor();
        }
    }

    private static void OnMoneyShortfall(object sender, MoneyShortfallEventArgs e)
    {
        var person = (Person)sender;
        double shortfallAmount = e.ShortfallAmount;

        // Handle shortfall by withdrawing from accounts in a specific order
        List<AccountType> accountTypePriorities = [AccountType.Savings, AccountType.RothIRA, AccountType.TraditionalIRA, AccountType.Traditional401k];

        foreach (var accountType in accountTypePriorities)
        {
            var accounts = person.Investments.Accounts.Where(acc => acc.Type == accountType && acc.Balance(e.Date) > 0);
            foreach (var account in accounts)
            {
                double amountWithdrawn = account.Withdraw(shortfallAmount, e.Date, TransactionCategory.Expenses);
                shortfallAmount -= amountWithdrawn;

                if (shortfallAmount <= 0) break;
            }
        }

        if (shortfallAmount > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unable to cover shortfall of {shortfallAmount:C} on {e.Date}");
            Console.ResetColor();
            //throw new Exception($"Unable to cover shortfall of {shortfallAmount:C} on {e.Date} for {e.TransactionCategory}");
        }
    }

    private static void OnNewMonth(object sender, DatedEventArgs e)
    {
        Console.WriteLine($"New Month: {e.Date}");

        var person = (Person)sender;

        person.Investments.ApplyMonthlyGrowth(e.Date);

        // Trigger spending events for EssentialExpenses and DiscretionarySpending
        double totalExpenses = (person.EssentialExpenses + person.DiscretionarySpending) / 12;
        OnSpending(sender, new SpendingEventArgs { Date = e.Date, Amount = totalExpenses, TransactionCategory = TransactionCategory.Expenses });

        // Print monthly summary
        var taxableAccount = person.Investments.Accounts.FirstOrDefault(w => w.Name == nameof(AccountType.Savings));
        double totalIncome = taxableAccount?.DepositHistory.Where(d => d.Date.Year == e.Date.Year && d.Date.Month == e.Date.Month).Sum(d => d.Amount) ?? 0;
        double totalWithdrawn = taxableAccount?.WithdrawalHistory.Where(w => w.Date.Year == e.Date.Year && w.Date.Month == e.Date.Month).Sum(w => w.Amount) ?? 0;

        PrintMonthlySummary(new Dictionary<string, double> { { "Total", totalIncome } }, taxableAccount, totalExpenses, totalWithdrawn);
    }

    private static void OnSpending(object sender, SpendingEventArgs e)
    {
        var person = (Person)sender;
        double spendingAmount = e.Amount;

        Console.WriteLine($"Spending: {e.Date}, Amount: {spendingAmount:C} ({e.TransactionCategory})");

        // Handle spending by withdrawing from accounts in a specific order

        foreach (var accountType in (List<AccountType>)([AccountType.Savings, AccountType.RothIRA, AccountType.TraditionalIRA, AccountType.Traditional401k]))
        {
            var accounts = person.Investments.Accounts.Where(acc => acc.Type == accountType && acc.Balance(e.Date) > 0);
            foreach (var account in accounts)
            {
                double amountWithdrawn = account.Withdraw(spendingAmount, e.Date, e.TransactionCategory);
                spendingAmount -= amountWithdrawn;

                if (spendingAmount <= 0) break;
            }

            if (spendingAmount <= 0) break;
        }

        if (spendingAmount > 0)
        {
            OnMoneyShortfall(sender, new MoneyShortfallEventArgs { Date = e.Date, ShortfallAmount = spendingAmount, TransactionCategory = e.TransactionCategory });
        }
    }

    private static void PrintMonthlySummary(Dictionary<string, double> totalIncome, InvestmentAccount taxableAccount, double totalExpenses, double totalWithdrawn)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"Income: {string.Join('\t', totalIncome.Select(s => s.Key + "=" + s.Value.ToString("C")))} -> {taxableAccount?.Name}\t");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"Expenses: {totalExpenses:C}\t");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Withdrawn: {totalWithdrawn:C}");
        Console.ResetColor();
    }

    private static double CalculateRMD(Person person, DateOnly date)
    {
        double totalRMD = 0;
        foreach (Traditional401kAccount account in person.Investments.Accounts.Select(a => a as Traditional401kAccount).Where(w => w is not null))
        {
            totalRMD += account!.RequiredMinimalDistributions(date);
        }

        Console.WriteLine($"RMD: {totalRMD:C}");

        return totalRMD;
    }
}
