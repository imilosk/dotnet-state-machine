using Moq;

namespace DotnetStateMachine.Tests;

public class StateMachineTests
{
    public static IEnumerable<object[]> ValidInputData()
    {
        yield return [AdvertState.None, AdvertTrigger.Create, AdvertState.Draft];
        yield return [AdvertState.Draft, AdvertTrigger.Publish, AdvertState.Pending];
        yield return [AdvertState.Pending, AdvertTrigger.Approve, AdvertState.Active];
        yield return [AdvertState.Pending, AdvertTrigger.Deny, AdvertState.Denied];
        yield return [AdvertState.Active, AdvertTrigger.Archive, AdvertState.Archived];
        yield return [AdvertState.Draft, AdvertTrigger.Edit, AdvertState.Draft]; // reentry
        yield return [AdvertState.Active, AdvertTrigger.Edit, AdvertState.Active]; // reentry 
        yield return [AdvertState.Active, AdvertTrigger.Publish, AdvertState.Active]; // ignored
    }

    public static IEnumerable<object[]> ProhibitedInputData()
    {
        yield return [AdvertState.StateWithoutConfiguration, AdvertTrigger.Edit];
        yield return [AdvertState.None, AdvertTrigger.Edit];
        yield return [AdvertState.Active, AdvertTrigger.Approve];
        yield return [AdvertState.Active, AdvertTrigger.Deny];
        yield return [AdvertState.Archived, AdvertTrigger.Edit];
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
        var context = new AdvertService().Context;
        var stateMachine = AdvertService.StateMachine;

        var actualState = stateMachine.Fire(sourceState, trigger, context);

        Assert.Equal(expectedState, actualState);
    }

    [Theory]
    [MemberData(nameof(ProhibitedInputData))]
    public void Fire_WithProhibitedInput_ThrowsException(AdvertState sourceState, AdvertTrigger trigger)
    {
        var context = new AdvertService().Context;
        var stateMachine = AdvertService.StateMachine;

        Assert.Throws<InvalidOperationException>(() => stateMachine.Fire(sourceState, trigger, context));
    }

    [Fact]
    public void Fire_MutatorExecutes()
    {
        var stateMachine = AdvertService.StateMachine;
        var context = new AdvertService().Context;

        var newState = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Publish, context);

        Assert.Equal(newState, context.Advert.State);
    }

    [Theory]
    [InlineData(AdvertTrigger.SetDelivering)]
    [InlineData(AdvertTrigger.SetNotDelivering)]
    public void Fire_WithInternalTrigger_DoesNotChangeStateAndExecutesDelegate(AdvertTrigger expectedTrigger)
    {
        AdvertTrigger? actualTrigger = null;

        var stateMachine = AdvertService.StateMachine;
        var context = new AdvertService().Context;

        stateMachine.Configure(AdvertState.Active)
            .InternalTransition(expectedTrigger, (t, _) => actualTrigger = t);

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
        var context = new AdvertService().Context;
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
        mock.Setup(context => context.SendNotification())
            .Callback(() => { onEntryCalled = true; });

        var context = mock.Object.Context;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Archive, context);

        Assert.True(onEntryCalled);
    }

    [Fact]
    public void OnExitAction_ExecutesDelegate()
    {
        var stateMachine = AdvertService.StateMachine;

        var onExitCalled = false;

        var mock = new Mock<AdvertService>();
        mock.Setup(context => context.SendNotification())
            .Callback(() => { onExitCalled = true; });

        var context = mock.Object.Context;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Archive, context);

        Assert.True(onExitCalled);
    }

    [Fact]
    public void OnEntryAction_WithReentryState_ExecutesOnEntryDelegate()
    {
        var stateMachine = AdvertService.StateMachine;

        var onEntryCalled = false;

        var mock = new Mock<AdvertService>();
        mock.Setup(context => context.SendNotification())
            .Callback(() => { onEntryCalled = true; });

        var context = mock.Object.Context;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Edit, context);

        Assert.True(onEntryCalled);
    }

    [Fact]
    public void OnExitAction_WithReentryState_ExecutesOnExitDelegate()
    {
        var stateMachine = AdvertService.StateMachine;

        var onExitCalled = false;

        var mock = new Mock<AdvertService>();
        mock.Setup(context => context.SendNotification())
            .Callback(() => { onExitCalled = true; });

        var context = mock.Object.Context;
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

    [Fact]
    public void CanDefine_DoesNotThrowException()
    {
        // make sure no exception is thrown
        _ = new AdvertServiceSecondWayDefinition();
    }

    [Fact]
    public void Fire_OnTransitionedCompletedExecutes()
    {
        var stateMachine = AdvertService.StateMachine;

        var onTransitionCompletedCalled = false;

        var mock = new Mock<AdvertService>();
        mock.Setup(context => context
                .LogTransition(AdvertState.Active, AdvertState.Archived, mock.Object.Context.Advert))
            .Callback(() => { onTransitionCompletedCalled = true; });

        var context = mock.Object.Context;
        stateMachine.Fire(AdvertState.Active, AdvertTrigger.Archive, context);

        Assert.True(onTransitionCompletedCalled);
    }
}