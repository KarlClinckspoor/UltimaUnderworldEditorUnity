using System;
using System.Collections.Generic;
using System.Text;

namespace UltimateEditor.Uw1.Zoning
{
	/// <summary>A door object between tiles, with its lock as the game reads it (USEITEMS.C checkLock, DAMAGE.C damage_object).</summary>
	public class DoorInfo
	{
		public int Level, X, Y;
		public int ObjectIndex;
		public int Item;
		public string ItemName;
		public bool Open;           // 0x148-0x14F
		public bool HasLock;
		public bool Locked;         // lock flag 9
		public bool LockStays;      // lock flag 10: the lock survives unlocking
		public int KeyId;           // lock link & 0x1FF; 0 = no key opens it
		public int PickDifficulty;  // lock z: 15 never, 14 needs skill 31, else 3z
		public bool Breakable;      // COMOBJ quality class != 3 and bit 13 clear
		public int QualityClass;
		public List<int> Zones = new List<int>();   // zones of the door tile and its open neighbours
		public bool Declared;       // a link between two of those zones exists
		public string Suggest;      // requirement text the audit would write

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("L{0} ({1},{2}) {3}{4}", Level, X, Y, ItemName, Open ? " (open)" : "");
			if (HasLock) sb.AppendFormat(", lock: key {0}{1}, pick z{2}{3}", KeyId, Locked ? "" : " (unlocked)", PickDifficulty, LockStays ? ", lock stays" : "");
			sb.Append(Breakable ? ", breakable (class " + QualityClass + ")" : ", UNBREAKABLE");
			sb.Append(", zones ").Append(string.Join("/", Zones.ConvertAll(z => z.ToString()).ToArray()));
			if (Zones.Count >= 2) sb.Append(Declared ? ", link declared" : ", NO LINK DECLARED");
			return sb.ToString();
		}
	}

	public class ThingInfo
	{
		public int Level, X, Y, Zone;
		public int ObjectIndex, Item;
		public string Name;
		public string Token;        // key:N, talisman:xxx, item:xxx, npc:xxx
		public string Where;        // "on the floor", "in <container>", "carried by <npc>"
		public override string ToString() { return string.Format("L{0} ({1},{2}) zone {3}: {4} [{5}] {6}", Level, X, Y, Zone, Name, Token, Where); }
	}

	public class TransitionInfo
	{
		public int Level, X, Y, Zone;
		public int ToLevel, ToX, ToY, ToZone;
		public bool Declared;   // same zone, or a link between the zones
		public override string ToString()
		{
			return string.Format("L{0} ({1},{2}) zone {3} -> L{4} ({5},{6}) zone {7}{8}", Level, X, Y, Zone, ToLevel, ToX, ToY, ToZone,
				Zone == ToZone ? " (same zone)" : Declared ? " (link declared)" : " NO LINK DECLARED");
		}
	}

	public class AuditResult
	{
		public List<string> UnassignedByLevel = new List<string>();
		public int UnassignedTotal;
		public List<string> PaintedSolid = new List<string>();
		public List<string> Components = new List<string>();
		public List<DoorInfo> Doors = new List<DoorInfo>();
		public List<ThingInfo> Things = new List<ThingInfo>();
		public List<TransitionInfo> Transitions = new List<TransitionInfo>();
		public List<string> PlacementChecks = new List<string>();

		public string Report(ZoneSet zs)
		{
			var sb = new StringBuilder();
			sb.AppendLine("Open tiles without a zone: " + UnassignedTotal);
			foreach (var s in UnassignedByLevel) sb.AppendLine("  " + s);
			if (PaintedSolid.Count > 0)
			{
				sb.AppendLine("Zone painted on solid tiles (" + PaintedSolid.Count + "):");
				foreach (var s in PaintedSolid) sb.AppendLine("  " + s);
			}
			if (Components.Count > 0)
			{
				sb.AppendLine("Zones in more than one piece on a level (may be fine: joined through another level or a drop):");
				foreach (var s in Components) sb.AppendLine("  " + s);
			}
			sb.AppendLine();
			sb.AppendLine("Level changes (teleport traps) and in-level teleports, with the zones at both ends:");
			foreach (var t in Transitions) sb.AppendLine("  " + t);
			sb.AppendLine();
			sb.AppendLine("Doors between zones (from LEV.ARK; locks as checkLock reads them):");
			foreach (var d in Doors) if (d.Zones.Count >= 2) sb.AppendLine("  " + d + (d.Suggest != null ? "\n      suggested requirement: " + d.Suggest : ""));
			int inner = 0;
			foreach (var d in Doors) if (d.Zones.Count < 2) inner++;
			sb.AppendLine("  (" + inner + " more doors lie inside one zone)");
			sb.AppendLine();
			sb.AppendLine("Keys, talismans, quest items and named creatures, by zone:");
			foreach (var t in Things) sb.AppendLine("  " + t);
			sb.AppendLine();
			if (PlacementChecks.Count > 0)
			{
				sb.AppendLine("Placements against the map:");
				foreach (var s in PlacementChecks) sb.AppendLine("  " + s);
			}
			return sb.ToString();
		}
	}

	/// <summary>
	/// Checks the zoning against the loaded levels: unassigned tiles, doors and their locks
	/// across zone borders, level changes, and where the keys, talismans and NPCs are.
	/// Works on the editor's LevelData; names come through the delegates so it runs without Unity.
	/// </summary>
	public static class ZoneAudit
	{
		public const int FirstKey = 0x100, LastKey = 0x10E, Lockpick = 0x101, Lock = 0x10F;
		public const int FirstDoor = 0x140, LastClosedDoor = 0x147, LastDoor = 0x14F;
		public const int TeleportTrap = 0x181;
		public const int CreateObjectTrap = 0x187;

		/// <summary>Items worth listing by zone besides keys and talismans (fate 10 in COMOBJ).</summary>
		public static readonly Dictionary<int, string> NotableItems = new Dictionary<int, string>
		{
			{ 0x0E1, "item:key_of_truth" }, { 0x0E2, "item:key_of_love" }, { 0x0E3, "item:key_of_courage" },
			{ 0x0E4, "item:two_part_key" }, { 0x0E5, "item:two_part_key" }, { 0x0E6, "item:two_part_key" }, { 0x0E7, "item:key_of_infinity" },
			{ 0x110, "item:picture_tom" }, { 0x111, "item:crystal" }, { 0x112, "item:orb_rock" }, { 0x113, "item:gem_cutter" },
			{ 0x115, "item:burning_incense" }, { 0x116, "item:incense" }, { 0x117, "item:orb" }, { 0x118, "item:broken_blade" },
			{ 0x119, "item:broken_hilt" }, { 0x11A, "item:figurine" }, { 0x11C, "item:strong_thread" }, { 0x11D, "item:dragon_scales" },
			{ 0x11E, "item:resilient_sphere" }, { 0x12C, "item:medallion" }, { 0x124, "item:flute" }, { 0x126, "item:moonstone" },
			{ 0x0D8, "item:pole" }, { 0x02F, "item:lava_boots" }, { 0x128, "item:rock_hammer" }, { 0x122, "item:silver_seed" },
		};

		public static readonly Dictionary<int, string> TalismanNames = new Dictionary<int, string>
		{
			{ 0x00A, "sword" }, { 0x036, "ring" }, { 0x037, "shield" }, { 0x093, "taper" }, { 0x097, "taper" },
			{ 0x0AE, "cup" }, { 0x0BF, "wine" }, { 0x11F, "standard" }, { 0x136, "book" },
		};

		/// <summary>Where an object lies: its tile, following container chains and NPC inventories.</summary>
		public static Dictionary<int, MapTile> LocateObjects(LevelData level, out Dictionary<int, StaticObject> holder)
		{
			var where = new Dictionary<int, MapTile>();
			holder = new Dictionary<int, StaticObject>();
			var objs = level.Objects;
			for (int x = 0; x < 64; x++)
				for (int y = 0; y < 64; y++)
				{
					var tile = level.MapTiles[x, y];
					if (tile == null) continue;
					int i = tile.ObjectAdress, guard = 0;
					while (i > 0 && i < objs.Length && guard++ < 1100)
					{
						if (where.ContainsKey(i)) break;
						where[i] = tile;
						var o = objs[i];
						if (o == null) break;
						i = o.NextAdress;
					}
				}
			bool changed = true;
			int rounds = 0;
			while (changed && rounds++ < 20)
			{
				changed = false;
				foreach (var kv in new List<KeyValuePair<int, MapTile>>(where))
				{
					var o = objs[kv.Key];
					if (o == null || o.IsQuantity || o.Special <= 0 || o.Special >= objs.Length) continue;
					if (o.IsTrigger() || (o.IsTrap() && o.ObjectID != CreateObjectTrap)) continue;   // their links are targets, not contents; a create-object trap's link is its template
					int j = o.Special, guard = 0;
					while (j > 0 && j < objs.Length && guard++ < 200)
					{
						if (where.ContainsKey(j)) break;
						where[j] = kv.Value;
						holder[j] = o;
						changed = true;
						var c = objs[j];
						if (c == null) break;
						j = c.NextAdress;
					}
				}
			}
			return where;
		}

		public static AuditResult Run(ZoneSet zs, IList<LevelData> levels, Func<int, string> itemName, Func<int, string> npcName, Func<int, int> comObjByte6, Func<int, int> comObjByte7)
		{
			var res = new AuditResult();
			for (int li = 0; li < levels.Count && li < ZoneSet.Levels; li++)
			{
				var lv = levels[li];
				int level1 = li + 1;
				if (lv == null) continue;
				int unassigned = 0;
				var examples = new List<string>();
				for (int x = 0; x < 64; x++)
					for (int y = 0; y < 64; y++)
					{
						var tile = lv.MapTiles[x, y];
						bool open = tile != null && tile.TileType != TileType.Solid;
						int z = zs.ZoneAt(level1, x, y);
						if (open && z <= ZoneSet.Unassigned)
						{
							unassigned++;
							if (examples.Count < 6) examples.Add(string.Format("({0},{1})", x, y));
						}
						if (!open && z > ZoneSet.Unassigned) res.PaintedSolid.Add(string.Format("L{0} ({1},{2}) zone {3}", level1, x, y, z));
					}
				res.UnassignedTotal += unassigned;
				if (zs.Tiles[li] == null) res.UnassignedByLevel.Add(string.Format("L{0}: not painted at all", level1));
				else if (unassigned > 0) res.UnassignedByLevel.Add(string.Format("L{0}: {1} tiles, e.g. {2}", level1, unassigned, string.Join(" ", examples.ToArray())));
				foreach (var kv in zs.CountsOnLevel(level1))
				{
					if (kv.Key <= ZoneSet.Unassigned) continue;
					var comps = zs.Components(level1, kv.Key);
					if (comps.Count > 1)
					{
						var sizes = comps.ConvertAll(c => c.Count);
						sizes.Sort((a, b) => b.CompareTo(a));
						res.Components.Add(string.Format("L{0} zone {1}: {2} pieces of {3} tiles", level1, kv.Key, comps.Count, string.Join(", ", sizes.ConvertAll(s => s.ToString()).ToArray())));
					}
				}

				Dictionary<int, StaticObject> holder;
				var where = LocateObjects(lv, out holder);
				var objs = lv.Objects;
				for (int i = 1; i < objs.Length; i++)
				{
					var o = objs[i];
					MapTile tile;
					if (o == null || !where.TryGetValue(i, out tile)) continue;
					int id = o.ObjectID;
					int zone = zs.ZoneAt(level1, tile.Position.x, tile.Position.y);

					if (id >= FirstDoor && id <= LastDoor)
					{
						var d = new DoorInfo { Level = level1, X = tile.Position.x, Y = tile.Position.y, ObjectIndex = i, Item = id, ItemName = itemName(id), Open = id > LastClosedDoor };
						int closedId = id > LastClosedDoor ? id - 8 : id;
						d.QualityClass = (comObjByte6(closedId) >> 2) & 3;
						d.Breakable = d.QualityClass != 3 && !o.IsDoorOpen;
						if (!o.IsQuantity && o.Special > 0 && o.Special < objs.Length)
						{
							int j = o.Special, guard = 0;
							while (j > 0 && j < objs.Length && guard++ < 50)
							{
								var c = objs[j];
								if (c == null) break;
								if (c.ObjectID == Lock)
								{
									d.HasLock = true;
									d.Locked = (c.Flags & 1) != 0;
									d.LockStays = (c.Flags & 2) != 0;
									d.KeyId = c.Special & 0x1FF;
									d.PickDifficulty = c.ZPos;
									break;
								}
								j = c.NextAdress;
							}
						}
						var zonesHere = new List<int>();
						AddZone(zonesHere, zone);
						int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
						for (int k = 0; k < 4; k++)
						{
							int nx = tile.Position.x + dx[k], ny = tile.Position.y + dy[k];
							if (nx < 0 || ny < 0 || nx > 63 || ny > 63) continue;
							var nt = lv.MapTiles[nx, ny];
							if (nt == null || nt.TileType == TileType.Solid) continue;
							AddZone(zonesHere, zs.ZoneAt(level1, nx, ny));
						}
						zonesHere.Sort();
						d.Zones = zonesHere;
						if (zonesHere.Count >= 2)
						{
							d.Declared = false;
							foreach (var k in zs.Links)
								if (zonesHere.Contains(k.From) && zonesHere.Contains(k.To) && k.From != k.To) { d.Declared = true; break; }
							var parts = new List<string>();
							if (d.HasLock && d.Locked)
							{
								if (d.KeyId > 0) parts.Add("key:" + d.KeyId);
								if (d.PickDifficulty < 15) parts.Add("pick:" + d.PickDifficulty);
							}
							else parts.Add("free");
							if (d.Breakable && !(parts.Count == 1 && parts[0] == "free")) parts.Add("bash");
							if (parts.Count == 0) parts.Add("zone:" + zonesHere[0] + "  (no key, unpickable, unbreakable: a lever, trigger or conversation must open it)");
							d.Suggest = string.Join(" | ", parts.ToArray());
						}
						res.Doors.Add(d);
						continue;
					}

					string token = null;
					// UW2: 0x100 is the curious implement and keys start at 0x102; the talismans and
					// notable items below are UW1's
					bool uw2 = LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW2;
					if (id >= (uw2 ? 0x102 : FirstKey) && id <= LastKey && id != Lockpick) token = "key:" + o.Owner;
					else if (uw2) token = null;
					else if (TalismanNames.ContainsKey(id) || ((comObjByte7(id) >> 1) & 0xF) == 10)
					{
						string tn;
						token = "talisman:" + (TalismanNames.TryGetValue(id, out tn) ? tn : itemName(id).Replace(' ', '_').ToLowerInvariant());
					}
					else if (NotableItems.ContainsKey(id)) token = NotableItems[id];
					var mob = o as MobileObject;
					if (mob != null && mob.Whoami > 0 && mob.Whoami < 0xFF)
					{
						string n = (npcName(mob.Whoami) ?? "").Trim();
						token = "npc:" + (n.Length > 0 ? n.Replace(' ', '_').Replace('\'', '_').Replace(".", "").ToLowerInvariant() : itemName(id).Replace(' ', '_').ToLowerInvariant() + "_0x" + mob.Whoami.ToString("x2"));
					}
					if (token == null) continue;
					var t = new ThingInfo { Level = level1, X = tile.Position.x, Y = tile.Position.y, Zone = zone, ObjectIndex = i, Item = id, Token = token };
					t.Name = mob != null && mob.Whoami > 0 ? (npcName(mob.Whoami) + " (" + itemName(id) + ", whoami 0x" + mob.Whoami.ToString("X2") + ")") : itemName(id);
					StaticObject h;
					if (holder.TryGetValue(i, out h))
					{
						var hm = h as MobileObject;
						if (h.ObjectID == CreateObjectTrap) t.Where = "created by a trap here (template)";
						else t.Where = hm != null ? "carried by " + (hm.Whoami > 0 ? npcName(hm.Whoami) : itemName(h.ObjectID)) : "in " + itemName(h.ObjectID);
					}
					else t.Where = mob != null ? "" : "on the floor";
					res.Things.Add(t);
				}

				// teleports: the trap usually sits in a solid wall tile; the player stands on the
				// trigger's tile (a move trigger linked to the trap), so report from there.
				for (int i = 1; i < objs.Length; i++)
				{
					var o = objs[i];
					MapTile trapTile;
					if (o == null || o.ObjectID != TeleportTrap || !where.TryGetValue(i, out trapTile)) continue;
					int toLevel = o.ZPos == 0 ? level1 : o.ZPos;
					if (toLevel < 1 || toLevel > 9) continue;
					if (o.Quality == 0x3F && o.Owner == 0x3F) continue;   // moonstone destination
					var sources = new List<MapTile>();
					for (int j = 1; j < objs.Length; j++)
					{
						var tr = objs[j];
						MapTile tt;
						if (tr != null && tr.IsTrigger() && tr.Special == i && where.TryGetValue(j, out tt) && !sources.Contains(tt)) sources.Add(tt);
					}
					if (sources.Count == 0) sources.Add(trapTile);
					foreach (var src in sources)
					{
						var tr = new TransitionInfo { Level = level1, X = src.Position.x, Y = src.Position.y, Zone = zone0(zs, level1, src), ToLevel = toLevel, ToX = o.Quality, ToY = o.Owner };
						if (tr.Zone == 0) tr.Zone = NearestZone(zs, lv, level1, src.Position.x, src.Position.y);
						tr.ToZone = zs.ZoneAt(toLevel, tr.ToX, tr.ToY);
						if (tr.ToZone == 0 && levels.Count >= toLevel && levels[toLevel - 1] != null) tr.ToZone = NearestZone(zs, levels[toLevel - 1], toLevel, tr.ToX, tr.ToY);
						tr.Declared = tr.Zone == tr.ToZone;
						if (!tr.Declared)
							foreach (var k in zs.Links)
								if ((k.From == tr.Zone && k.To == tr.ToZone) || (k.TwoWay && k.From == tr.ToZone && k.To == tr.Zone)) { tr.Declared = true; break; }
						res.Transitions.Add(tr);
					}
				}
			}
			res.Doors.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
			res.Things.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : string.CompareOrdinal(a.Token, b.Token));
			res.Transitions.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : a.X.CompareTo(b.X));

			// placements against the map: keys declared in a zone but found elsewhere, keys on the map with no placement
			var found = new Dictionary<string, HashSet<int>>();
			foreach (var t in res.Things)
			{
				HashSet<int> set;
				if (!found.TryGetValue(t.Token, out set)) found[t.Token] = set = new HashSet<int>();
				set.Add(t.Zone);
			}
			foreach (var p in zs.Placements)
			{
				string tok = Requirement.Normalize(p.Grants);
				if (!tok.StartsWith("key:") && !tok.StartsWith("talisman:")) continue;
				HashSet<int> set;
				if (!found.TryGetValue(tok, out set)) res.PlacementChecks.Add(tok + " placed in zone " + p.Zone + ": nothing like it on the map");
				else if (!set.Contains(p.Zone)) res.PlacementChecks.Add(tok + " placed in zone " + p.Zone + ": the map has it in zone(s) " + string.Join(", ", new List<int>(set).ConvertAll(z => z.ToString()).ToArray()));
			}
			foreach (var kv in found)
			{
				if (!kv.Key.StartsWith("key:") && !kv.Key.StartsWith("talisman:")) continue;
				bool any = false;
				foreach (var p in zs.Placements) if (Requirement.Normalize(p.Grants) == kv.Key) { any = true; break; }
				if (!any) res.PlacementChecks.Add(kv.Key + " is on the map in zone(s) " + string.Join(", ", new List<int>(kv.Value).ConvertAll(z => z.ToString()).ToArray()) + " but has no placement");
			}
			return res;
		}

		static int zone0(ZoneSet zs, int level1, MapTile t) { return zs.ZoneAt(level1, t.Position.x, t.Position.y); }

		/// <summary>The zone of the nearest open tile (for a point inside a wall, such as a trap's tile).</summary>
		static int NearestZone(ZoneSet zs, LevelData lv, int level1, int x, int y)
		{
			for (int r = 1; r <= 2; r++)
				for (int dx = -r; dx <= r; dx++)
					for (int dy = -r; dy <= r; dy++)
					{
						int nx = x + dx, ny = y + dy;
						if (nx < 0 || ny < 0 || nx > 63 || ny > 63) continue;
						int z = zs.ZoneAt(level1, nx, ny);
						if (z > ZoneSet.Unassigned) return z;
					}
			return 0;
		}

		static void AddZone(List<int> list, int z)
		{
			if (z <= ZoneSet.Unassigned) return;
			if (!list.Contains(z)) list.Add(z);
		}

		/// <summary>Placements the audit would add: keys, talismans, notable items and NPCs with their zones, not already present.</summary>
		public static List<Placement> SuggestPlacements(ZoneSet zs, AuditResult audit)
		{
			var result = new List<Placement>();
			var have = new HashSet<string>();
			foreach (var p in zs.Placements) have.Add(Requirement.Normalize(p.Grants) + "@" + p.Zone);
			foreach (var t in audit.Things)
			{
				if (t.Zone <= ZoneSet.Unassigned) continue;
				string key = t.Token + "@" + t.Zone;
				if (have.Contains(key)) continue;
				have.Add(key);
				result.Add(new Placement { Zone = t.Zone, Grants = t.Token, Notes = string.Format("L{0} ({1},{2}) {3} {4}", t.Level, t.X, t.Y, t.Name, t.Where).Trim(), Source = "levark" });
			}
			return result;
		}
	}
}
