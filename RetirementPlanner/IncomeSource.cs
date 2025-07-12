namespace RetirementPlanner;

public class IncomeSource
{
    public required string Title { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public double HoursWorkedWeekly { get; set; }
    public double Salary { get; set; } // For salaried jobs, this is the annual salary. For hourly jobs, this is the hourly rate.
    public double PayRaisePercent { get; set; }
    public double BonusPay { get; set; }
    public JobType Type { get; set; }
    public PaymentType PaymentType { get; set; }
    public double? RetirementContributionPercent { get; set; }
    public double? CompanyMatchContributionPercent { get; set; }
    public PayFrequency PayFrequency { get; set; }

    public double GrossAnnualIncome()
    {
        double baseIncome = PaymentType switch
        {
            PaymentType.Hourly => Salary * HoursWorkedWeekly * 52,
            PaymentType.Salaried => Salary,
            _ => 0
        };
        return baseIncome + BonusPay;
    }

    public double CalculateMonthlyIncome(double hoursWorked) => PaymentType switch
    {
        PaymentType.Hourly => Salary * hoursWorked * 4,
        PaymentType.Salaried => Salary / 12,
        _ => 0
    } + (BonusPay / 12);

    public void ApplyYearlyPayRaise() => Salary *= 1 + PayRaisePercent;

    public double CalculateRetirementContribution() => RetirementContributionPercent.HasValue ? GrossAnnualIncome() * RetirementContributionPercent.Value : 0;

    public double CalculateCompanyMatchContribution() => CompanyMatchContributionPercent.HasValue ? GrossAnnualIncome() * CompanyMatchContributionPercent.Value : 0;

    public double CalculateTaxableIncome() => GrossAnnualIncome() - CalculateRetirementContribution();
    public double CalculateNetPay(double taxRate)
    {
        var grossIncome = GrossAnnualIncome();
        var retirementContribution = CalculateRetirementContribution();
        var taxableIncome = grossIncome - retirementContribution;
        var taxesOwed = taxableIncome * taxRate;
        return grossIncome - retirementContribution - taxesOwed;
    }


    public bool IsPayday(DateOnly date) => PayFrequency switch
    {
        PayFrequency.Weekly => date.DayOfWeek == DayOfWeek.Friday,
        PayFrequency.BiWeekly => (date.DayOfYear % 14) == 0,
        PayFrequency.SemiMonthly => date.Day == 15 || date.Day == DateTime.DaysInMonth(date.Year, date.Month),
        PayFrequency.Monthly => date.Day == DateTime.DaysInMonth(date.Year, date.Month),
        _ => false
    };

    /// <summary>
    /// Determines the optimal allocation between Traditional and Roth 401k contributions
    /// based on current tax bracket, retirement contribution amount, and early retirement strategy.
    /// Always ensures we get the full employer match first.
    /// 
    /// Enhanced Strategy:
    /// - Always get full employer match in Traditional 401k
    /// - Low tax brackets (≤12%): Favor Roth for tax-free growth
    /// - High tax brackets (≥22%): Consider early retirement needs
    ///   * Early retirement candidates: 60% Roth for penalty-free bridge funding
    ///   * Young professionals (age <40): 60% Roth for long-term flexibility
    ///   * Others: Traditional for immediate tax deduction
    /// - Medium tax brackets (12-22%): Split based on early retirement plans
    /// </summary>
    public (double traditionalAmount, double rothAmount) CalculateOptimalRetirementAllocation(Person person, DateOnly date)
    {
        if (!RetirementContributionPercent.HasValue)
            return (0, 0);

        double totalContribution = CalculateRetirementContribution();
        if (totalContribution <= 0)
            return (0, 0);

        // Calculate employer match amount we need to contribute to get full match
        double employerMatchRate = CompanyMatchContributionPercent ?? 0;
        double grossIncome = GrossAnnualIncome();
        double requiredForMaxMatch = grossIncome * employerMatchRate;
        
        // Ensure we contribute at least enough to get full employer match
        // This should go to Traditional 401k since employer match goes there
        double guaranteedTraditional = Math.Min(totalContribution, requiredForMaxMatch);
        double remainingContribution = totalContribution - guaranteedTraditional;

        if (remainingContribution <= 0)
        {
            // All contribution needed for employer match
            return (guaranteedTraditional, 0);
        }

        // Calculate current taxable income for remaining allocation
        double currentTaxableIncome = person.Jobs.Sum(j => j.CalculateTaxableIncome());
        
        // Get current marginal tax rate
        double marginalRate = TaxBrackets.GetMarginalTaxRate(person.FileType, currentTaxableIncome);

        // Enhanced allocation logic considering early retirement strategy
        var currentAge = person.CurrentAge(date);
        var yearsToFullRetirement = person.FullRetirementAge - currentAge;
        
        // Low tax bracket threshold (12% or lower) - prefer Roth
        // Higher tax bracket (22% or higher) - prefer Traditional
        const double lowTaxBracketThreshold = 0.12;
        const double highTaxBracketThreshold = 0.22;

        double additionalTraditional = 0;
        double additionalRoth = 0;

        // Special consideration for early retirement scenarios
        bool isEarlyRetirementCandidate = person.PartTimeAge > 0 && person.PartTimeAge < person.FullRetirementAge;
        bool isYoungProfessional = currentAge < 40;
        
        if (marginalRate <= lowTaxBracketThreshold)
        {
            // Low tax bracket - strongly prefer Roth for tax-free growth
            additionalRoth = remainingContribution;
        }
        else if (marginalRate >= highTaxBracketThreshold)
        {
            // High tax bracket - but consider early retirement strategy
            if (isEarlyRetirementCandidate || (isYoungProfessional && yearsToFullRetirement > 30))
            {
                // Even in higher tax brackets, prioritize Roth for early retirement flexibility
                // Split more favorably toward Roth for early retirement access
                additionalTraditional = remainingContribution * 0.4; // 40% traditional
                additionalRoth = remainingContribution * 0.6; // 60% roth for early retirement bridge
            }
            else
            {
                // Standard high tax bracket - prefer Traditional for immediate tax deduction
                additionalTraditional = remainingContribution;
            }
        }
        else
        {
            // Medium tax bracket (12-22%) - consider early retirement strategy
            if (isEarlyRetirementCandidate || isYoungProfessional)
            {
                // Favor Roth for early retirement flexibility
                additionalTraditional = remainingContribution * 0.3; // 30% traditional
                additionalRoth = remainingContribution * 0.7; // 70% roth for early retirement access
            }
            else
            {
                // Standard medium tax bracket split
                additionalTraditional = remainingContribution * 0.6; // 60% traditional
                additionalRoth = remainingContribution * 0.4; // 40% roth
            }
        }

        return (guaranteedTraditional + additionalTraditional, additionalRoth);
    }
}

public enum JobType
{
    PartTime,
    FullTime,
    Unemployed,
    Independent
}

public enum PaymentType
{
    Hourly,
    Salaried
}

public enum PayFrequency
{
    Weekly = 52,
    BiWeekly = 52 / 2,
    SemiMonthly = 12 * 2,
    Monthly = 12
}