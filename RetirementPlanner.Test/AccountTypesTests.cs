using RetirementPlanner;
using RetirementPlanner.IRS;
using Xunit;

namespace RetirementPlanner.Test
{
    public class AccountTypesTests
    {
        [Fact]
        public void AllAccountTypes_ShouldInstantiate_Successfully()
        {
            // Arrange
            var person = new Person
            {
                BirthDate = new DateTime(1980, 1, 1),
                FileType = TaxBrackets.FileType.Single
            };

            // Act & Assert - should not throw exceptions
            var traditional401k = new Traditional401kAccount(0.07, "401k", DateOnly.FromDateTime(person.BirthDate), 10000);
            var roth401k = new Roth401kAccount(0.07, "Roth 401k", DateOnly.FromDateTime(person.BirthDate), 5000);
            var traditionalIRA = new TraditionalIRAAccount(0.07, "Traditional IRA", person, 8000);
            var rothIRA = new RothIRAAccount(0.07, "Roth IRA", person, 6000);
            var taxable = new TaxableAccount(0.06, "Taxable", 15000);
            var hsa = new HSAAccount(0.08, "HSA", 3000);

            // Verify balances
            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            Assert.True(traditional401k.Balance(currentDate) > 0);
            Assert.True(roth401k.Balance(currentDate) > 0);
            Assert.True(traditionalIRA.Balance(currentDate) > 0);
            Assert.True(rothIRA.Balance(currentDate) > 0);
            Assert.True(taxable.Balance(currentDate) > 0);
            Assert.True(hsa.Balance(currentDate) > 0);
        }

        [Fact]
        public void Person_Clone_ShouldCreateIndependentCopy()
        {
            // Arrange
            var originalPerson = new Person
            {
                BirthDate = new DateTime(1975, 5, 10),
                FullRetirementAge = 67,
                SocialSecurityClaimingAge = 67,
                EssentialExpenses = 50000,
                DiscretionarySpending = 15000,
                FileType = TaxBrackets.FileType.Single
            };

            var traditional401k = new Traditional401kAccount(0.07, "401k", DateOnly.FromDateTime(originalPerson.BirthDate), 100000);
            var rothIRA = new RothIRAAccount(0.07, "Roth IRA", originalPerson, 50000);
            
            originalPerson.Investments = new InvestmentManager([traditional401k, rothIRA]);

            // Act
            var clonedPerson = originalPerson.Clone();

            // Assert
            Assert.NotSame(originalPerson, clonedPerson);
            Assert.Equal(originalPerson.BirthDate, clonedPerson.BirthDate);
            Assert.Equal(originalPerson.FullRetirementAge, clonedPerson.FullRetirementAge);
            Assert.Equal(originalPerson.EssentialExpenses, clonedPerson.EssentialExpenses);
            Assert.Equal(originalPerson.Investments.Accounts.Count, clonedPerson.Investments.Accounts.Count);
            
            // Verify accounts are separate instances
            Assert.NotSame(originalPerson.Investments.Accounts[0], clonedPerson.Investments.Accounts[0]);
        }

        [Fact]
        public void HSA_ContributionLimits_ShouldBeRespected()
        {
            // Arrange
            var hsa = new HSAAccount(0.07, "HSA", 0);
            var currentDate = DateOnly.FromDateTime(DateTime.Now);

            // Act
            double depositResult = hsa.Deposit(5000, currentDate, TransactionCategory.ContributionPersonal);

            // Assert - should be limited to HSA contribution limit
            Assert.True(depositResult <= ContributionLimits.GetHSALimit(currentDate.Year));
        }
    }
}
