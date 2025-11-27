namespace RetirementPlanner.Calculators;
public static class EarlyWithdrawalPenaltyCalculator
{
    // Constants (IRC §72(t), SECURE / SECURE 2.0 references)
    private const double StandardEarlyPenaltyRate = 0.10;          // 10% general early withdrawal penalty
    private const double SimpleIraEarlyPenaltyRate = 0.25;         // First 2 years SIMPLE IRA (IRC §72(t)(6))
    private const double AgeEarlyPenaltyEnds = 59.5;               // Age 59½ threshold
    private const int RuleOf55Age = 55;                            // Separation from service exception starting age
    private const double BirthOrAdoptionMax = 5000;                // SECURE Act per event limit
    private const double EmergencyExpenseMax = 1000;               // SECURE 2.0 emergency distribution limit
    private const int EmergencyExpenseLookbackYears = 3;           // Once per 3-year period

    public static double CalculatePenalty(
        AccountType accountType,
        double withdrawalAmount,
        double age,
        double? separationFromServiceAge = null,
        WithdrawalReason withdrawalReason = WithdrawalReason.GeneralDistribution,
        double rothContributionAmount = 0,
        int emergencyWithdrawalsTakenInPast3Years = 0,
        DateTime? accountCreationDate = null)
    {
        if (age >= AgeEarlyPenaltyEnds) return 0;

        // Roth accounts: contributions always penalty-free
        if (accountType is AccountType.RothIRA or AccountType.Roth401k or AccountType.Roth403b)
            return CalculateRothPenalty(withdrawalAmount, rothContributionAmount, withdrawalReason, emergencyWithdrawalsTakenInPast3Years);

        // Rule of 55 (401k/403b only)
        if ((accountType is AccountType.Traditional401k or AccountType.Traditional403b or AccountType.Roth401k or AccountType.Roth403b)
            && separationFromServiceAge is >= RuleOf55Age)
            return 0;

        // Statutory & SECURE 2.0 exceptions
        if (IsExceptionApplicable(withdrawalReason, accountType, withdrawalAmount, emergencyWithdrawalsTakenInPast3Years))
            return 0;

        // SIMPLE IRA higher penalty first 2 years from establishment
        if (accountType != AccountType.SIMPLEIRA)
            return withdrawalAmount * StandardEarlyPenaltyRate;

        // Without creation date assume standard rate (conservative)
        if (!accountCreationDate.HasValue)
            return withdrawalAmount * StandardEarlyPenaltyRate;

        var yearsSinceStart = (DateTime.UtcNow - accountCreationDate.Value).TotalDays / 365.25;
        double rate = yearsSinceStart < 2.0 ? SimpleIraEarlyPenaltyRate : StandardEarlyPenaltyRate;
        return withdrawalAmount * rate;
    }

    private static double CalculateRothPenalty(double withdrawalAmount, double contributionAmount, WithdrawalReason reason, int emergencyWithdrawalsTakenInPast3Years)
    {
        if (withdrawalAmount <= contributionAmount) return 0; // Only earnings potentially penalized
        double earnings = withdrawalAmount - contributionAmount;
        if (IsExceptionApplicable(reason, AccountType.RothIRA, withdrawalAmount, emergencyWithdrawalsTakenInPast3Years)) return 0;
        return earnings * StandardEarlyPenaltyRate;
    }

    private static bool IsExceptionApplicable(WithdrawalReason reason, AccountType accountType, double withdrawalAmount = 0, int emergencyWithdrawalsTakenInPast3Years = 0)
    {
        if (IsSecure20ExceptionApplicable(reason, withdrawalAmount, emergencyWithdrawalsTakenInPast3Years))
            return true;

        return reason switch
        {
            WithdrawalReason.FirstTimeHomePurchase => true, // Up to $10k lifetime (IRA) – additional tracking not implemented
            WithdrawalReason.HigherEducationExpenses => true, // IRAs only (simplified – not validating account type income agg)
            WithdrawalReason.MedicalExpenses => true, // Assuming meets 7.5% AGI threshold (simplified)
            WithdrawalReason.HealthInsurancePremiums => true, // While unemployed – unemployment tracking not implemented
            WithdrawalReason.Disability => true,
            WithdrawalReason.SEPP => true, // Substantially Equal Periodic Payments (Rule 72t)
            WithdrawalReason.QDRO => accountType is AccountType.Traditional401k or AccountType.Roth401k or AccountType.Traditional403b or AccountType.Roth403b,
            WithdrawalReason.HardshipWithdrawal => accountType is AccountType.Traditional401k or AccountType.Roth401k or AccountType.Traditional403b or AccountType.Roth403b,
            _ => false
        };
    }

    private static bool IsSecure20ExceptionApplicable(WithdrawalReason reason, double withdrawalAmount, int emergencyWithdrawalsTakenInPast3Years) => reason switch
    {
        WithdrawalReason.BirthOrAdoption => withdrawalAmount <= BirthOrAdoptionMax,
        WithdrawalReason.TerminalIllness => true,
        WithdrawalReason.EmergencyExpense => withdrawalAmount <= EmergencyExpenseMax && emergencyWithdrawalsTakenInPast3Years == 0,
        WithdrawalReason.DomesticAbuseVictim => true,
        WithdrawalReason.FederallyDeclaredDisaster => true,
        _ => false
    };
}

public enum WithdrawalReason
{
    GeneralDistribution,
    FirstTimeHomePurchase,
    HigherEducationExpenses,
    MedicalExpenses,
    HealthInsurancePremiums,
    Disability,
    SEPP,
    QDRO,
    HardshipWithdrawal,
    RuleOf55,
    BirthOrAdoption,
    TerminalIllness,
    EmergencyExpense,
    DomesticAbuseVictim,
    FederallyDeclaredDisaster
}
