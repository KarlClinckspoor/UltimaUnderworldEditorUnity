using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UltimateEditor.Games;
using UltimateEditor.Modding;
using UltimateEditor.Modding.Exe;
using Xunit;

namespace Formats.Tests.Modding
{
	/// <summary>
	/// Export and install round trips: change a copy of the game as the editor would, export the
	/// difference as a package, apply it to the original files, and get the changed files back
	/// byte for byte. Plus the refusals: a mod applied to files it was not made from.
	/// </summary>
	public class DiffApplyTests
	{
		sealed class Pair
		{
			public string Original, Modded;
			public GameKind Kind;
			public GameFolder O { get { return new GameFolder(Original); } }
			public GameFolder M { get { return new GameFolder(Modded); } }
			public string ModPath(string rel) { return M.Resolve(rel); }
		}

		static Pair Make(string game)
		{
			string a = GameData.Copy(game);
			if (a == null) return null;
			return new Pair { Original = a, Modded = GameData.Copy(game), Kind = game == "UW2" ? GameKind.UW2 : GameKind.UW1 };
		}

		static ModPackage Export(Pair p, out ModDiffResult diff)
		{
			diff = ModDiff.Compare(p.O, p.M, p.Kind);
			Assert.Empty(diff.Problems);
			var details = new ModManifest { Id = "test.mod", Name = "Test mod", Game = p.Kind == GameKind.UW2 ? "uw2" : "uw1", Authors = { "Tester" } };
			var pkg = ModBuilder.FromDiff(diff, details, p.O);
			// through the zip and back, as an installer reads it
			pkg = ModPackage.FromBytes(pkg.ToBytes());
			Assert.Empty(pkg.Check());
			Assert.Empty(ModBuilder.Verify(pkg, p.O, p.M));
			return pkg;
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Unchanged_game_gives_no_components(string game)
		{
			var p = Make(game);
			if (p == null) return;
			var d = ModDiff.Compare(p.O, p.M, p.Kind);
			Assert.Empty(d.Components);
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Level_edit_is_one_block_and_needs_a_new_game(string game)
		{
			var p = Make(game);
			if (p == null) return;
			string lev = p.ModPath("DATA/LEV.ARK");
			var ar = Archive.Load(lev, p.Kind);
			var prof = GameProfile.For(p.Kind);
			byte[] blk = ar.Read(prof.LevelBlock(1));
			blk[4 * (10 * 64 + 10)] ^= 0x01;       // a tile's type bits on level 1
			blk[blk.Length - 3] ^= 0x40;           // something near the end
			ar.Write(prof.LevelBlock(1), blk);
			ar.Save(lev);
			ModDiffResult d;
			var pkg = Export(p, out d);
			var c = Assert.Single(pkg.Manifest.Components);
			var ab = Assert.IsType<ArkBlocksComponent>(c);
			Assert.Equal(prof.LevelBlock(1), Assert.Single(ab.Blocks).Index);
			Assert.Equal(ModClassifier.LevelMod, pkg.Manifest.Type);
			Assert.True(pkg.Manifest.Flags.NewGame);
			Assert.False(pkg.Manifest.Flags.ChangesSaves);
			Assert.Equal("gog-" + (p.Kind == GameKind.UW2 ? "uw2" : "uw1"), pkg.Manifest.BaseSet);
			// the package holds the one block, not the archive
			Assert.True(pkg.Files.Values.Sum(b => b.Length) < 40000);
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void String_and_picture_edits_are_content(string game)
		{
			var p = Make(game);
			if (p == null) return;
			// strings: change, add, and a new block
			string sp = p.ModPath("DATA/STRINGS.PAK");
			var s = StringsPakFile.Parse(File.ReadAllBytes(sp));
			s.Find(4).Strings[2] = "a very sharp dagger";
			s.Find(3).Strings.Add("A new book.\nIt says nothing.");
			s.Blocks.Add(new StringsPakFile.Block { Number = 3584 + 250, Strings = new List<string> { "Hello.", "Goodbye." } });
			File.WriteAllBytes(sp, s.Build(new[] { 3, 4, 3584 + 250 }));
			// a picture of the same size in a .TR, and a smaller one in OBJECTS.GR (the file shrinks)
			string tr = p.ModPath(p.Kind == GameKind.UW2 ? "DATA/T64.TR" : "DATA/W64.TR");
			var t = PictureFile.TryParse(File.ReadAllBytes(tr));
			t.Records[5] = t.Records[5].Select((x, i) => i < 5 ? x : (byte)(x ^ 0x11)).ToArray();
			File.WriteAllBytes(tr, t.Build());
			string og = p.ModPath("DATA/OBJECTS.GR");
			var o = PictureFile.TryParse(File.ReadAllBytes(og));
			o.Records[10] = o.Records[11];
			File.WriteAllBytes(og, o.Build());
			// object data: one byte
			string od = p.ModPath("DATA/OBJECTS.DAT");
			byte[] odb = File.ReadAllBytes(od); odb[20] ^= 0x7; File.WriteAllBytes(od, odb);
			ModDiffResult d;
			var pkg = Export(p, out d);
			var kinds = pkg.Manifest.Components.Select(c => c.Kind).OrderBy(k => k).ToList();
			Assert.Equal(new[] { "bytes", "images", "images", "strings" }, kinds);
			Assert.Equal(ModClassifier.ContentMod, pkg.Manifest.Type);
			Assert.True(pkg.Manifest.Flags.ChangesSaves);
			Assert.False(pkg.Manifest.Flags.NewGame);
			var sc = (StringsComponent)pkg.Manifest.Components.First(c => c is StringsComponent);
			Assert.Equal(4, sc.Count);
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Resized_files_are_whole_files_unknown_ones_are_left_out_and_removed_ones_refused(string game)
		{
			var p = Make(game);
			if (p == null) return;
			// not a file the game reads: left out, with a note
			File.WriteAllBytes(Path.Combine(p.Modded, "CUTS", "NEWCUT.N99"), Encoding.ASCII.GetBytes("new cutscene"));
			File.WriteAllBytes(Path.Combine(p.Modded, "DATA", "uw1-zones.json"), Encoding.ASCII.GetBytes("{}"));
			// a game file whose length changed: a whole file
			string snd = Directory.GetFiles(Path.Combine(p.Modded, "SOUND")).First();
			File.WriteAllBytes(snd, File.ReadAllBytes(snd).Concat(new byte[] { 1, 2, 3 }).ToArray());
			ModDiffResult d;
			var pkg = Export(p, out d);
			Assert.Single(pkg.Manifest.Components);
			Assert.IsType<FileComponent>(pkg.Manifest.Components[0]);
			Assert.Contains(d.Notes, n => n.Contains("NEWCUT.N99") && n.Contains("UW1-ZONES.JSON"));

			// a game file missing from the edited game: refused, not a delete for players' games
			string gone = Directory.GetFiles(Path.Combine(p.Modded, "CUTS")).First(f => !f.EndsWith("N99"));
			File.Delete(gone);
			d = ModDiff.Compare(new GameFolder(p.Original), new GameFolder(p.Modded), p.Kind);
			Assert.Contains(d.Problems, x => x.Contains(Path.GetFileName(gone).ToUpperInvariant()));
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void An_incomplete_install_is_a_problem_not_a_mod(string game)
		{
			var p = Make(game);
			if (p == null) return;
			Directory.Delete(Path.Combine(p.Original, "SOUND"), true);
			var d = ModDiff.Compare(new GameFolder(p.Original), new GameFolder(p.Modded), p.Kind);
			Assert.Contains(d.Problems, x => x.Contains("SOUND"));
			Assert.DoesNotContain(d.Components, c => c.File != null && c.File.StartsWith("SOUND/"));
			string exe = ExeIdentity.ExeName(p.Kind);
			File.Delete(p.ModPath(exe));
			d = ModDiff.Compare(new GameFolder(p.Original), new GameFolder(p.Modded), p.Kind);
			Assert.Contains(d.Problems, x => x.Contains(exe));
			Assert.DoesNotContain(d.Components, c => c.File == exe);
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Start_square_is_a_parameter_and_byte_patches_follow_ultimahacks(string game)
		{
			var p = Make(game);
			if (p == null) return;
			string exeRel = ExeIdentity.ExeName(p.Kind);
			string exePath = p.ModPath(exeRel);
			byte[] exe = File.ReadAllBytes(exePath);
			Assert.True(StartPosition.Patch(exe, p.Kind, 33, 5));
			// a data byte in the load module, away from relocations
			var mz = new MzExe(exe);
			var rel = mz.RelocatedWords();
			int at = mz.MzFileSize - 0x400;
			while (mz.TouchesRelocation(at, 1, rel)) at--;
			exe[at] ^= 0x55;
			File.WriteAllBytes(exePath, exe);
			ModDiffResult d;
			var pkg = Export(p, out d);
			var param = Assert.Single(pkg.Manifest.Components.OfType<ExeParamComponent>());
			Assert.Equal(33, param.GetInt("x")); Assert.Equal(5, param.GetInt("y"));
			var bytes = Assert.Single(pkg.Manifest.Components.OfType<ExeBytesComponent>());
			Assert.Equal(at, Assert.Single(bytes.Patches).FileOffset);
			Assert.Equal(ModClassifier.ExeTweak, pkg.Manifest.Type);
			Assert.Equal(new[] { "exe-patch", "start-square" }, pkg.Manifest.ExhumedNeeds);
			Assert.True(pkg.Manifest.Flags.NewGame);

			// the same mod on an executable moved about: an overlay copied to the end, as
			// UltimaHacks grows one, and its stub pointed there
			byte[] orig = File.ReadAllBytes(p.O.Resolve(exeRel));
			var omz = new MzExe(orig);
			var ov = omz.Segments.Where(s => s.IsOverlay).OrderByDescending(s => s.CodeSize).First();
			byte[] moved = orig.Concat(new byte[ov.CodeSize + ov.RelocationTableLength]).ToArray();
			Buffer.BlockCopy(orig, ov.OverlayStart, moved, orig.Length, ov.CodeSize + ov.RelocationTableLength);
			int newStart = orig.Length - omz.FbovEnd;
			moved[ov.StartInFile + 4] = (byte)newStart; moved[ov.StartInFile + 5] = (byte)(newStart >> 8); moved[ov.StartInFile + 6] = (byte)(newStart >> 16); moved[ov.StartInFile + 7] = (byte)(newStart >> 24);
			// patch a byte inside that overlay's code in a second mod
			var q = Make(game);
			byte[] qexe = File.ReadAllBytes(q.ModPath(exeRel));
			int inOv = ov.OverlayStart + ov.CodeSize / 2;
			var orels = omz.RelocatedWords();
			while (omz.TouchesRelocation(inOv, 1, orels)) inOv++;
			qexe[inOv] ^= 0x01;
			File.WriteAllBytes(q.ModPath(exeRel), qexe);
			ModDiffResult d2;
			var pkg2 = Export(q, out d2);
			var patch = Assert.Single(Assert.Single(pkg2.Manifest.Components.OfType<ExeBytesComponent>()).Patches);
			Assert.Equal(ov.Index, patch.Segment);
			byte[] target = (byte[])moved.Clone();
			ModApply.ApplyExe(pkg2, target);
			Assert.Equal((byte)(orig[inOv] ^ 0x01), target[orig.Length + (inOv - ov.OverlayStart)]);
			Assert.Equal(orig[inOv], target[inOv]);   // the dead old copy stays as it was
			// and a hack that writes the patched byte is refused
			var forbid = new List<KeyValuePair<int, int>> { new KeyValuePair<int, int>(orig.Length + (inOv - ov.OverlayStart), orig.Length + (inOv - ov.OverlayStart) + 1) };
			Assert.Throws<ModApplyException>(() => ModApply.ApplyExe(pkg2, (byte[])moved.Clone(), forbid));
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Changed_conversation_variable_count_is_flagged(string game)
		{
			var p = Make(game);
			if (p == null) return;
			string cnv = p.ModPath("DATA/CNV.ARK");
			var ar = Archive.Load(cnv, p.Kind);
			int slot = Enumerable.Range(1, ar.Count - 1).First(i => ar.Has(i));
			byte[] b = ar.Read(slot);
			b[0xC]++;
			ar.Write(slot, b);
			ar.Save(cnv);
			ModDiffResult d;
			var pkg = Export(p, out d);
			Assert.Equal(new[] { slot }, pkg.Manifest.Flags.BreaksConversations);
			Assert.Contains(ModClassifier.Warnings(pkg.Manifest), w => w.Contains("conversation"));
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void A_mod_refuses_files_it_was_not_made_from(string game)
		{
			var p = Make(game);
			if (p == null) return;
			string od = p.ModPath("DATA/OBJECTS.DAT");
			byte[] b = File.ReadAllBytes(od); b[100] ^= 1; File.WriteAllBytes(od, b);
			string lev = p.ModPath("DATA/LEV.ARK");
			var ar = Archive.Load(lev, p.Kind);
			int lb = GameProfile.For(p.Kind).LevelBlock(2);
			byte[] blk = ar.Read(lb); blk[7] ^= 1; ar.Write(lb, blk); ar.Save(lev);
			ModDiffResult d;
			var pkg = Export(p, out d);
			// the same files changed differently by someone else
			var other = new GameFolder(GameData.Copy(game));
			string od2 = other.Resolve("DATA/OBJECTS.DAT");
			byte[] c = File.ReadAllBytes(od2); c[100] ^= 2; File.WriteAllBytes(od2, c);
			string lev2 = other.Resolve("DATA/LEV.ARK");
			var ar2 = Archive.Load(lev2, p.Kind);
			byte[] blk2 = ar2.Read(lb); blk2[7] ^= 2; ar2.Write(lb, blk2); ar2.Save(lev2);
			var e = Assert.Throws<ModApplyException>(() => ModApply.Files(pkg, other.Read));
			Assert.Contains("OBJECTS.DAT", e.Message);
			Assert.Contains("LEV.ARK", e.Message);
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Saved_game_exports_checks_and_installs(string game)
		{
			var p = Make(game);
			if (p == null) return;
			var kind = p.Kind;
			// a save made from DATA's files: LEV.ARK, BGLOBALS from BABGLOBS (words zeroed), a player record
			string save = Path.Combine(p.Modded, "SAVE2");
			Directory.CreateDirectory(save);
			File.Copy(p.M.Resolve("DATA/LEV.ARK"), Path.Combine(save, "LEV.ARK"));
			var bg = BGlobals.Parse(File.ReadAllBytes(p.M.Resolve("DATA/BABGLOBS.DAT")), false);
			File.WriteAllBytes(Path.Combine(save, "BGLOBALS.DAT"), bg.ToBytes(true));
			File.WriteAllBytes(Path.Combine(save, "PLAYER.DAT"), new byte[PlayerDat.RecordSizeFor(kind) + 3]);
			File.WriteAllBytes(Path.Combine(save, "DESC"), Encoding.ASCII.GetBytes("Before the gate\0"));
			if (kind == GameKind.UW2) File.Copy(p.M.Resolve("DATA/SCD.ARK"), Path.Combine(save, "SCD.ARK"));
			var pkg = new ModPackage { Manifest = new ModManifest { Id = "t.save", Name = "A save", Game = kind == GameKind.UW2 ? "uw2" : "uw1" } };
			var c = SaveMods.Export(save, p.M, kind, 2, pkg.Files);
			pkg.Manifest.Components.Add(c);
			ModClassifier.Classify(pkg.Manifest, null, pkg);
			Assert.Equal(ModClassifier.SavedGame, pkg.Manifest.Type);
			Assert.Equal("Before the gate", c.Description);
			pkg = ModPackage.FromBytes(pkg.ToBytes());
			c = (SaveComponent)pkg.Manifest.Components[0];
			Assert.Empty(pkg.Check());
			Assert.Empty(SaveMods.Check(c, pkg, p.O, kind));
			// with another CNV.ARK (a conversation's count changed) the check says so
			string cnv = p.M.Resolve("DATA/CNV.ARK");
			var ar = Archive.Load(cnv, kind);
			int slot = bg.Records.First(r => ar.Has(r.Slot)).Slot;
			byte[] blk = ar.Read(slot); blk[0xC]++; ar.Write(slot, blk); ar.Save(cnv);
			var probs = SaveMods.Check(c, pkg, p.M, kind);
			Assert.Contains(probs, s => s.Contains("CNV.ARK"));
			Assert.Contains(probs, s => s.Contains("conversation " + slot));
			string slotDir = Path.Combine(p.Original, "SAVE3");
			// an old save in the slot, with a file the package doesn't have
			Directory.CreateDirectory(slotDir);
			File.WriteAllBytes(Path.Combine(slotDir, "PLAYER.DAT"), new byte[] { 9 });
			File.WriteAllBytes(Path.Combine(slotDir, "EXTRA.DAT"), new byte[] { 7 });
			string kept = SaveMods.Install(c, pkg, slotDir);
			foreach (var f in SaveMods.FilesFor(kind)) Assert.Equal(File.ReadAllBytes(Path.Combine(save, f)), File.ReadAllBytes(Path.Combine(slotDir, f)));
			// the slot holds the package's save only (the game restores every file in it), and
			// the old one is kept beside it
			Assert.False(File.Exists(Path.Combine(slotDir, "EXTRA.DAT")));
			Assert.NotNull(kept);
			Assert.Equal(new byte[] { 9 }, File.ReadAllBytes(Path.Combine(kept, "PLAYER.DAT")));
			Assert.False(Directory.Exists(slotDir + ".uwmod-new"));
		}

		[Fact]
		public void A_bad_package_is_refused_as_invalid_data()
		{
			var m = new ModManifest { Id = "x", Name = "x", Game = "uw1", Authors = { "x" } };
			var good = new ModPackage { Manifest = m }.ToBytes();
			var cases = new List<byte[]>();
			// mod.json nested deeper than the stack allows
			string deep = new string('[', 100000) + new string(']', 100000);
			cases.Add(MiniZip.Write(new[] { new MiniZip.Entry("mod.json", System.Text.Encoding.UTF8.GetBytes(deep)) }));
			// truncated JSON, a short \u escape
			cases.Add(MiniZip.Write(new[] { new MiniZip.Entry("mod.json", System.Text.Encoding.UTF8.GetBytes("{\"id\": \"x")) }));
			cases.Add(MiniZip.Write(new[] { new MiniZip.Entry("mod.json", System.Text.Encoding.UTF8.GetBytes("{\"id\": \"\\u12\"}")) }));
			// a zip whose sizes and offsets point outside it
			var bad = (byte[])good.Clone();
			int end = bad.Length - 22;
			bad[end + 16] = 0xFF; bad[end + 17] = 0xFF; bad[end + 18] = 0xFF; bad[end + 19] = 0x7F;
			cases.Add(bad);
			var huge = (byte[])good.Clone();
			int cd = huge[end + 16] | huge[end + 17] << 8 | huge[end + 18] << 16;
			huge[cd + 24] = 0xF0; huge[cd + 25] = 0xFF; huge[cd + 26] = 0xFF; huge[cd + 27] = 0x7F;
			cases.Add(huge);
			cases.Add(new byte[] { 1, 2, 3 });
			// an entry with a path that climbs out
			cases.Add(MiniZip.Write(new[] { new MiniZip.Entry("mod.json", System.Text.Encoding.UTF8.GetBytes(m.ToJson())), new MiniZip.Entry("payload/a/..", new byte[1]) }));
			foreach (var zip in cases)
				Assert.Throws<InvalidDataException>(() => ModPackage.FromBytes(zip));
		}

		[Fact]
		public void Manifest_round_trips_through_json()
		{
			var m = new ModManifest { Id = "karl.gate", Name = "Gate \"of\" Dawn", Version = "1.2.0", Game = "uw2", Authors = { "Karl", "Someone" }, Description = "Line 1\nLine 2", License = "CC BY 4.0", Type = ModClassifier.LevelMod };
			m.BaseFiles["DATA/LEV.ARK"] = "sha256:00";
			m.Components.Add(new ArkBlocksComponent { File = "DATA/LEV.ARK", Archive = "uw2", Blocks = { new ArkBlocksComponent.Block { Index = 3, Base = "sha256:aa", Data = "payload/x.bin" }, new ArkBlocksComponent.Block { Index = 9, Base = "sha256:bb", Delete = true } } });
			var ep = new ExeParamComponent { Param = ExeParams.StartSquare, File = "UW2.EXE" }; ep.Value["x"] = 33; ep.Value["y"] = 5;
			m.Components.Add(ep);
			m.Components.Add(new ExeBytesComponent { File = "UW2.EXE", Patches = { new ExeBytesComponent.Patch { Segment = 93, Offset = 0x10, FileOffset = 0x1234, Base = "90", Data = "cc", Before = "0102", After = "0304" } } });
			m.Flags.NewGame = true; m.Flags.BreaksConversations.Add(7);
			m.ExhumedNeeds.Add("start-square"); m.ExhumedRecommended.Add("tidy-objects");
			var back = ModManifest.Parse(m.ToJson());
			Same.Assert(m, back, "manifest");
			Assert.Throws<FormatException>(() => ModManifest.Parse("{\"format\": 99, \"game\": \"uw1\"}"));
		}
	}
}
