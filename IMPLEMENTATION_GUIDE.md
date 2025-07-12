# Retirement Planner Implementation Guide

## Overview
This document provides implementation guidance for using the enhanced retirement planning features based on the comprehensive retirement guide. The system now includes accurate age-based triggers, tax calculations, RMD handling, and penalty calculations.

## Key Enhancements Added

### 1. Age-Based Event System
The retirement planner now triggers events at all critical retirement ages:

```csharp
// New events added to RetirementPlanner class:
public static event EventHandler<DatedEventArgs>? OnCatchUpContributionsEligible;    // Age 50
public static event EventHandler<DatedEventArgs>? OnRuleOf55Eligible;                // Age 55
public static event EventHandler<DatedEventArgs>? OnEarlyWithdrawalPenaltyEnds;     // Age 59½
public static event EventHandler<DatedEventArgs>? OnSocialSecurityEarlyEligible;    // Age 62
public static event EventHandler<DatedEventArgs>? OnMedicareEligible;               // Age 65
public static event EventHandler<DatedEventArgs>? OnFullRetirementAge;              // Age 67 (varies by birth year)
public static event EventHandler<DatedEventArgs>? OnRMDAgeHit;                      // Age 73
```

### 2. Enhanced Account Types
Extended account type support:

```csharp
public enum AccountType
{
    Traditional401k,
    Roth401k,           // NEW
    Traditional403b,     // NEW
    Roth403b,           // NEW
    TraditionalIRA,
    RothIRA,
    SEPIRA,             // NEW
    SIMPLEIRA,          // NEW
    Savings,
    Taxable             // NEW
}
```

### 3. Required Minimum Distribution (RMD) Calculator
Accurate RMD calculations using IRS Uniform Lifetime Table:

```csharp
// Calculate RMD for any account
double rmdAmount = RMDCalculator.CalculateRMD(account, age, priorYearEndBalance, isStillWorking);

// Check if account is subject to RMDs
bool requiresRMD = RMDCalculator.IsSubjectToRMD(account.Type);

// Calculate penalty for missing RMD
double penalty = RMDCalculator.CalculateRMDPenalty(requiredAmount, actualWithdrawal);
```

### 4. Early Withdrawal Penalty Calculator
Comprehensive penalty calculation with exceptions:

```csharp
// Calculate early withdrawal penalty
double penalty = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(
    accountType: AccountType.Traditional401k,
    withdrawalAmount: 10000,
    age: 45,
    separationFromServiceAge: 55, // For Rule of 55
    withdrawalReason: WithdrawalReason.FirstTimeHomePurchase,
    rothContributionAmount: 5000 // For Roth accounts
);
```

### 5. Enhanced Social Security Taxation
Accurate provisional income calculation for all filing statuses:

```csharp
// Proper Social Security taxation calculation
private double GetTaxableSocialSecurity()
{
    double provisionalIncome = Person.IncomeYearly + (Person.SocialSecurityIncome * 0.5);
    
    return Person.FileType switch
    {
        FileType.Single => CalculateTaxableSocialSecuritySingle(provisionalIncome, Person.SocialSecurityIncome),
        FileType.MarriedFilingJointly => CalculateTaxableSocialSecurityMFJ(provisionalIncome, Person.SocialSecurityIncome),
        FileType.MarriedFilingSeparately => CalculateTaxableSocialSecurityMFS(provisionalIncome, Person.SocialSecurityIncome),
        _ => 0
    };
}
```

### 6. Updated Contribution Limits
Current and projected limits with enhanced catch-up contributions:

```csharp
// Get appropriate contribution limits
double limit401k = ContributionLimits.Get401kPersonalLimit(2024, age);
double limitIRA = ContributionLimits.GetIRALimit(2024, age);
double limitSEP = ContributionLimits.GetSEPLimit(2024, compensation);
```

### 7. Social Security Benefit Calculation by Claiming Age
Social Security benefits are calculated based on when you claim them relative to your Full Retirement Age (FRA):

```csharp
// Social Security benefit amounts based on claiming age
public class SocialSecurityBenefitCalculator
{
    public static double CalculateBenefitMultiplier(int claimingAge, int fullRetirementAge)
    {
        if (claimingAge < 62) return 0; // Cannot claim before 62
        
        if (claimingAge < fullRetirementAge)
        {
            // Early claiming reduction
            int monthsEarly = (fullRetirementAge - claimingAge) * 12;
            
            // First 36 months: 5/9 of 1% per month (6.67% per year)
            // Additional months: 5/12 of 1% per month (5% per year)
            double reduction = 0;
            if (monthsEarly <= 36)
            {
                reduction = monthsEarly * (5.0/9.0) / 100.0;
            }
            else
            {
                reduction = 36 * (5.0/9.0) / 100.0; // First 36 months
                reduction += (monthsEarly - 36) * (5.0/12.0) / 100.0; // Additional months
            }
            
            return 1.0 - reduction;
        }
        else if (claimingAge == fullRetirementAge)
        {
            return 1.0; // 100% of Primary Insurance Amount (PIA)
        }
        else
        {
            // Delayed retirement credits: 8% per year from FRA to age 70
            int yearsDelayed = Math.Min(claimingAge - fullRetirementAge, 70 - fullRetirementAge);
            return 1.0 + (yearsDelayed * 0.08);
        }
    }
    
    public static double CalculateMonthlyBenefit(double primaryInsuranceAmount, int claimingAge, int fullRetirementAge)
    {
        double multiplier = CalculateBenefitMultiplier(claimingAge, fullRetirementAge);
        return primaryInsuranceAmount * multiplier;
    }
}

// Example claiming scenarios for someone with $2,000 PIA and FRA of 67:
// Age 62: $1,400/month (70% of PIA) - Permanent 30% reduction
// Age 65: $1,733/month (86.7% of PIA) - 13.3% reduction  
// Age 67: $2,000/month (100% of PIA) - Full Retirement Age
// Age 70: $2,480/month (124% of PIA) - 24% increase from delayed credits
```

### 8. Medicare and Healthcare Cost Transitions
Healthcare costs change significantly at age 65 with Medicare eligibility:

```csharp
public class HealthcareCostCalculator
{
    public static double CalculateAnnualHealthcareCosts(int age, bool hasEmployerInsurance)
    {
        if (age < 65 && hasEmployerInsurance)
        {
            return 5000; // Typical employer plan employee contribution
        }
        else if (age < 65 && !hasEmployerInsurance)
        {
            return 15000; // ACA marketplace or COBRA premiums
        }
        else // Age 65+, Medicare eligible
        {
            double medicarePartB = 2000;    // Standard Part B premium
            double supplementalPlan = 3000; // Medigap or Medicare Advantage
            double partD = 600;             // Prescription drug coverage
            double outOfPocket = 2500;      // Deductibles and copays
            
            return medicarePartB + supplementalPlan + partD + outOfPocket; // ~$8,100/year
        }
    }
}
```

## Implementation Examples

### Setting Up a Complete Retirement Scenario

```csharp
// Create a person with birth date
var person = new Person
{
    BirthDate = new DateTime(1970, 6, 15),
    FullRetirementAge = 67,
    SocialSecurityClaimingAge = 67,
    FileType = FileType.Single
};

// Add various account types
var traditional401k = new Traditional401kAccount(0.07, "Company 401k", DateOnly.FromDateTime(person.BirthDate), 100000);
var roth401k = new Roth401kAccount(0.07, "Roth 401k", DateOnly.FromDateTime(person.BirthDate), 50000);
var rothIRA = new RothIRAAccount(0.07, "Roth IRA", person, 75000);

person.Investments = new InvestmentManager([traditional401k, roth401k, rothIRA]);

// Set up event handlers
RetirementPlanner.OnCatchUpContributionsEligible += (sender, e) => 
{
    Console.WriteLine($"Catch-up contributions now available at age {e.Age}!");
    // Increase contribution rates
};

RetirementPlanner.OnSocialSecurityEarlyEligible += (sender, e) => 
{
    Console.WriteLine($"Early Social Security claiming available at age {e.Age} with reduced benefits");
    // Calculate reduced benefit amount: ~70% of PIA at age 62
    double reducedBenefit = person.CalculateCurrentSocialSecurityBenefits(e.Date.ToDateTime(TimeOnly.MinValue));
    double multiplier = SocialSecurityBenefitCalculator.CalculateBenefitMultiplier(e.Age, person.FullRetirementAge);
    Console.WriteLine($"Monthly benefit if claimed now: ${reducedBenefit * multiplier:F0}");
};

RetirementPlanner.OnFullRetirementAge += (sender, e) => 
{
    Console.WriteLine($"Full Retirement Age reached at {e.Age}!");
    Console.WriteLine($"100% Social Security benefits now available");
    Console.WriteLine($"Delayed retirement credits of 8% per year available until age 70");
    // Calculate full benefit amount and potential delayed credits
};

RetirementPlanner.OnRMDAgeHit += (sender, e) => 
{
    Console.WriteLine($"RMDs now required at age {e.Age}!");
    // Calculate and process RMDs
};

// Run simulation
var planner = new RetirementPlanner(person);
await planner.RunRetirementSimulation();
```

### Handling RMDs During Simulation

```csharp
// In your monthly processing logic
foreach (var account in person.Investments.Accounts)
{
    if (RMDCalculator.IsSubjectToRMD(account.Type))
    {
        if (account is Traditional401kAccount t401k)
        {
            double rmdRequired = t401k.RequiredMinimalDistributions(currentDate);
            if (rmdRequired > 0)
            {
                double withdrawn = t401k.Withdraw(rmdRequired, currentDate, TransactionCategory.Income);
                // Process tax implications of RMD withdrawal
            }
        }
    }
}
```

### Processing Age-Based Contribution Limit Changes

```csharp
// Update contribution limits when catch-up eligibility is triggered
RetirementPlanner.OnCatchUpContributionsEligible += (sender, e) => 
{
    foreach (var job in person.Jobs)
    {
        // Increase 401k contribution rate to take advantage of catch-up
        if (job.Type == JobType.FullTime)
        {
            double newLimit = ContributionLimits.Get401kPersonalLimit(currentDate.Year, age);
            // Adjust contribution strategy
        }
    }
};
```

### Social Security Claiming Strategy Analysis

```csharp
// Example: Analyze optimal Social Security claiming strategy
private void AnalyzeSSClaimingStrategy(Person person, DateOnly currentDate)
{
    int currentAge = currentDate.Year - person.BirthDate.Year;
    double primaryInsuranceAmount = person.CalculateCurrentSocialSecurityBenefits(currentDate.ToDateTime(TimeOnly.MinValue));
    
    Console.WriteLine("Social Security Claiming Analysis:");
    Console.WriteLine("=================================");
    
    // Show benefit amounts for different claiming ages
    for (int claimAge = 62; claimAge <= 70; claimAge++)
    {
        double multiplier = SocialSecurityBenefitCalculator.CalculateBenefitMultiplier(claimAge, person.FullRetirementAge);
        double monthlyBenefit = primaryInsuranceAmount * multiplier;
        double annualBenefit = monthlyBenefit * 12;
        
        string description = claimAge switch
        {
            62 => "Early claiming (permanent reduction)",
            var age when age == person.FullRetirementAge => "Full Retirement Age (100% benefit)",
            70 => "Maximum benefit with delayed credits",
            _ when claimAge < person.FullRetirementAge => "Reduced benefit",
            _ => "Delayed retirement credits"
        };
        
        Console.WriteLine($"Age {claimAge}: ${monthlyBenefit:F0}/month (${annualBenefit:F0}/year) - {description}");
    }
    
    // Calculate break-even analysis
    if (currentAge < person.FullRetirementAge)
    {
        CalculateBreakEvenAge(primaryInsuranceAmount, person.FullRetirementAge);
    }
}

private void CalculateBreakEvenAge(double pia, int fullRetirementAge)
{
    double earlyBenefit = pia * SocialSecurityBenefitCalculator.CalculateBenefitMultiplier(62, fullRetirementAge);
    double fullBenefit = pia * SocialSecurityBenefitCalculator.CalculateBenefitMultiplier(fullRetirementAge, fullRetirementAge);
    double delayedBenefit = pia * SocialSecurityBenefitCalculator.CalculateBenefitMultiplier(70, fullRetirementAge);
    
    // Break-even between claiming at 62 vs FRA
    int monthsToBreakEven = (int)((fullBenefit - earlyBenefit) * 12 * (fullRetirementAge - 62) / (fullBenefit - earlyBenefit));
    int breakEvenAge = fullRetirementAge + (monthsToBreakEven / 12);
    
    Console.WriteLine($"\nBreak-even Analysis:");
    Console.WriteLine($"Claiming at 62 vs {fullRetirementAge}: Break-even at age {breakEvenAge}");
    
    // Similar calculation for FRA vs 70
    monthsToBreakEven = (int)((delayedBenefit - fullBenefit) * 12 * (70 - fullRetirementAge) / (delayedBenefit - fullBenefit));
    breakEvenAge = 70 + (monthsToBreakEven / 12);
    Console.WriteLine($"Claiming at {fullRetirementAge} vs 70: Break-even at age {breakEvenAge}");
}
```

## Best Practices

### 1. Event-Driven Architecture
Use the age-based events to trigger appropriate actions:

```csharp
// Subscribe to all relevant events
RetirementPlanner.OnEarlyWithdrawalPenaltyEnds += HandlePenaltyEnd;
RetirementPlanner.OnSocialSecurityEarlyEligible += HandleSSEarlyEligibility;
RetirementPlanner.OnMedicareEligible += HandleMedicareTransition;
```

### 2. Account-Specific Logic
Different account types have different rules:

```csharp
private void ProcessWithdrawal(InvestmentAccount account, double amount, DateOnly date)
{
    switch (account.Type)
    {
        case AccountType.RothIRA:
            // Contributions can be withdrawn penalty-free
            break;
        case AccountType.Traditional401k:
            // Subject to RMDs at 73, Rule of 55 applies
            break;
        case AccountType.Roth401k:
            // Subject to RMDs unlike Roth IRA
            break;
    }
}
```

### 3. Tax Optimization
Consider tax implications at each stage:

```csharp
private void OptimizeWithdrawalOrder(Person person, double amountNeeded, DateOnly date)
{
    // 1. Taxable accounts first (no penalties)
    // 2. Roth contributions (no tax or penalties)
    // 3. Traditional accounts if over 59½
    // 4. Consider penalties vs. tax brackets
}
```

### 4. Compliance Monitoring
Stay current with tax law changes:

```csharp
// Update contribution limits annually
public static void UpdateLimitsForYear(int year)
{
    // Check IRS publications for updated limits
    // Update ContributionLimits constants
    // Update RMD life expectancy tables if changed
}
```

## Testing Your Implementation

### Unit Tests Example

```csharp
[Test]
public void RMD_CalculatedCorrectly_At73()
{
    var account = new Traditional401kAccount(0.07, "Test", new DateOnly(1950, 1, 1), 100000);
    double rmd = account.RequiredMinimalDistributions(new DateOnly(2023, 1, 1));
    
    // Age 73, life expectancy factor 26.5
    Assert.AreEqual(100000 / 26.5, rmd, 0.01);
}

[Test]
public void EarlyWithdrawal_PenaltyApplied_Before59Half()
{
    double penalty = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(
        AccountType.Traditional401k, 10000, 45);
    
    Assert.AreEqual(1000, penalty); // 10% penalty
}
```

This enhanced system provides a comprehensive foundation for accurate retirement planning calculations that comply with current tax regulations and IRS requirements.
