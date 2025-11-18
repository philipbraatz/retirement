namespace RetirementPlanner;

public class InvestmentManager(IEnumerable<InvestmentAccount> accounts)
{
    public List<InvestmentAccount> Accounts { get; set; } = accounts.ToList();

    public void PerformRothConversion(Person person, DateOnly date)
    {
        double conversionLimit = TaxBrackets.GetOptimalRothConversionAmount(person, date);
        if (conversionLimit <= 0) return; // No conversion if already in high tax bracket

        var traditionalAccounts = Accounts.Where(a => a is Traditional401kAccount && a.Balance(date) > 0);
        var rothAccount = Accounts.FirstOrDefault(a => a is RothIRAAccount);

        if (rothAccount == null || !traditionalAccounts.Any()) return; // No Roth IRA available

        double annualExpenses = person.EssentialExpenses + person.DiscretionarySpending;
        double fiveYearRequiredBalance = annualExpenses * 5;
        double availableBalance = traditionalAccounts.Sum(a => a.Balance(date)) + rothAccount.Balance(date);

        double totalConverted = 0;
        foreach (var account in traditionalAccounts)
        {
            if (conversionLimit <= 0) break;

            // Ensure 5-Year Safety Net Before Converting
            double maxSafeConversion = availableBalance - fiveYearRequiredBalance;
            double conversionAmount = Math.Min(Math.Min(account.Balance(date), conversionLimit), maxSafeConversion);

            if (conversionAmount > 0)
            {
                account.Withdraw(conversionAmount, date, TransactionCategory.InternalTransfer);
                rothAccount.Deposit(conversionAmount, date, TransactionCategory.InternalTransfer);
                conversionLimit -= conversionAmount;
                totalConverted += conversionAmount;
            }
        }
        
        if (totalConverted > 0)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"🔄 Roth Conversion: {totalConverted:C} (Traditional → Roth IRA for future tax-free growth)");
            Console.ResetColor();
        }
    }

    public void ApplyMonthlyGrowth(DateOnly date) => Accounts.ForEach(a => a.ApplyMonthlyGrowth(date));
}