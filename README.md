# RetirementPlanner

A comprehensive C# library for retirement planning calculations, simulations, and strategy optimization.

## 📚 Documentation

Comprehensive documentation is available in the `/docs` folder:

- **[Documentation Overview](./docs/README.md)** - Start here for navigation
- **[Retirement Planning Overview](./docs/retirement-planning-overview.md)** - High-level concepts with detailed calculations and examples
- **[API Reference](./docs/api-reference.md)** - Complete developer documentation
- **[Examples & Scenarios](./docs/examples.md)** - Practical implementation examples

### Quick Links

| Topic | Description | Link |
|-------|-------------|------|
| Account Types | Detailed guide to 401(k), IRA, Roth, HSA accounts | [docs/account-types.md](./docs/account-types.md) |
| Social Security | Claiming strategies and optimization | [docs/social-security.md](./docs/social-security.md) |
| RMDs | Required Minimum Distribution rules and calculations | [docs/rmd.md](./docs/rmd.md) |
| Tax Planning | Tax-efficient withdrawal strategies | [docs/tax-planning.md](./docs/tax-planning.md) |
| Examples | Real-world scenarios and code samples | [docs/examples.md](./docs/examples.md) |

## 🚀 Quick Start

### Running the Console Application

```bash
# Build and run the retirement planning console
dotnet run --project RetirementPlanner.Console

# Or use VS Code
# Press F5 or use "Launch Retirement Console" configuration
```

### Basic Usage

```csharp
// Create a person
var person = new Person
{
    BirthDate = new DateTime(1975, 6, 15),
    FullRetirementAge = 67,
    SocialSecurityClaimingAge = 67,
    SocialSecurityIncome = 2400, // Monthly benefit at FRA
    EssentialExpenses = 70000,   // Annual essential expenses
    DiscretionarySpending = 20000
};

// Add retirement accounts
var traditional401k = new Traditional401kAccount(0.07, "401k", 
    DateOnly.FromDateTime(person.BirthDate), 350000);
var rothIRA = new RothIRAAccount(0.07, "Roth IRA", person, 100000);

person.Investments = new InvestmentManager([traditional401k, rothIRA]);

// Run simulation
var planner = new RetirementPlanner(person, new RetirementPlanner.Options
{
    StartDate = DateOnly.FromDateTime(DateTime.Now),
    EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(45)),
    ReportGranularity = TimeSpan.FromDays(365),
    TimeStep = TimeSpan.FromDays(30)
});

await planner.RunRetirementSimulation();
```

## 🎯 Features

- ✅ **Comprehensive Account Types**: 401(k), IRA, Roth, HSA, taxable accounts
- ✅ **Social Security Optimization**: Multiple claiming strategies and break-even analysis
- ✅ **Tax-Efficient Withdrawals**: Smart sequencing and bracket management
- ✅ **Required Minimum Distributions**: Automatic calculations and penalty tracking
- ✅ **Healthcare Cost Planning**: Medicare and long-term care projections
- ✅ **Catch-up Contributions**: Age-based contribution limit increases
- ✅ **Early Withdrawal Rules**: Penalties and exceptions (Rule of 55, SEPP, etc.)
- ✅ **Real-time Simulation**: Monthly time-step modeling with event notifications

## 🏗️ Project Structure

```
RetirementPlanner/
├── RetirementPlanner/          # Core library
├── RetirementPlanner.Console/  # Console application
├── RetirementPlanner.Test/     # Unit tests
├── docs/                       # Comprehensive documentation
└── .vscode/                    # VS Code configuration
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🔧 Development

### Prerequisites

- .NET 8.0 or later
- Visual Studio Code (recommended) or Visual Studio

### Building

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build RetirementPlanner.Console
```

### VS Code Integration

The project includes complete VS Code configuration:

- **Launch configurations** for debugging the console app
- **Tasks** for building and running
- **Settings** optimized for .NET development

## 📊 Key Calculations Supported

### Social Security
- Benefit calculation based on AIME and PIA formulas
- Claiming strategy optimization (ages 62-70)
- Break-even analysis between claiming strategies
- Spousal and survivor benefit calculations

### Account Management
- Growth projections with compound interest
- Contribution limit enforcement with catch-up provisions
- Tax-efficient withdrawal sequencing
- Early withdrawal penalty calculations with exceptions

### Tax Planning
- Marginal vs. effective tax rate analysis
- Roth conversion optimization
- Tax bracket management strategies
- Medicare IRMAA threshold planning

### Required Distributions
- RMD calculations using IRS life expectancy tables
- Penalty calculations for insufficient distributions
- Beneficiary planning for inherited accounts

## 📈 Example Scenarios

The documentation includes detailed examples for:

- **Early Retirement (FIRE)**: Strategies for retiring before traditional retirement age
- **Late-Career Catch-up**: Maximizing savings in final working years
- **Tax Diversification**: Balancing traditional, Roth, and taxable accounts
- **Healthcare Planning**: Using HSAs and planning for Medicare
- **Estate Planning**: Optimizing accounts for beneficiaries

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Update documentation
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🔗 Additional Resources

- **[IRS Publication 590-A](https://www.irs.gov/publications/p590a)** - Contributions to Individual Retirement Arrangements
- **[IRS Publication 590-B](https://www.irs.gov/publications/p590b)** - Distributions from Individual Retirement Arrangements
- **[Social Security Administration](https://www.ssa.gov/)** - Official Social Security information
- **[Medicare.gov](https://www.medicare.gov/)** - Official Medicare information

---

**Note**: This library is for educational and planning purposes. Consult with qualified financial and tax professionals for personalized advice.
