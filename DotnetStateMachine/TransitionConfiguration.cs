namespace DotnetStateMachine;

public struct TransitionConfiguration<TState, TTrigger, TContext> where TState : notnull
    where TTrigger : notnull
    where TContext : notnull
{
    public TTrigger Trigger { get; }
    public TState DestinationState { get; }
    public TriggerType TriggerType { get; }
    public Action<TTrigger, TContext>? InternalTransitionAction { get; }

    public TransitionConfiguration(TTrigger trigger, TState destinationState, TriggerType triggerType,
        Action<TTrigger, TContext>? internalTransitionAction)
    {
        Trigger = trigger;
        DestinationState = destinationState;
        TriggerType = triggerType;
        InternalTransitionAction = internalTransitionAction;
    }
}