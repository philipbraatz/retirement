using RetirementPlanner.IRS;
using static RetirementPlanner.TaxBrackets;

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


    public static double GetSocialSecurityYearly(Person person, int age)
    {
        if (age >= person.SocialSecurityClaimingAge)
            return person.SocialSecurityIncome;

        return 0; // No Social Security before claiming age
    }


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

    public static double WithdrawWithTaxes(FileType fileType, double amount)
    {
        double taxOwed = TaxBrackets.CalculateTaxes(fileType, amount);
        return amount - taxOwed; // Net withdrawal after taxes
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
        double totalTaxableIncome = 0;

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


    public static void RunRetirementSimulation(Person person, TimeSpan timeStep)
    {
        DateTime startDate = new DateTime(DateTime.Now.Year, 1, 1);
        DateTime endDate = new DateTime(person.BirthDate.Year + 100, 1, 1);
        DateTime currentDate = startDate;

        List<FinancialSnapshot> history = [];
        double adjustedSalary = person.CurrentSalary;
        double yearlyTaxesOwed = 0;

        while (currentDate < endDate)
        {
            int age = currentDate.Year - person.BirthDate.Year;
            var isRetired = age >= person.FullRetirementAge;

            // Apply salary increases ONLY on January 1st
            if (currentDate.Month == 1 && currentDate.Day == 1)
            {
                adjustedSalary *= (1 + person.SalaryGrowthRate);
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
        socialSecurityIncome = GetSocialSecurityYearly(person, age) / 12;

        // Total monthly expenses
        totalExpenses = (person.EssentialExpenses + person.DiscretionarySpending);

        // Total monthly income
        totalIncome = (adjustedSalary / 12) + socialSecurityIncome;
        double withdrawalNeeded = Math.Max(0, totalExpenses - totalIncome);
        totalWithdrawn = 0;

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
    }

    private static void TryWithdraw(Person person, AccountType accountType, ref double totalIncome, ref double withdrawalNeeded)
    {
        if (withdrawalNeeded <= 0) return;

        var accounts = person.Accounts.Where(w => w.Type == accountType && w.Balance > 0);
        foreach (var account in accounts)
        {
            double amountWithdrawn = account.Withdraw(withdrawalNeeded);
            withdrawalNeeded -= amountWithdrawn;
            totalIncome += amountWithdrawn;

            if (withdrawalNeeded <= 0) break;
        }
    }

}