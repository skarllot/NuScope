namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;

public interface INullableApiFixture<T>
    where T : class?
{
    string? Name { get; set; }
    string Required { get; }
    Dictionary<string, List<string?[]?>?>? Transform(string? input, ref string? output, T? value);
    (int, string?, List<object?>) Tuple { get; }
    KeyValuePair<int, string?>? Value { get; }
    string?[] Elements { get; }
    string[]? Array { get; }
    string?[,]? Matrix { get; }
    string? this[string? key] { get; }
    event EventHandler<string?>? Changed;
    TResult? Convert<TResult>(T? input)
        where TResult : notnull;
    void Constrain<TItem>()
        where TItem : IEnumerable<string?>;
    void Accept(out string? value, in string? input);
    string? AssignedName { set; }
}

public delegate string? NullableApiCallback(string? value);

public class NullableApiFields
{
    public static readonly string? Optional;
    public static readonly string Required = "";
    public static readonly Dictionary<string, object?>? Map;

    public static string? Echo(string? value) => value;

    public class Nested
    {
        public static string? Echo(string? value) => value;
    }

#nullable disable
    public static string Legacy(string value) => value;

    public class LegacyNested
    {
        public static string Echo(string value) => value;
    }
#nullable restore
}

public interface INullableBaseFixture : IEnumerable<string?>;

public abstract class NullableDerivedFixture : List<string?>;

public sealed class NullableInitFixture<T>
    where T : class?
{
    public string?[]? Names { get; init; }
    public List<T?>? Items { get; init; }
    public string?[,]? Grid { get; init; }
    public T? Value { get; init; }
}

#nullable disable
public interface ILegacyApiFixture
{
    string Echo(string value);
    Dictionary<string, object[]> Map { get; }
}
#nullable restore
