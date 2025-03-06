using System.Text;

namespace RetirementPlanner;

public static class Helpers
{
    public static void ExportToCSV(List<MonthlyAccountSummary> history, string filename)
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Date,Account,Deposits,Withdrawals,TotalBalance");

        foreach (var record in history)
        {
            csv.AppendLine($"{record.Date:yyyy-MM-dd},{record.AccountName},{record.Deposits},{record.Withdrawals},{record.TotalBalance}");
        }

        File.WriteAllText(filename, csv.ToString());
        Console.WriteLine($"Data exported to {filename}");
    }
}

public class MonthlyAccountSummary
{
    public DateOnly Date { get; set; }
    public string AccountName { get; set; }
    public double Deposits { get; set; }
    public double Withdrawals { get; set; }
    public double TotalBalance { get; set; }
}
