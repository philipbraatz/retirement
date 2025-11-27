# RetirementPlanner

RetirementPlanner is a .NET 8 library and console application for modeling long-term retirement scenarios, account growth, withdrawals, and tax considerations.

## Features
- Multi-account simulation (Traditional/Roth 401(k), IRAs, HSA, Taxable, Savings)
- Contribution limit enforcement with catch-up rules
- Early withdrawal penalty logic with SECURE Act and SECURE 2.0 exceptions (partial)
- Social Security claiming age modeling and benefit taxation (IRC §86)
- Required Minimum Distribution (RMD) framework
- Roth conversion strategy helper
- Emergency fund sizing heuristics
- Graph generation of balances and income/expense streams
- Year-aware tax bracket + standard deduction loading from JSON

## Getting Started
1. Clone the repository
2. Open solution in Visual Studio 2022 or later
3. Run the `RetirementPlanner.Console` project to interact with profiles

## Console Application
Choose a pre-built profile (early retiree, normal retiree, late retiree, or custom) and run a simulation to age 110. The console prints annual summaries and generates graphs. A legal/tax disclaimer footer is printed at shutdown.

## Tax Data
Federal tax brackets and standard deductions are stored in `IRS/tax-data.json` and loaded by `TaxYearDataProvider`.

## Legal & Compliance Documentation
- [Tax Compliance Review](./TAX_COMPLIANCE_REVIEW.md) – regulatory analysis & gaps
- [Legal Disclaimer](./LEGAL_DISCLAIMER.md) – usage and liability (add or update as needed)

## Disclaimer (Summary)
This software provides educational estimates only. No guarantee of accuracy or suitability. Tax laws and financial conditions change; consult qualified professionals. Federal only – state/local taxes not included.

## Roadmap Highlights
- StandardDeductionService abstraction for all deduction lookups
- Full Roth IRA income phase-out logic everywhere
- SEPP 72(t) schedules & exception modeling
- AMT and capital gains treatment with holding-period tracking
- HSA last-month rule + testing period
- State tax modeling hooks & UI surfacing

## License
MIT (placeholder). Update to reflect actual intended license.
