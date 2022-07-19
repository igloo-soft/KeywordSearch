using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordSearch
{
	partial class KeywordSearch<T>
	{
		internal struct EmptyNode : INode
		{
			public string Keyword { get; }
			public int Count => 0;

			public EmptyNode(string keyword)
			{
				Keyword = keyword;
			}

			void INode.Compact() { }

			void INode.Add(T item, Score score, IComparer<T> comparer)
			{
				throw new InvalidOperationException();
			}

			public static implicit operator EmptyNode(string keyword) => new(keyword);

			public int CompareTo(INode other) => string.CompareOrdinal(Keyword, other?.Keyword);
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			public IEnumerator<(T Item, Score Score)> GetEnumerator()
				=> Enumerable.Empty<(T Item, Score Score)>().GetEnumerator();
		}
	}
}
