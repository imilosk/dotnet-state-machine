using Moq;

namespace DotnetStateMachine.Tests;

public enum AdvertState
{
    StateWithoutConfiguration = 1,
    None,
    Draft,
    Pending,
    Active,
    Denied,
    Archived,
    Deactivated
}

public enum AdvertTrigger
{
    Create = 1,
    Publish,
    Edit,
    Approve,
    Deny,
    Archive,
    SetDelivering,
    SetNotDelivering,
    Deactivate
}

public class NotificationService
{
    public static readonly StateMachine<AdvertState, AdvertTrigger, NotificationService> StateMachine = new();

    static NotificationService()
    {
        StateMachine.Configure(AdvertState.None)
            .Permit(AdvertTrigger.Create, AdvertState.Draft);

        StateMachine.Configure(AdvertState.Draft)
            .PermitReentry(AdvertTrigger.Edit)
            .Permit(AdvertTrigger.Publish, AdvertState.Pending);

        StateMachine.Configure(AdvertState.Pending)
            .Permit(AdvertTrigger.Approve, AdvertState.Active)
            .Permit(AdvertTrigger.Deny, AdvertState.Denied);

        StateMachine.Configure(AdvertState.Active)
            .PermitReentry(AdvertTrigger.Edit)
            .Permit(AdvertTrigger.Archive, AdvertState.Archived)
            .Ignore(AdvertTrigger.Publish)
            .OnEntry((_, service) => service.SendEntryNotification())
            .OnExit((_, service) => service.SendExitNotification());

        StateMachine.Configure(AdvertState.Archived)
            .OnEntry((_, service) => service.SendEntryNotification());
    }

    public virtual void SendEntryNotification()
    {
        Console.WriteLine("Entry notification");
    }

    public virtual void SendExitNotification()
    {
        Console.WriteLine("Exit notification");
    }
}

public class StateMachineTests
{
    [Fact]
    public void Peek_WithValidInput_ReturnsCorrectDestinationState()
    {
        var stateMachine = NotificationService.StateMachine;

        // test transitions
        var destinationState = stateMachine.Peek(AdvertState.None, AdvertTrigger.Create);
        Assert.Equal(AdvertState.Draft, destinationState);

        destinationState = stateMachine.Peek(AdvertState.Draft, AdvertTrigger.Publish);
        Assert.Equal(AdvertState.Pending, destinationState);

        destinationState = stateMachine.Peek(AdvertState.Pending, AdvertTrigger.Approve);
        Assert.Equal(AdvertState.Active, destinationState);

        destinationState = stateMachine.Peek(AdvertState.Pending, AdvertTrigger.Deny);
        Assert.Equal(AdvertState.Denied, destinationState);

        destinationState = stateMachine.Peek(AdvertState.Active, AdvertTrigger.Archive);
        Assert.Equal(AdvertState.Archived, destinationState);

        // tests reentry
        destinationState = stateMachine.Peek(AdvertState.Draft, AdvertTrigger.Edit);
        Assert.Equal(AdvertState.Draft, destinationState);

        destinationState = stateMachine.Peek(AdvertState.Active, AdvertTrigger.Edit);
        Assert.Equal(AdvertState.Active, destinationState);

        // tests ignored triggers
        destinationState = stateMachine.Peek(AdvertState.Active, AdvertTrigger.Publish);
        Assert.Equal(AdvertState.Active, destinationState);
    }

    [Fact]
    public void Peek_WithInvalidInput_ThrowsException()
    {
        var stateMachine = NotificationService.StateMachine;

        var expectedException = typeof(InvalidOperationException);

        // unconfigured state
        Assert.Throws(expectedException,
            () => stateMachine.Peek(AdvertState.StateWithoutConfiguration, AdvertTrigger.Edit));

        Assert.Throws(expectedException, () => stateMachine.Peek(AdvertState.None, AdvertTrigger.Edit));
        Assert.Throws(expectedException, () => stateMachine.Peek(AdvertState.Active, AdvertTrigger.Approve));
        Assert.Throws(expectedException, () => stateMachine.Peek(AdvertState.Active, AdvertTrigger.Deny));
        Assert.Throws(expectedException, () => stateMachine.Peek(AdvertState.Archived, AdvertTrigger.Edit));
    }

    [Fact]
    public void Fire_WithValidInput_ReturnsCorrectDestinationState()
    {
        var notificationService = new NotificationService();
        var stateMachine = NotificationService.StateMachine;

        // test transitions
        var destinationState = stateMachine.Fire(AdvertState.None, AdvertTrigger.Create, notificationService);
        Assert.Equal(AdvertState.Draft, destinationState);

        destinationState = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Publish, notificationService);
        Assert.Equal(AdvertState.Pending, destinationState);

        destinationState = stateMachine.Fire(AdvertState.Pending, AdvertTrigger.Approve, notificationService);
        Assert.Equal(AdvertState.Active, destinationState);

        destinationState = stateMachine.Fire(AdvertState.Pending, AdvertTrigger.Deny, notificationService);
        Assert.Equal(AdvertState.Denied, destinationState);

        destinationState = stateMachine.Fire(AdvertState.Active, AdvertTrigger.Archive, notificationService);
        Assert.Equal(AdvertState.Archived, destinationState);

        // tests reentry
        destinationState = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Edit, notificationService);
        Assert.Equal(AdvertState.Draft, destinationState);

        destinationState = stateMachine.Fire(AdvertState.Active, AdvertTrigger.Edit, notificationService);
        Assert.Equal(AdvertState.Active, destinationState);

        // tests ignored triggers
        destinationState = stateMachine.Fire(AdvertState.Active, AdvertTrigger.Publish, notificationService);
        Assert.Equal(AdvertState.Active, destinationState);
    }

    [Fact]
    public void Fire_WithDelegateWithoutParameters_ExecutesDelegate()
    {
        var service = new NotificationService();
        var stateMachine = NotificationService.StateMachine;

        var actualValue = -1;

        _ = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Publish, service,
            _ => actualValue = 42);

        Assert.Equal(42, actualValue);
    }

    [Fact]
    public void Fire_WithDelegateWithParameters_ExecutesDelegate()
    {
        var service = new NotificationService();
        var stateMachine = NotificationService.StateMachine;

        AdvertState? expectedParameter = null;

        var destinationState = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Publish, service,
            newState => { expectedParameter = newState; });

        Assert.Equal(AdvertState.Pending, expectedParameter);
        Assert.Equal(expectedParameter, destinationState);
    }

    [Fact]
    public void Fire_WithInternalTrigger_DoesNotChangeStateAndExecutesDelegate()
    {
        AdvertTrigger? deliveringTrigger = null;
        AdvertTrigger? notDeliveringTrigger = null;

        var stateMachine = NotificationService.StateMachine;
        var service = new NotificationService();

        stateMachine.Configure(AdvertState.Active)
            .InternalTransition(AdvertTrigger.SetDelivering, t => deliveringTrigger = t)
            .InternalTransition(AdvertTrigger.SetNotDelivering, t => notDeliveringTrigger = t);

        var destinationState = stateMachine.Fire(AdvertState.Active, AdvertTrigger.SetDelivering, service);
        Assert.Equal(AdvertState.Active, destinationState);
        Assert.Equal(AdvertTrigger.SetDelivering, deliveringTrigger);

        destinationState = stateMachine.Fire(AdvertState.Active, AdvertTrigger.SetNotDelivering, service);
        Assert.Equal(AdvertState.Active, destinationState);
        Assert.Equal(AdvertTrigger.SetNotDelivering, notDeliveringTrigger);
    }

    [Fact]
    public void ConfigureExistingState_WithExistingTriggerConfiguration_ThrowsException()
    {
        var stateMachine = NotificationService.StateMachine;

        var expectedException = typeof(ArgumentException);

        Assert.Throws(expectedException, () =>
            stateMachine.Configure(AdvertState.None)
                .Permit(AdvertTrigger.Create, AdvertState.Draft)
        );
    }

    [Fact]
    public void ConfigureExistingState_WithNewTrigger_DoesNotThrowException()
    {
        var service = new NotificationService();
        var stateMachine = NotificationService.StateMachine;

        stateMachine.Configure(AdvertState.Pending)
            .Permit(AdvertTrigger.Deactivate, AdvertState.Deactivated);

        var exception = Record.Exception(() => stateMachine.Configure(AdvertState.None));
        Assert.Null(exception);

        // The trigger configuration should not be overriden meaning this should not throw an
        // unconfigured trigger exception 
        exception = Record.Exception(() => stateMachine.Fire(AdvertState.None, AdvertTrigger.Create, service));
        Assert.Null(exception);
    }

    [Fact]
    public void OnEntryAndOnExitActions_ExecutesDelegates()
    {
        var stateMachine = NotificationService.StateMachine;

        var onExitCalled = false;
        var onEntryCalled = false;

        var mock = new Mock<NotificationService>();
        mock.Setup(notificationService => notificationService.SendExitNotification())
            .Callback(() => { onExitCalled = true; });
        mock.Setup(notificationService => notificationService.SendEntryNotification())
            .Callback(() => { onEntryCalled = true; });

        var service = mock.Object;

        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Archive, service);

        Assert.True(onExitCalled);
        Assert.True(onEntryCalled);
    }

    [Fact]
    public void OnEntryAndOnExitActions_WithReentryState_ExecutesDelegates()
    {
        var stateMachine = NotificationService.StateMachine;

        var onExitCalled = false;
        var onEntryCalled = false;

        var mock = new Mock<NotificationService>();
        mock.Setup(notificationService => notificationService.SendExitNotification())
            .Callback(() => { onExitCalled = true; });
        mock.Setup(notificationService => notificationService.SendEntryNotification())
            .Callback(() => { onEntryCalled = true; });

        var service = mock.Object;

        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Edit, service);

        Assert.True(onExitCalled);
        Assert.True(onEntryCalled);
    }

    [Fact]
    public void CanFire_ReturnsCorrectResult()
    {
        var stateMachine = NotificationService.StateMachine;

        var canFire = stateMachine.CanFire(AdvertState.Active, AdvertTrigger.Archive);
        Assert.True(canFire);

        canFire = stateMachine.CanFire(AdvertState.Active, AdvertTrigger.Create);
        Assert.False(canFire);
    }
}