namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Fixture verifies nullable value types in fields and static method signatures."
)]
public sealed class NullableValueFixture<T>
    where T : struct
{
    public static readonly int? Number;
    public T? Value { get; init; }
    public int?[]? Numbers { get; init; }
    public Dictionary<string, KeyValuePair<int, string?>?>? Map { get; init; }

    public static int? Echo(int? value = null) => value;

    public static T? EchoValue(T? value) => value;

    public static TItem? EchoGeneric<TItem>(TItem? value)
        where TItem : struct => value;

    public static void Update(ref int? value, in T? input, out T? output) => output = input;
}

public delegate int? NullableValueCallback(int? value);
