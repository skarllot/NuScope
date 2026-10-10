using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class ApiMarkAttribute(string label) : Attribute
{
    public string Label { get; } = label;
    public AttributeFixtureKind Kind { get; set; }
    public AttributeFixtureOptions Options { get; set; }
    public AttributeFixtureSignedKind SignedKind { get; set; }
    public Type? Type { get; set; }
    public int[]? Values { get; set; }
    public object? Box { get; set; }
    public float Amount { get; set; }
    public string? Text { get; set; }
    public bool Enabled { get; set; }
    public int Order { get; }

    public ApiMarkAttribute(string label, int order)
        : this(label) => Order = order;
}

public enum AttributeFixtureKind : byte
{
    [ApiMark("enum-value")]
    One = 1,
    Two = 2,
}

[Flags]
public enum AttributeFixtureOptions : ulong
{
    None = 0,
    Read = 1,
    Write = 2,
    High = 1UL << 63,
}

public enum AttributeFixtureSignedKind : short
{
    MinusOne = -1,
}

[ApiMark(
    "type",
    Kind = AttributeFixtureKind.Two,
    Type = typeof(Dictionary<string, List<int[]>>),
    Values = new[] { 1, 2 },
    Box = (byte)7,
    Amount = 1.5F,
    Text = "a\n\"b",
    Enabled = true
)]
public sealed class AttributeApiFixture<[ApiMark("type-parameter")] T>
    where T : class?
{
    [ApiMark("field")]
    [SuppressMessage(
        "Design",
        "CA1051:Do not declare visible instance fields",
        Justification = "Fixture verifies field attributes."
    )]
    public readonly int Value;

    [ApiMark("constructor")]
    public AttributeApiFixture([ApiMark("constructor-parameter")] int value) => Value = value;

    [ApiMark("property")]
    [field: ApiMark("property-field")]
    public string? Name
    {
        [ApiMark("getter")]
        [return: ApiMark("getter-return")]
        get;
        [ApiMark("setter")]
        [param: ApiMark("setter-value")]
        set;
    }

    [ApiMark("indexer")]
    public string? this[[ApiMark("indexer-parameter")] string key] => key;

    [ApiMark("event")]
    [field: ApiMark("event-field")]
    [method: ApiMark("event-method")]
    public event EventHandler? Changed;

    [ApiMark("method", 3)]
    [return: ApiMark("return")]
    [return: MaybeNull]
    public TResult? Convert<[ApiMark("method-parameter")] TResult>([ApiMark("parameter")] T? value)
        where TResult : class
    {
        Changed?.Invoke(this, EventArgs.Empty);
        return default;
    }

    public bool HasValue([NotNullWhen(true)] string? value) => value is not null && Value >= 0;

    [ApiMark("nested-type")]
    public sealed class Nested;

    [ApiMark("hidden")]
    private string? Hidden() => Name;
}

[ApiMark("delegate")]
[return: ApiMark("delegate-return")]
public delegate string? AttributeApiCallback([ApiMark("delegate-parameter")] string? value);

public static class AttributeExtensionFixture
{
    public static int Length([ApiMark("receiver")] this string value) => value.Length;

    [ApiMark("unbound", Type = typeof(Dictionary<,>), Box = null, Values = null)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static string Echo(string value) => value;

    [GeneratedApiMarker]
    [NeverInstantiate]
    public static string MetadataOnly(string value) => value;

    [ApiMark("flags", Options = AttributeFixtureOptions.Read | AttributeFixtureOptions.Write)]
    [ApiMark("zero", Options = AttributeFixtureOptions.None)]
    [ApiMark("high", Options = AttributeFixtureOptions.High)]
    [ApiMark("negative", SignedKind = AttributeFixtureSignedKind.MinusOne)]
    [ApiMark("unnamed", Kind = (AttributeFixtureKind)99)]
    [ApiMark("unknown-bits", Options = (AttributeFixtureOptions)4)]
    public static string EnumValues(string value) => value;
}

[CompilerGenerated]
[AttributeUsage(AttributeTargets.Method)]
public sealed class GeneratedApiMarkerAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class NeverInstantiateAttribute : Attribute
{
    public NeverInstantiateAttribute() => throw new InvalidOperationException("Attribute constructors must not run.");
}
