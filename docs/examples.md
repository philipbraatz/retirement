# Examples & Scenarios

## Overview

This document provides practical examples demonstrating how to use the RetirementPlanner library to model various retirement scenarios. Each example includes detailed calculations, strategic considerations, and implementation code.

## Basic Usage Examples

### Example 1: Simple Retirement Planning

**Scenario:** 50-year-old planning for retirement at 65

```csharp
// Create person
var person = new Person
{
    BirthDate = new DateTime(1975, 6, 15),
    FullRetirementAge = 67,
    SocialSecurityClaimingAge = 67,
    SocialSecurityIncome = 2800, // Monthly benefit at FRA
    EssentialExpenses = 70000,   // Annual essential expenses
    DiscretionarySpending = 20000, // Annual discretionary
    FileType = TaxBrackets.FileType.Single
};

// Create investment accounts
var traditional401k = new Traditional401kAccount(
    annualGrowthRate: 0.07, 
    name: "Company 401(k)",
    birthdate: DateOnly.FromDateTime(person.BirthDate),
    startingBalance: 350000
);

var rothIRA = new RothIRAAccount(
    annualGrowthRate: 0.07,
    name: "Roth IRA", 
    person: person,
    startingBalance: 100000
);

person.Investments = new InvestmentManager([traditional401k, rothIRA]);

// Add employment
person.Jobs = new List<IncomeSource>
{
    new IncomeSource
    {
        Title = "Software Engineer",
        Salary = 100000,
        Personal401kContributionPercent = 15,
        CompanyMatchContributionPercent = 5,
        StartDate = DateOnly.FromDateTime(DateTime.Now)
    }
};

// Create retirement planner
var planner = new RetirementPlanner(person, new RetirementPlanner.Options
{
    StartDate = DateOnly.FromDateTime(DateTime.Now),
    EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(45)), // To age 95
    ReportGranularity = TimeSpan.FromDays(365), // Annual reports
    TimeStep = TimeSpan.FromDays(30) // Monthly steps
});

// Run simulation
await planner.RunRetirementSimulation();
```

**Expected Results:**
- Current age 50 retirement projection through age 95
- Monthly 401(k) contributions with employer match
- Roth IRA growth without contributions (over income limit)
- Social Security benefits starting at age 67
- Tax-efficient withdrawal strategies

### Example 2: Early Retirement Strategy

**Scenario:** 40-year-old planning to retire at 55 with FIRE strategy

```csharp
var person = new Person
{
    BirthDate = new DateTime(1985, 3, 10),
    FullRetirementAge = 67,
    SocialSecurityClaimingAge = 67, // Will bridge to SS
    SocialSecurityIncome = 2200,
    EssentialExpenses = 50000, // Lean FIRE approach
    DiscretionarySpending = 10000,
    FileType = TaxBrackets.FileType.MarriedJointly
};

// Aggressive savings approach
var traditional401k = new Traditional401kAccount(0.08, "401k", 
    DateOnly.FromDateTime(person.BirthDate), 180000);
var rothIRA = new RothIRAAccount(0.08, "Roth IRA", person, 80000);
var taxableAccount = new TaxableAccount(0.07, "Taxable", 120000);

person.Investments = new InvestmentManager([traditional401k, rothIRA, taxableAccount]);

// High-income, high-savings job
person.Jobs = new List<IncomeSource>
{
    new IncomeSource
    {
        Title = "Senior Engineer",
        Salary = 150000,
        Personal401kContributionPercent = 22, // Near max
        CompanyMatchContributionPercent = 6,
        StartDate = DateOnly.FromDateTime(DateTime.Now)
    }
};

// Plan retirement simulation from age 40 to 95
var planner = new RetirementPlanner(person, new RetirementPlanner.Options
{
    StartDate = DateOnly.FromDateTime(DateTime.Now),
    EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(55)),
    ReportGranularity = TimeSpan.FromDays(365),
    TimeStep = TimeSpan.FromDays(30)
});

await planner.RunRetirementSimulation();
```

**Key Features Demonstrated:**
- High savings rate (>50% of income)
- Taxable account bridge strategy for early retirement
- Rule of 55 for 401(k) access
- Conservative spending to support early retirement

### Example 3: Late-Career Catch-Up Strategy

**Scenario:** 55-year-old with limited savings needs aggressive catch-up

```csharp
var person = new Person
{
    BirthDate = new DateTime(1970, 8, 20),
    FullRetirementAge = 67,
    SocialSecurityClaimingAge = 70, // Delay for maximum benefit
    SocialSecurityIncome = 3200, // High earner benefit
    EssentialExpenses = 80000,
    DiscretionarySpending = 25000,
    FileType = TaxBrackets.FileType.Single
};

// Limited starting savings
var traditional401k = new Traditional401kAccount(0.07, "401k",
    DateOnly.FromDateTime(person.BirthDate), 120000);
var tradIRA = new TraditionalIRAAccount(0.07, "Traditional IRA", person, 50000);

person.Investments = new InvestmentManager([traditional401k, tradIRA]);

// High-income job with maximum contributions
person.Jobs = new List<IncomeSource>
{
    new IncomeSource
    {
        Title = "Executive",
        Salary = 180000,
        Personal401kContributionPercent = 23, // Max with catch-up
        CompanyMatchContributionPercent = 4,
        StartDate = DateOnly.FromDateTime(DateTime.Now)
    }
};

// Demonstrate catch-up contribution benefits
var options = new RetirementPlanner.Options
{
    StartDate = DateOnly.FromDateTime(DateTime.Now),
    EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(40)),
    ReportGranularity = TimeSpan.FromDays(365),
    TimeStep = TimeSpan.FromDays(30)
};

var planner = new RetirementPlanner(person, options);
await planner.RunRetirementSimulation();
```

**Analysis Points:**
- Catch-up contributions at age 50+ ($7,500 additional 401k, $1,000 IRA)
- Delayed Social Security for 32% higher benefits
- Aggressive savings rate in final working years
- Late-career income peak optimization

## Advanced Scenarios

### Scenario 1: Tax-Diversified Retirement Strategy

**Objective:** Demonstrate optimal tax diversification across account types

```csharp
public static Person CreateTaxDiversifiedScenario()
{
    var person = new Person
    {
        BirthDate = new DateTime(1973, 12, 5),
        FullRetirementAge = 67,
        SocialSecurityClaimingAge = 67,
        SocialSecurityIncome = 2600,
        EssentialExpenses = 75000,
        DiscretionarySpending = 20000,
        FileType = TaxBrackets.FileType.MarriedJointly
    };

    // Diversified account structure
    var accounts = new List<InvestmentAccount>
    {
        // Traditional accounts (tax-deferred)
        new Traditional401kAccount(0.07, "Traditional 401k", 
            DateOnly.FromDateTime(person.BirthDate), 400000),
        new TraditionalIRAAccount(0.07, "Traditional IRA", person, 150000),
        
        // Roth accounts (tax-free)
        new Roth401kAccount(0.07, "Roth 401k",
            DateOnly.FromDateTime(person.BirthDate), 200000),
        new RothIRAAccount(0.07, "Roth IRA", person, 100000),
        
        // Taxable account (tax-efficient)
        new TaxableAccount(0.06, "Taxable Investments", 180000),
        
        // HSA (triple tax advantage)
        new HSAAccount(0.07, "Health Savings", 25000)
    };

    person.Investments = new InvestmentManager(accounts);

    // Ongoing employment with balanced contributions
    person.Jobs = new List<IncomeSource>
    {
        new IncomeSource
        {
            Title = "Manager", 
            Salary = 120000,
            Personal401kContributionPercent = 12, // Split between traditional and Roth
            CompanyMatchContributionPercent = 6,
            StartDate = DateOnly.FromDateTime(DateTime.Now)
        }
    };

    return person;
}

// Usage
var person = CreateTaxDiversifiedScenario();
var planner = new RetirementPlanner(person, new RetirementPlanner.Options
{
    StartDate = DateOnly.FromDateTime(DateTime.Now),
    EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(45)),
    ReportGranularity = TimeSpan.FromDays(365),
    TimeStep = TimeSpan.FromDays(30)
});

// Set up event handlers to track tax-efficient withdrawals
RetirementPlanner.OnNewMonth += (sender, e) =>
{
    // Example: Log monthly account balances and withdrawals
    Console.WriteLine($"Month {e.Date:yyyy-MM}: Total Portfolio: ${person.Investments.Accounts.Sum(a => a.Balance(e.Date)):N0}");
};

await planner.RunRetirementSimulation();
```

**Strategic Benefits:**
- Tax arbitrage opportunities across brackets
- Roth conversions during low-income years
- Flexible withdrawal sequencing
- Healthcare cost coverage via HSA

### Scenario 2: Social Security Optimization Strategy

**Objective:** Compare different Social Security claiming strategies

```csharp
public static async Task CompareSocialSecurityStrategies()
{
    var basePerson = new Person
    {
        BirthDate = new DateTime(1958, 4, 15), // FRA = 66 years, 8 months
        FullRetirementAge = 67, // Simplified to 67 for calculations
        SocialSecurityIncome = 2800, // PIA at FRA
        EssentialExpenses = 65000,
        DiscretionarySpending = 15000,
        FileType = TaxBrackets.FileType.Single
    };

    // Shared investment setup
    var createInvestments = () => new InvestmentManager(new[]
    {
        new Traditional401kAccount(0.07, "401k", 
            DateOnly.FromDateTime(basePerson.BirthDate), 500000),
        new RothIRAAccount(0.07, "Roth IRA", basePerson, 200000)
    });

    // Strategy 1: Early claiming at 62
    var earlyClaimPerson = basePerson.Clone();
    earlyClaimPerson.SocialSecurityClaimingAge = 62;
    earlyClaimPerson.Investments = createInvestments();

    // Strategy 2: Full Retirement Age claiming
    var fraClaimPerson = basePerson.Clone();
    fraClaimPerson.SocialSecurityClaimingAge = 67;
    fraClaimPerson.Investments = createInvestments();

    // Strategy 3: Delayed claiming to 70
    var delayedClaimPerson = basePerson.Clone();
    delayedClaimPerson.SocialSecurityClaimingAge = 70;
    delayedClaimPerson.Investments = createInvestments();

    // Run simulations
    var strategies = new[]
    {
        ("Early (62)", earlyClaimPerson),
        ("FRA (67)", fraClaimPerson), 
        ("Delayed (70)", delayedClaimPerson)
    };

    foreach (var (name, person) in strategies)
    {
        Console.WriteLine($"\\nAnalyzing {name} claiming strategy:");
        
        var planner = new RetirementPlanner(person, new RetirementPlanner.Options
        {
            StartDate = DateOnly.FromDateTime(DateTime.Now),
            EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(35)),
            ReportGranularity = TimeSpan.FromDays(365),
            TimeStep = TimeSpan.FromDays(30)
        });

        await planner.RunRetirementSimulation();
        
        // Analyze results
        AnalyzeSocialSecurityOutcome(person, name);
    }
}

private static void AnalyzeSocialSecurityOutcome(Person person, string strategy)
{
    var claimingAge = person.SocialSecurityClaimingAge;
    var fullBenefit = person.SocialSecurityIncome;
    
    // Calculate actual benefit based on claiming age
    double multiplier = SocialSecurityBenefitCalculator.CalculateBenefitMultiplier(
        claimingAge, person.FullRetirementAge);
    double actualBenefit = fullBenefit * multiplier;
    
    Console.WriteLine($"  Claiming Age: {claimingAge}");
    Console.WriteLine($"  Monthly Benefit: ${actualBenefit:N0}");
    Console.WriteLine($"  Annual Benefit: ${actualBenefit * 12:N0}");
    Console.WriteLine($"  Lifetime Value (to 85): ${actualBenefit * 12 * (85 - claimingAge):N0}");
    
    // Calculate break-even points
    if (claimingAge == 62)
    {
        double breakEvenAge = SocialSecurityBenefitCalculator.CalculateBreakEvenAge(
            fullBenefit, 62, 67, person.FullRetirementAge);
        Console.WriteLine($"  Break-even vs FRA: Age {breakEvenAge:F1}");
    }
    else if (claimingAge == 70)
    {
        double breakEvenAge = SocialSecurityBenefitCalculator.CalculateBreakEvenAge(
            fullBenefit, 67, 70, person.FullRetirementAge);
        Console.WriteLine($"  Break-even vs FRA: Age {breakEvenAge:F1}");
    }
}
```

### Scenario 3: Required Minimum Distribution Management

**Objective:** Demonstrate RMD planning and tax-efficient distribution strategies

```csharp
public static Person CreateRMDManagementScenario()
{
    var person = new Person
    {
        BirthDate = new DateTime(1950, 10, 30), // Currently 74, RMDs active
        FullRetirementAge = 66,
        SocialSecurityClaimingAge = 66,
        SocialSecurityIncome = 2400,
        EssentialExpenses = 60000,
        DiscretionarySpending = 15000,
        FileType = TaxBrackets.FileType.MarriedJointly
    };

    // Large traditional account balances requiring RMDs
    var accounts = new List<InvestmentAccount>
    {
        new Traditional401kAccount(0.06, "Large 401k",
            DateOnly.FromDateTime(person.BirthDate), 800000),
        new TraditionalIRAAccount(0.06, "Traditional IRA", person, 400000),
        new RothIRAAccount(0.06, "Roth IRA", person, 200000), // No RMDs
        new TaxableAccount(0.05, "Taxable", 150000)
    };

    person.Investments = new InvestmentManager(accounts);
    return person;
}

// Demonstration of RMD calculations and strategies
public static async Task DemonstrateRMDManagement()
{
    var person = CreateRMDManagementScenario();
    int currentAge = person.CurrentAge(DateOnly.FromDateTime(DateTime.Now));
    
    Console.WriteLine($"RMD Analysis for Age {currentAge}:");
    Console.WriteLine("================================");

    // Calculate RMDs for each applicable account
    var rmdAccounts = person.Investments.Accounts
        .Where(a => RMDCalculator.IsSubjectToRMD(a.Type))
        .ToList();

    double totalRMD = 0;
    foreach (var account in rmdAccounts)
    {
        double balance = account.Balance(DateOnly.FromDateTime(DateTime.Now));
        double rmd = RMDCalculator.CalculateRMD(account, currentAge, balance);
        totalRMD += rmd;
        
        Console.WriteLine($"{account.Name}: ${rmd:N0} required (${balance:N0} balance)");
    }
    
    Console.WriteLine($"Total RMD Required: ${totalRMD:N0}");
    
    // Demonstrate tax-efficient RMD strategy
    double socialSecurity = person.SocialSecurityIncome * 12;
    double totalIncome = socialSecurity + totalRMD;
    double expenses = person.EssentialExpenses + person.DiscretionarySpending;
    
    Console.WriteLine($"\\nIncome Analysis:");
    Console.WriteLine($"Social Security: ${socialSecurity:N0}");
    Console.WriteLine($"Required RMDs: ${totalRMD:N0}");
    Console.WriteLine($"Total Income: ${totalIncome:N0}");
    Console.WriteLine($"Annual Expenses: ${expenses:N0}");
    Console.WriteLine($"Surplus/Shortfall: ${totalIncome - expenses:N0}");
    
    // Tax analysis using existing TaxCalculator
    var taxCalculator = new TaxCalculator(person, DateTime.Now.Year);
    double estimatedTax = taxCalculator.GetTaxesOwed(totalIncome);
    Console.WriteLine($"Estimated Tax: ${estimatedTax:N0}");
    Console.WriteLine($"After-tax Income: ${totalIncome - estimatedTax:N0}");

    // Run simulation to see long-term RMD impact
    var planner = new RetirementPlanner(person, new RetirementPlanner.Options
    {
        StartDate = DateOnly.FromDateTime(DateTime.Now),
        EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(20)),
        ReportGranularity = TimeSpan.FromDays(365),
        TimeStep = TimeSpan.FromDays(30)
    });

    await planner.RunRetirementSimulation();
}
```

## Healthcare Planning Scenarios

### Scenario 4: HSA Maximization Strategy

**Objective:** Optimize Health Savings Account for retirement healthcare costs

```csharp
public static Person CreateHSAOptimizationScenario()
{
    var person = new Person
    {
        BirthDate = new DateTime(1970, 2, 14), // Age 55, eligible for catch-up
        FullRetirementAge = 67,
        SocialSecurityClaimingAge = 67,
        SocialSecurityIncome = 2300,
        EssentialExpenses = 65000,
        DiscretionarySpending = 18000,
        FileType = TaxBrackets.FileType.Single
    };

    var accounts = new List<InvestmentAccount>
    {
        new Traditional401kAccount(0.07, "401k",
            DateOnly.FromDateTime(person.BirthDate), 320000),
        new RothIRAAccount(0.07, "Roth IRA", person, 85000),
        
        // HSA with aggressive contribution strategy
        new HSAAccount(0.08, "HSA", 15000) // Higher growth for long-term
    };

    person.Investments = new InvestmentManager(accounts);

    // Employment that allows HSA contributions
    person.Jobs = new List<IncomeSource>
    {
        new IncomeSource
        {
            Title = "Consultant",
            Salary = 95000,
            Personal401kContributionPercent = 18,
            CompanyMatchContributionPercent = 3,
            StartDate = DateOnly.FromDateTime(DateTime.Now)
        }
    };

    return person;
}

// Demonstrate HSA contribution and withdrawal strategies
public static void AnalyzeHSAStrategy(Person person)
{
    int age = person.CurrentAge(DateOnly.FromDateTime(DateTime.Now));
    int yearsTill65 = 65 - age;
    
    // HSA contribution analysis
    double annualContribution = age >= 55 ? 4550 : 3650; // 2025 limits with catch-up
    double totalContributions = annualContribution * yearsTill65;
    
    // Project HSA growth
    double futureValue = CalculateFutureValue(
        currentBalance: 15000,
        annualContribution: annualContribution,
        growthRate: 0.08,
        years: yearsTill65
    );
    
    Console.WriteLine("HSA Optimization Analysis:");
    Console.WriteLine("=========================");
    Console.WriteLine($"Current HSA Balance: $15,000");
    Console.WriteLine($"Annual Contribution (age {age}): ${annualContribution:N0}");
    Console.WriteLine($"Years to Medicare Eligibility: {yearsTill65}");
    Console.WriteLine($"Total Future Contributions: ${totalContributions:N0}");
    Console.WriteLine($"Projected HSA at Age 65: ${futureValue:N0}");
    
    // Healthcare cost analysis
    double annualHealthcareCost = HealthcareCostCalculator.CalculateAnnualHealthcareCosts(65, false);
    double lifetimeHealthcareCosts = annualHealthcareCost * 25; // 25 years of retirement
    
    Console.WriteLine($"\\nHealthcare Cost Projections:");
    Console.WriteLine($"Annual Cost at 65: ${annualHealthcareCost:N0}");
    Console.WriteLine($"Estimated Lifetime Costs: ${lifetimeHealthcareCosts:N0}");
    Console.WriteLine($"HSA Coverage Percentage: {(futureValue / lifetimeHealthcareCosts) * 100:F1}%");
    
    // Tax benefit analysis
    double taxSavings = totalContributions * 0.22; // Assume 22% bracket
    Console.WriteLine($"\\nTax Benefits:");
    Console.WriteLine($"Lifetime Tax Savings: ${taxSavings:N0}");
    Console.WriteLine($"Effective HSA Value: ${futureValue + taxSavings:N0}");
}

private static double CalculateFutureValue(double currentBalance, double annualContribution, 
    double growthRate, int years)
{
    // Formula: FV = PV(1+r)^n + PMT[((1+r)^n - 1) / r]
    double growthFactor = Math.Pow(1 + growthRate, years);
    double currentValueGrowth = currentBalance * growthFactor;
    double contributionGrowth = annualContribution * ((growthFactor - 1) / growthRate);
    
    return currentValueGrowth + contributionGrowth;
}
```

## Real-World Implementation Examples

### Complete Retirement Planning Workflow

```csharp
public class RetirementPlanningWorkflow
{
    public static async Task RunComprehensiveAnalysis()
    {
        // Step 1: Create person with realistic data
        var person = CreateRealisticScenario();
        
        // Step 2: Set up event monitoring
        SetupComprehensiveEventHandlers(person);
        
        // Step 3: Run base scenario
        await RunBaseScenario(person, "Base Case");
        
        // Step 4: Run sensitivity analysis
        await RunSensitivityAnalysis(person);
        
        // Step 5: Optimize strategies
        await OptimizeRetirementStrategy(person);
    }
    
    private static Person CreateRealisticScenario()
    {
        var person = new Person
        {
            BirthDate = new DateTime(1975, 7, 20),
            FullRetirementAge = 67,
            SocialSecurityClaimingAge = 67, // Will analyze alternatives
            SocialSecurityIncome = 2650,
            EssentialExpenses = 72000,
            DiscretionarySpending = 28000,
            FileType = TaxBrackets.FileType.MarriedJointly
        };

        // Realistic account balances for age 50
        var accounts = new List<InvestmentAccount>
        {
            new Traditional401kAccount(0.07, "Company 401k", 
                DateOnly.FromDateTime(person.BirthDate), 420000),
            new Roth401kAccount(0.07, "Roth 401k",
                DateOnly.FromDateTime(person.BirthDate), 180000),
            new RothIRAAccount(0.07, "Roth IRA", person, 95000),
            new TaxableAccount(0.06, "Taxable Investments", 140000),
            new HSAAccount(0.08, "HSA", 18000)
        };

        person.Investments = new InvestmentManager(accounts);

        person.Jobs = new List<IncomeSource>
        {
            new IncomeSource
            {
                Title = "Senior Manager",
                Salary = 125000,
                Personal401kContributionPercent = 16,
                CompanyMatchContributionPercent = 6,
                StartDate = DateOnly.FromDateTime(DateTime.Now)
            }
        };

        return person;
    }

    private static async Task RunSensitivityAnalysis(Person basePerson)
    {
        Console.WriteLine("\\n=== Sensitivity Analysis ===");
        
        var scenarios = new[]
        {
            ("Conservative Growth", 0.05),
            ("Moderate Growth", 0.07),
            ("Aggressive Growth", 0.09),
            ("Bear Market", 0.03)
        };

        foreach (var (name, growthRate) in scenarios)
        {
            var person = basePerson.Clone();
            
            // Adjust growth rates for all accounts
            foreach (var account in person.Investments.Accounts)
            {
                account.UpdateGrowthRate(growthRate);
            }

            Console.WriteLine($"\\nScenario: {name} ({growthRate:P1} growth)");
            await RunBaseScenario(person, name);
        }
    }

    private static async Task OptimizeRetirementStrategy(Person basePerson)
    {
        Console.WriteLine("\\n=== Strategy Optimization ===");

        // Test different Social Security claiming strategies
        var claimingAges = new[] { 62, 67, 70 };
        
        foreach (var claimingAge in claimingAges)
        {
            var person = basePerson.Clone();
            person.SocialSecurityClaimingAge = claimingAge;
            
            Console.WriteLine($"\\nOptimizing for SS claiming at age {claimingAge}:");
            await RunBaseScenario(person, $"SS at {claimingAge}");
        }
    }

    private static async Task RunBaseScenario(Person person, string scenarioName)
    {
        var planner = new RetirementPlanner(person, new RetirementPlanner.Options
        {
            StartDate = DateOnly.FromDateTime(DateTime.Now),
            EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(45)),
            ReportGranularity = TimeSpan.FromDays(365),
            TimeStep = TimeSpan.FromDays(30)
        });

        Console.WriteLine($"Running scenario: {scenarioName}");
        await planner.RunRetirementSimulation();
        
        // Analyze results
        AnalyzeScenarioResults(person, scenarioName);
    }

    private static void AnalyzeScenarioResults(Person person, string scenarioName)
    {
        var currentDate = DateOnly.FromDateTime(DateTime.Now);
        double totalAssets = person.Investments.Accounts.Sum(a => a.Balance(currentDate));
        
        Console.WriteLine($"\\nResults for {scenarioName}:");
        Console.WriteLine($"Total Assets: ${totalAssets:N0}");
        
        // Project to retirement
        int yearsToRetirement = 65 - person.CurrentAge(currentDate);
        double projectedRetirementAssets = ProjectRetirementAssets(person, yearsToRetirement);
        
        Console.WriteLine($"Projected Retirement Assets: ${projectedRetirementAssets:N0}");
        
        // Calculate withdrawal rate
        double annualExpenses = person.EssentialExpenses + person.DiscretionarySpending;
        double withdrawalRate = annualExpenses / projectedRetirementAssets;
        
        Console.WriteLine($"Required Withdrawal Rate: {withdrawalRate:P2}");
        Console.WriteLine($"Scenario Viability: {(withdrawalRate <= 0.04 ? "GOOD" : "NEEDS ADJUSTMENT")}");
    }

    private static double ProjectRetirementAssets(Person person, int years)
    {
        // Simplified projection - actual simulation provides more accurate results
        double currentAssets = person.Investments.Accounts.Sum(a => a.Balance(DateOnly.FromDateTime(DateTime.Now)));
        double annualContributions = person.Jobs.Sum(j => j.Salary * (j.Personal401kContributionPercent ?? 0) / 100);
        
        return currentAssets * Math.Pow(1.07, years) + 
               annualContributions * ((Math.Pow(1.07, years) - 1) / 0.07);
    }
}
```

## Performance Analysis and Optimization

### Benchmarking Different Strategies

```csharp
public class StrategyBenchmark
{
    public static async Task CompareRetirementStrategies()
    {
        var baselineScenario = CreateBaselineScenario();
        
        var strategies = new Dictionary<string, Func<Person>>
        {
            ["Conservative"] = () => CreateConservativeStrategy(baselineScenario),
            ["Aggressive"] = () => CreateAggressiveStrategy(baselineScenario),
            ["Balanced"] = () => CreateBalancedStrategy(baselineScenario),
            ["Tax-Optimized"] = () => CreateTaxOptimizedStrategy(baselineScenario)
        };

        var results = new Dictionary<string, StrategyResult>();

        foreach (var (strategyName, createStrategy) in strategies)
        {
            var person = createStrategy();
            var result = await AnalyzeStrategy(person, strategyName);
            results[strategyName] = result;
        }

        // Compare results
        DisplayStrategyComparison(results);
    }

    private static async Task<StrategyResult> AnalyzeStrategy(Person person, string strategyName)
    {
        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var endDate = startDate.AddYears(45);
        
        var planner = new RetirementPlanner(person, new RetirementPlanner.Options
        {
            StartDate = startDate,
            EndDate = endDate,
            ReportGranularity = TimeSpan.FromDays(365),
            TimeStep = TimeSpan.FromDays(30)
        });

        await planner.RunRetirementSimulation();

        return new StrategyResult
        {
            StrategyName = strategyName,
            FinalAssets = person.Investments.Accounts.Sum(a => a.Balance(endDate)),
            TotalTaxesPaid = CalculateTotalTaxesPaid(person),
            SuccessProbability = CalculateSuccessProbability(person)
        };
    }

    public class StrategyResult
    {
        public string StrategyName { get; set; }
        public double FinalAssets { get; set; }
        public double TotalTaxesPaid { get; set; }
        public double SuccessProbability { get; set; }
    }
}
```

This comprehensive set of examples demonstrates the practical application of the RetirementPlanner library across various scenarios, from basic retirement planning to advanced optimization strategies. Each example includes realistic assumptions, detailed calculations, and strategic analysis to help users understand both the implementation and the underlying retirement planning concepts.
