namespace RetirementPlanner.IRS;

public class TaxData
{
    public List<TaxYearConfig> Years { get; set; } = new();
}

public class TaxYearConfig
{
    public int Year { get; set; }
    public Dictionary<string, double> StandardDeduction { get; set; } = new();
    public Dictionary<string, List<TaxBracketConfig>> Brackets { get; set; } = new();
}

public class TaxBracketConfig
{
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public double Rate { get; set; }
}
