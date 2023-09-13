namespace DotnetStateMachine;

public struct TriggerConfiguration<TState, TTrigger> where TState : notnull where TTrigger : notnull
{
    public TTrigger Trigger { get; }
    public TState State { get; }
    public bool IsIgnored { get; }

    public TriggerConfiguration(TTrigger trigger, TState state, bool isIgnored)
    {
        Trigger = trigger;
        State = state;
        IsIgnored = isIgnored;
    }
}