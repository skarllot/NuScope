namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;

public abstract class ParamsApiFixture
{
    protected ParamsApiFixture([ApiMark("params-constructor")] params string?[] values) { }

    public abstract void Array(int first, [ApiMark("params-array")] params int[] values);
    public abstract void Collection<T>([ApiMark("params-collection")] params IEnumerable<T> values);
    public abstract void Span(params ReadOnlySpan<int> values);
    public abstract void Ordinary(int[] values);
    public abstract void Lookalike([ParamArray] [ParamCollection] int[] values);
    public abstract string this[[ApiMark("params-indexer")] params string[] values] { get; }
    internal abstract void Hidden(params int[] values);
}

public delegate void ParamsArrayCallback(params string?[] values);
public delegate void ParamsCollectionCallback<T>(params IEnumerable<T> values);

public static class ParamsExtensionFixture
{
    public static void Combine(this string value, params string[] values) { }
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ParamArrayAttribute : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ParamCollectionAttribute : Attribute;
