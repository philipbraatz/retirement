namespace RetirementPlanner.Event;

public class PayTaxesEventArgs : EventArgs
{
    public DateOnly Date { get; set; }
    public double TaxesOwed { get; set; }
}

public class DatedEventArgs : EventArgs
{
    public DateOnly Date { get; set; }
    public int Age { get; set; }
}

public class MoneyShortfallEventArgs : EventArgs
{
    public DateOnly Date { get; set; }
    public TransactionCategory TransactionCategory { get; set; }
    public double ShortfallAmount { get; set; }
}

public class JobPayEventArgs : EventArgs
{
    public DateOnly Date { get; set; }
    public IncomeSource Job { get; set; }
    public double GrossIncome { get; set; }
}

public class SpendingEventArgs : EventArgs
{
    public DateOnly Date { get; set; }
    public double Amount { get; set; }
    public TransactionCategory TransactionCategory { get; set; }
}