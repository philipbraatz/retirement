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
    public AmtConfig? Amt { get; set; } // AMT configuration
    public Dictionary<string, double>? NiitThresholds { get; set; }
    public double? NiitRate { get; set; }
    public Dictionary<string, double>? AdditionalMedicareThresholds { get; set; }
    public double? AdditionalMedicareRate { get; set; }
    public Dictionary<string, double>? AdditionalDeductionAmounts { get; set; }
    public Dictionary<string, double>? SocialSecurityThresholds { get; set; }
    public Dictionary<string, double>? ContributionLimits { get; set; }
    public Dictionary<string, double>? RothIraPhaseOut { get; set; }
    public Dictionary<string, double>? TraditionalIraDeductPhaseOutCovered { get; set; }
    public Dictionary<string, double>? TraditionalIraDeductPhaseOutSpousal { get; set; }
}

public class TaxBracketConfig
{
    public double LowerBound { get; set; }
    public double UpperBound { get; set; }
    public double Rate { get; set; }
}

public class AmtConfig
{
    public Dictionary<string, double> Exemption { get; set; } = [];
    public Dictionary<string, double> PhaseOutStart { get; set; } = [];
    public Dictionary<string, double> Breakpoint28Rate { get; set; } = [];
    public AmtRateConfig Rates { get; set; } = new();
}

public class AmtRateConfig
{
    public double Primary { get; set; }
    public double Secondary { get; set; }
}
