namespace Messy.Simplifications
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public enum Level : int
    {
        Low,
        High,
    }

    public record Reading(int Value) { }

    public class Simplifications : object, IDisposable
    {
        private readonly List<int> values = new List<int>() { 1, 2, 3 };

        public IEnumerable<int> Large() => values.Where((value) => value > 1);

        public string Label(Level level)
        {
            string prefix = $"Level", suffix = @"!";
            if (level == Level.Low)
            {
                return prefix + " low" + suffix;
            }
            else
            {
                if (level == Level.High)
                {
                    return prefix + " high" + suffix;
                }
                else
                {
                    return $$"""{unknown}""";
                }
            }
        }

        public IEnumerable<int> Positive()
        {
            foreach (var value in values)
            {
                if (value > 0)
                {
                    yield return value;
                }
            }

            yield break;
        }

        public void Dispose()
        {
            values.Clear();
            return;
        }
    }
}
