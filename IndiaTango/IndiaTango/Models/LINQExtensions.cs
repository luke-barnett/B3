using System;
using System.Collections.Generic;
using System.Linq;

namespace IndiaTango.Models
{
    /// <summary>
    /// Set of useful LINQ Extensions
    /// </summary>
    public static class LINQExtensions
    {
        public static float Variance(this IEnumerable<float> source)
        {
            var sourceInArray = source.ToArray();
            var avg = sourceInArray.Average();
            var d = sourceInArray.Aggregate(0f, (total, next) => total += (float)Math.Pow(next - avg, 2));
            return d / (sourceInArray.Length - 1);
        }

        public static float StandardDeviation(this IEnumerable<float> source)
        {
            return (float) Math.Sqrt(source.Variance());
        }

        public static float Median(this IEnumerable<float> source)
        {
            var sortedList = from number in source
                             orderby number
                             select number;

            var count = sortedList.Count();
            var itemIndex = count / 2;
            if (count % 2 == 0) // Even number of items. 
                return (sortedList.ElementAt(itemIndex) +
                        sortedList.ElementAt(itemIndex - 1)) / 2;

            // Odd number of items. 
            return sortedList.ElementAt(itemIndex);
        }

        public static IEnumerable<T> DropLast<T>(this IEnumerable<T> xs)
        {
            var lastX = default(T);
            var first = true;
            foreach (var x in xs)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    yield return lastX;
                }
                lastX = x;
            }
        }
    }
}
