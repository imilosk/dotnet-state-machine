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
    Archived
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
    SetNotDelivering
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
    public static IEnumerable<object[]> ValidInputData()
    {
        yield return new object[] { AdvertState.None, AdvertTrigger.Create, AdvertState.Draft };
        yield return new object[] { AdvertState.Draft, AdvertTrigger.Publish, AdvertState.Pending };
        yield return new object[] { AdvertState.Pending, AdvertTrigger.Approve, AdvertState.Active };
        yield return new object[] { AdvertState.Pending, AdvertTrigger.Deny, AdvertState.Denied };
        yield return new object[] { AdvertState.Active, AdvertTrigger.Archive, AdvertState.Archived };
        yield return new object[] { AdvertState.Draft, AdvertTrigger.Edit, AdvertState.Draft }; // reentry
        yield return new object[] { AdvertState.Active, AdvertTrigger.Edit, AdvertState.Active }; // reentry 
        yield return new object[] { AdvertState.Active, AdvertTrigger.Publish, AdvertState.Active }; // ignored
    }

    public static IEnumerable<object[]> ProhibitedInputData()
    {
        yield return new object[] { AdvertState.StateWithoutConfiguration, AdvertTrigger.Edit };
        yield return new object[] { AdvertState.None, AdvertTrigger.Edit };
        yield return new object[] { AdvertState.Active, AdvertTrigger.Approve };
        yield return new object[] { AdvertState.Active, AdvertTrigger.Deny };
        yield return new object[] { AdvertState.Archived, AdvertTrigger.Edit };
    }

    [Theory]
    [MemberData(nameof(ValidInputData))]
    public void Peek_WithValidInput_ReturnsCorrectDestinationState(AdvertState sourceState, AdvertTrigger trigger,
        AdvertState expectedState)
    {
        var stateMachine = NotificationService.StateMachine;

        var actualState = stateMachine.Peek(sourceState, trigger);

        Assert.Equal(expectedState, actualState);
    }

    [Theory]
    [MemberData(nameof(ProhibitedInputData))]
    public void Peek_WithProhibitedInput_ThrowsException(AdvertState sourceState, AdvertTrigger trigger)
    {
        var stateMachine = NotificationService.StateMachine;

        var expectedException = typeof(InvalidOperationException);

        Assert.Throws(expectedException, () => stateMachine.Peek(sourceState, trigger));
    }

    [Theory]
    [MemberData(nameof(ValidInputData))]
    public void Fire_WithValidInput_ReturnsCorrectDestinationState(AdvertState sourceState, AdvertTrigger trigger,
        AdvertState expectedState)
    {
        var notificationService = new NotificationService();
        var stateMachine = NotificationService.StateMachine;

        var actualState = stateMachine.Fire(sourceState, trigger, notificationService);

        Assert.Equal(expectedState, actualState);
    }

    [Theory]
    [MemberData(nameof(ProhibitedInputData))]
    public void Fire_WithProhibitedInput_ThrowsException(AdvertState sourceState, AdvertTrigger trigger)
    {
        var notificationService = new NotificationService();
        var stateMachine = NotificationService.StateMachine;

        var expectedException = typeof(InvalidOperationException);

        Assert.Throws(expectedException, () => stateMachine.Fire(sourceState, trigger, notificationService));
    }

    [Fact]
    public void Fire_WithDelegateWithoutParameters_ExecutesDelegate()
    {
        var service = new NotificationService();
        var stateMachine = NotificationService.StateMachine;

        var actualValue = -1;

        _ = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Publish, service, _ => actualValue = 42);

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

    [Theory]
    [InlineData(AdvertTrigger.SetDelivering)]
    [InlineData(AdvertTrigger.SetNotDelivering)]
    public void Fire_WithInternalTrigger_DoesNotChangeStateAndExecutesDelegate(AdvertTrigger expectedTrigger)
    {
        AdvertTrigger? actualTrigger = null;

        var stateMachine = NotificationService.StateMachine;
        var service = new NotificationService();

        stateMachine.Configure(AdvertState.Active)
            .InternalTransition(expectedTrigger, t => actualTrigger = t);

        var destinationState = stateMachine.Fire(AdvertState.Active, expectedTrigger, service);

        Assert.Equal(AdvertState.Active, destinationState);
        Assert.Equal(expectedTrigger, actualTrigger);
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
    public void Configure_ExistingState_DoesNotThrowException()
    {
        var stateMachine = NotificationService.StateMachine;

        var exception = Record.Exception(() => stateMachine.Configure(AdvertState.None));

        Assert.Null(exception);
    }

    [Fact]
    public void Configure_ExistingStateWithNewTrigger_DoesNotThrowException()
    {
        var service = new NotificationService();
        var stateMachine = NotificationService.StateMachine;

        // The trigger configuration should not be overriden meaning this should not throw an
        // unconfigured trigger exception 
        var exception = Record.Exception(() => stateMachine.Fire(AdvertState.None, AdvertTrigger.Create, service));

        Assert.Null(exception);
    }

    [Fact]
    public void OnEntryAction_ExecutesDelegate()
    {
        var stateMachine = NotificationService.StateMachine;

        var onEntryCalled = false;

        var mock = new Mock<NotificationService>();
        mock.Setup(notificationService => notificationService.SendEntryNotification())
            .Callback(() => { onEntryCalled = true; });

        var service = mock.Object;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Archive, service);

        Assert.True(onEntryCalled);
    }

    [Fact]
    public void OnExitAction_ExecutesDelegate()
    {
        var stateMachine = NotificationService.StateMachine;

        var onExitCalled = false;

        var mock = new Mock<NotificationService>();
        mock.Setup(notificationService => notificationService.SendExitNotification())
            .Callback(() => { onExitCalled = true; });

        var service = mock.Object;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Archive, service);

        Assert.True(onExitCalled);
    }

    [Fact]
    public void OnEntryAction_WithReentryState_ExecutesOnEntryDelegate()
    {
        var stateMachine = NotificationService.StateMachine;

        var onEntryCalled = false;

        var mock = new Mock<NotificationService>();
        mock.Setup(notificationService => notificationService.SendEntryNotification())
            .Callback(() => { onEntryCalled = true; });

        var service = mock.Object;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Edit, service);

        Assert.True(onEntryCalled);
    }

    [Fact]
    public void OnExitAction_WithReentryState_ExecutesOnExitDelegate()
    {
        var stateMachine = NotificationService.StateMachine;

        var onExitCalled = false;

        var mock = new Mock<NotificationService>();
        mock.Setup(notificationService => notificationService.SendExitNotification())
            .Callback(() => { onExitCalled = true; });

        var service = mock.Object;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Edit, service);

        Assert.True(onExitCalled);
    }

    [Theory]
    [InlineData(AdvertState.Active, AdvertTrigger.Archive)]
    [InlineData(AdvertState.Active, AdvertTrigger.Edit)]
    public void CanFire_ReturnsTrue(AdvertState sourceState, AdvertTrigger trigger)
    {
        var stateMachine = NotificationService.StateMachine;

        var canFire = stateMachine.CanFire(sourceState, trigger);

        Assert.True(canFire);
    }

    [Theory]
    [InlineData(AdvertState.Active, AdvertTrigger.Create)]
    [InlineData(AdvertState.None, AdvertTrigger.Edit)]
    public void CanFire_ReturnsFalse(AdvertState sourceState, AdvertTrigger trigger)
    {
        var stateMachine = NotificationService.StateMachine;

        var canFire = stateMachine.CanFire(sourceState, trigger);

        Assert.False(canFire);
    }
}