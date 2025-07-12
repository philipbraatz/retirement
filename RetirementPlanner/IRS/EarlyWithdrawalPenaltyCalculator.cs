namespace RetirementPlanner.IRS;

/// <summary>
/// Calculates early withdrawal penalties for retirement accounts
/// Based on IRS regulations for withdrawals before age 59½
/// </summary>
public static class EarlyWithdrawalPenaltyCalculator
{
    /// <summary>
    /// Calculates the early withdrawal penalty for a retirement account withdrawal
    /// </summary>
    /// <param name="accountType">Type of retirement account</param>
    /// <param name="withdrawalAmount">Amount being withdrawn</param>
    /// <param name="age">Age at time of withdrawal</param>
    /// <param name="separationFromServiceAge">Age when separated from service (for Rule of 55)</param>
    /// <param name="withdrawalReason">Reason for withdrawal (for exceptions)</param>
    /// <param name="rothContributionAmount">For Roth accounts, amount that is from contributions vs earnings</param>
    /// <returns>Penalty amount (typically 10% of taxable withdrawal)</returns>
    public static double CalculatePenalty(
        AccountType accountType, 
        double withdrawalAmount, 
        double age, 
        double? separationFromServiceAge = null,
        WithdrawalReason withdrawalReason = WithdrawalReason.GeneralDistribution,
        double rothContributionAmount = 0)
    {
        // No penalty after age 59½
        if (age >= 59.5) return 0;

        // Handle Roth account specifics
        if (accountType == AccountType.RothIRA || accountType == AccountType.Roth401k || accountType == AccountType.Roth403b)
        {
            return CalculateRothPenalty(withdrawalAmount, age, rothContributionAmount, withdrawalReason);
        }

        // Rule of 55 exception for 401(k) and 403(b)
        if ((accountType == AccountType.Traditional401k || accountType == AccountType.Traditional403b ||
             accountType == AccountType.Roth401k || accountType == AccountType.Roth403b) &&
            separationFromServiceAge.HasValue && separationFromServiceAge >= 55)
        {
            return 0; // No penalty under Rule of 55
        }

        // Check for other exceptions
        if (IsExceptionApplicable(withdrawalReason, accountType))
        {
            return 0;
        }

        // SIMPLE IRA has higher penalty in first 2 years
        if (accountType == AccountType.SIMPLEIRA)
        {
            // This would need additional logic to track when the account was established
            // For now, assume standard 10% penalty
            return withdrawalAmount * 0.10;
        }

        // Standard 10% penalty
        return withdrawalAmount * 0.10;
    }

    /// <summary>
    /// Calculates penalty for Roth account withdrawals
    /// Contributions can always be withdrawn penalty-free
    /// </summary>
    private static double CalculateRothPenalty(double withdrawalAmount, double age, double contributionAmount, WithdrawalReason reason)
    {
        // Contributions can always be withdrawn penalty and tax-free
        if (withdrawalAmount <= contributionAmount) return 0;

        double earningsWithdrawn = withdrawalAmount - contributionAmount;
        
        // Check for exceptions that apply to earnings
        if (IsExceptionApplicable(reason, AccountType.RothIRA))
        {
            return 0;
        }

        // 10% penalty on earnings portion only
        return earningsWithdrawn * 0.10;
    }

    /// <summary>
    /// Determines if a withdrawal reason qualifies for penalty exception
    /// </summary>
    private static bool IsExceptionApplicable(WithdrawalReason reason, AccountType accountType)
    {
        return reason switch
        {
            WithdrawalReason.FirstTimeHomePurchase => true, // Up to $10,000 lifetime limit for IRAs
            WithdrawalReason.HigherEducationExpenses => true, // IRAs only
            WithdrawalReason.MedicalExpenses => true, // Exceeding 7.5% of AGI
            WithdrawalReason.HealthInsurancePremiums => true, // While unemployed, IRAs only
            WithdrawalReason.Disability => true,
            WithdrawalReason.SEPP => true, // Substantially Equal Periodic Payments (Rule 72t)
            WithdrawalReason.QDRO => accountType == AccountType.Traditional401k || accountType == AccountType.Roth401k ||
                                   accountType == AccountType.Traditional403b || accountType == AccountType.Roth403b,
            WithdrawalReason.HardshipWithdrawal => accountType == AccountType.Traditional401k || accountType == AccountType.Roth401k ||
                                                 accountType == AccountType.Traditional403b || accountType == AccountType.Roth403b,
            _ => false
        };
    }
}

/// <summary>
/// Reasons for early withdrawal that may qualify for penalty exceptions
/// </summary>
public enum WithdrawalReason
{
    GeneralDistribution,
    FirstTimeHomePurchase,      // $10,000 lifetime limit for IRAs
    HigherEducationExpenses,    // IRAs only
    MedicalExpenses,           // Exceeding 7.5% of AGI
    HealthInsurancePremiums,   // While unemployed, IRAs only
    Disability,                // Permanent and total disability
    SEPP,                     // Substantially Equal Periodic Payments (Rule 72t)
    QDRO,                     // Qualified Domestic Relations Order (401k/403b only)
    HardshipWithdrawal,       // 401k/403b immediate and heavy financial need
    RuleOf55                  // Separation from service at 55+ (401k/403b only)
}
