using RetirementPlanner;
using RetirementPlanner.Event;
using RetirementPlanner.ConsoleApp;
using RetirementPlanner.Test;

namespace RetirementPlanner.ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Retirement Planner Console");
        Console.WriteLine("==========================\n");
        
        await ShowMainMenu();
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    private static async Task ShowMainMenu()
    {
        Console.WriteLine("Select a person profile to simulate:");
        Console.WriteLine("1. Me (Software Engineer, Age 27)");
        Console.WriteLine("2. Early Retiree (High Earner, Age 39)");
        Console.WriteLine("3. Normal Retiree (Traditional Path, Age 64)");
        Console.WriteLine("4. Late Retiree (Current Retiree, Age 74)");
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
            await ShowPersonMenu(person);
        }
    }
    
    private static async Task ShowPersonMenu(Person person)
    {
        Console.WriteLine($"Birth Date: {person.BirthDate:yyyy-MM-dd}");
        Console.WriteLine($"Current Age: {person.CurrentAge(DateOnly.FromDateTime(DateTime.Now))}");
        Console.WriteLine($"Full Retirement Age: {person.FullRetirementAge}");
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
        Console.WriteLine("\n=== Running Comprehensive Retirement Simulation ===\n");
        
        // Calculate end date to when person turns 110
        var currentAge = person.CurrentAge(DateOnly.FromDateTime(DateTime.Now));
        var yearsToAge110 = 110 - currentAge;
        var endDate = DateOnly.FromDateTime(DateTime.Now.AddYears(yearsToAge110));
        
        Console.WriteLine($"Simulating from age {currentAge} to age 110 ({yearsToAge110} years)");
        
        // Create graph generator to track simulation data
        var graphGenerator = new RetirementGraphGenerator(person);
        
        var planner = new RetirementPlanner(person, new RetirementPlanner.Options
        {
            StartDate = DateOnly.FromDateTime(DateTime.Now),
            EndDate = endDate,
            ReportGranularity = TimeSpan.FromDays(365), // Annual reports
            TimeStep = TimeSpan.FromDays(30) // Monthly simulation steps
        });
        
        // Set up event handlers with graph data collection
        LifeEvents.Subscribe(planner);
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
        
        // Generate the main account balance graph
        var graphPath = $"retirement_simulation_{person.BirthDate.Year}.png";
        graphGenerator.GenerateComprehensiveGraph(graphPath);
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    private static void SetupEventHandlers(Person person)
    {
        // Note: LifeEvents should be subscribed to the planner when it's created
        // This method is kept for future event handler setup
    }
    
    private static void ShowAccountBalances(Person person, DateOnly date)
    {
        Console.WriteLine($"=== Account Balances as of {date} ===");
        foreach (var account in person.Investments.Accounts)
        {
            Console.WriteLine($"{account.Name}: ${account.Balance(date):N2}");
        }
        Console.WriteLine();
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
        // Subscribe to static events to collect data for graphing
        RetirementPlanner.OnNewMonth += (sender, e) =>
        {
            var accounts = person.Investments.Accounts.ToDictionary(a => a.Name, a => a);
            graphGenerator.RecordDataPoint(e.Date, accounts);
        };
        
        RetirementPlanner.NewYear += (sender, e) =>
        {
            var accounts = person.Investments.Accounts.ToDictionary(a => a.Name, a => a);
            graphGenerator.RecordDataPoint(e.Date, accounts);
        };
    }
}