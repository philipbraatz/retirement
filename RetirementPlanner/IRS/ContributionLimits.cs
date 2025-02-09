namespace RetirementPlanner.IRS;

public static class ContributionLimits
{
    public const double Max401kPersonal = 22500;  // 2024 personal 401(k) limit
    public const double Max401kTotal = 66500;     // 2024 total (personal + employer)
    public const double MaxRothIRA = 6500;        // 2024 Roth IRA limit (under 50)
    public const double CompensationLimit = 330000; // Max salary considered for employer match

    public static double Limit401kPersonal(double salary, double personalRate) => Math.Min(salary * personalRate, Max401kPersonal);
    public static double LimitCompensation(double salary) => Math.Min(salary, CompensationLimit);
    public static double Calculate401kContributionYearly(double salary, double personalRate, double employerRate)
    {
        double annualContribution = Limit401kPersonal(salary, personalRate);
        double salaryForMatch = LimitCompensation(salary);
        double employerContribution = Math.Min(salaryForMatch * employerRate, Max401kTotal - annualContribution);

        return Math.Min(annualContribution + employerContribution, Max401kTotal);
    }

    public static double CalculateRothIRAContributionYearly(double salary) => Math.Min(MaxRothIRA, salary * 0.10);
}

