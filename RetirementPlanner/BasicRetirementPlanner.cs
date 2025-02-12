using RetirementPlanner.IRS;

namespace RetirementPlanner;

public class RetirementPlanner
{
    public static double AdjustSalary(double currentSalary, double growthRate, int years)
    {
        return currentSalary * Math.Pow(1 + growthRate, years);
    }


    public static double CalculateSalary(Person person, int age)
    => age switch
    {
        _ when age < person.PartTimeAge => AdjustSalary(person.CurrentSalary, person.SalaryGrowthRate, age - (DateTime.Now.Year - person.BirthDate.Year)),
        _ when age >= person.PartTimeAge && age < person.PartTimeEndAge => person.PartTimeSalary,
        _ => 0  // Fully retired
    };

    public static void AdjustContributionsYearly(Person person, int age, double salary)
    {
        if (age < person.PartTimeAge)
        {
            // Full-time: Contribute to 401(k) and Roth IRA
            double yearly401kContribution = ContributionLimits.Calculate401kContributionYearly(salary, person.Personal401kContributionRate, person.EmployerMatchPercentage);
            double yearlyRothIRAContribution = ContributionLimits.CalculateRothIRAContributionYearly(salary);

            person.GetAccount(AccountType.Traditional401k).Deposit(yearly401kContribution);
            person.GetAccount(AccountType.RothIRA).Deposit(yearlyRothIRAContribution);
        }
        else if (age >= person.PartTimeAge && age < person.PartTimeEndAge)
        {
            // Part-time: No 401(k) contributions, only Roth IRA (if possible)
            double yearlyRothIRAContribution = ContributionLimits.CalculateRothIRAContributionYearly(salary);
            person.GetAccount(AccountType.RothIRA).Deposit(yearlyRothIRAContribution);
        }
    }

    public static double ProcessRMDWithdrawal(Person person)
    {
        var accounts = person.Accounts.Where(acc => (acc.Type == AccountType.Traditional401k || acc.Type == AccountType.TraditionalIRA) && acc.Balance > 0);
        if (!(person.CurrentAge >= 73 && accounts.Any())) return 0; // No RMD before 73

        double totalRMDWithdrawn = 0;
        foreach (var account in accounts)
        {
            double rmd = account.Balance / LifeExpectancyTable.GetLifeExpectancy(person.CurrentAge, person.GenderMale);
            double withdrawn = account.Withdraw(rmd);
            totalRMDWithdrawn += withdrawn;
        }

        return totalRMDWithdrawn;
    }

    public static double CalculateSEPP(Person person)
    {
        double lifeExpectancy = LifeExpectancyTable.GetLifeExpectancy(person.CurrentAge, person.GenderMale);
        return person.GetAccount(AccountType.Traditional401k).Balance / lifeExpectancy;
    }

    public static double ApplyYearlyTaxes(Person person)
    {
        double totalTaxableIncome = person.RothConversionsThisYear;
        person.RothConversionsThisYear = 0;

        totalTaxableIncome += GetTaxableSocialSecurity(person, person.IncomeYearly);

        // Sum taxable withdrawals from all tax-deferred accounts
        foreach (var account in person.Accounts)
        {
            if (account.Type == AccountType.Traditional401k || account.Type == AccountType.TraditionalIRA)
            {
                totalTaxableIncome += account.TaxableWithdrawalsThisYear;
            }
        }

        // Calculate taxes
        double taxesOwed = TaxBrackets.CalculateTaxes(person.FileType, totalTaxableIncome);

        // Reset tracking for the next year
        foreach (var account in person.Accounts)
        {
            account.ResetYearlyTaxTracking();
        }

        return taxesOwed;
    }

    private static void PerformRothConversion(Person person)
    {
        double conversionLimit = TaxBrackets.GetOptimalRothConversionAmount(person);

        if (conversionLimit <= 0) return; // No conversion if already in high tax bracket

        var traditionalAccounts = person.Accounts.Where(a => a.Type == AccountType.Traditional401k || a.Type == AccountType.TraditionalIRA);
        var rothAccount = person.GetAccount(AccountType.RothIRA);

        if (rothAccount == null) return; // No Roth IRA available

        foreach (var account in traditionalAccounts)
        {
            double conversionAmount = Math.Min(account.Balance, conversionLimit);
            account.Withdraw(conversionAmount);
            rothAccount.Deposit(conversionAmount);
            person.RothConversionsThisYear += conversionAmount;
            conversionLimit -= conversionAmount;
        }
    }

    public static double GetTaxableSocialSecurity(Person person, double totalIncome)
    {
        double provisionalIncome = totalIncome + (person.SocialSecurityIncome * 0.5);

        if (provisionalIncome <= 25000) return 0; // No SS taxation (single)
        if (provisionalIncome <= 34000) return person.SocialSecurityIncome * 0.50; // 50% taxable
        return person.SocialSecurityIncome * 0.85; // 85% taxable
    }


    public static void RunRetirementSimulation(Person person, TimeSpan timeStep)
    {
        DateTime startDate = new(DateTime.Now.Year, 1, 1);
        DateTime endDate = new(person.BirthDate.Year + 100, 1, 1);
        DateTime currentDate = startDate;

        List<FinancialSnapshot> history = new();
        double adjustedSalary = person.CurrentSalary;
        double yearlyTaxesOwed = 0;

        while (currentDate < endDate)
        {
            int age = currentDate.Year - person.BirthDate.Year;
            bool isRetired = age >= person.FullRetirementAge;

            // Apply salary increases and adjust contributions on January 1st
            if (currentDate.Month == 1 && currentDate.Day == 1)
            {
                adjustedSalary *= (1 + person.SalaryGrowthRate);
                AdjustContributionsYearly(person, age, adjustedSalary);
            }

            SimulateMonth(person, adjustedSalary, age, out double socialSecurityIncome, out double totalExpenses, out double totalIncome, out double totalWithdrawn);

            // Apply yearly taxes at the end of December
            if (currentDate.Month == 12)
            {
                yearlyTaxesOwed = ApplyYearlyTaxes(person);
            }

            // Store history with exact date
            history.Add(new FinancialSnapshot
            {
                Date = currentDate,
                Salary = adjustedSalary,
                SocialSecurityIncome = socialSecurityIncome,
                TotalIncome = totalIncome,
                EssentialExpenses = person.EssentialExpenses,
                DiscretionarySpending = person.DiscretionarySpending,
                TotalExpenses = totalExpenses,
                Withdrawals = totalWithdrawn,
                SurplusBalance = person.Accounts.Where(w => w.Type == AccountType.Taxable).Sum(s => s.Balance),
                Account401kBalance = person.Accounts.Where(w => w.Type == AccountType.Traditional401k).Sum(s => s.Balance),
                RothIRABalance = person.Accounts.Where(w => w.Type == AccountType.RothIRA).Sum(s => s.Balance)
            });

            // Move to the next time step
            currentDate = currentDate.Add(timeStep);
        }

        // Export results
        Helpers.ExportToCSV(history, "retirement_simulation.csv");
    }


    private static void SimulateMonth(Person person, double adjustedSalary, int age, out double socialSecurityIncome, out double totalExpenses, out double totalIncome, out double totalWithdrawn)
    {
        // Social Security income
        socialSecurityIncome = socialSecurityIncome = (age >= person.SocialSecurityClaimingAge)
        ? SocialSecurity.CalculateSocialSecurityBenefit(person.BirthDate.Year, person.SocialSecurityClaimingAge, person.CurrentSalary) / 12
        : 0;

        // Total monthly expenses
        totalExpenses = person.EssentialExpenses + person.DiscretionarySpending;

        // Total monthly income
        totalIncome = (adjustedSalary / 12) + socialSecurityIncome;
        double withdrawalNeeded = Math.Max(0, totalExpenses - totalIncome);
        totalWithdrawn = 0;

        // Perform Roth conversion before RMDs
        if (age >= 59.5)
        {
            PerformRothConversion(person);
        }

        // Apply RMDs first at age 73+
        if (age >= 73)
        {
            double rmdWithdrawn = ProcessRMDWithdrawal(person);
            withdrawalNeeded -= rmdWithdrawn;
            totalIncome += rmdWithdrawn;
        }

        // Attempt withdrawals in priority order (Taxable -> Roth -> Traditional401k -> TraditionalIRA)
        List<AccountType> accountTypePriorities = [AccountType.Taxable, AccountType.RothIRA, AccountType.Traditional401k, AccountType.TraditionalIRA];
        foreach (var accountType in accountTypePriorities)
        {
            TryWithdraw(person, accountType, ref totalIncome, ref withdrawalNeeded);
        }

        foreach (var account in person.Accounts)
        {
            account.ApplyMonthlyGrowth();
        }
    }

    private static void TryWithdraw(Person person, AccountType accountType, ref double totalIncome, ref double withdrawalNeeded)
    {
        if (withdrawalNeeded <= 0) return;

        var accounts = person.Accounts.Where(acc => acc.Type == accountType && acc.Balance > 0);
        foreach (var account in accounts)
        {
            double amountWithdrawn = 0;

            // If under 59.5 and using Traditional 401(k)/IRA, use SEPP
            if (accountType == AccountType.Traditional401k || accountType == AccountType.TraditionalIRA)
            {
                amountWithdrawn = person.CurrentAge < 59.5 ? CalculateSEPP(person) / 12 : account.Withdraw(withdrawalNeeded);
            }
            else
            {
                amountWithdrawn = account.Withdraw(withdrawalNeeded);
            }

            withdrawalNeeded -= amountWithdrawn;
            totalIncome += amountWithdrawn;

            if (withdrawalNeeded <= 0) break;
        }
    }




}