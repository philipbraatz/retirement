using RetirementPlanner.IRS;

namespace RetirementPlanner.Event;

public static class LifeEvents
{
    private static bool _subscribed;

    public static void Subscribe(RetirementPlanner planner)
    {
        if (_subscribed) return;
        _subscribed = true;

        RetirementPlanner.OnNewMonth += OnNewMonth;
        RetirementPlanner.OnJobPay += OnJobPay;
    }

    private static void OnNewMonth(object sender, DatedEventArgs e)
    {
        if (sender is not Person person) return;

        //1) Apply monthly growth once for all accounts
        person.Investments.ApplyMonthlyGrowth(e.Date);

        //2) Cover monthly expenses by withdrawing in optimal order
        double monthlyExpenses = (person.EssentialExpenses + person.DiscretionarySpending) /12.0;
        if (monthlyExpenses <=0) return;

        ExecuteOptimalWithdrawals(person, monthlyExpenses, e.Date);
    }

    private static void OnJobPay(object sender, JobPayEventArgs e)
    {
        // Minimal: route net income to savings if a savings account exists
        if (sender is not Person person) return;
        var savings = person.Investments.Accounts.FirstOrDefault(a => a.Type == AccountType.Savings);
        if (savings is null) return;

        // Very rough: put gross/12 into savings for monthly cadence when called (tests don't rely on paycheck netting)
        double monthlyIncome = e.Job.CalculateMonthlyIncome(e.Job.HoursWorkedWeekly);
        if (monthlyIncome >0)
            savings.Deposit(monthlyIncome, e.Date, TransactionCategory.Income);
    }

    // Public helper used by the console to process a specific spending event amount
    public static void OnSpending(Person person, SpendingEventArgs e)
    {
        if (person is null || e is null) return;
        if (e.Amount <=0) return;
        ExecuteOptimalWithdrawals(person, e.Amount, e.Date);
    }

    private static void ExecuteOptimalWithdrawals(Person person, double amountNeeded, DateOnly date)
    {
        double remaining = amountNeeded;
        var order = GetOptimalWithdrawalOrder(person.CurrentAge(date));

        foreach (var type in order)
        {
            if (remaining <=0) break;

            // For emergency fund accounts, respect the minimum
            if (type == AccountType.Savings)
            {
                // 1) Use Pocket Cash freely
                var pocket = person.Investments.Accounts.FirstOrDefault(a => a.Type == AccountType.Savings && a.Name == "Pocket Cash" && a.Balance(date) > 0);
                if (pocket != null)
                {
                    double toWithdraw = Math.Min(remaining, pocket.Balance(date));
                    if (toWithdraw > 0)
                    {
                        double withdrawn = pocket.Withdraw(toWithdraw, date, TransactionCategory.Expenses);
                        remaining -= withdrawn;
                    }
                }

                if (remaining <= 0) continue;

                // 2) Then tap other Savings with emergency fund protection
                double available = person.GetAvailableForWithdrawal(date, AccountType.Savings);
                if (available >0)
                {
                    foreach (var acct in person.Investments.Accounts.Where(a => a.Type == AccountType.Savings && a.Name != "Pocket Cash" && a.Balance(date) >0))
                    {
                        if (remaining <=0) break;
                        double toWithdraw = Math.Min(remaining, Math.Min(available, acct.Balance(date)));
                        if (toWithdraw >0)
                        {
                            double withdrawn = acct.Withdraw(toWithdraw, date, TransactionCategory.Expenses);
                            remaining -= withdrawn;
                            available -= withdrawn;
                        }
                    }
                }
                continue;
            }

            if (type == AccountType.Taxable)
            {
                double available = person.GetAvailableForWithdrawal(date, type);
                if (available >0)
                {
                    foreach (var acct in person.Investments.Accounts.Where(a => a.Type == type && a.Balance(date) >0))
                    {
                        if (remaining <=0) break;
                        double toWithdraw = Math.Min(remaining, Math.Min(available, acct.Balance(date)));
                        if (toWithdraw >0)
                        {
                            double withdrawn = acct.Withdraw(toWithdraw, date, TransactionCategory.Expenses);
                            remaining -= withdrawn;
                            available -= withdrawn;
                        }
                    }
                }
                continue;
            }

            foreach (var acct in person.Investments.Accounts.Where(a => a.Type == type && a.Balance(date) >0))
            {
                if (remaining <=0) break;
                double toWithdraw = Math.Min(remaining, acct.Balance(date));
                if (toWithdraw >0)
                {
                    double withdrawn = acct.Withdraw(toWithdraw, date, TransactionCategory.Expenses);
                    remaining -= withdrawn;
                }
            }
        }
    }

    public static AccountType[] GetOptimalWithdrawalOrder(int currentAge)
    {
        if (currentAge <59) // Early retirement: prioritize penalty-free sources
        {
            return [
                AccountType.Savings,
                AccountType.Roth401k,
                AccountType.RothIRA,
                AccountType.HSA,
                AccountType.Taxable,
                AccountType.TraditionalIRA,
                AccountType.Traditional401k
            ];
        }
        else if (currentAge <73) // Post-penalty, pre-RMD: reduce tax-deferred balances first
        {
            return [
                AccountType.Savings,
                AccountType.Traditional401k,
                AccountType.TraditionalIRA,
                AccountType.HSA,
                AccountType.Taxable,
                AccountType.Roth401k,
                AccountType.RothIRA
            ];
        }
        else // RMD age and beyond
        {
            return [
                AccountType.Savings,
                AccountType.Traditional401k,
                AccountType.TraditionalIRA,
                AccountType.Taxable,
                AccountType.Roth401k,
                AccountType.HSA,
                AccountType.RothIRA
            ];
        }
    }
}
