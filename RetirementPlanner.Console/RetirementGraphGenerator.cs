using ScottPlot;
using RetirementPlanner;

namespace RetirementPlanner.ConsoleApp;

public class RetirementGraphGenerator
{
    // Configurable scaling constant for dollar amounts
    private const double DOLLAR_SCALE = 1000.0; // Default to 1000 for thousands display
    
    private class SimulationDataPoint
    {
        public DateOnly Date { get; set; }
        public int Age { get; set; }
        public double PreciseAge { get; set; } // Add precise age for smoother graphing
        public double Traditional401k { get; set; }
        public double Roth401k { get; set; }
        public double RothIRA { get; set; }
        public double Savings { get; set; }
        public double PocketCash { get; set; } // New: pocket cash tracked separately
        public double TotalAssets { get; set; }
        public double MonthlyIncome { get; set; }
        public double MonthlyExpenses { get; set; }
        public double NetWorth { get; set; }
        public double EarlyWithdrawalPenalties { get; set; }
        public double NormalWithdrawals { get; set; }
        public double TotalWithdrawals { get; set; }
    }

    private readonly List<SimulationDataPoint> _dataPoints = new();
    private readonly Person _person;

    public RetirementGraphGenerator(Person person)
    {
        _person = person;
    }

    public void RecordDataPoint(DateOnly date, Dictionary<string, InvestmentAccount> accounts, 
        double monthlyIncome = 0, double monthlyExpenses = 0)
    {
        double traditional401k = 0;
        double roth401k = 0;
        double rothIRA = 0;
        double savings = 0;
        double pocketCash = 0;

        foreach (var account in accounts.Values)
        {
            double balance = account.Balance(date);
            if (account is Traditional401kAccount)
                traditional401k = balance;
            else if (account is Roth401kAccount)
                roth401k = balance;
            else if (account is RothIRAAccount)
                rothIRA = balance;
            else if (account.Name == "Savings")
                savings = balance;
            else if (account.Name == "Pocket Cash")
                pocketCash = balance;
        }
        var totalAssets = traditional401k + roth401k + rothIRA + savings + pocketCash;
        var age = CalculateAge(date);
        var preciseAge = CalculatePreciseAge(date);

        // Calculate withdrawal data for this period
        var (earlyWithdrawalPenalties, normalWithdrawals, totalWithdrawals) = CalculateWithdrawalData(accounts, date);

        // Avoid duplicate data points for the same precise age (with small tolerance)
        if (_dataPoints.Any() && Math.Abs(_dataPoints.Last().PreciseAge - preciseAge) < 0.01)
        {
            return; // Skip this data point as it's too close to the previous one
        }

        _dataPoints.Add(new SimulationDataPoint
        {
            Date = date,
            Age = age,
            PreciseAge = preciseAge,
            Traditional401k = traditional401k,
            Roth401k = roth401k,
            RothIRA = rothIRA,
            Savings = savings,
            PocketCash = pocketCash,
            TotalAssets = totalAssets,
            MonthlyIncome = monthlyIncome,
            MonthlyExpenses = monthlyExpenses,
            NetWorth = totalAssets,
            EarlyWithdrawalPenalties = earlyWithdrawalPenalties,
            NormalWithdrawals = normalWithdrawals,
            TotalWithdrawals = totalWithdrawals
        });
    }

    private (double earlyWithdrawalPenalties, double normalWithdrawals, double totalWithdrawals) CalculateWithdrawalData(
        Dictionary<string, InvestmentAccount> accounts, DateOnly date)
    {
        double earlyWithdrawalPenalties = 0;
        double normalWithdrawals = 0;
        double totalWithdrawals = 0;

        foreach (var account in accounts.Values)
        {
            // Get withdrawals for this month
            var monthlyWithdrawals = account.WithdrawalHistory
                .Where(w => w.Date.Year == date.Year && w.Date.Month == date.Month)
                .ToList();

            // Separate early withdrawal penalties from normal withdrawals
            var penalties = monthlyWithdrawals
                .Where(w => w.Category == TransactionCategory.EarlyWithdrawalPenality)
                .Sum(w => w.Amount);

            var normal = monthlyWithdrawals
                .Where(w => w.Category != TransactionCategory.EarlyWithdrawalPenality)
                .Sum(w => w.Amount);

            earlyWithdrawalPenalties += penalties;
            normalWithdrawals += normal;
            totalWithdrawals += penalties + normal;
        }

        return (earlyWithdrawalPenalties, normalWithdrawals, totalWithdrawals);
    }

    public void GenerateComprehensiveGraph(string outputPath = "retirement_simulation_graph.png")
    {
        if (_dataPoints.Count == 0)
        {
            System.Console.WriteLine("No data points to graph.");
            return;
        }

        // Create the plot
        var plt = new Plot();
        
        // Prepare data arrays using precise ages for smoother curves and consistent scaling
        var ages = _dataPoints.Select(dp => dp.PreciseAge).ToArray();
        var traditional401k = _dataPoints.Select(dp => dp.Traditional401k / DOLLAR_SCALE).ToArray();
        var roth401k = _dataPoints.Select(dp => dp.Roth401k / DOLLAR_SCALE).ToArray();
        var rothIRA = _dataPoints.Select(dp => dp.RothIRA / DOLLAR_SCALE).ToArray();
        var savings = _dataPoints.Select(dp => dp.Savings / DOLLAR_SCALE).ToArray();
        var pocketCashVals = _dataPoints.Select(dp => dp.PocketCash).ToArray(); // unscaled for right axis
        var totalAssets = _dataPoints.Select(dp => dp.TotalAssets / DOLLAR_SCALE).ToArray();

        // Add account balance lines with smooth curves
        var tradPlot = plt.Add.Scatter(ages, traditional401k);
        tradPlot.LegendText = "Traditional 401k";
        tradPlot.Color = ScottPlot.Color.FromHex("#1f77b4"); // Blue
        tradPlot.LineWidth = 2;
        tradPlot.MarkerSize = 0; // Remove markers for cleaner lines

        var rothPlot = plt.Add.Scatter(ages, roth401k);
        rothPlot.LegendText = "Roth 401k";
        rothPlot.Color = ScottPlot.Color.FromHex("#ff7f0e"); // Orange
        rothPlot.LineWidth = 2;
        rothPlot.MarkerSize = 0;

        var iraPlot = plt.Add.Scatter(ages, rothIRA);
        iraPlot.LegendText = "Roth IRA";
        iraPlot.Color = ScottPlot.Color.FromHex("#2ca02c"); // Green
        iraPlot.LineWidth = 2;
        iraPlot.MarkerSize = 0;

        var savingsPlot = plt.Add.Scatter(ages, savings);
        savingsPlot.LegendText = "Savings";
        savingsPlot.Color = ScottPlot.Color.FromHex("#d62728"); // Red
        savingsPlot.LineWidth = 2;
        savingsPlot.MarkerSize = 0;

        // Pocket Cash on right Y axis (own scale)
        var pocketPlot = plt.Add.Scatter(ages, pocketCashVals);
        pocketPlot.LegendText = "Pocket Cash";
        pocketPlot.Color = ScottPlot.Color.FromHex("#b8860b"); // DarkGoldenRod
        pocketPlot.LineWidth = 2;
        pocketPlot.MarkerSize = 0;

        var rightAxis = plt.Axes.AddRightAxis();
        rightAxis.Label.Text = "Pocket Cash ($)";
        rightAxis.Color(pocketPlot.Color);
        pocketPlot.Axes.YAxis = rightAxis;

        var totalPlot = plt.Add.Scatter(ages, totalAssets);
        totalPlot.LegendText = "Total Assets";
        totalPlot.Color = ScottPlot.Color.FromHex("#9467bd"); // Purple
        totalPlot.LineWidth = 3;
        totalPlot.LinePattern = LinePattern.Dashed;
        totalPlot.MarkerSize = 0;

        // Add milestone markers including early retirement
        AddMilestoneMarkers(plt, ages);

        // Add early withdrawal indicators
        AddEarlyWithdrawalIndicators(plt, ages);

        // Style the plot with dynamic Y-label based on scale
        plt.Title($"Retirement Simulation with Early Retirement & Withdrawal Analysis");
        plt.XLabel("Age");
        plt.YLabel(GetYAxisLabel());
        
        // Enable legend
        plt.ShowLegend();

        // Set reasonable axis limits
        var minAge = ages.Min();
        var maxAge = ages.Max();
        var maxBalance = totalAssets.Max();
        
        plt.Axes.SetLimitsX(minAge - 1, maxAge + 1);
        plt.Axes.SetLimitsY(0, maxBalance * 1.1);

        // Add grid for better readability
        plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E0E0E0");

        // Save the plot
        plt.SavePng(outputPath, 1200, 800);
        
        System.Console.WriteLine($"\n📊 Graph saved to: {outputPath}");
        System.Console.WriteLine($"   Data points: {_dataPoints.Count} (Age range: {minAge:F1} - {maxAge:F1})");
        System.Console.WriteLine($"   Scale: 1 unit = ${DOLLAR_SCALE:N0}");
        System.Console.WriteLine("   Opening graph in default image viewer...");
        
        // Try to open the graph
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = outputPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"   Could not automatically open graph: {ex.Message}");
        }
    }

    public void GenerateIncomeExpenseGraph(string outputPath = "retirement_income_expenses.png")
    {
        if (_dataPoints.Count == 0)
        {
            System.Console.WriteLine("No data points to graph.");
            return;
        }

        var plt = new Plot();
        
        var ages = _dataPoints.Select(dp => dp.PreciseAge).ToArray();
        var income = _dataPoints.Select(dp => dp.MonthlyIncome).ToArray();
        var expenses = _dataPoints.Select(dp => dp.MonthlyExpenses).ToArray();

        if (income.Any(i => i > 0) || expenses.Any(e => e > 0))
        {
            var incomePlot = plt.Add.Scatter(ages, income);
            incomePlot.LegendText = "Monthly Income";
            incomePlot.Color = ScottPlot.Color.FromHex("#2ca02c"); // Green
            incomePlot.LineWidth = 2;
            incomePlot.MarkerSize = 0;

            var expensePlot = plt.Add.Scatter(ages, expenses);
            expensePlot.LegendText = "Monthly Expenses";
            expensePlot.Color = ScottPlot.Color.FromHex("#d62728"); // Red
            expensePlot.LineWidth = 2;
            expensePlot.MarkerSize = 0;

            plt.Title($"Income vs Expenses Over Time");
            plt.XLabel("Age");
            plt.YLabel("Monthly Amount ($)");
            plt.ShowLegend();
            // Grid styling
            plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E0E0E0");

            plt.SavePng(outputPath, 1200, 600);
            System.Console.WriteLine($"📊 Income/Expense graph saved to: {outputPath}");
        }
    }

    public void GenerateWithdrawalAnalysisGraph(string outputPath = "retirement_withdrawal_analysis.png")
    {
        if (_dataPoints.Count == 0)
        {
            System.Console.WriteLine("No data points to graph.");
            return;
        }

        var plt = new Plot();
        
        var ages = _dataPoints.Select(dp => dp.PreciseAge).ToArray();
        var earlyPenalties = _dataPoints.Select(dp => dp.EarlyWithdrawalPenalties).ToArray();
        var normalWithdrawals = _dataPoints.Select(dp => dp.NormalWithdrawals / DOLLAR_SCALE).ToArray();
        var totalWithdrawals = _dataPoints.Select(dp => dp.TotalWithdrawals / DOLLAR_SCALE).ToArray();

        // Only show data where there are actual withdrawals
        var hasWithdrawals = totalWithdrawals.Any(w => w > 0);
        var hasPenalties = earlyPenalties.Any(p => p > 0);

        if (hasWithdrawals)
        {
            var normalPlot = plt.Add.Scatter(ages, normalWithdrawals);
            normalPlot.LegendText = "Normal Withdrawals";
            normalPlot.Color = ScottPlot.Color.FromHex("#2ca02c"); // Green
            normalPlot.LineWidth = 2;
            normalPlot.MarkerSize = 0;

            var totalPlot = plt.Add.Scatter(ages, totalWithdrawals);
            totalPlot.LegendText = "Total Withdrawals";
            totalPlot.Color = ScottPlot.Color.FromHex("#1f77b4"); // Blue
            totalPlot.LineWidth = 2;
            totalPlot.LinePattern = LinePattern.Dashed;
            totalPlot.MarkerSize = 0;
        }

        if (hasPenalties)
        {
            var penaltyPlot = plt.Add.Scatter(ages, earlyPenalties);
            penaltyPlot.LegendText = "Early Withdrawal Penalties";
            penaltyPlot.Color = ScottPlot.Color.FromHex("#d62728"); // Red
            penaltyPlot.LineWidth = 3;
            penaltyPlot.MarkerSize = 8; // Keep markers for penalties to make them visible
        }

        // Add milestone markers
        AddMilestoneMarkers(plt, ages);

        plt.Title($"Withdrawal Analysis - Early Penalties vs Normal Withdrawals");
        plt.XLabel("Age");
        plt.YLabel($"Amount ({GetScaleLabel()} for withdrawals, $ for penalties)");
        plt.ShowLegend();
        // Grid styling
        plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E0E0E0");

        if (hasWithdrawals || hasPenalties)
        {
            plt.SavePng(outputPath, 1200, 600);
            System.Console.WriteLine($"📊 Withdrawal analysis graph saved to: {outputPath}");
        }
    }

    private void AddMilestoneMarkers(Plot plt, double[] ages)
    {
        var milestones = new List<(int age, string label, ScottPlot.Color color)>
        {
            (_person.RetirementAge, "Planned/Early Retirement", ScottPlot.Color.FromHex("#ff9500")),
            (_person.PartTimeAge, "Part-Time", ScottPlot.Color.FromHex("#ffa64d")),
            (_person.FullRetirementAge, "Full Retirement", ScottPlot.Color.FromHex("#ff7f0e")),
            (59, "No Penalty Age", ScottPlot.Color.FromHex("#9467bd")),
            (70, "Max Social Security", ScottPlot.Color.FromHex("#2ca02c")),
            (73, "RMD Start", ScottPlot.Color.FromHex("#d62728"))
        };

        foreach (var (age, label, color) in milestones)
        {
            if (age >= ages.Min() && age <= ages.Max() && age > 0) // Only show if age is set and within range
            {
                var line = plt.Add.VerticalLine(age);
                line.Color = color;
                line.LineWidth = 2;
                line.LinePattern = LinePattern.Dashed;
                
                var text = plt.Add.Text(label, age, plt.Axes.GetLimits().Top * 0.9);
                text.LabelFontColor = color;
                text.LabelFontSize = 10;
                text.LabelRotation = -90;
            }
        }
    }

    private void AddEarlyWithdrawalIndicators(Plot plt, double[] ages)
    {
        // Find points where early withdrawal penalties occurred
        var earlyWithdrawalPoints = new List<(double preciseAge, double amount)>();
        
        for (int i = 0; i < _dataPoints.Count; i++)
        {
            var point = _dataPoints[i];
            if (point.EarlyWithdrawalPenalties > 0)
            {
                // Get the account balance at this point for positioning (scaled)
                double balanceAtPenalty = point.TotalAssets / DOLLAR_SCALE;
                earlyWithdrawalPoints.Add((point.PreciseAge, balanceAtPenalty));
            }
        }

        // Add markers for early withdrawal penalties
        if (earlyWithdrawalPoints.Any())
        {
            var penaltyAges = earlyWithdrawalPoints.Select(p => p.preciseAge).ToArray();
            var penaltyPositions = earlyWithdrawalPoints.Select(p => p.amount * 1.05).ToArray(); // Position slightly above balance
            
            var penaltyMarkers = plt.Add.Scatter(penaltyAges, penaltyPositions);
            penaltyMarkers.LegendText = "Early Withdrawal Penalty";
            penaltyMarkers.Color = ScottPlot.Color.FromHex("#ff0000"); // Bright red
            penaltyMarkers.MarkerSize = 15;
            penaltyMarkers.LineWidth = 0; // No connecting lines, just markers
        }
    }

    private int CalculateAge(DateOnly date)
    {
        var birthDate = DateOnly.FromDateTime(_person.BirthDate);
        var age = date.Year - birthDate.Year;
        
        // More precise age calculation - subtract 1 if birthday hasn't occurred yet this year
        if (date.Month < birthDate.Month || (date.Month == birthDate.Month && date.Day < birthDate.Day))
            age--;
            
        return age;
    }
    
    /// <summary>
    /// Calculate precise age with decimal precision for smoother graphing
    /// </summary>
    private double CalculatePreciseAge(DateOnly date)
    {
        var birthDate = DateOnly.FromDateTime(_person.BirthDate);
        var baseAge = date.Year - birthDate.Year;
        
        // Calculate the fraction of the year completed
        var startOfYear = new DateOnly(date.Year, birthDate.Month, birthDate.Day);
        var endOfYear = new DateOnly(date.Year + 1, birthDate.Month, birthDate.Day);
        
        // If birthday hasn't occurred this year, use previous year as base
        if (date < startOfYear)
        {
            baseAge--;
            startOfYear = new DateOnly(date.Year - 1, birthDate.Month, birthDate.Day);
            endOfYear = new DateOnly(date.Year, birthDate.Month, birthDate.Day);
        }
        
        var daysSinceBirthday = date.DayNumber - startOfYear.DayNumber;
        var daysInYear = endOfYear.DayNumber - startOfYear.DayNumber;
        var yearFraction = (double)daysSinceBirthday / daysInYear;
        
        return baseAge + yearFraction;
    }

    /// <summary>
    /// Get the appropriate Y-axis label based on the current scale
    /// </summary>
    private string GetYAxisLabel()
    {
        return DOLLAR_SCALE switch
        {
            1.0 => "Account Balance ($)",
            1000.0 => "Account Balance (Thousands $)",
            1000000.0 => "Account Balance (Millions $)",
            _ => $"Account Balance (${DOLLAR_SCALE:N0} units)"
        };
    }

    /// <summary>
    /// Get the scale description for labels
    /// </summary>
    private string GetScaleLabel()
    {
        return DOLLAR_SCALE switch
        {
            1.0 => "$",
            1000.0 => "Thousands $",
            1000000.0 => "Millions $",
            _ => $"${DOLLAR_SCALE:N0} units"
        };
    }

    public void PrintDataSummary()
    {
        if (_dataPoints.Count == 0)
        {
            System.Console.WriteLine("No data points recorded.");
            return;
        }

        var firstPoint = _dataPoints.First();
        var lastPoint = _dataPoints.Last();
        var maxAssets = _dataPoints.Max(dp => dp.TotalAssets);
        var maxAssetsAge = _dataPoints.Where(dp => dp.TotalAssets == maxAssets).First().Age;

        // Calculate total early withdrawal penalties
        var totalEarlyPenalties = _dataPoints.Sum(dp => dp.EarlyWithdrawalPenalties);
        var earlyPenaltyOccurrences = _dataPoints.Count(dp => dp.EarlyWithdrawalPenalties > 0);

        System.Console.WriteLine("\n=== Simulation Summary ===");
        System.Console.WriteLine($"Age Range: {firstPoint.Age} - {lastPoint.Age}");
        System.Console.WriteLine($"Starting Assets: ${firstPoint.TotalAssets:C}");
        System.Console.WriteLine($"Ending Assets: ${lastPoint.TotalAssets:C}");
        System.Console.WriteLine($"Peak Assets: ${maxAssets:C} at age {maxAssetsAge}");
        System.Console.WriteLine($"Data Points Collected: {_dataPoints.Count}");
        System.Console.WriteLine($"Graph Scale: 1 unit = ${DOLLAR_SCALE:N0}");
        
        if (_person.PartTimeAge > 0)
        {
            System.Console.WriteLine($"Early Retirement Age: {_person.PartTimeAge}");
        }

        if (totalEarlyPenalties > 0)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"⚠️  Total Early Withdrawal Penalties: ${totalEarlyPenalties:C} ({earlyPenaltyOccurrences} occurrences)");
            System.Console.ResetColor();
        }
        else
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"✅ No Early Withdrawal Penalties Incurred");
            System.Console.ResetColor();
        }
    }
}
