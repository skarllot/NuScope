using System.ComponentModel;
using System.Net.Http;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;

[Obsolete("System.IO.Stream and System.Collections.Generic.List are literal text")]
public abstract class ImplicitUsingsApiFixture
{
    public abstract DateTime Time { get; }
    public abstract List<Stream[]> Streams { get; }
    public abstract IQueryable<string> Query { get; }
    public abstract HttpClient Client { get; }
    public abstract CancellationToken Token { get; }
    public abstract Task<List<string?>> Run(CancellationToken token);
    public abstract Environment.SpecialFolder Folder { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract System.Collections.IEnumerable OtherNamespace { get; }
}
