using System;
using System.Collections.Generic;
using System.Linq;

namespace Messy.Simplifications
{
    public enum Level
    {
        Low,
        High,
    }

    public record Reading(int Value);

    public class Simplifications : IDisposable
    {
        private readonly List<int> _values = new List<int> { 1, 2, 3 };

        public IEnumerable<int> Large() => _values.Where(value => value > 1);

        public string Label(Level level)
        {
            string prefix = "Level";
            string suffix = "!";
            if (level == Level.Low)
            {
                return prefix + " low" + suffix;
            }
            else if (level == Level.High)
            {
                return prefix + " high" + suffix;
            }
            else
            {
                return "{unknown}";
            }
        }

        public IEnumerable<int> Positive()
        {
            foreach (var value in _values)
            {
                if (value > 0)
                {
                    yield return value;
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _values.Clear();
        }
    }
}
