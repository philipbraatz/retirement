public static class LifeExpectancyTable
{
    var lifeExpectancy = new Dictionary<int, (double Males, double Females)>
        {
            { 0, (0.00353, 0.00304) },
            { 1, (0.00025, 0.00021) },
            { 2, (0.00017, 0.00013) },
            { 3, (0.00012, 0.00010) },
            { 4, (0.00011, 0.00008) },
            { 5, (0.00009, 0.00007) },
            { 6, (0.00008, 0.00006) },
            { 7, (0.00008, 0.00006) },
            { 8, (0.00006, 0.00005) },
            { 9, (0.00005, 0.00005) },
            { 10, (0.00005, 0.00005) },
            { 11, (0.00005, 0.00006) },
            { 12, (0.00008, 0.00006) },
            { 13, (0.00010, 0.00007) },
            { 14, (0.00013, 0.00008) },
            { 15, (0.00017, 0.00008) },
            { 16, (0.00021, 0.00009) },
            { 17, (0.00025, 0.00010) },
            { 18, (0.00029, 0.00010) },
            { 19, (0.00033, 0.00010) },
            { 20, (0.00036, 0.00010) },
            { 21, (0.00036, 0.00010) },
            { 22, (0.00037, 0.00011) },
            { 23, (0.00037, 0.00013) },
            { 24, (0.00039, 0.00014) },
            { 25, (0.00039, 0.00014) },
            { 26, (0.00041, 0.00014) },
            { 27, (0.00042, 0.00016) },
            { 28, (0.00044, 0.00016) },
            { 29, (0.00046, 0.00017) },
            { 30, (0.00048, 0.00018) },
            { 31, (0.00050, 0.00019) },
            { 32, (0.00053, 0.00021) },
            { 33, (0.00056, 0.00023) },
            { 34, (0.00058, 0.00024) },
            { 35, (0.00061, 0.00026) },
            { 36, (0.00064, 0.00028) },
            { 37, (0.00067, 0.00031) },
            { 38, (0.00069, 0.00032) },
            { 39, (0.00072, 0.00035) },
            { 40, (0.00073, 0.00037) },
            { 41, (0.00075, 0.00039) },
            { 42, (0.00076, 0.00041) },
            { 43, (0.00079, 0.00043) },
            { 44, (0.00080, 0.00045) },
            { 45, (0.00083, 0.00047) },
            { 46, (0.00087, 0.00051) },
            { 47, (0.00091, 0.00054) },
            { 48, (0.00097, 0.00058) },
            { 49, (0.00103, 0.00064) },
            { 50, (0.00112, 0.00070) },
            { 51, (0.00123, 0.00079) },
            { 52, (0.00136, 0.00089) },
            { 53, (0.00152, 0.00101) },
            { 54, (0.00172, 0.00114) },
            { 55, (0.00204, 0.00136) },
            { 56, (0.00251, 0.00169) },
            { 57, (0.00293, 0.00193) },
            { 58, (0.00341, 0.00223) },
            { 59, (0.00394, 0.00256) },
            { 60, (0.00454, 0.00297) },
            { 61, (0.00519, 0.00341) },
            { 62, (0.00610, 0.00406) },
            { 63, (0.00698, 0.00474) },
            { 64, (0.00768, 0.00532) },
            { 65, (0.00854, 0.00614) },
            { 66, (0.00949, 0.00702) },
            { 67, (0.01046, 0.00779) },
            { 68, (0.01154, 0.00864) },
            { 69, (0.01273, 0.00960) },
            { 70, (0.01408, 0.01074) },
            { 71, (0.01563, 0.01207) },
            { 72, (0.01736, 0.01357) },
            { 73, (0.01934, 0.01528) },
            { 74, (0.02158, 0.01729) },
            { 75, (0.02415, 0.01959) },
            { 76, (0.02708, 0.02223) },
            { 77, (0.03045, 0.02520) },
            { 78, (0.03433, 0.02856) },
            { 79, (0.03882, 0.03229) },
            { 80, (0.04408, 0.03686) },
            { 81, (0.04969, 0.04125) },
            { 82, (0.05605, 0.04614) },
            { 83, (0.06323, 0.05161) },
            { 84, (0.07137, 0.05777) },
            { 85, (0.08066, 0.06477) },
            { 86, (0.09115, 0.07282) },
            { 87, (0.10295, 0.08197) },
            { 88, (0.11615, 0.09244) },
            { 89, (0.13069, 0.10416) },
            { 90, (0.14650, 0.11723) },
            { 91, (0.16317, 0.13071) },
            { 92, (0.18022, 0.14477) },
            { 93, (0.19758, 0.15936) },
            { 94, (0.21499, 0.17426) },
            { 95, (0.23232, 0.18953) },
            { 96, (0.25065, 0.20589) },
            { 97, (0.26924, 0.22289) },
            { 98, (0.28813, 0.24064) },
            { 99, (0.30750, 0.25903) },
            { 100, (0.32701, 0.27800) },
            { 101, (0.34652, 0.29740) },
            { 102, (0.36577, 0.31688) },
            { 103, (0.38470, 0.33639) },
            { 104, (0.40330, 0.35579) },
            { 105, (0.42090, 0.37505) },
            { 106, (0.43800, 0.39383) },
            { 107, (0.45424, 0.41213) },
            { 108, (0.46975, 0.42960) },
            { 109, (0.48458, 0.44636) },
            { 110, (0.49379, 0.46232) },
            { 111, (0.49492, 0.47751) },
            { 112, (0.49606, 0.49181) },
            { 113, (0.49721, 0.49790) },
            { 114, (0.49845, 0.49880) },
            { 115, (0.49960, 0.49970) },
            { 116, (0.49980, 0.49985) },
            { 117, (0.49990, 0.49995) },
            { 118, (0.49995, 0.50000) },
            { 119, (0.50000, 0.50000) }
        };

    public static double GetLifeExpectancy(int age)
    {
        return Table.ContainsKey(age) ? Table[age] : Table[100]; // Default to age 100 if not found
    }
}

public static class ContributionLimits
{
    public static double Max401kPersonal = 22500;  // 2024 personal 401(k) limit
    public static double Max401kTotal = 66500;     // 2024 total (personal + employer)
    public static double MaxRothIRA = 6500;        // 2024 Roth IRA limit (under 50)
    public static double CompensationLimit = 330000; // Max salary considered for employer match
}

public static class SocialSecurityCalculator
{
    public static int GetFullRetirementAge(int birthYear)
    {
        if (birthYear >= 1960) return 67; // FRA for those born in 1960 or later
        if (birthYear >= 1955) return 66 + (birthYear - 1954); // FRA gradually increases
        return 66; // FRA for those born before 1954
    }

    public static double CalculateSocialSecurityBenefit(int birthYear, int claimingAge, double averageEarnings)
    {
        int fra = GetFullRetirementAge(birthYear);
        double pia = CalculatePIA(averageEarnings);
        double adjustedBenefit = AdjustForClaimingAge(pia, claimingAge, birthYear);
        double yearsUntilClaiming = claimingAge - (DateTime.Now.Year - birthYear);

        return ApplyCOLA(adjustedBenefit, yearsUntilClaiming);
    }
}

public static class TaxBrackets
{
    public class Bracket
    {
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public double Rate { get; set; }
    }

    // List of tax brackets (Example: U.S. Federal Brackets - Populate later)
    public static List<Bracket> Brackets = new List<Bracket>
    {
        new Bracket { LowerBound = 0, UpperBound = 9875, Rate = 0.10 },
        new Bracket { LowerBound = 9876, UpperBound = 40125, Rate = 0.12 },
        new Bracket { LowerBound = 40126, UpperBound = 85525, Rate = 0.22 },
        new Bracket { LowerBound = 85526, UpperBound = 163300, Rate = 0.24 }
    };

    public static double CalculateTaxes(double income)
    {
        double tax = 0;
        foreach (var bracket in Brackets)
        {
            if (income > bracket.LowerBound)
            {
                double taxableAmount = Math.Min(income, bracket.UpperBound) - bracket.LowerBound;
                tax += taxableAmount * bracket.Rate;
            }
        }
        return tax;
    }
}
