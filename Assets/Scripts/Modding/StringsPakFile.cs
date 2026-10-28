using System;
using System.Collections.Generic;
using System.IO;
using UltimateEditor.Games;

namespace UltimateEditor.Modding
{
	/// <summary>
	/// STRINGS.PAK as blocks of decoded strings, kept with their original bytes, so a mod can
	/// change some strings and leave every other block byte for byte as it was. Decoding and
	/// encoding are the editor's (StringsPak, Formats assembly).
	/// </summary>
	public sealed class StringsPakFile
	{
		public sealed class Block
		{
			public int Number;
			public List<string> Strings;
			/// <summary>The block's bytes in the original file (count, offsets, bit stream, and any
			/// bytes up to the next block).</summary>
			public byte[] Raw;
		}

		public HuffmanNode[] Nodes;
		/// <summary>The node table's bytes (count word and four bytes a node).</summary>
		public byte[] NodeTable;
		/// <summary>In block-table order.</summary>
		public List<Block> Blocks = new List<Block>();

		public Block Find(int number) { return Blocks.Find(b => b.Number == number); }

		public static StringsPakFile Parse(byte[] file)
		{
			var p = new StringsPakFile();
			var ms = new MemoryStream(file, false);
			var nodes = StringsPak.ReadHuffmanNodes(ms);
			p.Nodes = nodes.Item1;
			int tableStart = (int)nodes.Item2;
			p.NodeTable = new byte[tableStart];
			Buffer.BlockCopy(file, 0, p.NodeTable, 0, tableStart);
			var blocks = StringsPak.ReadStringBlocks(ms, tableStart).Item1;
			var starts = new List<long>();
			foreach (var b in blocks) starts.Add(b.Offset);
			starts.Sort();
			foreach (var b in blocks)
			{
				StringsPak.ReadStrings(ms, b, p.Nodes);
				long end = file.Length;
				foreach (var s in starts) if (s > b.Offset) { end = s; break; }
				var raw = new byte[end - b.Offset];
				Buffer.BlockCopy(file, (int)b.Offset, raw, 0, raw.Length);
				p.Blocks.Add(new Block { Number = b.BlockNumber, Strings = b.Strings, Raw = raw });
			}
			return p;
		}

		/// <summary>True when both files have the same Huffman tree (then strings can move between them).</summary>
		public bool SameTree(StringsPakFile other)
		{
			if (NodeTable.Length != other.NodeTable.Length) return false;
			for (int i = 0; i < NodeTable.Length; i++) if (NodeTable[i] != other.NodeTable[i]) return false;
			return true;
		}

		/// <summary>
		/// The file, with <paramref name="changed"/> blocks (by number) re-encoded and every other
		/// block's original bytes. A changed block not in the file is added at the end. Throws if a
		/// string has a character the tree cannot encode.
		/// </summary>
		public byte[] Build(ICollection<int> changed)
		{
			HuffmanNode.SetTree(Nodes);   // the encoder looks leaves up in the tree set last
			var ms = new MemoryStream();
			ms.Write(NodeTable, 0, NodeTable.Length);
			int n = Blocks.Count;
			ms.WriteByte((byte)n); ms.WriteByte((byte)(n >> 8));
			var bodies = new List<byte[]>();
			foreach (var b in Blocks)
			{
				if (changed != null && changed.Contains(b.Number))
				{
					for (int i = 0; i < b.Strings.Count; i++)
						if (StringsPak.ConvertString(b.Strings[i], Nodes) == null)
							throw new ModApplyException(string.Format("string {0} of block {1} has a character STRINGS.PAK cannot encode: {2}", i, b.Number, b.Strings[i]));
					var sb = new StringBlock { BlockNumber = b.Number, Strings = b.Strings, StringCount = b.Strings.Count };
					var bm = new MemoryStream();
					int len = StringsPak.SaveStringBlock(bm, sb, 0, Nodes);
					var body = bm.ToArray();
					if (body.Length != len) Array.Resize(ref body, len);
					bodies.Add(body);
				}
				else bodies.Add(b.Raw);
			}
			long at = ms.Position + 6L * n;
			for (int i = 0; i < n; i++)
			{
				int num = Blocks[i].Number;
				ms.WriteByte((byte)num); ms.WriteByte((byte)(num >> 8));
				ms.WriteByte((byte)at); ms.WriteByte((byte)(at >> 8)); ms.WriteByte((byte)(at >> 16)); ms.WriteByte((byte)(at >> 24));
				at += bodies[i].Length;
			}
			foreach (var body in bodies) ms.Write(body, 0, body.Length);
			return ms.ToArray();
		}
	}
}
