namespace DotnetStateMachine;

public class StateConfiguration<TState, TTrigger> where TState : notnull where TTrigger : notnull
{
    private readonly TState _state;
    public Dictionary<TTrigger, TriggerConfiguration<TState, TTrigger>> AllowedTransitions { get; } = new();

    public StateConfiguration(TState state)
    {
        _state = state;
    }

    private StateConfiguration<TState, TTrigger> AddAllowedTransition(TTrigger trigger, TState state, bool isIgnored)
    {
        if (AllowedTransitions.ContainsKey(trigger))
        {
            throw new Exception($"Trigger '{trigger}' already configured for state '{_state}'.");
        }
        
        var triggerConfiguration = new TriggerConfiguration<TState, TTrigger>(trigger, state, isIgnored);
        AllowedTransitions.Add(trigger, triggerConfiguration);

        return this;
    }
    
    public StateConfiguration<TState, TTrigger> Permit(TTrigger trigger, TState destinationState)
    {
        return AddAllowedTransition(trigger, destinationState, false);
    }
    
    public StateConfiguration<TState, TTrigger> PermitReentry(TTrigger trigger)
    {
        return AddAllowedTransition(trigger, _state, false);
    }

    public StateConfiguration<TState, TTrigger> Ignore(TTrigger trigger)
    {
        return AddAllowedTransition(trigger, _state, true);
    }
}