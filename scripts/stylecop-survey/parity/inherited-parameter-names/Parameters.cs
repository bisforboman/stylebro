namespace Probe
{
    internal interface IStore
    {
        void Save(int id, string name);

        void Load(int id);
    }

    internal interface IList
    {
        int this[int index] { get; }

        void Add(int item);
    }

    internal class Items : IList
    {
        public int this[int position] => position;

        void IList.Add(int value) => System.Console.WriteLine(value);
    }

    internal class Middle : Base
    {
        public override void Run(int count) => System.Console.WriteLine(count);
    }

    internal class Last : Middle
    {
        public override void Run(int total) => System.Console.WriteLine(total);
    }

    internal interface IOtherStore
    {
        void Load(int key);
    }

    internal abstract class Base
    {
        public abstract void Run(int count);

        public virtual void Stop(int code)
        {
        }
    }

    internal class Store : IStore, IOtherStore
    {
        public void Save(int key, string name) => System.Console.WriteLine(key + name);

        // Implements both Load(int id) and Load(int key).
        public void Load(int key) => System.Console.WriteLine(key);

        public override bool Equals(object other) => other is Store;

        public override int GetHashCode() => 0;
    }

    internal class Derived : Base
    {
        public override void Run(int number) => System.Console.WriteLine(number);

        public override void Stop(int _)
        {
        }
    }

    internal class Checked : Base
    {
        public override void Run(int amount)
        {
            if (amount < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(amount));
            }
        }
    }

    public interface IPublicStore
    {
        void Put(int id);
    }

    public class PublicStore : IPublicStore
    {
        public void Put(int key) => System.Console.WriteLine(key);
    }

    internal static class Caller
    {
        public static void Use(Store store, Derived derived)
        {
            store.Save(key: 1, name: "a");
            derived.Run(number: 2);
        }
    }
}
