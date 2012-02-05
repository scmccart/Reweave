using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reweave.Core
{
    static class LinqExtensions
    {
        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, T item)
        {
            yield return item;

            foreach (var x in source)
            {
                yield return x;
            }
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T item)
        {
            foreach (var x in source)
            {
                yield return x;
            }

            yield return item;
        }

        public static IEnumerable<T> AsEnumerable<T>(this T item)
        {
            yield return item;
        }
    }
}
