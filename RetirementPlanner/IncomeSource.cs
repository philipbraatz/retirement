namespace RetirementPlanner;

public class IncomeSource
{
    public string Title { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public double HoursWorkedWeekly { get; set; }
    public double Salary { get; set; } // For salaried jobs, this is the annual salary. For hourly jobs, this is the hourly rate.
    public double PayRaisePercent { get; set; }
    public double BonusPay { get; set; }
    public JobType Type { get; set; }
    public PaymentType PaymentType { get; set; }
    public double? Personal401kContributionPercent { get; set; }
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

    public double CalculatePersonal401kContribution() => Personal401kContributionPercent.HasValue ? GrossAnnualIncome() * Personal401kContributionPercent.Value : 0;

    public double CalculateCompanyMatchContribution() => CompanyMatchContributionPercent.HasValue ? GrossAnnualIncome() * CompanyMatchContributionPercent.Value : 0;

    public double CalculateTaxableIncome() => GrossAnnualIncome() - CalculatePersonal401kContribution();
    public double CalculateNetPay(double taxRate)
    {
        var grossIncome = GrossAnnualIncome();
        var personal401kContribution = CalculatePersonal401kContribution();
        var taxableIncome = grossIncome - personal401kContribution;
        var taxesOwed = taxableIncome * taxRate;
        return grossIncome - personal401kContribution - taxesOwed;
    }


    public bool IsPayday(DateOnly date) => PayFrequency switch
    {
        PayFrequency.Weekly => date.DayOfWeek == DayOfWeek.Friday,
        PayFrequency.BiWeekly => (date.DayOfYear % 14) == 0,
        PayFrequency.SemiMonthly => date.Day == 15 || date.Day == DateTime.DaysInMonth(date.Year, date.Month),
        PayFrequency.Monthly => date.Day == DateTime.DaysInMonth(date.Year, date.Month),
        _ => false
    };
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