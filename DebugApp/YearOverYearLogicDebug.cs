using RetirementPlanner;
using RetirementPlanner.Test;

namespace DebugApp;

public static class YearOverYearLogicDebug
{
    public static void DebugYearOverYearComparison()
    {
        Console.WriteLine("=== Debugging Exact Year-Over-Year Logic ===");
        
        var person = TestPersonFactory.Me();
        var account = person.Investments.Accounts.First(a => a.Name == "Traditional 401k");
        
        Console.WriteLine($"Account: {account.Name}");
        Console.WriteLine($"Starting balance: ${account.Balance(DateOnly.FromDateTime(DateTime.Now)):C}");
        
        // Simulate what happens in Events.cs OnNewYear for the year 2073
        // (this is one of the years we see in the simulation output)
        var newYearDate = new DateOnly(2073, 1, 1);
        var twoYearsAgo = newYearDate.Year - 2; // 2071
        var lastYear = newYearDate.Year - 1;     // 2072
        
        var twoYearsAgoEndDate = new DateOnly(twoYearsAgo, 12, 31); // 2071-12-31
        var lastYearEndDate = new DateOnly(lastYear, 12, 31);       // 2072-12-31
        
        Console.WriteLine($"\nEvents.cs simulation at {newYearDate}:");
        Console.WriteLine($"Two years ago: {twoYearsAgo} (end date: {twoYearsAgoEndDate})");
        Console.WriteLine($"Last year: {lastYear} (end date: {lastYearEndDate})");
        
        var twoYearsAgoBalance = account.Balance(twoYearsAgoEndDate);
        var lastYearBalance = account.Balance(lastYearEndDate);
        var balanceChange = lastYearBalance - twoYearsAgoBalance;
        
        Console.WriteLine($"Balance on {twoYearsAgoEndDate}: {twoYearsAgoBalance:C}");
        Console.WriteLine($"Balance on {lastYearEndDate}: {lastYearBalance:C}");
        Console.WriteLine($"Change: {balanceChange:C}");
        
        // Let's add some activity to simulate what should happen during those years
        Console.WriteLine("\n=== Adding Some Activity ===");
        
        // Add some deposits and growth for 2071
        account.Deposit(1000, new DateOnly(2071, 6, 1), TransactionCategory.ContributionPersonal);
        account.ApplyMonthlyGrowth(new DateOnly(2071, 12, 31));
        
        // Add some deposits and growth for 2072
        account.Deposit(1500, new DateOnly(2072, 3, 1), TransactionCategory.ContributionPersonal);
        account.ApplyMonthlyGrowth(new DateOnly(2072, 6, 30));
        account.ApplyMonthlyGrowth(new DateOnly(2072, 12, 31));
        
        // Now check the balances again
        var twoYearsAgoBalanceAfter = account.Balance(twoYearsAgoEndDate);
        var lastYearBalanceAfter = account.Balance(lastYearEndDate);
        var balanceChangeAfter = lastYearBalanceAfter - twoYearsAgoBalanceAfter;
        
        Console.WriteLine($"After activity:");
        Console.WriteLine($"Balance on {twoYearsAgoEndDate}: {twoYearsAgoBalanceAfter:C}");
        Console.WriteLine($"Balance on {lastYearEndDate}: {lastYearBalanceAfter:C}");
        Console.WriteLine($"Change: {balanceChangeAfter:C}");
        
        // Let's also check YearlyStartingBalances to see what gets recorded
        Console.WriteLine("\n=== YearlyStartingBalances ===");
        foreach (var yearBalance in account.YearlyStartingBalances.OrderBy(x => x.Key))
        {
            Console.WriteLine($"Year {yearBalance.Key}: {yearBalance.Value:C}");
        }
        
        // Check deposit history
        Console.WriteLine("\n=== Recent Deposit History ===");
        var deposits = account.DepositHistory.Where(d => d.Date.Year >= 2071 && d.Date.Year <= 2072).OrderBy(d => d.Date);
        foreach (var deposit in deposits)
        {
            Console.WriteLine($"{deposit.Date}: {deposit.Amount:C} ({deposit.Category})");
        }
    }
}
