using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal static class LinqExtensions
    {
        /// <summary>
        /// Implement FirstOrDefault() with full scan option. This is used in GroupBy method to read all source even if want only first document
        /// </summary>
        public static T FirstOrDefault<T>(this IEnumerable<T> source, bool scanSource)
            where T : class
        {
            if (scanSource == false)
            {
                return source.FirstOrDefault();
            }

            T first = null;

            foreach (var item in source)
            {
                if (first == null) first = item;
            }

            return first;
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<int, T> action)
        {
            var index = 0;

            foreach(var item in source)
            {
                action(index++, item);

                yield return item;
            }
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return YieldBatchElements(enumerator, batchSize - 1);
                }
            }
        }

        private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> source, int batchSize)
        {
            yield return source.Current;

            for (int i = 0; i < batchSize && source.MoveNext(); i++)
            {
                yield return source.Current;
            }
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return _();

            IEnumerable<TSource> _()
            {
                var knownKeys = new HashSet<TKey>(comparer);

                foreach (var element in source)
                {
                    if (knownKeys.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }
            }
        }
    }
}