using System.Text;

namespace RetirementPlanner;

public static class Helpers
{
    public static void ExportToCSV(List<FinancialSnapshot> history, string filename)
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Date,Salary,SocialSecurityIncome,TotalIncome,EssentialExpenses,DiscretionarySpending,TotalExpenses,Withdrawals,SurplusSaved,SurplusBalance,Account401kBalance,RothIRABalance,TaxableBalance");

        foreach (var record in history)
        {
            csv.AppendLine($"{record.Date:yyyy-MM-dd},{record.Salary},{record.SocialSecurityIncome},{record.TotalIncome},{record.EssentialExpenses},{record.DiscretionarySpending},{record.TotalExpenses},{record.Withdrawals},{record.SurplusSaved},{record.SurplusBalance},{record.Account401kBalance},{record.RothIRABalance},{record.TaxableBalance}");
        }

        File.WriteAllText(filename, csv.ToString());
        Console.WriteLine($"Data exported to {filename}");
    }
}