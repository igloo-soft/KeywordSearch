using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace KeywordSearch
{
	public static class KeywordSearch
	{
		public static readonly Regex RegexTokenizeCamelCase = new(@"(\p{Lu}+(?!\p{Ll})|\p{Lu}\p{Ll}+|\p{Ll}+|[0-9]+[,\.][0-9]+|[0-9]+)",
				RegexOptions.CultureInvariant | RegexOptions.Compiled);

		/// <summary>
		/// Tokenizes latin words, decimal numbers and CamelCaseString into individual tokens.
		/// See <see cref="RegexTokenizeCamelCase"/>.
		/// </summary>
		public static readonly Func<string, IEnumerable<string>> CamelCaseTokenizer
				= s => RegexTokenizeCamelCase.Matches(s).Cast<Match>().Select(x => x.Value);
	}

	/// <summary>
	/// A keyword search index optimised for interactive search while the user is typing.
	/// <para/>
	/// Example:
	/// <code>
	/// var index = new KeywordSearch&lt;KnownColor&gt;();
	/// index.AddPhrases(Enum.GetValues&lt;KnownColor&gt;().Select(color => (color, color.ToString())));
	///	var result = index.Search( "gol" );
	///	// result: { Gold, Goldenrod, DarkGoldenrod, LightGoldenrodYellow, PaleGoldenrod }
	/// </code>
	/// </summary>
	/// <typeparam name="T">Type of indexed item.</typeparam>
	public partial class KeywordSearch<T>
	{
		readonly List<INode> Nodes;
		readonly IEqualityComparer<T> ItemEqualityComparer;
		readonly IComparer<T> NodeItemComparer;
		double AverageKeywordLengthInverse = double.PositiveInfinity;

		/// <summary>
		/// Number of indexed keywords.
		/// Each keyword may associate with multiple <typeparamref name="T"/> items with different scores.
		/// </summary>
		public int Count => Nodes.Count;

		/// <summary>
		/// The default tokenizer for <see cref="AddPhrases"/>.
		/// Default is <see cref="KeywordSearch.CamelCaseTokenizer"/>.
		/// </summary>
		public Func<string, IEnumerable<string>> PhraseTokenizer { get; set; } = KeywordSearch.CamelCaseTokenizer;

		/// <summary>
		/// The default tokenizer for <see cref="Search"/>.
		/// Default is <see cref="KeywordSearch.CamelCaseTokenizer"/>.
		/// </summary>
		public Func<string, IEnumerable<string>> SearchWordTokenizer { get; set; } = KeywordSearch.CamelCaseTokenizer;

		/// <summary>
		/// Creates an empty keyword search index.
		/// </summary>
		public KeywordSearch()
		{
			Nodes = new List<INode>();
			ItemEqualityComparer = EqualityComparer<T>.Default;
			NodeItemComparer = Node.DefaultKeyComparer;
		}

		/// <summary>
		/// Adds the items and their associated tags to this index.
		/// </summary>
		/// <param name="inputs"></param>
		/// <param name="score">Score for the added tags.</param>
		public void AddTags(IEnumerable<(T Item, IEnumerable<string> Keywords)> inputs, double score = 1.0)
				=> AddKeywords(inputs.Select(t => (t.Item, t.Keywords.Select(k => (k, score)))));

		/// <summary>
		/// For each items, adds enumerated keywords with individual score to the index.
		/// If score exists for a particular entry (item and keyword), the higher score will be kept.
		/// </summary>
		/// <param name="inputs"></param>
		public void AddKeywords(IEnumerable<(T Item, IEnumerable<(string Keyword, double Score)> Keywords)> inputs)
		{
			var newNodes = new Dictionary<string, Node>(StringComparer.Ordinal);
			var lengthSum = Nodes.Count / AverageKeywordLengthInverse;

			foreach (var (item, keywords) in inputs)
			{
				foreach (var (keyword, score) in keywords)
				{
					var byteScore = Score.FromDouble(score);
					var normalizedKeyword = keyword.ToUpperInvariant();
					if (newNodes.TryGetValue(normalizedKeyword, out var node))
					{
						node.Add(item, byteScore, NodeItemComparer);
					}
					else if (!TryAddToNode(normalizedKeyword, item, byteScore, NodeItemComparer))
					{
						// Node/keyword not exist. Add it to newNodes.
						node = new Node(normalizedKeyword, item, byteScore);
						newNodes.Add(normalizedKeyword, node);
						lengthSum += keyword.Length;
					}
				}
			}

			Nodes.AddRange(newNodes.Values);
			AverageKeywordLengthInverse = Nodes.Count / lengthSum;

			// List/Array.Sort() uses introsort internally,
			// which should work reasonably well when appending (i.e. partially sorted).
			Nodes.Sort();
		}

		/// <summary>
		/// Adds phrases to this index in a batch.
		/// A phrase will be tokenized into individual keywords with descending scores following the word order.
		/// </summary>
		/// <param name="inputs"></param>
		/// <param name="wordOrderPenalty">
		/// Descending score following the word order when this is &gt;0.
		/// Expects a non-negative number.
		/// </param>
		/// <param name="tokenizer">
		/// Tokenizes the input phrase into keywords.
		/// If null, <see cref="PhraseTokenizer"/> will be used.
		/// </param>
		public void AddPhrases(IEnumerable<(T Item, string Phrase)> inputs,
			double wordOrderPenalty = 1.0,
			Func<string, IEnumerable<string>>? tokenizer = null)
			=> AddKeywords(inputs.Select(t => (
				Items: t.Item,
				Keywords: (tokenizer ?? KeywordSearch.CamelCaseTokenizer)
					.Invoke(t.Phrase)
					.Select((keyword, order) => (
						Keyword: keyword,
						Score: 1.0 / Math.Pow(order + 1, wordOrderPenalty)
					))
			)));

		/// <summary>
		/// Tries finding the node of <paramref name="keyword"/>,
		/// and add <paramref name="item"/> with <paramref name="score"/> to it.
		/// </summary>
		/// <param name="keyword"></param>
		/// <param name="item"></param>
		/// <param name="score"></param>
		/// <param name="comparer"></param>
		/// <returns>Whether <paramref name="keyword"/> exists in <see cref="Nodes"/>.</returns>
		private bool TryAddToNode(string keyword, T item, Score score, IComparer<T> comparer)
		{
			var count = Nodes.Count;
			if (count > 0)
			{
				var lastNode = Nodes[count - 1];
				var diff = string.CompareOrdinal(keyword, lastNode.Keyword);

				if (diff == 0)
				{
					lastNode.Add(item, score, comparer);
					return true;
				}
				else if (diff < 0)
				{
					var i = BinarySearch(keyword);
					if (i >= 0)
					{
						lastNode.Add(item, score, comparer);
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Releases unused capacity reserved for new entries.
		/// Then performs <see cref="GC.Collect"/>.
		/// </summary>
		/// <param name="skipGCCollect">If true, skip <see cref="GC.Collect"/>.</param>
		public void Compact(bool skipGCCollect = false)
		{
			for (var i = 0; i < Nodes.Count; i++)
			{
				Nodes[i].Compact();
			}

			if (!skipGCCollect) { GC.Collect(); }
		}

		/// <summary>
		/// Tokenizes <paramref name="searchWords"/> and finds <typeparamref name="T"/> items with the matching keywords.
		/// </summary>
		/// <param name="searchWords"></param>
		/// <param name="minSubstringMatchLength"></param>
		/// <param name="tokenizer"></param>
		/// <returns>Matched <typeparamref name="T"/> items in descending score.</returns>
		public IEnumerable<(T Item, double Score)> Search(string searchWords, int minSubstringMatchLength = 1, Func<string, IEnumerable<string>>? tokenizer = null)
		{
			if (!string.IsNullOrWhiteSpace(searchWords))
			{
				var tokens = (tokenizer ?? SearchWordTokenizer).Invoke(searchWords).ToList();
				var matches = tokens.Select(s => Match(s, minSubstringMatchLength));

				Dictionary<T, double>? scores = null;
				foreach (var match in matches.OrderBy(m => m.Count))
				{
					if (match.Count == 0) { break; }

					if (scores is null)
					{
						scores = match;
					}
					else
					{
						scores = scores.Keys
							.Intersect(match.Keys, ItemEqualityComparer)
							.ToDictionary(o => o, o => scores[o] + match[o]);
					}
				}

				if (scores is not null)
				{
					return scores
						.OrderByDescending(kvp => kvp.Value)
						.ThenBy(kvp => kvp.Key)
						.Select(kvp => (kvp.Key, kvp.Value));
				}
			}

			return Enumerable.Empty<(T Item, double Score)>();
		}

		private Dictionary<T, double> Match(string token, int minSubstringMatchLength)
		{
			token = token.ToUpperInvariant();

			var scores = new Dictionary<T, double>(ItemEqualityComparer);
			var i = BinarySearch(token);
			if (i >= 0)
			{
				var node = Nodes[i];
				foreach (var (o, score) in Nodes[i])
				{
					scores.Add(o, score.Value / node.Count);
				}
				i++;
			}
			else
			{
				i = ~i;
			}

			if (token.Length < minSubstringMatchLength) { return scores; }

			while (i < Nodes.Count)
			{
				var node = Nodes[i];
				if (node.Keyword.StartsWith(token))
				{
					var tokenScore = Math.Min(AverageKeywordLengthInverse * token.Length, 0.9);
					foreach (var (o, score) in node)
					{
						scores.TryGetValue(o, out var oldScore);
						scores[o] = oldScore + score.Value * tokenScore / node.Count;
					}
				}
				else
				{
					break;
				}
				i++;
			}

			return scores;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int BinarySearch(string token)
			=> Nodes.BinarySearch(new EmptyNode(token));
	}
}
