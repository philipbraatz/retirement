namespace RetirementPlanner.Calculators;
public static class SEPPCalculator
{
    public static double CalculateAnnualDistribution(double accountBalance,int age,double interestRate)
    {
        interestRate = Math.Clamp(interestRate,0.01,0.05);
        double lifeExpectancy = GetSingleLifeExpectancy(age);
        if (lifeExpectancy <= 0) return 0;
        double r = interestRate; double n = lifeExpectancy;
        return accountBalance * r / (1 - Math.Pow(1+r,-n));
    }
    private static double GetSingleLifeExpectancy(int age) => age switch { <50 =>35, <55=>30, <60=>26, <65=>22, <70=>18, <75=>14, <80=>11, <85=>8, <90=>6, _=>5 };
}