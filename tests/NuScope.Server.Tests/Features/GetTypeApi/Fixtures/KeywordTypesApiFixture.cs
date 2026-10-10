namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;

[AttributeUsage(AttributeTargets.Class)]
public sealed class KeywordTypesAttribute(params Type[] types) : Attribute
{
    public Type[] Types { get; } = types;
    public string? Text { get; set; }
}

[KeywordTypes(
    typeof(bool),
    typeof(byte),
    typeof(sbyte),
    typeof(char),
    typeof(decimal),
    typeof(double),
    typeof(float),
    typeof(short),
    typeof(ushort),
    typeof(int),
    typeof(uint),
    typeof(long),
    typeof(ulong),
    typeof(object),
    typeof(string),
    typeof(void),
    typeof(nint),
    typeof(nuint),
    typeof(Dictionary<string, int[]>),
    Text = "System.Int32 and System.String"
)]
public abstract class KeywordTypesApiFixture
{
    public abstract decimal? Value { get; }
}
