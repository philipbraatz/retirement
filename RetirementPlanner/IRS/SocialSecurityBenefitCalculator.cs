namespace RetirementPlanner.IRS;

/// <summary>
/// Calculates Social Security benefits based on claiming age relative to Full Retirement Age
/// Implements the IRS benefit reduction and delayed retirement credit formulas
/// </summary>
public static class SocialSecurityBenefitCalculator
{
    /// <summary>
    /// Calculates the benefit multiplier based on claiming age relative to Full Retirement Age
    /// </summary>
    /// <param name="claimingAge">Age when Social Security is claimed</param>
    /// <param name="fullRetirementAge">Full Retirement Age (typically 66-67 depending on birth year)</param>
    /// <returns>Multiplier to apply to Primary Insurance Amount (PIA)</returns>
    public static double CalculateBenefitMultiplier(int claimingAge, int fullRetirementAge)
    {
        if (claimingAge < 62) return 0; // Cannot claim before 62
        
        if (claimingAge < fullRetirementAge)
        {
            // Early claiming reduction
            int monthsEarly = (fullRetirementAge - claimingAge) * 12;
            
            // First 36 months: 5/9 of 1% per month (6.67% per year)
            // Additional months: 5/12 of 1% per month (5% per year)
            double reduction = 0;
            if (monthsEarly <= 36)
            {
                reduction = monthsEarly * (5.0/9.0) / 100.0;
            }
            else
            {
                reduction = 36 * (5.0/9.0) / 100.0; // First 36 months
                reduction += (monthsEarly - 36) * (5.0/12.0) / 100.0; // Additional months
            }
            
            return 1.0 - reduction;
        }
        else if (claimingAge == fullRetirementAge)
        {
            return 1.0; // 100% of Primary Insurance Amount (PIA)
        }
        else
        {
            // Delayed retirement credits: 8% per year from FRA to age 70
            int yearsDelayed = Math.Min(claimingAge - fullRetirementAge, 70 - fullRetirementAge);
            return 1.0 + (yearsDelayed * 0.08);
        }
    }
    
    /// <summary>
    /// Calculates the monthly Social Security benefit amount
    /// </summary>
    /// <param name="primaryInsuranceAmount">The Primary Insurance Amount (PIA) - benefit at FRA</param>
    /// <param name="claimingAge">Age when Social Security is claimed</param>
    /// <param name="fullRetirementAge">Full Retirement Age</param>
    /// <returns>Monthly benefit amount</returns>
    public static double CalculateMonthlyBenefit(double primaryInsuranceAmount, int claimingAge, int fullRetirementAge)
    {
        double multiplier = CalculateBenefitMultiplier(claimingAge, fullRetirementAge);
        return primaryInsuranceAmount * multiplier;
    }
    
    /// <summary>
    /// Calculates break-even age between two claiming strategies
    /// </summary>
    /// <param name="primaryInsuranceAmount">The Primary Insurance Amount (PIA)</param>
    /// <param name="earlyClaimAge">Earlier claiming age</param>
    /// <param name="laterClaimAge">Later claiming age</param>
    /// <param name="fullRetirementAge">Full Retirement Age</param>
    /// <returns>Age at which total benefits received are equal</returns>
    public static double CalculateBreakEvenAge(double primaryInsuranceAmount, int earlyClaimAge, int laterClaimAge, int fullRetirementAge)
    {
        double earlyBenefit = CalculateMonthlyBenefit(primaryInsuranceAmount, earlyClaimAge, fullRetirementAge);
        double laterBenefit = CalculateMonthlyBenefit(primaryInsuranceAmount, laterClaimAge, fullRetirementAge);
        
        // Total benefits received by claiming early for the delay period
        double totalEarlyBenefits = earlyBenefit * 12 * (laterClaimAge - earlyClaimAge);
        
        // Monthly difference between later and early benefits
        double monthlyDifference = laterBenefit - earlyBenefit;
        
        if (monthlyDifference <= 0) return double.MaxValue; // Later claiming is never better
        
        // Months needed for later benefit to catch up
        double monthsToBreakEven = totalEarlyBenefits / monthlyDifference;
        
        return laterClaimAge + (monthsToBreakEven / 12.0);
    }
    
    /// <summary>
    /// Gets the Full Retirement Age based on birth year
    /// </summary>
    /// <param name="birthYear">Year of birth</param>
    /// <returns>Full Retirement Age</returns>
    public static int GetFullRetirementAge(int birthYear)
    {
        return birthYear switch
        {
            <= 1937 => 65,
            1938 => 65, // 65 + 2 months, simplified to 65
            1939 => 65, // 65 + 4 months, simplified to 65
            1940 => 65, // 65 + 6 months, simplified to 65
            1941 => 65, // 65 + 8 months, simplified to 65
            1942 => 65, // 65 + 10 months, simplified to 65
            >= 1943 and <= 1954 => 66,
            1955 => 66, // 66 + 2 months, simplified to 66
            1956 => 66, // 66 + 4 months, simplified to 66
            1957 => 66, // 66 + 6 months, simplified to 66
            1958 => 66, // 66 + 8 months, simplified to 66
            1959 => 66, // 66 + 10 months, simplified to 66
            >= 1960 => 67,
        };
    }
    
    /// <summary>
    /// Provides a comprehensive analysis of claiming strategies
    /// </summary>
    /// <param name="primaryInsuranceAmount">The Primary Insurance Amount (PIA)</param>
    /// <param name="fullRetirementAge">Full Retirement Age</param>
    /// <returns>Dictionary with claiming age as key and benefit info as value</returns>
    public static Dictionary<int, (double MonthlyBenefit, double AnnualBenefit, string Description)> 
        AnalyzeClaimingStrategies(double primaryInsuranceAmount, int fullRetirementAge)
    {
        var strategies = new Dictionary<int, (double, double, string)>();
        
        for (int age = 62; age <= 70; age++)
        {
            double monthlyBenefit = CalculateMonthlyBenefit(primaryInsuranceAmount, age, fullRetirementAge);
            double annualBenefit = monthlyBenefit * 12;
            
            string description = age switch
            {
                62 => "Early claiming (permanent reduction)",
                var a when a == fullRetirementAge => "Full Retirement Age (100% benefit)",
                70 => "Maximum benefit with delayed credits",
                var a when a < fullRetirementAge => "Reduced benefit",
                _ => "Delayed retirement credits"
            };
            
            strategies[age] = (monthlyBenefit, annualBenefit, description);
        }
        
        return strategies;
    }
}
