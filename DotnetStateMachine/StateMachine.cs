namespace DotnetStateMachine;

public class StateConfiguration<TState, TTrigger> where TState : notnull where TTrigger : notnull
{
    private readonly TState _state;
    public Dictionary<TTrigger, TState> AllowedTransitions { get; } = new();

    public StateConfiguration(TState state)
    {
        _state = state;
    }

    public StateConfiguration<TState, TTrigger> Permit(TTrigger trigger, TState destinationState)
    {
        AllowedTransitions.Add(trigger, destinationState);

        return this;
    }
    
    public StateConfiguration<TState, TTrigger> PermitReentry(TTrigger trigger)
    {
        if (AllowedTransitions.ContainsKey(trigger))
        {
            throw new Exception($"Trigger '{trigger}' already configured for state '{_state}'");
        }
        
        AllowedTransitions.Add(trigger, _state);

        return this;
    }
}

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
            throw new Exception($"State '{state}' is not configured'");
        }
        
        var allowedTransitions =  _stateConfiguration[state].AllowedTransitions;
        
        if (!allowedTransitions.ContainsKey(trigger))
        {
            throw new Exception($"No leaving transitions for state '{state}' for trigger '{trigger}'");
        }

        return allowedTransitions[trigger];
    }
}