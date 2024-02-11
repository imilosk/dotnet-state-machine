# .NET State Machine

The `.NET State Machine` library offers an intuitive way to define and manage workflows as state machines in your .NET
applications. Tailored for modern web, cloud-based and microservice architectures. Crafted with performance in mind, the library is stateless and thread-safe in order to minimize overhead. 

Inspired heavily by [Stateless](https://github.com/dotnet-state-machine/stateless).

## Table of Contents

- [Quick start](#quick-start)
- [Features](#features)
    - [States & Triggers](#states--triggers)
    - [Configuring the State Machine](#configuring-the-state-machine)
    - [Runtime Context](#runtime-context)
    - [Transitioning Between States](#transitioning-between-states)
    - [Exception Handling](#exception-handling)
- [Coming Soon](#coming-soon)

## Quick start

Define the context:
```csharp
public record AdvertStateMachineContext
{
    public Advert Advert { get; init; };
    public Action<Advert, AdvertState> Mutator { get; init; };
    public Action SendNotification { get; init; };
}
```

There are two ways to define the state machine and it's transitions: 

- Define in a separate class that extends the StateMachine class:
  ```csharp
  public class AdvertStateMachine : StateMachine<AdvertState, AdvertTrigger, AdvertStateMachineContext>
  {
      public AdvertStateMachine()
      {
          Configure(AdvertState.None)
              .Permit(AdvertTrigger.Create, AdvertState.Draft);
      }
  }
  ```
  And use it as a static property:
  ```csharp
  public class AdvertService
  {
      private static readonly AdvertStateMachine StateMachine = new();
  }
  ```
- Or define it without a separate class in a static constructor:
  ```csharp
  public class AdvertService
  {
      private static readonly StateMachine<AdvertState, AdvertTrigger, AdvertStateMachineContext> StateMachine = new();
      
      public static AdvertStateMachine()
      {
          StateMachine.Configure(AdvertState.None)
              .Permit(AdvertTrigger.Create, AdvertState.Draft);
              
          StateMachine.Mutator = (newState, context) => { context.Mutator(context.Advert, newState); };
      }
  }
  ```
  
Usage:
```csharp
private void UpdateData(Advert advert, AdvertState newState)
{
    advert.State = newState;
    // Example: Store advert to the database
}

private void SendNotification()
{
    Console.WriteLine("Entry notification");
    // Example: Use another service to send the notification
}

public void CreateAdvert(AdvertRequest request) {
    var advert = new Advert
    {
        State = AdvertState.None,
        Name = request.Name
    };
    
    var context = new AdvertStateMachineContext
    {
        Advert = advert,
        Mutator = UpdateData,
        SendNotification = SendNotification
    };
    
    var newState = stateMachine.Fire(advert.State, AdvertTrigger.Create, context); // result: Draft
}
```

## Features

- Persistent state machine definition: Set up your state machine just once, and it remains active for the entire application's lifespan.
- Generic type support: define states and triggers using any data type (numbers, strings, enums, etc.)
- Entry & exit actions: Execute specific tasks or actions when states are entered or exited
- Internal transitions: Perform an action without transitioning to a new state
- Ignore triggers: Override the default behaviour of throwing an exception when an unhandled trigger is fired
- Reentrant states: Enable states to transition back to themselves
- Ability to store state externally using a mutator
- Events
  - `OnTransitionCompleted` - This event is invoked every time after the state machine changes state. Note: This event will not the fired for ignored triggers and internal transitions. 

## Coming soon

**State configuration**
- Hierarchical states: `SubstateOf`
- On entry from a specific trigger: `OnEntryFrom`
- Guard clauses/conditional transitions: 
    - `PermitIf`
    - `PermitReentryIf`
    - `InternalTransitionIf`
    - `IgnoreIf`
- Dynamic selection of the destination state:
    - `PermitDynamic`
    - `PermitDynamicIf`

**Events**
- `OnBeforeTransition`
- `OnUnhandledTrigger`

**Other**
- Exporting to [DOT graph language](https://en.wikipedia.org/wiki/DOT_(graph_description_language)) to be visualized in tools like http://www.webgraphviz.com.
- Exporting to [XState](https://xstate.js.org/) definition JSON to be used for visualizing the state machine on https://xstate.js.org/viz.

### Will probably never be supported
- Parameterised Triggers
- Async triggers