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
        double retirementContribution = grossIncome * (person.Jobs[0].RetirementContributionPercent.HasValue ? person.Jobs[0].RetirementContributionPercent.Value : 0);
        double companyMatchContribution = grossIncome * (person.Jobs[0].CompanyMatchContributionPercent.HasValue ? person.Jobs[0].CompanyMatchContributionPercent.Value * (person.Jobs[0].RetirementContributionPercent ?? 0) : 0);

        person.TaxableIncome -= retirementContribution;

        // Update RMD IncomeSource every new year
        var rmdIncomeSource = person.Jobs.FirstOrDefault(j => j.Title == "RMD");
        if (rmdIncomeSource != null)
        {
            rmdIncomeSource.Salary = CalculateRMD(person, e.Date) / 12;
        }
        // Print the change in balances for each account from two years ago to last year
        // This gives us a meaningful year-over-year comparison since we're at the start of the current year
        foreach (var account in person.Investments.Accounts)
        {
            var twoYearsAgo = e.Date.Year - 2;
            var lastYear = e.Date.Year - 1;

            // Compare balance from end of two years ago to end of last year
            var twoYearsAgoEndDate = new DateOnly(twoYearsAgo, 12, 31);
            var lastYearEndDate = new DateOnly(lastYear, 12, 31);

            var twoYearsAgoBalance = account.Balance(twoYearsAgoEndDate);
            var lastYearBalance = account.Balance(lastYearEndDate);

            var balanceChange = lastYearBalance - twoYearsAgoBalance;

            // Only show the comparison if both years have meaningful data
            // Skip if both balances are the same due to missing YearlyStartingBalances
            if (balanceChange != 0 || account.YearlyStartingBalances.ContainsKey(twoYearsAgo) || account.YearlyStartingBalances.ContainsKey(lastYear))
            {
                Console.WriteLine($"Account: {account.Name}, Year {twoYearsAgo} Balance: {twoYearsAgoBalance:C}, Year {lastYear} Balance: {lastYearBalance:C}, Change: {balanceChange:C}");
            }
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

        // Determine optimal allocation between Traditional and Roth 401k
        var (traditionalAmount, rothAmount) = job.CalculateOptimalRetirementAllocation(person, e.Date);
        
        // Scale contributions to paycheck frequency
        traditionalAmount /= (int)job.PayFrequency;
        rothAmount /= (int)job.PayFrequency;
        
        // Total retirement contribution per paycheck
        double retirementContribution = traditionalAmount + rothAmount;
        double companyMatchContribution = grossIncome / (int)job.PayFrequency * (job.CompanyMatchContributionPercent.HasValue ? job.CompanyMatchContributionPercent.Value * (job.RetirementContributionPercent ?? 0) : 0);

        netIncome -= retirementContribution;

        // Deposit contributions into retirement accounts
        var traditional401k = person.Investments.Accounts.FirstOrDefault(w => w is Traditional401kAccount);
        var roth401k = person.Investments.Accounts.FirstOrDefault(w => w is Roth401kAccount);
        
        double actualTraditionalContribution = 0;
        double actualRothContribution = 0;
        
        if (traditionalAmount > 0 && traditional401k != null)
        {
            actualTraditionalContribution = traditional401k.Deposit(traditionalAmount, e.Date, TransactionCategory.ContributionPersonal);
        }
        
        if (rothAmount > 0 && roth401k != null)
        {
            actualRothContribution = roth401k.Deposit(rothAmount, e.Date, TransactionCategory.ContributionPersonal);
        }
        
        // If no Roth 401k account exists but we want Roth contributions, put in Traditional 401k
        if (rothAmount > 0 && roth401k == null && traditional401k != null)
        {
            actualTraditionalContribution += traditional401k.Deposit(rothAmount, e.Date, TransactionCategory.ContributionPersonal);
            Console.WriteLine($"\t Note: Roth 401k not available, contributed ${rothAmount:C} to Traditional 401k instead");
        }
        
        // If no Traditional 401k account exists but we want Traditional contributions, put in Roth 401k
        if (traditionalAmount > 0 && traditional401k == null && roth401k != null)
        {
            actualRothContribution += roth401k.Deposit(traditionalAmount, e.Date, TransactionCategory.ContributionPersonal);
            Console.WriteLine($"\t Note: Traditional 401k not available, contributed ${traditionalAmount:C} to Roth 401k instead");
        }

        // Company match goes to Traditional 401k by default (most common)
        double actualCompanyMatch = traditional401k?.Deposit(companyMatchContribution, e.Date, TransactionCategory.ContributionEmployer) ?? 0;

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

    internal static void OnSpending(object sender, SpendingEventArgs e)
    {
        var person = (Person)sender;
        double spendingAmount = e.Amount;
        var currentAge = person.CurrentAge(e.Date);

        Console.WriteLine($"Spending: {e.Date}, Amount: {spendingAmount:C} ({e.TransactionCategory})");

        // Age-aware withdrawal strategy
        var withdrawalOrder = GetOptimalWithdrawalOrder(currentAge);

        foreach (var accountType in withdrawalOrder)
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

    /// <summary>
    /// Returns the optimal withdrawal order based on current age and tax considerations
    /// </summary>
    internal static AccountType[] GetOptimalWithdrawalOrder(int currentAge)
    {
        if (currentAge < 59.5)
        {
            // Early retirement strategy: Prioritize penalty-free sources
            return new[]
            {
                AccountType.Savings,           // Always penalty-free
                AccountType.Roth401k,          // Contributions penalty-free after 5 years
                AccountType.RothIRA,           // Contributions always penalty-free
                AccountType.HSA,               // Medical expenses penalty-free
                AccountType.TraditionalIRA,    // 10% penalty but may be necessary
                AccountType.Traditional401k    // 10% penalty - last resort
            };
        }
        else if (currentAge < 73)
        {
            // Post-59.5 to RMD age: Prioritize Traditional accounts to reduce future RMDs
            return new[]
            {
                AccountType.Savings,           // Always penalty-free, use first
                AccountType.Traditional401k,   // No penalty, reduce future RMDs
                AccountType.TraditionalIRA,    // No penalty, reduce future RMDs
                AccountType.HSA,               // Tax-free for medical, preserve if possible
                AccountType.Roth401k,          // Tax-free growth, preserve for later
                AccountType.RothIRA            // Tax-free growth, no RMDs, preserve
            };
        }
        else
        {
            // RMD age and beyond: More balanced approach
            // If Traditional accounts are getting low relative to Roth, start using Roth more aggressively
            return new[]
            {
                AccountType.Savings,           // Always use first
                AccountType.Traditional401k,   // RMDs required anyway, but don't over-deplete
                AccountType.TraditionalIRA,    // RMDs required anyway, but don't over-deplete
                AccountType.Roth401k,          // Start using Roth more in later retirement
                AccountType.HSA,               // Still tax-free for medical
                AccountType.RothIRA            // Preserve for heirs, but use if needed
            };
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
        foreach (Traditional401kAccount account in person.Investments.Accounts.OfType<Traditional401kAccount>())
        {
            totalRMD += account.RequiredMinimalDistributions(date);
        }

        Console.WriteLine($"RMD: {totalRMD:C}");

        return totalRMD;
    }
}
