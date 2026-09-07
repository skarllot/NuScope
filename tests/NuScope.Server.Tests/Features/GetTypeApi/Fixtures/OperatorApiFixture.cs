namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;

public struct OperatorApiFixture : IEquatable<OperatorApiFixture>
{
    private int value;

    public static OperatorApiFixture operator +(OperatorApiFixture operand) => operand;

    public static OperatorApiFixture operator -(OperatorApiFixture operand) => operand;

    public static OperatorApiFixture operator !(OperatorApiFixture operand) => operand;

    public static OperatorApiFixture operator ~(OperatorApiFixture operand) => operand;

    public static OperatorApiFixture operator ++(OperatorApiFixture operand) => operand;

    public static OperatorApiFixture operator --(OperatorApiFixture operand) => operand;

    public static OperatorApiFixture operator checked -(OperatorApiFixture operand) => operand;

    public static OperatorApiFixture operator checked ++(OperatorApiFixture operand) => operand;

    public static OperatorApiFixture operator checked --(OperatorApiFixture operand) => operand;

    public static bool operator true(OperatorApiFixture operand) => operand.value != 0;

    public static bool operator false(OperatorApiFixture operand) => operand.value != 0;

    public static OperatorApiFixture operator +(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator -(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator *(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator /(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator %(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator &(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator |(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator ^(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator checked +(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator checked -(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator checked *(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator checked /(OperatorApiFixture left, OperatorApiFixture right) => left;

    public static OperatorApiFixture operator <<(OperatorApiFixture left, int right) => left;

    public static OperatorApiFixture operator >>(OperatorApiFixture left, int right) => left;

    public static OperatorApiFixture operator >>>(OperatorApiFixture left, int right) => left;

    public static bool operator ==(OperatorApiFixture left, OperatorApiFixture right) => left.value == right.value;

    public static bool operator !=(OperatorApiFixture left, OperatorApiFixture right) => left.value != right.value;

    public static bool operator <(OperatorApiFixture left, OperatorApiFixture right) => left.value < right.value;

    public static bool operator >(OperatorApiFixture left, OperatorApiFixture right) => left.value > right.value;

    public static bool operator <=(OperatorApiFixture left, OperatorApiFixture right) => left.value <= right.value;

    public static bool operator >=(OperatorApiFixture left, OperatorApiFixture right) => left.value >= right.value;

    public void operator +=(OperatorApiFixture operand) => value += operand.value;

    public void operator -=(OperatorApiFixture operand) => value += operand.value;

    public void operator *=(OperatorApiFixture operand) => value += operand.value;

    public void operator /=(OperatorApiFixture operand) => value += operand.value;

    public void operator %=(OperatorApiFixture operand) => value += operand.value;

    public void operator &=(OperatorApiFixture operand) => value += operand.value;

    public void operator |=(OperatorApiFixture operand) => value += operand.value;

    public void operator ^=(OperatorApiFixture operand) => value += operand.value;

    public void operator checked +=(OperatorApiFixture operand) => value += operand.value;

    public void operator checked -=(OperatorApiFixture operand) => value += operand.value;

    public void operator checked *=(OperatorApiFixture operand) => value += operand.value;

    public void operator checked /=(OperatorApiFixture operand) => value += operand.value;

    public void operator <<=(int operand) => value <<= operand;

    public void operator >>=(int operand) => value <<= operand;

    public void operator >>>=(int operand) => value <<= operand;

    public void operator ++() => value++;

    public void operator --() => value++;

    public void operator checked ++() => value++;

    public void operator checked --() => value++;

    public static implicit operator OperatorApiFixture(int operand) => new() { value = operand };

    public static explicit operator int(OperatorApiFixture operand) => operand.value;

    public static explicit operator checked int(OperatorApiFixture operand) => operand.value;

    public readonly bool Equals(OperatorApiFixture other) => value == other.value;

    public override readonly bool Equals(object? obj) => obj is OperatorApiFixture other && Equals(other);

    public override readonly int GetHashCode() => value.GetHashCode();

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1707:Identifiers should not contain underscores",
        Justification = "Exercises an unrecognized operator metadata name."
    )]
    public readonly int op_Unknown() => value;
}
