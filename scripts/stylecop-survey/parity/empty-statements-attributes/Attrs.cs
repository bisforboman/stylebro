[assembly: System.CLSCompliant(false), System.Reflection.AssemblyTrademark("x")]

namespace Probe
{
    using System;
    using System.Diagnostics;

    public class Attrs
    {
        [Obsolete, DebuggerStepThrough]
        public void A() { }

        [return: MarshalAsDummy, NotNullDummy]
        public int B() => 0;

        public void C([Dummy, Dummy2] int x) { }

        [Obsolete("a, b"), /* why */ DebuggerStepThrough]
        public void D() { }
    }

    public class MarshalAsDummyAttribute : Attribute { }
    public class NotNullDummyAttribute : Attribute { }
    public class DummyAttribute : Attribute { }
    public class Dummy2Attribute : Attribute { }
}
