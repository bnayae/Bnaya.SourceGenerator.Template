namespace Skeleton;

/// <summary>
/// Code generation Omit functionality, mimik the TypeScript feature: https://www.typescriptlang.org/docs/handbook/utility-types.html#omittype-keys
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class OmitAttribute : Attribute
{
    public OmitAttribute(params string[] properties)
    {
        Properties = properties;
    }

    public string[] Properties { get; }
}
