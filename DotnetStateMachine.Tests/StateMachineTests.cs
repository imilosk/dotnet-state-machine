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

public class StateMachineTests
{
    private static AdvertTrigger _deliveringTrigger;
    private static AdvertTrigger _notDeliveringTrigger;

    private static StateMachine<AdvertState, AdvertTrigger> CreateCommonStateMachine()
    {
        var stateMachine = new StateMachine<AdvertState, AdvertTrigger>();

        stateMachine.Configure(AdvertState.None)
            .Permit(AdvertTrigger.Create, AdvertState.Draft);

        stateMachine.Configure(AdvertState.Draft)
            .PermitReentry(AdvertTrigger.Edit)
            .Permit(AdvertTrigger.Publish, AdvertState.Pending);

        stateMachine.Configure(AdvertState.Pending)
            .Permit(AdvertTrigger.Approve, AdvertState.Active)
            .Permit(AdvertTrigger.Deny, AdvertState.Denied)
            .Ignore(AdvertTrigger.Publish);

        stateMachine.Configure(AdvertState.Active)
            .PermitReentry(AdvertTrigger.Edit)
            .Permit(AdvertTrigger.Archive, AdvertState.Archived)
            .InternalTransition(AdvertTrigger.SetDelivering, t => _deliveringTrigger = t)
            .InternalTransition(AdvertTrigger.SetNotDelivering, t => _notDeliveringTrigger = t);


        return stateMachine;
    }

    [Fact]
    public void TestPeekDestinationState()
    {
        var stateMachine = CreateCommonStateMachine();

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
    }

    [Fact]
    public void TestBadPeek()
    {
        var stateMachine = CreateCommonStateMachine();

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
    public void TestFireDestinationState()
    {
        var stateMachine = CreateCommonStateMachine();

        // test transitions
        var destinationState = stateMachine.Fire(AdvertState.None, AdvertTrigger.Create);
        Assert.Equal(AdvertState.Draft, destinationState);

        destinationState = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Publish);
        Assert.Equal(AdvertState.Pending, destinationState);

        destinationState = stateMachine.Fire(AdvertState.Pending, AdvertTrigger.Approve);
        Assert.Equal(AdvertState.Active, destinationState);

        destinationState = stateMachine.Fire(AdvertState.Pending, AdvertTrigger.Deny);
        Assert.Equal(AdvertState.Denied, destinationState);

        destinationState = stateMachine.Fire(AdvertState.Active, AdvertTrigger.Archive);
        Assert.Equal(AdvertState.Archived, destinationState);

        // tests reentry
        destinationState = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Edit);
        Assert.Equal(AdvertState.Draft, destinationState);

        destinationState = stateMachine.Fire(AdvertState.Active, AdvertTrigger.Edit);
        Assert.Equal(AdvertState.Active, destinationState);
    }

    [Fact]
    public void TestFireCallbackWithoutUsingParameter()
    {
        var stateMachine = CreateCommonStateMachine();

        var actualValue = -1;

        _ = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Publish,
            _ => actualValue = 42);

        Assert.Equal(42, actualValue);
    }

    [Fact]
    public void TestFireWithParameter()
    {
        var stateMachine = CreateCommonStateMachine();

        AdvertState? expectedParameter = null;

        var destinationState = stateMachine.Fire(AdvertState.Draft, AdvertTrigger.Publish,
            newState => { expectedParameter = newState; });

        Assert.Equal(AdvertState.Pending, expectedParameter);
        Assert.Equal(expectedParameter, destinationState);
    }

    [Fact]
    public void TestIgnoredTrigger()
    {
        var stateMachine = CreateCommonStateMachine();

        AdvertState? destinationState = null;
        var exception = Record.Exception(() =>
            destinationState = stateMachine.Fire(AdvertState.Pending, AdvertTrigger.Publish));

        Assert.Null(exception);
        Assert.Equal(AdvertState.Pending, destinationState);
    }

    
    [Fact]
    public void TestInternalTrigger()
    {
        // TODO: Improve this test by not using class static properties
        var stateMachine = CreateCommonStateMachine();

        var destinationState = stateMachine.Fire(AdvertState.None, AdvertTrigger.Create);
        // Assert.Equal(AdvertTrigger.SetDelivering, _deliveringTrigger);

        Assert.Equal(AdvertState.Draft, destinationState);
        // Assert.Equal(AdvertTrigger.SetNotDelivering, _notDeliveringTrigger);
    }
}