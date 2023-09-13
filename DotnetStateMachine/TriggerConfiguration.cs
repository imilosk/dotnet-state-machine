namespace DotnetStateMachine;

public struct TriggerConfiguration<TState, TTrigger> where TState : notnull where TTrigger : notnull
{
    public TTrigger Trigger { get; }
    public TState State { get; }
    public TriggerType TriggerType { get; }

    public TriggerConfiguration(TTrigger trigger, TState state, TriggerType triggerType)
    {
        Trigger = trigger;
        State = state;
        TriggerType = triggerType;
    }
}