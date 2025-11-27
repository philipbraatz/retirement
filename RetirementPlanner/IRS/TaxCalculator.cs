// Moved to Calculators/TaxCalculator.cs
// This file retained as shim for backward compatibility.
using RetirementPlanner.Calculators;
using static RetirementPlanner.TaxBrackets;
namespace RetirementPlanner.IRS;
public class TaxCalculatorShim(Person person, int taxYear) : TaxCalculator(person, taxYear) { }