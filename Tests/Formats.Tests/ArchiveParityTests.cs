extern alias legacy;
extern alias current;
using System.IO;
using System.Linq;
using UltimateEditor.Games;
using Xunit;
using LG = legacy::UltimateEditor.Games;

namespace Formats.Tests
{
	/// <summary>Archives, LZSS and level blocks: the old code against the moved code, on every archive of both games.</summary>
	public class ArchiveParityTests
	{
		[Theory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Archives_read_and_rewrite_the_same(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			var kind = game == "UW2" ? GameKind.UW2 : GameKind.UW1;
			var lkind = game == "UW2" ? LG.GameKind.UW2 : LG.GameKind.UW1;
			Assert.Equal((int)lkind, (int)GameProfile.Detect(Path.Combine(dir, "DATA")));
			Assert.Equal((int)LG.GameProfile.Detect(Path.Combine(dir, "DATA")), (int)GameProfile.Detect(Path.Combine(dir, "DATA")));
			int arks = 0;
			foreach (var path in Directory.GetFiles(Path.Combine(dir, "DATA"), "*.ARK"))
			{
				// UW1's CNV.ARK and LEV.ARK are UW1 archives; UW2's are all the UW2 kind
				byte[] file = File.ReadAllBytes(path);
				var a = LG.Archive.Open(file, lkind);
				var b = Archive.Open(file, kind);
				Assert.Equal(a.Count, b.Count);
				for (int i = 0; i < b.Count; i++)
				{
					Assert.Equal(a.Has(i), b.Has(i));
					if (!b.Has(i)) continue;
					byte[] ra = a.Read(i), rb = b.Read(i);
					Assert.Equal(ra, rb);
					a.Write(i, ra); b.Write(i, rb);   // rewriting a block unchanged goes through put_arc's cases
					if (b is Uw2Archive u2 && u2.Compressed(i))
						Assert.Equal(LG.Lzss.EncodeBlock(ra), Lzss.EncodeBlock(rb));
				}
				Same.Bytes(a.ToArray(), b.ToArray(), Path.GetFileName(path) + " rewritten");
				Same.Bytes(file, b.ToArray(), Path.GetFileName(path) + " rewritten unchanged");
				arks++;
			}
			Assert.True(arks >= 2);
		}

		[Theory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Levels_load_and_build_the_same_blocks(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			string lev = Path.Combine(dir, "DATA", "LEV.ARK");
			legacy::DataReader.FilePath = current::DataReader.FilePath = lev;
			legacy::LevArk.Reset(); current::LevArk.Reset();
			var a = legacy::LevArk.Load(lev);
			var b = current::LevArk.Load(lev);
			Assert.Equal(a.Count, b.Count);
			for (int l = 1; l <= b.Count; l++)
			{
				byte[] ba = legacy::LevArk.BuildLevelBlock(a[l - 1], l);
				byte[] bb = current::LevArk.BuildLevelBlock(b[l - 1], l);
				if (ba == null) { Assert.Null(bb); continue; }
				Same.Bytes(ba, bb, game + " level " + l + " block");
			}
		}
	}
}
