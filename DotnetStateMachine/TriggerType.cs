namespace DotnetStateMachine;

public enum TriggerType
{
    TransitionWithEntryAndExitActions = 1,
    InternalTransition = 2,
    Ignored = 3
}