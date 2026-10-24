using System;
using System.Collections.Generic;
using System.IO;

namespace UltimateEditor.Games
{
	/// <summary>
	/// An .ARK archive held in memory as the file's bytes. Reading gives a block's data
	/// (decompressed for UW2); writing changes only that block, the way the game's own
	/// put_arc does, so an archive whose blocks were not written saves byte for byte as it was.
	/// </summary>
	public abstract class Archive
	{
		protected byte[] file;

		public abstract int Count { get; }
		public abstract bool Has(int block);
		/// <summary>The block's data, decompressed; null if absent.</summary>
		public abstract byte[] Read(int block);
		/// <summary>Writes a block's data as the game would (compressing per its flags for UW2).</summary>
		public abstract void Write(int block, byte[] data);
		/// <summary>Removes a block: its bytes leave the file, later blocks move down, its offset becomes 0.</summary>
		public abstract void Delete(int block);

		public byte[] ToArray() { return (byte[])file.Clone(); }
		public void Save(string path) { File.WriteAllBytes(path, file); }

		/// <summary>Opens either format: UW2 when the long after the count is 0 and the four tables fit, else UW1.</summary>
		public static Archive Open(byte[] data, GameKind kind)
		{
			return kind == GameKind.UW2 ? (Archive)new Uw2Archive(data) : new Uw1Archive(data);
		}

		public static Archive Load(string path, GameKind kind) { return Open(File.ReadAllBytes(path), kind); }

		protected static uint U32(byte[] b, int o) { return (uint)(b[o] | b[o + 1] << 8 | b[o + 2] << 16 | b[o + 3] << 24); }
		protected static void PutU32(byte[] b, int o, uint v) { b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); b[o + 2] = (byte)(v >> 16); b[o + 3] = (byte)(v >> 24); }
		protected static int U16(byte[] b, int o) { return b[o] | b[o + 1] << 8; }
	}

	/// <summary>
	/// UW1: a word n, then n long offsets; no lengths, flags or compression. A block runs to
	/// the smallest greater offset or the end of the file (uw1/src/sys/ARC.C). Writing a
	/// block of the same length overwrites it; a new one is appended; one whose length
	/// changed is removed and appended at the end.
	/// </summary>
	public sealed class Uw1Archive : Archive
	{
		public Uw1Archive(byte[] data) { file = (byte[])data.Clone(); }

		public override int Count { get { return U16(file, 0); } }
		public uint Offset(int b) { return U32(file, 2 + 4 * b); }
		public override bool Has(int b) { return b >= 0 && b < Count && Offset(b) != 0; }

		int Length(int b)
		{
			uint off = Offset(b);
			if (off == 0) return 0;
			uint end = (uint)file.Length;
			for (int i = 0; i < Count; i++)
			{
				uint o = Offset(i);
				if (o > off && o < end) end = o;
			}
			return (int)(end - off);
		}

		public override byte[] Read(int b)
		{
			if (!Has(b)) return null;
			var d = new byte[Length(b)];
			Array.Copy(file, Offset(b), d, 0, d.Length);
			return d;
		}

		public override void Write(int b, byte[] data)
		{
			if (b < 0 || b >= Count) throw new ArgumentOutOfRangeException("block");
			uint off = Offset(b);
			if (off != 0 && Length(b) == data.Length)
			{
				Array.Copy(data, 0, file, off, data.Length);
				return;
			}
			var list = new List<byte>(file);
			if (off != 0)
			{
				int len = Length(b);
				var olds = new uint[Count];
				for (int i = 0; i < Count; i++) olds[i] = Offset(i);
				list.RemoveRange((int)off, len);
				for (int i = 0; i < Count; i++)
					if (olds[i] > off) PutAt(list, 2 + 4 * i, olds[i] - (uint)len);
			}
			uint at = (uint)list.Count;
			list.AddRange(data);
			PutAt(list, 2 + 4 * b, at);
			file = list.ToArray();
		}

		public override void Delete(int b)
		{
			if (!Has(b)) return;
			uint off = Offset(b);
			int len = Length(b);
			var olds = new uint[Count];
			for (int i = 0; i < Count; i++) olds[i] = Offset(i);
			var list = new List<byte>(file);
			list.RemoveRange((int)off, len);
			for (int i = 0; i < Count; i++)
				if (olds[i] > off) PutAt(list, 2 + 4 * i, olds[i] - (uint)len);
			PutAt(list, 2 + 4 * b, 0);
			file = list.ToArray();
		}

		static void PutAt(List<byte> l, int o, uint v) { l[o] = (byte)v; l[o + 1] = (byte)(v >> 8); l[o + 2] = (byte)(v >> 16); l[o + 3] = (byte)(v >> 24); }
	}

	/// <summary>
	/// UW2: word n, a long (0), then four tables of n longs: offsets, flags (bit 0 compress
	/// when writing, bit 1 stored compressed, bit 2 room reserved), stored lengths, room.
	/// Writing follows put_arc (uw2/src/sys/ARC.C:203) exactly: compress if flagged and it
	/// helps; write in place if it fits the room (or equals it without bit 2); append a new
	/// block with 15% zero room when bit 2 is set; otherwise remove the old copy, move the
	/// later offsets down by the old room, and append.
	/// </summary>
	public sealed class Uw2Archive : Archive
	{
		public Uw2Archive(byte[] data) { file = (byte[])data.Clone(); }

		public override int Count { get { return U16(file, 0); } }
		int OffPos(int b) { return 6 + 4 * b; }
		int FlagPos(int b) { return 6 + 4 * Count + 4 * b; }
		int LenPos(int b) { return 6 + 8 * Count + 4 * b; }
		int RoomPos(int b) { return 6 + 12 * Count + 4 * b; }

		public uint Offset(int b) { return U32(file, OffPos(b)); }
		public uint Flags(int b) { return U32(file, FlagPos(b)); }
		public uint StoredLength(int b) { return U32(file, LenPos(b)); }
		public uint Room(int b) { return U32(file, RoomPos(b)); }
		public bool Compressed(int b) { return (Flags(b) & 2) != 0; }

		public override bool Has(int b) { return b >= 0 && b < Count && Offset(b) != 0; }

		public byte[] ReadStored(int b)
		{
			if (!Has(b)) return null;
			var d = new byte[StoredLength(b)];
			Array.Copy(file, Offset(b), d, 0, d.Length);
			return d;
		}

		public override byte[] Read(int b)
		{
			var stored = ReadStored(b);
			if (stored == null) return null;
			return Compressed(b) ? Lzss.DecodeBlock(stored) : stored;
		}

		/// <summary>The bytes the game would store for this data under the block's flags, and whether compressed.</summary>
		public byte[] Encode(int b, byte[] data, out bool compressed)
		{
			compressed = false;
			if ((Flags(b) & 1) != 0)
			{
				var c = Lzss.EncodeBlock(data);
				if (c != null) { compressed = true; return c; }
			}
			return data;
		}

		public override void Write(int b, byte[] data)
		{
			if (b < 0 || b >= Count) throw new ArgumentOutOfRangeException("block");
			uint off = Offset(b), flags = Flags(b), room = Room(b);
			bool extra = (flags & 4) != 0;
			bool compressed;
			var stored = Encode(b, data, out compressed);
			uint size = (uint)stored.Length;
			uint newFlags = compressed ? flags | 2 : flags & ~2u;
			PutU32(file, LenPos(b), size);
			PutU32(file, FlagPos(b), newFlags);
			if (size == 0) return;
			if ((extra && size <= room) || size == room)
			{
				Array.Copy(stored, 0, file, off, stored.Length);
				return;
			}
			if (off == 0)
			{
				uint pos = (uint)file.Length;
				uint pad = extra ? 15 * size / 100 : 0;
				var grown = new byte[file.Length + size + pad];
				Array.Copy(file, grown, file.Length);
				Array.Copy(stored, 0, grown, pos, size);
				file = grown;
				PutU32(file, OffPos(b), pos);
				PutU32(file, RoomPos(b), size + pad);
				return;
			}
			// grown: copy without the old room, append, move later offsets down
			uint pad2 = extra ? 15 * size / 100 : 0;
			var ms = new MemoryStream();
			ms.Write(file, 0, (int)off);
			long restStart = off + room;
			if (restStart < file.Length) ms.Write(file, (int)restStart, (int)(file.Length - restStart));
			uint writePos = (uint)ms.Length;
			ms.Write(stored, 0, stored.Length);
			ms.Write(new byte[pad2], 0, (int)pad2);
			var nf = ms.ToArray();
			int n = Count;
			for (int i = 0; i < n; i++)
			{
				uint o = U32(nf, 6 + 4 * i);
				if (o > 0 && o > off) PutU32(nf, 6 + 4 * i, o - room);
			}
			PutU32(nf, 6 + 4 * b, writePos);
			PutU32(nf, 6 + 12 * n + 4 * b, size + pad2);
			file = nf;
		}

		/// <summary>Removes a block and its room; later offsets move down; offset, length and room become 0 (flags stay).</summary>
		public override void Delete(int b)
		{
			if (!Has(b)) return;
			uint off = Offset(b), room = Math.Max(Room(b), StoredLength(b));
			if (off + room > file.Length) room = (uint)file.Length - off;
			var nf = new byte[file.Length - room];
			Array.Copy(file, 0, nf, 0, off);
			Array.Copy(file, off + room, nf, off, file.Length - off - room);
			int n = Count;
			for (int i = 0; i < n; i++)
			{
				uint o = U32(nf, 6 + 4 * i);
				if (o > off) PutU32(nf, 6 + 4 * i, o - room);
			}
			PutU32(nf, 6 + 4 * b, 0);
			PutU32(nf, 6 + 4 * n + 4 * b, U32(nf, 6 + 4 * n + 4 * b) & ~2u);   // nothing stored, so not compressed
			PutU32(nf, 6 + 8 * n + 4 * b, 0);
			PutU32(nf, 6 + 12 * n + 4 * b, 0);
			file = nf;
		}

		/// <summary>A new, empty UW2 archive of n blocks with the given flags for every block.</summary>
		public static Uw2Archive Empty(int n, uint flags)
		{
			var b = new byte[6 + 16 * n];
			b[0] = (byte)n; b[1] = (byte)(n >> 8);
			for (int i = 0; i < n; i++) PutU32(b, 6 + 4 * n + 4 * i, flags);
			return new Uw2Archive(b);
		}
	}
}
