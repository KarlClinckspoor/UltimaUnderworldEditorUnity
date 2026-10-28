extern alias legacy;
extern alias current;
using System.Collections.Generic;
using System.IO;
using Xunit;
using L = legacy::DataReader;
using LW = legacy::DataWriter;
using C = current::DataReader;
using CW = current::DataWriter;

namespace Formats.Tests
{
	/// <summary>STRINGS.PAK: the old DataReader/DataWriter code against the moved StringsPak.</summary>
	public class StringsParityTests
	{
		[Theory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Reading_gives_the_same_tree_blocks_and_strings(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			L.FilePath = C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			var a = L.LoadStrings();
			var b = C.LoadStrings();
			Assert.True(b.Blocks.Count > 10);
			Same.Assert(a.Nodes, b.Nodes, "Huffman nodes");
			Same.Assert(a.Blocks, b.Blocks, "string blocks");
			Same.Assert(a.BlockDictionary, b.BlockDictionary, "block dictionary");
		}

		[Theory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Writing_gives_the_same_bytes(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			L.FilePath = C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			var a = L.LoadStrings();
			var b = C.LoadStrings();
			a.SortBlocks(); b.SortBlocks();
			byte[] wa = Write(fs => { long t = LW.SaveHuffmanNodes(fs, a.Nodes); LW.SaveStringBlocks(fs, a.Blocks, a.Nodes, t); });
			byte[] wb = Write(fs => { long t = CW.SaveHuffmanNodes(fs, b.Nodes); CW.SaveStringBlocks(fs, b.Blocks, b.Nodes, t); });
			Same.Bytes(wa, wb, game + " STRINGS.PAK written");

			// every string encodes the same way, and the written file reads back to the same strings
			for (int i = 0; i < a.Blocks.Count; i++)
				for (int j = 0; j < a.Blocks[i].Strings.Count; j++)
					Assert.Equal(LW.ConvertString(a.Blocks[i].Strings[j], a.Nodes), CW.ConvertString(b.Blocks[i].Strings[j], b.Nodes));
			File.WriteAllBytes(GameData.InData(dir, "STRINGS.PAK"), wb);
			var again = C.LoadStrings();
			for (int i = 0; i < b.Blocks.Count; i++)
				Assert.Equal(b.Blocks[i].Strings, again.Blocks[i].Strings);
		}

		static byte[] Write(System.Action<Stream> w)
		{
			using (var ms = new MemoryStream()) { w(ms); return ms.ToArray(); }
		}
	}
}
