using static RetirementPlanner.TaxBrackets;

namespace RetirementPlanner.Test;

public static class TestPersonFactory
{
    public static Person CreateEarlyRetiree()
    {
        Person person = new()
        {
            BirthDate = new DateTime(1985, 6, 15), // Age 39
            FullRetirementAge = 67,
            SalaryGrowthRate = 0.03,
            EssentialExpenses = 40000,
            DiscretionarySpending = 10000,
            SocialSecurityClaimingAge = 62,
            Jobs = [
                new(){
                    BonusPay = 5000,
                    CompanyMatchContributionPercent = 0.03,
                    Salary = 80000,
                    PayFrequency = PayFrequency.BiWeekly,
                    StartDate = new DateOnly(2010, 1, 1),
                    Title = "Software Engineer",
                    Type = JobType.FullTime,
                    PaymentType = PaymentType.Salaried,
                    Personal401kContributionPercent = 0.05
                }
            ]
        };

        person.Investments = new([
            new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(person.BirthDate), 500000),
                new RothIRAAccount(0.05, "Roth IRA", person, 200000),
                new InvestmentAccount(0.05, nameof(AccountType.Savings), 100000, AccountType.Savings)
            ]);

        return person;
    }

    public static Person CreateNormalRetiree()
    {
        Person person = new()
        {
            BirthDate = new DateTime(1960, 3, 20), // Age 64
            FullRetirementAge = 67,
            SalaryGrowthRate = 0.03,
            EssentialExpenses = 50000,
            DiscretionarySpending = 15000,
            SocialSecurityClaimingAge = 67,
            Jobs = [
                new(){
                    BonusPay = 5000,
                    CompanyMatchContributionPercent = 0.03,
                    Salary = 80000,
                    PayFrequency = PayFrequency.BiWeekly,
                    StartDate = new DateOnly(2010, 1, 1),
                    Title = "Software Engineer",
                    Type = JobType.FullTime,
                    PaymentType = PaymentType.Salaried,
                    Personal401kContributionPercent = 0.05
                }
            ]
        };

        person.Investments = new([
             new Traditional401kAccount(0.05, "Traditional IRA", DateOnly.FromDateTime(person.BirthDate), 600000 ),
                new RothIRAAccount(0.05, "Roth IRA",person, 250000 ),
                new InvestmentAccount(0.05, nameof(AccountType.Savings), 50000, AccountType.Savings )
            ]);

        return person;
    }

    public static Person CreateLateRetiree()
    {
        Person person = new()
        {
            BirthDate = new DateTime(1950, 1, 1), // Age 74
            FullRetirementAge = 67,
            EssentialExpenses = 45000,
            DiscretionarySpending = 20000,
            SocialSecurityClaimingAge = 70,
            Jobs = [
                new(){
                    BonusPay = 5000,
                    CompanyMatchContributionPercent = 0.03,
                    Salary = 80000,
                    PayFrequency = PayFrequency.BiWeekly,
                    StartDate = new DateOnly(2010, 1, 1),
                    Title = "Software Engineer",
                    Type = JobType.FullTime,
                    PaymentType = PaymentType.Salaried,
                    Personal401kContributionPercent = 0.05
                }
            ]
        };

        person.Investments = new([
                new Traditional401kAccount(0.05, "Traditional 401k",  DateOnly.FromDateTime(person.BirthDate), 400000 ),
                new RothIRAAccount(0.05, "Roth IRA", person, 300000 ),
                new InvestmentAccount(0.05, nameof(AccountType.Savings), 75000, AccountType.Savings )
            ]);

        return person;
    }

    public static Person Me()
    {
        Person person = new()
        {
            BirthDate = new DateTime(1998, 1, 14),
            FullRetirementAge = 67,
            PartTimeAge = 45,
            PartTimeEndAge = 45,
            EssentialExpenses = 500 * 52 / 2,
            DiscretionarySpending = 1000 * 52 / 2,
            SocialSecurityClaimingAge = 70,
            FileType = FileType.Single,
            SalaryGrowthRate = 0.03,
            InflationRate = 0.02,
            GenderMale = true,
            SocialSecurityIncome = 0,
            Jobs = [
                new(){
                    BonusPay = 1000,
                    CompanyMatchContributionPercent = 0.26,
                    Salary = 65000,
                    PayFrequency = PayFrequency.BiWeekly,
                    StartDate = new DateOnly(2021, 6, 1),
                    Title = "Software Engineer",
                    Type = JobType.FullTime,
                    PaymentType = PaymentType.Salaried,
                    Personal401kContributionPercent = 0.05
                }
            ]
        };

        person.Investments = new([
                new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(person.BirthDate), 75000 ),
                new RothIRAAccount(0.05, "Roth IRA", person, 200 ),
                new InvestmentAccount(0.05, nameof(AccountType.Savings), 3000, AccountType.Savings )
            ]);

        return person;
    }
}
