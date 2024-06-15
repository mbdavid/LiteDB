namespace LiteDB.Utils.Extensions;

using System;
using System.Collections.Generic;

internal static class EnumerableExtensions
{
    // calls method on dispose
    public static IEnumerable<T> OnDispose<T>(this IEnumerable<T> source, Action onDispose)
    {
        try
        {
            foreach (var item in source)
            {
                yield return item;
            }
        }
        finally
        {
            onDispose();
        }
    }
}