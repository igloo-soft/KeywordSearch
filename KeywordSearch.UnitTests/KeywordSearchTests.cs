using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;

namespace KeywordSearch.UnitTests
{
	[TestClass]
	public class KeywordSearchTests
	{
		[TestMethod]
		public void Example()
		{
			var index = new KeywordSearch<KnownColor>();
			index.AddPhrases(Enum.GetValues<KnownColor>().Select(color => (color, color.ToString())));
			var result = index.Search("gol");
			// result: { Gold, Goldenrod, DarkGoldenrod, LightGoldenrodYellow, PaleGoldenrod }

			var expected = new KnownColor[]
			{
				KnownColor.Gold,
				KnownColor.Goldenrod,
				KnownColor.DarkGoldenrod,
				KnownColor.LightGoldenrodYellow,
				KnownColor.PaleGoldenrod,
			};
			Assert.IsTrue(expected.SequenceEqual(result.Select(t => t.Item)));
		}

		readonly static IEnumerable<(KnownColor, string)> KnownColors = Enum.GetValues<KnownColor>().SelectMany(s => new[]
		{
			(s, s.ToString()),
			(s, s switch
			{
				<= KnownColor.Window or
				(>= KnownColor.ButtonFace and <= KnownColor.MenuHighlight) => "Window",
				_ => "Natural"
			})
		}).ToList();

		static KeywordSearch<KnownColor> CreateKnownColorIndex()
		{
			var index = new KeywordSearch<KnownColor>();
			var sw = Stopwatch.StartNew();
			index.AddPhrases(KnownColors);
			sw.Stop();
			Console.WriteLine($"KeywordSearch.Load() took {sw.ElapsedMilliseconds} ms.");
			return index;
		}

		static KeywordSearch<KnownColor> KnownColorIndex
			=> _KnownColorIndex ??= CreateKnownColorIndex();
		static KeywordSearch<KnownColor>? _KnownColorIndex;

		[TestMethod]
		public void Create()
		{
			_KnownColorIndex = CreateKnownColorIndex();
			Assert.IsNotNull(_KnownColorIndex);
		}

		[DataTestMethod]
		[DataRow("golden")]
		[DataRow("gray")]
		[DataRow("blue")]
		public void Search(string words)
		{
			var index = KnownColorIndex;
			for (var i = 0; i <= words.Length; i++)
			{
				var searchText = words.Substring(0, i);
				var sw = Stopwatch.StartNew();
				var results = index.Search(searchText).ToArray();
				sw.Stop();

				Console.WriteLine($"\nKeywordSearch.Search(\"{searchText}\") took {sw.ElapsedMilliseconds} ms.");
				foreach (var result in results)
				{
					Console.WriteLine(result);
				}
			}
		}

		[TestMethod]
		public async Task CreateLargeIndex()
		{
			var index = new KeywordSearch<(int Start, int End)>();
			var text = await TextCorpus.AM65x;
			GC.Collect();

			long mem = 0;
			GetMemDifferenceMB(ref mem);
			static double GetMemDifferenceMB(ref long mem)
			{
				var current = Process.GetCurrentProcess().PrivateMemorySize64;
				var mb = (current - mem) / 1048576.0;
				mem = current;
				return mb;
			}

			var baseMem = mem;
			Console.WriteLine($"Base RAM = {baseMem / 1048576.0:#0.0} [MB]");
			Console.WriteLine($"Duration [ms]\tRAM [MB]\t- Base [MB]\tDelta [MB]\tMilestone");
			var sw = Stopwatch.StartNew();
			void SetMilestone(string milestone)
			{
				sw!.Stop();
				var mb = GetMemDifferenceMB(ref mem);

				Console.WriteLine(string.Format("{0,8:#0.0}\t{1,8:#0.0}\t{2,8:#0.0}\t{3,8:#0.0}\t{4}",
					sw.Elapsed.TotalMilliseconds,
					mem / 1048576.0,
					(mem - baseMem) / 1048576.0,
					mb,
					milestone
				));

				sw.Restart();
			}

			index.AddPhrases(Tokenize(text));
			SetMilestone($"{nameof(index.AddPhrases)}(AM65x) // 27 MB .txt");

			index.Compact();
			SetMilestone($"{nameof(index.Compact)}()");

			index.Search("gpmc");
			SetMilestone($"Search(\"gpmc\")");
		}

		public IEnumerable<((int Start, int End), string Token)> Tokenize(string text)
		{
			foreach (Match m in KeywordSearch.RegexTokenizeCamelCase.Matches(text))
			{
				var start = m.Groups[0].Index;
				yield return ((start, start + m.Length), m.Value);
			}
		}
	}
}
