namespace RetirementPlanner;

public class Job
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

    public double CalculateAnnualIncome()
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
    } + BonusPay / 12;

    public void ApplyYearlyPayRaise()
    {
        Salary *= (1 + PayRaisePercent);
    }

    public double CalculatePersonal401kContribution() => Personal401kContributionPercent.HasValue ? CalculateAnnualIncome() * Personal401kContributionPercent.Value : 0;

    public double CalculateCompanyMatchContribution() => CompanyMatchContributionPercent.HasValue ? CalculateAnnualIncome() * CompanyMatchContributionPercent.Value : 0;

    public double CalculateTaxableIncome() => CalculateAnnualIncome() - CalculatePersonal401kContribution();
    public double CalculateNetPay(double taxRate)
    {
        var grossIncome = CalculateAnnualIncome();
        var personal401kContribution = CalculatePersonal401kContribution();
        var taxableIncome = grossIncome - personal401kContribution;
        var taxesOwed = taxableIncome * taxRate;
        return grossIncome - personal401kContribution - taxesOwed;
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

