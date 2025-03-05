using RetirementPlanner;
using RetirementPlanner.IRS;
using RetirementPlanner.Test;
using Planner = RetirementPlanner.RetirementPlanner;

public class RetirementSimulationTests
{
    [Fact]
    public void EarlyRetiree_Should_Use_SEPP_Before_59_5()
    {
        // Arrange
        var person = TestPersonFactory.CreateEarlyRetiree();

        // Act
        double seppWithdrawal = Planner.CalculateSEPP(person);

        // Assert
        Assert.True(seppWithdrawal > 0, "SEPP should be applied for early retiree.");
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
        // Arrange
        var person = TestPersonFactory.CreateLateRetiree();

        // Act
        double rmd = Planner.ProcessRMDWithdrawal(person);

        // Assert
        Assert.True(rmd > 0, "RMD should be applied at age 73+.");
    }

    [Fact]
    public void RothConversion_Should_Only_Occur_When_Beneficial()
    {
        // Arrange
        var person = TestPersonFactory.CreateNormalRetiree();
        double initialRothBalance = person.GetAccount(AccountType.RothIRA).Balance;

        // Act
        Planner.PerformRothConversion(person);

        // Assert
        double finalRothBalance = person.GetAccount(AccountType.RothIRA).Balance;
        Assert.True(finalRothBalance > initialRothBalance, "Roth conversion should increase Roth IRA balance.");
    }

    [Fact]
    public void TaxEfficientWithdrawals_Should_Use_Taxable_First()
    {
        // Arrange
        var person = TestPersonFactory.CreateNormalRetiree();
        double initialTaxableBalance = person.GetAccount(AccountType.Savings).Balance;

        // Act
        double totalIncome = 0;
        double withdrawalNeeded = 5000;
        Planner.TryWithdraw(person, AccountType.Savings, ref totalIncome, ref withdrawalNeeded);

        // Assert
        double finalTaxableBalance = person.GetAccount(AccountType.Savings).Balance;
        Assert.True(finalTaxableBalance < initialTaxableBalance, "Taxable account should be withdrawn first.");
    }
}
