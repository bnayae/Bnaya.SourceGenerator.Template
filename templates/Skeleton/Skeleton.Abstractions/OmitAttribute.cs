namespace Skeleton;

/// <summary>
/// Code generation Partial functionality, mimik the TypeScript feature: https://www.typescriptlang.org/docs/handbook/utility-types.html#Partialtype-keys
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class PartialAttribute : Attribute
{
    public PartialAttribute(params string[] properties)
    {
        Properties = properties;
    }

    public string[] Properties { get; }
}
