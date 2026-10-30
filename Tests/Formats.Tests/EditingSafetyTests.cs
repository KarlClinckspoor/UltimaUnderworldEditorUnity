extern alias current;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UltimateEditor.Games;
using UnityEngine;
using Xunit;
using C = current::DataReader;
using CW = current::DataWriter;
using CLevArk = current::LevArk;
using CMap = current::MapCreator;
using CObj = current::StaticObject;
using CMob = current::MobileObject;

namespace Formats.Tests
{
	/// <summary>
	/// Editing must not write data the game reads differently (code review, work package 1):
	/// fields masked to their widths, empty UW1 archive blocks, chains that loop, long contents,
	/// deleting a critter template, triggers on stacks, moving traps, the item cleaner.
	/// </summary>
	public class EditingSafetyTests
	{
		/// <summary>Loads a game's levels as the editor does and makes them MapCreator's.</summary>
		static List<current::LevelData> Load(string game, out string dir)
		{
			dir = GameData.Copy(game);
			Application.dataPath = Path.Combine(dir, "unity-data");
			Application.streamingAssetsPath = Path.Combine(Repo.Root, "Assets", "StreamingAssets");
			C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			typeof(C).GetField("uiManager", BindingFlags.NonPublic | BindingFlags.Static)
				.SetValue(null, RuntimeHelpers.GetUninitializedObject(typeof(current::UIManager)));
			var levels = CLevArk.Load(C.FilePath);
			typeof(CMap).GetProperty("LevelData").SetValue(null, levels);
			CMap.ObjectToGO = new Dictionary<CObj, GameObject>();
			return levels;
		}

		static void UseObjectData()
		{
			typeof(CMap).GetProperty("ObjectData").SetValue(null, C.LoadObjectData());
		}

		[Fact]
		public void An_empty_uw1_block_is_deleted_not_left_pointing_at_the_next()
		{
			// a small UW1 archive: 3 blocks, the first two present
			var a = Archive.Open(new byte[] { 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, GameKind.UW1);
			a.Write(0, new byte[] { 1, 2, 3 });
			a.Write(1, new byte[] { 4, 5 });
			a.Write(1, new byte[0]);		// every map note erased
			Assert.False(a.Has(1));
			a.Write(2, new byte[] { 9, 9, 9, 9 });	// another level mapped later
			Assert.Null(a.Read(1));
			Assert.Equal(new byte[] { 1, 2, 3 }, a.Read(0));
			Assert.Equal(new byte[] { 9, 9, 9, 9 }, a.Read(2));
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Fields_out_of_range_do_not_reach_the_next_field(string game)
		{
			if (!GameData.Has(game)) return;
			string dir;
			var levels = Load(game, out dir);
			var ld = levels[0];
			var so = ld.Objects.First(o => o != null && o.CurrentAdress >= 256 && o.NextAdress != 0);
			var mo = ld.Objects.OfType<CMob>().First(o => o != null);
			int next = so.NextAdress, special = so.Special;
			so.Quality = 100; so.Owner = 70; so.Flags = 9; so.ZPos = 200;
			mo.Goal = 17; mo.Attitude = 5; mo.XHome = 70;
			byte[] block = CLevArk.BuildLevelBlock(ld, 1);
			var lb = new LevelBlock(block, game == "UW2" ? GameKind.UW2 : GameKind.UW1);
			Assert.Equal(next, lb.Next(so.CurrentAdress));
			Assert.Equal(special, lb.Link(so.CurrentAdress));
			int at = LevelBlock.ObjectOffset(so.CurrentAdress);
			Assert.Equal(0, block[at + 1] & 0x10);		// the enchanted bit is not set by flags 9
			Assert.Equal(so.Direction & 1, block[at + 2] >> 7);	// height 200 doesn't change the heading
		}

		[GameFact("UW")]
		public void A_level_whose_chain_loops_loads_and_cuts_the_loop()
		{
			string dir = GameData.Copy("UW");
			string lev = GameData.InData(dir, "LEV.ARK");
			var a = Archive.Load(lev, GameKind.UW1);
			byte[] level = a.Read(0);
			var lb = new LevelBlock(level, GameKind.UW1);
			// an object on a tile, made to point at itself
			int obj = Enumerable.Range(256, 768).First(i => lb.Next(i) != 0 && lb.Item(i) < 0x180);
			int at = LevelBlock.ObjectOffset(obj);
			level[at + 4] = (byte)((level[at + 4] & 0x3F) | ((obj & 3) << 6));
			level[at + 5] = (byte)(obj >> 2);
			a.Write(0, level);
			a.Save(lev);

			string ignored;
			var task = Task.Run(() => Load("UW", out ignored));
			Assert.True(task.Wait(TimeSpan.FromSeconds(30)), "the load did not finish");
		}

		[GameFact("UW")]
		public void Contents_of_any_length_are_listed_and_quantity_links_are_not_contents()
		{
			string dir;
			var ld = Load("UW", out dir)[0];
			var bag = ld.Objects.First(o => o != null && o.IsContainer() && !o.IsQuantity);
			var free = Enumerable.Range(256, 768).Where(i => ld.Objects[i] == null).Take(45).ToList();
			Assert.Equal(45, free.Count);
			// 45 items chained into the bag (the old walks gave up after 40 steps)
			CObj prev = bag;
			foreach (int i in free)
			{
				var item = (CObj)RuntimeHelpers.GetUninitializedObject(typeof(CObj));
				typeof(CObj).GetProperty("ObjectID").SetValue(item, 0xA0);
				item.CurrentAdress = i; item.CurrentLevel = 1; item.Tile = bag.Tile;
				ld.Objects[i] = item;
				if (prev == bag) { bag.Special = i; } else prev.NextAdress = i;
				item.PrevAdress = prev.CurrentAdress;
				prev = item;
			}
			Assert.Equal(45, bag.GetContainedObjects().Count);
			Assert.Same(prev, bag.GetLastContainedObject());
			Assert.Same(bag, prev.GetContainer());

			var stack = ld.Objects.First(o => o != null && o.IsQuantity && o.Special > 1 && o.Special < 512);
			Assert.Null(stack.GetContainedObject());
			Assert.Empty(stack.GetContainedObjects());
		}

		[GameFact("UW")]
		public void Deleting_a_critter_template_does_not_put_it_on_the_active_list()
		{
			string dir;
			var levels = Load("UW", out dir);
			for (int l = 0; l < levels.Count; l++)
			{
				var ld = levels[l];
				var template = ld.Objects.OfType<CMob>().FirstOrDefault(m => m != null && m.CurrentAdress > 1 && m.GetContainer() != null && m.GetContainer().ObjectID >= 0x180);
				if (template == null) continue;
				int index = template.CurrentAdress, active = ld.ActiveMobs;
				Assert.True(CMap.RemoveObject(template, true));
				ld.Objects[index] = null;
				Assert.Equal(active, ld.ActiveMobs);
				Assert.DoesNotContain(index, ld.MobList.Take(ld.ActiveMobs));
				return;
			}
			Assert.Fail("no critter template in a trap on any UW1 level");
		}

		[GameFact("UW")]
		public void A_trigger_is_not_added_to_a_stack()
		{
			string dir;
			var ld = Load("UW", out dir)[0];
			var stack = ld.Objects.First(o => o != null && o.IsQuantity && o.Special > 1);
			int special = stack.Special;
			var mc = (CMap)RuntimeHelpers.GetUninitializedObject(typeof(CMap));
			var trigger = ld.Objects.First(o => o != null && o.ObjectID >= 0x1A0 && o.ObjectID < 0x1C0);
			Assert.False(mc.AddTrigger(stack, trigger));
			Assert.True(stack.IsQuantity);
			Assert.Equal(special, stack.Special);
		}

		[GameFact("UW")]
		public void A_moved_trap_keeps_its_height_and_fine_position()
		{
			string dir;
			var levels = Load("UW", out dir);
			var trap = levels.SelectMany(l => l.Objects).First(o => o != null && o.ObjectID == 0x181 && o.Tile != null && o.ZPos != o.Tile.FloorHeight * 8);	// a teleport trap to another level
			var ld = levels[trap.CurrentLevel - 1];
			int x = trap.XPos, y = trap.YPos, z = trap.ZPos;
			var to = ld.MapTiles[trap.Tile.Position.x == 1 ? 2 : 1, 1];
			CMap.ObjectToGO[trap] = new GameObject("trap");
			CMap.SetNewPosition(trap, trap.Tile, to, new Vector2Int((x + 3) % 8, (y + 3) % 8));
			Assert.Equal(new[] { x, y, z }, new[] { trap.XPos, trap.YPos, trap.ZPos });
			Assert.Same(to, trap.Tile);
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Undo_of_a_delete_puts_the_objects_back_and_keeps_later_edits(string game)
		{
			if (!GameData.Has(game)) return;
			string dir;
			var levels = Load(game, out dir);
			typeof(CMap).GetProperty("StringData").SetValue(null, C.LoadStrings());
			foreach (var l in levels)
				foreach (var anim in l.AnimationOverlays)
					if (anim && l.Objects[anim.Adress] != null) l.Objects[anim.Adress].Animation = anim;
			int level = 1;
			var ld = levels[level - 1];
			// a container holding something, on a tile
			var bag = ld.Objects.First(o => o != null && o.CurrentAdress >= 256 && !o.IsQuantity && o.Special > 0 && o.Tile != null && o.GetContainer() == null && o.GetContainedObjects().Count > 0);
			byte[] before = current::LevArk.BuildLevelBlock(ld, level);
			var c = new current::SnapshotCommand(level, "delete", () => CMap.RemoveObject(bag, true));
			c.Do();
			Assert.NotEqual(before, current::LevArk.BuildLevelBlock(ld, level));
			// a tile edited after the delete
			var tile = levels[level - 1].MapTiles[10, 10];
			int height = tile.FloorHeight;
			tile.FloorHeight = (height + 3) % 16;
			c.Undo();
			var back = CMap.LevelData[level - 1];
			Assert.NotSame(ld, back);
			byte[] after = current::LevArk.BuildLevelBlock(back, level);
			Assert.Equal((height + 3) % 16, back.MapTiles[10, 10].FloorHeight);   // the later edit stays
			back.MapTiles[10, 10].FloorHeight = height;
			after = current::LevArk.BuildLevelBlock(back, level);
			Same.Bytes(before, after, game + " level block after undo");
			var again = back.Objects[bag.CurrentAdress];
			Assert.NotNull(again);
			Assert.NotEmpty(again.GetContainedObjects());
		}

		[GameFact("UW")]
		public void The_item_cleaner_keeps_garamons_bones_and_the_talismans()
		{
			string dir;
			var levels = Load("UW", out dir);
			UseObjectData();
			// the default filter, and every category with containers' contents
			var f = new current::UltimateEditor.Uw1.UI.FixesWindow.CleanFilter();
			Assert.DoesNotContain(current::UltimateEditor.Uw1.UI.FixesWindow.Select(f), o => o.IsBones() && o.Owner == 0x3E);
			foreach (current::UltimateEditor.Uw1.ItemCategory c in Enum.GetValues(typeof(current::UltimateEditor.Uw1.ItemCategory))) f.Categories |= c;
			f.IncludeContainers = true;
			var listed = current::UltimateEditor.Uw1.UI.FixesWindow.Select(f);
			Assert.NotEmpty(listed);
			Assert.DoesNotContain(listed, o => o.IsBones() && o.Owner == 0x3E);
			var com = CMap.ObjectData.CommonData;
			Assert.DoesNotContain(listed, o => com[o.ObjectID].ObjectType == 10);
			// the bones are there to be kept
			Assert.Contains(levels.SelectMany(l => l.Objects), o => o != null && o.IsBones() && o.Owner == 0x3E);
		}
	}
}
