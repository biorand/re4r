using System;
using System.Collections.Generic;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand
{
    public static class RandomExtensions
    {
        public static T NextOf<T>(this Random random, IEnumerable<T> items)
        {
            var count = items.Count();
            var index = random.Next(0, count);
            return items.ElementAt(index);
        }

        public static T[] Shuffle<T>(this IEnumerable<T> items, Random random)
        {
            var array = items.ToArray();
            for (int i = 0; i < array.Length - 1; i++)
            {
                var ri = random.Next(i, array.Length);
                var tmp = array[ri];
                array[ri] = array[i];
                array[i] = tmp;
            }
            return array;
        }
    }
}
