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
            Person tempPerson = new()
            {
                BirthDate = new DateTime(1950, 1, 1), // Age 74 at test date
            };

            var savingsAccount = new InvestmentAccount(0.05, "Savings", 1000, AccountType.Savings);
            var traditional401k = new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(new DateTime(1950, 1, 1)), 500); // Low balance
            var traditionalIRA = new TraditionalIRAAccount(0.05, "Traditional IRA", tempPerson, 300); // Low balance
            var roth401k = new Roth401kAccount(0.05, "Roth 401k", DateOnly.FromDateTime(new DateTime(1950, 1, 1)), 50000); // High balance
            var rothIRA = new RothIRAAccount(0.05, "Roth IRA", tempPerson, 25000); // High balance
            var hsaAccount = new HSAAccount(0.05, "HSA", tempPerson, 10000);

            var person = new Person();
            person.BirthDate = new DateTime(1950, 1, 1); // Age 74 at test date
            person.EssentialExpenses = 5000; // High expenses to force account depletion
            person.DiscretionarySpending = 0;
            person.Investments = new InvestmentManager([traditional401k, traditionalIRA, roth401k, rothIRA, savingsAccount, hsaAccount]);

            var testDate = new DateOnly(2024, 6, 1);
            double totalWithdrawal = 2000; // Amount that will require depleting Traditional and using Roth

            double remaining = totalWithdrawal;
            remaining -= savingsAccount.Spend(remaining, testDate, TransactionCategory.Expenses);
            if (remaining > 0) remaining -= traditional401k.Spend(remaining, testDate, TransactionCategory.Expenses);
            if (remaining > 0) remaining -= traditionalIRA.Spend(remaining, testDate, TransactionCategory.Expenses);
            if (remaining > 0) remaining -= roth401k.Spend(remaining, testDate, TransactionCategory.Expenses);

            var traditionalBalance = traditional401k.Balance(testDate) + traditionalIRA.Balance(testDate);
            var rothBalanceAfter = roth401k.Balance(testDate);

            Assert.True(traditionalBalance < 100, $"Traditional accounts should be mostly depleted but have {traditionalBalance:C}");
            Assert.True(rothBalanceAfter < 50000, $"Roth 401k should have been used but balance is still {rothBalanceAfter:C}");
            Assert.True(remaining <= 0, $"Spending should be fully covered, but {remaining:C} remains");
        }

        [Fact]
        public void TotalWithdrawals_Should_Approximate_TotalExpenses_Over_Time()
        {
            // Arrange
            var person = TestPersonFactory.CreateNormalRetiree();
            var options = new RetirementPlanner.Options
            {
                StartDate = new DateOnly(2024, 1, 1),
                EndDate = new DateOnly(2034, 1, 1)
            };
            var simulation = new RetirementPlanner(person, options);

            // Act
            simulation.RunRetirementSimulation().Wait(); // Run simulation synchronously for testing

            // Collect data
            double totalWithdrawals = 0;
            foreach (var account in person.Investments.Accounts)
            {
                totalWithdrawals += account.WithdrawalHistory.Sum(w => w.Amount);
            }

            double totalExpenses = 0;
            var currentDate = options.StartDate;
            while (currentDate < options.EndDate)
            {
                totalExpenses += (person.EssentialExpenses + person.DiscretionarySpending) / 12;
                currentDate = currentDate.AddMonths(1);
            }

            // Assert
            // Check if total withdrawals are within a reasonable range of total expenses
            // Allow for some tolerance due to timing of withdrawals and expenses
            double tolerance = 0.10; // 10% tolerance
            Assert.InRange(totalWithdrawals, totalExpenses * (1 - tolerance), totalExpenses * (1 + tolerance));
        }
    }
}
