namespace Raiqub.NuScope.Features.GetTypeApi.Services.Metadata;

// Retain the signature tree until flags can be consumed in metadata's pre-order traversal.
internal sealed record NullableType(string Name, bool IsValueType = false, Func<Func<byte>, string>? Formatter = null)
{
    public string Render(Func<byte> next) =>
        Formatter is not null ? Formatter(next) : Name + (!IsValueType && next() == 2 ? "?" : "");
}
