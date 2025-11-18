using RetirementPlanner;
using RetirementPlanner.Event;

namespace RetirementPlanner.Test
{
    public class EmergencyFundTests
    {
        [Fact]
        public void EmergencyFund_Should_Calculate_Correct_Minimums()
        {
            // Arrange
            var person = new Person()
            {
                BirthDate = new DateTime(1985, 1, 1), // Age 39
                EssentialExpenses = 48000, // $4,000/month
                DiscretionarySpending = 12000, // $1,000/month, Total = $5,000/month
                PartTimeAge = 45,
                AutoCalculateEmergencyFunds = true,
                PreRetirementEmergencyMonths = 6,
                EarlyRetirementEmergencyMonths = 24,
                PostRetirementEmergencyMonths = 12
            };
            
            person.Jobs.Add(new IncomeSource
            {
                Title = "Test Job",
                Type = JobType.FullTime,
                Salary = 100000
            });
            
            var testDate = new DateOnly(2024, 6, 1);
            
            // Act & Assert
            // Still working (age 39) - should use pre-retirement minimum
            double monthlyExpenses = (48000 + 12000) / 12; // $5,000/month
            double expectedPreRetirement = monthlyExpenses * 6; // 6 months = $30,000
            double actualPreRetirement = person.GetRequiredEmergencyFundMinimum(testDate);
            Assert.Equal(expectedPreRetirement, actualPreRetirement);
        }
        
        [Fact]
        public void EmergencyFund_Should_Protect_Withdrawals()
        {
            // Arrange
            var person = new Person()
            {
                BirthDate = new DateTime(1985, 1, 1),
                EssentialExpenses = 48000, // $4000/month
                DiscretionarySpending = 12000, // $1000/month
                PartTimeAge = 45,
                AutoCalculateEmergencyFunds = true,
                PreRetirementEmergencyMonths = 6 // Need $30k emergency fund (6 * $5k)
            };
            
            person.Jobs.Add(new IncomeSource { Title = "Test", Type = JobType.FullTime, Salary = 80000 });
            
            // Create savings account with exactly the emergency fund minimum
            var savingsAccount = new InvestmentAccount(0.02, "Savings", 30000, AccountType.Savings);
            person.Investments = new InvestmentManager([savingsAccount]);
            
            var testDate = new DateOnly(2024, 6, 1);
            
            // Act
            double availableForWithdrawal = person.GetAvailableForWithdrawal(testDate, AccountType.Savings);
            
            // Assert
            Assert.Equal(0, availableForWithdrawal); // Should be 0 because we need all $30k for emergency fund
        }
        
        [Fact]
        public void EmergencyFund_Should_Allow_Surplus_Withdrawals()
        {
            // Arrange
            var person = new Person()
            {
                BirthDate = new DateTime(1985, 1, 1),
                EssentialExpenses = 48000, // $4000/month
                DiscretionarySpending = 12000, // $1000/month
                PartTimeAge = 45,
                AutoCalculateEmergencyFunds = true,
                PreRetirementEmergencyMonths = 6 // Need $30k emergency fund
            };
            
            person.Jobs.Add(new IncomeSource { Title = "Test", Type = JobType.FullTime, Salary = 80000 });
            
            // Create savings account with more than emergency fund minimum
            var savingsAccount = new InvestmentAccount(0.02, "Savings", 40000, AccountType.Savings);
            person.Investments = new InvestmentManager([savingsAccount]);
            
            var testDate = new DateOnly(2024, 6, 1);
            
            // Act
            double availableForWithdrawal = person.GetAvailableForWithdrawal(testDate, AccountType.Savings);
            
            // Assert
            Assert.Equal(10000, availableForWithdrawal); // Should be $10k ($40k - $30k minimum)
        }
        
        [Fact]
        public void EmergencyFund_Should_Detect_Shortfall()
        {
            // Arrange
            var person = new Person()
            {
                BirthDate = new DateTime(1985, 1, 1),
                EssentialExpenses = 48000,
                DiscretionarySpending = 12000,
                PartTimeAge = 45,
                AutoCalculateEmergencyFunds = true,
                PreRetirementEmergencyMonths = 6 // Need $30k emergency fund
            };
            
            person.Jobs.Add(new IncomeSource { Title = "Test", Type = JobType.FullTime, Salary = 80000 });
            
            // Create savings account with less than emergency fund minimum
            var savingsAccount = new InvestmentAccount(0.02, "Savings", 20000, AccountType.Savings);
            person.Investments = new InvestmentManager([savingsAccount]);
            
            var testDate = new DateOnly(2024, 6, 1);
            
            // Act
            bool isLow = person.IsEmergencyFundLow(testDate);
            double shortfall = person.GetEmergencyFundShortfall(testDate);
            
            // Assert
            Assert.True(isLow);
            Assert.Equal(10000, shortfall); // Need $10k more to reach $30k minimum
        }
        
        [Fact]
        public void EmergencyFund_Requirements_Should_Change_With_Life_Stage()
        {
            // Arrange
            var person = new Person()
            {
                BirthDate = new DateTime(1980, 1, 1), // Will be different ages for different tests
                EssentialExpenses = 48000, // $4,000/month
                DiscretionarySpending = 12000, // $1,000/month, Total = $5,000/month
                PartTimeAge = 50,
                AutoCalculateEmergencyFunds = true,
                PreRetirementEmergencyMonths = 6,
                EarlyRetirementEmergencyMonths = 24,
                PostRetirementEmergencyMonths = 12
            };
            
            // Add job for working phase
            person.Jobs.Add(new IncomeSource { Title = "Test", Type = JobType.FullTime, Salary = 100000 });
            
            double monthlyExpenses = (48000 + 12000) / 12; // $5,000/month
            
            // Test working phase (age 44)
            var workingDate = new DateOnly(2024, 1, 1);
            double workingMinimum = person.GetRequiredEmergencyFundMinimum(workingDate);
            double expectedWorking = monthlyExpenses * 6; // 6 months = $30,000
            Assert.Equal(expectedWorking, workingMinimum);
            
            // Test early retirement phase (age 55, after part-time age but before 59.5)
            person.Jobs.Clear(); // No longer working
            var earlyRetirementDate = new DateOnly(2035, 1, 1);
            double earlyRetirementMinimum = person.GetRequiredEmergencyFundMinimum(earlyRetirementDate);
            double expectedEarlyRetirement = monthlyExpenses * 24; // 24 months = $120,000
            Assert.Equal(expectedEarlyRetirement, earlyRetirementMinimum);
            
            // Test traditional retirement phase (age 65)
            var traditionalRetirementDate = new DateOnly(2045, 1, 1);
            double traditionalRetirementMinimum = person.GetRequiredEmergencyFundMinimum(traditionalRetirementDate);
            double expectedTraditionalRetirement = monthlyExpenses * 12; // 12 months = $60,000
            Assert.Equal(expectedTraditionalRetirement, traditionalRetirementMinimum);
        }
        
        [Fact]
        public void Debug_Emergency_Fund_Calculations()
        {
            // Debug test to understand what's happening
            var person = new Person()
            {
                BirthDate = new DateTime(1985, 1, 1), // Age 39 in 2024
                EssentialExpenses = 48000,
                DiscretionarySpending = 12000,
                PartTimeAge = 45,
                AutoCalculateEmergencyFunds = true,
                PreRetirementEmergencyMonths = 6,
                EarlyRetirementEmergencyMonths = 18
            };
            
            person.Jobs.Add(new IncomeSource { Title = "Test", Type = JobType.FullTime, Salary = 80000 });
            
            var savingsAccount = new InvestmentAccount(0.02, "Savings", 20000, AccountType.Savings);
            person.Investments = new InvestmentManager([savingsAccount]);
            
            var testDate = new DateOnly(2024, 6, 1);
            
            // Debug the calculation step by step
            int age = person.CurrentAge(testDate);
            bool hasFullTimeJob = person.Jobs.Any(j => j.Type == JobType.FullTime);
            double monthlyExpenses = (person.EssentialExpenses + person.DiscretionarySpending) / 12;
            double requiredMinimum = person.GetRequiredEmergencyFundMinimum(testDate);
            double currentBalance = person.GetCurrentEmergencyFundBalance(testDate);
            double shortfall = person.GetEmergencyFundShortfall(testDate);
            
            // Output for debugging
            Console.WriteLine($"Age: {age}");
            Console.WriteLine($"PartTimeAge: {person.PartTimeAge}");
            Console.WriteLine($"Has FullTime Job: {hasFullTimeJob}");
            Console.WriteLine($"Monthly Expenses: {monthlyExpenses}");
            Console.WriteLine($"Required Minimum: {requiredMinimum}");
            Console.WriteLine($"Current Balance: {currentBalance}");
            Console.WriteLine($"Shortfall: {shortfall}");
            
            // The test
            Assert.Equal(30000, requiredMinimum); // Should be $30k (6 months * $5k)
            Assert.Equal(10000, shortfall); // Should be $10k ($30k - $20k)
        }
    }
}