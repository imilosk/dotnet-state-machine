namespace DotnetStateMachine;

public class StateMachine<TState, TTrigger> where TState : notnull where TTrigger : notnull
{
    private readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> _stateConfiguration = new();

    public StateConfiguration<TState, TTrigger> Configure(TState state)
    {
        if (!_stateConfiguration.ContainsKey(state))
        {
            _stateConfiguration.Add(state, new StateConfiguration<TState, TTrigger>(state));
        }

        return _stateConfiguration[state];
    }

    private TransitionConfiguration<TState, TTrigger> PeekAllowedTransition(TState sourceState, TTrigger trigger)
    {
        if (!_stateConfiguration.ContainsKey(sourceState))
        {
            throw new InvalidOperationException($"State '{sourceState}' is not configured.'");
        }

        var allowedTransitions = _stateConfiguration[sourceState].AllowedTransitions;

        if (!allowedTransitions.ContainsKey(trigger))
        {
            throw new InvalidOperationException(
                $"No valid leaving transitions are permitted from state '{sourceState}' for trigger '{trigger}'.");
        }

        return allowedTransitions[trigger];
    }

    public TState Peek(TState sourceState, TTrigger trigger)
    {
        return PeekAllowedTransition(sourceState, trigger).DestinationState;
    }

    public TState Fire(TState sourceState, TTrigger trigger)
    {
        return Fire(sourceState, trigger, null);
    }

    public TState Fire(TState sourceState, TTrigger trigger, Action<TState>? transitionAction)
    {
        var transition = PeekAllowedTransition(sourceState, trigger);

        switch (transition.TriggerType)
        {
            case TriggerType.Ignored:
                break;
            case TriggerType.InternalTransition:
                transition.InternalTransitionAction?.Invoke(transition.Trigger);
                break;
            case TriggerType.TransitionWithEntryAndExitActions:
                // TODO: execute exit triggers on current state, skip if trigger is ignored
                transitionAction?.Invoke(transition.DestinationState);
                // TODO: execute enter triggers on new state, skip if trigger is ignored
                break;
            default:
                throw new NotImplementedException("Unimplemented transition type.");
        }

        return transition.DestinationState;
    }
}