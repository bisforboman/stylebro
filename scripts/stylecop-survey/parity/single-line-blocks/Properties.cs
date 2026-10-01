namespace Probe2
{
    public class Props
    {
        private int x;

        public int A { get => this.x; }

        public int B { get => this.x; set => this.x = value; }

        public int C { get; private set; }

        public int D { get; init; }

        public int E { get { return this.x; } set; }

        public int F { get; set { this.x = value; } }
    }

    public record R(int X) { }

    public record R2 { public int Y { get; set; } }

    public record struct RS(int X) { }

    public class Primary(int x) { public int X => x; }

    public interface IDefault { void M() { } }

    public class Generic<T> where T : class { }

    public class Nested
    {
        public class Inner { }

        public void M(bool b) { if (b) { return; } }
    }
}
