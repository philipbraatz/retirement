using static RetirementPlanner.InvestmentAccount;
namespace RetirementPlanner.Calculators;
public static class RMDCalculator
{
    private static readonly Dictionary<int,double> UniformLifetimeTable = new(){ {72,27.4},{73,26.5},{74,25.5},{75,24.6},{76,23.7},{77,22.9},{78,22.0},{79,21.1},{80,20.2},{81,19.4},{82,18.5},{83,17.7},{84,16.8},{85,16.0},{86,15.2},{87,14.4},{88,13.7},{89,12.9},{90,12.2},{91,11.5},{92,10.8},{93,10.1},{94,9.5},{95,8.9},{96,8.4},{97,7.8},{98,7.3},{99,6.8},{100,6.4},{101,6.0},{102,5.6},{103,5.2},{104,4.9},{105,4.6},{106,4.3},{107,4.1},{108,3.9},{109,3.7},{110,3.5},{111,3.4},{112,3.3},{113,3.1},{114,3.0},{115,2.9},{116,2.8},{117,2.7},{118,2.5},{119,2.3},{120,2.0} };
    private static readonly Dictionary<int,double> JointLifeExpectancyTable = new(){ {72,29.9},{73,29.0},{74,28.1},{75,27.2},{76,26.4},{77,25.5},{78,24.7},{79,23.8},{80,22.9},{81,22.1},{82,21.2},{83,20.4},{84,19.5},{85,18.7},{86,17.9},{87,17.1},{88,16.3},{89,15.5},{90,14.7},{91,13.9},{92,13.1},{93,12.3},{94,11.5},{95,10.8},{96,10.1},{97,9.5},{98,8.9},{99,8.4},{100,7.9} };
    public static double CalculateRMD(InvestmentAccount account,int age,double priorYearEndBalance,bool isStillWorking=false,DateTime? spouseBirthDate=null)
    {
        int rmdStartAge = GetRMDStartAge(account);
        if (age < rmdStartAge) return 0;
        if (isStillWorking && (account.Type==AccountType.Traditional401k||account.Type==AccountType.Traditional403b)) return 0;
        if (account.Type==AccountType.RothIRA) return 0;
        bool useJoint = false;
        if (spouseBirthDate.HasValue)
        {
            int spouseAge = DateTime.Now.Year - spouseBirthDate.Value.Year;
            useJoint = spouseAge + 10 < age;
        }
        double factor = useJoint ? GetJointLifeFactor(age) : GetLifeExpectancyFactor(age);
        if (factor <= 0) return priorYearEndBalance;
        return priorYearEndBalance / factor;
    }
    private static double GetJointLifeFactor(int age){ if (JointLifeExpectancyTable.TryGetValue(age,out var f)) return f; if (age>100) return 7.9; return 0; }
    private static int GetRMDStartAge(InvestmentAccount account){ int birthYear = DateTime.Now.Year - 90; switch(account){ case Traditional401kAccount t401k: birthYear = ((DateOnly)t401k.GetType().GetField("birthdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(t401k)).Year; break; case Roth401kAccount r401k: birthYear = ((DateOnly)r401k.GetType().GetField("birthdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(r401k)).Year; break; case TraditionalIRAAccount ira: birthYear = ira.Owner.BirthDate.Year; break;} return GetRMDStartAge(birthYear); }
    public static int GetRMDStartAge(int birthYear){ if (birthYear < 1951) return 72; if (birthYear <= 1959) return 73; return 75; }
    private static double GetLifeExpectancyFactor(int age){ if (UniformLifetimeTable.TryGetValue(age,out var f)) return f; if (age>120) return 2.0; return 0; }
    public static bool IsSubjectToRMD(AccountType type)=> type is AccountType.Traditional401k or AccountType.Traditional403b or AccountType.TraditionalIRA or AccountType.SEPIRA or AccountType.SIMPLEIRA;
    public static (double StandardPenalty,double CorrectedPenalty) CalculateRMDPenalty(double required,double actual){ double shortfall = Math.Max(0, required - actual); return (shortfall*0.25, shortfall*0.10); }
}