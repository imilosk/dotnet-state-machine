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
            throw new Exception($"Trigger '{trigger}' already configured for state '{_state}'.");
        }
        
        AllowedTransitions.Add(trigger, _state);

        return this;
    }
}