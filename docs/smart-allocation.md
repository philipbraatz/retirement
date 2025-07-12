# Smart Retirement Contribution Allocation

## Overview

The RetirementPlanner now includes an intelligent allocation system that automatically optimizes retirement contributions between Traditional and Roth 401k accounts based on current tax brackets while ensuring maximum employer match benefits.

## How It Works

### 1. Employer Match Priority
The system always ensures that enough money is contributed to the Traditional 401k to receive the full employer match before allocating remaining contributions.

**Example**: If employer matches 3% and you contribute 10%, the first 3% automatically goes to Traditional 401k to secure the full match.

### 2. Tax-Optimized Allocation
After securing the employer match, remaining contributions are allocated based on current marginal tax rate:

- **Low Tax Bracket (≤12%)**: Remaining contributions go to Roth 401k for tax-free growth
- **High Tax Bracket (≥22%)**: Remaining contributions go to Traditional 401k for immediate tax deduction  
- **Medium Tax Bracket (12-22%)**: Split remaining contributions (60% Traditional, 40% Roth)

### 3. Configuration
Set your total retirement contribution percentage using `RetirementContributionPercent` in `IncomeSource`. The system handles the optimal allocation automatically.

```csharp
var job = new IncomeSource
{
    RetirementContributionPercent = 0.15, // 15% total contribution
    CompanyMatchContributionPercent = 0.03 // 3% employer match
};
```

## Implementation Details

### Key Methods
- `CalculateOptimalRetirementAllocation()` in `IncomeSource.cs`
- `GetMarginalTaxRate()` in `TaxBrackets.cs`
- Updated paycheck processing in `Events.cs`

### Account Requirements
- **Traditional401kAccount**: Required for employer match and traditional contributions
- **Roth401kAccount**: Optional, but recommended for optimal tax diversification

If a person doesn't have a Roth 401k account, all contributions will go to Traditional 401k with a notification.

## Benefits

1. **Automatic Optimization**: No manual calculation needed
2. **Employer Match Maximization**: Never miss out on free money
3. **Tax Diversification**: Builds both tax-deferred and tax-free retirement savings
4. **Dynamic Adjustment**: Allocation changes as income and tax brackets change over time

## Example Output

```
=> $660.00 Into Traditional 401k [ensuring employer match + tax optimization]
=> $101.54 Into Roth 401k [tax-free growth in low tax bracket] 
=> $198.00 Into Traditional 401k [employer match contribution]
```

This shows the smart allocation in action: securing employer match, optimizing for current tax situation, and maximizing long-term retirement savings.
