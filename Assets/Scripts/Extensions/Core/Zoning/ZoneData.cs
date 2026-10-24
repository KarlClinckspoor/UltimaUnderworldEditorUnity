using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace UltimateEditor.Uw1.Zoning
{
	/// <summary>A zone: an area the player moves in freely. Ids are the labels of the painted maps.</summary>
	public class Zone
	{
		public int Id;
		public string Name = "";
		public string Levels = "";      // free text, e.g. "L1-L6"; the tile maps are the truth
		public string Status = "";      // done, unfinished, unpainted, unassigned
		public string Notes = "";
		public string Color = "";       // "#RRGGBB"; empty = the default palette colour for the id

		public override string ToString() { return Id + " " + Name; }
	}

	/// <summary>A connection between two zones with what it takes to use it.</summary>
	public class ZoneLink
	{
		public int Id;
		public int From;
		public int To;
		public string Kind = "walk";    // walk door portcullis secret doorman drop climb teleport lever stairs other
		public string Requires = "free";
		public bool TwoWay;
		public string Notes = "";
		public string Confidence = "certain";   // certain, todo
		public int Level = 0;           // optional anchor: level (1-9) and tile of the passage, 0 = none
		public int X = -1, Y = -1;

		public static readonly string[] Kinds = { "walk", "door", "portcullis", "secret", "doorman", "drop", "climb", "teleport", "lever", "stairs", "other" };

		public override string ToString()
		{
			return string.Format("{0} {1} {2} ({3}: {4})", From, TwoWay ? "<->" : "->", To, Kind, Requires);
		}
	}

	/// <summary>What a zone gives the player once reached: a key, an item, a flag, an NPC to talk to.</summary>
	public class Placement
	{
		public int Zone;
		public string Grants = "";  // one token: key:5, item:medallion, flag:gazer_dead, npc:eb, talisman:cup
		public string Requires = "";    // optional: granted only when this holds too (Judy's key of Love needs item:picture_tom)
		public string Notes = "";
		public string Source = "";  // "" (hand), "levark" (found by the audit)
	}

	/// <summary>
	/// The zoning document: zone ids per tile for the nine levels, the zones, the links
	/// between them and the placements. Saved as JSON (uw1-zones.json next to LEV.ARK).
	/// </summary>
	public class ZoneSet
	{
		public const int Levels = 9;
		public const int Side = 64;
		public const byte Solid = 0;
		public const byte Unassigned = 1;

		public string Source = "";
		public int StartZone = 53;
		public int GoalZone = 87;
		/// <summary>Tile zone ids: [level 0-8][y * 64 + x]; null for a level never painted.</summary>
		public byte[][] Tiles = new byte[Levels][];
		public List<Zone> Zones = new List<Zone>();
		public List<ZoneLink> Links = new List<ZoneLink>();
		public List<Placement> Placements = new List<Placement>();
		public Dictionary<string, string> OpenSources = new Dictionary<string, string>();

		public Zone GetZone(int id) { return Zones.Find(z => z.Id == id); }

		public Zone EnsureZone(int id)
		{
			var z = GetZone(id);
			if (z == null)
			{
				z = new Zone { Id = id, Name = "Zone " + id, Status = "unfinished" };
				Zones.Add(z);
				Zones.Sort((a, b) => a.Id.CompareTo(b.Id));
			}
			return z;
		}

		public int NextZoneId()
		{
			var used = new HashSet<int>();
			foreach (var z in Zones) used.Add(z.Id);
			for (int l = 0; l < Levels; l++)
				if (Tiles[l] != null) foreach (var b in Tiles[l]) used.Add(b);
			for (int i = 2; i < 256; i++) if (!used.Contains(i)) return i;
			return -1;
		}

		public int NextLinkId()
		{
			int m = 0;
			foreach (var l in Links) m = Math.Max(m, l.Id);
			return m + 1;
		}

		public byte ZoneAt(int level1, int x, int y)
		{
			var t = Tiles[level1 - 1];
			if (t == null || x < 0 || y < 0 || x >= Side || y >= Side) return 0;
			return t[y * Side + x];
		}

		public void SetZone(int level1, int x, int y, byte zone)
		{
			if (Tiles[level1 - 1] == null) Tiles[level1 - 1] = new byte[Side * Side];
			Tiles[level1 - 1][y * Side + x] = zone;
		}

		/// <summary>Zone ids present on a level with their tile counts.</summary>
		public Dictionary<int, int> CountsOnLevel(int level1)
		{
			var d = new Dictionary<int, int>();
			var t = Tiles[level1 - 1];
			if (t == null) return d;
			foreach (var b in t)
			{
				int c;
				d.TryGetValue(b, out c);
				d[b] = c + 1;
			}
			return d;
		}

		/// <summary>Levels (1-9) a zone has tiles on.</summary>
		public List<int> LevelsOf(int zone)
		{
			var r = new List<int>();
			for (int l = 0; l < Levels; l++)
			{
				if (Tiles[l] == null) continue;
				foreach (var b in Tiles[l]) if (b == zone) { r.Add(l + 1); break; }
			}
			return r;
		}

		/// <summary>
		/// Connected components (4-neighbour) of one zone on one level, as lists of tile
		/// indexes (y * 64 + x). More than one component is not wrong by itself: the parts may
		/// join through another level or over a drop, but it is worth a look.
		/// </summary>
		public List<List<int>> Components(int level1, int zone)
		{
			var result = new List<List<int>>();
			var t = Tiles[level1 - 1];
			if (t == null) return result;
			var seen = new bool[Side * Side];
			var stack = new Stack<int>();
			for (int i = 0; i < t.Length; i++)
			{
				if (t[i] != zone || seen[i]) continue;
				var comp = new List<int>();
				stack.Push(i);
				seen[i] = true;
				while (stack.Count > 0)
				{
					int k = stack.Pop();
					comp.Add(k);
					int x = k % Side, y = k / Side;
					int[] nb = { x > 0 ? k - 1 : -1, x < Side - 1 ? k + 1 : -1, y > 0 ? k - Side : -1, y < Side - 1 ? k + Side : -1 };
					foreach (int n in nb)
						if (n >= 0 && !seen[n] && t[n] == zone) { seen[n] = true; stack.Push(n); }
				}
				result.Add(comp);
			}
			return result;
		}

		/// <summary>
		/// Flood fill from (x, y) over tiles of the same current id, 4-neighbour, with the new id.
		/// Returns the number of tiles changed.
		/// </summary>
		public int Fill(int level1, int x, int y, byte zone)
		{
			var t = Tiles[level1 - 1];
			if (t == null) return 0;
			byte from = t[y * Side + x];
			if (from == zone) return 0;
			int n = 0;
			var stack = new Stack<int>();
			stack.Push(y * Side + x);
			while (stack.Count > 0)
			{
				int k = stack.Pop();
				if (t[k] != from) continue;
				t[k] = zone;
				n++;
				int kx = k % Side, ky = k / Side;
				if (kx > 0) stack.Push(k - 1);
				if (kx < Side - 1) stack.Push(k + 1);
				if (ky > 0) stack.Push(k - Side);
				if (ky < Side - 1) stack.Push(k + Side);
			}
			return n;
		}

		/// <summary>Replaces every tile of zone a with zone b and redirects links and placements.</summary>
		public void Merge(int a, int b)
		{
			for (int l = 0; l < Levels; l++)
			{
				if (Tiles[l] == null) continue;
				for (int i = 0; i < Tiles[l].Length; i++) if (Tiles[l][i] == a) Tiles[l][i] = (byte)b;
			}
			foreach (var k in Links) { if (k.From == a) k.From = b; if (k.To == a) k.To = b; }
			Links.RemoveAll(k => k.From == k.To);
			foreach (var p in Placements) if (p.Zone == a) p.Zone = b;
			var za = GetZone(a);
			var zb = EnsureZone(b);
			if (za != null)
			{
				if (!string.IsNullOrEmpty(za.Notes)) zb.Notes = (zb.Notes + "\nMerged from zone " + a + " (" + za.Name + "): " + za.Notes).Trim();
				Zones.Remove(za);
			}
		}

		// ---------------------------------------------------------------- colours

		/// <summary>Glasbey-like distinct colours for ids 2..255 (id 1 is the 'unassigned' warning colour).</summary>
		public static void DefaultColor(int id, out byte r, out byte g, out byte b)
		{
			if (id == Unassigned) { r = 255; g = 0; b = 255; return; }
			if (id == Solid) { r = 0; g = 0; b = 0; return; }
			// golden-angle hue walk with three value/saturation rings: neighbours differ clearly
			double h = (id * 137.508) % 360.0;
			double s = 0.55 + 0.45 * ((id * 7) % 3) / 2.0;
			double v = 0.55 + 0.45 * ((id * 11) % 4) / 3.0;
			double c = v * s, hh = h / 60.0, x = c * (1 - Math.Abs(hh % 2 - 1)), m = v - c;
			double rr, gg, bb;
			if (hh < 1) { rr = c; gg = x; bb = 0; }
			else if (hh < 2) { rr = x; gg = c; bb = 0; }
			else if (hh < 3) { rr = 0; gg = c; bb = x; }
			else if (hh < 4) { rr = 0; gg = x; bb = c; }
			else if (hh < 5) { rr = x; gg = 0; bb = c; }
			else { rr = c; gg = 0; bb = x; }
			r = (byte)Math.Round((rr + m) * 255); g = (byte)Math.Round((gg + m) * 255); b = (byte)Math.Round((bb + m) * 255);
		}

		public void ColorOf(int id, out byte r, out byte g, out byte b)
		{
			var z = GetZone(id);
			if (z != null && TryParseColor(z.Color, out r, out g, out b)) return;
			DefaultColor(id, out r, out g, out b);
		}

		public static bool TryParseColor(string hex, out byte r, out byte g, out byte b)
		{
			r = g = b = 0;
			if (string.IsNullOrEmpty(hex)) return false;
			string h = hex.TrimStart('#');
			if (h.Length != 6) return false;
			try
			{
				r = byte.Parse(h.Substring(0, 2), NumberStyles.HexNumber);
				g = byte.Parse(h.Substring(2, 2), NumberStyles.HexNumber);
				b = byte.Parse(h.Substring(4, 2), NumberStyles.HexNumber);
				return true;
			}
			catch (FormatException) { return false; }
		}

		public static string ColorString(byte r, byte g, byte b) { return string.Format("#{0:X2}{1:X2}{2:X2}", r, g, b); }

		// ---------------------------------------------------------------- JSON

		public static ZoneSet Load(string path) { return FromJson(File.ReadAllText(path, Encoding.UTF8)); }

		public void Save(string path) { File.WriteAllText(path, ToJson(), new UTF8Encoding(false)); }

		public static ZoneSet FromJson(string json)
		{
			var root = MiniJson.Obj(MiniJson.Parse(json));
			if (root == null || MiniJson.Str(root, "format") != "uw1-zones")
				throw new InvalidDataException("Not a uw1-zones file (\"format\": \"uw1-zones\" missing).");
			var zs = new ZoneSet();
			zs.Source = MiniJson.Str(root, "source");
			zs.StartZone = MiniJson.Int(MiniJson.Obj(MiniJson.Get(root, "start")), "zone", 53);
			zs.GoalZone = MiniJson.Int(MiniJson.Obj(MiniJson.Get(root, "goal")), "zone", 87);
			var zones = MiniJson.Arr(MiniJson.Get(root, "zones"));
			if (zones != null)
				foreach (var o in zones)
				{
					var d = MiniJson.Obj(o);
					zs.Zones.Add(new Zone
					{
						Id = MiniJson.Int(d, "id"), Name = MiniJson.Str(d, "name"), Levels = MiniJson.Str(d, "levels"),
						Status = MiniJson.Str(d, "status"), Notes = MiniJson.Str(d, "notes"), Color = MiniJson.Str(d, "color")
					});
				}
			var links = MiniJson.Arr(MiniJson.Get(root, "links"));
			if (links != null)
				foreach (var o in links)
				{
					var d = MiniJson.Obj(o);
					zs.Links.Add(new ZoneLink
					{
						Id = MiniJson.Int(d, "id"), From = MiniJson.Int(d, "from"), To = MiniJson.Int(d, "to"),
						Kind = MiniJson.Str(d, "kind", "walk"), Requires = MiniJson.Str(d, "requires", "free"),
						TwoWay = MiniJson.Bool(d, "twoWay"), Notes = MiniJson.Str(d, "notes"),
						Confidence = MiniJson.Str(d, "confidence", "certain"),
						Level = MiniJson.Int(d, "level"), X = MiniJson.Int(d, "x", -1), Y = MiniJson.Int(d, "y", -1)
					});
				}
			var places = MiniJson.Arr(MiniJson.Get(root, "placements"));
			if (places != null)
				foreach (var o in places)
				{
					var d = MiniJson.Obj(o);
					zs.Placements.Add(new Placement { Zone = MiniJson.Int(d, "zone"), Grants = MiniJson.Str(d, "grants"), Requires = MiniJson.Str(d, "requires"), Notes = MiniJson.Str(d, "notes"), Source = MiniJson.Str(d, "source") });
				}
			var open = MiniJson.Obj(MiniJson.Get(root, "openSources"));
			if (open != null) foreach (var kv in open) zs.OpenSources[kv.Key] = kv.Value == null ? "" : kv.Value.ToString();
			var tiles = MiniJson.Obj(MiniJson.Get(root, "tiles"));
			var levels = MiniJson.Arr(MiniJson.Get(tiles, "levels"));
			if (levels != null)
				for (int l = 0; l < Levels && l < levels.Count; l++)
				{
					var rows = MiniJson.Arr(levels[l]);
					if (rows == null) continue;
					var t = new byte[Side * Side];
					for (int y = 0; y < Side && y < rows.Count; y++)
					{
						string row = rows[y] as string ?? "";
						for (int x = 0; x < Side && 2 * x + 1 < row.Length; x++)
							t[y * Side + x] = byte.Parse(row.Substring(2 * x, 2), NumberStyles.HexNumber);
					}
					zs.Tiles[l] = t;
				}
			foreach (var k in zs.Links) { zs.EnsureZone(k.From); zs.EnsureZone(k.To); }
			return zs;
		}

		public string ToJson()
		{
			var root = new Dictionary<string, object>();
			root["format"] = "uw1-zones";
			root["version"] = 1;
			root["game"] = "UW1";
			root["source"] = Source;
			root["start"] = new Dictionary<string, object> { { "zone", StartZone } };
			root["goal"] = new Dictionary<string, object> { { "zone", GoalZone } };
			root["requirementLanguage"] = new List<object>(Requirement.LanguageHelp);
			var zones = new List<object>();
			foreach (var z in Zones)
			{
				var d = new Dictionary<string, object>();
				d["id"] = z.Id; d["name"] = z.Name; d["levels"] = z.Levels; d["status"] = z.Status; d["notes"] = z.Notes;
				if (!string.IsNullOrEmpty(z.Color)) d["color"] = z.Color;
				zones.Add(d);
			}
			root["zones"] = zones;
			var links = new List<object>();
			foreach (var k in Links)
			{
				var d = new Dictionary<string, object>();
				d["id"] = k.Id; d["from"] = k.From; d["to"] = k.To; d["kind"] = k.Kind; d["requires"] = k.Requires;
				d["twoWay"] = k.TwoWay; d["notes"] = k.Notes; d["confidence"] = k.Confidence;
				if (k.Level > 0) { d["level"] = k.Level; d["x"] = k.X; d["y"] = k.Y; }
				links.Add(d);
			}
			root["links"] = links;
			var places = new List<object>();
			foreach (var p in Placements)
			{
				var d = new Dictionary<string, object>();
				d["zone"] = p.Zone; d["grants"] = p.Grants;
				if (!string.IsNullOrEmpty(p.Requires)) d["requires"] = p.Requires;
				d["notes"] = p.Notes;
				if (!string.IsNullOrEmpty(p.Source)) d["source"] = p.Source;
				places.Add(d);
			}
			root["placements"] = places;
			var open = new Dictionary<string, object>();
			foreach (var kv in OpenSources) open[kv.Key] = kv.Value;
			root["openSources"] = open;
			var tiles = new Dictionary<string, object>();
			tiles["encoding"] = "per level: 64 rows (y = 0..63), each 64 zone ids as two hex digits (x = 0..63); 00 solid, 01 open and unassigned";
			var levels = new List<object>();
			for (int l = 0; l < Levels; l++)
			{
				if (Tiles[l] == null) { levels.Add(null); continue; }
				var rows = new List<object>();
				for (int y = 0; y < Side; y++)
				{
					var sb = new StringBuilder(Side * 2);
					for (int x = 0; x < Side; x++) sb.Append(Tiles[l][y * Side + x].ToString("X2"));
					rows.Add(sb.ToString());
				}
				levels.Add(rows);
			}
			tiles["levels"] = levels;
			root["tiles"] = tiles;
			return MiniJson.Write(root);
		}

		// ---------------------------------------------------------------- import

		/// <summary>
		/// Reads one level's zone ids from an 8-bit indexed BMP as Gimp saves them (the pixel
		/// index is the zone id; the file's bottom-up row r is tile y = r). Returns an error
		/// text, or null.
		/// </summary>
		public string ImportBmp(int level1, byte[] bmp)
		{
			if (bmp.Length < 54 || bmp[0] != 'B' || bmp[1] != 'M') return "Not a BMP file.";
			int off = BitConverter.ToInt32(bmp, 10);
			int w = BitConverter.ToInt32(bmp, 18);
			int h = BitConverter.ToInt32(bmp, 22);
			int bpp = BitConverter.ToUInt16(bmp, 28);
			if (w != Side || Math.Abs(h) != Side) return string.Format("The picture is {0}x{1}, not 64x64.", w, h);
			if (bpp != 8) return "Not an 8-bit indexed BMP (" + bpp + " bits per pixel). Export from Gimp as an indexed image.";
			var t = new byte[Side * Side];
			for (int r = 0; r < Side; r++)
			{
				int y = h > 0 ? r : Side - 1 - r;
				Array.Copy(bmp, off + r * Side, t, y * Side, Side);
			}
			Tiles[level1 - 1] = t;
			foreach (var b in t) if (b > Unassigned) EnsureZone(b);
			return null;
		}

		/// <summary>Marks every open tile of a level that has no zone yet with Unassigned, and solid ones with Solid.</summary>
		public void InitLevelFromTiles(int level1, Func<int, int, bool> isOpen)
		{
			if (Tiles[level1 - 1] == null) Tiles[level1 - 1] = new byte[Side * Side];
			var t = Tiles[level1 - 1];
			for (int y = 0; y < Side; y++)
				for (int x = 0; x < Side; x++)
				{
					bool open = isOpen(x, y);
					int k = y * Side + x;
					if (!open) t[k] = Solid;
					else if (t[k] == Solid) t[k] = Unassigned;
				}
		}
	}
}
