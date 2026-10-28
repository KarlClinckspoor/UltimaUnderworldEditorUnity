using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace UltimateEditor.Modding
{
	/// <summary>
	/// A small zip reader and writer (PKWARE APPNOTE: local headers, central directory, stored or
	/// deflated entries, no zip64, no encryption). .uwmod packages are zip files. This avoids
	/// System.IO.Compression.ZipArchive, which Unity 2020's .NET 4.x profile does not reference
	/// by default; DeflateStream is in System.dll there. Names use '/' and UTF-8.
	/// </summary>
	public static class MiniZip
	{
		public sealed class Entry
		{
			public string Name;
			public byte[] Data;
			public Entry(string name, byte[] data) { Name = name; Data = data; }
		}

		const uint LocalSig = 0x04034B50, CentralSig = 0x02014B50, EndSig = 0x06054B50;

		public static byte[] Write(IEnumerable<Entry> entries)
		{
			var ms = new MemoryStream();
			var w = new BinaryWriter(ms);
			var central = new MemoryStream();
			var cw = new BinaryWriter(central);
			int count = 0;
			// A fixed time stamp (1 Jan 2020): the same package content gives the same bytes
			ushort dosTime = 0, dosDate = (ushort)(((2020 - 1980) << 9) | (1 << 5) | 1);
			var names = new HashSet<string>(StringComparer.Ordinal);
			foreach (var e in entries)
			{
				if (!names.Add(e.Name)) throw new ArgumentException("duplicate zip entry " + e.Name);
				byte[] name = Encoding.UTF8.GetBytes(e.Name);
				byte[] data = e.Data ?? new byte[0];
				uint crc = Hashing.Crc32(data);
				byte[] packed = Deflate(data);
				bool deflated = packed.Length < data.Length;
				byte[] stored = deflated ? packed : data;
				uint at = (uint)ms.Position;
				w.Write(LocalSig); w.Write((ushort)20); w.Write((ushort)0x0800); w.Write((ushort)(deflated ? 8 : 0));
				w.Write(dosTime); w.Write(dosDate); w.Write(crc); w.Write((uint)stored.Length); w.Write((uint)data.Length);
				w.Write((ushort)name.Length); w.Write((ushort)0); w.Write(name); w.Write(stored);
				cw.Write(CentralSig); cw.Write((ushort)20); cw.Write((ushort)20); cw.Write((ushort)0x0800); cw.Write((ushort)(deflated ? 8 : 0));
				cw.Write(dosTime); cw.Write(dosDate); cw.Write(crc); cw.Write((uint)stored.Length); cw.Write((uint)data.Length);
				cw.Write((ushort)name.Length); cw.Write((ushort)0); cw.Write((ushort)0); cw.Write((ushort)0); cw.Write((ushort)0);
				cw.Write((uint)0); cw.Write(at); cw.Write(name);
				count++;
			}
			if (count > 0xFFFF || ms.Length > 0xFFFFFFF0L) throw new IOException("too large for a zip without zip64");
			uint cdAt = (uint)ms.Position;
			byte[] cd = central.ToArray();
			w.Write(cd);
			w.Write(EndSig); w.Write((ushort)0); w.Write((ushort)0); w.Write((ushort)count); w.Write((ushort)count);
			w.Write((uint)cd.Length); w.Write(cdAt); w.Write((ushort)0);
			w.Flush();
			return ms.ToArray();
		}

		public static List<Entry> Read(byte[] zip)
		{
			int end = -1;
			for (int i = zip.Length - 22; i >= Math.Max(0, zip.Length - 22 - 0xFFFF); i--)
				if (U32(zip, i) == EndSig) { end = i; break; }
			if (end < 0) throw new InvalidDataException("not a zip file (no end of central directory)");
			int count = U16(zip, end + 10);
			int p = (int)U32(zip, end + 16);
			var list = new List<Entry>(count);
			for (int k = 0; k < count; k++)
			{
				if (U32(zip, p) != CentralSig) throw new InvalidDataException("bad central directory entry");
				int flags = U16(zip, p + 8), method = U16(zip, p + 10);
				uint crc = U32(zip, p + 16);
				int csize = (int)U32(zip, p + 20), usize = (int)U32(zip, p + 24);
				int nlen = U16(zip, p + 28), xlen = U16(zip, p + 30), clen = U16(zip, p + 32);
				int local = (int)U32(zip, p + 42);
				string name = ((flags & 0x800) != 0 ? Encoding.UTF8 : Encoding.GetEncoding(28591)).GetString(zip, p + 46, nlen);
				p += 46 + nlen + xlen + clen;
				if ((flags & 1) != 0) throw new InvalidDataException(name + ": encrypted zip entries are not supported");
				if (U32(zip, local) != LocalSig) throw new InvalidDataException(name + ": bad local header");
				int dataAt = local + 30 + U16(zip, local + 26) + U16(zip, local + 28);
				byte[] data;
				if (method == 0) { data = new byte[usize]; Buffer.BlockCopy(zip, dataAt, data, 0, usize); }
				else if (method == 8) data = Inflate(zip, dataAt, csize, usize);
				else throw new InvalidDataException(name + ": compression method " + method + " is not supported");
				if (Hashing.Crc32(data) != crc) throw new InvalidDataException(name + ": CRC mismatch");
				if (name.EndsWith("/")) continue;   // a folder
				list.Add(new Entry(name, data));
			}
			return list;
		}

		static byte[] Deflate(byte[] data)
		{
			var ms = new MemoryStream();
			using (var d = new DeflateStream(ms, CompressionMode.Compress, true)) d.Write(data, 0, data.Length);
			return ms.ToArray();
		}

		static byte[] Inflate(byte[] src, int at, int csize, int usize)
		{
			var r = new byte[usize];
			using (var d = new DeflateStream(new MemoryStream(src, at, csize, false), CompressionMode.Decompress))
			{
				int got = 0;
				while (got < usize)
				{
					int n = d.Read(r, got, usize - got);
					if (n <= 0) throw new InvalidDataException("zip entry shorter than its size");
					got += n;
				}
			}
			return r;
		}

		static int U16(byte[] b, int p) { return b[p] | b[p + 1] << 8; }
		static uint U32(byte[] b, int p) { return (uint)(b[p] | b[p + 1] << 8 | b[p + 2] << 16 | b[p + 3] << 24); }
	}
}
