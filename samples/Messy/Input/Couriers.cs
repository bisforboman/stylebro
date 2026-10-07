namespace Messy.Couriers
{
    public interface ICourier
    {
        string Name { get; }

        void Send(string parcel);
    }

    public static class Routes
    {
        public static T First<T>(T[] items)
            where T : class
        {
            return items[0];
        }
    }

    public abstract class Courier : ICourier
    {
        protected Courier(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public abstract void Send(string parcel);

        public int Priority() => 1;
        public int Weight() => 2;
    }

    public sealed class Bike : Courier
    {
        public Bike()
            : base("bike")
        {
        }

        public override void Send(string parcel) => System.Console.WriteLine(parcel);
    }
}
