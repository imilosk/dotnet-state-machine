namespace DotnetStateMachine;

public class StateMachine<TState, TTrigger> where TState : notnull where TTrigger : notnull
{
    private readonly Dictionary<TState, StateConfiguration<TState, TTrigger>> _stateConfiguration = new();

    public StateConfiguration<TState, TTrigger> Configure(TState state)
    {
        _stateConfiguration.Add(state, new StateConfiguration<TState, TTrigger>(state));

        return _stateConfiguration[state];
    }

    public TState Peek(TState state, TTrigger trigger)
    {
        if (!_stateConfiguration.ContainsKey(state))
        {
            throw new InvalidOperationException($"State '{state}' is not configured.'");
        }

        var allowedTransitions = _stateConfiguration[state].AllowedTransitions;

        if (!allowedTransitions.ContainsKey(trigger))
        {
            throw new InvalidOperationException(
                $"No valid leaving transitions are permitted from state '{state}' for trigger '{trigger}'.");
        }

        return allowedTransitions[trigger];
    }
}