using System.Text;

namespace Raiqub.NuScope.Features.Common.Extensions;

internal static class StringBuilderExtensions
{
    public static StringBuilder AppendIndent(this StringBuilder builder, int indent) => builder.Append(' ', indent * 4);
}
