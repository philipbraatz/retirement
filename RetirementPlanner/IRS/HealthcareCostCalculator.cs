namespace RetirementPlanner.IRS;

/// <summary>
/// Calculates healthcare costs for retirement planning
/// Accounts for the transition from employer insurance to Medicare at age 65
/// </summary>
public static class HealthcareCostCalculator
{
    /// <summary>
    /// Calculates annual healthcare costs based on age and insurance status
    /// </summary>
    /// <param name="age">Current age</param>
    /// <param name="hasEmployerInsurance">Whether the person has employer-provided insurance</param>
    /// <param name="incomeLevel">Income level for Medicare premium adjustments (IRMAA)</param>
    /// <returns>Estimated annual healthcare costs</returns>
    public static double CalculateAnnualHealthcareCosts(int age, bool hasEmployerInsurance, double incomeLevel = 0)
    {
        if (age < 65 && hasEmployerInsurance)
        {
            return 5000; // Typical employer plan employee contribution
        }
        else if (age < 65 && !hasEmployerInsurance)
        {
            return 15000; // ACA marketplace or COBRA premiums
        }
        else // Age 65+, Medicare eligible
        {
            return CalculateMedicareCosts(incomeLevel);
        }
    }
    
    /// <summary>
    /// Calculates Medicare costs including IRMAA adjustments for high-income individuals
    /// </summary>
    /// <param name="modifiedAdjustedGrossIncome">MAGI for IRMAA calculation</param>
    /// <returns>Annual Medicare costs</returns>
    private static double CalculateMedicareCosts(double modifiedAdjustedGrossIncome)
    {
        // 2024 Medicare costs (updated annually)
        double medicarePartA = 0;           // Free for most people
        double medicarePartB = GetPartBPremium(modifiedAdjustedGrossIncome);
        double supplementalPlan = 3000;     // Medigap or Medicare Advantage
        double partD = GetPartDPremium(modifiedAdjustedGrossIncome);
        double outOfPocket = 2500;          // Deductibles, copays, and uncovered expenses
        
        return medicarePartA + medicarePartB + supplementalPlan + partD + outOfPocket;
    }
    
    /// <summary>
    /// Calculates Medicare Part B premium including IRMAA adjustments
    /// </summary>
    /// <param name="magi">Modified Adjusted Gross Income</param>
    /// <returns>Annual Part B premium</returns>
    private static double GetPartBPremium(double magi)
    {
        // 2024 Medicare Part B IRMAA thresholds (single filer)
        double basePremium = 1748; // Standard Part B premium for 2024
        
        if (magi <= 103000) return basePremium;
        if (magi <= 129000) return basePremium + (70 * 12);   // +$70/month
        if (magi <= 161000) return basePremium + (175 * 12);  // +$175/month
        if (magi <= 193000) return basePremium + (280 * 12);  // +$280/month
        if (magi <= 500000) return basePremium + (385 * 12);  // +$385/month
        
        return basePremium + (490 * 12); // +$490/month for highest tier
    }
    
    /// <summary>
    /// Calculates Medicare Part D premium including IRMAA adjustments
    /// </summary>
    /// <param name="magi">Modified Adjusted Gross Income</param>
    /// <returns>Annual Part D premium</returns>
    private static double GetPartDPremium(double magi)
    {
        // 2024 Medicare Part D IRMAA adjustments
        double basePremium = 600; // Average Part D premium
        
        if (magi <= 103000) return basePremium;
        if (magi <= 129000) return basePremium + (12 * 12);   // +$12/month
        if (magi <= 161000) return basePremium + (31 * 12);   // +$31/month
        if (magi <= 193000) return basePremium + (50 * 12);   // +$50/month
        if (magi <= 500000) return basePremium + (69 * 12);   // +$69/month
        
        return basePremium + (87 * 12); // +$87/month for highest tier
    }
    
    /// <summary>
    /// Estimates long-term care costs that may not be covered by Medicare
    /// </summary>
    /// <param name="age">Current age</param>
    /// <param name="hasLongTermCareInsurance">Whether person has LTC insurance</param>
    /// <returns>Estimated annual long-term care costs</returns>
    public static double EstimateLongTermCareCosts(int age, bool hasLongTermCareInsurance = false)
    {
        if (age < 75) return 0; // Low probability before 75
        
        // Probability of needing care increases with age
        double probabilityOfCare = age switch
        {
            >= 75 and < 80 => 0.05,  // 5% chance
            >= 80 and < 85 => 0.15,  // 15% chance
            >= 85 and < 90 => 0.35,  // 35% chance
            >= 90 => 0.60,           // 60% chance
            _ => 0
        };
        
        // Average annual cost of care (varies by location and type)
        double averageAnnualCost = 60000; // Assisted living average
        double nursingHomeCost = 120000;  // Nursing home average
        
        // Weight the costs by probability and severity
        double expectedCost = (averageAnnualCost * 0.7 + nursingHomeCost * 0.3) * probabilityOfCare;
        
        if (hasLongTermCareInsurance)
        {
            expectedCost *= 0.3; // Insurance covers ~70% of costs
        }
        
        return expectedCost;
    }
    
    /// <summary>
    /// Provides healthcare cost projections for retirement planning
    /// </summary>
    /// <param name="currentAge">Current age</param>
    /// <param name="projectionYears">Number of years to project</param>
    /// <param name="inflationRate">Healthcare inflation rate (typically higher than general inflation)</param>
    /// <param name="incomeLevel">Income level for Medicare premium calculations</param>
    /// <returns>Year-by-year healthcare cost projections</returns>
    public static Dictionary<int, double> ProjectHealthcareCosts(
        int currentAge, 
        int projectionYears, 
        double inflationRate = 0.05, // Healthcare inflation typically 5%+
        double incomeLevel = 0)
    {
        var projections = new Dictionary<int, double>();
        
        for (int year = 0; year < projectionYears; year++)
        {
            int futureAge = currentAge + year;
            double baseCost = CalculateAnnualHealthcareCosts(futureAge, false, incomeLevel);
            double ltcCost = EstimateLongTermCareCosts(futureAge);
            
            // Apply healthcare inflation
            double inflatedCost = (baseCost + ltcCost) * Math.Pow(1 + inflationRate, year);
            
            projections[futureAge] = inflatedCost;
        }
        
        return projections;
    }
}
