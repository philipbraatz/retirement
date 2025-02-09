namespace RetirementPlanner.IRS;

public static class SocialSecurity
    {
        public const int MIN_RETIREMENT_AGE = 67;
        public const int MIN_RETIREMENT_AGE_1955 = 66;

        public static double CalculatePIA(double averageEarnings)
        {
            const double bendPoint1 = 1115;  // First bend point (2024 value)
            const double bendPoint2 = 6721;  // Second bend point
            const double piaPercent1 = 0.9;
            const double piaPercent2 = 0.32;
            const double piaPercent3 = 0.15;

            double pia = averageEarnings * piaPercent1;
            if (averageEarnings <= bendPoint2)
                pia += (averageEarnings - bendPoint1) * piaPercent2;
            else
                pia += (bendPoint2 - bendPoint1) * piaPercent2 + 
                       (averageEarnings - bendPoint2) * piaPercent3;

            return pia;
        }

        public static double AdjustForClaimingAge(double pia, int claimingAge, int RetirementAge)
        {
            double change = 1;
            if (claimingAge < RetirementAge)
            {
                int monthsEarly = (RetirementAge - claimingAge) * 12;
                change -= monthsEarly * 0.0056; // ~0.56% per month
            }
            else if (claimingAge > RetirementAge && claimingAge <= 70)
            {
                int monthsDelayed = (claimingAge - RetirementAge) * 12;
                change += monthsDelayed * 0.00667; // 0.667% per month (8% per year)
            }

            return pia * change;
        }

        public static int GetFullRetirementAge(int birthYear)
        {
            if (birthYear >= 1960) return IRS.SocialSecurity.MIN_RETIREMENT_AGE; // FRA for those born in 1960 or later
            if (birthYear >= 1955) return IRS.SocialSecurity.MIN_RETIREMENT_AGE_1955 + (birthYear - 1954); // FRA gradually increases
            return IRS.SocialSecurity.MIN_RETIREMENT_AGE_1955; // FRA for those born before 1954
        }

        public static double CalculateSocialSecurityBenefit(int birthYear, int claimingAge, double averageEarnings)
        {
            int fra = GetFullRetirementAge(birthYear);
            double pia = IRS.SocialSecurity.CalculatePIA(averageEarnings);
            double adjustedBenefit = IRS.SocialSecurity.AdjustForClaimingAge(pia, claimingAge, birthYear);
            int yearsUntilClaiming = claimingAge - (DateTime.Now.Year - birthYear);

            return ApplyCOLA(adjustedBenefit, yearsUntilClaiming);
        }

        public static double ApplyCOLA(double benefit, int yearsUntilClaiming, double colaRate = 0.02) => benefit * Math.Pow(1 + colaRate, yearsUntilClaiming);
    }

