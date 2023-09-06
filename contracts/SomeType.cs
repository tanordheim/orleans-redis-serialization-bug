using Orleans;

namespace Contracts;

[GenerateSerializer]
public class SomeType
{
    [Id(0)]
    public Guid Value { get; set; }
}