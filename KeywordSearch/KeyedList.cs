using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordSearch
{
	internal class KeyedList<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
	{
		public readonly static IComparer<TKey> DefaultKeyComparer = CreateKeyComparer();
		static IComparer<TKey> CreateKeyComparer()
		{
			var t = typeof(TKey);
			if (t == typeof(string))
			{
				return (IComparer<TKey>)StringComparer.Ordinal;
			}
			else if (typeof(IComparable<TKey>).IsAssignableFrom(t)
				|| typeof(IComparable).IsAssignableFrom(t))
			{
				return Comparer<TKey>.Default;
			}
			else
			{
				return new HashComparer<TKey>(); ;
			}
		}

		public List<TKey> Keys { get; private set; } = new();
		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

		public List<TValue> Values { get; private set; } = new();
		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

		public int Count => Keys.Count;

		public TValue this[TKey key]
		{
			get
			{
				TryGetValue(key, out var value);
				return value!;
			}
		}

		public void Compact()
		{
			var n = Count;
			if (Keys.Capacity != n) { Keys = new(Keys); }
			if (Values.Capacity != n) { Values = new(Values); }
		}

		public bool ContainsKey(TKey key) => TryFindIndex(key, out _, DefaultKeyComparer);
		public bool ContainsKey(TKey key, IComparer<TKey> comparer) => TryFindIndex(key, out _, comparer);

		public bool TryGetValue(TKey key, out TValue value)
			=> TryGetValue(key, out value, DefaultKeyComparer);
		public bool TryGetValue(TKey key, out TValue value, IComparer<TKey> comparer)
		{
			if (TryFindIndex(key, out var i, comparer))
			{
				value = Values[i];
				return true;
			}
			else
			{
				value = default!;
				return false;
			}
		}

		public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
			=> AddOrUpdate(key, addValue, updateValueFactory, DefaultKeyComparer);
		public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory, IComparer<TKey> comparer)
		{
			int insertBefore;
			var last = Keys.Count - 1;

			if (last < 0)
			{
				// Empty.
				insertBefore = 0;
			}
			else
			{
				// Compare last as fast path for append.
				var compareLast = comparer.Compare(Keys[last], key);
				if (compareLast == 0)
				{
					// Replace the last value.
					var newValue = updateValueFactory.Invoke(key, Values[last]);
					Values[last] = newValue;
					return newValue;
				}
				else if (compareLast > 0)
				{
					// Append at the end.
					insertBefore = last + 1;
				}
				else
				{
					if (TryFindIndex(key, out var i, comparer))
					{
						// Binary search found key. Repalce value.
						var newValue = updateValueFactory.Invoke(key, Values[i]);
						Values[i] = newValue;
						return newValue;
					}
					else
					{
						insertBefore = ~i;
					}
				}

			}

			Keys.Insert(insertBefore, key);
			Values.Insert(insertBefore, addValue);

			return addValue;
		}

		public bool TryFindIndex(TKey key, out int index) => TryFindIndex(key, out index, DefaultKeyComparer);
		public bool TryFindIndex(TKey key, out int index, IComparer<TKey> comparer)
		{
			index = Keys.BinarySearch(key, comparer);
			return index >= 0;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			for (var i = 0; i < Keys.Count; i++)
			{
				yield return new(Keys[i], Values[i]);
			}
		}
	}
}
