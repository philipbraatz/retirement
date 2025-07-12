# Contribution Limits & Rules

## Overview

Retirement account contribution limits are set annually by the IRS and are adjusted for inflation. Understanding these limits and the associated rules is crucial for maximizing retirement savings and avoiding penalties.

## 2025 Contribution Limits

### 401(k) Plans

**Employee Contributions:**
- **Standard Limit:** $23,500
- **Age 50+ Catch-up:** Additional $7,500
- **Total for 50+:** $31,000

**Total Annual Additions (Employee + Employer):**
- **Standard Limit:** $70,000
- **Age 50+ Limit:** $77,500

**Highly Compensated Employee (HCE) Considerations:**
- HCE threshold (2025): $155,000
- May face additional testing limits
- Actual Deferral Percentage (ADP) testing may reduce contributions

**Example:**
```
Employee age 55, salary $150,000:
Maximum employee contribution: $31,000 (20.7% of salary)
Employer match (6%): $9,000
Total contributions: $40,000
```

### Traditional and Roth IRAs

**Contribution Limits:**
- **Standard Limit:** $7,000
- **Age 50+ Catch-up:** Additional $1,000
- **Total for 50+:** $8,000

**Income Limits for Roth IRA (2025):**

**Single Filers:**
- Full contribution: AGI up to $138,000
- Phase-out range: $138,000 - $153,000
- No contribution: AGI above $153,000

**Married Filing Jointly:**
- Full contribution: AGI up to $218,000
- Phase-out range: $218,000 - $228,000
- No contribution: AGI above $228,000

**Phase-out Calculation Example:**
```
Single filer, AGI: $145,000
Phase-out calculation:
($145,000 - $138,000) ÷ ($153,000 - $138,000) = $7,000 ÷ $15,000 = 46.7%
Reduced contribution: $7,000 × (1 - 0.467) = $3,731
```

**Traditional IRA Deduction Limits (2025):**

**With Workplace Retirement Plan:**
- Single: Phase-out $77,000 - $87,000
- Married Filing Jointly: Phase-out $123,000 - $143,000

**Without Workplace Plan:**
- Single: No income limit
- Married (spouse has plan): Phase-out $230,000 - $240,000

### Health Savings Accounts (HSA)

**2025 Contribution Limits:**
- **Individual Coverage:** $4,150
- **Family Coverage:** $8,300
- **Age 55+ Catch-up:** Additional $1,000

**High Deductible Health Plan (HDHP) Requirements:**
- Minimum deductible (2025): $1,650 (individual), $3,300 (family)
- Maximum out-of-pocket: $8,300 (individual), $16,600 (family)

**Triple Tax Advantage:**
```
HSA Contribution Example (Family, age 57):
Annual contribution: $9,300 ($8,300 + $1,000 catch-up)
Tax deduction: $9,300 (reduces current taxable income)
Growth: Tax-free on investments
Withdrawals: Tax-free for qualified medical expenses
```

### SEP-IRA

**2025 Contribution Limits:**
- **Maximum:** Lesser of 25% of compensation or $70,000
- **Minimum compensation:** $750
- **Maximum compensation:** $345,000

**Example:**
```
Self-employed income: $100,000
Maximum SEP-IRA contribution: $25,000 (25% of $100,000)
Tax deduction: $25,000
```

### SIMPLE IRA

**2025 Contribution Limits:**
- **Employee contribution:** $16,000
- **Age 50+ catch-up:** Additional $3,500
- **Total for 50+:** $19,500

**Employer Contributions:**
- **Matching:** Up to 3% of compensation
- **Non-elective:** 2% of compensation for all eligible employees

## Catch-Up Contribution Rules

### Eligibility

**Age Requirement:**
- Must turn 50 by December 31 of the contribution year
- No partial year proration

**Implementation:**
```csharp
public static double GetContributionLimit(int year, int age, AccountType accountType)
{
    double baseLimit = GetBaseLimit(year, accountType);
    double catchUpAmount = GetCatchUpAmount(year, accountType);
    
    return age >= 50 ? baseLimit + catchUpAmount : baseLimit;
}

// Example usage
int age = 52;
double limit401k = GetContributionLimit(2025, age, AccountType.Traditional401k); // $31,000
double limitIRA = GetContributionLimit(2025, age, AccountType.TraditionalIRA);   // $8,000
```

### Catch-Up Contribution Strategy

**Maximizing Catch-Up Benefits:**
```
Ages 50-59 additional annual capacity:
401(k): $7,500
IRA: $1,000
HSA: $1,000 (age 55+)
Total additional: $9,500-$10,500

10-year catch-up period total: $95,000-$105,000
With 7% growth: $131,000-$145,000 future value
```

## Special Rules and Considerations

### Compensation Limits

**2025 Compensation Limits:**
- **401(k), 403(b), 457:** $345,000
- **Social Security wage base:** $168,600
- **Highly compensated employee:** $155,000

### Timing Rules

**Contribution Deadlines:**

**401(k) Plans:**
- Must be contributed by December 31 of the tax year
- Payroll deduction deadline varies by employer

**IRAs:**
- Deadline: April 15 of following year (same as tax filing deadline)
- No extensions allowed (even with tax return extensions)

**HSAs:**
- Deadline: April 15 of following year
- Contributions through employer payroll must be by December 31

### Rollover Rules

**60-Day Rollover Rule:**
- Must complete rollover within 60 days
- Once per 12-month period per IRA
- No limit on trustee-to-trustee transfers

**Direct Rollover:**
- No time limit
- No withholding requirements
- Unlimited frequency

**Example Rollover Strategy:**
```
401(k) balance at job change: $200,000
Options:
1. Direct rollover to new employer's 401(k)
2. Direct rollover to Traditional IRA
3. 60-day rollover (not recommended due to withholding)

Recommended: Direct rollover to maintain tax-deferred status
```

## Contribution Strategies by Life Stage

### Early Career (Ages 22-35)

**Priorities:**
1. Employer match (free money)
2. High-deductible health plan with HSA
3. Roth contributions (low tax bracket)

**Example Strategy:**
```
Age 28, salary $60,000:
1. 401(k) contribution: 6% ($3,600) to get full employer match
2. HSA contribution: $4,150 (max individual)
3. Roth IRA: $7,000 (max)
Total savings: $14,750 (24.6% of gross income)
```

### Mid-Career (Ages 35-50)

**Priorities:**
1. Maximize employer matches
2. Balance traditional vs. Roth contributions
3. Begin aggressive savings rate

**Example Strategy:**
```
Age 42, salary $120,000:
1. 401(k) contribution: 15% ($18,000)
2. Employer match: 6% ($7,200)
3. HSA contribution: $8,300 (family coverage)
4. Backdoor Roth IRA: $7,000
Total contributions: $40,500 (33.8% of gross income)
```

### Pre-Retirement (Ages 50-65)

**Priorities:**
1. Maximize all catch-up contributions
2. Tax-loss harvesting in taxable accounts
3. Roth conversion planning

**Example Strategy:**
```
Age 55, salary $150,000:
1. 401(k) contribution: $31,000 (max with catch-up)
2. Employer match: $9,000 (6%)
3. HSA contribution: $9,300 (family + catch-up)
4. Backdoor Roth IRA: $8,000 (with catch-up)
Total contributions: $57,300 (38.2% of gross income)
```

## Advanced Contribution Strategies

### Mega Backdoor Roth

**Requirements:**
- 401(k) plan allows after-tax contributions
- Plan allows in-service distributions or conversions

**Process:**
```
2025 limits:
Total 401(k) limit: $70,000
Employee + employer contributions: $31,000 + $9,000 = $40,000
Available for after-tax: $30,000

Steps:
1. Contribute $30,000 after-tax to 401(k)
2. Immediately convert to Roth 401(k) or roll to Roth IRA
3. Pay taxes only on growth since contribution
```

### Tax Bracket Arbitrage

**Strategy:**
Optimize traditional vs. Roth contributions based on current and expected future tax brackets.

**Decision Matrix:**
```
Current Tax Bracket | Expected Retirement Bracket | Recommendation
10%-12%            | Any                         | Roth preferred
22%                | 10%-12%                     | Traditional preferred
22%                | 22%+                        | Roth preferred
24%+               | Lower                       | Traditional preferred
```

### HSA as Retirement Account

**Triple Tax Advantage Strategy:**
```
HSA contribution strategy:
1. Contribute maximum annually
2. Pay medical expenses out-of-pocket
3. Invest HSA funds aggressively
4. Save receipts for future reimbursement
5. After age 65: Use for any purpose (taxed as ordinary income)

20-year example:
Annual contribution: $4,150
Total contributions: $83,000
With 7% growth: $170,000
Plus saved medical receipts: $50,000
Effective tax-free retirement fund: $220,000
```

## Compliance and Penalties

### Excess Contribution Penalties

**Traditional and Roth IRAs:**
- **Penalty:** 6% annually until corrected
- **Correction:** Remove excess plus earnings by tax deadline

**401(k) Plans:**
- **Penalty:** 10% on excess amounts
- **Correction:** Employer must distribute excess by April 15

### Late Contribution Corrections

**Example Excess Contribution:**
```
Roth IRA contribution: $8,000 (excess: $1,000)
6% penalty annually until corrected
Year 1 penalty: $60
Year 2 penalty: $60 (if not corrected)

Correction process:
1. Calculate earnings on excess
2. Withdraw excess plus earnings
3. Report earnings as taxable income
4. Pay 10% penalty on earnings if under 59½
```

### Record Keeping Requirements

**Documentation to Maintain:**
- Annual contribution receipts
- Form 5498 (IRA contributions)
- Form 1099-R (distributions)
- Basis tracking for non-deductible IRA contributions
- HSA contribution and distribution records

Understanding and maximizing retirement account contributions within legal limits is fundamental to building wealth for retirement. Regular review and adjustment of contribution strategies ensures optimal tax efficiency and retirement readiness.
