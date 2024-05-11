using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    public static class CollectionExtensions
    {
        public static Queue<T> ToQueue<T>(this IEnumerable<T> collection)
        {
            return new Queue<T>(collection);
        }

        public static IEnumerable<IGrouping<TKey, TValue>> GroupByProportion<TKey, TValue>(
            this IEnumerable<TValue> collection,
            Dictionary<TKey, double> proportions) where TKey : notnull
        {
            var orderedProportions = proportions.OrderBy(x => x.Value).ToArray();
            var total = orderedProportions.Sum(x => x.Value);

            var q = collection.ToQueue();
            var result = new List<IGrouping<TKey, TValue>>();
            for (var i = 0; i < orderedProportions.Length; i++)
            {
                var p = orderedProportions[i];
                var count = i == orderedProportions.Length - 1
                    ? q.Count
                    : Math.Min(q.Count, (int)Math.Round((p.Value / total) * q.Count));
                var group = new List<TValue>();
                for (var j = 0; j < count; j++)
                {
                    group.Add(q.Dequeue());
                }
                result.Add(new ProportionalGrouping<TKey, TValue>(p.Key, group));
            }
            return result;
        }

        private readonly struct ProportionalGrouping<TKey, TValue>(TKey key, List<TValue> items) : IGrouping<TKey, TValue>
        {
            public TKey Key => key;
            public IEnumerator<TValue> GetEnumerator() => items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
        }
    }
}
