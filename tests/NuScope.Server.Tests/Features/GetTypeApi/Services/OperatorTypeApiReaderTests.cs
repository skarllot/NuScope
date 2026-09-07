using Raiqub.NuScope.Features.GetTypeApi.Services;
using Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;
using Xunit;

namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Services;

public sealed class OperatorTypeApiReaderTests
{
    [Theory]
    [InlineData("public static OperatorApiFixture operator +(OperatorApiFixture operand);")]
    [InlineData("public static OperatorApiFixture operator -(OperatorApiFixture operand);")]
    [InlineData("public static OperatorApiFixture operator !(OperatorApiFixture operand);")]
    [InlineData("public static OperatorApiFixture operator ~(OperatorApiFixture operand);")]
    [InlineData("public static OperatorApiFixture operator ++(OperatorApiFixture operand);")]
    [InlineData("public static OperatorApiFixture operator --(OperatorApiFixture operand);")]
    [InlineData("public static OperatorApiFixture operator checked -(OperatorApiFixture operand);")]
    [InlineData("public static OperatorApiFixture operator checked ++(OperatorApiFixture operand);")]
    [InlineData("public static OperatorApiFixture operator checked --(OperatorApiFixture operand);")]
    [InlineData("public static bool operator true(OperatorApiFixture operand);")]
    [InlineData("public static bool operator false(OperatorApiFixture operand);")]
    [InlineData("public static OperatorApiFixture operator +(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static OperatorApiFixture operator -(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static OperatorApiFixture operator *(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static OperatorApiFixture operator /(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static OperatorApiFixture operator %(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static OperatorApiFixture operator &(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static OperatorApiFixture operator |(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static OperatorApiFixture operator ^(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData(
        "public static OperatorApiFixture operator checked +(OperatorApiFixture left, OperatorApiFixture right);"
    )]
    [InlineData(
        "public static OperatorApiFixture operator checked -(OperatorApiFixture left, OperatorApiFixture right);"
    )]
    [InlineData(
        "public static OperatorApiFixture operator checked *(OperatorApiFixture left, OperatorApiFixture right);"
    )]
    [InlineData(
        "public static OperatorApiFixture operator checked /(OperatorApiFixture left, OperatorApiFixture right);"
    )]
    [InlineData("public static OperatorApiFixture operator <<(OperatorApiFixture left, int right);")]
    [InlineData("public static OperatorApiFixture operator >>(OperatorApiFixture left, int right);")]
    [InlineData("public static OperatorApiFixture operator >>>(OperatorApiFixture left, int right);")]
    [InlineData("public static bool operator ==(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static bool operator !=(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static bool operator <(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static bool operator >(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static bool operator <=(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public static bool operator >=(OperatorApiFixture left, OperatorApiFixture right);")]
    [InlineData("public void operator +=(OperatorApiFixture operand);")]
    [InlineData("public void operator -=(OperatorApiFixture operand);")]
    [InlineData("public void operator *=(OperatorApiFixture operand);")]
    [InlineData("public void operator /=(OperatorApiFixture operand);")]
    [InlineData("public void operator %=(OperatorApiFixture operand);")]
    [InlineData("public void operator &=(OperatorApiFixture operand);")]
    [InlineData("public void operator |=(OperatorApiFixture operand);")]
    [InlineData("public void operator ^=(OperatorApiFixture operand);")]
    [InlineData("public void operator checked +=(OperatorApiFixture operand);")]
    [InlineData("public void operator checked -=(OperatorApiFixture operand);")]
    [InlineData("public void operator checked *=(OperatorApiFixture operand);")]
    [InlineData("public void operator checked /=(OperatorApiFixture operand);")]
    [InlineData("public void operator <<=(int operand);")]
    [InlineData("public void operator >>=(int operand);")]
    [InlineData("public void operator >>>=(int operand);")]
    [InlineData("public void operator ++();")]
    [InlineData("public void operator --();")]
    [InlineData("public void operator checked ++();")]
    [InlineData("public void operator checked --();")]
    [InlineData("public static implicit operator OperatorApiFixture(int operand);")]
    [InlineData("public static explicit operator int(OperatorApiFixture operand);")]
    [InlineData("public static explicit operator checked int(OperatorApiFixture operand);")]
    public void RendersEveryOverloadableOperator(string declaration)
    {
        using var stream = File.OpenRead(typeof(OperatorApiFixture).Assembly.Location);
        var api = new NuGetTypeApiReader().ReadTypeApi(stream, typeof(OperatorApiFixture).FullName!, false);
        var expected = declaration.Replace(
            nameof(OperatorApiFixture),
            typeof(OperatorApiFixture).FullName!,
            StringComparison.Ordinal
        );

        Assert.Contains(expected, api, StringComparison.Ordinal);
    }

    [Fact]
    public void PreservesUnrecognizedOperatorNames()
    {
        using var stream = File.OpenRead(typeof(OperatorApiFixture).Assembly.Location);
        var api = new NuGetTypeApiReader().ReadTypeApi(stream, typeof(OperatorApiFixture).FullName!, false);

        Assert.Contains("public int op_Unknown();", api, StringComparison.Ordinal);
    }
}
