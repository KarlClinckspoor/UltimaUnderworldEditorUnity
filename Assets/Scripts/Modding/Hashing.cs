using System;
using System.Security.Cryptography;
using System.Text;

namespace UltimateEditor.Modding
{
	/// <summary>Content hashes as written in mod.json: "sha256:" and 64 lower-case hex digits.</summary>
	public static class Hashing
	{
		public const string Absent = "absent";

		public static string Sha256(byte[] data)
		{
			if (data == null) return Absent;
			using (var h = SHA256.Create())
				return "sha256:" + Hex(h.ComputeHash(data));
		}

		public static string Sha256(byte[] data, int offset, int count)
		{
			using (var h = SHA256.Create())
				return "sha256:" + Hex(h.ComputeHash(data, offset, count));
		}

		public static string Sha256(string text) { return text == null ? Absent : Sha256(Encoding.UTF8.GetBytes(text)); }

		public static string Hex(byte[] b)
		{
			if (b == null) return "";
			var sb = new StringBuilder(b.Length * 2);
			foreach (var x in b) sb.Append(x.ToString("x2"));
			return sb.ToString();
		}

		public static string Hex(byte[] b, int offset, int count)
		{
			var sb = new StringBuilder(count * 2);
			for (int i = 0; i < count; i++) sb.Append(b[offset + i].ToString("x2"));
			return sb.ToString();
		}

		public static byte[] FromHex(string s)
		{
			if (string.IsNullOrEmpty(s)) return new byte[0];
			if (s.Length % 2 != 0) throw new FormatException("odd number of hex digits");
			var r = new byte[s.Length / 2];
			for (int i = 0; i < r.Length; i++) r[i] = Convert.ToByte(s.Substring(2 * i, 2), 16);
			return r;
		}

		static readonly uint[] crcTable = MakeCrcTable();

		static uint[] MakeCrcTable()
		{
			var t = new uint[256];
			for (uint n = 0; n < 256; n++)
			{
				uint c = n;
				for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
				t[n] = c;
			}
			return t;
		}

		/// <summary>CRC-32 (zip, PNG and Exhumed's check of the executable).</summary>
		public static uint Crc32(byte[] data, int offset, int count)
		{
			uint c = 0xFFFFFFFFu;
			for (int i = 0; i < count; i++) c = crcTable[(c ^ data[offset + i]) & 0xFF] ^ (c >> 8);
			return c ^ 0xFFFFFFFFu;
		}

		public static uint Crc32(byte[] data) { return Crc32(data, 0, data.Length); }
	}
}
