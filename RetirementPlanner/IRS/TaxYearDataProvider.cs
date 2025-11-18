using System.Text.Json;

namespace RetirementPlanner.IRS;

/// <summary>
/// Provides year-specific federal tax bracket data and standard deduction amounts.
/// Now loads from JSON configuration to allow easy updates without code changes.
/// </summary>
public class TaxYearDataProvider
{
    private readonly Dictionary<int, Dictionary<TaxBrackets.FileType, TaxBrackets.Bracket[]>> _bracketsByYear = new();
    private readonly Dictionary<int, Dictionary<TaxBrackets.FileType, double>> _standardDeductionByYear = new();

    public TaxYearDataProvider(Stream jsonStream)
    {
        using var doc = JsonDocument.Parse(jsonStream);
        LoadFromJson(doc.RootElement);
    }

    public TaxYearDataProvider(string jsonFilePath)
    {
        using var stream = File.OpenRead(jsonFilePath);
        using var doc = JsonDocument.Parse(stream);
        LoadFromJson(doc.RootElement);
    }

    public TaxYearDataProvider() : this(Path.Combine(AppContext.BaseDirectory, "IRS", "tax-data.json"))
    {
    }

    private void LoadFromJson(JsonElement root)
    {
        if (!root.TryGetProperty("years", out var years)) return;
        foreach (var yearNode in years.EnumerateArray())
        {
            int year = yearNode.GetProperty("year").GetInt32();

            // Standard deduction
            var sdMap = new Dictionary<TaxBrackets.FileType, double>();
            if (yearNode.TryGetProperty("standardDeduction", out var sd))
            {
                foreach (var prop in sd.EnumerateObject())
                {
                    if (Enum.TryParse<TaxBrackets.FileType>(prop.Name, out var fileType))
                    {
                        sdMap[fileType] = prop.Value.GetDouble();
                    }
                }
            }
            _standardDeductionByYear[year] = sdMap;

            // Brackets
            var bracketsMap = new Dictionary<TaxBrackets.FileType, TaxBrackets.Bracket[]>();
            if (yearNode.TryGetProperty("brackets", out var bracketsPerFiling))
            {
                foreach (var filingKvp in bracketsPerFiling.EnumerateObject())
                {
                    if (!Enum.TryParse<TaxBrackets.FileType>(filingKvp.Name, out var ft))
                        continue;

                    var list = new List<TaxBrackets.Bracket>();
                    foreach (var br in filingKvp.Value.EnumerateArray())
                    {
                        list.Add(new TaxBrackets.Bracket
                        {
                            LowerBound = br.GetProperty("lowerBound").GetDouble(),
                            UpperBound = br.GetProperty("upperBound").GetDouble(),
                            Rate = br.GetProperty("rate").GetDouble()
                        });
                    }
                    bracketsMap[ft] = list.ToArray();
                }
            }
            _bracketsByYear[year] = bracketsMap;
        }
    }

    public TaxBrackets.Bracket[]? GetBracketsForYear(int year, TaxBrackets.FileType fileType)
    {
        if (_bracketsByYear.TryGetValue(year, out var byFs) && byFs.TryGetValue(fileType, out var arr))
            return arr;
        return null;
    }

    public double? GetBaseStandardDeduction(int year, TaxBrackets.FileType fileType)
    {
        if (_standardDeductionByYear.TryGetValue(year, out var byFs) && byFs.TryGetValue(fileType, out var amt))
            return amt;
        return null;
    }

    // Static convenience methods while we thread DI through
    private static TaxYearDataProvider? _default;
    public static void ConfigureDefault(TaxYearDataProvider provider) => _default = provider;
    private static TaxYearDataProvider Default => _default ??= new TaxYearDataProvider();

    public static TaxBrackets.Bracket[]? GetBracketsForYearStatic(int year, TaxBrackets.FileType fileType)
        => Default.GetBracketsForYear(year, fileType);

    public static double? GetBaseStandardDeductionStatic(int year, TaxBrackets.FileType fileType)
        => Default.GetBaseStandardDeduction(year, fileType);
}
