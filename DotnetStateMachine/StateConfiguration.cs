namespace DotnetStateMachine;

public class StateConfiguration<TState, TTrigger, TContext>
    where TState : notnull where TTrigger : notnull where TContext : notnull
{
    private readonly TState _state;
    public Dictionary<TTrigger, TransitionConfiguration<TState, TTrigger>> AllowedTransitions { get; } = new();
    internal Action<TTrigger, TContext>? OnEntryAction { get; set; }
    internal Action<TTrigger, TContext>? OnExitAction { get; set; }

    public StateConfiguration(TState state)
    {
        _state = state;
    }

    private StateConfiguration<TState, TTrigger, TContext> AddAllowedTransition(TTrigger trigger,
        TState destinationState,
        TriggerType triggerType, Action<TTrigger>? internalTransitionAction = null)
    {
        if (AllowedTransitions.ContainsKey(trigger))
        {
            throw new ArgumentException(
                "Permit() (and PermitIf()) require that the destination state is not equal to the source " +
                "state. To accept a trigger without changing state, use either Ignore() or PermitReentry().");
        }

        var triggerConfiguration =
            new TransitionConfiguration<TState, TTrigger>(trigger, destinationState, triggerType,
                internalTransitionAction);
        AllowedTransitions.Add(trigger, triggerConfiguration);

        return this;
    }

    public StateConfiguration<TState, TTrigger, TContext> Permit(TTrigger trigger, TState destinationState)
    {
        return AddAllowedTransition(trigger, destinationState, TriggerType.TransitionWithEntryAndExitActions);
    }

    public StateConfiguration<TState, TTrigger, TContext> PermitReentry(TTrigger trigger)
    {
        return AddAllowedTransition(trigger, _state, TriggerType.TransitionWithEntryAndExitActions);
    }

    public StateConfiguration<TState, TTrigger, TContext> Ignore(TTrigger trigger)
    {
        return AddAllowedTransition(trigger, _state, TriggerType.Ignored);
    }

    public StateConfiguration<TState, TTrigger, TContext> InternalTransition(TTrigger trigger, Action<TTrigger> action)
    {
        return AddAllowedTransition(trigger, _state, TriggerType.InternalTransition, action);
    }

    public StateConfiguration<TState, TTrigger, TContext> OnEntry(Action<TTrigger, TContext> action)
    {
        OnEntryAction = action;

        return this;
    }

    public StateConfiguration<TState, TTrigger, TContext> OnExit(Action<TTrigger, TContext> action)
    {
        OnExitAction = action;

        return this;
    }
}