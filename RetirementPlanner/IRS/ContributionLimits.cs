namespace RetirementPlanner.IRS;

public static class ContributionLimits
{
    public const double Max401kPersonal = 23500;  // 2025 personal 401(k) limit
    public const double Max401kCatchUp50 = 7500;  // 2025 catch-up contribution for 50 or older
    public const double Max401kCatchUp60to63 = 11250; // 2025 catch-up contribution for 60 to 63 years old
    public const double Max401kTotal = 66500;     // 2024 total (personal + employer)
    public const double CompensationLimit = 330000; // Max salary considered for employer match
    public const double EmployerLimit = 69000; // Max salary considered for employer match
    public const double EmployerCatchUpLimit = 76500; // Max salary considered for employer match for 50 and older

    public static double Limit401kPersonal(double personalContributions, double personalRate, double age)
    {
        double baseLimit = Math.Max(personalContributions * personalRate, Max401kPersonal);
        double catchUpLimit = 0;

        if (age >= 50 && age < 60)
        {
            catchUpLimit = Max401kCatchUp50;
        }
        else if (age >= 60 && age <= 63)
        {
            catchUpLimit = Max401kCatchUp60to63;
        }

        return baseLimit + catchUpLimit;
    }

    public static double Limit401kEmployer(double currentPersonal, double employerContribution, int age)
    {
        double totalLimit = (age >= 50) ? EmployerCatchUpLimit : EmployerLimit; // Catch-up at age 50
        return Math.Max(0, totalLimit - currentPersonal - employerContribution);
    }

    public static double LimitCompensation(double salary) => Math.Min(salary, CompensationLimit);

    public static double Calculate401kContributionYearly(double salary, double personalRate, double employerRate, double age)
    {
        double annualContribution = Limit401kPersonal(salary, personalRate, age);
        double salaryForMatch = LimitCompensation(salary);
        double employerContribution = Math.Min(salaryForMatch * employerRate, Max401kTotal - annualContribution);

        return Math.Min(annualContribution + employerContribution, Max401kTotal);
    }
}