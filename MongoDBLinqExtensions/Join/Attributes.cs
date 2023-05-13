namespace MongoDBLinqExtensions.Join;

[AttributeUsage(AttributeTargets.Property)]
public sealed class LocalKeyAttribute : Attribute
{
    public string Name { get; }

    public LocalKeyAttribute(string name) => Name = name;
}