using RetirementPlanner;

// Create a simple test to debug the balance issue
var account = new Traditional401kAccount(0.05, "Test Account", new DateOnly(1990, 1, 1), 1000);
var testDate = new DateOnly(2025, 1, 15);

Console.WriteLine($"Initial balance: {account.Balance(testDate):C}");

// Check YearlyStartingBalances
Console.WriteLine("YearlyStartingBalances:");
foreach (var yearBalance in account.YearlyStartingBalances)
{
    Console.WriteLine($"  Year {yearBalance.Key}: {yearBalance.Value:C}");
}

// Check DepositHistory
Console.WriteLine("DepositHistory:");
foreach (var deposit in account.DepositHistory)
{
    Console.WriteLine($"  {deposit.Date}: {deposit.Amount:C} ({deposit.Category})");
}

// Apply growth
Console.WriteLine("\n--- Applying Growth ---");
account.ApplyMonthlyGrowth(testDate);

Console.WriteLine($"\nBalance after growth: {account.Balance(testDate):C}");

// Check DepositHistory again
Console.WriteLine("DepositHistory after growth:");
foreach (var deposit in account.DepositHistory)
{
    Console.WriteLine($"  {deposit.Date}: {deposit.Amount:C} ({deposit.Category})");
}

// Check YearlyStartingBalances again
Console.WriteLine("YearlyStartingBalances after growth:");
foreach (var yearBalance in account.YearlyStartingBalances)
{
    Console.WriteLine($"  Year {yearBalance.Key}: {yearBalance.Value:C}");
}
