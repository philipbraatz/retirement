using System.Text.Json;
using RetirementPlanner.IRS; // for TaxData, TaxYearConfig
namespace RetirementPlanner.Calculators;
public static class AMTCalculator
{
    private static TaxData _cached = new();
    private static TaxData LoadData()
    {
        if (_cached.Years.Count > 0) return _cached;
        string path = Path.Combine(AppContext.BaseDirectory, "IRS", "tax-data.json");
        using var stream = File.OpenRead(path);
        _cached = JsonSerializer.Deserialize<TaxData>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TaxData();
        return _cached;
    }

    private static (double Exemption,double PhaseOutStart,double BreakpointPrimary,double Rate1,double Rate2) LoadConfig(int year, TaxBrackets.FileType filingStatus)
    {
        var data = LoadData();
        var yearCfg = data.Years.FirstOrDefault(y => y.Year == year);
        if (yearCfg?.Amt == null) return (0,double.MaxValue,double.MaxValue,0.26,0.28);
        string key = filingStatus.ToString();
        double exemption = yearCfg.Amt.Exemption.TryGetValue(key, out var ex) ? ex : 0;
        double phaseOutStart = yearCfg.Amt.PhaseOutStart.TryGetValue(key, out var po) ? po : double.MaxValue;
        double breakpointPrimary = yearCfg.Amt.Breakpoint28Rate.TryGetValue(key, out var bp) ? bp : (yearCfg.Amt.Breakpoint28Rate.TryGetValue("Default", out var defBp) ? defBp : double.MaxValue);
        double rate1 = yearCfg.Amt.Rates.Primary;
        double rate2 = yearCfg.Amt.Rates.Secondary;
        return (exemption, phaseOutStart, breakpointPrimary, rate1, rate2);
    }

    public static double CalculateAMTI(double regularTaxableIncome,double preferenceItems) => regularTaxableIncome + preferenceItems;

    public static double CalculateTentativeMinimumTax(double amti,int year,TaxBrackets.FileType filingStatus)
    {
        var cfg = LoadConfig(year, filingStatus);
        double first = Math.Min(amti, cfg.BreakpointPrimary) * cfg.Rate1;
        double second = amti > cfg.BreakpointPrimary ? (amti - cfg.BreakpointPrimary) * cfg.Rate2 : 0;
        return first + second;
    }

    public static double GetAMTExemption(double amti,int year,TaxBrackets.FileType filingStatus)
    {
        var cfg = LoadConfig(year, filingStatus);
        if (amti <= cfg.PhaseOutStart) return cfg.Exemption;
        double reduction = (amti - cfg.PhaseOutStart) * 0.25;
        return Math.Max(0, cfg.Exemption - reduction);
    }

    public static double CalculateAMT(double regularTax,double regularTaxableIncome,double preferenceItems,int year,TaxBrackets.FileType filingStatus)
    {
        double amti = CalculateAMTI(regularTaxableIncome, preferenceItems);
        double exemption = GetAMTExemption(amti, year, filingStatus);
        double taxableAMT = Math.Max(0, amti - exemption);
        double tmt = CalculateTentativeMinimumTax(taxableAMT, year, filingStatus);
        return Math.Max(0, tmt - regularTax);
    }
}