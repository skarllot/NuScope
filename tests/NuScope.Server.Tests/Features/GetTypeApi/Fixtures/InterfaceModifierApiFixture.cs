namespace Raiqub.NuScope.Tests.Features.GetTypeApi.Fixtures;

#pragma warning disable CA1822 // Instance declarations intentionally exercise interface member metadata.

public interface IInterfaceModifierApiFixture
{
    void AbstractMethod();

    virtual void VirtualMethod() { }

    sealed void SealedMethod() { }

    static abstract void StaticAbstractMethod();

    static virtual void StaticVirtualMethod() { }

    static void StaticMethod() { }

    int AbstractProperty { get; set; }

    virtual int VirtualProperty
    {
        get => 0;
        set { }
    }

    sealed int SealedProperty
    {
        get => 0;
        set { }
    }

    static abstract int StaticAbstractProperty { get; set; }

    static virtual int StaticVirtualProperty
    {
        get => 0;
        set { }
    }

    static int StaticProperty
    {
        get => 0;
        set { }
    }

    int this[int index] { get; }

    virtual int this[string key] => 0;

    sealed int this[long index] => 0;

    event EventHandler AbstractEvent;

    virtual event EventHandler VirtualEvent
    {
        add { }
        remove { }
    }

    sealed event EventHandler SealedEvent
    {
        add { }
        remove { }
    }

    static abstract event EventHandler StaticAbstractEvent;

    static virtual event EventHandler StaticVirtualEvent
    {
        add { }
        remove { }
    }

    static event EventHandler StaticEvent
    {
        add { }
        remove { }
    }

    private void PrivateMethod() { }

    private static void PrivateStaticMethod() { }
}
