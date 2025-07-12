namespace RetirementPlanner.IRS;

/// <summary>
/// IRS contribution limits for retirement accounts (2024/2025 tax years)
/// Based on IRS Publication 560 and annual updates
/// </summary>
public static class ContributionLimits
{
    // 2024 Limits
    public const double Max401k2024Personal = 23000;
    public const double Max401k2024CatchUp50 = 7500;
    public const double Max401k2024CatchUp60to63 = 11250; // SECURE 2.0 enhanced catch-up
    public const double Max401k2024Total = 69000;
    public const double Max401k2024TotalWithCatchUp = 76500;
    public const double MaxIRA2024 = 7000;
    public const double MaxIRA2024CatchUp = 1000;
    
    // 2025 Limits (projected)
    public const double Max401k2025Personal = 23500;
    public const double Max401k2025CatchUp50 = 7500;
    public const double Max401k2025CatchUp60to63 = 11250;
    public const double Max401k2025Total = 70000;
    public const double Max401k2025TotalWithCatchUp = 77500;
    public const double MaxIRA2025 = 7000;
    public const double MaxIRA2025CatchUp = 1000;
    
    // HSA limits
    public const double MaxHSA2024Individual = 4150;
    public const double MaxHSA2024Family = 8300;
    public const double MaxHSA2024CatchUp = 1000;
    public const double MaxHSA2025Individual = 4300; // Projected
    public const double MaxHSA2025Family = 8550; // Projected
    public const double MaxHSA2025CatchUp = 1000;

    // Compensation limits
    public const double CompensationLimit2024 = 345000;
    public const double CompensationLimit2025 = 350000; // Projected
    
    // SEP-IRA limits (25% of compensation or dollar limit)
    public const double MaxSEP2024 = 69000;
    public const double MaxSEP2025 = 70000;
    
    // SIMPLE IRA limits
    public const double MaxSIMPLE2024 = 16000;
    public const double MaxSIMPLE2024CatchUp = 3500;
    public const double MaxSIMPLE2025 = 16500; // Projected
    public const double MaxSIMPLE2025CatchUp = 3500;

    /// <summary>
    /// Get 401(k) personal contribution limit for a given year and age
    /// </summary>
    public static double Get401kPersonalLimit(int year, int age)
    {
        double baseLimit = year >= 2025 ? Max401k2025Personal : Max401k2024Personal;
        double catchUpLimit = 0;

        if (age >= 50 && age < 60)
        {
            catchUpLimit = year >= 2025 ? Max401k2025CatchUp50 : Max401k2024CatchUp50;
        }
        else if (age >= 60 && age <= 63) // SECURE 2.0 enhanced catch-up
        {
            catchUpLimit = year >= 2025 ? Max401k2025CatchUp60to63 : Max401k2024CatchUp60to63;
        }
        else if (age >= 64) // Regular catch-up after enhanced period
        {
            catchUpLimit = year >= 2025 ? Max401k2025CatchUp50 : Max401k2024CatchUp50;
        }

        return baseLimit + catchUpLimit;
    }

    /// <summary>
    /// Get total 401(k) contribution limit (employee + employer) for a given year and age
    /// </summary>
    public static double Get401kTotalLimit(int year, int age)
    {
        if (age >= 50)
        {
            return year >= 2025 ? Max401k2025TotalWithCatchUp : Max401k2024TotalWithCatchUp;
        }
        return year >= 2025 ? Max401k2025Total : Max401k2024Total;
    }

    /// <summary>
    /// Get IRA contribution limit for a given year and age
    /// </summary>
    public static double GetIRALimit(int year, int age)
    {
        double baseLimit = year >= 2025 ? MaxIRA2025 : MaxIRA2024;
        double catchUpLimit = 0;

        if (age >= 50)
        {
            catchUpLimit = year >= 2025 ? MaxIRA2025CatchUp : MaxIRA2024CatchUp;
        }

        return baseLimit + catchUpLimit;
    }

    /// <summary>
    /// Get SEP-IRA contribution limit for a given year (25% of compensation or dollar limit)
    /// </summary>
    public static double GetSEPLimit(int year, double compensation)
    {
        double dollarLimit = year >= 2025 ? MaxSEP2025 : MaxSEP2024;
        double percentageLimit = compensation * 0.25;
        return Math.Min(dollarLimit, percentageLimit);
    }

    /// <summary>
    /// Get SIMPLE IRA contribution limit for a given year and age
    /// </summary>
    public static double GetSIMPLELimit(int year, int age)
    {
        double baseLimit = year >= 2025 ? MaxSIMPLE2025 : MaxSIMPLE2024;
        double catchUpLimit = 0;

        if (age >= 50)
        {
            catchUpLimit = year >= 2025 ? MaxSIMPLE2025CatchUp : MaxSIMPLE2024CatchUp;
        }

        return baseLimit + catchUpLimit;
    }

    /// <summary>
    /// Get compensation limit for a given year
    /// </summary>
    public static double GetCompensationLimit(int year)
    {
        return year >= 2025 ? CompensationLimit2025 : CompensationLimit2024;
    }

    /// <summary>
    /// Get HSA contribution limit for a given year and coverage type
    /// </summary>
    public static double GetHSALimit(int year, bool isFamilyCoverage = false, int age = 0)
    {
        double baseLimit = year >= 2025 
            ? (isFamilyCoverage ? MaxHSA2025Family : MaxHSA2025Individual)
            : (isFamilyCoverage ? MaxHSA2024Family : MaxHSA2024Individual);
        
        double catchUpLimit = 0;
        if (age >= 55)
        {
            catchUpLimit = year >= 2025 ? MaxHSA2025CatchUp : MaxHSA2024CatchUp;
        }

        return baseLimit + catchUpLimit;
    }

    /// <summary>
    /// Get HSA contribution limit for a given year (individual coverage)
    /// </summary>
    public static double GetHSALimit(int year)
    {
        return GetHSALimit(year, false, 0);
    }

    /// <summary>
    /// Calculate maximum employer 401(k) contribution
    /// </summary>
    public static double CalculateMaxEmployer401k(int year, int age, double employeeContribution, double compensation)
    {
        double totalLimit = Get401kTotalLimit(year, age);
        double limitedCompensation = Math.Min(compensation, GetCompensationLimit(year));
        double maxEmployerFromComp = limitedCompensation * 0.25; // Typical limit
        
        return Math.Min(totalLimit - employeeContribution, maxEmployerFromComp);
    }

    public static double Limit401kPersonal(double personalContributions, double personalRate, double age)
    {
        int currentYear = DateTime.Now.Year;
        double baseLimit = Math.Max(personalContributions * personalRate, Get401kPersonalLimit(currentYear, (int)age));
        return baseLimit;
    }

    public static double Limit401kEmployer(double currentPersonal, double employerContribution, int age)
    {
        int currentYear = DateTime.Now.Year;
        double totalLimit = Get401kTotalLimit(currentYear, age);
        return Math.Max(0, totalLimit - currentPersonal - employerContribution);
    }

    public static double LimitCompensation(double salary) => Math.Min(salary, GetCompensationLimit(DateTime.Now.Year));

    public static double Calculate401kContributionYearly(double salary, double personalRate, double employerRate, double age)
    {
        int currentYear = DateTime.Now.Year;
        double annualContribution = Get401kPersonalLimit(currentYear, (int)age);
        double salaryForMatch = LimitCompensation(salary);
        double employerContribution = Math.Min(salaryForMatch * employerRate, Get401kTotalLimit(currentYear, (int)age) - annualContribution);

        return Math.Min(annualContribution + employerContribution, Get401kTotalLimit(currentYear, (int)age));
    }
}