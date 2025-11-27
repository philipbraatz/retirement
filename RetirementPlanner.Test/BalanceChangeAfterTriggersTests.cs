using RetirementPlanner;
using RetirementPlanner.Event;

namespace RetirementPlanner.Test
{
    public class BalanceChangeAfterTriggersTests
    {
        [Fact]
        public void Prove_Balance_Change_Is_Nonzero_After_Growth_Trigger()
        {
            // Arrange - Create accounts with starting balances
            var owner = new Person { BirthDate = new DateTime(1990, 1, 1) };
            var traditional401k = new Traditional401kAccount(0.05, "Traditional 401k", new DateOnly(1990, 1, 1), 50000);
            var rothIRA = new RothIRAAccount(0.05, "Roth IRA", owner, 10000);
            var taxableAccount = new TaxableAccount(0.05, "Taxable", 20000);
            var hsaAccount = new HSAAccount(0.05, "HSA", owner, 5000);
            
            var testDate = new DateOnly(2025, 6, 15);
            
            // Act & Assert - Record balances before triggers
            var balances_before = new Dictionary<string, double>
            {
                ["Traditional 401k"] = traditional401k.Balance(testDate),
                ["Roth IRA"] = rothIRA.Balance(testDate),
                ["Taxable"] = taxableAccount.Balance(testDate),
                ["HSA"] = hsaAccount.Balance(testDate)
            };
            
            // Apply growth triggers
            traditional401k.ApplyMonthlyGrowth(testDate);
            rothIRA.ApplyMonthlyGrowth(testDate);
            taxableAccount.ApplyMonthlyGrowth(testDate);
            hsaAccount.ApplyMonthlyGrowth(testDate);
            
            // Record balances after triggers
            var balances_after = new Dictionary<string, double>
            {
                ["Traditional 401k"] = traditional401k.Balance(testDate),
                ["Roth IRA"] = rothIRA.Balance(testDate),
                ["Taxable"] = taxableAccount.Balance(testDate),
                ["HSA"] = hsaAccount.Balance(testDate)
            };
            
            // Calculate changes
            var balance_changes = balances_after.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value - balances_before[kvp.Key]);
            
            // Assert - All balance changes should be positive (nonzero)
            foreach (var change in balance_changes)
            {
                Assert.True(change.Value > 0, 
                    $"{change.Key} balance change should be positive. " +
                    $"Before: {balances_before[change.Key]:C}, " +
                    $"After: {balances_after[change.Key]:C}, " +
                    $"Change: {change.Value:C}");
            }
            
            // Verify specific expected growth amounts (approximately 5% annual = ~0.407% monthly)
            double expectedMonthlyRate = Math.Pow(1.05, 1.0 / 12) - 1;
            
            foreach (var account in balances_before)
            {
                double expectedGrowth = account.Value * expectedMonthlyRate;
                double actualGrowth = balance_changes[account.Key];
                
                Assert.True(Math.Abs(actualGrowth - expectedGrowth) < 0.01,
                    $"{account.Key} growth should be approximately {expectedGrowth:C}. " +
                    $"Actual: {actualGrowth:C}");
            }
            
            // Additional verification: Total portfolio value should increase
            double totalBefore = balances_before.Values.Sum();
            double totalAfter = balances_after.Values.Sum();
            double totalChange = totalAfter - totalBefore;
            
            Assert.True(totalChange > 0,
                $"Total portfolio value should increase. Before: {totalBefore:C}, " +
                $"After: {totalAfter:C}, Change: {totalChange:C}");
            
            // Log results for clarity
            Console.WriteLine("=== Balance Changes After Growth Triggers ===");
            foreach (var change in balance_changes)
            {
                Console.WriteLine($"{change.Key}: {balances_before[change.Key]:C} → " +
                                  $"{balances_after[change.Key]:C} (Change: +{change.Value:C})");
            }
            Console.WriteLine($"Total Portfolio: {totalBefore:C} → {totalAfter:C} (Change: +{totalChange:C})");
        }
        
        [Fact]
        public void Prove_Year_Over_Year_Balance_Change_Is_Nonzero()
        {
            // Arrange - Create an account with starting balance
            var account = new Traditional401kAccount(0.05, "Test Account", new DateOnly(1990, 1, 1), 10000);
            
            var startOfYear = new DateOnly(2025, 1, 1);
            var endOfYear = new DateOnly(2025, 12, 31);
            
            // Record starting balance
            double balanceStartOfYear = account.Balance(startOfYear);
            
            // Act - Apply growth throughout the year (12 months)
            for (int month = 1; month <= 12; month++)
            {
                var monthDate = new DateOnly(2025, month, 15);
                account.ApplyMonthlyGrowth(monthDate);
            }
            
            // Record ending balance
            double balanceEndOfYear = account.Balance(endOfYear);
            double yearOverYearChange = balanceEndOfYear - balanceStartOfYear;
            
            // Assert - Year-over-year change should be positive and approximately 5%
            Assert.True(yearOverYearChange > 0,
                $"Year-over-year change should be positive. " +
                $"Start: {balanceStartOfYear:C}, End: {balanceEndOfYear:C}, " +
                $"Change: {yearOverYearChange:C}");
            
            double growthRate = yearOverYearChange / balanceStartOfYear;
            Assert.True(growthRate >= 0.048 && growthRate <= 0.052,
                $"Annual growth rate should be approximately 5%. " +
                $"Actual: {growthRate:P2}");
            
            // Log results
            Console.WriteLine($"Year-over-year: {balanceStartOfYear:C} → {balanceEndOfYear:C}");
            Console.WriteLine($"Change: +{yearOverYearChange:C} ({growthRate:P2})");
        }
        
        [Fact]
        public void Prove_Multiple_Deposits_And_Growth_Accumulate_Correctly()
        {
            // Arrange
            var account = new Traditional401kAccount(0.05, "Test Account", new DateOnly(1990, 1, 1), 5000);
            var testDate = new DateOnly(2025, 3, 15);
            
            double initialBalance = account.Balance(testDate);
            
            // Act - Apply several deposits and growth cycles
            account.Deposit(1000, testDate, TransactionCategory.ContributionPersonal);
            double balanceAfterDeposit1 = account.Balance(testDate);
            
            account.ApplyMonthlyGrowth(testDate);
            double balanceAfterGrowth1 = account.Balance(testDate);
            
            account.Deposit(500, testDate.AddDays(1), TransactionCategory.ContributionEmployer);
            double balanceAfterDeposit2 = account.Balance(testDate.AddDays(1));
            
            account.ApplyMonthlyGrowth(testDate.AddDays(1));
            double balanceAfterGrowth2 = account.Balance(testDate.AddDays(1));
            
            // Assert - Each step should increase the balance
            Assert.True(balanceAfterDeposit1 > initialBalance,
                $"Balance should increase after first deposit: {initialBalance:C} → {balanceAfterDeposit1:C}");
            
            Assert.True(balanceAfterGrowth1 > balanceAfterDeposit1,
                $"Balance should increase after first growth: {balanceAfterDeposit1:C} → {balanceAfterGrowth1:C}");
            
            Assert.True(balanceAfterDeposit2 > balanceAfterGrowth1,
                $"Balance should increase after second deposit: {balanceAfterGrowth1:C} → {balanceAfterDeposit2:C}");
            
            Assert.True(balanceAfterGrowth2 > balanceAfterDeposit2,
                $"Balance should increase after second growth: {balanceAfterDeposit2:C} → {balanceAfterGrowth2:C}");
            
            // Verify total change is substantial
            double totalChange = balanceAfterGrowth2 - initialBalance;
            Assert.True(totalChange > 1500, // Should be at least deposits + some growth
                $"Total change should be substantial: {totalChange:C}");
                
            // Log the progression
            Console.WriteLine("=== Account Balance Progression ===");
            Console.WriteLine($"Initial: {initialBalance:C}");
            Console.WriteLine($"After $1000 deposit: {balanceAfterDeposit1:C}");
            Console.WriteLine($"After growth: {balanceAfterGrowth1:C}");
            Console.WriteLine($"After $500 deposit: {balanceAfterDeposit2:C}");
            Console.WriteLine($"After growth: {balanceAfterGrowth2:C}");
            Console.WriteLine($"Total change: +{totalChange:C}");
        }
    }
}
