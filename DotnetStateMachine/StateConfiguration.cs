namespace DotnetStateMachine;

public class StateConfiguration<TState, TTrigger> where TState : notnull where TTrigger : notnull
{
    private readonly TState _state;
    public Dictionary<TTrigger, TransitionConfiguration<TState, TTrigger>> AllowedTransitions { get; } = new();
    internal Action<TTrigger>? OnEntryAction { get; set; }
    internal Action<TTrigger>? OnExitAction { get; set; }

    public StateConfiguration(TState state)
    {
        _state = state;
    }

    private StateConfiguration<TState, TTrigger> AddAllowedTransition(TTrigger trigger, TState destinationState,
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

    public StateConfiguration<TState, TTrigger> Permit(TTrigger trigger, TState destinationState)
    {
        return AddAllowedTransition(trigger, destinationState, TriggerType.TransitionWithEntryAndExitActions);
    }

    public StateConfiguration<TState, TTrigger> PermitReentry(TTrigger trigger)
    {
        return AddAllowedTransition(trigger, _state, TriggerType.TransitionWithEntryAndExitActions);
    }

    public StateConfiguration<TState, TTrigger> Ignore(TTrigger trigger)
    {
        return AddAllowedTransition(trigger, _state, TriggerType.Ignored);
    }

    public StateConfiguration<TState, TTrigger> InternalTransition(TTrigger trigger, Action<TTrigger> action)
    {
        return AddAllowedTransition(trigger, _state, TriggerType.InternalTransition, action);
    }

    public StateConfiguration<TState, TTrigger> OnEntry(Action<TTrigger> action)
    {
        OnEntryAction = action;

        return this;
    }

    public StateConfiguration<TState, TTrigger> OnExit(Action<TTrigger> action)
    {
        OnExitAction = action;

        return this;
    }
}