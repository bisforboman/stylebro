namespace Probe
{
    using System;
    using System.Collections.Generic;

    public class Box<T> where T : class
    {
    }

    public class Pair<TKey, TValue> where TKey : IComparable<TKey> where TValue : new()
    {
    }

    public class Split<T>
        where T : struct
    {
    }

    public class TwoOnOneLine<TA, TB>
        where TA : class where TB : class
    {
    }

    public interface IRepo<T> where T : class
    {
        TOut Map<TOut>(T item) where TOut : new();
    }

    public delegate void Handler<T>(T value) where T : class;

    public static class Methods
    {
        public static void M<T>(T value) where T : class
        {
        }

        public static void Expression<T>(T value) where T : class => Console.WriteLine(value);

        public static void Local()
        {
            void Inner<T>(T value) where T : class
            {
            }
        }

        public static void Abstract<T>() where T : class, IDisposable, new()
        {
        }
    }

    #region Fields
    public class Regions
    {
        #region Inner
        private int x;
        #endregion

        public void M()
        {
            #region InBody
            var y = 1;
            #endregion
        }

        #region Empty
        #endregion
    }
    #endregion

    /// <summary>Regions in expressions.</summary>
    public static class Switches
    {
        /// <summary>Names a number.</summary>
        /// <param name="n">The number.</param>
        /// <returns>The name.</returns>
        public static string Name(int n) => n switch
        {
            1 => "one",

            #region Larger

            2 => "two",

            #endregion

            _ => "many",
        };

        /// <summary>Lists numbers.</summary>
        /// <returns>The numbers.</returns>
        public static int[] List()
        {
            return new[]
            {
                1,
                #region More
                2,
                #endregion
            };
        }
    }
}
