using System;
using System.Collections.Generic;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class EndlessBag<T>
    {
        private readonly Rng _rng = new();
        private readonly List<T> _allItems = [];
        private readonly Queue<T> _items = new();

        public int Count => _allItems.Count;

        public EndlessBag()
        {
        }

        public EndlessBag(Rng rng)
        {
            _rng = rng;
        }

        public EndlessBag(Rng rng, IEnumerable<T> items)
        {
            _rng = rng;
            _allItems.AddRange(items);
        }

        public void AddRange(IEnumerable<T> items)
        {
            var remainingItems = _items.Concat(items).Shuffle(_rng);

            _allItems.AddRange(items);
            _items.Clear();
            foreach (var item in remainingItems)
            {
                _items.Enqueue(item);
            }
        }

        public T Next()
        {
            if (_allItems.Count == 0)
                throw new Exception("No items in bag.");

            if (_items.Count == 0)
            {
                var toAdd = _allItems.Shuffle(_rng);
                foreach (var item in toAdd)
                {
                    _items.Enqueue(item);
                }
            }
            return _items.Dequeue();
        }

        public T[] Next(int count)
        {
            var result = new T[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = Next();
            }
            return result;
        }
    }
}
