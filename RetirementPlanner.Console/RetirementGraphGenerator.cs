using ScottPlot;
using RetirementPlanner;

namespace RetirementPlanner.ConsoleApp;

public class RetirementGraphGenerator
{
    private class SimulationDataPoint
    {
        public DateOnly Date { get; set; }
        public int Age { get; set; }
        public double Traditional401k { get; set; }
        public double Roth401k { get; set; }
        public double RothIRA { get; set; }
        public double Savings { get; set; }
        public double TotalAssets { get; set; }
        public double MonthlyIncome { get; set; }
        public double MonthlyExpenses { get; set; }
        public double NetWorth { get; set; }
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

        foreach (var account in accounts.Values)
        {
            if (account is Traditional401kAccount)
                traditional401k = account.Balance(date);
            else if (account is Roth401kAccount)
                roth401k = account.Balance(date);
            else if (account is RothIRAAccount)
                rothIRA = account.Balance(date);
            else if (account.Name == "Savings")
                savings = account.Balance(date);
        }
        
        var totalAssets = traditional401k + roth401k + rothIRA + savings;
        var age = CalculateAge(date);

        _dataPoints.Add(new SimulationDataPoint
        {
            Date = date,
            Age = age,
            Traditional401k = traditional401k,
            Roth401k = roth401k,
            RothIRA = rothIRA,
            Savings = savings,
            TotalAssets = totalAssets,
            MonthlyIncome = monthlyIncome,
            MonthlyExpenses = monthlyExpenses,
            NetWorth = totalAssets
        });
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
        
        // Prepare data arrays
        var ages = _dataPoints.Select(dp => (double)dp.Age).ToArray();
        var traditional401k = _dataPoints.Select(dp => dp.Traditional401k / 1000).ToArray(); // Convert to thousands
        var roth401k = _dataPoints.Select(dp => dp.Roth401k / 1000).ToArray();
        var rothIRA = _dataPoints.Select(dp => dp.RothIRA / 1000).ToArray();
        var savings = _dataPoints.Select(dp => dp.Savings / 1000).ToArray();
        var totalAssets = _dataPoints.Select(dp => dp.TotalAssets / 1000).ToArray();

        // Add account balance lines
        var tradPlot = plt.Add.Scatter(ages, traditional401k);
        tradPlot.LegendText = "Traditional 401k";
        tradPlot.Color = ScottPlot.Color.FromHex("#1f77b4"); // Blue
        tradPlot.LineWidth = 2;

        var rothPlot = plt.Add.Scatter(ages, roth401k);
        rothPlot.LegendText = "Roth 401k";
        rothPlot.Color = ScottPlot.Color.FromHex("#ff7f0e"); // Orange
        rothPlot.LineWidth = 2;

        var iraPlot = plt.Add.Scatter(ages, rothIRA);
        iraPlot.LegendText = "Roth IRA";
        iraPlot.Color = ScottPlot.Color.FromHex("#2ca02c"); // Green
        iraPlot.LineWidth = 2;

        var savingsPlot = plt.Add.Scatter(ages, savings);
        savingsPlot.LegendText = "Savings";
        savingsPlot.Color = ScottPlot.Color.FromHex("#d62728"); // Red
        savingsPlot.LineWidth = 2;

        var totalPlot = plt.Add.Scatter(ages, totalAssets);
        totalPlot.LegendText = "Total Assets";
        totalPlot.Color = ScottPlot.Color.FromHex("#9467bd"); // Purple
        totalPlot.LineWidth = 3;
        totalPlot.LinePattern = LinePattern.Dashed;

        // Add milestone markers
        AddMilestoneMarkers(plt, ages);

        // Style the plot
        plt.Title($"Retirement Simulation");
        plt.XLabel("Age");
        plt.YLabel("Account Balance (Thousands $)");
        
        // Enable legend
        plt.ShowLegend();

        // Set reasonable axis limits
        var minAge = ages.Min();
        var maxAge = ages.Max();
        var maxBalance = totalAssets.Max();
        
        plt.Axes.SetLimitsX(minAge - 1, maxAge + 1);
        plt.Axes.SetLimitsY(0, maxBalance * 1.1);

        // Add grid
        plt.Grid.MajorLineColor = ScottPlot.Color.FromHex("#E0E0E0");
        //plt.Grid.Enable();

        // Save the plot
        plt.SavePng(outputPath, 1200, 800);
        
        System.Console.WriteLine($"\n📊 Graph saved to: {outputPath}");
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
        
        var ages = _dataPoints.Select(dp => (double)dp.Age).ToArray();
        var income = _dataPoints.Select(dp => dp.MonthlyIncome).ToArray();
        var expenses = _dataPoints.Select(dp => dp.MonthlyExpenses).ToArray();

        if (income.Any(i => i > 0) || expenses.Any(e => e > 0))
        {
            var incomePlot = plt.Add.Scatter(ages, income);
            incomePlot.LegendText = "Monthly Income";
            incomePlot.Color = ScottPlot.Color.FromHex("#2ca02c"); // Green
            incomePlot.LineWidth = 2;

            var expensePlot = plt.Add.Scatter(ages, expenses);
            expensePlot.LegendText = "Monthly Expenses";
            expensePlot.Color = ScottPlot.Color.FromHex("#d62728"); // Red
            expensePlot.LineWidth = 2;

            plt.Title($"Income vs Expenses Over Time");
            plt.XLabel("Age");
            plt.YLabel("Monthly Amount ($)");
            plt.ShowLegend();
            //plt.Grid.Enable();

            plt.SavePng(outputPath, 1200, 600);
            System.Console.WriteLine($"📊 Income/Expense graph saved to: {outputPath}");
        }
    }

    private void AddMilestoneMarkers(Plot plt, double[] ages)
    {
        var milestones = new List<(int age, string label, ScottPlot.Color color)>
        {
            (_person.FullRetirementAge, "Full Retirement", ScottPlot.Color.FromHex("#ff7f0e")),
            (70, "Social Security", ScottPlot.Color.FromHex("#2ca02c")),
            (73, "RMD Start", ScottPlot.Color.FromHex("#d62728"))
        };

        foreach (var (age, label, color) in milestones)
        {
            if (age >= ages.Min() && age <= ages.Max())
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

    private int CalculateAge(DateOnly date)
    {
        var birthDate = DateOnly.FromDateTime(_person.BirthDate);
        var age = date.Year - birthDate.Year;
        if (date < birthDate.AddYears(age))
            age--;
        return age;
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

        System.Console.WriteLine("\n=== Simulation Summary ===");
        System.Console.WriteLine($"Age Range: {firstPoint.Age} - {lastPoint.Age}");
        System.Console.WriteLine($"Starting Assets: ${firstPoint.TotalAssets:C}");
        System.Console.WriteLine($"Ending Assets: ${lastPoint.TotalAssets:C}");
        System.Console.WriteLine($"Peak Assets: ${maxAssets:C} at age {maxAssetsAge}");
        System.Console.WriteLine($"Data Points Collected: {_dataPoints.Count}");
    }

    public void PrintSimpleASCIIChart()
    {
        if (_dataPoints.Count == 0)
        {
            System.Console.WriteLine("No data points to display.");
            return;
        }

        System.Console.WriteLine("\n=== Account Balance Progression (ASCII Chart) ===");
        
        var maxAssets = _dataPoints.Max(dp => dp.TotalAssets);
        var chartHeight = 20;
        var chartWidth = Math.Min(80, _dataPoints.Count);
        
        // Sample data points to fit chart width
        var sampledPoints = _dataPoints.Take(chartWidth).ToList();
        if (_dataPoints.Count > chartWidth)
        {
            var step = _dataPoints.Count / chartWidth;
            sampledPoints = _dataPoints.Where((dp, i) => i % step == 0).Take(chartWidth).ToList();
        }

        System.Console.WriteLine($"Total Assets over Time (Max: ${maxAssets:C})");
        System.Console.WriteLine("Age: " + string.Join("", sampledPoints.Select(dp => (dp.Age % 10).ToString())));
        
        for (int row = chartHeight; row >= 0; row--)
        {
            var threshold = (maxAssets * row) / chartHeight;
            var line = "";
            
            foreach (var point in sampledPoints)
            {
                line += point.TotalAssets >= threshold ? "█" : " ";
            }
            
            if (row % 5 == 0)
                System.Console.WriteLine($"{threshold/1000,3:F0}k|{line}");
            else
                System.Console.WriteLine($"    |{line}");
        }
        
        System.Console.WriteLine("    +" + new string('-', chartWidth));
        System.Console.WriteLine($"    Ages {sampledPoints.First().Age}-{sampledPoints.Last().Age}");
    }
}
