namespace RetirementPlanner;

public class RetirementPlanner
{
    public static double AdjustSalary(double currentSalary, double growthRate, int years)
    {
        return currentSalary * Math.Pow(1 + growthRate, years);
    }

    public static double CalculateSEPP(Person person, DateTime date)
    {
        //double lifeExpectancy = LifeExpectancyTable.GetLifeExpectancy(person.CurrentAge(date), person.GenderMale);
        return 0; //return person.GetAccount(AccountType.Traditional401k).Balance / lifeExpectancy;
    }

    public static double GetTaxableSocialSecurity(Person person, double totalIncome)
    {
        double provisionalIncome = totalIncome + (person.SocialSecurityIncome * 0.5);

        if (provisionalIncome <= 25000) return 0; // No SS taxation (single)
        if (provisionalIncome <= 34000) return person.SocialSecurityIncome * 0.50; // 50% taxable
        return person.SocialSecurityIncome * 0.85; // 85% taxable
    }

    public static void RunRetirementSimulation(Person person)
    {
        DateOnly startDate = new(DateTime.Now.Year, 1, 1);
        DateOnly endDate = new(person.BirthDate.Year + 100, 1, 1);
        DateOnly currentDate = startDate;

        List<FinancialSnapshot> history = new();
        double yearlyTaxesOwed = 0;

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"Started retirement for {person.BirthDate} with {person.Investments.Accounts.Count} accounts");
        Console.ResetColor();

        while (currentDate < endDate)
        {
            int age = currentDate.Year - person.BirthDate.Year;
            bool isRetired = age >= person.FullRetirementAge;

            // Apply salary increases and adjust contributions on January 1st
            if (currentDate.Month == 1 && currentDate.Day == 1)
            {
                person.ApplyYearlyPayRaises();
                person.EssentialExpenses *= (1 + person.InflationRate);
                person.DiscretionarySpending *= (1 + person.InflationRate);
            }

            SimulateMonth(person, currentDate, age, out double totalExpenses, out double totalIncome, out double totalWithdrawn);

            // Apply yearly taxes at the end of December
            if (currentDate.Month == 12)
            {
                yearlyTaxesOwed = ApplyYearlyTaxes(person);
            }

        // Collect data for each account
        foreach (var account in person.Investments.Accounts)
        {
            double deposits = account.DepositHistory.Where(d => d.Date.Year == currentDate.Year && d.Date.Month == currentDate.Month).Sum(d => d.Amount);
            double withdrawals = account.WithdrawalHistory.Where(w => w.Date.Year == currentDate.Year && w.Date.Month == currentDate.Month).Sum(w => w.Amount);
            double balance = account.Balance(currentDate);

            history.Add(new MonthlyAccountSummary
            {
                Date = currentDate,
                AccountName = account.Name,
                Deposits = deposits,
                Withdrawals = withdrawals,
                TotalBalance = balance
            });
        }


            PrintColoredLine($"Age {person.CurrentAge(currentDate)}\t", person.Investments.Accounts.Select(a => $"{a.Name}={a.Balance:C}").ToArray(), (person.EssentialExpenses + person.DiscretionarySpending) / 12);

            // Check if all balances are zero
            if (person.Investments.Accounts.All(a => a.Balance(currentDate) <= 0))
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"All account balances are zero at age {currentDate.Year - person.BirthDate.Year}. Goal of {person.PartTimeEndAge}/{person.FullRetirementAge}. Ending simulation.");
                Console.ResetColor();
                break;
            }

            // Move to the next time step
            currentDate = currentDate.AddMonths(1);
        }

        // Export results
        Helpers.ExportToCSV(history, $"retirement_simulation_{person.FullRetirementAge}_{person.BirthDate.Year}.csv");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("Exported");
        Console.ResetColor();
    }

    private static void SimulateMonth(Person person, DateOnly date, int age, out double totalExpenses, out double totalIncome, out double totalWithdrawn)
    {
        // Calculate Income Before Taxes
        if (person.SocialSecurityIncome == 0)
        {
            person.SocialSecurityIncome = (age >= person.SocialSecurityClaimingAge)
                ? person.CalculateCurrentSocialSecurityBenefits(date.ToDateTime(TimeOnly.MinValue))
                : 0;
        }

        totalExpenses = (person.EssentialExpenses + person.DiscretionarySpending) / 12;
        totalIncome = person.CalculateTotalNetPay() / 12 + person.SocialSecurityIncome;

        // Deduct Taxes from Total Income
        double monthlyTaxOwed = ApplyYearlyTaxes(person) / 12;
        totalIncome = Math.Max(0, totalIncome - monthlyTaxOwed);

        // Deposit Post-Tax Income into Taxable Account
        var taxableAccount = person.Investments.Accounts.FirstOrDefault(w => w.Name == nameof(AccountType.Savings));
        taxableAccount?.Deposit(totalIncome, date, TransactionCategory.Income);

        double withdrawalNeeded = Math.Max(0, totalExpenses);
        totalWithdrawn = 0;

        // Apply Required Minimum Distributions (RMDs) first at age 73+
        double rmdWithdrawn = 0;
        if (age >= 73)
        {
            var rothAccount = person.Investments.Accounts.FirstOrDefault(w => w is RothIRAAccount);

            rmdWithdrawn = ProcessRMDWithdrawal(person, date);
            rothAccount?.Deposit(rmdWithdrawn, date, TransactionCategory.InternalTransfer);
        }

        // Perform Roth Conversion AFTER RMDs to Reflect Tax Impact
        if (age >= 59.5)
        {
            person.Investments.PerformRothConversion(person, date);
        }

        // Withdraw in Tax-Efficient Order
        List<AccountType> accountTypePriorities = [AccountType.Savings];

        if (age > person.PartTimeEndAge)
        {
            accountTypePriorities.Add(AccountType.RothIRA);

            if (age >= 59.5)
            {
                accountTypePriorities.Add(AccountType.TraditionalIRA);
                accountTypePriorities.Add(AccountType.Traditional401k);
            }
            else
            {
                accountTypePriorities.Add(AccountType.Traditional401k);
                accountTypePriorities.Add(AccountType.TraditionalIRA);
            }
        }

        foreach (var accountType in accountTypePriorities)
        {
            TryWithdraw(person, date, accountType, ref withdrawalNeeded);
        }

        // If Still Short on Funds, Withdraw from Other Accounts (With Penalties if Needed)
        if (withdrawalNeeded > 0 && age <= person.PartTimeEndAge)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Shortfall detected: {withdrawalNeeded:C}. Pulling from other accounts...");
            Console.ResetColor();

            List<AccountType> penaltyAccounts = new() { AccountType.Traditional401k, AccountType.TraditionalIRA, AccountType.RothIRA };
            foreach (var accountType in penaltyAccounts)
            {
                TryWithdraw(person, date, accountType, ref withdrawalNeeded);
                if (withdrawalNeeded <= 0) break;
            }
        }

        // Apply Investment Growth
        person.Investments.ApplyMonthlyGrowth(date);

        // Print monthly summary
        PrintMonthlySummary(totalIncome, taxableAccount, totalExpenses, totalWithdrawn);
    }

    public static void TryWithdraw(Person person, DateOnly date, AccountType accountType, ref double withdrawalNeeded)
    {
        if (withdrawalNeeded <= 0) return;

        var accounts = person.Investments.Accounts.Where(acc => acc.Name == accountType.ToString() && acc.Balance(date) > 0);
        foreach (var account in accounts)
        {
            double amountWithdrawn = account.Withdraw(withdrawalNeeded, date, TransactionCategory.Withdrawal);
            withdrawalNeeded -= amountWithdrawn;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\t Withdrawing {amountWithdrawn:C} from {account.Name}");
            Console.ResetColor();

            if (withdrawalNeeded <= 0) break;
        }
    }

    private static double ProcessRMDWithdrawal(Person person, DateOnly date)
    {
        double totalRMD = 0;
        foreach (TraditionalAccount account in person.Investments.Accounts.Select(a => a as TraditionalAccount).Where(w => w is not null))
        {
            double rmd = account!.RequiredMinimalDistributions(date);
            totalRMD += rmd;
            account.Withdraw(rmd, date, TransactionCategory.Withdrawal);
        }
        return totalRMD;
    }

    private static double ApplyYearlyTaxes(Person person)
    {
        double totalTaxableIncome = person.Jobs.Sum(s => s.CalculateTaxableIncome());

        // Include taxable portion of Social Security
        totalTaxableIncome += GetTaxableSocialSecurity(person, person.IncomeYearly);

        // Sum taxable withdrawals from tax-deferred accounts (401k, Traditional IRA)
        totalTaxableIncome += person.Investments.Accounts.Select(a => a as TraditionalAccount).Where(w => w is not null)
            .Sum(s => s!.WithdrawalHistory.Where(w => w.Date.Year == DateTime.Now.Year).Sum(s => s.Amount));

        return TaxBrackets.CalculateTaxes(person.FileType, Math.Max(0, totalTaxableIncome));
    }

    private static void PrintColoredLine(string text, string[] moneyValues, double monthlyExpenses)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(text);
        foreach (var moneyValue in moneyValues)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{moneyValue}\t");
        }
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"\t PAID: {monthlyExpenses:C}");
        Console.ResetColor();
    }

    private static void PrintMonthlySummary(double totalIncome, InvestmentAccount taxableAccount, double totalExpenses, double totalWithdrawn)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"Income: {totalIncome:C} -> {taxableAccount?.Name}\t");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"Expenses: {totalExpenses:C}\t");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Withdrawn: {totalWithdrawn:C}");
        Console.ResetColor();
    }
}

