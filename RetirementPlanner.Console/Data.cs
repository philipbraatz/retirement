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
                    RetirementContributionPercent = 0.05
                }
            ]
        };

        person.Investments = new([
            new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(person.BirthDate), 500000, DateOnly.FromDateTime(person.BirthDate)),
            new Roth401kAccount(0.05, "Roth 401k", DateOnly.FromDateTime(person.BirthDate), 0, DateOnly.FromDateTime(person.BirthDate)),
            new RothIRAAccount(0.05, "Roth IRA", person, 200000, DateOnly.FromDateTime(person.BirthDate)),
            new InvestmentAccount(0.05, nameof(AccountType.Savings), 100000, AccountType.Savings, DateOnly.FromDateTime(person.BirthDate))
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
                    RetirementContributionPercent = 0.05
                }
            ]
        };

        person.Investments = new([
             new Traditional401kAccount(0.05, "Traditional IRA", DateOnly.FromDateTime(person.BirthDate), 600000, DateOnly.FromDateTime(person.BirthDate)),
             new Roth401kAccount(0.05, "Roth 401k", DateOnly.FromDateTime(person.BirthDate), 0, DateOnly.FromDateTime(person.BirthDate)),
             new RothIRAAccount(0.05, "Roth IRA",person, 250000, DateOnly.FromDateTime(person.BirthDate)),
             new InvestmentAccount(0.05, nameof(AccountType.Savings), 50000, AccountType.Savings, DateOnly.FromDateTime(person.BirthDate))
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
                    BonusPay = 1000,
                    CompanyMatchContributionPercent = 0.03,
                    Salary = 65000,
                    PayFrequency = PayFrequency.BiWeekly,
                    StartDate = new DateOnly(2020, 1, 1),
                    Title = "Software Engineer",
                    Type = JobType.FullTime,
                    PaymentType = PaymentType.Salaried,
                    RetirementContributionPercent = 0.05
                }
            ]
        };

        person.Investments = new([
                new Traditional401kAccount(0.05, "Traditional 401k",  DateOnly.FromDateTime(person.BirthDate), 80000, DateOnly.FromDateTime(person.BirthDate)),
                new Roth401kAccount(0.05, "Roth 401k", DateOnly.FromDateTime(person.BirthDate), 500, DateOnly.FromDateTime(person.BirthDate)),
                new InvestmentAccount(0.05, nameof(AccountType.Savings), 5000, AccountType.Savings, DateOnly.FromDateTime(person.BirthDate))
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
            EssentialExpenses = 300 * 12, // $3,600/year (reasonable for single person)
            DiscretionarySpending = 200 * 12, // $2,400/year (reasonable discretionary)
            SocialSecurityClaimingAge = 70,
            FileType = FileType.Single,
            SalaryGrowthRate = 0.03,
            InflationRate = 0.024, // 2.4% inflation rate
            GenderMale = true,
            SocialSecurityIncome = 0,
            
            // Emergency Fund Configuration - More reasonable
            AutoCalculateEmergencyFunds = true,
            PreRetirementEmergencyMonths = 6, // 6 months for working professional
            EarlyRetirementEmergencyMonths = 18, // 18 months for early retirement
            PostRetirementEmergencyMonths = 12,
            
            Jobs = [
                new(){
                    BonusPay = 1000,
                    CompanyMatchContributionPercent = 0.03, // 3% employer match (more realistic)
                    Salary = 65000,
                    PayFrequency = PayFrequency.BiWeekly,
                    StartDate = new DateOnly(2024, 1, 1), // Job already started (not future date)
                    Title = "Software Engineer",
                    Type = JobType.FullTime,
                    PaymentType = PaymentType.Salaried,
                    RetirementContributionPercent = 0.15 // 15% total contribution
                }
            ]
        };

        person.Investments = new([
            new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(person.BirthDate), 75000, DateOnly.FromDateTime(person.BirthDate)),
            new Roth401kAccount(0.05, "Roth 401k", DateOnly.FromDateTime(person.BirthDate), 0, DateOnly.FromDateTime(person.BirthDate)),
            new RothIRAAccount(0.05, "Roth IRA", person, 500, DateOnly.FromDateTime(person.BirthDate)),
            new InvestmentAccount(0.05, nameof(AccountType.Savings), 15000, AccountType.Savings, DateOnly.FromDateTime(person.BirthDate)) // Increased to adequate emergency fund
        ]);

        return person;
    }
}
