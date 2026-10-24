using System;
using System.Collections.Generic;
using UltimateEditor.Games;

/// <summary>
/// The Fixes tab's checks, both games. They look at the level block the editor would save
/// (LevArk.PreviewBlock), so they see what the game will see, and fix the editor's model.
///
/// The game's rule (Obj_ListOkay, obj/OBJECTS.C): every object index from 2 is either on a
/// free stack or reached from a tile list, exactly once (the port's tidy-objects puts back
/// unreferenced statics the same way). The active mobile list holds every mobile in use
/// (Obj_Alloc adds, Obj_Free removes: active_critter / free_critter). Traps count the
/// triggers aimed at them in their flags (delete_trap relies on it).
/// </summary>
public static class LevelFixes
{
	public sealed class Issue
	{
		public int Level;
		public string Kind;
		public string Text;
		public int Object = -1;
		public int X = -1, Y = -1;
		/// <summary>Null when there is no automatic fix.</summary>
		public Action Fix;
		public override string ToString() { return string.Format("L{0} {1}: {2}", Level, Kind, Text); }
	}

	public static List<Issue> Check(LevelData ld, GameKind game)
	{
		var list = new List<Issue>();
		if (ld == null || !ld.Present) return list;
		int L = ld.Level;

		// ---- object lists, on the block the editor would save
		int[] free, reached;
		var block = new LevelBlock(LevArk.PreviewBlock(ld), game);
		block.Counts(out free, out reached);
		bool rebuildFree = false;
		for (int i = 2; i < LevelBlock.Objects; i++)
		{
			var o = ld.Objects[i];
			if (free[i] > 1) { rebuildFree = true; list.Add(new Issue { Level = L, Kind = "Free list", Object = i, Text = string.Format("index {0} is on the free list {1} times", i, free[i]) }); }
			if (free[i] > 0 && reached[i] > 0) { rebuildFree = true; list.Add(new Issue { Level = L, Kind = "Free list", Object = i, Text = string.Format("index {0} ({1}) is in use but also on the free list: the game would hand it out again", i, Name(o)) }); }
			if (reached[i] > 1) list.Add(new Issue { Level = L, Kind = "Reached twice", Object = i, Text = string.Format("object {0} ({1}) is reached {2} times from the tile lists (a shared or looping chain)", i, Name(o), reached[i]) });
			if (free[i] == 0 && reached[i] == 0)
			{
				if (o == null)
				{
					int idx = i;
					list.Add(new Issue { Level = L, Kind = "Lost slot", Object = i, Text = string.Format("index {0} is neither free nor used: the game can never use it", i), Fix = () => PushFree(ld, idx) });
				}
				else
				{
					int idx = i;
					bool mobile = i < LevelBlock.Mobiles;
					list.Add(new Issue
					{
						Level = L, Kind = "Unreferenced", Object = i,
						Text = string.Format("{0} {1} ({2}) is on no tile and in no container{3}", mobile ? "mobile" : "static", i, Name(o),
							mobile ? " (the game's tidy-up leaves mobiles alone; freeing it is safe if nothing refers to it)" : ""),
						Fix = () => FreeObject(ld, idx),
					});
				}
			}
		}
		if (rebuildFree)
			list.Add(new Issue { Level = L, Kind = "Free list", Text = "rebuild both free lists from the objects in use", Fix = () => RebuildFreeLists(ld) });

		// ---- active mobile list
		var active = new Dictionary<int, int>();
		for (int k = 0; k < ld.ActiveMobs && k < ld.MobList.Length; k++)
		{
			int v = ld.MobList[k];
			int c; active.TryGetValue(v, out c); active[v] = c + 1;
		}
		bool rebuildActive = false;
		foreach (var kv in active)
		{
			if (kv.Value > 1) { rebuildActive = true; list.Add(new Issue { Level = L, Kind = "Active list", Object = kv.Key, Text = string.Format("mobile {0} is on the active list {1} times", kv.Key, kv.Value) }); }
			if (kv.Key < 2 || kv.Key >= LevelBlock.Mobiles || ld.Objects[kv.Key] == null) { rebuildActive = true; list.Add(new Issue { Level = L, Kind = "Active list", Object = kv.Key, Text = string.Format("the active list holds {0}, which is not a mobile in use", kv.Key) }); }
		}
		var onTiles = block.MobilesOnTiles();
		foreach (int i in onTiles)
			if (!active.ContainsKey(i)) { rebuildActive = true; list.Add(new Issue { Level = L, Kind = "Active list", Object = i, Text = string.Format("mobile {0} ({1}) is on the map but not on the active list: it will not move or act", i, Name(ld.Objects[i])) }); }
		foreach (var kv in active)
			if (kv.Key >= 2 && kv.Key < LevelBlock.Mobiles && ld.Objects[kv.Key] != null && !onTiles.Contains(kv.Key))
			{ rebuildActive = true; list.Add(new Issue { Level = L, Kind = "Active list", Object = kv.Key, Text = string.Format("mobile {0} ({1}) is inside a container or a trap's link but on the active list: the game would run it as a critter", kv.Key, Name(ld.Objects[kv.Key])) }); }
		if (rebuildActive)
		{
			var keep = onTiles;
			list.Add(new Issue { Level = L, Kind = "Active list", Text = "rebuild the active list from the mobiles on the map", Fix = () => RebuildActive(ld, keep) });
		}

		// ---- traps and triggers
		foreach (var o in ld.Objects)
		{
			if (o == null) continue;
			var t = StaticObject.GetObjectType(o.ObjectID);
			if (t == ObjectType.Trigger && o.IsQuantity && o.Special > 0)
			{
				var target = o.Special < ld.Objects.Length ? ld.Objects[o.Special] : null;
				if (target == null || !IsEvent(target.ObjectID))
					list.Add(new Issue { Level = L, Kind = "Trigger link", Object = o.CurrentAdress, Text = string.Format("trigger {0} ({1}) links to {2}, which is {3}", o.CurrentAdress, Name(o), o.Special, target == null ? "free" : "not a trap (" + Name(target) + ")") });
			}
			if (IsEvent(o.ObjectID) && o.IsQuantity)
			{
				// a chain that comes back to itself never ends
				var seen = new HashSet<int>();
				var cur = o;
				while (cur != null && cur.IsQuantity && cur.Special > 0 && cur.Special < ld.Objects.Length)
				{
					if (!seen.Add(cur.CurrentAdress)) { list.Add(new Issue { Level = L, Kind = "Trap loop", Object = o.CurrentAdress, Text = string.Format("the chain from {0} ({1}) loops back to {2}", o.CurrentAdress, Name(o), cur.CurrentAdress) }); break; }
					var nx = ld.Objects[cur.Special];
					if (nx == null) break;
					if (!IsEvent(nx.ObjectID)) break;
					cur = nx;
				}
			}
		}

		// ---- animation overlays on objects that are not animated
		foreach (var o in ld.Objects)
			if (o != null && o.Animation != null && !o.IsAnimated() && !o.IsDoor())
			{
				var so = o;
				list.Add(new Issue { Level = L, Kind = "Overlay", Object = o.CurrentAdress, Text = string.Format("{0} ({1}) has an animation overlay but is not animated", o.CurrentAdress, Name(o)), Fix = () => so.Animation = null });
			}

		// ---- items lying under the floor of an open tile (decals, bridges and traps sit where they like)
		for (int y = 0; y < 64; y++)
			for (int x = 0; x < 64; x++)
			{
				var tile = ld.MapTiles[x, y];
				var objs = tile.GetTileObjects() ?? new List<StaticObject>();
				if (tile.TileType == TileType.Open)
				{
					int floor = tile.FloorHeight * 8;
					foreach (var o in objs)
						if (o != null && o.ZPos < floor && o.IsItem())
						{
							var so = o; int f = floor;
							list.Add(new Issue { Level = L, Kind = "Below floor", Object = o.CurrentAdress, X = x, Y = y, Text = string.Format("{0} ({1}) at z {2} is below the floor (z {3}) of tile ({4}, {5})", o.CurrentAdress, Name(o), o.ZPos, floor, x, y), Fix = () => so.ZPos = Math.Min(127, f) });
						}
				}
			}
		return list;
	}

	/// <summary>Traps and triggers by id (0x180-0x1BF), whatever type the editor gives them (UW1's 0x183 is a "Do trap").</summary>
	static bool IsEvent(int id) { return id >= 0x180 && id < 0x1C0; }

	static string Name(StaticObject o) { return o == null ? "free" : StaticObject.GetName(o.ObjectID); }

	/// <summary>Pushes an index on its free stack (mobile 2-255, static 256-1023).</summary>
	public static void PushFree(LevelData ld, int i)
	{
		if (i < LevelBlock.Mobiles)
		{
			if (ld.MobileListStart >= 253) return;
			ld.MobileListStart++;
			ld.FreeObjects[ld.MobileListStart] = i;
		}
		else
		{
			if (ld.StaticListStart >= 767) return;
			ld.StaticListStart++;
			ld.FreeObjects[254 + ld.StaticListStart] = i;
		}
	}

	/// <summary>Frees an object that nothing reaches: its slot goes on the free list, a mobile leaves the active list.</summary>
	public static void FreeObject(LevelData ld, int i)
	{
		var o = ld.Objects[i];
		if (o == null) return;
		ld.Objects[i] = null;
		if (i < LevelBlock.Mobiles) RemoveActive(ld, i);
		PushFree(ld, i);
	}

	static void RemoveActive(LevelData ld, int i)
	{
		for (int k = 0; k < ld.ActiveMobs; k++)
			if (ld.MobList[k] == i)
			{
				ld.ActiveMobs--;
				ld.MobList[k] = ld.MobList[ld.ActiveMobs];
				ld.MobList[ld.ActiveMobs] = 0;
				var moved = ld.Objects[ld.MobList[k]] as MobileObject;
				if (moved != null) moved.MobListIndex = k;
				return;
			}
	}

	/// <summary>
	/// Rebuilds both free stacks: the entries already there that are free and unique, in their
	/// order (the game pops from the top), then any other free index. Free means no object
	/// in the editor's model at that index.
	/// </summary>
	public static void RebuildFreeLists(LevelData ld)
	{
		var mob = new List<int>(); var stat = new List<int>();
		var seen = new HashSet<int>();
		for (int k = 0; k <= ld.MobileListStart && k < 254; k++) { int i = ld.FreeObjects[k]; if (i >= 2 && i < 256 && ld.Objects[i] == null && seen.Add(i)) mob.Add(i); }
		for (int k = 0; k <= ld.StaticListStart && k < 768; k++) { int i = ld.FreeObjects[254 + k]; if (i >= 256 && i < 1024 && ld.Objects[i] == null && seen.Add(i)) stat.Add(i); }
		var mobMissing = new List<int>(); var statMissing = new List<int>();
		for (int i = 2; i < 256; i++) if (ld.Objects[i] == null && !seen.Contains(i)) mobMissing.Add(i);
		for (int i = 256; i < 1024; i++) if (ld.Objects[i] == null && !seen.Contains(i)) statMissing.Add(i);
		// missing ones go at the bottom, so the stack's top (the next to be used) stays as it was
		mobMissing.Reverse(); statMissing.Reverse();
		mob.InsertRange(0, mobMissing); stat.InsertRange(0, statMissing);
		for (int k = 0; k < mob.Count; k++) ld.FreeObjects[k] = mob[k];
		for (int k = 0; k < stat.Count; k++) ld.FreeObjects[254 + k] = stat[k];
		ld.MobileListStart = mob.Count - 1;
		ld.StaticListStart = stat.Count - 1;
	}

	/// <summary>The active list as the mobiles on the map, keeping the existing order first.</summary>
	public static void RebuildActive(LevelData ld, HashSet<int> onMap)
	{
		var list = new List<int>();
		for (int k = 0; k < ld.ActiveMobs && k < ld.MobList.Length; k++)
		{
			int v = ld.MobList[k];
			if (onMap.Contains(v) && ld.Objects[v] != null && !list.Contains(v)) list.Add(v);
		}
		for (int i = 2; i < 256; i++) if (onMap.Contains(i) && ld.Objects[i] != null && !list.Contains(i)) list.Add(i);
		for (int i = 2; i < 256; i++)
		{
			var m = ld.Objects[i] as MobileObject;
			if (m != null && !list.Contains(i)) { m.ActiveMob = false; m.MobListIndex = -1; }
		}
		for (int k = 0; k < ld.MobList.Length; k++) ld.MobList[k] = k < list.Count ? list[k] : 0;
		ld.ActiveMobs = list.Count;
		for (int k = 0; k < list.Count; k++)
		{
			var m = ld.Objects[list[k]] as MobileObject;
			if (m != null) { m.ActiveMob = true; m.MobListIndex = k; }
		}
	}
}
