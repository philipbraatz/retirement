using RetirementPlanner;
using RetirementPlanner.Calculators;
using RetirementPlanner.IRS;
using RetirementPlanner.Test;
using Planner = RetirementPlanner.RetirementPlanner;

public class RetirementSimulationTests
{
    [Fact]
    public void EarlyRetiree_Should_Calculate_SocialSecurity_Benefits()
    {
        // Arrange
        var person = TestPersonFactory.CreateEarlyRetiree();

        // Act - Calculate Social Security benefits at claiming age
        double benefit = person.CalculateCurrentSocialSecurityBenefits(new DateTime(2047, 6, 15)); // When person turns 62

        // Assert - Social Security benefits should be calculated (may be 0 if no prior earnings)
        Assert.True(benefit >= 0, "Social Security benefit calculation should not be negative.");
    }

    [Fact]
    public void NormalRetiree_Should_Claim_SocialSecurity_At_FRA()
    {
        // Arrange
        var person = TestPersonFactory.CreateNormalRetiree();

        // Act
        double benefit = SocialSecurity.CalculateSocialSecurityBenefit(person.BirthDate.Year, person.SocialSecurityClaimingAge, person.IncomeYearly);

        // Assert
        Assert.True(benefit > 0, "Social Security should be available at FRA.");
    }

    [Fact]
    public void LateRetiree_Should_Have_RMD_Applied()
    {
        // Arrange - Use base InvestmentAccount to avoid contribution limits for testing
        var traditional401k = new InvestmentAccount(0.05, "Traditional 401k", 400000, AccountType.Traditional401k);
        
        var person = new Person()
        {
            BirthDate = new DateTime(1950, 1, 1), // Age 74
            FullRetirementAge = 67,
        };
        
        person.Investments = new InvestmentManager([traditional401k]);
        
        // Debug: Check the person's current age in 2024
        int currentAge = person.CurrentAge(new DateOnly(2024, 1, 1));
        
        // Act - Calculate RMD for current year (assuming age 74)
        double priorYearBalance = traditional401k.Balance(new DateOnly(2023, 12, 31));
        double rmd = RMDCalculator.CalculateRMD(traditional401k, currentAge, priorYearBalance);
        
        // Assert
        Assert.True(currentAge >= 73, $"Person should be at least 73, but is {currentAge}");
        Assert.True(rmd > 0, "RMD should be applied at age 73+.");
    }

    [Fact]
    public void RothIRA_Should_Allow_Contribution_Withdrawal()
    {
        // Arrange
        var person = TestPersonFactory.CreateNormalRetiree();
        var rothIRA = person.Investments.Accounts.First(a => a.Type == AccountType.RothIRA);
        double initialBalance = rothIRA.Balance(new DateOnly(2024, 1, 1));

        // Act - Withdraw a small amount (should be from contributions)
        double withdrawalAmount = 1000;
        double actualWithdrawn = rothIRA.Spend(withdrawalAmount, new DateOnly(2024, 1, 1), TransactionCategory.Expenses);

        // Assert
        double finalBalance = rothIRA.Balance(new DateOnly(2024, 1, 1));
        Assert.True(finalBalance < initialBalance, "Roth IRA balance should decrease after withdrawal.");
        Assert.True(actualWithdrawn > 0, "Should be able to withdraw from Roth IRA.");
    }

    [Fact]
    public void TaxableAccount_Should_Allow_Withdrawal_Without_Penalty()
    {
        // Arrange
        var person = TestPersonFactory.CreateNormalRetiree();
        var taxableAccount = person.Investments.Accounts.First(a => a.Type == AccountType.Savings);
        double initialBalance = taxableAccount.Balance(new DateOnly(2024, 1, 1));

        // Act
        double withdrawalAmount = 5000;
        double actualWithdrawn = taxableAccount.Spend(withdrawalAmount, new DateOnly(2024, 1, 1), TransactionCategory.Expenses);

        // Assert
        double finalBalance = taxableAccount.Balance(new DateOnly(2024, 1, 1));
        Assert.True(finalBalance < initialBalance, "Taxable account balance should decrease after withdrawal.");
        Assert.Equal(withdrawalAmount, actualWithdrawn);
    }

    [Fact]
    public void EarlyWithdrawal_Should_Apply_Penalty_Before_59_Half()
    {
        // Arrange
        int age = 45;
        double withdrawalAmount = 10000;

        // Act
        double penalty = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(
            accountType: AccountType.Traditional401k,
            withdrawalAmount: withdrawalAmount,
            age: age,
            separationFromServiceAge: null,
            withdrawalReason: WithdrawalReason.GeneralDistribution);

        // Assert
        Assert.Equal(1000, penalty); // 10% penalty on $10,000
    }

    [Fact]
    public void RMD_Should_Be_Calculated_Correctly()
    {
        // Arrange - Use base InvestmentAccount to avoid contribution limits for testing
        var traditional401k = new InvestmentAccount(0.05, "Traditional 401k", 400000, AccountType.Traditional401k);
        
        var person = new Person()
        {
            BirthDate = new DateTime(1950, 1, 1), // Age 74
            FullRetirementAge = 67,
        };
        
        person.Investments = new InvestmentManager([traditional401k]);
        
        // Act - Calculate RMD for a 74-year-old
        int currentAge = person.CurrentAge(new DateOnly(2024, 1, 1));
        double priorYearBalance = traditional401k.Balance(new DateOnly(2023, 12, 31));
        double rmd = RMDCalculator.CalculateRMD(traditional401k, currentAge, priorYearBalance);
        
        // Assert
        Assert.True(currentAge >= 73, $"Person should be at least 73, but is {currentAge}");
        Assert.True(rmd > 0, "RMD should be greater than 0 for someone over 73");
        Assert.True(rmd < traditional401k.Balance(new DateOnly(2024, 1, 1)), "RMD should be less than total balance");
        
        // Verify the RMD calculation is approximately correct for age 74
        // At age 74, life expectancy factor is 25.5, so RMD should be balance / 25.5
        double expectedRMD = 400000 / 25.5;
        Assert.True(Math.Abs(rmd - expectedRMD) < 1, $"RMD should be approximately {expectedRMD:F2}, but was {rmd:F2}");
    }
}
