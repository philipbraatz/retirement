using static RetirementPlanner.TaxBrackets;

namespace RetirementPlanner.Test;

public static class TestPersonFactory
{
    public static Person CreateEarlyRetiree()
    {
        // Create person first for RothIRA constructor
        Person tempPerson = new()
        {
            BirthDate = new DateTime(1985, 6, 15),
        };

        var traditional401k = new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(new DateTime(1985, 6, 15)), 500000);
        var rothIRA = new RothIRAAccount(0.05, "Roth IRA", tempPerson, 200000);
        var taxableAccount = new InvestmentAccount(0.05, "Taxable Account", 100000, AccountType.Savings);
        var pocketCash = new InvestmentAccount(0.0, "Pocket Cash", 0, AccountType.Savings);

        Person person = new(traditional401k, rothIRA, taxableAccount, pocketCash)
        {
            BirthDate = new DateTime(1985, 6, 15), // Age 39
            FullRetirementAge = 67,
            SalaryGrowthRate = 0.03,
            EssentialExpenses = 40000,
            DiscretionarySpending = 10000,
            SocialSecurityClaimingAge = 62,
            PartTimeAge = 40, // Early retirement plan
            
            // Emergency Fund Configuration for Early Retiree
            AutoCalculateEmergencyFunds = true,
            PreRetirementEmergencyMonths = 6,
            EarlyRetirementEmergencyMonths = 24, // 2 years for early retirement
            PostRetirementEmergencyMonths = 12
        };

        // Add a job to generate income
        person.Jobs.Add(new IncomeSource
        {
            Type = JobType.FullTime,
            PaymentType = PaymentType.Salaried,
            Salary = 100000,
            RetirementContributionPercent = 0.10,
            CompanyMatchContributionPercent = 0.05,
            PayRaisePercent = 0.03,
            Title = "Software Engineer",
            StartDate = new DateOnly(2010, 1, 1),
            PayFrequency = PayFrequency.BiWeekly
        });

        return person;
    }

    public static Person CreateNormalRetiree()
    {
        // Create person first for RothIRA constructor
        Person tempPerson = new()
        {
            BirthDate = new DateTime(1960, 3, 20),
        };

        var traditionalIRA = new InvestmentAccount(0.05, "Traditional IRA", 600000, AccountType.TraditionalIRA);
        var rothIRA = new RothIRAAccount(0.05, "Roth IRA", tempPerson, 250000);
        var taxableAccount = new InvestmentAccount(0.05, "Taxable Account", 50000, AccountType.Savings);
        var pocketCash = new InvestmentAccount(0.0, "Pocket Cash", 0, AccountType.Savings);

        Person person = new(traditionalIRA, rothIRA, taxableAccount, pocketCash)
        {
            BirthDate = new DateTime(1960, 3, 20), // Age 64
            FullRetirementAge = 67,
            SalaryGrowthRate = 0.03,
            EssentialExpenses = 50000,
            DiscretionarySpending = 15000,
            SocialSecurityClaimingAge = 67,
            
            // Emergency Fund Configuration for Normal Retiree
            AutoCalculateEmergencyFunds = true,
            PreRetirementEmergencyMonths = 6,
            EarlyRetirementEmergencyMonths = 18,
            PostRetirementEmergencyMonths = 12
        };

        // Add a job with lower income for someone approaching retirement
        person.Jobs.Add(new IncomeSource
        {
            Type = JobType.FullTime,
            PaymentType = PaymentType.Salaried,
            Salary = 90000,
            RetirementContributionPercent = 0.06,
            CompanyMatchContributionPercent = 0.03,
            PayRaisePercent = 0.02,
            Title = "Software Engineer",
            StartDate = new DateOnly(2010, 1, 1),
            PayFrequency = PayFrequency.BiWeekly
        });

        return person;
    }

    public static Person CreateLateRetiree()
    {
        // Create person first for RothIRA constructor
        Person tempPerson = new()
        {
            BirthDate = new DateTime(1950, 1, 1),
        };

        // Create the person first, then add accounts and deposits
        Person person = new()
        {
            BirthDate = new DateTime(1950, 1, 1), // Age 74
            FullRetirementAge = 67,
            EssentialExpenses = 45000,
            DiscretionarySpending = 20000,
            SocialSecurityClaimingAge = 70,
            SocialSecurityIncome = 3000, // Monthly SS income
            
            // Emergency Fund Configuration for Late Retiree
            AutoCalculateEmergencyFunds = true,
            PreRetirementEmergencyMonths = 6,
            EarlyRetirementEmergencyMonths = 18,
            PostRetirementEmergencyMonths = 12
        };

        // Create accounts and add deposits with proper dates
        var traditional401k = new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(new DateTime(1950, 1, 1)), 0);
        traditional401k.Deposit(400000, new DateOnly(2023, 1, 1), TransactionCategory.InternalTransfer);
        
        var rothIRA = new RothIRAAccount(0.05, "Roth IRA", person, 0);
        rothIRA.Deposit(300000, new DateOnly(2023, 1, 1), TransactionCategory.InternalTransfer);
        
        var taxableAccount = new InvestmentAccount(0.05, "Taxable Account", 0, AccountType.Savings);
        taxableAccount.Deposit(75000, new DateOnly(2023, 1, 1), TransactionCategory.InternalTransfer);

        var pocketCash = new InvestmentAccount(0.0, "Pocket Cash", 0, AccountType.Savings);

        // Set up investment manager with the accounts
        person.Investments = new InvestmentManager([traditional401k, rothIRA, taxableAccount]);

        // No jobs - fully retired
        return person;
    }

    public static Person Me()
    {
        // Create person first for RothIRA constructor
        Person tempPerson = new()
        {
            BirthDate = new DateTime(1998, 1, 14),
        };

        var traditional401k = new Traditional401kAccount(0.05, "Traditional 401k", DateOnly.FromDateTime(new DateTime(1998, 1, 14)), 75000);
        var roth401k = new Roth401kAccount(0.05, "Roth 401k", DateOnly.FromDateTime(new DateTime(1998, 1, 14)), 0); // Start with 0 to see growth
        var rothIRA = new RothIRAAccount(0.05, "Roth IRA", tempPerson, 500);
        var savingsAccount = new InvestmentAccount(0.05, "Savings", 8000, AccountType.Savings); // Increased from 3000 to 8000
        var pocketCash = new InvestmentAccount(0.0, "Pocket Cash", 0, AccountType.Savings); // Added Pocket Cash account

        Person person = new(traditional401k, roth401k, rothIRA, savingsAccount, pocketCash)
        {
            BirthDate = new DateTime(1998, 1, 14),
            FullRetirementAge = 67,
            PartTimeAge = 45,
            PartTimeEndAge = 45,
            EssentialExpenses = 400 * 12, // $4,800/year
            DiscretionarySpending = 500 * 12, // $6,000/year  
            SocialSecurityClaimingAge = 70,
            FileType = FileType.Single,
            SalaryGrowthRate = 0.03,
            InflationRate = 0.02,
            GenderMale = true,
            SocialSecurityIncome = 0,
            
            // Emergency Fund Configuration - Reasonable for young working professional
            AutoCalculateEmergencyFunds = true,
            PreRetirementEmergencyMonths = 6, // 6 months = $5,400 (reasonable while working)
            EarlyRetirementEmergencyMonths = 18, // 18 months for actual early retirement (not planning)
            PostRetirementEmergencyMonths = 12
        };

        // Add current job
        person.Jobs.Add(new IncomeSource
        {
            Type = JobType.FullTime,
            PaymentType = PaymentType.Salaried,
            Salary = 63000,
            RetirementContributionPercent = 0.15, // More reasonable 15% instead of 26%
            CompanyMatchContributionPercent = 0.05,
            PayRaisePercent = 0.03,
            Title = "Software Engineer",
            StartDate = new DateOnly(2025, 6, 1),
            PayFrequency = PayFrequency.Monthly // Changed to monthly for easier tracking
        });

        return person;
    }
}
