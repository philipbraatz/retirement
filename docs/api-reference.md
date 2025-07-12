# API Reference

## Overview

This document provides comprehensive API documentation for the RetirementPlanner library, including classes, methods, properties, and usage examples for developers implementing retirement planning functionality.

## Core Classes

### RetirementPlanner

The main class that orchestrates retirement planning simulations.

**Constructor:**
```csharp
public RetirementPlanner(Person person, Options options)
```

**Properties:**
- `Person Person` - The person for whom the retirement plan is being calculated
- `Options Options` - Configuration options for the simulation

**Methods:**

#### RunRetirementSimulation()
```csharp
public async Task RunRetirementSimulation()
```
Executes the complete retirement simulation from start date to end date.

**Example:**
```csharp
var planner = new RetirementPlanner(person, options);
await planner.RunRetirementSimulation();
```

### RetirementPlanner.Options

Configuration class for retirement planning simulations.

**Properties:**
```csharp
public class Options
{
    public DateOnly StartDate { get; set; }           // Simulation start date
    public DateOnly EndDate { get; set; }             // Simulation end date  
    public TimeSpan ReportGranularity { get; set; }   // Frequency of progress reports
    public TimeSpan TimeStep { get; set; }            // Simulation time step interval
}
```

**Example:**
```csharp
var options = new RetirementPlanner.Options
{
    StartDate = DateOnly.FromDateTime(DateTime.Now),
    EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(40)),
    ReportGranularity = TimeSpan.FromDays(365), // Annual reports
    TimeStep = TimeSpan.FromDays(30)            // Monthly simulation steps
};
```

### Person

Represents an individual's complete financial and demographic profile.

**Properties:**
```csharp
public class Person
{
    // Demographics
    public DateTime BirthDate { get; set; }
    public int FullRetirementAge { get; set; }
    public int SocialSecurityClaimingAge { get; set; }
    
    // Income and Expenses
    public double SocialSecurityIncome { get; set; }  // Monthly benefit at FRA
    public double EssentialExpenses { get; set; }     // Annual essential expenses
    public double DiscretionarySpending { get; set; } // Annual discretionary spending
    public List<IncomeSource> Jobs { get; set; }
    
    // Tax Information
    public TaxBrackets.FileType FileType { get; set; }
    
    // Investments
    public InvestmentManager Investments { get; set; }
}
```

**Methods:**

#### CurrentAge()
```csharp
public int CurrentAge(DateOnly date)
```
Calculates age at a specific date.

**Example:**
```csharp
var person = new Person { BirthDate = new DateTime(1975, 6, 15) };
int age = person.CurrentAge(DateOnly.FromDateTime(DateTime.Now));
```

#### IncomeYearly
```csharp
public double IncomeYearly { get; }
```
Calculated property returning total annual income from all sources.

### IncomeSource

Represents employment income and benefits.

**Properties:**
```csharp
public class IncomeSource
{
    public string Title { get; set; }
    public JobType Type { get; set; }
    public double Salary { get; set; }
    public PaymentType PaymentType { get; set; }
    public PayFrequency PayFrequency { get; set; }
    
    // 401(k) Contributions
    public double? Personal401kContributionPercent { get; set; }
    public double? CompanyMatchContributionPercent { get; set; }
    
    // Work Schedule
    public double HoursWorkedWeekly { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
```

**Enumerations:**
```csharp
public enum JobType { FullTime, PartTime, Contract, Consulting }
public enum PaymentType { Salaried, Hourly }  
public enum PayFrequency { Weekly, BiWeekly, Monthly, Quarterly, Annually }
```

## Investment Account Classes

### InvestmentAccount (Base Class)

Abstract base class for all investment accounts.

**Properties:**
```csharp
public abstract class InvestmentAccount
{
    public string Name { get; protected set; }
    public AccountType Type { get; protected set; }
    public double AnnualGrowthRate { get; protected set; }
    public DateOnly BirthDate { get; protected set; }
    
    // Transaction History
    public List<Transaction> DepositHistory { get; protected set; }
    public List<Transaction> WithdrawalHistory { get; protected set; }
}
```

**Methods:**

#### Balance()
```csharp
public abstract double Balance(DateOnly date)
```
Returns account balance on a specific date.

#### Deposit()
```csharp
public virtual double Deposit(double amount, DateOnly date, TransactionCategory category)
```
Adds money to the account, subject to contribution limits.

#### Withdraw()
```csharp
public virtual double Withdraw(double amount, DateOnly date, TransactionCategory category)
```
Removes money from the account, subject to availability and rules.

### Traditional401kAccount

Traditional employer-sponsored 401(k) account with pre-tax contributions.

**Constructor:**
```csharp
public Traditional401kAccount(double annualGrowthRate, string name, 
    DateOnly birthdate, double startingBalance = 0)
```

**Key Features:**
- Pre-tax contributions reduce current taxable income
- Subject to Required Minimum Distributions at age 73
- Early withdrawal penalties before age 59½ (with exceptions)
- Annual contribution limits with catch-up provisions

**Example:**
```csharp
var account = new Traditional401kAccount(
    annualGrowthRate: 0.07,
    name: "Company 401(k)",
    birthdate: DateOnly.FromDateTime(person.BirthDate),
    startingBalance: 250000
);
```

### Roth401kAccount

Employer-sponsored Roth 401(k) with after-tax contributions and tax-free growth.

**Constructor:**
```csharp
public Roth401kAccount(double annualGrowthRate, string name, 
    DateOnly birthdate, double startingBalance = 0)
```

**Key Features:**
- After-tax contributions (no current tax deduction)
- Tax-free qualified withdrawals
- Subject to RMDs (unlike Roth IRA)
- Can be rolled to Roth IRA to eliminate RMDs

### TraditionalIRAAccount

Individual Retirement Account with potential tax-deductible contributions.

**Constructor:**
```csharp
public TraditionalIRAAccount(double annualGrowthRate, string name, 
    Person person, double startingBalance = 0)
```

**Key Features:**
- Deduction limits based on income and workplace plan participation
- Subject to RMDs at age 73
- Can be aggregated with other IRAs for RMD purposes

### RothIRAAccount

Individual Retirement Account with after-tax contributions and tax-free growth.

**Constructor:**
```csharp
public RothIRAAccount(double annualGrowthRate, string name, 
    Person person, double startingBalance = 0)
```

**Key Features:**
- Income limits for direct contributions
- No RMDs during owner's lifetime
- 5-year rule for tax-free withdrawals
- Contribution withdrawals always penalty-free

### TaxableAccount

Standard investment account with no special tax treatment.

**Constructor:**
```csharp
public TaxableAccount(double annualGrowthRate, string name, double startingBalance = 0)
```

**Key Features:**
- No contribution limits
- Taxed annually on dividends and interest
- Capital gains rates on appreciation
- Full liquidity and access

### HSAAccount

Health Savings Account with triple tax advantage.

**Constructor:**
```csharp
public HSAAccount(double annualGrowthRate, string name, double startingBalance = 0)
```

**Key Features:**
- Tax-deductible contributions
- Tax-free growth
- Tax-free withdrawals for medical expenses
- After age 65: penalty-free non-medical withdrawals (taxed as ordinary income)

## Utility Classes

### InvestmentManager

Manages a collection of investment accounts for a person.

**Constructor:**
```csharp
public InvestmentManager(IEnumerable<InvestmentAccount> accounts)
```

**Properties:**
```csharp
public List<InvestmentAccount> Accounts { get; }
```

**Methods:**

#### TotalBalance()
```csharp
public double TotalBalance(DateOnly date)
```
Returns sum of all account balances on a specific date.

**Example:**
```csharp
var manager = new InvestmentManager(new[]
{
    new Traditional401kAccount(0.07, "401k", birthdate, 300000),
    new RothIRAAccount(0.07, "Roth IRA", person, 100000)
});

double total = manager.TotalBalance(DateOnly.FromDateTime(DateTime.Now));
```

### ContributionLimits

Static class providing current year contribution limits for retirement accounts.

**Methods:**

#### Get401kPersonalLimit()
```csharp
public static double Get401kPersonalLimit(int year, int age)
```
Returns 401(k) contribution limit including catch-up contributions.

#### GetIRALimit()
```csharp
public static double GetIRALimit(int year, int age)
```
Returns IRA contribution limit including catch-up contributions.

**Example:**
```csharp
int year = 2025;
int age = 55;

double limit401k = ContributionLimits.Get401kPersonalLimit(year, age); // $31,000
double limitIRA = ContributionLimits.GetIRALimit(year, age);           // $8,000
```

### SocialSecurityBenefitCalculator

Static class for Social Security benefit calculations and analysis.

**Methods:**

#### CalculateBenefitMultiplier()
```csharp
public static double CalculateBenefitMultiplier(int claimingAge, int fullRetirementAge)
```
Returns benefit multiplier based on claiming age relative to FRA.

#### CalculateBreakEvenAge()
```csharp
public static double CalculateBreakEvenAge(double monthlyPIA, int earlyAge, 
    int laterAge, int fullRetirementAge)
```
Calculates break-even age between two claiming strategies.

#### AnalyzeClaimingStrategies()
```csharp
public static Dictionary<int, (double MonthlyBenefit, double AnnualBenefit, string Description)> 
    AnalyzeClaimingStrategies(double monthlyPIA, int fullRetirementAge)
```
Provides comprehensive analysis of all claiming ages from 62-70.

**Example:**
```csharp
double pia = 2400; // Monthly benefit at FRA
int fra = 67;

// Get benefit at age 62
double earlyMultiplier = SocialSecurityBenefitCalculator.CalculateBenefitMultiplier(62, fra);
double earlyBenefit = pia * earlyMultiplier; // $1,680

// Analyze all strategies
var strategies = SocialSecurityBenefitCalculator.AnalyzeClaimingStrategies(pia, fra);
foreach (var (age, (monthly, annual, description)) in strategies)
{
    Console.WriteLine($"Age {age}: ${monthly:N0}/month - {description}");
}
```

### RMDCalculator

Static class for Required Minimum Distribution calculations.

**Methods:**

#### IsSubjectToRMD()
```csharp
public static bool IsSubjectToRMD(AccountType accountType)
```
Determines if account type requires RMDs.

#### CalculateRMD()
```csharp
public static double CalculateRMD(InvestmentAccount account, int age, double balance)
```
Calculates required minimum distribution for given age and balance.

#### CalculateRMDPenalty()
```csharp
public static double CalculateRMDPenalty(double requiredAmount, double actualAmount)
```
Calculates penalty for insufficient RMD.

**Example:**
```csharp
var account = new Traditional401kAccount(0.07, "401k", birthdate, 500000);
int age = 75;
double balance = account.Balance(DateOnly.FromDateTime(DateTime.Now));

bool requiresRMD = RMDCalculator.IsSubjectToRMD(account.Type); // true
double rmd = RMDCalculator.CalculateRMD(account, age, balance); // ~$20,325

// If insufficient distribution
double penalty = RMDCalculator.CalculateRMDPenalty(rmd, rmd * 0.8); // 25% of shortfall
```

### TaxBrackets

Static class providing tax bracket information for retirement planning.

**Properties:**
```csharp
public static Dictionary<FileType, List<TaxBracket>> Brackets { get; }

public enum FileType
{
    Single,
    MarriedJointly, 
    MarriedSeparately,
    HeadOfHousehold
}

public class TaxBracket
{
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public double Rate { get; set; }
}
```

**Example:**
```csharp
var singleBrackets = TaxBrackets.Brackets[TaxBrackets.FileType.Single];
foreach (var bracket in singleBrackets)
{
    Console.WriteLine($"{bracket.Rate:P0}: ${bracket.LowerBound:N0} - ${bracket.UpperBound:N0}");
}
```

### HealthcareCostCalculator

Static class for healthcare cost projections in retirement.

**Methods:**

#### CalculateAnnualHealthcareCosts()
```csharp
public static double CalculateAnnualHealthcareCosts(int age, bool hasSupplementalInsurance)
```
Estimates annual healthcare costs based on age and insurance coverage.

**Example:**
```csharp
int age = 70;
bool hasSupplemental = false;

double annualCost = HealthcareCostCalculator.CalculateAnnualHealthcareCosts(age, hasSupplemental);
Console.WriteLine($"Estimated annual healthcare cost at {age}: ${annualCost:N0}");
```

### EarlyWithdrawalPenaltyCalculator

Static class for calculating early withdrawal penalties from retirement accounts.

**Methods:**

#### CalculatePenalty()
```csharp
public static double CalculatePenalty(AccountType accountType, double withdrawalAmount, 
    int age, DateTime? separationDate, WithdrawalReason reason)
```
Calculates early withdrawal penalty considering all applicable exceptions.

**Example:**
```csharp
var penalty = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(
    AccountType.Traditional401k,
    withdrawalAmount: 50000,
    age: 57,
    separationDate: DateTime.Now.AddYears(-1), // Separated last year
    reason: WithdrawalReason.GeneralDistribution
); // May qualify for Rule of 55 exception
```

## Events and Event Handling

The RetirementPlanner class fires various events during simulation to track important milestones and enable custom handling.

### Event Types

#### Age-Based Events
```csharp
// Catch-up contributions become available
public static event EventHandler<AgeEventArgs> OnCatchUpContributionsEligible;

// Rule of 55 becomes available  
public static event EventHandler<AgeEventArgs> OnRuleOf55Eligible;

// Early withdrawal penalties end
public static event EventHandler<AgeEventArgs> OnEarlyWithdrawalPenaltyEnds;

// Social Security early eligibility
public static event EventHandler<AgeEventArgs> OnSocialSecurityEarlyEligible;

// Medicare eligibility
public static event EventHandler<AgeEventArgs> OnMedicareEligible;

// Full Retirement Age reached
public static event EventHandler<AgeEventArgs> OnFullRetirementAge;

// RMDs begin
public static event EventHandler<AgeEventArgs> OnRMDAgeHit;
```

#### Financial Events
```csharp
// Monthly processing
public static event EventHandler<MonthlyEventArgs> OnNewMonth;

// Job pay events
public static event EventHandler<JobPayEventArgs> OnJobPay;
```

### Event Args Classes

#### AgeEventArgs
```csharp
public class AgeEventArgs : EventArgs
{
    public int Age { get; set; }
    public DateOnly Date { get; set; }
}
```

#### MonthlyEventArgs  
```csharp
public class MonthlyEventArgs : EventArgs
{
    public DateOnly Date { get; set; }
    public int Age { get; set; }
    public double MonthlyExpenses { get; set; }
}
```

#### JobPayEventArgs
```csharp
public class JobPayEventArgs : EventArgs
{
    public DateOnly Date { get; set; }
    public IncomeSource Job { get; set; }
    public double GrossIncome { get; set; }
    public double NetIncome { get; set; }
}
```

### Event Handler Examples

```csharp
// Set up comprehensive event monitoring
RetirementPlanner.OnCatchUpContributionsEligible += (sender, e) =>
{
    Console.WriteLine($"Age {e.Age}: Catch-up contributions now available!");
    
    // Show new limits
    int year = e.Date.Year;
    double new401kLimit = ContributionLimits.Get401kPersonalLimit(year, e.Age);
    double newIRALimit = ContributionLimits.GetIRALimit(year, e.Age);
    
    Console.WriteLine($"  401(k) limit: ${new401kLimit:N0}");
    Console.WriteLine($"  IRA limit: ${newIRALimit:N0}");
};

RetirementPlanner.OnRMDAgeHit += (sender, e) =>
{
    Console.WriteLine($"Age {e.Age}: Required Minimum Distributions begin!");
    
    // Calculate RMDs for all applicable accounts
    var person = (Person)sender;
    foreach (var account in person.Investments.Accounts)
    {
        if (RMDCalculator.IsSubjectToRMD(account.Type))
        {
            double balance = account.Balance(e.Date);
            double rmd = RMDCalculator.CalculateRMD(account, e.Age, balance);
            Console.WriteLine($"  {account.Name}: ${rmd:N0} required");
        }
    }
};

RetirementPlanner.OnJobPay += (sender, e) =>
{
    Console.WriteLine($"Pay event: ${e.GrossIncome:N0} from {e.Job.Title}");
    
    // Process 401(k) contributions
    var person = (Person)sender;
    ProcessPayrollContributions(person, e);
};
```

## Transaction System

### Transaction Class

Represents individual account transactions for tracking and analysis.

```csharp
public class Transaction
{
    public DateOnly Date { get; set; }
    public double Amount { get; set; }
    public TransactionCategory Category { get; set; }
    public string Description { get; set; }
}

public enum TransactionCategory
{
    ContributionPersonal,
    ContributionEmployer, 
    ContributionRollover,
    Income,
    Expenses,
    Growth,
    Fees,
    Taxes,
    Penalty
}
```

### Usage Example

```csharp
// Access transaction history
var account = new Traditional401kAccount(0.07, "401k", birthdate, 100000);

// Make a contribution
double contributed = account.Deposit(5000, DateOnly.FromDateTime(DateTime.Now), 
    TransactionCategory.ContributionPersonal);

// Review deposit history
foreach (var transaction in account.DepositHistory)
{
    Console.WriteLine($"{transaction.Date}: ${transaction.Amount:N0} - {transaction.Category}");
}
```

## Error Handling and Validation

The library includes comprehensive validation and error handling:

### Common Validation Scenarios

```csharp
// Age validation
if (person.CurrentAge(DateOnly.FromDateTime(DateTime.Now)) < 0)
    throw new ArgumentException("Birth date cannot be in the future");

// Contribution limit validation  
double maxContribution = ContributionLimits.Get401kPersonalLimit(year, age);
if (contributionAmount > maxContribution)
{
    Console.WriteLine($"Contribution limited to ${maxContribution:N0}");
    contributionAmount = maxContribution;
}

// Account balance validation
if (withdrawalAmount > account.Balance(date))
{
    Console.WriteLine($"Insufficient funds. Available: ${account.Balance(date):N0}");
    return 0; // No withdrawal processed
}
```

This API reference provides comprehensive documentation for implementing retirement planning functionality using the RetirementPlanner library. For practical usage examples, see the [Examples & Scenarios](./examples.md) documentation.
