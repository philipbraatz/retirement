using System;
using System.Collections.Generic;

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

    public static double Calculate401kContributionMonthly(double salary, double personalRate, double employerRate)
    {
        double annualContribution = Math.Min(salary * personalRate, ContributionLimits.Max401kPersonal);
        double salaryForMatch = Math.Min(salary, ContributionLimits.CompensationLimit);
        double employerContribution = Math.Min(salaryForMatch * employerRate, ContributionLimits.Max401kTotal - annualContribution);

        double totalContribution = Math.Min(annualContribution + employerContribution, ContributionLimits.Max401kTotal);
        return totalContribution / 12; // Convert to monthly deposit
    }

    public static double CalculateRothIRAContributionMonthly(double salary)
    {
        return Math.Min(ContributionLimits.MaxRothIRA, salary * 0.10) / 12;
    }


    public static double ApplyCOLA(double benefit, int yearsUntilClaiming, double colaRate = 0.02)
    {
        return benefit * Math.Pow(1 + colaRate, yearsUntilClaiming);
    }

    public static double CalculatePIA(double averageEarnings)
    {
        double bendPoint1 = 1115;  // First bend point (2024 value)
        double bendPoint2 = 6721;  // Second bend point

        double pia = averageEarnings * 0.90;
        if (averageEarnings <= bendPoint2)
            pia += ((averageEarnings - bendPoint1) * 0.32);
        else
            pia += ((bendPoint2 - bendPoint1) * 0.32) + ((averageEarnings - bendPoint2) * 0.15);

        return pia;
    }

    public static double GetSocialSecurityMonthly(Person person, int age)
    {
        if (age >= person.SocialSecurityClaimingAge)
            return person.SocialSecurityIncome / 12;

        return 0; // No Social Security before claiming age
    }


    public static void AdjustContributions(Person person, int age, double salary)
    {
        if (age < person.PartTimeAge)
        {
            // Full-time: Contribute to 401(k) and Roth IRA
            double yearly401kContribution = Calculate401kContribution(salary, person.Personal401kContributionRate, person.EmployerMatchPercentage);
            double yearlyRothIRAContribution = CalculateRothIRAContribution(salary);

            person.Account401k.Deposit(yearly401kContribution / 12); // Monthly deposits
            person.RothIRA.Deposit(yearlyRothIRAContribution / 12);
        }
        else if (age >= person.PartTimeAge && age < person.PartTimeEndAge)
        {
            // Part-time: No 401(k) contributions, only Roth IRA (if possible)
            double yearlyRothIRAContribution = CalculateRothIRAContribution(salary);
            person.RothIRA.Deposit(yearlyRothIRAContribution / 12);
        }
    }

    public static double WithdrawWithTaxes(Person person, double amount)
    {
        double taxOwed = TaxBrackets.CalculateTaxes(amount);
        return amount - taxOwed; // Net withdrawal after taxes
    }

    public static double CalculateRMDMonthly(Person person)
    {
        if (person.CurrentAge < 73) return 0; // No RMD before 73

        double lifeExpectancy = LifeExpectancyTable.GetLifeExpectancy(person.CurrentAge);
        double rmd = person.Account401k.Balance / lifeExpectancy;

        double taxOwed = TaxBrackets.CalculateTaxes(rmd);
        double afterTaxWithdrawal = rmd - taxOwed;

        person.Account401k.Withdraw(rmd);
        return afterTaxWithdrawal / 12; // Convert yearly RMD to monthly withdrawal
    }


    public static double CalculateSEPP(Person person)
    {
        double lifeExpectancy = LifeExpectancyTable.GetLifeExpectancy(person.CurrentAge);
        return person.Account401k.Balance / lifeExpectancy / 12; // Monthly withdrawal
    }


    public static void RunRetirementSimulation(Person person, TimeSpan timeStep)
    {
        DateTime startDate = new DateTime(DateTime.Now.Year, 1, 1);
        DateTime endDate = new DateTime(person.BirthDate.Year + 100, 1, 1);
        DateTime currentDate = startDate;

        List<FinancialSnapshot> history = new List<FinancialSnapshot>();

        double adjustedSalary = person.CurrentSalary; // Start with initial salary

        while (currentDate < endDate)
        {
            int age = currentDate.Year - person.BirthDate.Year;
            bool isRetired = age >= person.FullRetirementAge;

            // Apply salary increases ONLY on January 1st
            if (currentDate.Month == 1 && currentDate.Day == 1)
            {
                adjustedSalary *= (1 + person.SalaryGrowthRate);
            }

            // Get monthly Social Security income
            double socialSecurityIncome = GetSocialSecurityMonthly(person, age);

            // Total expenses (needs + wants)
            double totalExpenses = (person.EssentialExpenses + person.DiscretionarySpending) / 12;

            // Initial income sources
            double totalIncome = (adjustedSalary / 12) + socialSecurityIncome;
            double withdrawalNeeded = Math.Max(0, totalExpenses - totalIncome);
            double totalWithdrawn = 0;

            // Withdraw from surplus first
            if (withdrawalNeeded > 0 && person.SurplusAccount.Balance > 0)
            {
                double fromSurplus = Math.Min(person.SurplusAccount.Balance, withdrawalNeeded);
                person.SurplusAccount.Withdraw(fromSurplus);
                withdrawalNeeded -= fromSurplus;
                totalIncome += fromSurplus;
            }

            // Withdraw from retirement accounts if needed
            if (withdrawalNeeded > 0)
            {
                totalWithdrawn = WithdrawWithTaxes(person, withdrawalNeeded);
                totalIncome += totalWithdrawn;
            }

            // If extra money is available, save to surplus
            double surplusSaved = Math.Max(0, totalIncome - totalExpenses);
            if (surplusSaved > 0)
            {
                person.SurplusAccount.Deposit(surplusSaved);
            }

            // Store history with exact date
            history.Add(new FinancialSnapshot
            {
                Date = currentDate,
                Salary = adjustedSalary,
                SocialSecurityIncome = socialSecurityIncome * 12,
                TotalIncome = totalIncome * 12,
                EssentialExpenses = person.EssentialExpenses,
                DiscretionarySpending = person.DiscretionarySpending,
                TotalExpenses = totalExpenses * 12,
                Withdrawals = totalWithdrawn * 12,
                SurplusSaved = surplusSaved * 12,
                SurplusBalance = person.SurplusAccount.Balance,
                Account401kBalance = person.Account401k.Balance,
                RothIRABalance = person.RothIRA.Balance,
                TaxableBalance = person.TaxableAccount.Balance
            });

            // Move to the next time step (monthly, weekly, etc.)
            currentDate = currentDate.Add(timeStep);
        }

        // Export results
        ExportToCSV(history, "retirement_simulation.csv");
    }


}
