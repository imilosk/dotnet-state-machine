namespace DotnetStateMachine.Tests;

public enum AdvertState
{
    StateWithoutConfiguration = 1,
    None = 2,
    Draft = 3,
    Pending = 4,
    Active = 5,
    Denied = 6,
    Archived = 7
}

public enum AdvertTrigger
{
    Create = 1,
    Publish = 2,
    Edit = 3,
    Approve = 4,
    Deny = 5,
    Archive = 6,
    SetDelivering = 7,
    SetNotDelivering = 8
}

public record Advert
{
    public AdvertState State { get; set; }
}

public record AdvertStateMachineContext
{
    public Advert Advert { get; init; } = null!;
    public Action<Advert, AdvertState> Mutator { get; init; } = null!;
    public Action SendNotification { get; init; } = null!;
    public Action<AdvertState, AdvertState, Advert> LogTransition { get; init; } = null!;
}

internal static class StateMachineHelper
{
    public static void ConfigureStateMachine(
        StateMachine<AdvertState, AdvertTrigger, AdvertStateMachineContext> stateMachine)
    {
        stateMachine.Mutator = (newState, context) => { context.Mutator(context.Advert, newState); };
        stateMachine.OnTransitionCompleted = (oldState, newState, context) =>
        {
            context.LogTransition(oldState, newState, context.Advert);
        };

        stateMachine.Configure(AdvertState.None)
            .Permit(AdvertTrigger.Create, AdvertState.Draft);

        stateMachine.Configure(AdvertState.Draft)
            .PermitReentry(AdvertTrigger.Edit)
            .Permit(AdvertTrigger.Publish, AdvertState.Pending);

        stateMachine.Configure(AdvertState.Pending)
            .Permit(AdvertTrigger.Approve, AdvertState.Active)
            .Permit(AdvertTrigger.Deny, AdvertState.Denied);

        stateMachine.Configure(AdvertState.Active)
            .PermitReentry(AdvertTrigger.Edit)
            .Permit(AdvertTrigger.Archive, AdvertState.Archived)
            .Ignore(AdvertTrigger.Publish)
            .OnEntry((_, context) => context.SendNotification())
            .OnExit((_, context) => context.SendNotification());

        stateMachine.Configure(AdvertState.Archived)
            .OnEntry((_, context) => context.SendNotification());
    }
}

public class AdvertStateMachine : StateMachine<AdvertState, AdvertTrigger, AdvertStateMachineContext>
{
    public AdvertStateMachine()
    {
        StateMachineHelper.ConfigureStateMachine(this);
    }
}

public class AdvertServiceSecondWayDefinition
{
    private static readonly StateMachine<AdvertState, AdvertTrigger, AdvertStateMachineContext> StateMachine = new();

    static AdvertServiceSecondWayDefinition()
    {
        StateMachineHelper.ConfigureStateMachine(StateMachine);
    }
}

public class AdvertService
{
    public static readonly AdvertStateMachine StateMachine = new();
    public readonly AdvertStateMachineContext Context;

    public AdvertService()
    {
        var advert = new Advert
        {
            State = AdvertState.None
        };

        Context = new AdvertStateMachineContext
        {
            Advert = advert,
            Mutator = UpdateData,
            SendNotification = SendNotification,
            LogTransition = LogTransition
        };
    }

    protected virtual void UpdateData(Advert advert, AdvertState newState)
    {
        advert.State = newState;
    }

    public virtual void SendNotification()
    {
        Console.WriteLine("Entry notification");
    }

    public virtual void LogTransition(AdvertState sourceState, AdvertState destinationState, Advert advert)
    {
        Console.WriteLine("Transition logged");
    }
}