using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordSearch
{
	/// <summary>
	/// Encodes (0, 1] to 0x00..0xFF.
	/// i.e. 1 = 0xFF and 0.5 = 0x7F.
	/// </summary>
	internal readonly struct Score
	{
		const double Factor = 1.0 / 256;

		public readonly byte Byte;

		public Score(byte score)
		{
			Byte = score;
		}

		public static Score FromDouble(double score)
		{
			var byteScore = Math.Ceiling(score * 256) - 1;
			return byteScore > 255 ? (byte)255
				: byteScore >= 0 ? (byte)byteScore
				: default;
		}

		public double Value => (Byte + 1.0) * Factor;

		public static Score Min(Score a, Score b) => Math.Min(a.Byte, b.Byte);
		public static Score Max(Score a, Score b) => Math.Max(a.Byte, b.Byte);

		public static implicit operator Score(byte score) => new(score);

		public override string ToString() => Value.ToString("F3");
	}
}
