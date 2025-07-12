using RetirementPlanner;
using RetirementPlanner.Event;
using RetirementPlanner.Test;

namespace DebugApp;

public static class DebugYearOverYear
{
    public static void TestYearOverYearReporting()
    {
        Console.WriteLine("=== Debug Year-Over-Year Reporting ===");
        
        var person = TestData.CreateMinimalPerson();
        
        // Subscribe to events
        LifeEvents.Subscribe(null);
        
        // Run a few years of simulation
        var sim = new BasicRetirementPlanner();
        sim.RunSimulation(person, person.BirthDate.AddYears(30), person.BirthDate.AddYears(35));
        
        Console.WriteLine("\n=== Manual Balance Check ===");
        
        // Check balances manually
        var account = person.Investments.Accounts.First();
        Console.WriteLine($"Account: {account.Name}");
        
        // Check balances for specific dates
        var year1End = new DateOnly(person.BirthDate.Year + 31, 12, 31);
        var year2End = new DateOnly(person.BirthDate.Year + 32, 12, 31);
        var year3End = new DateOnly(person.BirthDate.Year + 33, 12, 31);
        var year4End = new DateOnly(person.BirthDate.Year + 34, 12, 31);
        
        var balance1 = account.Balance(year1End);
        var balance2 = account.Balance(year2End);
        var balance3 = account.Balance(year3End);
        var balance4 = account.Balance(year4End);
        
        Console.WriteLine($"Year 1 (end {year1End}): {balance1:C}");
        Console.WriteLine($"Year 2 (end {year2End}): {balance2:C}");
        Console.WriteLine($"Year 3 (end {year3End}): {balance3:C}");
        Console.WriteLine($"Year 4 (end {year4End}): {balance4:C}");
        
        Console.WriteLine($"\nYear-over-year changes:");
        Console.WriteLine($"Year 1 to 2: {balance2 - balance1:C}");
        Console.WriteLine($"Year 2 to 3: {balance3 - balance2:C}");
        Console.WriteLine($"Year 3 to 4: {balance4 - balance3:C}");
        
        // Also test the logic from Events.cs
        Console.WriteLine("\n=== Testing Events.cs Logic ===");
        var currentYear = person.BirthDate.Year + 34; // Simulate being at start of year 5
        
        var twoYearsAgo = currentYear - 2; // Year 3
        var lastYear = currentYear - 1; // Year 4
        
        var twoYearsAgoEndDate = new DateOnly(twoYearsAgo, 12, 31);
        var lastYearEndDate = new DateOnly(lastYear, 12, 31);
        
        var twoYearsAgoBalance = account.Balance(twoYearsAgoEndDate);
        var lastYearBalance = account.Balance(lastYearEndDate);
        
        var balanceChange = lastYearBalance - twoYearsAgoBalance;
        
        Console.WriteLine($"Events.cs logic - Year {twoYearsAgo} Balance: {twoYearsAgoBalance:C}");
        Console.WriteLine($"Events.cs logic - Year {lastYear} Balance: {lastYearBalance:C}");
        Console.WriteLine($"Events.cs logic - Change: {balanceChange:C}");
    }
}

public static class TestData
{
    public static Person CreateMinimalPerson()
    {
        var birthDate = new DateOnly(1990, 1, 1);
        var person = new Person
        {
            Name = "Test Person",
            BirthDate = birthDate,
            RetirementAge = 65,
            LifeExpectancy = 85,
            SocialSecurityClaimingAge = 67,
            SocialSecurityWageBase = 50000,
            EssentialExpenses = 40000,
            DiscretionarySpending = 10000,
            Investments = new InvestmentManager(),
            InflationRate = 0.03,
            Jobs = new List<IncomeSource>
            {
                new IncomeSource
                {
                    Title = "Software Engineer",
                    StartDate = birthDate.AddYears(22),
                    EndDate = birthDate.AddYears(65),
                    Salary = 80000,
                    PaymentType = PaymentType.Salaried,
                    PayFrequency = PayFrequency.Monthly,
                    RetirementContributionPercent = 0.15,
                    CompanyMatchContributionPercent = 0.05,
                    AnnualSalaryGrowthRate = 0.03
                }
            }
        };

        // Add a simple 401k account
        person.Investments.Accounts.Add(new Traditional401kAccount(0.05, "401k", birthDate, 5000));
        
        return person;
    }
}
