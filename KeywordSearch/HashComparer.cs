using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordSearch
{
	internal sealed class HashComparer<T> : IComparer<T>, IEqualityComparer<T>
	{
		static readonly Func<T, T, int> CompareFunc
			= typeof(T).IsValueType ? CompareValues : CompareRefs;

		static readonly EqualityComparer<T> EqualityComparer = EqualityComparer<T>.Default;

		static int CompareValues(T x, T y)
			=> EqualityComparer.Equals(x, y) ? 0
			: y!.GetHashCode() - x!.GetHashCode();

		static int CompareRefs(T x, T y)
			=> EqualityComparer.Equals(x, y) ? 0
			: x is null ? -1
			: y is null ? 1
			: y.GetHashCode() - x.GetHashCode();

		public int Compare(T x, T y) => CompareFunc.Invoke(x, y);

		public bool Equals(T x, T y) => EqualityComparer.Equals(x, y);

		public int GetHashCode(T obj) => EqualityComparer.GetHashCode(obj);
	}
}
