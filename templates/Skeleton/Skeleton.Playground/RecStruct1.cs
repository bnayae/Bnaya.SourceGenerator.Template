#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation

using Skeleton;

namespace Skeleton.Playground;

[Partial]
public readonly partial record struct RecStruct1(int Value, string Name)
{
    public required int Quantity { get; init; }
    public string Tag { get; init; } = string.Empty;

    public string Description => $"{Name}: {Quantity}";
}
