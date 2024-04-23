using System;
using System.Collections.Generic;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public sealed class Rng
    {
        private readonly Random _random;

        public Rng()
        {
            _random = new Random();
        }

        public Rng(int seed)
        {
            _random = new Random(seed);
        }

        public Rng NextFork()
        {
            return new Rng(_random.Next());
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }

        public double NextDouble(double min, double max)
        {
            if (max <= min)
                return min;

            var range = max - min;
            return min + (_random.NextDouble() * range);
        }

        public bool NextProbability(int percent)
        {
            return Next(0, 100) < percent;
        }

        public int Next(int min, int max)
        {
            if (max <= min)
                return min;
            return _random.Next(min, max);
        }

        public T NextOf<T>(params T[] values)
        {
            var i = _random.Next(0, values.Length);
            return values[i];
        }

        public T Next<T>(IEnumerable<T> values)
        {
            var i = _random.Next(0, values.Count());
            return values.ElementAt(i);
        }

        public double NextGaussian(double mean, double stdDev)
        {
            var u1 = 1.0 - _random.NextDouble();
            var u2 = 1.0 - _random.NextDouble();

            // random normal(0, 1)
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            // random normal(mean, stdDev ^ 2)
            var randNormal = mean + stdDev * randStdNormal;

            return randNormal;
        }

        public Table<T> CreateProbabilityTable<T>()
        {
            return new Table<T>(this);
        }

        public Guid NextGuid()
        {
            var buffer = new byte[16];
            _random.NextBytes(buffer);
            return new Guid(buffer);
        }

        public class Table<T>
        {
            private readonly Rng _rng;
            private readonly List<(T, double)> _table = new List<(T, double)>();
            private double _total;

            public bool IsEmpty => _table.Count == 0;
            public T[] Values => _table.Select(x => x.Item1).ToArray();

            public Table(Rng rng)
            {
                _rng = rng;
            }

            public void Add(T value, double prob)
            {
                if (prob == 0)
                    return;

                _table.Add((value, prob));
                _total += prob;
            }

            public T Next()
            {
                if (_table.Count == 0)
                    throw new InvalidOperationException("No probability entries added");
                if (_table.Count > 1)
                {
                    var p = 0.0;
                    var n = _rng.NextDouble() * _total;
                    for (int i = 0; i < _table.Count - 1; i++)
                    {
                        var entry = _table[i];
                        var nextI = p + entry.Item2;
                        if (n < nextI)
                        {
                            return entry.Item1;
                        }
                        p = nextI;
                    }
                }
                return _table[_table.Count - 1].Item1;
            }
        }
    }
}
