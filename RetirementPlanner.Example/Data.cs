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
            CurrentSalary = 100000,
            SalaryGrowthRate = 0.03,
            EssentialExpenses = 40000,
            DiscretionarySpending = 10000,
            SocialSecurityClaimingAge = 62,
            IncomeYearly = 70000,
        };

        person.Accounts = [
            new InvestmentAccount(0.05, "Traditional 401k", AccountType.Traditional401k, person, 500000),
                new InvestmentAccount(0.05, "Roth IRA", AccountType.RothIRA, person, 200000),
                new InvestmentAccount(0.05, "Taxable Account", AccountType.Savings, person, 100000)
            ];

        return person;
    }

    public static Person CreateNormalRetiree()
    {
        Person person = new()
        {
            BirthDate = new DateTime(1960, 3, 20), // Age 64
            FullRetirementAge = 67,
            CurrentSalary = 90000,
            SalaryGrowthRate = 0.03,
            EssentialExpenses = 50000,
            DiscretionarySpending = 15000,
            SocialSecurityClaimingAge = 67,
            IncomeYearly = 80000,
        };

        person.Accounts = [
             new InvestmentAccount(0.05, "Traditional IRA", AccountType.TraditionalIRA, person, 600000 ),
                new InvestmentAccount(0.05, "Roth IRA", AccountType.RothIRA, person,250000 ),
                new InvestmentAccount(0.05, "Taxable Account", AccountType.Savings, person, 50000 )
            ];
        return person;
    }

    public static Person CreateLateRetiree()
    {
        Person person = new()
        {
            BirthDate = new DateTime(1950, 1, 1), // Age 74
            FullRetirementAge = 67,
            CurrentSalary = 0, // Fully retired
            EssentialExpenses = 45000,
            DiscretionarySpending = 20000,
            SocialSecurityClaimingAge = 70,
            IncomeYearly = 90000
        };

        person.Accounts = [
                new InvestmentAccount(0.05, "Traditional 401k", AccountType.Traditional401k, person, 400000 ),
                new InvestmentAccount(0.05, "Roth IRA", AccountType.RothIRA, person, 300000 ),
                new InvestmentAccount(0.05, "Taxable Account", AccountType.Savings, person, 75000 )
            ];

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
            CurrentSalary = 63000, // Fully retired
            EssentialExpenses = 400 * 12 * 10,
            DiscretionarySpending = 500 * 12 * 3,
            SocialSecurityClaimingAge = 70,
            IncomeYearly = 63000,
            FileType = FileType.Single,
            EmployerMatchPercentage = 0.05,
            Personal401kContributionRate = 0.15,
            SalaryGrowthRate = 0.03,
            InflationRate = 0.02,
            GenderMale = true,
            SocialSecurityIncome = 0
        };

        person.Accounts = [
                new InvestmentAccount(0.05, "Traditional 401k", AccountType.Traditional401k, person, 75000 ),
                new InvestmentAccount(0.05, "Roth IRA", AccountType.RothIRA, person, 200 ),
                new InvestmentAccount(0.05, "Taxable Account", AccountType.Savings, person, 3000 )
            ];

        return person;
    }
}
