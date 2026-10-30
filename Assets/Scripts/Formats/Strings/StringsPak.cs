using System;
using System.Collections.Generic;
using System.IO;

namespace UltimateEditor.Games
{
	/// <summary>
	/// STRINGS.PAK, both games (UW-Formats 3.3; GAMESTRN.C): a Huffman tree (word node count,
	/// four bytes a node), a block table (word count, then word block number and doubleword
	/// offset), and each block (word string count, word offsets, then the bit stream).
	///
	/// The methods are DataReader's and DataWriter's string code, moved here so they build
	/// without Unity; DataReader and DataWriter call them. Logging goes through FormatsLog. Since
	/// the move, ConvertString uses a code table per tree (the same bytes, several times faster)
	/// and CanEncode checks a string without encoding it.
	/// </summary>
	public static class StringsPak
	{
		public static Tuple<HuffmanNode[], long> ReadHuffmanNodes(Stream fs)
		{
			fs.Position = 0;

			int a = fs.ReadByte();
			int b = fs.ReadByte();

			int nodeCount = a + b * 256;

			HuffmanNode[] nodes = new HuffmanNode[nodeCount];
			//Debug.Log("Node count  " + nodeCount);
			for (int i = 0; i < nodeCount; i++)
			{
				int _char = fs.ReadByte();
				int parent = fs.ReadByte();
				int left = fs.ReadByte();
				int right = fs.ReadByte();
				nodes[i] = new HuffmanNode(_char, i, parent, left, right);
				//Debug.LogFormat("Node {0} : {1}", i, nodes[i]);
			}
			Tuple<HuffmanNode[], long> pair = new Tuple<HuffmanNode[], long>(nodes, fs.Position);
			return pair;
		}
		public static Tuple<List<StringBlock>, Dictionary<int, int>> ReadStringBlocks(Stream fs, long startOffset)
		{
			fs.Position = startOffset;

			int a = fs.ReadByte();
			int b = fs.ReadByte();

			int blockCount = a + b * 256;
			List<StringBlock> blocks = new List<StringBlock>();
			Dictionary<int, int> blockDict = new Dictionary<int, int>();

			for (int i = 0; i < blockCount; i++)
			{
				int blk_a = fs.ReadByte();
				int blk_b = fs.ReadByte();
				int blk_c = fs.ReadByte();
				int blk_d = fs.ReadByte();
				int blk_e = fs.ReadByte();
				int blk_f = fs.ReadByte();

				StringBlock block = new StringBlock();
				block = new StringBlock();
				block.BlockNumber = blk_a + blk_b * 256;
				block.Offset = blk_c + blk_d * 256 + blk_e * 256 * 256 + blk_f * 256 * 256 * 256;
				blockDict[block.BlockNumber] = i;
				blocks.Add(block);
				//Debug.LogFormat("String block {0}, block number {1}", i, blocks[i].BlockNumber);
			}
			for (int i = 0; i < blockCount; i++)
			{
				fs.Position = blocks[i].Offset;
				int blk2_a = fs.ReadByte();
				int blk2_b = fs.ReadByte();

				blocks[i].StringCount = blk2_a + blk2_b * 256;
				blocks[i].Offsets = new List<int>();
				for (int j = 0; j < blocks[i].StringCount; j++)
				{
					int blk3_a = fs.ReadByte();
					int blk3_b = fs.ReadByte();
					int offset = blk3_a + blk3_b * 256;
					blocks[i].Offsets.Add(offset);

					//Debug.LogFormat("string block {0} string {1} offset : {2}", i, j, offset);
				}
			}

			return new Tuple<List<StringBlock>, Dictionary<int, int>>(blocks, blockDict);
		}
		public static List<string> ReadStrings(Stream fs, StringBlock block, HuffmanNode[] nodes, bool debug = false)
		{
			if (block == null)
				return null;

			block.Strings = new List<string>();
			for (int i = 0; i < block.StringCount; i++)
			{
				fs.Position = block.Offset + 2 + (2 * block.StringCount) + block.Offsets[i];
				string curString = "";
				char curChar = default(char);

				// GAMESTRN.C: a string ends at '|', at a 0xFF character or after 512 characters.
				int safe = 512;
				int raw = 0;
				int bit = 0;
				while (true)
				{
					curChar = GetChar(fs, nodes, ref raw, ref bit, debug);
					if (curChar == '|' || curChar == (char)0xFF)
					{
						block.Strings.Add(curString);
						break;
					}
					curString += curChar;

					safe--;
					if (safe == 0)
					{
						FormatsLog.Warning(string.Format("String {0} of block {1} reached 512 characters without a terminator", i, block.BlockNumber));
						block.Strings.Add(curString);
						break;
					}
				}
				//if (i == block.StringCount - 1)
				//{
				//	Debug.LogFormat("Last string ({0}) of block {1}, offset : {2}, string : {3}", i, block.BlockNumber, ToHex(fs.Position - 1), block.Strings[i]);
				//}
			}
			return block.Strings;
		}
		public static char GetChar(Stream fs, HuffmanNode[] nodes, ref int raw, ref int bit, bool debug = false)
		{
			HuffmanNode curNode = nodes[nodes.Length - 1];
			//Debug.Log("nodes.Length - 1 " + (nodes.Length - 1));

			int safe = 100;

			while (curNode.Left != 255 && curNode.Right != 255)
			{

				if (bit == 0)
				{
					raw = fs.ReadByte();
					bit = 8;
				}
				//Debug.LogFormat("curNode : {0}, raw : {1}({3}), bit : {2}, fs.Position (after) : {4}", curNode, raw, bit, ToHex(raw), ToHex(fs.Position));
				if (((raw & 0x80) >> 7) == 1)
				{
					curNode = nodes[curNode.Right];
					//Debug.Log("right");
				}
				else
				{
					curNode = nodes[curNode.Left];
					//Debug.Log("left");
				}

				bit--;
				raw = raw << 1;

				safe--;
				if (safe == 0)
				{
					FormatsLog.Error(string.Format("Failed to get char, curNode {0}, raw {1}, bit {2}, fs.Position {3}", curNode, raw, bit, fs.Position));
					break;
				}
			}
			if (debug)
			{
				FormatsLog.Info(string.Format("Got char : {0}[{2}], node {1}", (char)curNode.Char, curNode, curNode.Char));
			}
			return (char)curNode.Char;
		}
		/// <summary>Each leaf's code (its path from the root, the first step in the highest bit)
		/// for the tree in use, worked out once per tree instead of walking up the tree for every
		/// character of every string.</summary>
		sealed class CodeTable
		{
			public HuffmanNode[] Nodes;
			public readonly Dictionary<char, KeyValuePair<ulong, int>> Codes = new Dictionary<char, KeyValuePair<ulong, int>>();
		}
		static readonly System.Runtime.CompilerServices.ConditionalWeakTable<HuffmanNode[], CodeTable> codes
			= new System.Runtime.CompilerServices.ConditionalWeakTable<HuffmanNode[], CodeTable>();

		static CodeTable Codes(HuffmanNode[] nodes)
		{
			return codes.GetValue(nodes, n => new CodeTable { Nodes = n });
		}

		/// <summary>The code of a character (bits, length), or length -1 when the tree has no leaf
		/// for it. Same walk as before: from the leaf up to the root, right child = 1.</summary>
		static KeyValuePair<ulong, int> Code(HuffmanNode[] nodes, char c)
		{
			var t = Codes(nodes);
			KeyValuePair<ulong, int> code;
			if (t.Codes.TryGetValue(c, out code)) return code;
			HuffmanNode node = GetNode(nodes, c);
			if (node == null) { code = new KeyValuePair<ulong, int>(0, -1); t.Codes[c] = code; return code; }
			ulong bits = 0;
			int len = 0;
			int current = node.Current;
			int nextParent = node.Parent;
			do
			{
				node = nodes[nextParent];
				nextParent = node.Parent;
				// bit len is the step len levels above the leaf, so the highest bit is the
				// root's step: written from the highest bit down, the code goes root first
				if (len < 64 && current == node.Right) bits |= 1UL << len;
				len++;
				current = node.Current;
			}
			while (nextParent != 255 && len <= 64);
			if (len > 64) { code = new KeyValuePair<ulong, int>(0, -2); t.Codes[c] = code; return code; }
			code = new KeyValuePair<ulong, int>(bits, len);
			t.Codes[c] = code;
			return code;
		}

		/// <summary>Whether every character of the string, and the terminator, has a code.</summary>
		public static bool CanEncode(string str, HuffmanNode[] nodes)
		{
			if (Code(nodes, '|').Value < 0) return false;
			foreach (char c in str)
				if (Code(nodes, c).Value < 0) return false;
			return true;
		}

		/// <summary>The string's bytes: each character's code and then the terminator '|',
		/// packed from the highest bit, the last byte padded with zeros. Null when a character
		/// has no code.</summary>
		public static List<int> ConvertString(string str, HuffmanNode[] nodes)
		{
			var convString = new List<int>(str.Length / 2 + 2);
			int cur = 0, bit = 8;   // bits still free in cur
			for (int i = 0; i < str.Length + 1; i++)
			{
				char c = i == str.Length ? '|' : str[i];
				var code = Code(nodes, c);
				if (code.Value == -2)
					return ConvertStringSlow(str, nodes);   // a code longer than 64 bits: walk it
				if (code.Value < 0)
				{
					FormatsLog.Error(string.Format("Couldn't get node for char {0}, string \n{1}\ni : {2}", c, str, i));
					return null;
				}
				for (int k = code.Value - 1; k >= 0; k--)
				{
					if (bit == 0) { convString.Add(cur); cur = 0; bit = 8; }
					if ((code.Key & (1UL << k)) != 0) cur |= 1 << (bit - 1);
					bit--;
				}
			}
			convString.Add(cur);
			return convString;
		}

		/// <summary>The original bit-by-bit encoder, kept for codes over 64 bits.</summary>
		static List<int> ConvertStringSlow(string str, HuffmanNode[] nodes)
		{
			List<bool> curBits = new List<bool>();
			List<bool> bits = new List<bool>();

			for (int i = 0; i < str.Length + 1; i++)
			{
				char c;
				if(i == str.Length)
					c = '|';
				else
					c = str[i];
				HuffmanNode node = GetNode(nodes, c);
				if(node == null)
				{
					FormatsLog.Error(string.Format("Couldn't get node for char {0}, string \n{1}\ni : {2}", c, str, i));
					return null;
				}
				int current = node.Current;
				int nextParent = node.Parent;

				do
				{
					node = nodes[nextParent];
					nextParent = node.Parent;

					if (current == node.Right)
						curBits.Add(true);
					else
						curBits.Add(false);

					current = node.Current;
				}
				while (nextParent != 255);
				for (int j = curBits.Count - 1; j > -1; j--)
					bits.Add(curBits[j]);
				curBits = new List<bool>();
			}
			return ConvertString(nodes, bits);
		}
		private static List<int> ConvertString(HuffmanNode[] nodes, List<bool> bits)
		{
			int bit = 0;
			int cur = 0;
			List<int> convString = new List<int>();
			for (int i = 0; i < bits.Count; i++)
			{
				if (bit == 0)
				{
					if (i > 0)
						convString.Add(cur);
					bit = 8;
					cur = 0;
				}
				if (bits[i])
					cur |= 1 << (bit - 1);
				bit--;
			}
			convString.Add(cur);
			return convString;
		}
		public static HuffmanNode GetNode(HuffmanNode[] nodes, char c)
		{
			int i = HuffmanNode.LeafOf(nodes, c);
			if (i < 0 || nodes == null || i >= nodes.Length)
				return null;

			return nodes[i];
		}
		/// <summary>
		/// Returns string block size
		/// </summary>
		public static int SaveStringBlock(Stream fs, StringBlock block, int blockPos, HuffmanNode[] nodes)
		{
			fs.Position = blockPos;
			//First, write string count (int16)
			byte count_a = (byte)(block.StringCount & 0xFF);
			byte count_b = (byte)((block.StringCount & 0xFF00) >> 8);

			fs.WriteByte(count_a);
			fs.WriteByte(count_b);

			int currentOffset = 0;
			int firstStringOffset = 2 + (2 * block.StringCount);
			for (int i = 0; i < block.StringCount; i++)
			{
				//Debug.LogFormat("Saving string : {0}", block.Strings[i]);
				fs.Position = blockPos + 2 + i * 2;
				byte offset_a = (byte)(currentOffset & 0xFF);
				byte offset_b = (byte)((currentOffset & 0xFF00) >> 8);
				fs.WriteByte(offset_a);
				fs.WriteByte(offset_b);
				fs.Position = blockPos + firstStringOffset + currentOffset;
				List<int> rawString = ConvertString(block.Strings[i], nodes);
				if (rawString == null)
					break;
				for (int j = 0; j < rawString.Count; j++)
					fs.WriteByte((byte)rawString[j]);
				currentOffset += rawString.Count;
			}
			int offset = firstStringOffset + currentOffset;
			//Debug.LogFormat("Block {0} offset {1}", block.BlockNumber, offset);
			//if ((offset % 2) == 1)
			//{
			//	fs.WriteByte(0);
			//	offset++;
			//}
			return offset;
		}
		public static void SaveStringBlocks(Stream fs, List<StringBlock> blocks, HuffmanNode[] nodes, long tableStart)
		{
			// The block table follows the node table (GAMESTRN.C): 2 + 4 * node count. It was
			// hard-coded as 726, right only for the shipped tree of 181 nodes.
			int start = (int)tableStart;
			fs.Position = start;

			//Write block count - int16
			byte count_a = (byte)(blocks.Count & 0xFF);
			byte count_b = (byte)((blocks.Count & 0xFF00) >> 8);

			fs.WriteByte(count_a);
			fs.WriteByte(count_b);

			//Next, get the length of the block "table"
			int firstBlock = 2 + (6 * blocks.Count);
			int currentOffset = start + firstBlock;
			for (int i = 0; i < blocks.Count; i++)
			{
				//Debug.LogFormat("Saving string block {0}, cur offset : {1}", blocks[i].BlockNumber, currentOffset);
				fs.Position = start + 2 + i * 6;
				byte strings_a = (byte)(blocks[i].BlockNumber & 0xFF);
				byte strings_b = (byte)((blocks[i].BlockNumber & 0xFF00) >> 8);
				byte offset_a = (byte)(currentOffset & 0xFF);
				byte offset_b = (byte)((currentOffset & 0xFF00) >> 8);
				byte offset_c = (byte)((currentOffset & 0xFF0000) >> 16);
				byte offset_d = (byte)((currentOffset & 0xFF000000) >> 24);

				fs.WriteByte(strings_a); fs.WriteByte(strings_b);
				fs.WriteByte(offset_a); fs.WriteByte(offset_b); fs.WriteByte(offset_c); fs.WriteByte(offset_d);

				int blockOffset = SaveStringBlock(fs, blocks[i], currentOffset, nodes);
				currentOffset += blockOffset;
			
			}
		}
		public static long SaveHuffmanNodes(Stream fs, HuffmanNode[] nodes)
		{
			fs.Position = 0;
			byte len_a = (byte)(nodes.Length & 0xFF);
			byte len_b = (byte)((nodes.Length & 0xFF00) >> 8);
			fs.WriteByte(len_a);
			fs.WriteByte(len_b);
			foreach (var node in nodes)
			{
				fs.WriteByte((byte)node.Char);
				fs.WriteByte((byte)node.Parent);
				fs.WriteByte((byte)node.Left);
				fs.WriteByte((byte)node.Right);
			}
			return fs.Position;
		}
	}
}
