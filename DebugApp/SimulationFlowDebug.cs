using RetirementPlanner;
using RetirementPlanner.Test;

namespace DebugApp;

public static class SimulationFlowDebug
{
    public static void DebugActualSimulationFlow()
    {
        Console.WriteLine("=== Debugging Actual Simulation Flow ===");
        
        var person = TestPersonFactory.Me();
        var account = person.Investments.Accounts.First(a => a.Name == "Traditional 401k");
        
        Console.WriteLine($"Initial balance (from 2025): ${account.Balance(new DateOnly(2025, 1, 1)):C}");
        
        // Simulate a few years of growth as would happen in the simulation
        
        // Year 2025 - apply growth each month
        for (int month = 1; month <= 12; month++)
        {
            var date = new DateOnly(2025, month, 1);
            account.ApplyMonthlyGrowth(date);
        }
        
        Console.WriteLine($"End of 2025 balance: ${account.Balance(new DateOnly(2025, 12, 31)):C}");
        
        // Year 2026 - apply growth each month
        for (int month = 1; month <= 12; month++)
        {
            var date = new DateOnly(2026, month, 1);
            account.ApplyMonthlyGrowth(date);
        }
        
        Console.WriteLine($"End of 2026 balance: ${account.Balance(new DateOnly(2026, 12, 31)):C}");
        
        // Year 2027 - apply growth each month
        for (int month = 1; month <= 12; month++)
        {
            var date = new DateOnly(2027, month, 1);
            account.ApplyMonthlyGrowth(date);
        }
        
        Console.WriteLine($"End of 2027 balance: ${account.Balance(new DateOnly(2027, 12, 31)):C}");
        
        // Now simulate the year-over-year comparison that happens on New Year 2028
        Console.WriteLine("\n=== Simulating Events.cs OnNewYear for 2028 ===");
        var newYearDate = new DateOnly(2028, 1, 1);
        var twoYearsAgo = newYearDate.Year - 2; // 2026
        var lastYear = newYearDate.Year - 1;     // 2027
        
        var twoYearsAgoEndDate = new DateOnly(twoYearsAgo, 12, 31);
        var lastYearEndDate = new DateOnly(lastYear, 12, 31);
        
        var twoYearsAgoBalance = account.Balance(twoYearsAgoEndDate);
        var lastYearBalance = account.Balance(lastYearEndDate);
        var balanceChange = lastYearBalance - twoYearsAgoBalance;
        
        Console.WriteLine($"Comparing {twoYearsAgo} vs {lastYear}:");
        Console.WriteLine($"Balance on {twoYearsAgoEndDate}: {twoYearsAgoBalance:C}");
        Console.WriteLine($"Balance on {lastYearEndDate}: {lastYearBalance:C}");
        Console.WriteLine($"Change: {balanceChange:C}");
        
        // Check YearlyStartingBalances to see what was recorded
        Console.WriteLine("\n=== YearlyStartingBalances ===");
        foreach (var yearBalance in account.YearlyStartingBalances.OrderBy(x => x.Key))
        {
            Console.WriteLine($"Year {yearBalance.Key}: {yearBalance.Value:C}");
        }
        
        // Check a sampling of deposits to see what happened
        Console.WriteLine("\n=== Sample Deposit History ===");
        var growthDeposits = account.DepositHistory
            .Where(d => d.Category == TransactionCategory.Intrest)
            .OrderBy(d => d.Date)
            .Take(10);
            
        foreach (var deposit in growthDeposits)
        {
            Console.WriteLine($"{deposit.Date}: {deposit.Amount:C} (Interest)");
        }
    }
}
