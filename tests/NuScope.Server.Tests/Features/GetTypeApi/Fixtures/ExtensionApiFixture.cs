namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;

public static class ExtensionApiFixture
{
    public static string? Echo(this string? value, string suffix = "") => value + suffix;

    public static int CountItems<T>(this IEnumerable<T> values) => values.Count();

    public static void Increment(this ref int value) => value++;

    public static int Read(this in int value) => value;

    public static int Ordinary(string value) => value.Length;

    internal static int Hidden(this string value) => value.Length;
}
