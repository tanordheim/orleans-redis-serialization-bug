using Contracts;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Silo;

[GenerateSerializer]
public class StatefulState
{
    [Id(0)]
    public string Value { get; set; } = "";

    [Id(1)]
    public SomeType SomeType { get; set; } = new SomeType();
}

public interface IStatefulGrain : IGrainWithGuidKey
{
    Task SetState(StatefulState state);
    Task<StatefulState> GetState();
    [OneWay]
    Task ShutdownGrain();
}

public class StatefulGrain : Grain, IStatefulGrain
{
    private readonly IPersistentState<StatefulState> _state;

    public StatefulGrain([PersistentState("MyState", "TestStorage")] IPersistentState<StatefulState> state)
    {
        _state = state;
    }
    
    public async Task SetState(StatefulState state)
    {
        _state.State = state;
        await _state.WriteStateAsync();
    }

    public Task<StatefulState> GetState()
    {
        return Task.FromResult(_state.State);
    }

    public Task ShutdownGrain()
    {
        DeactivateOnIdle();
        return Task.CompletedTask;
    }
}