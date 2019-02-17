using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreRpc.Utilities
{
	public static class CollectionsExtentions
	{
		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (var item in collection)
			{
				action(item);
			}
		}

		public static void ParallelForEach<T>(this IEnumerable<T> collection, Action<T> action) => Parallel.ForEach(collection, action);

		public static bool Equals(byte[] first, byte[] second)
		{
			var difference = (uint) first.Length ^ (uint) second.Length;

			for (var i = 0; i < first.Length && i < second.Length; i++)
			{
				difference |= (uint) (first[i] ^ second[i]);
			}

			return difference == 0;
		}

		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue, TItem>(
			this IEnumerable<TItem> items,
			Func<int, TItem, TKey> keySelector,
			Func<int, TItem, TValue> valueSelector)
		{
			var result = new Dictionary<TKey, TValue>();
			var currentIndex = 0;

			foreach (var item in items)
			{
				result[keySelector(currentIndex, item)] = valueSelector(currentIndex, item);
				currentIndex++;
			}

			return result;
		}

		public static T[] AsArray<T>(this T item) => new[] { item };
	}
}