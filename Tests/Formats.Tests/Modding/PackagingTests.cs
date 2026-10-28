using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UltimateEditor.Games;
using UltimateEditor.Modding;
using UltimateEditor.Modding.Exe;
using Xunit;

namespace Formats.Tests.Modding
{
	/// <summary>The containers: zip, picture files, STRINGS.PAK blocks, the executable's layout.</summary>
	public class PackagingTests
	{
		[Fact]
		public void Zip_round_trips_and_is_a_standard_zip()
		{
			var entries = new List<MiniZip.Entry>
			{
				new MiniZip.Entry("mod.json", Encoding.UTF8.GetBytes("{\"a\": 1}")),
				new MiniZip.Entry("payload/x/0001.bin", Enumerable.Range(0, 5000).Select(i => (byte)(i % 7)).ToArray()),
				new MiniZip.Entry("payload/empty.bin", new byte[0]),
				new MiniZip.Entry("README.md", Encoding.UTF8.GetBytes("Ünïcode ✓")),
			};
			byte[] zip = MiniZip.Write(entries);
			var back = MiniZip.Read(zip);
			Assert.Equal(entries.Select(e => e.Name), back.Select(e => e.Name));
			for (int i = 0; i < entries.Count; i++) Assert.Equal(entries[i].Data, back[i].Data);
			// a standard reader agrees, and we read a standard writer's zip
			using (var za = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
			{
				Assert.Equal(4, za.Entries.Count);
				using (var s = za.GetEntry("payload/x/0001.bin").Open()) { var ms = new MemoryStream(); s.CopyTo(ms); Assert.Equal(entries[1].Data, ms.ToArray()); }
			}
			var ms2 = new MemoryStream();
			using (var za = new ZipArchive(ms2, ZipArchiveMode.Create, true))
			{
				var e = za.CreateEntry("dir/file.txt"); using (var s = e.Open()) s.Write(Encoding.UTF8.GetBytes("hello"), 0, 5);
				za.CreateEntry("dir/");
			}
			var read = MiniZip.Read(ms2.ToArray());
			Assert.Single(read);
			Assert.Equal("hello", Encoding.UTF8.GetString(read[0].Data));
			Assert.Equal(zip, MiniZip.Write(entries));   // same content, same bytes
		}

		[Theory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Every_picture_file_rebuilds_byte_for_byte(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			int n = 0;
			foreach (var f in Directory.GetFiles(Path.Combine(dir, "DATA")).Where(f => f.EndsWith(".GR") || f.EndsWith(".TR")))
			{
				byte[] b = File.ReadAllBytes(f);
				var p = PictureFile.TryParse(b);
				Assert.NotNull(p);
				Assert.Equal(b, p.Build());
				n++;
			}
			Assert.True(n > 30);
		}

		[Theory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void StringsPak_rebuilds_unchanged_and_changes_only_the_blocks_it_must(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			byte[] orig = File.ReadAllBytes(GameData.InData(dir, "STRINGS.PAK"));
			var f = StringsPakFile.Parse(orig);
			Assert.Equal(orig, f.Build(null));
			// change one string of block 4 (item names) and add one
			var b4 = f.Find(4);
			b4.Strings[1] = "a sharper knife";
			b4.Strings.Add("a new thing");
			byte[] changed = f.Build(new[] { 4 });
			var g = StringsPakFile.Parse(changed);
			Assert.Equal("a sharper knife", g.Find(4).Strings[1]);
			Assert.Equal("a new thing", g.Find(4).Strings.Last());
			var o = StringsPakFile.Parse(orig);
			foreach (var blk in o.Blocks)
				if (blk.Number != 4) Assert.Equal(blk.Raw, g.Find(blk.Number).Raw);
		}

		[Theory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Executable_layout_locates_and_finds_relocations(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			byte[] exe = File.ReadAllBytes(GameData.Exe(dir));
			var kind = game == "UW2" ? GameKind.UW2 : GameKind.UW1;
			Assert.True(ExeIdentity.IsGogOriginal(exe, kind));
			var mz = new MzExe(exe);
			Assert.Contains(mz.Segments, s => s.IsOverlay);
			Assert.NotEmpty(mz.LoadModuleRelocations);
			var rnd = new System.Random(1);
			for (int k = 0; k < 2000; k++)
			{
				int at = rnd.Next(exe.Length);
				var l = mz.Locate(at);
				Assert.Equal(at, mz.FileOffset(l));
			}
			// the start square's bytes are plain code bytes, not relocated words
			int sq = StartPosition.Find(exe, kind);
			Assert.True(sq > 0);
			Assert.False(mz.TouchesRelocation(sq + 3, 3));
			Assert.NotEqual(-2, mz.Locate(sq).Segment);
		}

		[Fact]
		public void Hack_files_parse_and_apply_like_the_hack_applier()
		{
			// a hand-made hack: insert 4 zero bytes at 2, copy [0, 2) to 6, overwrite 1 byte at 0
			var proto = new List<byte>();
			void Msg(int field, byte[] body) { proto.Add((byte)(field << 3 | 2)); proto.Add((byte)body.Length); proto.AddRange(body); }
			byte[] Edit(int kind, params byte[] inner) { var l = new List<byte> { (byte)(kind << 3 | 2), (byte)inner.Length }; l.AddRange(inner); return l.ToArray(); }
			Msg(1, Edit(1, 0x08, 2, 0x10, 4));
			Msg(1, Edit(2, 0x08, 0, 0x10, 6, 0x18, 2));
			Msg(1, Edit(3, 0x08, 0, 0x12, 1, 0xEE));
			Msg(2, new byte[] { 0x08, 6 });
			var h = UltimaHack.Parse(proto.ToArray());
			Assert.Equal(6, h.TargetLength);
			Assert.Equal(3, h.Edits.Count);
			Assert.Equal(new byte[] { 0xEE, 2, 0, 0, 0, 0, 1, 2, 5, 6 }, h.Apply(new byte[] { 1, 2, 3, 4, 5, 6 }));
			Assert.Throws<InvalidDataException>(() => h.Apply(new byte[5]));
		}
	}
}
