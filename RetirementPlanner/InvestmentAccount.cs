namespace RetirementPlanner;

public class FinancialSnapshot
{
    public DateTime Date { get; set; }
    public double Salary { get; set; }
    public double SocialSecurityIncome { get; set; }
    public double TotalIncome { get; set; }
    public double EssentialExpenses { get; set; }
    public double DiscretionarySpending { get; set; }
    public double TotalExpenses { get; set; }
    public double Withdrawals { get; set; }
    public double SurplusSaved { get; set; }
    public double SurplusBalance { get; set; }
    public double Account401kBalance { get; set; }
    public double RothIRABalance { get; set; }
    public double TaxableBalance { get; set; }
}


public class InvestmentAccount(double annualGrowthRate, string name, AccountType type, Person owner)
{
    public string Name { get; } = name;
    public double Balance { get; private set; }
    public double AnnualGrowthRate { get; } = annualGrowthRate;
    public double MonthlyContribution { get; set; }
    public AccountType Type { get; } = type;
    public Person Owner { get; } = owner;

    // Track taxable withdrawals separately for annual taxation
    public double TaxableWithdrawalsThisYear { get; private set; } = 0;

    public void ApplyMonthlyGrowth()
    {
        double monthlyRate = Math.Pow(1 + AnnualGrowthRate, 1.0 / 12) - 1;
        Balance *= (1 + monthlyRate);
    }

    public double Withdraw(double amount)
    {
        if (Balance <= 0) return 0;

        double amountWithdrawn = Math.Min(Balance, amount);
        double penalty = 0;
        double taxBurden = 0;

        // Apply penalties for early withdrawals (before age 59.5)
        if ((Type == AccountType.Traditional401k || Type == AccountType.TraditionalIRA) && Owner.CurrentAge < 59.5)
        {
            penalty = amountWithdrawn * 0.10;  // 10% early withdrawal penalty
            amountWithdrawn -= penalty;
        }

        // Track taxable withdrawals for year-end tax calculation
        if (Type == AccountType.Traditional401k || Type == AccountType.TraditionalIRA)
        {
            TaxableWithdrawalsThisYear += amountWithdrawn;
        }

        // Withdraw and return actual amount received after penalties
        Balance -= amountWithdrawn + penalty;
        return amountWithdrawn;
    }

    public double Deposit(double amount)
    {
        Balance += amount;
        return Balance;
    }

    public void ResetYearlyTaxTracking()
    {
        TaxableWithdrawalsThisYear = 0;
    }
}

public enum AccountType
{
    Traditional401k,
    TraditionalIRA,
    RothIRA,
    Taxable
}
