using RetirementPlanner;
using RetirementPlanner.Event;

namespace RetirementPlanner.Test
{
    public class BalanceGrowthTests
    {
        [Fact]
        public void Balance_Should_Increase_After_Monthly_Growth_Applied()
        {
            // Arrange - Create a person with an account
            Person tempPerson = new()
            {
                BirthDate = new DateTime(1990, 1, 1),
            };

            var traditional401k = new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(tempPerson.BirthDate), 10000);
            
            Person person = new(traditional401k)
            {
                BirthDate = new DateTime(1990, 1, 1),
                EssentialExpenses = 40000,
                DiscretionarySpending = 10000,
            };

            var testDate = new DateOnly(2025, 1, 15);
            
            // Act - Record balance before growth
            double balanceBeforeGrowth = traditional401k.Balance(testDate);
            
            // Apply monthly growth manually
            traditional401k.ApplyMonthlyGrowth(testDate);
            
            // Record balance after growth
            double balanceAfterGrowth = traditional401k.Balance(testDate);
            
            // Assert - Balance should have increased
            Assert.True(balanceAfterGrowth > balanceBeforeGrowth, 
                $"Balance should increase after growth. Before: {balanceBeforeGrowth:C}, After: {balanceAfterGrowth:C}");
            
            // Expected growth calculation (5% annual = ~0.407% monthly)
            double expectedMonthlyRate = Math.Pow(1.05, 1.0 / 12) - 1;
            double expectedGrowth = balanceBeforeGrowth * expectedMonthlyRate;
            double expectedBalanceAfter = balanceBeforeGrowth + expectedGrowth;
            
            // Assert the growth amount is correct (within a small tolerance)
            Assert.True(Math.Abs(balanceAfterGrowth - expectedBalanceAfter) < 0.01,
                $"Growth amount should be correct. Expected: {expectedBalanceAfter:C}, Actual: {balanceAfterGrowth:C}");
        }

        [Fact]
        public void Balance_Should_Show_Correct_Year_Over_Year_Change()
        {
            // Arrange - Create a person with an account
            Person tempPerson = new()
            {
                BirthDate = new DateTime(1990, 1, 1),
            };

            var traditional401k = new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(tempPerson.BirthDate), 10000);
            
            Person person = new(traditional401k)
            {
                BirthDate = new DateTime(1990, 1, 1),
                EssentialExpenses = 40000,
                DiscretionarySpending = 10000,
            };

            var startDate = new DateOnly(2025, 1, 1);
            var endDate = new DateOnly(2026, 1, 1);
            
            // Act - Record balance at start of year
            double balanceStartOfYear = traditional401k.Balance(startDate);
            
            // Apply 12 months of growth
            for (int month = 1; month <= 12; month++)
            {
                var monthDate = new DateOnly(2025, month, 15);
                traditional401k.ApplyMonthlyGrowth(monthDate);
            }
            
            // Record balance at end of year
            double balanceEndOfYear = traditional401k.Balance(endDate);
            
            // Assert - Balance should have increased significantly over the year
            double yearOverYearChange = balanceEndOfYear - balanceStartOfYear;
            
            Assert.True(yearOverYearChange > 0, 
                $"Year-over-year change should be positive. Start: {balanceStartOfYear:C}, End: {balanceEndOfYear:C}, Change: {yearOverYearChange:C}");
            
            // Expected annual growth should be approximately 5%
            double expectedGrowth = balanceStartOfYear * 0.05;
            double actualGrowthRate = yearOverYearChange / balanceStartOfYear;
            
            Assert.True(actualGrowthRate > 0.04 && actualGrowthRate < 0.06,
                $"Annual growth rate should be approximately 5%. Actual: {actualGrowthRate:P2}, Expected: ~5%");
        }

        [Fact]
        public void Balance_Calculation_Should_Include_All_Deposits_Including_Growth()
        {
            // Arrange - Create a fresh account
            var account = new Traditional401kAccount(0.05, "Test Account", new DateOnly(2020, 1, 1), 1000);
            var testDate = new DateOnly(2025, 6, 15);
            
            // Act - Apply some growth and track deposits
            double initialBalance = account.Balance(testDate);
            
            // Apply growth
            account.ApplyMonthlyGrowth(testDate);
            
            // Check deposits history
            var growthDeposits = account.DepositHistory.Where(d => d.Category == TransactionCategory.Intrest);
            
            // Assert - Should have growth deposits
            Assert.True(growthDeposits.Any(), "Should have interest deposits recorded");
            
            // Total of all deposits should match balance change
            double totalGrowthDeposits = growthDeposits.Sum(d => d.Amount);
            double balanceAfterGrowth = account.Balance(testDate);
            double balanceIncrease = balanceAfterGrowth - initialBalance;
            
            Assert.True(Math.Abs(balanceIncrease - totalGrowthDeposits) < 0.01,
                $"Balance increase should match growth deposits. Increase: {balanceIncrease:C}, Growth deposits: {totalGrowthDeposits:C}");
        }

        [Fact]
        public void OnNewYear_Event_Should_Show_Actual_Balance_Change()
        {
            // Arrange - Create a minimal simulation setup
            Person tempPerson = new()
            {
                BirthDate = new DateTime(1990, 1, 1),
            };

            var traditional401k = new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(tempPerson.BirthDate), 10000);
            
            Person person = new(traditional401k)
            {
                BirthDate = new DateTime(1990, 1, 1),
                EssentialExpenses = 40000,
                DiscretionarySpending = 10000,
            };

            // Simulate some monthly growth throughout the year
            var year2025Months = new[]
            {
                new DateOnly(2025, 1, 15),
                new DateOnly(2025, 2, 15),
                new DateOnly(2025, 3, 15),
                new DateOnly(2025, 4, 15),
                new DateOnly(2025, 5, 15),
                new DateOnly(2025, 6, 15)
            };

            // Act - Apply growth for several months
            foreach (var monthDate in year2025Months)
            {
                traditional401k.ApplyMonthlyGrowth(monthDate);
            }

            // Test the balance calculation at two different points in time
            var balanceDec31_2024 = traditional401k.Balance(new DateOnly(2024, 12, 31));
            var balanceJan1_2025 = traditional401k.Balance(new DateOnly(2025, 1, 1));
            var balanceJun30_2025 = traditional401k.Balance(new DateOnly(2025, 6, 30));

            // Assert - June balance should be higher than January balance
            Assert.True(balanceJun30_2025 > balanceJan1_2025,
                $"June balance should be higher than January balance. Jan 1: {balanceJan1_2025:C}, Jun 30: {balanceJun30_2025:C}");

            // The year-over-year comparison should show growth
            var yearOverYearChange = balanceJun30_2025 - balanceDec31_2024;
            Assert.True(yearOverYearChange > 0,
                $"Year-over-year change should be positive. Dec 31 2024: {balanceDec31_2024:C}, Jun 30 2025: {balanceJun30_2025:C}, Change: {yearOverYearChange:C}");
        }

        [Fact]
        public void Balance_Should_Reflect_YearlyStartingBalances_Correctly()
        {
            // Arrange - Create account and add some starting balances
            var account = new Traditional401kAccount(0.05, "Test Account", new DateOnly(2020, 1, 1), 1000);
            
            // Act - Check how YearlyStartingBalances affects balance calculation
            var balance2020 = account.Balance(new DateOnly(2020, 6, 15));
            var balance2025 = account.Balance(new DateOnly(2025, 6, 15));
            
            // Apply some growth in 2025
            account.ApplyMonthlyGrowth(new DateOnly(2025, 6, 15));
            var balanceAfterGrowth = account.Balance(new DateOnly(2025, 6, 15));
            
            // Assert - Growth should be reflected
            Assert.True(balanceAfterGrowth > balance2025,
                $"Balance after growth should be higher. Before: {balance2025:C}, After: {balanceAfterGrowth:C}");
            
            // Debug output
            Console.WriteLine($"2020 Balance: {balance2020:C}");
            Console.WriteLine($"2025 Balance (before growth): {balance2025:C}");
            Console.WriteLine($"2025 Balance (after growth): {balanceAfterGrowth:C}");
            Console.WriteLine($"Growth applied: {balanceAfterGrowth - balance2025:C}");
            
            // Check YearlyStartingBalances
            var yearlyBalances = account.YearlyStartingBalances;
            Console.WriteLine("YearlyStartingBalances:");
            foreach (var yearBalance in yearlyBalances)
            {
                Console.WriteLine($"  Year {yearBalance.Key}: {yearBalance.Value:C}");
            }
            
            // Check deposit history
            var deposits = account.DepositHistory;
            Console.WriteLine("Deposit History:");
            foreach (var deposit in deposits)
            {
                Console.WriteLine($"  {deposit.Date}: {deposit.Amount:C} ({deposit.Category})");
            }
        }
    }
}
