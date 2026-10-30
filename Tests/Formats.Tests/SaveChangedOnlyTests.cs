extern alias legacy;
extern alias current;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UltimateEditor.Games;
using UnityEngine;
using Xunit;
using L = legacy::DataReader;
using LW = legacy::DataWriter;
using C = current::DataReader;
using CW = current::DataWriter;

namespace Formats.Tests
{
	/// <summary>
	/// Saving writes only what changed (the speed-up plan's S1-S4): the palette table is read
	/// once instead of seeked per pixel, both games' picture files are patched for replaced
	/// pictures only (UW1 through Uw1TrFiles and GrPictures), and STRINGS.PAK is skipped when
	/// no string changed. Each test checks the new code against the old (legacy::) where both
	/// write the same thing, and that untouched files keep their bytes.
	/// </summary>
	public class SaveChangedOnlyTests
	{
		static readonly string[] PictureFiles = { "W64.TR", "W16.TR", "F32.TR", "F16.TR", "T64.TR", "DOORS.GR", "TMFLAT.GR", "TMOBJ.GR", "GENHEAD.GR", "CHARHEAD.GR", "OBJECTS.GR", "TERRAIN.DAT" };

		static string Setup(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return null;
			Application.dataPath = Path.Combine(dir, "unity-data");
			Application.streamingAssetsPath = Path.Combine(Repo.Root, "Assets", "StreamingAssets");
			L.FilePath = C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			SetUiManager(typeof(L), typeof(legacy::UIManager));
			SetUiManager(typeof(C), typeof(current::UIManager));
			return dir;
		}

		static void SetUiManager(Type reader, Type ui)
		{
			var f = reader.GetField("uiManager", BindingFlags.NonPublic | BindingFlags.Static);
			f.SetValue(null, RuntimeHelpers.GetUninitializedObject(ui));
		}

		static Dictionary<string, byte[]> Snapshot(string dir)
		{
			var d = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
			foreach (var f in Directory.GetFiles(Path.Combine(dir, "DATA")))
				d[Path.GetFileName(f)] = File.ReadAllBytes(f);
			return d;
		}

		static bool HasBackup(string dir, string name)
		{
			string f = GameData.InData(dir, name);
			return f != null && File.Exists(f + ".BKP");
		}

		/// <summary>A picture made of palette colours (so it maps back exactly), opaque.</summary>
		static Texture2D Pattern(int w, int h, Color[] pal, int seed)
		{
			var t = new Texture2D(w, h);
			var rnd = new System.Random(seed);
			var px = new Color32[w * h];
			for (int i = 0; i < px.Length; i++) { Color32 c = pal[1 + rnd.Next(255)]; c.a = 255; px[i] = c; }
			t.SetPixels32(px);
			return t;
		}

		[GameFact("UW")]
		public void Palette_lookup_in_memory_gives_the_old_indexes()
		{
			string dir = Setup("UW");
			if (dir == null) return;
			var tex = C.LoadTextures();
			Assert.NotNull(tex);
			var lists = new[] { tex.Walls, tex.Floors, tex.Doors, tex.Levers, tex.GenHeads, tex.NPCHeads, tex.Other };
			int n = 0;
			foreach (var list in lists)
				foreach (var t in list.Textures)
				{
					int[] old = LW.ConvertToRawTexture(t, tex.Palettes[0], false);
					Assert.Equal(old, CW.ConvertToRawTexture(t, tex.Palettes[0], false));
					Assert.Equal(old, CW.ToPaletteIndexes(t));
					if (t.width == t.height)
						Assert.Equal(LW.ConvertToRawTexture(t, tex.Palettes[0], true), CW.ConvertToRawTexture(t, tex.Palettes[0], true));
					n++;
				}
			// the small copies: sampled from the indexes as the old writer sampled the picture
			foreach (var pair in new[] { Tuple.Create(tex.Walls, 64, 4), Tuple.Create(tex.Floors, 32, 2) })
				foreach (var t in pair.Item1.Textures)
					Assert.Equal(LW.ConvertToRawTexture(LW.ScaleDownTexture(t, pair.Item3), tex.Palettes[0], false),
						current::Uw1TrFiles.SampleDown(CW.ToPaletteIndexes(t), pair.Item2, pair.Item3));
			Assert.True(n > 400);
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Saving_unchanged_pictures_writes_no_file(string game)
		{
			string dir = Setup(game);
			if (dir == null) return;
			var tex = C.LoadTextures();
			var before = Snapshot(dir);
			CW.SaveTextureData(tex);
			CW.SaveTextureData(tex);
			var after = Snapshot(dir);
			foreach (var name in PictureFiles)
			{
				if (!before.ContainsKey(name)) continue;
				Same.Bytes(before[name], after[name], game + " " + name);
				Assert.False(HasBackup(dir, name), game + " " + name + " was backed up, so it was written");
			}
			Assert.Empty(current::GrPictures.Problems);
		}

		[GameFact("UW")]
		public void A_replaced_uw1_wall_and_floor_are_patched_in_place()
		{
			string dir = Setup("UW");
			if (dir == null) return;
			var tex = C.LoadTextures();
			var before = Snapshot(dir);
			Color[] pal = tex.Palettes[0];
			var wall = Pattern(64, 64, pal, 1);
			var floor = Pattern(32, 32, pal, 2);
			tex.Walls.Textures[7] = wall;
			tex.Floors.Textures[3] = floor;
			CW.SaveTextureData(tex);
			var after = Snapshot(dir);

			foreach (var f in new[] { Tuple.Create("W64.TR", "W16.TR", 7, wall, 64, 4), Tuple.Create("F32.TR", "F16.TR", 3, floor, 32, 2) })
			{
				byte[] big = after[f.Item1], small = after[f.Item2];
				Assert.Equal(before[f.Item1].Length, big.Length);
				Assert.Equal(before[f.Item2].Length, small.Length);
				int[] o = current::Uw1TrFiles.Offsets(big), os = current::Uw1TrFiles.Offsets(small);
				int size = f.Item5, s = size / f.Item6;
				int[] want = LW.ConvertToRawTexture(f.Item4, pal, false);
				int[] wantSmall = LW.ConvertToRawTexture(LW.ScaleDownTexture(f.Item4, f.Item6), pal, false);
				for (int k = 0; k < size * size; k++) Assert.Equal((byte)want[k], big[o[f.Item3] + k]);
				for (int k = 0; k < s * s; k++) Assert.Equal((byte)wantSmall[k], small[os[f.Item3] + k]);
				// every byte outside the replaced picture is as shipped
				for (int k = 0; k < big.Length; k++)
					if (k < o[f.Item3] || k >= o[f.Item3] + size * size) Assert.Equal(before[f.Item1][k], big[k]);
				for (int k = 0; k < small.Length; k++)
					if (k < os[f.Item3] || k >= os[f.Item3] + s * s) Assert.Equal(before[f.Item2][k], small[k]);
				Assert.True(HasBackup(dir, f.Item1));
			}
			foreach (var name in new[] { "DOORS.GR", "TMFLAT.GR", "TMOBJ.GR", "GENHEAD.GR", "CHARHEAD.GR", "OBJECTS.GR" })
				Same.Bytes(before[name], after[name], name);

			// read back, the editor shows the new pictures
			var again = C.LoadTextures();
			Assert.Equal(wall.GetPixels32(), again.Walls.Textures[7].GetPixels32());
			Assert.Equal(floor.GetPixels32(), again.Floors.Textures[3].GetPixels32());

			// and a second save writes nothing more
			var once = Snapshot(dir);
			CW.SaveTextureData(again);
			var twice = Snapshot(dir);
			foreach (var name in PictureFiles)
				if (once.ContainsKey(name)) Same.Bytes(once[name], twice[name], "second save, " + name);
		}

		[GameFact("UW")]
		public void A_replaced_uw1_door_rebuilds_only_that_picture()
		{
			string dir = Setup("UW");
			if (dir == null) return;
			var tex = C.LoadTextures();
			var before = Snapshot(dir);
			var door = Pattern(32, 64, tex.Palettes[0], 3);
			tex.Doors.Textures[2] = door;
			CW.SaveTextureData(tex);
			var after = Snapshot(dir);
			byte[] a = before["DOORS.GR"], b = after["DOORS.GR"];
			int[] oa = GrFile.Offsets(a), ob = GrFile.Offsets(b);
			Assert.Equal(oa.Length, ob.Length);
			for (int i = 0; i + 1 < oa.Length; i++)
			{
				var pa = GrFile.Decode(a, oa, i); var pb = GrFile.Decode(b, ob, i);
				if (i == 2)
					Assert.Equal(CW.ToPaletteIndexes(door), pb.Pixels);
				else
					Assert.Equal(pa.Pixels, pb.Pixels);
			}
			Assert.Empty(current::GrPictures.Problems);
			Same.Bytes(before["W64.TR"], after["W64.TR"], "W64.TR");
		}

		[GameFact("UW")]
		public void An_added_uw1_wall_rewrites_the_pair_whole_and_then_patches_again()
		{
			string dir = Setup("UW");
			if (dir == null) return;
			var tex = C.LoadTextures();
			int count = tex.Walls.Count;
			var added = Pattern(64, 64, tex.Palettes[0], 4);
			tex.Walls.Textures.Add(added);
			CW.SaveTextureData(tex);
			Assert.Equal(count + 1, current::Uw1TrFiles.Offsets(File.ReadAllBytes(GameData.InData(dir, "W64.TR"))).Length);
			Assert.Equal(count + 1, current::Uw1TrFiles.Offsets(File.ReadAllBytes(GameData.InData(dir, "W16.TR"))).Length);
			var again = C.LoadTextures();
			Assert.Equal(count + 1, again.Walls.Count);
			Assert.Equal(added.GetPixels32(), again.Walls.Textures[count].GetPixels32());
			// the pair is remembered as written: an unchanged save leaves it alone, and the next
			// replaced picture is patched in place
			var once = Snapshot(dir);
			CW.SaveTextureData(tex);
			Same.Bytes(once["W64.TR"], Snapshot(dir)["W64.TR"], "W64.TR after a save with no change");
			tex.Walls.Textures[0] = Pattern(64, 64, tex.Palettes[0], 5);
			CW.SaveTextureData(tex);
			Assert.Equal(once["W64.TR"].Length, File.ReadAllBytes(GameData.InData(dir, "W64.TR")).Length);
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Unchanged_strings_are_not_written(string game)
		{
			string dir = Setup(game);
			if (dir == null) return;
			var s = C.LoadStrings();
			byte[] before = File.ReadAllBytes(GameData.InData(dir, "STRINGS.PAK"));
			CW.SaveStringData(s);
			Same.Bytes(before, File.ReadAllBytes(GameData.InData(dir, "STRINGS.PAK")), game + " STRINGS.PAK");
			Assert.False(HasBackup(dir, "STRINGS.PAK"));
			Assert.False(s.ChangedSinceSnapshot());
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void A_changed_string_is_written_as_the_old_writer_writes_it(string game)
		{
			// the old writer, on its own copy
			string oldDir = Setup(game);
			if (oldDir == null) return;
			var ls = L.LoadStrings();
			ls.Blocks[2].Strings[1] = ls.Blocks[2].Strings[1] + " (edited)";
			LW.SaveStringData(ls);
			byte[] want = File.ReadAllBytes(GameData.InData(oldDir, "STRINGS.PAK"));

			string dir = Setup(game);
			var s = C.LoadStrings();
			s.Blocks[2].Strings[1] = s.Blocks[2].Strings[1] + " (edited)";
			Assert.True(s.ChangedSinceSnapshot());
			CW.SaveStringData(s);
			string path = GameData.InData(dir, "STRINGS.PAK");
			Same.Bytes(want, File.ReadAllBytes(path), game + " STRINGS.PAK");
			Assert.True(HasBackup(dir, "STRINGS.PAK"));
			// saved: the snapshot follows, so the next save is skipped
			Assert.False(s.ChangedSinceSnapshot());
			// a new block (a new conversation's strings) counts as a change
			var block = s.AddNewBlock(250);
			block.Strings.Add("Hello."); block.StringCount = 1;
			Assert.True(s.ChangedSinceSnapshot());
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Every_string_encodes_as_before_and_can_be_checked_without_encoding(string game)
		{
			string dir = Setup(game);
			if (dir == null) return;
			var s = C.LoadStrings();
			var ls = L.LoadStrings();
			int n = 0;
			foreach (var b in s.Blocks)
				for (int i = 0; i < b.StringCount; i++)
				{
					Assert.True(StringsPak.CanEncode(b.Strings[i], s.Nodes));
					Assert.Equal(LW.ConvertString(b.Strings[i], ls.Nodes), CW.ConvertString(b.Strings[i], s.Nodes));
					n++;
				}
			Assert.True(n > 5000);
			Assert.False(StringsPak.CanEncode("€", s.Nodes));
			Assert.Null(CW.ConvertString("€", s.Nodes));
		}
	}
}
