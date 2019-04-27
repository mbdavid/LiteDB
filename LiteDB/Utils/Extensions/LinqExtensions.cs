using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal static class LinqExtensions
    {
        /// <summary>
        /// Better user message over SingleOrDefault() for ExecuteScalar
        /// </summary>
        public static BsonValue ScalarValue(this IEnumerable<BsonValue> source, string expressionSource)
        {
            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext()) return BsonValue.Null;
                var result = e.Current;
                if (!e.MoveNext()) return result;
            }

            throw new LiteException(0, $"Invalid scalar expression: Expression `{expressionSource}` returns more than one result");
        }

        /// <summary>
        /// Better user message over SingleOrDefault() for ExecuteScalar
        /// </summary>
        public static BsonValue ScalarValue(this IEnumerable<BsonValue> source, BsonExpression expr) => ScalarValue(source, expr.Source);

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

        /// <summary>
        /// Return same IEnumerable but indicate if item last item in enumerable
        /// </summary>
        public static IEnumerable<LastItem<T>> IsLast<T>(this IEnumerable<T> source) where T : class
        {
            T last = null;

            foreach(var item in source)
            {
                if (last != default(T))
                {
                    yield return new LastItem<T> { Item = last, IsLast = false };
                }

                last = item;
            }

            if (last != null)
            {
                yield return new LastItem<T> { Item = last, IsLast = true };
            }
        }
    }

    internal class LastItem<T>
    {
        public T Item { get; set; }
        public bool IsLast { get; set; }
    }
}