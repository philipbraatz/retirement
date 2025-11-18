namespace RetirementPlanner;

public static class TaxBrackets
{
    public class Bracket
    {
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public double Rate { get; set; }
    }

    // Filing statuses (no embedded numeric values)
    public enum FileType
    {
        Single,
        MarriedFilingJointly,
        MarriedFilingSeparately,
        HeadOfHousehold
    }

    // Default (2024) brackets retained as fallback
    public static Dictionary<FileType, Bracket[]> Brackets = new()
    {
        {
            FileType.Single,
            [
                new Bracket { LowerBound = 0, UpperBound = 11600, Rate = 0.10 },
                new Bracket { LowerBound = 11600, UpperBound = 47150, Rate = 0.12 },
                new Bracket { LowerBound = 47150, UpperBound = 100525, Rate = 0.22 },
                new Bracket { LowerBound = 100525, UpperBound = 191950, Rate = 0.24 },
                new Bracket { LowerBound = 191950, UpperBound = 243725, Rate = 0.32 },
                new Bracket { LowerBound = 243725, UpperBound = 609350, Rate = 0.35 },
                new Bracket { LowerBound = 609350, UpperBound = double.MaxValue, Rate = 0.37 }
            ]
        },
        {
            FileType.MarriedFilingJointly,
            [
                new Bracket { LowerBound = 0, UpperBound = 23200, Rate = 0.10 },
                new Bracket { LowerBound = 23200, UpperBound = 94300, Rate = 0.12 },
                new Bracket { LowerBound = 94300, UpperBound = 201050, Rate = 0.22 },
                new Bracket { LowerBound = 201050, UpperBound = 383900, Rate = 0.24 },
                new Bracket { LowerBound = 383900, UpperBound = 487450, Rate = 0.32 },
                new Bracket { LowerBound = 487450, UpperBound = 731200, Rate = 0.35 },
                new Bracket { LowerBound = 731200, UpperBound = double.MaxValue, Rate = 0.37 }
            ]
        },
        {
            FileType.MarriedFilingSeparately,
            [
                new Bracket { LowerBound = 0, UpperBound = 11600, Rate = 0.10 },
                new Bracket { LowerBound = 11600, UpperBound = 47150, Rate = 0.12 },
                new Bracket { LowerBound = 47150, UpperBound = 100525, Rate = 0.22 },
                new Bracket { LowerBound = 100525, UpperBound = 191950, Rate = 0.24 },
                new Bracket { LowerBound = 191950, UpperBound = 243725, Rate = 0.32 },
                new Bracket { LowerBound = 243725, UpperBound = 365600, Rate = 0.35 },
                new Bracket { LowerBound = 365600, UpperBound = double.MaxValue, Rate = 0.37 }
            ]
        },
        {
            FileType.HeadOfHousehold,
            [
                new Bracket { LowerBound = 0, UpperBound = 16550, Rate = 0.10 },
                new Bracket { LowerBound = 16550, UpperBound = 63100, Rate = 0.12 },
                new Bracket { LowerBound = 63100, UpperBound = 100500, Rate = 0.22 },
                new Bracket { LowerBound = 100500, UpperBound = 191950, Rate = 0.24 },
                new Bracket { LowerBound = 191950, UpperBound = 243700, Rate = 0.32 },
                new Bracket { LowerBound = 243700, UpperBound = 609350, Rate = 0.35 },
                new Bracket { LowerBound = 609350, UpperBound = double.MaxValue, Rate = 0.37 }
            ]
        }
    };

    // Year-aware calculation using JSON-backed provider; falls back to default
    public static double CalculateTaxes(FileType fileType, double income, int taxYear)
    {
        var brackets = IRS.TaxYearDataProvider.GetBracketsForYearStatic(taxYear, fileType) ?? Brackets[fileType];
        return brackets
            .Where(bracket => income > bracket.LowerBound)
            .Sum(bracket => (Math.Min(income, bracket.UpperBound) - bracket.LowerBound) * bracket.Rate);
    }

    // Backward-compatible overload (assumes current year)
    public static double CalculateTaxes(FileType fileType, double income) => CalculateTaxes(fileType, income, DateTime.Now.Year);

    public static double GetOptimalRothConversionAmount(Person person, DateOnly date)
    {
        double taxableIncome = person.IncomeYearly + person.Investments.Accounts.FirstOrDefault(f => f is RothIRAAccount)
            .DepositHistory.Where(d => d.Date.Year == date.Year).Sum(d => d.Amount);

        var brackets = IRS.TaxYearDataProvider.GetBracketsForYearStatic(date.Year, person.FileType) ?? Brackets[person.FileType];

        // Find the highest bracket we can fill without exceeding
        foreach (var bracket in brackets)
        {
            if (taxableIncome < bracket.UpperBound)
            {
                return bracket.UpperBound - taxableIncome; // Fill the bracket
            }
        }

        return 0;
    }

    /// <summary>
    /// Get the marginal tax rate for a given income and filing type
    /// </summary>
    public static double GetMarginalTaxRate(FileType fileType, double income, int taxYear)
    {
        var brackets = IRS.TaxYearDataProvider.GetBracketsForYearStatic(taxYear, fileType) ?? Brackets[fileType];

        foreach (var bracket in brackets)
        {
            if (income >= bracket.LowerBound && income < bracket.UpperBound)
            {
                return bracket.Rate;
            }
        }

        // If income exceeds all brackets, return the highest bracket rate
        return brackets.Last().Rate;
    }

    // Backward-compatible marginal rate method (assumes current year)
    public static double GetMarginalTaxRate(FileType fileType, double income)
        => GetMarginalTaxRate(fileType, income, DateTime.Now.Year);
}
