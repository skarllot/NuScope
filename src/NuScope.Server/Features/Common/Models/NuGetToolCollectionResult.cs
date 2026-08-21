using System.Collections;

namespace Raiqub.NuScope.Features.Common.Models;

public abstract record NuGetToolCollectionResult<T> : NuGetToolResult, IReadOnlyList<T>
{
    private readonly IReadOnlyList<T> _items;

    protected NuGetToolCollectionResult() => _items = [];

    protected NuGetToolCollectionResult(IReadOnlyList<T> items) => _items = items;

    protected NuGetToolCollectionResult(ReadOnlySpan<T> items) => _items = items.ToArray();

    public int Count => _items.Count;

    public T this[int index] => _items[index];

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_items).GetEnumerator();
}
