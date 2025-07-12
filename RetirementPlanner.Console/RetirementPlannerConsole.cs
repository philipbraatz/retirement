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
        
        // Set up comprehensive event handlers
        SetupEventHandlers(person);
        
        Console.WriteLine($"Starting retirement simulation for {person.BirthDate:yyyy-MM-dd}");
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
        // Create person born in 1975 (currently 50 years old) - this is a scenario person for demonstration
        var person = new Person
        {
            BirthDate = new DateTime(1975, 3, 15),
            FullRetirementAge = SocialSecurityBenefitCalculator.GetFullRetirementAge(1975), // 67
            SocialSecurityClaimingAge = 67, // Claim at FRA
            FileType = TaxBrackets.FileType.Single,
            SocialSecurityIncome = 2400, // Monthly SS benefit at FRA
            EssentialExpenses = 60000, // Annual essential expenses
            DiscretionarySpending = 20000 // Annual discretionary spending
        };
        
        // Create diverse investment accounts
        var traditional401k = new Traditional401kAccount(
            annualGrowthRate: 0.07, 
            name: "Company 401(k)", 
            birthdate: DateOnly.FromDateTime(person.BirthDate), 
            startingBalance: 450000);
            
        var roth401k = new Roth401kAccount(
            annualGrowthRate: 0.07, 
            name: "Roth 401(k)", 
            birthdate: DateOnly.FromDateTime(person.BirthDate), 
            startingBalance: 200000);
            
        var rothIRA = new RothIRAAccount(
            annualGrowthRate: 0.07, 
            name: "Roth IRA", 
            person: person, 
            startingBalance: 150000);
        
        person.Investments = new InvestmentManager([traditional401k, roth401k, rothIRA]);
        
        // Add employment income
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
        
        return person;
    }
    
    private static void SetupEventHandlers(Person person)
    {
        // Age 50 - Catch-up contributions
        RetirementPlanner.OnCatchUpContributionsEligible += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n🎉 AGE {e.Age}: Catch-up contributions now available!");
            
            // Show new contribution limits
            int currentYear = e.Date.Year;
            double new401kLimit = ContributionLimits.Get401kPersonalLimit(currentYear, e.Age);
            double newIRALimit = ContributionLimits.GetIRALimit(currentYear, e.Age);
            
            Console.WriteLine($"   401(k) limit increased to: ${new401kLimit:N0}");
            Console.WriteLine($"   IRA limit increased to: ${newIRALimit:N0}");
            Console.ResetColor();
        };
        
        // Age 55 - Rule of 55
        RetirementPlanner.OnRuleOf55Eligible += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n📋 AGE {e.Age}: Rule of 55 now available!");
            Console.WriteLine($"   Can withdraw from 401(k) without penalty if separated from service");
            Console.ResetColor();
        };
        
        // Age 59½ - Early withdrawal penalties end
        RetirementPlanner.OnEarlyWithdrawalPenaltyEnds += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n🎯 AGE {e.Age}: Early withdrawal penalties eliminated!");
            Console.WriteLine($"   All retirement accounts can be accessed without 10% penalty");
            Console.ResetColor();
        };
        
        // Age 62 - Social Security early eligibility
        RetirementPlanner.OnSocialSecurityEarlyEligible += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"\n💰 AGE {e.Age}: Social Security early claiming available!");
            
            double multiplier = SocialSecurityBenefitCalculator.CalculateBenefitMultiplier(62, person.FullRetirementAge);
            double earlyBenefit = person.SocialSecurityIncome * multiplier;
            
            Console.WriteLine($"   Early benefit (62): ${earlyBenefit:N0}/month (vs ${person.SocialSecurityIncome:N0} at FRA)");
            Console.WriteLine($"   Permanent reduction: {(1-multiplier)*100:F1}%");
            
            // Show break-even analysis
            double breakEvenAge = SocialSecurityBenefitCalculator.CalculateBreakEvenAge(
                person.SocialSecurityIncome, 62, person.FullRetirementAge, person.FullRetirementAge);
            Console.WriteLine($"   Break-even age vs FRA claiming: {breakEvenAge:F1}");
            Console.ResetColor();
        };
        
        // Age 65 - Medicare eligibility
        RetirementPlanner.OnMedicareEligible += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\n🏥 AGE {e.Age}: Medicare eligibility begins!");
            
            double preMedicareCost = HealthcareCostCalculator.CalculateAnnualHealthcareCosts(64, false);
            double medicareCost = HealthcareCostCalculator.CalculateAnnualHealthcareCosts(65, false);
            
            Console.WriteLine($"   Healthcare cost transition:");
            Console.WriteLine($"   Pre-Medicare: ${preMedicareCost:N0}/year");
            Console.WriteLine($"   Medicare: ${medicareCost:N0}/year");
            Console.WriteLine($"   Must enroll to avoid lifetime penalties");
            Console.ResetColor();
        };
        
        // Full Retirement Age - 100% Social Security
        RetirementPlanner.OnFullRetirementAge += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n🎊 AGE {e.Age}: Full Retirement Age reached!");
            Console.WriteLine($"   100% Social Security benefits: ${person.SocialSecurityIncome:N0}/month");
            
            double delayedBenefit70 = person.SocialSecurityIncome * 
                SocialSecurityBenefitCalculator.CalculateBenefitMultiplier(70, person.FullRetirementAge);
            Console.WriteLine($"   Delayed credits (age 70): ${delayedBenefit70:N0}/month (+24%)");
            
            double breakEvenAge = SocialSecurityBenefitCalculator.CalculateBreakEvenAge(
                person.SocialSecurityIncome, person.FullRetirementAge, 70, person.FullRetirementAge);
            Console.WriteLine($"   Break-even age for delaying to 70: {breakEvenAge:F1}");
            Console.ResetColor();
        };
        
        // Age 73 - RMDs begin
        RetirementPlanner.OnRMDAgeHit += (sender, e) =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n📊 AGE {e.Age}: Required Minimum Distributions begin!");
            
            foreach (var account in person.Investments.Accounts)
            {
                if (RMDCalculator.IsSubjectToRMD(account.Type))
                {
                    double balance = account.Balance(e.Date);
                    double rmd = RMDCalculator.CalculateRMD(account, e.Age, balance);
                    
                    Console.WriteLine($"   {account.Name}: ${rmd:N0} required (${balance:N0} balance)");
                }
            }
            Console.ResetColor();
        };
        
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
        
        // 2. Process Social Security income if eligible and claiming
        double monthlySocialSecurityIncome = 0;
        if (age >= person.SocialSecurityClaimingAge && age >= 62)
        {
            double multiplier = SocialSecurityBenefitCalculator.CalculateBenefitMultiplier(
                person.SocialSecurityClaimingAge, person.FullRetirementAge);
            monthlySocialSecurityIncome = person.SocialSecurityIncome * multiplier;
        }
        
        // 3. Calculate healthcare costs based on age and Medicare eligibility
        double monthlyHealthcareCosts = CalculateMonthlyHealthcareCosts(age) / 12.0;
        
        // 4. Calculate total monthly expenses including healthcare
        double baseMonthlyExpenses = (person.EssentialExpenses + person.DiscretionarySpending) / 12;
        double totalMonthlyExpenses = baseMonthlyExpenses + monthlyHealthcareCosts;
        
        // 5. Process Required Minimum Distributions if applicable
        double monthlyRMDIncome = ProcessRequiredMinimumDistributions(person, date, age);
        
        // 6. Calculate net income vs expenses
        double totalMonthlyIncome = monthlySocialSecurityIncome + monthlyRMDIncome;
        double monthlyShortfall = totalMonthlyExpenses - totalMonthlyIncome;
        
        // 7. If there's a shortfall, implement comprehensive withdrawal strategy
        if (monthlyShortfall > 0)
        {
            ExecuteOptimalWithdrawalStrategy(person, monthlyShortfall, date, age);
        }
        
        // 8. If there's surplus and still working, consider additional contributions
        else if (monthlyShortfall < 0 && age < person.FullRetirementAge)
        {
            double surplus = -monthlyShortfall;
            ConsiderAdditionalRetirementContributions(person, surplus, date, age);
        }
    }
    
    /// <summary>
    /// Comprehensive job pay event processing with optimal contribution strategies
    /// </summary>
    private static void ProcessJobPayEvent(Person person, JobPayEventArgs e)
    {
        int age = person.CurrentAge(e.Date);
        
        // Calculate optimal contribution amounts based on current limits and tax strategy
        var contributionStrategy = CalculateOptimalContributionStrategy(person, e.Job, e.GrossIncome, age, e.Date.Year);
        
        // Process 401(k) contributions
        foreach (var account in person.Investments.Accounts)
        {
            if (account is Traditional401kAccount t401k && contributionStrategy.Traditional401k > 0)
            {
                // Personal contribution
                double actualPersonal = t401k.Deposit(contributionStrategy.Traditional401k, e.Date, TransactionCategory.ContributionPersonal);
                
                // Company match (up to match percentage)
                double maxMatch = e.GrossIncome * (e.Job.CompanyMatchContributionPercent ?? 0) / 100;
                double actualMatch = t401k.Deposit(maxMatch, e.Date, TransactionCategory.ContributionEmployer);
                
                if (actualPersonal < contributionStrategy.Traditional401k)
                {
                    Console.WriteLine($"   401(k) contribution limited: ${actualPersonal:N0} (wanted ${contributionStrategy.Traditional401k:N0})");
                }
            }
            else if (account is Roth401kAccount r401k && contributionStrategy.Roth401k > 0)
            {
                // Roth 401(k) contribution
                double actualRoth = r401k.Deposit(contributionStrategy.Roth401k, e.Date, TransactionCategory.ContributionPersonal);
                
                if (actualRoth < contributionStrategy.Roth401k)
                {
                    Console.WriteLine($"   Roth 401(k) contribution limited: ${actualRoth:N0} (wanted ${contributionStrategy.Roth401k:N0})");
                }
            }
        }
        
        // Process IRA contributions if there's remaining capacity
        if (contributionStrategy.RothIRA > 0)
        {
            var rothIRA = person.Investments.Accounts.FirstOrDefault(a => a is RothIRAAccount) as RothIRAAccount;
            if (rothIRA != null)
            {
                rothIRA.Deposit(contributionStrategy.RothIRA, e.Date, TransactionCategory.ContributionPersonal);
            }
        }
    }
    
    /// <summary>
    /// Calculate optimal contribution strategy based on tax situation and limits
    /// </summary>
    private static (double Traditional401k, double Roth401k, double RothIRA) CalculateOptimalContributionStrategy(
        Person person, IncomeSource job, double grossIncome, int age, int year)
    {
        // Get current contribution limits
        double limit401k = ContributionLimits.Get401kPersonalLimit(year, age);
        double limitIRA = ContributionLimits.GetIRALimit(year, age);
        
        // Calculate desired contribution percentages
        double retirementPercent = job.RetirementContributionPercent ?? 0;
        double totalDesiredRetirement = grossIncome * (retirementPercent / 100);
        
        // Cap at legal limits
        totalDesiredRetirement = Math.Min(totalDesiredRetirement, limit401k);
        
        // Tax-based strategy: Use traditional 401(k) for higher tax brackets, Roth for lower
        double estimatedTaxRate = EstimateCurrentTaxRate(person, year);
        
        double traditional401k = 0;
        double roth401k = 0;
        double rothIRA = 0;
        
        if (estimatedTaxRate > 0.22) // High tax bracket - prefer traditional
        {
            traditional401k = totalDesiredRetirement;
        }
        else if (estimatedTaxRate < 0.12) // Low tax bracket - prefer Roth
        {
            roth401k = totalDesiredRetirement;
            // Also max out Roth IRA if possible
            rothIRA = Math.Min(limitIRA, grossIncome * 0.05); // 5% to Roth IRA
        }
        else // Middle bracket - split strategy
        {
            traditional401k = totalDesiredRetirement * 0.7;
            roth401k = totalDesiredRetirement * 0.3;
        }
        
        return (traditional401k, roth401k, rothIRA);
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
                        
                        // Check for RMD penalty if not enough withdrawn
                        if (actualWithdrawn < monthlyRMD)
                        {
                            double penalty = RMDCalculator.CalculateRMDPenalty(monthlyRMD, actualWithdrawn);
                            if (penalty > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"   RMD Penalty: ${penalty:N0} for insufficient RMD from {account.Name}");
                                Console.ResetColor();
                            }
                        }
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
        var withdrawalPlan = CreateOptimalWithdrawalPlan(person, amountNeeded, date, age);
        
        foreach (var withdrawal in withdrawalPlan)
        {
            if (remainingNeeded <= 0) break;
            
            var account = person.Investments.Accounts.FirstOrDefault(a => a.Name == withdrawal.AccountName);
            if (account == null) continue;
            
            double toWithdraw = Math.Min(withdrawal.Amount, remainingNeeded);
            double actualWithdrawn = account.Withdraw(toWithdraw, date, TransactionCategory.Expenses);
            
            remainingNeeded -= actualWithdrawn;
            
            // Calculate and display tax implications
            double taxOwed = CalculateWithdrawalTaxes(account.Type, actualWithdrawn, person, date.Year);
            if (taxOwed > 0)
            {
                Console.WriteLine($"   Tax on ${actualWithdrawn:N0} from {account.Name}: ${taxOwed:N0}");
            }
            
            // Calculate early withdrawal penalties if applicable using comprehensive logic
            double penalty = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(
                account.Type, actualWithdrawn, age, null, WithdrawalReason.GeneralDistribution);
            
            if (penalty > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   Early withdrawal penalty: ${penalty:N0}");
                Console.ResetColor();
            }
        }
        
        if (remainingNeeded > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"⚠️  Shortfall: ${remainingNeeded:N0} could not be withdrawn");
            Console.ResetColor();
        }
    }
    
    /// <summary>
    /// Create optimal withdrawal plan considering tax efficiency and penalties
    /// </summary>
    private static List<(string AccountName, double Amount)> CreateOptimalWithdrawalPlan(
        Person person, double totalNeeded, DateOnly date, int age)
    {
        var plan = new List<(string AccountName, double Amount)>();
        var accounts = person.Investments.Accounts
            .Where(a => a.Balance(date) > 0)
            .OrderBy(a => CalculateWithdrawalCost(a, age, person, date.Year))
            .ToList();
        
        double remaining = totalNeeded;
        
        foreach (var account in accounts)
        {
            if (remaining <= 0) break;
            
            double available = account.Balance(date);
            double toTake = Math.Min(remaining, available);
            
            if (toTake > 0)
            {
                plan.Add((account.Name, toTake));
                remaining -= toTake;
            }
        }
        
        return plan;
    }
    
    /// <summary>
    /// Calculate the total cost (taxes + penalties) of withdrawing from an account
    /// </summary>
    private static double CalculateWithdrawalCost(InvestmentAccount account, int age, Person person, int year)
    {
        double withdrawalAmount = 1000; // Test amount for comparison
        
        // Tax cost
        double taxCost = CalculateWithdrawalTaxes(account.Type, withdrawalAmount, person, year);
        
        // Penalty cost using comprehensive logic
        double penaltyCost = EarlyWithdrawalPenaltyCalculator.CalculatePenalty(
            account.Type, withdrawalAmount, age, null, WithdrawalReason.GeneralDistribution);
        
        return taxCost + penaltyCost;
    }
    
    /// <summary>
    /// Calculate taxes owed on retirement account withdrawals
    /// </summary>
    private static double CalculateWithdrawalTaxes(AccountType accountType, double amount, Person person, int year)
    {
        return accountType switch
        {
            AccountType.Traditional401k => amount * EstimateCurrentTaxRate(person, year),
            AccountType.TraditionalIRA => amount * EstimateCurrentTaxRate(person, year),
            AccountType.Traditional403b => amount * EstimateCurrentTaxRate(person, year),
            AccountType.Roth401k => 0, // Tax-free after 59½ and 5-year rule
            AccountType.RothIRA => 0,  // Tax-free after 59½ and 5-year rule
            AccountType.Taxable => amount * 0.15, // Assume 15% capital gains
            AccountType.Savings => 0, // No taxes on principal
            _ => 0
        };
    }
    
    /// <summary>
    /// Estimate current marginal tax rate based on comprehensive income analysis
    /// </summary>
    private static double EstimateCurrentTaxRate(Person person, int year)
    {
        double annualIncome = person.IncomeYearly;
        
        // Use the comprehensive TaxBrackets for accurate marginal rate estimation
        var brackets = TaxBrackets.Brackets[person.FileType];
        
        // Calculate the marginal tax rate by finding which bracket the income falls into
        foreach (var bracket in brackets)
        {
            if (annualIncome >= bracket.LowerBound && annualIncome <= bracket.UpperBound)
            {
                return bracket.Rate;
            }
        }
        
        // Fallback to highest bracket rate
        return brackets.LastOrDefault()?.Rate ?? 0.22;
    }
    
    /// <summary>
    /// Calculate monthly healthcare costs based on age and Medicare eligibility
    /// </summary>
    private static double CalculateMonthlyHealthcareCosts(int age)
    {
        return HealthcareCostCalculator.CalculateAnnualHealthcareCosts(age, false) / 12.0;
    }
    
    /// <summary>
    /// Consider additional retirement contributions if there's surplus income
    /// </summary>
    private static void ConsiderAdditionalRetirementContributions(Person person, double surplus, DateOnly date, int age)
    {
        // If there's surplus and we haven't maxed contributions, consider additional retirement savings
        int currentYear = date.Year;
        double limit401k = ContributionLimits.Get401kPersonalLimit(currentYear, age);
        double limitIRA = ContributionLimits.GetIRALimit(currentYear, age);
        
        // Check current year contributions
        foreach (var account in person.Investments.Accounts)
        {
            if (account is RothIRAAccount rothIRA)
            {
                double currentYearContributions = account.DepositHistory
                    .Where(d => d.Date.Year == currentYear && d.Category == TransactionCategory.ContributionPersonal)
                    .Sum(d => d.Amount);
                
                double remainingIRARoom = limitIRA - currentYearContributions;
                if (remainingIRARoom > 0 && surplus > 0)
                {
                    double additionalContribution = Math.Min(surplus, remainingIRARoom);
                    rothIRA.Deposit(additionalContribution, date, TransactionCategory.ContributionPersonal);
                    surplus -= additionalContribution;
                    
                    Console.WriteLine($"   Additional Roth IRA contribution: ${additionalContribution:N0}");
                }
            }
        }
    }
    
    private static void WithdrawForExpenses(Person person, double monthlyExpenses, DateOnly date)
    {
        // This method is now replaced by ExecuteOptimalWithdrawalStrategy
        // Keep for backward compatibility but redirect to comprehensive logic
        int age = person.CurrentAge(date);
        ExecuteOptimalWithdrawalStrategy(person, monthlyExpenses, date, age);
    }
    
    private static int GetWithdrawalPriority(AccountType accountType)
    {
        return accountType switch
        {
            AccountType.Taxable => 1,
            AccountType.Savings => 2,
            AccountType.RothIRA => 3,
            AccountType.Roth401k => 4,
            AccountType.Traditional401k => 5,
            AccountType.TraditionalIRA => 6,
            _ => 10
        };
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
