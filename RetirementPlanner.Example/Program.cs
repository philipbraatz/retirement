// See https://aka.ms/new-console-template for more information
using RetirementPlanner;
using RetirementPlanner.Event;
using RetirementPlanner.Test;

Console.WriteLine("Hello, World!");

List<RetirementPlanner.RetirementPlanner> planners = [];

foreach (var person in (Person[])[//TestPersonFactory.CreateEarlyRetiree(),
                                  //TestPersonFactory.CreateLateRetiree(),
                                  //TestPersonFactory.CreateNormalRetiree(),
                                  TestPersonFactory.Me()])
{
    RetirementPlanner.RetirementPlanner planner = new(person);
    LifeEvents.Subscribe(planner);
    planners.Add(new(person));

}

planners.ForEach(f => f.RunRetirementSimulation().Wait());