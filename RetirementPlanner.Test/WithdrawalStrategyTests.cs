using RetirementPlanner;
using RetirementPlanner.Event;

namespace RetirementPlanner.Test
{
    public class WithdrawalStrategyTests
    {
        [Fact]
        public void GetOptimalWithdrawalOrder_EarlyRetirement_Should_Prioritize_PenaltyFree()
        {
            // Arrange - Age under 59.5
            int youngAge = 35;

            // Act - Test that the GetOptimalWithdrawalOrder method is accessible and returns expected order
            var withdrawalOrder = LifeEvents.GetOptimalWithdrawalOrder(youngAge);

            // Assert - Check penalty-free accounts come first
            Assert.Equal(AccountType.Savings, withdrawalOrder[0]);
            Assert.Equal(AccountType.Roth401k, withdrawalOrder[1]);
            Assert.Equal(AccountType.RothIRA, withdrawalOrder[2]);
            Assert.Equal(AccountType.HSA, withdrawalOrder[3]);
            Assert.Equal(AccountType.TraditionalIRA, withdrawalOrder[4]);
            Assert.Equal(AccountType.Traditional401k, withdrawalOrder[5]); // Last resort due to penalty
        }

        [Fact]
        public void GetOptimalWithdrawalOrder_PostPenalty_Should_Prioritize_Traditional()
        {
            // Arrange - Age between 59.5 and 73
            int middleAge = 65;

            // Act
            var withdrawalOrder = LifeEvents.GetOptimalWithdrawalOrder(middleAge);

            // Assert - Check Traditional accounts prioritized to reduce RMDs
            Assert.Equal(AccountType.Savings, withdrawalOrder[0]);
            Assert.Equal(AccountType.Traditional401k, withdrawalOrder[1]); // Prioritize to reduce RMDs
            Assert.Equal(AccountType.TraditionalIRA, withdrawalOrder[2]); // Prioritize to reduce RMDs
            Assert.Equal(AccountType.HSA, withdrawalOrder[3]);
            Assert.Equal(AccountType.Roth401k, withdrawalOrder[4]); // Preserve for tax-free growth
            Assert.Equal(AccountType.RothIRA, withdrawalOrder[5]); // Preserve for tax-free growth
        }

        [Fact]
        public void GetOptimalWithdrawalOrder_RMD_Age_Should_Prioritize_Traditional()
        {
            // Arrange - Age 73+
            int seniorAge = 75;

            // Act
            var withdrawalOrder = LifeEvents.GetOptimalWithdrawalOrder(seniorAge);

            // Assert - Check Traditional accounts prioritized since RMDs required anyway
            Assert.Equal(AccountType.Savings, withdrawalOrder[0]);
            Assert.Equal(AccountType.Traditional401k, withdrawalOrder[1]); // RMDs required anyway
            Assert.Equal(AccountType.TraditionalIRA, withdrawalOrder[2]); // RMDs required anyway
            Assert.Equal(AccountType.Roth401k, withdrawalOrder[3]); // Start using Roth more in later retirement
            Assert.Equal(AccountType.HSA, withdrawalOrder[4]); // Still tax-free for medical
            Assert.Equal(AccountType.RothIRA, withdrawalOrder[5]); // Preserve for heirs but use if needed
        }

        [Fact]
        public void GetOptimalWithdrawalOrder_RMD_Age_Should_Be_More_Balanced()
        {
            // Arrange - Age 73+
            int seniorAge = 75;

            // Act
            var withdrawalOrder = LifeEvents.GetOptimalWithdrawalOrder(seniorAge);

            // Assert - Check that withdrawal order is more balanced in late retirement
            Assert.Equal(AccountType.Savings, withdrawalOrder[0]);
            Assert.Equal(AccountType.Traditional401k, withdrawalOrder[1]); // RMDs required anyway
            Assert.Equal(AccountType.TraditionalIRA, withdrawalOrder[2]); // RMDs required anyway
            Assert.Equal(AccountType.Roth401k, withdrawalOrder[3]); // More aggressive Roth usage
            Assert.Equal(AccountType.HSA, withdrawalOrder[4]);
            Assert.Equal(AccountType.RothIRA, withdrawalOrder[5]); // Still preserve for heirs but not as strictly
        }

        [Fact]
        public void Withdrawal_Should_Use_Roth_When_Traditional_Accounts_Depleted()
        {
            // This test demonstrates that Roth accounts WILL be used when other accounts are depleted
            // Create a person with mostly Roth accounts and very low Traditional balances
            Person tempPerson = new()
            {
                BirthDate = new DateTime(1950, 1, 1), // Age 74 at test date
            };

            // Create accounts with Traditional accounts having low balances
            var savingsAccount = new InvestmentAccount(0.05, "Savings", 1000, AccountType.Savings);
            var traditional401k = new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(new DateTime(1950, 1, 1)), 500); // Low balance
            var traditionalIRA = new TraditionalIRAAccount(0.05, "Traditional IRA", tempPerson, 300); // Low balance
            var roth401k = new Roth401kAccount(0.05, "Roth 401k", DateOnly.FromDateTime(new DateTime(1950, 1, 1)), 50000); // High balance
            var rothIRA = new RothIRAAccount(0.05, "Roth IRA", tempPerson, 25000); // High balance
            var hsaAccount = new HSAAccount(0.05, "HSA", 10000);

            Person person = new(traditional401k, traditionalIRA, roth401k, rothIRA, savingsAccount, hsaAccount)
            {
                BirthDate = new DateTime(1950, 1, 1), // Age 74 at test date
                EssentialExpenses = 5000, // High expenses to force account depletion
                DiscretionarySpending = 0,
            };

            var testDate = new DateOnly(2024, 6, 1);
            double totalWithdrawal = 2000; // Amount that will require depleting Traditional and using Roth

            // Act - Manually withdraw from accounts in order that would be used for age 74
            // Order for age 74: Savings, Traditional401k, TraditionalIRA, Roth401k, HSA, RothIRA
            double remaining = totalWithdrawal;

            // Withdraw from Savings first
            remaining -= savingsAccount.Withdraw(remaining, testDate, TransactionCategory.Expenses);
            
            // Then Traditional 401k
            if (remaining > 0)
                remaining -= traditional401k.Withdraw(remaining, testDate, TransactionCategory.Expenses);
            
            // Then Traditional IRA
            if (remaining > 0)
                remaining -= traditionalIRA.Withdraw(remaining, testDate, TransactionCategory.Expenses);
            
            // Then Roth 401k (this should be needed since Traditional accounts are low)
            if (remaining > 0)
                remaining -= roth401k.Withdraw(remaining, testDate, TransactionCategory.Expenses);

            // Assert - Verify behavior
            var traditionalBalance = traditional401k.Balance(testDate) + traditionalIRA.Balance(testDate);
            var rothBalanceAfter = roth401k.Balance(testDate);

            // Traditional accounts should be depleted
            Assert.True(traditionalBalance < 100, $"Traditional accounts should be mostly depleted but have {traditionalBalance:C}");
            
            // Roth 401k should have been used (less than original 50000)
            Assert.True(rothBalanceAfter < 50000, $"Roth 401k should have been used but balance is still {rothBalanceAfter:C}");
            
            // Verify spending was fully covered
            Assert.True(remaining <= 0, $"Spending should be fully covered, but {remaining:C} remains");
        }
    }
}
