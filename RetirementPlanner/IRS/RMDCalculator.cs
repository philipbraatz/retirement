using static RetirementPlanner.InvestmentAccount;

namespace RetirementPlanner.IRS;

/// <summary>
/// Calculates Required Minimum Distributions (RMDs) based on IRS regulations
/// RMDs are required starting at age 73 for most retirement accounts
/// </summary>
public static class RMDCalculator
{
    /// <summary>
    /// IRS Uniform Lifetime Table (2022) - Most common table used for RMD calculations
    /// Assumes beneficiary is 10 years younger than account owner
    /// </summary>
    private static readonly Dictionary<int, double> UniformLifetimeTable = new()
    {
        { 72, 27.4 }, { 73, 26.5 }, { 74, 25.5 }, { 75, 24.6 }, { 76, 23.7 },
        { 77, 22.9 }, { 78, 22.0 }, { 79, 21.1 }, { 80, 20.2 }, { 81, 19.4 },
        { 82, 18.5 }, { 83, 17.7 }, { 84, 16.8 }, { 85, 16.0 }, { 86, 15.2 },
        { 87, 14.4 }, { 88, 13.7 }, { 89, 12.9 }, { 90, 12.2 }, { 91, 11.5 },
        { 92, 10.8 }, { 93, 10.1 }, { 94, 9.5 }, { 95, 8.9 }, { 96, 8.4 },
        { 97, 7.8 }, { 98, 7.3 }, { 99, 6.8 }, { 100, 6.4 }, { 101, 6.0 },
        { 102, 5.6 }, { 103, 5.2 }, { 104, 4.9 }, { 105, 4.6 }, { 106, 4.3 },
        { 107, 4.1 }, { 108, 3.9 }, { 109, 3.7 }, { 110, 3.5 }, { 111, 3.4 },
        { 112, 3.3 }, { 113, 3.1 }, { 114, 3.0 }, { 115, 2.9 }, { 116, 2.8 },
        { 117, 2.7 }, { 118, 2.5 }, { 119, 2.3 }, { 120, 2.0 }
    };

    /// <summary>
    /// Calculates the Required Minimum Distribution for an account
    /// </summary>
    /// <param name="account">The retirement account</param>
    /// <param name="age">Owner's age at end of the year</param>
    /// <param name="priorYearEndBalance">Account balance as of December 31 of prior year</param>
    /// <param name="isStillWorking">Whether the owner is still working for the employer (401k only)</param>
    /// <returns>Required minimum distribution amount</returns>
    public static double CalculateRMD(InvestmentAccount account, int age, double priorYearEndBalance, bool isStillWorking = false)
    {
        // RMDs start at age 73
        if (age < 73) return 0;

        // Still working exception for 401(k) plans (not IRAs)
        if (isStillWorking && (account.Type == AccountType.Traditional401k || account.Type == AccountType.Traditional403b))
        {
            return 0;
        }

        // Roth IRAs don't have RMDs during owner's lifetime
        if (account.Type == AccountType.RothIRA)
        {
            return 0;
        }

        // Get life expectancy factor
        double lifeExpectancyFactor = GetLifeExpectancyFactor(age);
        
        if (lifeExpectancyFactor <= 0) return priorYearEndBalance; // Edge case for very old ages

        return priorYearEndBalance / lifeExpectancyFactor;
    }

    /// <summary>
    /// Gets the life expectancy factor from the IRS Uniform Lifetime Table
    /// </summary>
    /// <param name="age">Age at end of year</param>
    /// <returns>Life expectancy factor</returns>
    private static double GetLifeExpectancyFactor(int age)
    {
        if (UniformLifetimeTable.TryGetValue(age, out double factor))
        {
            return factor;
        }

        // For ages beyond the table, use the last value
        if (age > 120) return 2.0;
        
        // For ages below 72, no RMD required
        return 0;
    }

    /// <summary>
    /// Determines if an account is subject to RMDs
    /// </summary>
    /// <param name="accountType">The type of retirement account</param>
    /// <returns>True if account requires RMDs</returns>
    public static bool IsSubjectToRMD(AccountType accountType)
    {
        return accountType switch
        {
            AccountType.Traditional401k => true,
            AccountType.Traditional403b => true,
            AccountType.TraditionalIRA => true,
            AccountType.SEPIRA => true,
            AccountType.SIMPLEIRA => true,
            AccountType.Roth401k => true,  // Roth 401(k) requires RMDs unlike Roth IRA
            AccountType.Roth403b => true,  // Roth 403(b) requires RMDs unlike Roth IRA
            AccountType.RothIRA => false,  // No RMDs during owner's lifetime
            AccountType.Savings => false,
            AccountType.Taxable => false,
            _ => false
        };
    }

    /// <summary>
    /// Calculates the penalty for not taking required RMD
    /// </summary>
    /// <param name="requiredAmount">Required RMD amount</param>
    /// <param name="actualWithdrawal">Amount actually withdrawn</param>
    /// <returns>Penalty amount (25% of shortfall as of 2023)</returns>
    public static double CalculateRMDPenalty(double requiredAmount, double actualWithdrawal)
    {
        double shortfall = Math.Max(0, requiredAmount - actualWithdrawal);
        return shortfall * 0.25; // 25% penalty as of 2023 (reduced from 50%)
    }
}
