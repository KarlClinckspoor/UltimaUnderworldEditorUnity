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
using C = current::DataReader;
using CLevArk = current::LevArk;
using CMap = current::MapCreator;
using CObj = current::StaticObject;
using CMob = current::MobileObject;
using Rec = current::ObjectRecord;
using Fixes = current::LevelFixes;

namespace Formats.Tests
{
	/// <summary>
	/// Review package 10: one field table for object records (reader, writer, inspector, panel
	/// ranges), the trap field encodings, the object-list service, and the tools that write data
	/// (PLAYER.DAT, BGLOBALS.DAT, map notes, level fixes, the start square).
	/// </summary>
	public class RestructureTests
	{
		static List<current::LevelData> Load(string game)
		{
			string dir = GameData.Copy(game);
			Application.dataPath = Path.Combine(dir, "unity-data");
			Application.streamingAssetsPath = Path.Combine(Repo.Root, "Assets", "StreamingAssets");
			C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			typeof(C).GetField("uiManager", BindingFlags.NonPublic | BindingFlags.Static)
				.SetValue(null, RuntimeHelpers.GetUninitializedObject(typeof(current::UIManager)));
			var levels = CLevArk.Load(C.FilePath);
			typeof(CMap).GetProperty("LevelData").SetValue(null, levels);
			CMap.ObjectToGO = new Dictionary<CObj, GameObject>();
			CMap.MapTileToGO = new Dictionary<current::MapTile, GameObject>();
			return levels;
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Every_object_record_reads_and_writes_back_through_the_field_table(string game)
		{
			if (!GameData.Has(game)) return;
			int records = 0;
			foreach (var ld in Load(game).Where(l => l.Present && l.SourceBlock != null))
			{
				byte[] block = ld.SourceBlock;
				for (int i = 0; i < LevelBlock.Objects; i++)
				{
					int at = LevelBlock.ObjectOffset(i);
					int size = i < LevelBlock.Mobiles ? LevelBlock.MobileSize : LevelBlock.StaticSize;
					CObj o = i < LevelBlock.Mobiles ? new CMob() : new CObj();
					Rec.Read(o, block, at, true);
					var back = new byte[size];
					Rec.Write(o, back, 0);
					Same.Bytes(block.Skip(at).Take(size).ToArray(), back, game + " level " + ld.Level + " object " + i);
					// the object the editor loaded holds the same fields
					var loaded = ld.Objects[i];
					if (loaded != null)
					{
						Assert.Equal(new[] { o.ObjectID, o.Quality, o.Owner, o.Special, o.ZPos, o.Direction, o.XPos, o.YPos, o.Flags },
							new[] { loaded.ObjectID, loaded.Quality, loaded.Owner, loaded.Special, loaded.ZPos, loaded.Direction, loaded.XPos, loaded.YPos, loaded.Flags });
						var m = loaded as CMob;
						if (m != null)
							Assert.Equal(new[] { ((CMob)o).HP, ((CMob)o).Goal, ((CMob)o).GTarg, ((CMob)o).Attitude, ((CMob)o).XHome, ((CMob)o).YHome, ((CMob)o).Whoami },
								new[] { m.HP, m.Goal, m.GTarg, m.Attitude, m.XHome, m.YHome, m.Whoami });
					}
					records++;
				}
			}
			Assert.True(records > 1000);
		}

		[Fact]
		public void The_panels_ranges_are_the_field_widths()
		{
			int min, max;
			var expect = new Dictionary<string, int> { { "XPos", 7 }, { "YPos", 7 }, { "Direction", 7 }, { "Flags", 7 }, { "Height", 127 }, { "Quality", 63 }, { "OwnerSpecial", 63 }, { "QuantityLink", 1023 } };
			foreach (var kv in expect)
			{
				Assert.True(Rec.Range(kv.Key, false, out min, out max), kv.Key);
				Assert.Equal((0, kv.Value), (min, max));
			}
			var mobile = new Dictionary<string, int> { { "Attitude", 3 }, { "Goal", 15 }, { "HomeX", 63 }, { "HomeY", 63 }, { "HP", 255 }, { "NPC", 255 } };
			foreach (var kv in mobile)
			{
				Assert.False(Rec.Range(kv.Key, false, out min, out max), kv.Key + " is not a static object's");
				Assert.True(Rec.Range(kv.Key, true, out min, out max), kv.Key);
				Assert.Equal(kv.Value, max);
			}
			Assert.False(Rec.Range("Name", true, out min, out max));
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Trap_fields_have_one_encoding(string game)
		{
			if (!GameData.Has(game)) return;
			Load(game);
			var t = new CObj();
			// text string trap: quality << 5 | owner (UW2: the owner's low five bits)
			t.Quality = 1; t.Owner = 0x25;
			Assert.Equal(game == "UW2" ? 32 + 5 : 32 | 0x25, t.GetTextTrapIndex());
			t.SetTextTrapIndex(70);
			Assert.Equal(70, t.GetTextTrapIndex());
			Assert.Equal(2, t.Quality);
			if (game == "UW2") Assert.Equal(0x20, t.Owner & 0x20);   // the "only with the flag" bit stays
			// change terrain: Z >> 3, every height 0-13 reachable, above 13 keeps the tile's
			for (int h = 0; h <= CObj.MaxTerrainHeight; h++) { t.SetTerrainTrapHeight(h); Assert.Equal(h, t.GetTerrainTrapHeight()); }
			t.SetTerrainTrapHeight(99);
			Assert.True(t.GetTerrainTrapHeight() > CObj.MaxTerrainHeight);
			// damage trap: a non-zero owner poisons the player (event/WORLDEV.C whack_thing)
			t.SetDamageTrapPoisons(true); Assert.True(t.GetDamageTrapPoisons());
			t.SetDamageTrapPoisons(false); Assert.Equal(0, t.Owner);
			// trigger count: 4 bits, the enchanted bit is the 8s
			t.SetTrapTriggerCount(15); Assert.Equal(7, t.Flags); Assert.True(t.IsEnchanted); Assert.Equal(15, t.GetTrapTriggerCount());
			t.SetTrapTriggerCount(3); Assert.Equal(3, t.Flags); Assert.False(t.IsEnchanted);
			t.SetTrapTriggerCount(20); Assert.Equal(15, t.GetTrapTriggerCount());
			// variable traps: the value spreads over quality, owner and fine Y
			t.SetVarTrapValue(0x2A5); Assert.Equal(0x2A5, t.GetVarTrapValue());
		}

		[GameFact("UW")]
		public void Object_list_takes_out_and_refuses_to_put_a_container_in_itself()
		{
			var ld = Load("UW")[0];
			var bag = ld.Objects.First(o => o != null && o.IsContainer() && !o.IsQuantity && o.Tile != null && o.GetContainer() == null && o.GetContainedObjects().Count > 0);
			var inside = bag.GetContainedObjects()[0];
			int onTile = bag.Tile.GetTileObjects().Count;
			Assert.True(current::ObjectList.TakeOut(inside));
			Assert.Null(inside.GetContainer());
			Assert.Equal(onTile + 1, bag.Tile.GetTileObjects().Count);
			Assert.False(current::ObjectList.TakeOut(inside));   // in no container now
			// a bag never goes into something inside it
			var pouch = ld.Objects.FirstOrDefault(o => o != null && o != bag && o.IsContainer() && !o.IsQuantity && o.GetContainer() == null && o.Tile != null);
			if (pouch == null) return;
			Assert.True(current::ObjectList.PutIn(pouch, bag));
			Assert.Same(bag, pouch.GetContainer());
			Assert.False(current::ObjectList.PutIn(bag, pouch));
			Assert.Null(bag.GetContainer());
		}

		// ---- the tools that write data (review T3)

		[Theory]
		[InlineData(GameKind.UW1)]
		[InlineData(GameKind.UW2)]
		public void A_player_record_edit_changes_only_its_bytes_and_keeps_the_key(GameKind game)
		{
			var rnd = new System.Random(7);
			var plain = new byte[PlayerDat.RecordSizeFor(game)];
			rnd.NextBytes(plain);
			plain[0] = (byte)'K';
			var rest = new byte[2 + 0x5B + 8 * 1 + 0x18];
			rnd.NextBytes(rest);
			int count = (rest.Length - 2 - 0x5B) / 8;
			rest[0] = (byte)count; rest[1] = 0;
			byte key = (byte)('K' ^ 0xAA);
			var file = new[] { key }.Concat(PlayerDat.Cipher(plain, key, game, true)).Concat(rest).ToArray();

			Assert.Equal(game, PlayerDat.Detect(file));
			var p = PlayerDat.Parse(file, game);
			Assert.Equal(plain, p.Record);
			Assert.Equal(file, p.ToBytes());

			p.Strength = 30;
			p.SetSkill(3, 12);
			var edited = p.ToBytes();
			Assert.True(PlayerDat.KeyMatches(edited, game));
			var q = PlayerDat.Parse(edited, game);
			Assert.Equal(30, q.Strength);
			Assert.Equal(12, q.Skill(3));
			for (int i = 0; i < plain.Length; i++)
				if (i != 0x1E && i != 0x21 + 3) Assert.True(plain[i] == q.Record[i], "record byte " + i + " changed");
			Assert.Equal(rest, q.Rest);
			Assert.Throws<ArgumentOutOfRangeException>(() => q.Strength = 256);

			q.SetPosition(3, 20, 40, 5, false, 2);
			var r = PlayerDat.Parse(q.ToBytes(), game);
			Assert.Equal((3, 20, 40, 2), (r.SavedLevel, r.TileX, r.TileY, r.Heading));
		}

		[Fact]
		public void A_saves_conversation_variables_round_trip()
		{
			// three conversations' variables and a trailer, as BGLOBALS.DAT in a save
			var o = new List<byte>();
			Action<int, int[]> add = (slot, words) =>
			{
				o.Add((byte)slot); o.Add((byte)(slot >> 8)); o.Add((byte)words.Length); o.Add(0);
				foreach (int w in words) { o.Add((byte)w); o.Add((byte)(w >> 8)); }
			};
			add(1, new[] { 0, 5, -1 });
			add(16, new int[0]);
			add(300, new[] { 32767, -32768 });
			o.AddRange(new byte[] { 0xEE, 0x01 });
			byte[] d = o.ToArray();
			var g = BGlobals.Parse(d, true);
			Assert.Equal(3, g.Records.Count);
			Assert.Equal(new[] { 0, 5, -1 }, g.Find(1).Words);
			Assert.Equal(d, g.ToBytes(true));
			g.Find(300).Words[0] = 12;
			var again = BGlobals.Parse(g.ToBytes(true), true);
			Assert.Equal(new[] { 12, -32768 }, again.Find(300).Words);
			Assert.Equal(new byte[] { 0xEE, 0x01 }, again.Trailer);
		}

		[Fact]
		public void Map_notes_keep_their_leftover_bytes_drop_erased_ones_and_sit_on_their_tile()
		{
			var raw = new byte[MapNote.RecordSize * 3];
			var rnd = new System.Random(3);
			rnd.NextBytes(raw);
			for (int n = 0; n < 3; n++)
			{
				int o = n * MapNote.RecordSize;
				raw[o + 5] = 0;   // a short text, leftovers after it
				raw[o + 0x32] = (byte)(10 + 3 * n); raw[o + 0x33] = 0;
				raw[o + 0x34] = 7; raw[o + 0x35] = 0;
				for (int k = 0; k < 5; k++) raw[o + k] = (byte)('a' + k);
			}
			var notes = MapNote.ParseAll(raw);
			Assert.Equal(3, notes.Count);
			Assert.Equal(raw, MapNote.ToBytes(notes));                 // unchanged: byte for byte
			Assert.Equal((1, 1), (notes[0].TileX, notes[0].TileY));
			Assert.Equal(3, notes[2].TileX);
			notes[1].X = -1;                                          // erased
			notes[2].Text = "the exit";
			notes[2].TileX = 62; notes[2].TileY = 62;
			var back = MapNote.ParseAll(MapNote.ToBytes(notes));
			Assert.Equal(2, back.Count);
			Assert.Equal("the exit", back[1].Text);
			Assert.Equal((62, 62), (back[1].TileX, back[1].TileY));
			Assert.Equal(raw.Take(MapNote.RecordSize).ToArray(), MapNote.ToBytes(notes).Take(MapNote.RecordSize).ToArray());
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Level_fixes_find_a_used_index_on_the_free_list_and_rebuild_it(string game)
		{
			if (!GameData.Has(game)) return;
			var kind = game == "UW2" ? GameKind.UW2 : GameKind.UW1;
			var ld = Load(game).First(l => l.Present);
			Func<int, bool> reported = i => Fixes.Check(ld, kind).Any(x => x.Kind == "Free list" && x.Object == i);
			int used = Enumerable.Range(LevelBlock.Mobiles, LevelBlock.Objects - LevelBlock.Mobiles).First(i => ld.Objects[i] != null && ld.Objects[i].Tile != null);
			Assert.False(reported(used));
			byte[] objectsBefore = CLevArk.BuildLevelBlock(ld, ld.Level).Skip(LevelBlock.MobilesAt).Take(LevelBlock.MobFreeAt - LevelBlock.MobilesAt).ToArray();
			if (ld.StaticListStart > 767) ld.StaticListStart = -1;
			ld.StaticListStart++;
			ld.FreeObjects[254 + ld.StaticListStart] = used;
			Assert.True(reported(used));
			Fixes.RebuildFreeLists(ld);
			Assert.False(reported(used));
			byte[] objectsAfter = CLevArk.BuildLevelBlock(ld, ld.Level).Skip(LevelBlock.MobilesAt).Take(LevelBlock.MobFreeAt - LevelBlock.MobilesAt).ToArray();
			Same.Bytes(objectsBefore, objectsAfter, game + " objects after rebuilding the free lists");
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void The_start_square_patch_reads_back_and_changes_two_bytes(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			var kind = game == "UW2" ? GameKind.UW2 : GameKind.UW1;
			byte[] exe = File.ReadAllBytes(GameData.Exe(dir));
			byte[] patched = (byte[])exe.Clone();
			Assert.True(StartPosition.Patch(patched, kind, 33, 5));
			int x, y;
			Assert.True(StartPosition.Read(patched, kind, out x, out y));
			Assert.Equal((33, 5), (x, y));
			Assert.Equal(2, exe.Zip(patched, (a, b) => a != b).Count(c => c));
			// patched again (an already patched executable is found) and back to the original
			int ox, oy;
			StartPosition.Read(exe, kind, out ox, out oy);
			Assert.True(StartPosition.Patch(patched, kind, ox, oy));
			Assert.Equal(exe, patched);
			Assert.Throws<ArgumentOutOfRangeException>(() => StartPosition.Patch(patched, kind, 0, 5));
			Assert.Throws<ArgumentOutOfRangeException>(() => StartPosition.Patch(patched, kind, 5, 63));
			Assert.False(StartPosition.Patch(new byte[64], kind, 5, 5));
			// the editor reads it next to DATA
			Assert.True(StartPosition.Load(Path.Combine(dir, "DATA"), kind, out x, out y));
			Assert.Equal((ox, oy), (x, y));
		}
	}
}
