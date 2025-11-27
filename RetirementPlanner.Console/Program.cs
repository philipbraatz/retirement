using RetirementPlanner;
using RetirementPlanner.Calculators;
using RetirementPlanner.Event;
using RetirementPlanner.Test;

namespace RetirementPlanner.ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Retirement Planner Console");
        Console.WriteLine("==========================\n");
        PrintStartupDisclaimer();
        await ShowMainMenu();
        PrintExitDisclaimer();
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    private static void PrintStartupDisclaimer()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("DISCLAIMER: Educational simulation only. Not tax, legal, or investment advice.");
        Console.WriteLine("Federal tax modeling simplified; state/local taxes omitted.");
        Console.WriteLine("See TAX_COMPLIANCE_REVIEW.md and LEGAL_DISCLAIMER.md for details.\n");
        Console.ResetColor();
    }

    private static void PrintExitDisclaimer()
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("\n====================================================");
        Console.WriteLine("Simulation complete.");
        Console.WriteLine("This software provides estimates only; accuracy not guaranteed.");
        Console.WriteLine("Consult qualified professionals before acting on results.");
        Console.WriteLine("Docs: TAX_COMPLIANCE_REVIEW.md | LEGAL_DISCLAIMER.md");
        Console.WriteLine("====================================================");
        Console.ResetColor();
    }
    
    private static async Task ShowMainMenu()
    {
        Console.WriteLine("Select a person profile to simulate:");
        Console.WriteLine("1. Me (Software Engineer, Age27)");
        Console.WriteLine("2. Early Retiree (High Earner, Age39)");
        Console.WriteLine("3. Normal Retiree (Traditional Path, Age64)");
        Console.WriteLine("4. Late Retiree (Current Retiree, Age74)");
        Console.WriteLine("5. Custom Person (Create your own)");
        Console.WriteLine("6. Exit");
        
        Console.Write("\nEnter your choice (1-6): ");
        string? choice = Console.ReadLine();
        
        Person? person = null;
        
        switch (choice)
        {
            case "1":
                person = TestPersonFactory.Me();
                Console.WriteLine("\n=== Selected: Me Profile ===");
                break;
            case "2":
                person = TestPersonFactory.CreateEarlyRetiree();
                Console.WriteLine("\n=== Selected: Early Retiree Profile ===");
                break;
            case "3":
                person = TestPersonFactory.CreateNormalRetiree();
                Console.WriteLine("\n=== Selected: Normal Retiree Profile ===");
                break;
            case "4":
                person = TestPersonFactory.CreateLateRetiree();
                Console.WriteLine("\n=== Selected: Late Retiree Profile ===");
                break;
            case "5":
                person = await CreateCustomPerson();
                Console.WriteLine("\n=== Selected: Custom Profile ===");
                break;
            case "6":
                return;
            default:
                Console.WriteLine("Invalid choice. Please try again.\n");
                await ShowMainMenu();
                return;
        }
        
        if (person != null)
        {
            // Ensure global cash account exists and is registered
            var existingCash = person.Investments.Accounts.FirstOrDefault(a => a.Name == "Cash");
            if (existingCash is null)
            {
                var cash = new InvestmentAccount.CashAccount();
                person.Investments.Accounts.Add(cash);
                InvestmentAccount.SetGlobalCashAccount((InvestmentAccount.CashAccount)cash);
            }
            else if (existingCash is InvestmentAccount.CashAccount cashAcct)
            {
                InvestmentAccount.SetGlobalCashAccount(cashAcct);
            }
            await ShowPersonMenu(person);
        }
    }
    
    private static async Task ShowPersonMenu(Person person)
    {
        Console.WriteLine($"Birth Date: {person.BirthDate:yyyy-MM-dd}");
        Console.WriteLine($"Current Age: {person.CurrentAge(DateOnly.FromDateTime(DateTime.Now))}");
        Console.WriteLine($"Full Retirement Age: {person.FullRetirementAge}");
        Console.WriteLine($"Early/Planned Retirement Age: {(person.RetirementAge > 0 ? person.RetirementAge : person.PartTimeAge)}");
        Console.WriteLine($"Essential Expenses: ${person.EssentialExpenses:N0}/year");
        Console.WriteLine($"Discretionary Spending: ${person.DiscretionarySpending:N0}/year");
        Console.WriteLine($"Active Jobs: {person.Jobs.Count}");
        Console.WriteLine($"Investment Accounts: {person.Investments.Accounts.Count}");
        
        Console.WriteLine("\nWhat would you like to do?");
        Console.WriteLine("1. Run Retirement Simulation");
        Console.WriteLine("2. Customize This Person");
        Console.WriteLine("3. View Account Details");
        Console.WriteLine("4. Back to Main Menu");
        
        Console.Write("\nEnter your choice (1-4): ");
        string? choice = Console.ReadLine();
        
        switch (choice)
        {
            case "1":
                await RunRetirementSimulation(person);
                break;
            case "2":
                CustomizePerson(person);
                await ShowPersonMenu(person); // Return to person menu after customization
                break;
            case "3":
                ShowAccountDetails(person);
                await ShowPersonMenu(person); // Return to person menu after viewing details
                break;
            case "4":
                await ShowMainMenu();
                break;
            default:
                Console.WriteLine("Invalid choice. Please try again.\n");
                await ShowPersonMenu(person);
                break;
        }
    }
    
    private static async Task RunRetirementSimulation(Person person)
    {
        Console.WriteLine("\n=== Running Retirement Simulation ===\n");
        
        // IMPORTANT: Clear any previous event subscriptions to avoid duplicate handlers
        RetirementPlanner.ResetAllEventHandlers();
        
        // Calculate end date to when person turns110
        var currentAge = person.CurrentAge(DateOnly.FromDateTime(DateTime.Now));
        var yearsToAge110 =110 - currentAge;
        var endDate = DateOnly.FromDateTime(DateTime.Now.AddYears(yearsToAge110));
        
        Console.WriteLine($"Simulating from age {currentAge} to age110 ({yearsToAge110} years)");
        
        // Create graph generator to track simulation data
        var graphGenerator = new RetirementGraphGenerator(person);
        
        var planner = new RetirementPlanner(person, new RetirementPlanner.Options
        {
            StartDate = DateOnly.FromDateTime(DateTime.Now),
            EndDate = endDate,
            ReportGranularity = TimeSpan.FromDays(365), // Annual reports
            TimeStep = TimeSpan.FromDays(7), // Weekly simulation steps for more accuracy
            AutoSubscribeLifeEvents = false // Use custom enhanced handlers below
        });
        
        // Use the unified enhanced event handlers
        SetupEnhancedEventHandlers(planner, person);
        SetupGraphDataCollection(planner, person, graphGenerator);
        
        // Show initial account balances
        ShowAccountBalances(person, DateOnly.FromDateTime(DateTime.Now));
        
        // Record initial data point
        var initialAccounts = person.Investments.Accounts.ToDictionary(a => a.Name, a => a);
        graphGenerator.RecordDataPoint(DateOnly.FromDateTime(DateTime.Now), initialAccounts);
        
        // Run the simulation
        await planner.RunRetirementSimulation();
        
        Console.WriteLine("\n=== Simulation Complete ===");
        
        // Generate and display graphs
        Console.WriteLine("\nGenerating retirement simulation graphs...");
        graphGenerator.PrintDataSummary();
        
        // Generate the main account balance graph with early retirement indicators
        var graphPath = $"retirement_simulation_{person.BirthDate.Year}.png";
        graphGenerator.GenerateComprehensiveGraph(graphPath);
        
        // Generate withdrawal analysis graph
        var withdrawalGraphPath = $"withdrawal_analysis_{person.BirthDate.Year}.png";
        graphGenerator.GenerateWithdrawalAnalysisGraph(withdrawalGraphPath);
        
        // Generate income/expense graph if there's income data
        var incomeGraphPath = $"income_expenses_{person.BirthDate.Year}.png";
        graphGenerator.GenerateIncomeExpenseGraph(incomeGraphPath);
        
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
    
    private static void SetupEnhancedEventHandlers(RetirementPlanner planner, Person person)
    {
        // Subscribe to enhanced event handlers for comprehensive retirement simulation
        // This replaces the basic LifeEvents.Subscribe() with sophisticated logic
        
        // Monthly processing with optimal withdrawal strategy and Roth conversions
        RetirementPlanner.OnNewMonth += (sender, e) =>
        {
            // Apply monthly growth first - ONLY ONCE
            person.Investments.ApplyMonthlyGrowth(e.Date);
            
            // Perform annual Roth conversions on January if beneficial
            if (e.Date.Month == 1 && person.CurrentAge(e.Date) < 73) // Don't convert after RMDs start
            {
                person.Investments.PerformRothConversion(person, e.Date);
            }
            
            // Handle monthly expenses using optimal withdrawal strategy with early penalty protection
            double monthlyExpenses = (person.EssentialExpenses + person.DiscretionarySpending) / 12.0;
            
            if (monthlyExpenses > 0)
            {
                // Use the sophisticated OnSpending logic from LifeEvents instead of duplicating it
                LifeEvents.OnSpending(person, new SpendingEventArgs 
                { 
                    Date = e.Date, 
                    Amount = monthlyExpenses, 
                    TransactionCategory = TransactionCategory.Expenses 
                });
            }
            
            // Print simplified annual summary
            if (e.Date.Month == 1)
            {
                PrintAnnualSummary(person, e.Date);
            }
        };
        
        // Enhanced job pay processing with optimal contribution allocation
        RetirementPlanner.OnJobPay += (sender, e) =>
        {
            int age = person.CurrentAge(e.Date);
            
            // Calculate optimal retirement contribution allocation using the sophisticated logic
            var (traditionalAmount, rothAmount) = e.Job.CalculateOptimalRetirementAllocation(person, e.Date);
            double companyMatch = e.GrossIncome * ((e.Job.CompanyMatchContributionPercent ?? 0) / 100);
            
            // Deposit gross income to global cash first (realistic inflow)
            var globalCash = person.Investments.Accounts.FirstOrDefault(a => a.Name == "Cash") as InvestmentAccount.CashAccount;
            if (globalCash != null && e.GrossIncome > 0)
            {
                globalCash.Deposit(e.GrossIncome, e.Date, TransactionCategory.Income);
            }
            // Find both Traditional and Roth 401k accounts
            var traditional401k = person.Investments.Accounts.FirstOrDefault(a => a is Traditional401kAccount);
            var roth401k = person.Investments.Accounts.FirstOrDefault(a => a is Roth401kAccount);
            // Allocate contributions from cash via transfer
            if (traditionalAmount > 0 && traditional401k != null && globalCash != null)
            {
                double actualTraditionalDeposit = globalCash.TransferTo(traditional401k, traditionalAmount, e.Date, TransactionCategory.ContributionPersonal);
                double actualMatchDeposit = globalCash.TransferTo(traditional401k, companyMatch, e.Date, TransactionCategory.ContributionEmployer);
                if (actualTraditionalDeposit > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  💰 Traditional 401k: {actualTraditionalDeposit:C} personal + {actualMatchDeposit:C} match");
                    Console.ResetColor();
                }
            }
            if (rothAmount > 0 && roth401k != null && globalCash != null)
            {
                double actualRothDeposit = globalCash.TransferTo(roth401k, rothAmount, e.Date, TransactionCategory.ContributionPersonal);
                if (actualRothDeposit > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"  🌟 Roth 401k: {actualRothDeposit:C} (tax-free growth!)");
                    Console.ResetColor();
                }
            }
            // Recompute net income remaining in cash after contributions & rough tax
            double netIncome = e.GrossIncome - (traditionalAmount + rothAmount) - (e.GrossIncome * 0.25);
            if (globalCash != null && netIncome > 0)
            {
                globalCash.Deposit(netIncome, e.Date, TransactionCategory.Income);
            }
            // Pocket cash allocation now moves from Cash to Pocket Cash account
            var pocketCashAcct = person.Investments.Accounts.FirstOrDefault(a => a.Name == "Pocket Cash");
            if (pocketCashAcct != null && netIncome > 0 && globalCash != null)
            {
                double pocketShortfall = person.PocketCashShortfall(e.Date);
                if (pocketShortfall > 0)
                {
                    double toPocket = Math.Min(netIncome, pocketShortfall);
                    globalCash.TransferTo(pocketCashAcct, toPocket, e.Date, TransactionCategory.InternalTransfer);
                    netIncome -= toPocket;
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"  🟡 Pocket Cash Refill: {toPocket:C} (Target {person.PocketCashTarget(e.Date):C})");
                    Console.ResetColor();
                }
            }
            var savingsAccount = person.Investments.Accounts.FirstOrDefault(a => a.Type == AccountType.Savings && a.Name != "Cash" && a.Name != "Pocket Cash");
            if (savingsAccount != null && netIncome > 0 && globalCash != null)
            {
                double emergencyFundShortfall = person.GetEmergencyFundShortfall(e.Date);
                if (emergencyFundShortfall > 0)
                {
                    double emergencyFundBoost = Math.Min(netIncome, emergencyFundShortfall);
                    globalCash.TransferTo(savingsAccount, emergencyFundBoost, e.Date, TransactionCategory.InternalTransfer);
                    netIncome -= emergencyFundBoost;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"  💧 Emergency Fund Boost: {emergencyFundBoost:C} → Savings");
                    Console.ResetColor();
                }
                if (netIncome > 0)
                {
                    globalCash.TransferTo(savingsAccount, netIncome, e.Date, TransactionCategory.InternalTransfer);
                }
            }
        };
        
        // Birthday events for milestone tracking using switch expressions
        RetirementPlanner.Birthday += (sender, e) =>
        {
            Console.WriteLine($"🎂 Age {e.Age} ({e.Date})");
            
            // Process age-based milestone using switch expression
            var (message, color) = GetAgeMilestone(e.Age, person.FullRetirementAge);
            
            if (!string.IsNullOrEmpty(message))
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        };
        
        // New Year events for annual processing
        RetirementPlanner.NewYear += (sender, e) =>
        {
            Console.WriteLine($"\n=== NEW YEAR {e.Date.Year} ===");
            
            // Show account balances every year
            ShowYearlyAccountBalances(person, e.Date);
            
            // Apply yearly pay raises
            person.ApplyYearlyPayRaises();
            
            // Apply inflation to expenses
            person.EssentialExpenses *= (1 + person.InflationRate);
            person.DiscretionarySpending *= (1 + person.InflationRate);
            
            // Calculate and update taxable income
            var grossIncome = person.Jobs.Sum(s => s.GrossAnnualIncome());
            person.TaxableIncome = grossIncome;
            
            // Calculate retirement contributions
            double retirementContribution = grossIncome * (person.Jobs.FirstOrDefault()?.RetirementContributionPercent / 100 ?? 0);
            person.TaxableIncome -= retirementContribution;
        };
    }
    
    /// <summary>
    /// Determines the milestone message and color based on age using switch expression
    /// </summary>
    private static (string message, ConsoleColor color) GetAgeMilestone(int age, int fullRetirementAge)
    {
        return age switch
        {
            50 => ($"🎉 AGE {age}: Catch-up contributions now available!", ConsoleColor.Green),
            55 => ($"🎯 AGE {age}: Rule of 55 - potential early 401k access!", ConsoleColor.Yellow),
            59 => ($"🎯 AGE {age}: Early withdrawal penalties eliminated!", ConsoleColor.Yellow),
            62 => ($"💰 AGE {age}: Social Security early claiming available!", ConsoleColor.Blue),
            65 => ($"🏥 AGE {age}: Medicare eligibility begins!", ConsoleColor.Cyan),
            70 => ($"💎 AGE {age}: Maximum Social Security benefits!", ConsoleColor.Green),
            73 => ($"📊 AGE {age}: Required Minimum Distributions begin!", ConsoleColor.Red),
            _ when age == fullRetirementAge => ($"🎊 AGE {age}: Full Retirement Age reached!", ConsoleColor.Green),
            _ => (string.Empty, ConsoleColor.White)
        };
    }
    
    private static void PrintAnnualSummary(Person person, DateOnly date)
    {
        Console.WriteLine($"\n📅 ANNUAL SUMMARY - {date:yyyy} (Age {person.CurrentAge(date)})");
        Console.WriteLine("═══════════════════════════════════════════════════");
        
        double totalBalance = 0;
        
        Console.WriteLine("ACCOUNT BALANCES:");
        foreach (var account in person.Investments.Accounts.OrderBy(a => a.Name))
        {
            double balance = account.Balance(date);
            totalBalance += balance;
            
            // Show annual activity
            double annualDeposits = account.DepositHistory
                .Where(d => d.Date.Year == date.Year)
                .Sum(d => d.Amount);
            
            double annualWithdrawals = account.WithdrawalHistory
                .Where(w => w.Date.Year == date.Year)
                .Sum(w => w.Amount);
            
            string activity = "";
            if (annualDeposits > 0 || annualWithdrawals > 0)
            {
                activity = $" (+{annualDeposits:C} -{annualWithdrawals:C})";
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
            Console.WriteLine($"  Shortfall:               {emergencyFundShortfall,12:C}");
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
    
    private static void ShowAccountBalances(Person person, DateOnly date)
    {
        Console.WriteLine($"=== Account Balances as of {date:M/d/yyyy} ===");
        
        double totalBalance = 0;
        foreach (var account in person.Investments.Accounts)
        {
            double balance = account.Balance(date);
            totalBalance += balance;
            
            string rmdStatus = RMDCalculator.IsSubjectToRMD(account.Type) ? " (RMD required at 73)" : "";
            Console.WriteLine($"{account.Name}: ${balance:N2}{rmdStatus}");
        }
        
        Console.WriteLine($"Total: ${totalBalance:N2}");
        
        // Show emergency fund information
        double emergencyFund = person.GetCurrentEmergencyFundBalance(date);
        double requiredEmergency = person.GetRequiredEmergencyFundMinimum(date);
        
        Console.WriteLine($"\nEmergency Fund: ${emergencyFund:N2} (Required: ${requiredEmergency:N2})");
        Console.WriteLine();
    }
    
    private static void ShowYearlyAccountBalances(Person person, DateOnly date)
    {
        Console.WriteLine($"=== ACCOUNT BALANCES - {date.Year} (Age {person.CurrentAge(date)}) ===");
        
        double totalBalance = 0;
        foreach (var account in person.Investments.Accounts.OrderBy(a => a.Name))
        {
            double balance = account.Balance(date);
            totalBalance += balance;
            
            // Calculate year-over-year change if previous year data exists
            double previousYearBalance = account.Balance(new DateOnly(date.Year - 1, 12, 31));
            double yearOverYearChange = balance - previousYearBalance;
            double percentChange = previousYearBalance > 0 ? (yearOverYearChange / previousYearBalance) * 100 : 0;
            
            string changeIndicator = yearOverYearChange >= 0 ? "+" : "";
            Console.WriteLine($"{account.Name,-20}: ${balance,12:N0} ({changeIndicator}{yearOverYearChange,10:N0} | {percentChange,6:F1}%)");
        }
        
        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"{"TOTAL",-20}: ${totalBalance,12:N0}");
        
        // Show Income vs Expenditure Analysis for troubleshooting
        ShowIncomeExpenditureComparison(person, date);
        
        // Show emergency fund status
        double emergencyFund = person.GetCurrentEmergencyFundBalance(date);
        double requiredEmergency = person.GetRequiredEmergencyFundMinimum(date);
        bool isAdequate = emergencyFund >= requiredEmergency;
        string status = isAdequate ? "✅" : "⚠️";
        
        Console.WriteLine($"\nEmergency Fund: ${emergencyFund:N0} / ${requiredEmergency:N0} {status}");
        Console.WriteLine();
    }
    
    private static void ShowIncomeExpenditureComparison(Person person, DateOnly date)
    {
        Console.WriteLine($"\n=== INCOME vs EXPENDITURES - {date.Year} ===");
        
        // Calculate annual income sources
        double jobIncome = 0;
        double socialSecurityIncome = 0;
        double withdrawalIncome = 0;
        
        // Job income
        if (person.Jobs.Any())
        {
            jobIncome = person.Jobs.Sum(j => j.GrossAnnualIncome());
        }
        
        // Social Security income
        int age = person.CurrentAge(date);
        if (age >= person.SocialSecurityClaimingAge && age >= 62)
        {
            socialSecurityIncome = person.SocialSecurityIncome * 12; // Convert monthly to annual
        }
        
        // Calculate withdrawal income (from all accounts for the year)
        foreach (var account in person.Investments.Accounts)
        {
            var yearlyWithdrawals = account.WithdrawalHistory
                .Where(w => w.Date.Year == date.Year && w.Category != TransactionCategory.EarlyWithdrawalPenality)
                .Sum(w => w.Amount);
            withdrawalIncome += yearlyWithdrawals;
        }
        
        double totalIncome = jobIncome + socialSecurityIncome + withdrawalIncome;
        
        // These ARE annual amounts - confirmed by looking at Data.cs and RetirementPlannerConsole.cs
        // The properties are documented as annual and set as annual values
        double essentialExpenses = person.EssentialExpenses;        // Annual amount
        double discretionarySpending = person.DiscretionarySpending; // Annual amount
        double totalExpenses = essentialExpenses + discretionarySpending;
        
        // Calculate penalties paid
        double penaltiesPaid = 0;
        foreach (var account in person.Investments.Accounts)
        {
            var yearlyPenalties = account.WithdrawalHistory
                .Where(w => w.Date.Year == date.Year && w.Category == TransactionCategory.EarlyWithdrawalPenality)
                .Sum(w => w.Amount);
            penaltiesPaid += yearlyPenalties;
        }
        
        // Side-by-side comparison display
        Console.WriteLine($"{"INCOME SOURCES",-25} {"AMOUNT",12} {"EXPENDITURES",-25} {"AMOUNT",12}");
        Console.WriteLine(new string('-', 74));
        
        // Show side-by-side comparison with proper annual amounts
        Console.WriteLine($"{"Job Income",-25} {jobIncome,12:C0} {"Essential Expenses",-25} {essentialExpenses,12:C0}");
        Console.WriteLine($"{"Social Security",-25} {socialSecurityIncome,12:C0} {"Discretionary Spending",-25} {discretionarySpending,12:C0}");
        Console.WriteLine($"{"Account Withdrawals",-25} {withdrawalIncome,12:C0} {"Early Withdrawal Penalties",-25} {penaltiesPaid,12:C0}");
        
        Console.WriteLine(new string('-', 74));
        Console.WriteLine($"{"TOTAL INCOME",-25} {totalIncome,12:C0} {"TOTAL EXPENDITURES",-25} {(totalExpenses + penaltiesPaid),12:C0}");
        
        // Calculate net cash flow
        double netCashFlow = totalIncome - (totalExpenses + penaltiesPaid);
        string flowIndicator = netCashFlow >= 0 ? "POSITIVE" : "NEGATIVE";
        var flowColor = netCashFlow >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
        
        Console.WriteLine(new string('=', 74));
        Console.ForegroundColor = flowColor;
        Console.WriteLine($"{"NET CASH FLOW",-25} {netCashFlow,12:C0} ({flowIndicator})");
        Console.ResetColor();
        
        // Show breakdown by category if there are penalties
        if (penaltiesPaid > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n⚠️  PENALTY BREAKDOWN:");
            foreach (var account in person.Investments.Accounts)
            {
                var accountPenalties = account.WithdrawalHistory
                    .Where(w => w.Date.Year == date.Year && w.Category == TransactionCategory.EarlyWithdrawalPenality)
                    .Sum(w => w.Amount);
                if (accountPenalties > 0)
                {
                    Console.WriteLine($"   {account.Name}: {accountPenalties:C}");
                }
            }
            Console.ResetColor();
        }
        
        // Show cash flow efficiency metrics
        if (totalIncome > 0)
        {
            double savingsRate = netCashFlow / totalIncome * 100;
            double penaltyRate = penaltiesPaid / totalIncome * 100;
            
            Console.WriteLine($"\nCASH FLOW METRICS:");
            Console.WriteLine($"  Savings Rate: {savingsRate:F1}% (Net savings as % of income)");
            if (penaltyRate > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Penalty Rate: {penaltyRate:F1}% (Penalties as % of income)");
                Console.ResetColor();
            }
        }
        
        // Show debugging information to verify values are reasonable
        Console.WriteLine($"\nDEBUG INFO:");
        Console.WriteLine($"  Raw person.EssentialExpenses: ${person.EssentialExpenses:C} (should be annual)");
        Console.WriteLine($"  Raw person.DiscretionarySpending: ${person.DiscretionarySpending:C} (should be annual)");
        Console.WriteLine($"  Monthly Essential Equivalent: ${essentialExpenses / 12:C} /month");
        Console.WriteLine($"  Monthly Discretionary Equivalent: ${discretionarySpending / 12:C} /month");
        Console.WriteLine($"  Total Monthly Spending: ${totalExpenses / 12:C} /month");
        
        // Validate that the values are reasonable for annual amounts
        if (essentialExpenses < 12000 || discretionarySpending < 1000)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"⚠️  WARNING: Expense values appear to be too low for annual amounts!");
            Console.WriteLine($"   Essential: ${essentialExpenses:C} (expected >$12,000 annually)");
            Console.WriteLine($"   Discretionary: ${discretionarySpending:C} (expected >$1,000 annually)");
            Console.WriteLine($"   This suggests these values may have been corrupted during simulation.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Expense values appear to be proper annual amounts.");
            Console.ResetColor();
        }
    }
    
    private static async Task<Person?> CreateCustomPerson()
    {
        Console.WriteLine("\n=== Create Custom Person ===");
        
        try
        {
            // Get birth date
            Console.Write("Enter birth year (1940-2010): ");
            if (!int.TryParse(Console.ReadLine(), out int birthYear) || birthYear < 1940 || birthYear > 2010)
            {
                Console.WriteLine("Invalid birth year. Using default 1980.");
                birthYear = 1980;
            }
            
            // Get expenses
            Console.Write("Enter annual essential expenses (default $40,000): ");
            if (!double.TryParse(Console.ReadLine(), out double essentialExpenses) || essentialExpenses < 0)
            {
                essentialExpenses = 40000;
            }
            
            Console.Write("Enter annual discretionary spending (default $15,000): ");
            if (!double.TryParse(Console.ReadLine(), out double discretionarySpending) || discretionarySpending < 0)
            {
                discretionarySpending = 15000;
            }
            
            // Get initial account balances
            Console.Write("Enter starting Traditional 401k balance (default $50,000): ");
            if (!double.TryParse(Console.ReadLine(), out double traditional401kBalance) || traditional401kBalance < 0)
            {
                traditional401kBalance = 50000;
            }
            
            Console.Write("Enter starting Roth 401k balance (default $0): ");
            if (!double.TryParse(Console.ReadLine(), out double roth401kBalance) || roth401kBalance < 0)
            {
                roth401kBalance = 0;
            }
            
            Console.Write("Enter starting Roth IRA balance (default $10,000): ");
            if (!double.TryParse(Console.ReadLine(), out double rothIRABalance) || rothIRABalance < 0)
            {
                rothIRABalance = 10000;
            }
            
            Console.Write("Enter starting savings balance (default $20,000): ");
            if (!double.TryParse(Console.ReadLine(), out double savingsBalance) || savingsBalance < 0)
            {
                savingsBalance = 20000;
            }
            
            // Create custom person
            var person = new Person()
            {
                BirthDate = new DateTime(birthYear, 1, 1),
                EssentialExpenses = essentialExpenses,
                DiscretionarySpending = discretionarySpending,
                FullRetirementAge = 67,
                SocialSecurityClaimingAge = 70
            };
            
            // Add accounts
            var birthDateOnly = DateOnly.FromDateTime(person.BirthDate);
            person.Investments = new([
                new Traditional401kAccount(0.05, "Traditional 401k", birthDateOnly, traditional401kBalance),
                new Roth401kAccount(0.05, "Roth 401k", birthDateOnly, roth401kBalance),
                new RothIRAAccount(0.05, "Roth IRA", person, rothIRABalance),
                new InvestmentAccount(0.05, "Savings", savingsBalance, AccountType.Savings)
            ]);
            
            // Ensure Pocket Cash holding exists
            if (!person.Investments.Accounts.Any(a => a.Name == "Pocket Cash"))
            {
                person.Investments.Accounts.Add(new InvestmentAccount(0.0, "Pocket Cash", 0, AccountType.Savings));
            }
            
            // Add job if person is working age
            var currentAge = person.CurrentAge(DateOnly.FromDateTime(DateTime.Now));
            if (currentAge < 65)
            {
                Console.Write("Enter annual salary (default $80,000): ");
                if (!double.TryParse(Console.ReadLine(), out double salary) || salary < 0)
                {
                    salary = 80000;
                }
                
                person.Jobs.Add(new IncomeSource()
                {
                    Title = "Custom Job",
                    Salary = salary / 12, // Monthly salary
                    RetirementContributionPercent = 0.15, // 15% retirement contribution
                    CompanyMatchContributionPercent = 0.05 // 5% company match
                });
            }
            
            Console.WriteLine($"\n=== Custom Person Created ===");
            Console.WriteLine($"Age: {currentAge}");
            Console.WriteLine($"Essential Expenses: {essentialExpenses:C}/year");
            Console.WriteLine($"Discretionary Spending: {discretionarySpending:C}/year");
            Console.WriteLine($"Total Account Balances: {person.Investments.Accounts.Sum(a => a.Balance(DateOnly.FromDateTime(DateTime.Now))):C}");
            
            return person;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating custom person: {ex.Message}");
            return null;
        }
    }
    
    private static void CustomizePerson(Person person)
    {
        Console.WriteLine("\n=== Customize Person ===");
        Console.WriteLine("1. Update Retirement Age");
        Console.WriteLine("2. Update Expenses");
        Console.WriteLine("3. Update Social Security Claiming Age");
        Console.WriteLine("4. Back");
        
        Console.Write("\nEnter your choice (1-4): ");
        string? choice = Console.ReadLine();
        
        switch (choice)
        {
            case "1":
                Console.Write($"Current retirement age: {person.FullRetirementAge}. Enter new age: ");
                if (int.TryParse(Console.ReadLine(), out int retirementAge) && retirementAge >= 59 && retirementAge <= 75)
                {
                    person.FullRetirementAge = retirementAge;
                    Console.WriteLine($"Retirement age updated to {retirementAge}");
                }
                else
                {
                    Console.WriteLine("Invalid age. Must be between 59 and 75.");
                }
                break;
            case "2":
                Console.Write($"Current essential expenses: ${person.EssentialExpenses:N0}. Enter new amount: ");
                if (decimal.TryParse(Console.ReadLine(), out decimal expenses) && expenses >= 0)
                {
                    person.EssentialExpenses = (int)expenses;
                    Console.WriteLine($"Essential expenses updated to ${expenses:N0}");
                }
                else
                {
                    Console.WriteLine("Invalid amount. Must be a positive number.");
                }
                break;
            case "3":
                Console.Write($"Current SS claiming age: {person.SocialSecurityClaimingAge}. Enter new age: ");
                if (int.TryParse(Console.ReadLine(), out int ssAge) && ssAge >= 62 && ssAge <= 70)
                {
                    person.SocialSecurityClaimingAge = ssAge;
                    Console.WriteLine($"Social Security claiming age updated to {ssAge}");
                }
                else
                {
                    Console.WriteLine("Invalid age. Must be between 62 and 70.");
                }
                break;
            case "4":
                return;
            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
        
        if (choice != "4")
        {
            Console.WriteLine();
            CustomizePerson(person); // Allow multiple customizations
        }
    }
    
    private static void ShowAccountDetails(Person person)
    {
        Console.WriteLine("\n=== Account Details ===");
        var currentDate = DateOnly.FromDateTime(DateTime.Now);
        foreach (var account in person.Investments.Accounts)
        {
            Console.WriteLine($"{account.Name}:");
            Console.WriteLine($"  Type: {account.Type}");
            Console.WriteLine($"  Balance: ${account.Balance(currentDate):N2}");
            Console.WriteLine($"  Growth Rate: {account.AnnualGrowthRate:P2}");
            Console.WriteLine();
        }
        
        if (person.Jobs.Count > 0)
        {
            Console.WriteLine("=== Employment Details ===");
            foreach (var job in person.Jobs)
            {
                Console.WriteLine($"Job Type: {job.Type}");
                Console.WriteLine($"Salary: ${job.Salary:N0}");
                Console.WriteLine($"Retirement Contribution: {job.RetirementContributionPercent:P0}");
                Console.WriteLine($"Company Match: {job.CompanyMatchContributionPercent:P0}");
                Console.WriteLine();
            }
        }
    }
    
    private static void SetupGraphDataCollection(RetirementPlanner planner, Person person, RetirementGraphGenerator graphGenerator)
    {
        // Subscribe to static events to collect data for graphing with more frequent sampling
        RetirementPlanner.OnNewMonth += (sender, e) =>
        {
            var accounts = person.Investments.Accounts.ToDictionary(a => a.Name, a => a);
            
            // Calculate monthly expenses (actual spending for this month)
            double monthlyExpenses = (person.EssentialExpenses + person.DiscretionarySpending) / 12.0;
            
            // Calculate monthly income (sum of all income sources for this month)
            double monthlyIncome = 0;
            
            // Job income - calculate if jobs are active
            foreach (var job in person.Jobs)
            {
                if (job.StartDate <= e.Date && (job.TerminationDate == null || job.TerminationDate > e.Date))
                {
                    // Calculate monthly income based on job type and frequency
                    monthlyIncome += job.GrossAnnualIncome() / 12.0;
                }
            }
            
            // Social Security income
            int age = person.CurrentAge(e.Date);
            if (age >= person.SocialSecurityClaimingAge && age >= 62)
            {
                monthlyIncome += person.SocialSecurityIncome; // Already monthly amount
            }
            
            graphGenerator.RecordDataPoint(e.Date, accounts, monthlyIncome, monthlyExpenses);
        };
        
        RetirementPlanner.NewYear += (sender, e) =>
        {
            var accounts = person.Investments.Accounts.ToDictionary(a => a.Name, a => a);
            
            // Calculate monthly amounts for New Year data point
            double monthlyExpenses = (person.EssentialExpenses + person.DiscretionarySpending) / 12.0;
            double monthlyIncome = 0;
            
            foreach (var job in person.Jobs)
            {
                if (job.StartDate <= e.Date && (job.TerminationDate == null || job.TerminationDate > e.Date))
                {
                    monthlyIncome += job.GrossAnnualIncome() / 12.0;
                }
            }
            
            int age = person.CurrentAge(e.Date);
            if (age >= person.SocialSecurityClaimingAge && age >= 62)
            {
                monthlyIncome += person.SocialSecurityIncome;
            }
            
            graphGenerator.RecordDataPoint(e.Date, accounts, monthlyIncome, monthlyExpenses);
        };
        
        // Add quarterly data collection for more granular graphing
        RetirementPlanner.Birthday += (sender, e) =>
        {
            var accounts = person.Investments.Accounts.ToDictionary(a => a.Name, a => a);
            
            // Calculate monthly amounts for Birthday data point
            double monthlyExpenses = (person.EssentialExpenses + person.DiscretionarySpending) / 12.0;
            double monthlyIncome = 0;
            
            foreach (var job in person.Jobs)
            {
                if (job.StartDate <= e.Date && (job.TerminationDate == null || job.TerminationDate > e.Date))
                {
                    monthlyIncome += job.GrossAnnualIncome() / 12.0;
                }
            }
            
            int age = person.CurrentAge(e.Date);
            if (age >= person.SocialSecurityClaimingAge && age >= 62)
            {
                monthlyIncome += person.SocialSecurityIncome;
            }
            
            graphGenerator.RecordDataPoint(e.Date, accounts, monthlyIncome, monthlyExpenses);
        };
        
        // Add data collection on major financial events
        RetirementPlanner.OnJobPay += (sender, e) =>
        {
            // Record data points after major financial transactions (every 3 months to avoid too much data)
            if (e.Date.Month % 3 == 1) // January, April, July, October
            {
                var accounts = person.Investments.Accounts.ToDictionary(a => a.Name, a => a);
                
                // For job pay events, we know the exact income from this job
                double monthlyExpenses = (person.EssentialExpenses + person.DiscretionarySpending) / 12.0;
                double monthlyIncome = 0;
                
                // Calculate total monthly income from all active jobs
                foreach (var job in person.Jobs)
                {
                    if (job.StartDate <= e.Date && (job.TerminationDate == null || job.TerminationDate > e.Date))
                    {
                        monthlyIncome += job.GrossAnnualIncome() / 12.0;
                    }
                }
                
                // Add Social Security if applicable
                int age = person.CurrentAge(e.Date);
                if (age >= person.SocialSecurityClaimingAge && age >= 62)
                {
                    monthlyIncome += person.SocialSecurityIncome;
                }
                
                graphGenerator.RecordDataPoint(e.Date, accounts, monthlyIncome, monthlyExpenses);
            }
        };
    }
}