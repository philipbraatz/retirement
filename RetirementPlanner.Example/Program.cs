using RetirementPlanner;
using RetirementPlanner.Event;
using RetirementPlanner.Example;
using RetirementPlanner.Test;

namespace RetirementPlanner.Example;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Retirement Planner Examples");
        Console.WriteLine("===========================\n");
        
        Console.WriteLine("Select an example to run:");
        Console.WriteLine("1. Me (27 years old, early retirement strategy) - RECOMMENDED");
        Console.WriteLine("2. Enhanced Examples (Multiple scenarios)");
        Console.WriteLine("3. Basic Example");
        Console.WriteLine("4. Exit");
        
        Console.Write("\nEnter your choice (1-4): ");
        string? choice = Console.ReadLine();
        
        switch (choice)
        {
            case "1":
                await RunMeExample();
                break;
            case "2":
                await EnhancedRetirementExample.RunExample();
                break;
            case "3":
                await RunBasicExample();
                break;
            case "4":
                return;
            default:
                Console.WriteLine("Invalid choice. Running 'Me' example...");
                await RunMeExample();
                break;
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
    
    private static async Task RunMeExample()
    {
        Console.WriteLine("\n🎯 Running Retirement Simulation for 'Me'");
        Console.WriteLine("==========================================");
        
        var person = TestPersonFactory.Me();
        
        // Display profile information
        var currentAge = person.CurrentAge(DateOnly.FromDateTime(DateTime.Now));
        var totalPortfolio = person.Investments.Accounts.Sum(a => a.Balance(DateOnly.FromDateTime(DateTime.Now)));
        
        Console.WriteLine($"\n👤 Profile Summary:");
        Console.WriteLine($"📅 Current Age: {currentAge} years old");
        Console.WriteLine($"💼 Annual Salary: ${person.Jobs.Sum(j => j.Salary):N0}");
        Console.WriteLine($"💰 Total Portfolio: ${totalPortfolio:N0}");
        Console.WriteLine($"🏠 Annual Expenses: ${person.EssentialExpenses + person.DiscretionarySpending:N0}");
        Console.WriteLine($"🎂 Part-time Age: {person.PartTimeAge}");
        Console.WriteLine($"🎯 Social Security Claiming Age: {person.SocialSecurityClaimingAge}");
        Console.WriteLine($"💳 Retirement Contribution Rate: {person.Jobs.First().RetirementContributionPercent:P0}");
        
        Console.WriteLine("\n📊 Account Breakdown:");
        foreach (var account in person.Investments.Accounts)
        {
            var balance = account.Balance(DateOnly.FromDateTime(DateTime.Now));
            var percentage = totalPortfolio > 0 ? (balance / totalPortfolio * 100) : 0;
            Console.WriteLine($"   • {account.Name}: ${balance:N0} ({percentage:F1}%)");
        }
        
        Console.WriteLine("\n🚀 Starting retirement simulation...\n");
        
        var options = new RetirementPlanner.Options
        {
            StartDate = DateOnly.FromDateTime(DateTime.Now),
            EndDate = DateOnly.FromDateTime(DateTime.Now.AddYears(50)), // To age 77
            ReportGranularity = TimeSpan.FromDays(365), // Annual reports
            TimeStep = TimeSpan.FromDays(30) // Monthly simulation steps
        };
        
        var planner = new RetirementPlanner(person, options);
        LifeEvents.Subscribe(planner);
        
        await planner.RunRetirementSimulation();
        
        Console.WriteLine("\n📊 Key Insights for 'Me':");
        Console.WriteLine("• Early retirement strategy with part-time work at 45");
        Console.WriteLine("• High 401k contribution rate (26%) for accelerated savings");
        Console.WriteLine("• Delayed Social Security claiming at 70 for maximum benefits");
        Console.WriteLine("• Diversified portfolio across Traditional 401k, Roth IRA, and Taxable accounts");
        Console.WriteLine("• Current portfolio of $763k at age 27 puts you on track for early retirement!");
    }
    
    private static async Task RunBasicExample()
    {
        // Keep the original basic example for comparison
        Console.WriteLine("Running basic example...");
        
        var person = TestPersonFactory.Me();
        var planner = new RetirementPlanner(person);
        LifeEvents.Subscribe(planner);
        
        await planner.RunRetirementSimulation();
    }
}