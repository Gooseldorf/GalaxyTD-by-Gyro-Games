using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Pool;

namespace CardTD.Utilities
{
    public static class ListExtensions
    {
        public static void Release<T>(this List<T> source)
        {
            if (source != null)
            {
                ListPool<T>.Release(source);
            }
        }
        
        public static void TryRelease<TKey, TValue>(this Dictionary<TKey, TValue> source)
        {
            if (source != null)
            {
                DictionaryPool<TKey,TValue>.Release(source);
            }
        }
        

        public static bool TrueForAll<T>(this IList<T> source, Predicate<T> match)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (match == null)
                throw new ArgumentNullException(nameof(match));

            for (int index = 0; index < source.Count; ++index)
            {
                if (!match(source[index]))
                    return false;
            }

            return true;
        }

        public static void RemoveRange<T>(this IList<T> source, IList<T> other)
        {
            if (IsNullOrEmpty(other))
                return;

            foreach (T item in other)
                source.Remove(item);
        }

        public static List<T> Except<T>(this IReadOnlyList<T> source, [NotNull] Predicate<T> predicate)
        {
            List<T> result = new();
            for (int i = 0; i < source.Count; i++)
            {
                if (!predicate.Invoke(source[i]))
                    result.Add(source[i]);
            }

            return result;
        }

        public static List<T> Except<T>(this IReadOnlyList<T> source, IList<T> other)
        {
            List<T> result = new();

            for (int i = 0; i < source.Count; i++)
            {
                if (other.Contains(source[i]))
                    continue;

                result.Add(source[i]);
            }

            return result;
        }


        public static List<T> PooledExcept<T>(this IReadOnlyList<T> source, [NotNull] Predicate<T> predicate)
        {
            List<T> result = ListPool<T>.Get();

            foreach (T item in source)
            {
                if (!predicate.Invoke(item))
                    result.Add(item);
            }

            return result;
        }

        /// <summary>Checks if elements of another sequence are in this sequence</summary>
        /// <param name="source">Origin sequence</param>
        /// <param name="other">Checked sequence</param>
        public static bool Contains<T>(this IList<T> source, IList<T> other)
        {
            if (IsNullOrEmpty(other))
                return false;

            foreach (T item in other)
            {
                if (!source.Contains(item))
                    return false;
            }

            return true;
        }


        public static bool Contains<T>(this IList<T> source, T sample, out T reference)
        {
            reference = default;

            if (IsNullOrEmpty(source))
                return false;

            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].Equals(sample))
                {
                    reference = source[i];
                    return true;
                }
            }

            return false;
        }


        /// <summary>Checks if there is at least one element of another sequence in this sequence</summary>
        /// <param name="source">Origin sequence</param>
        /// <param name="other">Checked sequence</param>
        public static bool ContainsAny<T>(this IList<T> source, IList<T> other)
        {
            if (IsNullOrEmpty(other))
                return false;

            foreach (T item in other)
            {
                if (source.Contains(item))
                    return true;
            }

            return false;
        }

        public static bool ContainsAny<T>(this IList<T> source, IList<T> other, out T firstAnyResult)
        {
            firstAnyResult = default;

            if (IsNullOrEmpty(other))
                return false;

            foreach (T item in other)
            {
                if (source.Contains(item, out T itemRef))
                {
                    firstAnyResult = itemRef;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Returns the first element of the sequence matching the condition</summary>
        public static T FirstOrDefault<T>(this IList<T> source, Predicate<T> predicate = null)
        {
            if (predicate == null)
            {
                return source is {Count: > 0}
                    ? source[0]
                    : default;
            }

            foreach (T value in source)
            {
                if (predicate.Invoke(value))
                    return value;
            }

            return default;
        }

        public static T FirstOrDefault<T>(IEnumerable<T> source, Predicate<T> predicate = null)
        {
            return FirstOrDefault((IList<T>)source, predicate);
        }

        public static int Sum<T>(this IList<T> source, Func<T, int> predicate)
        {
            int result = 0;

            foreach (T value in source)
                result += predicate.Invoke(value);

            return result;
        }

        public static int Min<T>(this IList<T> source, Func<T, int> predicate)
        {
            int min = int.MaxValue;

            foreach (T value in source)
            {
                int tmpValue = predicate.Invoke(value);
                if (tmpValue < min)
                    min = tmpValue;
            }

            return min;
        }

        public static int Max<T>(this IList<T> source, Func<T, int> predicate)
        {
            int max = int.MinValue;

            foreach (T value in source)
            {
                int tmpValue = predicate.Invoke(value);
                if (tmpValue > max)
                    max = tmpValue;
            }

            return max;
        }

        public static IList<T> Clone<T>(this IList<T> source) where T : ICloneable
        {
            IList<T> result = new List<T>(source.Count);

            foreach (T item in source)
                result.Add((T)item.Clone());

            return result;
        }

        public static T[] ToArray<T>(this IEnumerable<T> source)
        {
            int index = 0;
            int count = ((ICollection<T>)source).Count;
            T[] result = new T[count];

            foreach (T item in source)
                result[index++] = item;

            return result;
        }
        
        public static U[] ToArray<T, U>(this IEnumerable<T> source, Func<T, U> value)
        {
            int index = 0;
            int count = ((ICollection<T>)source).Count;
            U[] result = new U[count];

            foreach (T item in source)
                result[index++] = value(item);

            return result;
        }

        public static bool SequenceEqual<TSource>(this IList<TSource> first, IList<TSource> second,
            IEqualityComparer<TSource> comparer = null)
        {
            comparer ??= EqualityComparer<TSource>.Default;

            if (first == null)
                throw new ArgumentNullException(nameof(first));

            if (second == null)
                throw new ArgumentNullException(nameof(second));


            using IEnumerator<TSource> firstEnum = first.GetEnumerator();
            using IEnumerator<TSource> secondEnum = second.GetEnumerator();

            while (firstEnum.MoveNext())
            {
                if (!(secondEnum.MoveNext() && comparer.Equals(firstEnum.Current, secondEnum.Current)))
                    return false;
            }

            return !secondEnum.MoveNext();
        }

        public static List<U> Cast<U>(this IList source)
        {
            List<U> result = new(source.Count);

            foreach (object o in source)
                result.Add((U)o);

            return result;
        }

        public static bool IsNullOrEmpty<T>(IList<T> list) => list == null || list.Count == 0;

        public static List<T> Intersect<T>(this List<T> listA, List<T> listB)
        {
            List<T> result = new();

            for (int i = 0; i < listA.Count; i++)
            {
                if (listB.Contains(listA[i]))
                {
                    result.Add(listA[i]);
                }
            }

            return result;
        }

        public static void AddRangeUnique<T>(this IList<T> source, IEnumerable<T> collection)
        {
            foreach (T item in collection)
            {
                if (!source.Contains(item))
                    source.Add(item);
            }
        }

        public static void AddIfTrue<T>(this IList<T> source, bool condition, T item)
        {
            if (condition)
                source.Add(item);
        }

        public static void AddIfTrue<T>(this IList<T> source, Predicate<T> condition, T item)
        {
            if (condition(item))
                source.Add(item);
        }

        public static IEnumerable<T> OfType<T>(this IEnumerable source)
        {
            foreach (object obj in source)
            {
                if (obj is T t)
                    yield return t;
            }
        }
    }
}