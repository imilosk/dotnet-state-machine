namespace DotnetStateMachine;

public struct TransitionConfiguration<TState, TTrigger> where TState : notnull where TTrigger : notnull
{
    public TTrigger Trigger { get; }
    public TState DestinationState { get; }
    public TriggerType TriggerType { get; }
    public Action<TTrigger>? InternalTransitionAction { get; }

    public TransitionConfiguration(TTrigger trigger, TState destinationState, TriggerType triggerType,
        Action<TTrigger>? internalTransitionAction)
    {
        Trigger = trigger;
        DestinationState = destinationState;
        TriggerType = triggerType;
        InternalTransitionAction = internalTransitionAction;
    }
}