namespace RetirementPlanner;

public static class TaxBrackets
{
    public class Bracket
    {
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public double Rate { get; set; }
    }
    public enum FileType
    {
        Single = 13850,
        MarriedFilingJointly = 27700,
        HeadOfHousehold = 20800
    }

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
    public static double CalculateTaxes(FileType fileType, double income) => Brackets[fileType]
            .Where(bracket => income > bracket.LowerBound)
            .Sum(bracket => (Math.Min(income, bracket.UpperBound) - bracket.LowerBound) * bracket.Rate);

    public static double GetOptimalRothConversionAmount(Person person, DateOnly date)
    {
        double taxableIncome = person.IncomeYearly + person.Investments.Accounts.FirstOrDefault(f => f is RothIRAAccount)
            .DepositHistory.Where(d => d.Date.Year == date.Year).Sum(d => d.Amount);

        // Find the highest bracket we can fill without exceeding
        foreach (var bracket in Brackets[person.FileType])
        {
            if (taxableIncome < bracket.UpperBound)
            {
                return bracket.UpperBound - taxableIncome; // Fill the bracket
            }
        }

        return 0;
    }

}
