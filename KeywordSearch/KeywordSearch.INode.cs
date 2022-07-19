using System;
using System.Collections.Generic;

namespace KeywordSearch
{
	partial class KeywordSearch<T>
	{
		internal interface INode : IComparable<INode>, IReadOnlyCollection<(T Item, Score Score)>
		{
			string Keyword { get; }

			void Compact();
			void Add(T item, Score score, IComparer<T> comparer);

		}
	}
}