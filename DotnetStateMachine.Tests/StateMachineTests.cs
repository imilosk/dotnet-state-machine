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

public class AdvertStateMachine : StateMachine<AdvertState, AdvertTrigger, AdvertService>
{
    public AdvertStateMachine()
    {
        Configure(AdvertState.None)
            .Permit(AdvertTrigger.Create, AdvertState.Draft);

        Configure(AdvertState.Draft)
            .PermitReentry(AdvertTrigger.Edit)
            .Permit(AdvertTrigger.Publish, AdvertState.Pending);

        Configure(AdvertState.Pending)
            .Permit(AdvertTrigger.Approve, AdvertState.Active)
            .Permit(AdvertTrigger.Deny, AdvertState.Denied);

        Configure(AdvertState.Active)
            .PermitReentry(AdvertTrigger.Edit)
            .Permit(AdvertTrigger.Archive, AdvertState.Archived)
            .Ignore(AdvertTrigger.Publish)
            .OnEntry((_, context) => context.SendEntryNotification())
            .OnExit((_, context) => context.SendExitNotification());

        Configure(AdvertState.Archived)
            .OnEntry((_, context) => context.SendEntryNotification());
    }
}

public class AdvertService
{
    public static readonly AdvertStateMachine StateMachine = new();

    public void SendEntryNotification()
    {
        Console.WriteLine("Entry notification");
    }

    public void SendExitNotification()
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
        var stateMachine = AdvertService.StateMachine;

        var actualState = stateMachine.Peek(sourceState, trigger);

        Assert.Equal(expectedState, actualState);
    }

    [Theory]
    [MemberData(nameof(ProhibitedInputData))]
    public void Peek_WithProhibitedInput_ThrowsException(AdvertState sourceState, AdvertTrigger trigger)
    {
        var stateMachine = AdvertService.StateMachine;

        Assert.Throws<InvalidOperationException>(() => stateMachine.Peek(sourceState, trigger));
    }

    [Theory]
    [MemberData(nameof(ValidInputData))]
    public void Fire_WithValidInput_ReturnsCorrectDestinationState(AdvertState sourceState, AdvertTrigger trigger,
        AdvertState expectedState)
    {
        var context = new AdvertService();
        var stateMachine = AdvertService.StateMachine;

        var actualState = stateMachine.Fire(sourceState, trigger, context);

        Assert.Equal(expectedState, actualState);
    }

    [Theory]
    [MemberData(nameof(ProhibitedInputData))]
    public void Fire_WithProhibitedInput_ThrowsException(AdvertState sourceState, AdvertTrigger trigger)
    {
        var context = new AdvertService();
        var stateMachine = AdvertService.StateMachine;

        Assert.Throws<InvalidOperationException>(() => stateMachine.Fire(sourceState, trigger, context));
    }

    [Fact]
    public void Fire_WithDelegateWithoutParameters_ExecutesDelegate()
    {
        var context = new AdvertService();
        var stateMachine = AdvertService.StateMachine;

        var actualValue = -1;

        _ = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Publish, context, _ => actualValue = 42);

        Assert.Equal(42, actualValue);
    }

    [Fact]
    public void Fire_WithDelegateWithParameters_ExecutesDelegate()
    {
        var context = new AdvertService();
        var stateMachine = AdvertService.StateMachine;

        AdvertState? expectedParameter = null;

        var destinationState = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Publish, context,
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

        var stateMachine = AdvertService.StateMachine;
        var context = new AdvertService();

        stateMachine.Configure(AdvertState.Active)
            .InternalTransition(expectedTrigger, t => actualTrigger = t);

        var destinationState = stateMachine.Fire(AdvertState.Active, expectedTrigger, context);

        Assert.Equal(AdvertState.Active, destinationState);
        Assert.Equal(expectedTrigger, actualTrigger);
    }

    [Fact]
    public void ConfigureExistingState_WithExistingTriggerConfiguration_ThrowsException()
    {
        var stateMachine = AdvertService.StateMachine;

        Assert.Throws<ArgumentException>(() =>
            stateMachine.Configure(AdvertState.None)
                .Permit(AdvertTrigger.Create, AdvertState.Draft)
        );
    }

    [Fact]
    public void Configure_ExistingState_DoesNotThrowException()
    {
        var stateMachine = AdvertService.StateMachine;

        var exception = Record.Exception(() => stateMachine.Configure(AdvertState.None));

        Assert.Null(exception);
    }

    [Fact]
    public void Configure_ExistingStateWithNewTrigger_DoesNotThrowException()
    {
        var context = new AdvertService();
        var stateMachine = AdvertService.StateMachine;

        // The trigger configuration should not be overriden meaning this should not throw an
        // unconfigured trigger exception 
        var exception = Record.Exception(() => stateMachine.Fire(AdvertState.None, AdvertTrigger.Create, context));

        Assert.Null(exception);
    }

    [Fact]
    public void OnEntryAction_ExecutesDelegate()
    {
        var stateMachine = AdvertService.StateMachine;

        var onEntryCalled = false;

        var mock = new Mock<AdvertService>();
        mock.Setup(context => context.SendEntryNotification())
            .Callback(() => { onEntryCalled = true; });

        var context = mock.Object;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Archive, context);

        Assert.True(onEntryCalled);
    }

    [Fact]
    public void OnExitAction_ExecutesDelegate()
    {
        var stateMachine = AdvertService.StateMachine;

        var onExitCalled = false;

        var mock = new Mock<AdvertService>();
        mock.Setup(context => context.SendExitNotification())
            .Callback(() => { onExitCalled = true; });

        var context = mock.Object;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Archive, context);

        Assert.True(onExitCalled);
    }

    [Fact]
    public void OnEntryAction_WithReentryState_ExecutesOnEntryDelegate()
    {
        var stateMachine = AdvertService.StateMachine;

        var onEntryCalled = false;

        var mock = new Mock<AdvertService>();
        mock.Setup(context => context.SendEntryNotification())
            .Callback(() => { onEntryCalled = true; });

        var context = mock.Object;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Edit, context);

        Assert.True(onEntryCalled);
    }

    [Fact]
    public void OnExitAction_WithReentryState_ExecutesOnExitDelegate()
    {
        var stateMachine = AdvertService.StateMachine;

        var onExitCalled = false;

        var mock = new Mock<AdvertService>();
        mock.Setup(context => context.SendExitNotification())
            .Callback(() => { onExitCalled = true; });

        var context = mock.Object;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Edit, context);

        Assert.True(onExitCalled);
    }

    [Theory]
    [InlineData(AdvertState.Active, AdvertTrigger.Archive)]
    [InlineData(AdvertState.Active, AdvertTrigger.Edit)]
    public void CanFire_ReturnsTrue(AdvertState sourceState, AdvertTrigger trigger)
    {
        var stateMachine = AdvertService.StateMachine;

        var canFire = stateMachine.CanFire(sourceState, trigger);

        Assert.True(canFire);
    }

    [Theory]
    [InlineData(AdvertState.Active, AdvertTrigger.Create)]
    [InlineData(AdvertState.None, AdvertTrigger.Edit)]
    public void CanFire_ReturnsFalse(AdvertState sourceState, AdvertTrigger trigger)
    {
        var stateMachine = AdvertService.StateMachine;

        var canFire = stateMachine.CanFire(sourceState, trigger);

        Assert.False(canFire);
    }
}