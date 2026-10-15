namespace Cases
{
    using System;

    public interface IServer
    {
        event EventHandler E;

        event EventHandler F;

        void M();

        int P { get; }
    }

    public class ExplicitEvent : IServer
    {
        private EventHandler e;

        private static event EventHandler PrivateStaticEvent;

        event EventHandler IServer.E
        {
            add => this.e += value;
            remove => this.e -= value;
        }

        private event EventHandler PrivateEvent;

        public event EventHandler F;

        public int P => 0;

        private void Helper()
        {
        }

        void IServer.M()
        {
        }
    }

    public record RecordStatic(string Name)
    {
        internal void Instance()
        {
        }

        internal static RecordStatic Create() => new("x");
    }

    public record RecordAccess(string Name)
    {
        private void P()
        {
        }

        internal void I()
        {
        }
    }

    public record RecordKind(string Name)
    {
        public void M()
        {
        }

        public int Count { get; set; }
    }

    public record RecordConstant
    {
        private static int s;

        private const int C = 1;

        private int a;

        private readonly int b;
    }

    public record struct RecordStructStatic(int X)
    {
        public void Instance()
        {
        }

        public static void Create()
        {
        }
    }

    public class ClassStatic
    {
        internal void Instance()
        {
        }

        internal static ClassStatic Create() => new();
    }
}
