using System;
using System.Collections.Generic;

namespace UrlActionGenerator.Extensions
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TKey> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer = null)
        {
            if (first is null) throw new ArgumentNullException(nameof(first));
            if (second is null) throw new ArgumentNullException(nameof(second));
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

            var set = new HashSet<TKey>(second, comparer);
            foreach (var element in first)
            {
                if (set.Add(keySelector(element)))
                    yield return element;
            }
        }
    }
}
