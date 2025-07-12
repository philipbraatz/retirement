using RetirementPlanner;
using RetirementPlanner.Event;
using RetirementPlanner.Test;

namespace DebugApp;

public static class SimpleYearOverYearDebug
{
    public static void TestSimpleYearOverYear()
    {
        Console.WriteLine("=== Simple Year-Over-Year Debug ===");
        
        // Use the existing TestPersonFactory from the Test project
        var person = TestPersonFactory.Me();
        
        Console.WriteLine($"Person birth year: {person.BirthDate.Year}");
        Console.WriteLine($"Current date: {DateTime.Now:yyyy-MM-dd}");
        
        // Test the Events.cs logic directly with manual dates
        Console.WriteLine("\n=== Testing Events.cs Year-Over-Year Logic ===");
        
        // Simulate we're at the start of 2027 (year 5 of simulation)
        var currentDate = new DateOnly(2027, 1, 1);
        var twoYearsAgo = currentDate.Year - 2; // 2025
        var lastYear = currentDate.Year - 1;     // 2026
        
        var twoYearsAgoEndDate = new DateOnly(twoYearsAgo, 12, 31);
        var lastYearEndDate = new DateOnly(lastYear, 12, 31);
        
        Console.WriteLine($"Current simulation date: {currentDate}");
        Console.WriteLine($"Comparing year {twoYearsAgo} to year {lastYear}");
        Console.WriteLine($"Two years ago end date: {twoYearsAgoEndDate}");
        Console.WriteLine($"Last year end date: {lastYearEndDate}");
        
        foreach (var account in person.Investments.Accounts)
        {
            var twoYearsAgoBalance = account.Balance(twoYearsAgoEndDate);
            var lastYearBalance = account.Balance(lastYearEndDate);
            var balanceChange = lastYearBalance - twoYearsAgoBalance;
            
            Console.WriteLine($"Account: {account.Name}");
            Console.WriteLine($"  Year {twoYearsAgo} Balance: {twoYearsAgoBalance:C}");
            Console.WriteLine($"  Year {lastYear} Balance: {lastYearBalance:C}");
            Console.WriteLine($"  Change: {balanceChange:C}");
            Console.WriteLine();
        }
        
        // Let's also simulate adding some growth to see what happens
        Console.WriteLine("=== Adding Growth to Test Account ===");
        var testAccount = person.Investments.Accounts.First();
        
        // Add some deposits and growth
        testAccount.Deposit(5000, new DateOnly(2025, 6, 1), TransactionCategory.ContributionPersonal);
        testAccount.ApplyMonthlyGrowth(new DateOnly(2025, 12, 31));
        
        testAccount.Deposit(3000, new DateOnly(2026, 3, 1), TransactionCategory.ContributionPersonal);
        testAccount.ApplyMonthlyGrowth(new DateOnly(2026, 12, 31));
        
        // Now check the balances again
        var twoYearsAgoBalanceAfter = testAccount.Balance(twoYearsAgoEndDate);
        var lastYearBalanceAfter = testAccount.Balance(lastYearEndDate);
        var balanceChangeAfter = lastYearBalanceAfter - twoYearsAgoBalanceAfter;
        
        Console.WriteLine($"After adding activity:");
        Console.WriteLine($"Account: {testAccount.Name}");
        Console.WriteLine($"  Year {twoYearsAgo} Balance: {twoYearsAgoBalanceAfter:C}");
        Console.WriteLine($"  Year {lastYear} Balance: {lastYearBalanceAfter:C}");
        Console.WriteLine($"  Change: {balanceChangeAfter:C}");
    }
}
