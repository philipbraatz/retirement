using RetirementPlanner;
using RetirementPlanner.IRS;
using RetirementPlanner.Event;

namespace RetirementPlanner.ConsoleApp;

/// <summary>
/// Console application demonstrating comprehensive retirement planning scenarios
/// </summary>
public class RetirementPlannerConsole
{
    public static async Task RunComprehensiveSimulation()
    {
        Console.WriteLine("=== Comprehensive Retirement Planning Simulation ===\n");
        
        // Create a realistic retirement scenario
        var person = CreateExampleScenarioPerson();
        var planner = new RetirementPlanner(person, new RetirementPlanner.Options
        {
            StartDate = DateOnly.FromDateTime(DateTime.Now),
            EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(50)),
            ReportGranularity = TimeSpan.FromDays(365), // Annual reports
            TimeStep = TimeSpan.FromDays(30) // Monthly simulation steps
        });
        
        // DO NOT subscribe to LifeEvents to avoid duplicate event handling
        // Set up ONLY comprehensive event handlers
        SetupComprehensiveEventHandlers(person);
        
        Console.WriteLine($"Starting simulation for {person.BirthDate:yyyy-MM-dd}");
        Console.WriteLine($"Current age: {person.CurrentAge(DateOnly.FromDateTime(DateTime.Now))}");
        Console.WriteLine($"Full Retirement Age: {person.FullRetirementAge}");
        Console.WriteLine($"Social Security Claiming Age: {person.SocialSecurityClaimingAge}");
        Console.WriteLine();
        
        // Show initial Social Security analysis
        AnalyzeSocialSecurityStrategies(person);
        
        // Show initial account balances
        ShowAccountBalances(person, DateOnly.FromDateTime(DateTime.Now));
        
        // Run the simulation
        await planner.RunRetirementSimulation();
        
        Console.WriteLine("\n=== Simulation Complete ===");
    }
    
    private static Person CreateExampleScenarioPerson()
    {
        // Create person born in 1975 (currently 49 years old) - this is a scenario person for demonstration
        var person = new Person
        {
            BirthDate = new DateTime(1975, 3, 15),
            FullRetirementAge = SocialSecurityBenefitCalculator.GetFullRetirementAge(1975), // 67
            SocialSecurityClaimingAge = 67, // Claim at FRA
            FileType = TaxBrackets.FileType.Single,
            SocialSecurityIncome = 2400, // Monthly SS benefit at FRA
            EssentialExpenses = 60000, // Annual essential expenses
            DiscretionarySpending = 20000, // Annual discretionary spending
            PartTimeAge = 55, // Plan to go part-time at 55
            
            // Emergency Fund Configuration
            AutoCalculateEmergencyFunds = true,
            PreRetirementEmergencyMonths = 6, // 6 months of expenses while working
            EarlyRetirementEmergencyMonths = 24, // 24 months for early retirement safety
            PostRetirementEmergencyMonths = 12 // 12 months after 59.5
        };
        
        // Create diverse investment accounts with realistic balances for a 49-year-old
        var traditional401k = new Traditional401kAccount(
            annualGrowthRate: 0.07, 
            name: "Traditional 401k", 
            birthdate: DateOnly.FromDateTime(person.BirthDate), 
            startingBalance: 450000);
            
        var roth401k = new Roth401kAccount(
            annualGrowthRate: 0.07, 
            name: "Roth 401k", 
            birthdate: DateOnly.FromDateTime(person.BirthDate), 
            startingBalance: 200000);
            
        var rothIRA = new RothIRAAccount(
            annualGrowthRate: 0.07, 
            name: "Roth IRA", 
            person: person, 
            startingBalance: 150000);
            
        var savingsAccount = new InvestmentAccount(
            annualGrowthRate: 0.02, 
            name: "Savings", 
            startingBalance: 50000, // Start with adequate emergency fund
            AccountType.Savings);
        
        person.Investments = new InvestmentManager([traditional401k, roth401k, rothIRA, savingsAccount]);
        
        // Add employment income for someone still working
        person.Jobs = new List<IncomeSource>
        {
            new IncomeSource
            {
                Title = "Software Engineer",
                Type = JobType.FullTime,
                Salary = 120000,
                PaymentType = PaymentType.Salaried,
                RetirementContributionPercent = 15, // 15% contribution
                CompanyMatchContributionPercent = 5, // 5% company match
                HoursWorkedWeekly = 40,
                PayFrequency = PayFrequency.Monthly,
                StartDate = DateOnly.FromDateTime(DateTime.Now)
            }
        };
        
        // Add pocket cash buffer account (no growth)
        if (!person.Investments.Accounts.Any(a => a.Name == "Pocket Cash"))
            person.Investments.Accounts.Add(new InvestmentAccount(0.0, "Pocket Cash", 0, AccountType.Savings));
        
        return person;
    }
    
    private static void SetupComprehensiveEventHandlers(Person person)
    {
        // Monthly spending and income events
        RetirementPlanner.OnNewMonth += (sender, e) =>
        {
            ProcessMonthlyFinancialEvents(person, e.Date, e.Age);
        };
        
        // Job pay events
        RetirementPlanner.OnJobPay += (sender, e) =>
        {
            ProcessJobPayEvent(person, e);
        };
        
        // Birthday events for milestone tracking
        RetirementPlanner.Birthday += (sender, e) =>
        {
            Console.WriteLine($"🎂 Age {e.Age} ({e.Date})");
            
            // Add key milestone notifications
            if (e.Age == 50)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"🎉 AGE {e.Age}: Catch-up contributions now available!");
                Console.ResetColor();
            }
            else if (e.Age == 59.5)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"🎯 AGE {e.Age}: Early withdrawal penalties eliminated!");
                Console.ResetColor();
            }
            else if (e.Age == 62)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"💰 AGE {e.Age}: Social Security early claiming available!");
                Console.ResetColor();
            }
            else if (e.Age == person.FullRetirementAge)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"🎊 AGE {e.Age}: Full Retirement Age reached!");
                Console.ResetColor();
            }
            else if (e.Age == 73)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"📊 AGE {e.Age}: Required Minimum Distributions begin!");
                Console.ResetColor();
            }
        };
    }
    
    /// <summary>
    /// Comprehensive monthly financial event processing including growth, income, expenses, taxes, and RMDs
    /// </summary>
    private static void ProcessMonthlyFinancialEvents(Person person, DateOnly date, int age)
    {
        // 1. Apply monthly investment growth to all accounts
        foreach (var account in person.Investments.Accounts)
        {
            account.ApplyMonthlyGrowth(date);
        }
        
        // 2. Perform annual Roth conversions on January if beneficial
        if (date.Month == 1 && age < 73) // Don't convert after RMDs start
        {
            person.Investments.PerformRothConversion(person, date);
        }
        
        // 3. Process Social Security income if eligible and claiming
        double monthlySocialSecurityIncome = 0;
        if (age >= person.SocialSecurityClaimingAge && age >= 62)
        {
            double multiplier = SocialSecurityBenefitCalculator.CalculateBenefitMultiplier(
                person.SocialSecurityClaimingAge, person.FullRetirementAge);
            monthlySocialSecurityIncome = person.SocialSecurityIncome * multiplier;
        }
        
        // 4. Calculate total monthly expenses including healthcare
        double baseMonthlyExpenses = (person.EssentialExpenses + person.DiscretionarySpending) / 12;
        double totalMonthlyExpenses = baseMonthlyExpenses; // Simplified for now
        
        // 5. Process Required Minimum Distributions if applicable
        double monthlyRMDIncome = ProcessRequiredMinimumDistributions(person, date, age);
        
        // 6. Calculate net income vs expenses
        double totalMonthlyIncome = monthlySocialSecurityIncome + monthlyRMDIncome;
        double monthlyShortfall = totalMonthlyExpenses - totalMonthlyIncome;
        
        // 7. If there's a shortfall, implement withdrawal strategy with emergency fund protection
        if (monthlyShortfall > 0)
        {
            ExecuteOptimalWithdrawalStrategyWithEmergencyProtection(person, monthlyShortfall, date, age);
        }
        
        // 8. Print monthly summary (show every 3 months to provide regular updates)
        if (date.Month % 3 == 1) // January, April, July, October
        {
            PrintMonthlyAccountSummary(person, date);
        }
    }
    
    /// <summary>
    /// Comprehensive job pay event processing with optimal contribution strategies and emergency fund boosting
    /// </summary>
    private static void ProcessJobPayEvent(Person person, JobPayEventArgs e)
    {
        int age = person.CurrentAge(e.Date);
        
        // Calculate optimal retirement contribution allocation using the sophisticated logic
        var (traditionalAmount, rothAmount) = e.Job.CalculateOptimalRetirementAllocation(person, e.Date);
        double monthlyContribution = traditionalAmount + rothAmount;
        double companyMatch = e.GrossIncome * ((e.Job.CompanyMatchContributionPercent ?? 0) / 100);
        
        // Find both Traditional and Roth 401k accounts
        var traditional401k = person.Investments.Accounts.FirstOrDefault(a => a is Traditional401kAccount);
        var roth401k = person.Investments.Accounts.FirstOrDefault(a => a is Roth401kAccount);
        
        // Allocate contributions based on optimal strategy
        if (traditionalAmount > 0 && traditional401k != null)
        {
            double actualTraditionalDeposit = traditional401k.Deposit(traditionalAmount, e.Date, TransactionCategory.ContributionPersonal);
            double actualMatchDeposit = traditional401k.Deposit(companyMatch, e.Date, TransactionCategory.ContributionEmployer);
            
            if (actualTraditionalDeposit > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  💰 Traditional 401k: {actualTraditionalDeposit:C} personal + {actualMatchDeposit:C} match");
                Console.ResetColor();
            }
        }
        
        if (rothAmount > 0 && roth401k != null)
        {
            double actualRothDeposit = roth401k.Deposit(rothAmount, e.Date, TransactionCategory.ContributionPersonal);
            
            if (actualRothDeposit > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  🌟 Roth 401k: {actualRothDeposit:C} (tax-free growth!)");
                Console.ResetColor();
            }
        }
        
        // Show allocation strategy reasoning
        if (monthlyContribution > 0)
        {
            double traditionalPercent = traditionalAmount / monthlyContribution * 100;
            double rothPercent = rothAmount / monthlyContribution * 100;
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  📊 Allocation Strategy: {traditionalPercent:F0}% Traditional, {rothPercent:F0}% Roth (Age: {age})");
            Console.ResetColor();
        }
        
        // Calculate net income after contributions and taxes
        var savingsAccount = person.Investments.Accounts.FirstOrDefault(a => a.Type == AccountType.Savings);
        if (savingsAccount != null)
        {
            // Simplified net income calculation
            double netIncome = e.GrossIncome - monthlyContribution - (e.GrossIncome * 0.25); // Rough tax estimate
            
            if (netIncome > 0)
            {
                // Check emergency fund status and prioritize replenishment
                double emergencyFundShortfall = person.GetEmergencyFundShortfall(e.Date);
                
                if (emergencyFundShortfall > 0)
                {
                    // Emergency fund is low - prioritize replenishment
                    double emergencyFundBoost = Math.Min(netIncome, emergencyFundShortfall);
                    savingsAccount.Deposit(emergencyFundBoost, e.Date, TransactionCategory.Income);
                    netIncome -= emergencyFundBoost;
                    
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"💧 Emergency Fund Boost: ${emergencyFundBoost:C} → Savings");
                    Console.ResetColor();
                }
                
                // Deposit remaining net income
                if (netIncome > 0)
                {
                    savingsAccount.Deposit(netIncome, e.Date, TransactionCategory.Income);
                }
            }
        }
    }
    
    /// <summary>
    /// Process Required Minimum Distributions for all applicable accounts
    /// </summary>
    private static double ProcessRequiredMinimumDistributions(Person person, DateOnly date, int age)
    {
        double totalRMDIncome = 0;
        
        if (age >= 73) // RMDs start at age 73
        {
            foreach (var account in person.Investments.Accounts)
            {
                if (RMDCalculator.IsSubjectToRMD(account.Type))
                {
                    double priorYearBalance = account.Balance(new DateOnly(date.Year - 1, 12, 31));
                    double requiredRMD = RMDCalculator.CalculateRMD(account, age, priorYearBalance);
                    
                    if (requiredRMD > 0)
                    {
                        // Distribute RMD over 12 months
                        double monthlyRMD = requiredRMD / 12;
                        double actualWithdrawn = account.Withdraw(monthlyRMD, date, TransactionCategory.Income);
                        totalRMDIncome += actualWithdrawn;
                    }
                }
            }
        }
        
        return totalRMDIncome;
    }
    
    /// <summary>
    /// Execute optimal withdrawal strategy considering taxes, penalties, and account types
    /// </summary>
    private static void ExecuteOptimalWithdrawalStrategy(Person person, double amountNeeded, DateOnly date, int age)
    {
        double remainingNeeded = amountNeeded;
        var withdrawalOrder = LifeEvents.GetOptimalWithdrawalOrder(age);
        
        foreach (var accountType in withdrawalOrder)
        {
            if (remainingNeeded <= 0) break;
            
            var accounts = person.Investments.Accounts.Where(a => a.Type == accountType && a.Balance(date) > 0);
            foreach (var account in accounts)
            {
                if (remainingNeeded <= 0) break;
                
                double actualWithdrawn = account.Withdraw(remainingNeeded, date, TransactionCategory.Expenses);
                remainingNeeded -= actualWithdrawn;
            }
        }
        
        if (remainingNeeded > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"⚠️ SHORTFALL: ${remainingNeeded:C} could not be withdrawn ({date})");
            Console.ResetColor();
        }
    }
    
    /// <summary>
    /// Execute optimal withdrawal strategy with emergency fund protection
    /// </summary>
    private static void ExecuteOptimalWithdrawalStrategyWithEmergencyProtection(Person person, double amountNeeded, DateOnly date, int age)
    {
        double remainingNeeded = amountNeeded;
        var withdrawalOrder = LifeEvents.GetOptimalWithdrawalOrder(age);
        
        foreach (var accountType in withdrawalOrder)
        {
            if (remainingNeeded <= 0) break;
            
            var accounts = person.Investments.Accounts.Where(a => a.Type == accountType && a.Balance(date) > 0);
            foreach (var account in accounts)
            {
                if (remainingNeeded <= 0) break;
                
                double availableForWithdrawal;
                
                // Check if this is an emergency fund account and protect the minimum
                if (accountType == AccountType.Savings || accountType == AccountType.Taxable)
                {
                    availableForWithdrawal = person.GetAvailableForWithdrawal(date, accountType);
                }
                else
                {
                    availableForWithdrawal = account.Balance(date);
                }
                
                double toWithdraw = Math.Min(remainingNeeded, availableForWithdrawal);
                if (toWithdraw > 0)
                {
                    double actualWithdrawn = account.Withdraw(toWithdraw, date, TransactionCategory.Expenses);
                    remainingNeeded -= actualWithdrawn;
                }
            }
        }
        
        if (remainingNeeded > 0)
        {
            // Check if emergency fund protection is the cause
            if (person.IsEmergencyFundLow(date))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"⚠️ EMERGENCY FUND PROTECTED: ${person.GetCurrentEmergencyFundBalance(date):C} preserved, ${remainingNeeded:C} shortfall");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"⚠️ SHORTFALL: ${remainingNeeded:C} could not be withdrawn ({date})");
                Console.ResetColor();
            }
        }
    }
    
    private static void PrintMonthlyAccountSummary(Person person, DateOnly date)
    {
        Console.WriteLine($"\n📅 MONTHLY SUMMARY - {date:yyyy-MM} (Age {person.CurrentAge(date)})");
        Console.WriteLine("═══════════════════════════════════════════════════");
        
        double totalBalance = 0;
        
        Console.WriteLine("ACCOUNT BALANCES:");
        foreach (var account in person.Investments.Accounts.OrderBy(a => a.Name))
        {
            double balance = account.Balance(date);
            totalBalance += balance;
            
            // Show monthly activity if any
            double monthlyDeposits = account.DepositHistory
                .Where(d => d.Date.Year == date.Year && d.Date.Month == date.Month)
                .Sum(d => d.Amount);
            
            double monthlyWithdrawals = account.WithdrawalHistory
                .Where(w => w.Date.Year == date.Year && w.Date.Month == date.Month)
                .Sum(w => w.Amount);
            
            string activity = "";
            if (monthlyDeposits > 0 || monthlyWithdrawals > 0)
            {
                activity = $" (+{monthlyDeposits:C} -{monthlyWithdrawals:C})";
            }
            
            Console.WriteLine($"  {account.Name,-20} {balance,15:C}{activity}");
        }
        
        Console.WriteLine($"  {"TOTAL",-20} {totalBalance,15:C}");
        
        // Show Emergency Fund Status
        double currentEmergencyFund = person.GetCurrentEmergencyFundBalance(date);
        double requiredEmergencyFund = person.GetRequiredEmergencyFundMinimum(date);
        double emergencyFundShortfall = person.GetEmergencyFundShortfall(date);
        
        Console.WriteLine("\nEMERGENCY FUND STATUS:");
        if (emergencyFundShortfall > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Current Emergency Fund:  {currentEmergencyFund,12:C} ⚠️");
            Console.WriteLine($"  Required Minimum:        {requiredEmergencyFund,12:C}");
            Console.WriteLine($"  Shortfall:               {emergencyFundShortfall,12:C} (boosting savings)");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  Emergency Fund:          {currentEmergencyFund,12:C} ✅");
            Console.WriteLine($"  Required Minimum:        {requiredEmergencyFund,12:C}");
            Console.WriteLine($"  Surplus:                 {(currentEmergencyFund - requiredEmergencyFund),12:C}");
            Console.ResetColor();
        }
        
        Console.WriteLine();
    }
    
    private static void AnalyzeSocialSecurityStrategies(Person person)
    {
        Console.WriteLine("Social Security Claiming Analysis:");
        Console.WriteLine("================================");
        
        var strategies = SocialSecurityBenefitCalculator.AnalyzeClaimingStrategies(
            person.SocialSecurityIncome, person.FullRetirementAge);
        
        foreach (var strategy in strategies)
        {
            int age = strategy.Key;
            var (monthlyBenefit, annualBenefit, description) = strategy.Value;
            
            Console.WriteLine($"Age {age}: ${monthlyBenefit:N0}/month (${annualBenefit:N0}/year) - {description}");
        }
        
        Console.WriteLine();
    }
    
    private static void ShowAccountBalances(Person person, DateOnly date)
    {
        Console.WriteLine("Initial Account Balances:");
        Console.WriteLine("========================");
        
        double totalBalance = 0;
        foreach (var account in person.Investments.Accounts)
        {
            double balance = account.Balance(date);
            totalBalance += balance;
            
            string rmdStatus = RMDCalculator.IsSubjectToRMD(account.Type) ? " (RMD required at 73)" : "";
            Console.WriteLine($"{account.Name}: ${balance:N0}{rmdStatus}");
        }
        
        Console.WriteLine($"Total: ${totalBalance:N0}");
        Console.WriteLine();
    }
}
