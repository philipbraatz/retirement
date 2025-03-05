// See https://aka.ms/new-console-template for more information
using RetirementPlanner;
using RetirementPlanner.Test;

Console.WriteLine("Hello, World!");


foreach (var person in (Person[])[//TestPersonFactory.CreateEarlyRetiree(),
                                  //TestPersonFactory.CreateLateRetiree(),
                                  //TestPersonFactory.CreateNormalRetiree(),
                                  TestPersonFactory.Me()])
{
    RetirementPlanner.RetirementPlanner.RunRetirementSimulation(person);
}