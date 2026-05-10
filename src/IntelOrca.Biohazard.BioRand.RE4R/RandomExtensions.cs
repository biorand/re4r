using System;
using System.Collections.Generic;
using System.Linq;
namespace IntelOrca.Biohazard.BioRand
{
    public static class RandomExtensions
    {
        public static T NextOf<T>(this Rng rng, IEnumerable<T> items)
        {
            var count = items.Count();
            var index = rng.Next(0, count);
            return items.ElementAt(index);
        }

        public static T NextOf8020<T>(this Rng rng, params T[] values)
        {
            for (var i = 0; i < values.Length - 1; i++)
            {
                if (rng.NextProbability(80))
                {
                    return values[i];
                }
            }
            return values[^1];
        }

    }
}
