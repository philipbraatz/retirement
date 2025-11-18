namespace RetirementPlanner.Event;

/// <summary>
/// Represents a milestone event that knows its own trigger condition
/// </summary>
public class MilestoneEvent
{
    /// <summary>
    /// The name of this milestone
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Predicate that determines if this milestone should trigger
    /// </summary>
    public Func<Person, int, bool> TriggerCondition { get; }
    
    /// <summary>
    /// The event handler that will be invoked when triggered
    /// </summary>
    public EventHandler<DatedEventArgs>? EventHandler { get; set; }
    
    /// <summary>
    /// Whether this milestone has already been triggered
    /// </summary>
    public bool HasTriggered { get; private set; }
    
    /// <summary>
    /// Optional action to execute when the milestone triggers (in addition to invoking the event)
    /// </summary>
    public Action<DateOnly, int>? OnTrigger { get; set; }

    public MilestoneEvent(
        string name, 
        Func<Person, int, bool> triggerCondition, 
        EventHandler<DatedEventArgs>? eventHandler = null,
        Action<DateOnly, int>? onTrigger = null)
    {
        Name = name;
        TriggerCondition = triggerCondition;
        EventHandler = eventHandler;
        OnTrigger = onTrigger;
        HasTriggered = false;
    }

    /// <summary>
    /// Check if this milestone should trigger and invoke it if so
    /// </summary>
    /// <param name="person">The person context</param>
    /// <param name="currentAge">Current age</param>
    /// <param name="date">Current date</param>
    /// <param name="sender">Event sender (typically the RetirementPlanner instance)</param>
    /// <returns>True if the milestone was triggered</returns>
    public bool CheckAndTrigger(Person person, int currentAge, DateOnly date, object sender)
    {
        // If already triggered or condition not met, return false
        if (HasTriggered || !TriggerCondition(person, currentAge))
        {
            return false;
        }

        // Mark as triggered
        HasTriggered = true;

        // Invoke the event handler
        EventHandler?.Invoke(sender, new DatedEventArgs { Date = date, Age = currentAge });

        // Execute the optional action
        OnTrigger?.Invoke(date, currentAge);

        return true;
    }

    /// <summary>
    /// Reset the triggered state (useful for testing or multi-run scenarios)
    /// </summary>
    public void Reset()
    {
        HasTriggered = false;
    }
}
