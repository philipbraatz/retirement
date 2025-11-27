using RetirementPlanner; 
using RetirementPlanner.Calculators; 
using static RetirementPlanner.TaxBrackets; 
using Xunit; 

namespace RetirementPlanner.Test;

public class Phase3HsaAndSocialSecurityTests
{
    [Theory]
    [InlineData(64, 2000, 400)] // 20% penalty under 65
    [InlineData(65, 2000, 0)]   // No penalty at 65+
    public void HSA_NonQualifiedWithdrawalPenalty_AgeDependent(int age, double withdrawAmount, double expectedPenalty)
    {
        var person = new Person(){ BirthDate = DateTime.Today.AddYears(-age) };
        var hsa = new HSAAccount(0.05, "HSA", person, 5000);
        var date = DateOnly.FromDateTime(DateTime.Today);
        double before = hsa.Balance(date);
        double withdrawn = hsa.Withdraw(withdrawAmount, date, TransactionCategory.Expenses); // Non-medical
        double penaltyCharged = hsa.WithdrawalHistory.Where(w => w.Date == date && w.Category == TransactionCategory.EarlyWithdrawalPenality).Sum(w => w.Amount);
        Assert.Equal(expectedPenalty, penaltyCharged);
        Assert.Equal(withdrawAmount, withdrawn);
    }

    [Theory]
    [InlineData(FileType.Single, 20000, 0)]      // Below lower threshold
    [InlineData(FileType.Single, 30000, 12000)]  // 50% taxable band approximation (annual)
    [InlineData(FileType.Single, 40000, 20400)]  // Approaching 85% cap (annual)
    public void SocialSecurity_TaxablePortion_Tiers(FileType status, double provisionalIncome, double expectedAnnualTaxable)
    {
        var person = new Person(){ BirthDate = new DateTime(1955,1,1), FileType = status, SocialSecurityIncome = 2000 }; // monthly benefit
        person.TaxExemptInterest = 0;
        double annualSS = person.SocialSecurityIncome * 12.0;
        // provisional = IncomeYearly + 0.5 * annualSS
        double incomeNeeded = provisionalIncome - (annualSS * 0.5);
        if (incomeNeeded < 0) incomeNeeded = 0;
        person.Jobs.Add(new IncomeSource{ Title="Job", PaymentType=PaymentType.Salaried, Salary = incomeNeeded, PayFrequency=PayFrequency.Monthly, Type=JobType.FullTime });
        var calc = new TaxCalculator(person, 2024);
        double taxableAnnual = calc.GetType().GetMethod("GetTaxableSocialSecurity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(calc, null) as double? ?? 0;
        Assert.InRange(taxableAnnual, expectedAnnualTaxable *0.95, expectedAnnualTaxable *1.05);
    }
}
