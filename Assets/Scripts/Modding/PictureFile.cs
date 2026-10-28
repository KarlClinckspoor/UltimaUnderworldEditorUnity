using System;
using System.Collections.Generic;
using System.IO;

namespace UltimateEditor.Modding
{
	/// <summary>
	/// A .GR or .TR picture file as a list of records: a type byte (1 .GR, 2 .TR), for .TR a
	/// size byte, a word count n, n + 1 doubleword offsets, then the pictures one after another
	/// (the last offset is the end of the file). Every shipped file of both games is laid out
	/// this way, so rebuilding an unchanged file gives the same bytes.
	/// </summary>
	public sealed class PictureFile
	{
		public byte Type;
		/// <summary>.TR's picture size byte, or -1.</summary>
		public int SizeByte = -1;
		public List<byte[]> Records = new List<byte[]>();

		int HeaderLength { get { return (SizeByte >= 0 ? 2 : 1) + 2 + 4 * (Records.Count + 1); } }

		public static PictureFile TryParse(byte[] f)
		{
			if (f == null || f.Length < 3 || (f[0] != 1 && f[0] != 2)) return null;
			var p = new PictureFile { Type = f[0] };
			int at = 1;
			if (f[0] == 2) { p.SizeByte = f[1]; at = 2; }
			if (at + 2 > f.Length) return null;
			int n = f[at] | f[at + 1] << 8;
			at += 2;
			if (at + 4L * (n + 1) > f.Length) return null;
			var o = new int[n + 1];
			for (int i = 0; i <= n; i++) o[i] = f[at + 4 * i] | f[at + 4 * i + 1] << 8 | f[at + 4 * i + 2] << 16 | f[at + 4 * i + 3] << 24;
			if (o[0] != at + 4 * (n + 1) || o[n] != f.Length) return null;
			for (int i = 0; i < n; i++)
			{
				if (o[i + 1] < o[i]) return null;
				var r = new byte[o[i + 1] - o[i]];
				Buffer.BlockCopy(f, o[i], r, 0, r.Length);
				p.Records.Add(r);
			}
			return p;
		}

		public byte[] Build()
		{
			var ms = new MemoryStream();
			ms.WriteByte(Type);
			if (SizeByte >= 0) ms.WriteByte((byte)SizeByte);
			int n = Records.Count;
			ms.WriteByte((byte)n); ms.WriteByte((byte)(n >> 8));
			int at = HeaderLength;
			for (int i = 0; i <= n; i++)
			{
				ms.WriteByte((byte)at); ms.WriteByte((byte)(at >> 8)); ms.WriteByte((byte)(at >> 16)); ms.WriteByte((byte)(at >> 24));
				if (i < n) at += Records[i].Length;
			}
			foreach (var r in Records) ms.Write(r, 0, r.Length);
			return ms.ToArray();
		}
	}
}
