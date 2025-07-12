using RetirementPlanner;
using RetirementPlanner.Test;

namespace DebugApp;

public static class BalanceMethodDebug
{
    public static void TestBalanceMethod()
    {
        Console.WriteLine("=== Testing Balance Method Behavior ===");
        
        var person = TestPersonFactory.Me();
        var account = person.Investments.Accounts.First();
        
        Console.WriteLine($"Account: {account.Name}");
        Console.WriteLine($"Person birth date: {person.BirthDate}");
        Console.WriteLine($"Current simulation date would be around: {DateTime.Now:yyyy-MM-dd}");
        
        // Test balance method for various dates
        var testDates = new[]
        {
            new DateOnly(2023, 12, 31),
            new DateOnly(2024, 12, 31),
            new DateOnly(2025, 12, 31),
            new DateOnly(2026, 12, 31),
            new DateOnly(2027, 12, 31),
        };
        
        Console.WriteLine("\n=== Balance Method Results ===");
        foreach (var date in testDates)
        {
            var balance = account.Balance(date);
            Console.WriteLine($"Balance on {date}: {balance:C}");
        }
        
        // Check the YearlyStartingBalances
        Console.WriteLine("\n=== YearlyStartingBalances ===");
        foreach (var yearBalance in account.YearlyStartingBalances.OrderBy(x => x.Key))
        {
            Console.WriteLine($"Year {yearBalance.Key}: {yearBalance.Value:C}");
        }
        
        // Check deposit history
        Console.WriteLine("\n=== Deposit History ===");
        var recentDeposits = account.DepositHistory.Take(5);
        foreach (var deposit in recentDeposits)
        {
            Console.WriteLine($"{deposit.Date}: {deposit.Amount:C} ({deposit.Category})");
        }
        
        // Test what happens when we add growth
        Console.WriteLine("\n=== Testing with Manual Growth ===");
        
        // Apply some growth to the account for 2025
        account.ApplyMonthlyGrowth(new DateOnly(2025, 12, 31));
        
        Console.WriteLine("After applying growth for 2025:");
        foreach (var date in testDates)
        {
            var balance = account.Balance(date);
            Console.WriteLine($"Balance on {date}: {balance:C}");
        }
    }
}
