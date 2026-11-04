using Raiqub.NuScope.Features.GetTypeApi.Services;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class ImplicitUsingsTypeApiReaderTests
{
    [Fact]
    public void ImportsStandardNamespacesAndShortensOnlyTheirTypes()
    {
        using var stream = File.OpenRead(typeof(ImplicitUsingsTypeApiReaderTests).Assembly.Location);
        var api = new NuGetTypeApiReader().ReadTypeApi(
            stream,
            "Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures.ImplicitUsingsApiFixture",
            false
        );

        Assert.NotNull(api);
        var expected = """
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Net.Http;
            using System.Threading;
            using System.Threading.Tasks;

            namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;

            [ObsoleteAttribute("System.IO.Stream and System.Collections.Generic.List are literal text")]
            public abstract class ImplicitUsingsApiFixture
            {
                public abstract Task<List<string?>> Run(CancellationToken token);
                protected ImplicitUsingsApiFixture();
                public abstract DateTime Time { get; }
                public abstract List<Stream[]> Streams { get; }
                public abstract IQueryable<string> Query { get; }
                public abstract HttpClient Client { get; }
                public abstract CancellationToken Token { get; }
                public abstract Environment.SpecialFolder Folder { get; }
                [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                public abstract System.Collections.IEnumerable OtherNamespace { get; }
            }
            """;
        Assert.Equal(expected.ReplaceLineEndings("\n") + "\n", api.ReplaceLineEndings("\n"));
    }
}
