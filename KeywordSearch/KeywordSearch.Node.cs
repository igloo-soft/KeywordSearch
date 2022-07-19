using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace KeywordSearch
{
	public partial class KeywordSearch<T>
	{
		[DebuggerDisplay("{DebuggerDisplay,nq}")]
		internal sealed class Node : KeyedList<T, Score>, INode
		{
			string DebuggerDisplay => $"{Keyword}, Count = {Count}";

			public string Keyword { get; }

			public Node(string keyword)
			{
				Keyword = keyword;
			}

			public Node(string keyword, T item, Score score)
			{
				Keyword = keyword;
				Add(item, score, DefaultKeyComparer);
			}

			/// <summary>
			/// Adds 
			/// </summary>
			/// <param name="item"></param>
			/// <param name="score"></param>
			/// <param name="comparer"></param>
			public void Add(T item, Score score, IComparer<T> comparer)
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
					var compareLast = comparer.Compare(Keys[last], item);
					if (compareLast == 0)
					{
						// Replace the last value.
						Values[last] = Math.Max(score.Byte, Values[last].Byte);
						return;
					}
					else if (compareLast > 0)
					{
						// Append at the end.
						insertBefore = last + 1;
					}
					else
					{
						if (TryFindIndex(item, out var i, comparer))
						{
							// Binary search found key. Replace value.
							Values[i] = Math.Max(score.Byte, Values[i].Byte);
							return;
						}
						else
						{
							insertBefore = ~i;
						}
					}
				}

				Keys.Insert(insertBefore, item);
				Values.Insert(insertBefore, score);
			}

			public int CompareTo(INode other) => string.CompareOrdinal(Keyword, other.Keyword);
			public override bool Equals(object obj) => Equals(obj as INode);
			public bool Equals(INode? other) => other?.Keyword == Keyword;
			public override int GetHashCode() => Keyword.GetHashCode();
			public override string ToString() => Keyword;

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
			public new IEnumerator<(T Item, Score Score)> GetEnumerator()
			{
				for (var i = 0; i < Keys.Count; i++)
				{
					yield return (Keys[i], Values[i]);
				}
			}
		}
#pragma warning restore CS8714 // Node list should never contain null.
	}
}
