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
        var canTransition = TryPeekAllowedTransition(sourceState, trigger, out var allowedTransitions);

        if (!canTransition)
        {
            throw new InvalidOperationException(
                $"No valid leaving transitions are permitted from state '{sourceState}' for trigger " +
                $"'{trigger}'. Consider ignoring the trigger.");
        }

        return allowedTransitions;
    }

    private bool TryPeekAllowedTransition(TState sourceState, TTrigger trigger,
        out TransitionConfiguration<TState, TTrigger> allowedTransitions)
    {
        allowedTransitions = default;
        return _stateConfiguration.ContainsKey(sourceState) &&
               _stateConfiguration[sourceState].AllowedTransitions.TryGetValue(trigger, out allowedTransitions);
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
                var destinationState = transition.DestinationState;

                _stateConfiguration[sourceState].OnExitAction?.Invoke(transition.Trigger);

                transitionAction?.Invoke(destinationState);

                if (_stateConfiguration.TryGetValue(destinationState, out var stateConfiguration))
                {
                    stateConfiguration.OnEntryAction?.Invoke(transition.Trigger);
                }

                break;
            default:
                throw new NotImplementedException("Unimplemented transition type.");
        }

        return transition.DestinationState;
    }

    public bool CanFire(TState sourceState, TTrigger trigger)
    {
        return TryPeekAllowedTransition(sourceState, trigger, out _);
    }
}