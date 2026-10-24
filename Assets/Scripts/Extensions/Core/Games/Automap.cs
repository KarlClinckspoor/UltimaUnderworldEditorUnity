using System;
using System.Collections.Generic;
using System.Text;

namespace UltimateEditor.Games
{
	/// <summary>
	/// The player's map of a level and the notes written on it, as LEV.ARK keeps them
	/// (UW1 blocks 0x1A + level and 0x23 + level; UW2 0x9F + level and 0xEF + level; uw1 and
	/// uw2 ui/AUTOMAP.C). A new game has none (UW1) or a few (UW2: level 1, and the map scraps'
	/// "levels" 0x47 + quality). The game writes them to the save directory's LEV.ARK as the
	/// player explores and writes notes.
	///
	/// The map is a byte per tile, 64 rows of 64 (row y = tile y, as Map_GetAddr):
	/// bits 0-3 the tile type plus one state: 0 never seen, 1-9 seen (the level's tile types;
	/// 0 solid is never drawn), 0xA-0xF known but not seen (tile_mapcode: solid 0xA, open 0xB,
	/// diagonals 0xC-0xF, slopes 0xB). UW2: bits 4-5 the object code (1 a door, 2 and 3 drawn
	/// as water or dark), bits 6-7 the floor's terrain class (TERRAIN.DAT & 0xC0: 1 water,
	/// 2 lava, 3 ice). UW1: the floor's terrain word or-ed in (bits 4-5) and the object
	/// code in bits 6-7 (GRIDDB.C grdb_elem).
	/// </summary>
	public sealed class AutomapLevel
	{
		public const int Size = 64, Bytes = 0x1000;
		public readonly byte[] Tiles;

		public AutomapLevel(byte[] data = null)
		{
			Tiles = new byte[Bytes];
			if (data != null) Array.Copy(data, Tiles, Math.Min(data.Length, Bytes));
		}

		public byte this[int x, int y] { get { return Tiles[y * Size + x]; } set { Tiles[y * Size + x] = value; } }

		public static int State(byte code) { return code & 0xF; }
		public static bool Seen(byte code) { int t = code & 0xF; return t > 0 && t < 0xA; }
		public static bool KnownUnseen(byte code) { return (code & 0xF) >= 0xA; }

		/// <summary>tile_mapcode (GRIDDB.C): the "known, not seen" code for a tile type.</summary>
		public static readonly byte[] UnseenCode = { 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x0B, 0x0B, 0x0B, 0x0B, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

		/// <summary>The code the game writes for a tile it sees: the tile type with the floor's terrain.</summary>
		public static byte SeenCode(GameKind game, int tileType, int terrainWord)
		{
			if (game == GameKind.UW2) return (byte)((tileType & 0xF) | (terrainWord & 0xC0));
			return (byte)((tileType & 0xF) | terrainWord);
		}

		/// <summary>
		/// Maps tiles x0..x1, y0..y1 as seen (automap_area, the Map Area spell; the game skips
		/// the border rows and columns). tileType(x, y) and terrain(x, y) come from the level.
		/// </summary>
		public void Reveal(GameKind game, int x0, int y0, int x1, int y1, Func<int, int, int> tileType, Func<int, int, int> terrain)
		{
			for (int y = Math.Max(1, y0); y <= Math.Min(0x3E, y1); y++)
				for (int x = Math.Max(1, x0); x <= Math.Min(0x3E, x1); x++)
					this[x, y] = SeenCode(game, tileType(x, y), terrain(x, y));
		}

		/// <summary>make_terrain_unseen (UW2 hack trap 45): seen types 0-5 become known, not seen.</summary>
		public void MakeUnseen(int x0, int y0, int x1, int y1)
		{
			for (int y = Math.Max(0, y0); y <= Math.Min(63, y1); y++)
				for (int x = Math.Max(0, x0); x <= Math.Min(63, x1); x++)
				{
					int t = this[x, y] & 0xF;
					if (t <= 5) t += 0xA;
					this[x, y] = (byte)((this[x, y] & 0xF0) | (t & 0xF));
				}
		}

		public void Clear(int x0, int y0, int x1, int y1)
		{
			for (int y = Math.Max(0, y0); y <= Math.Min(63, y1); y++)
				for (int x = Math.Max(0, x0); x <= Math.Min(63, x1); x++)
					this[x, y] = 0;
		}

		public int CountSeen()
		{
			int n = 0;
			foreach (var b in Tiles) if (Seen(b)) n++;
			return n;
		}

		/// <summary>
		/// map_section (UW2 AUTOMAP.C): which of a map scrap's 8 sections tile (x, y) is in:
		/// 0 within 9 tiles of the middle (32, 32), 1-7 the sectors round it.
		/// </summary>
		public static int MapSection(int tileX, int tileY)
		{
			int x = tileX - 0x20, y = tileY - 0x20;
			int s = 0, t;
			if (x * x + y * y <= 0x51) return 0;
			if (y < 0) { y = -y; x = -x; s = 0x1C; }
			if (x < 0) { t = y; y = -x; x = t; s += 0xE; }
			if (y < x) s += y * 7 / x;
			else s += 0xE - x * 7 / y;
			s = s / 8 + 1;
			return s > 7 ? 7 : s;
		}
	}

	/// <summary>A note on the map (struct ATM, 0x36 bytes): 0x32 bytes of text ending in 0, then
	/// the screen position x, y of its first letter (x -1 once erased; the game drops those).</summary>
	public sealed class MapNote
	{
		public const int RecordSize = 0x36, TextSize = 0x32, MaxNotes = 100;
		/// <summary>ManageDungeonMap lets the player type at most 46 characters.</summary>
		public const int GameMaxChars = 46;

		public string Text = "";
		public int X, Y;

		/// <summary>The tile under the note's position (the map is drawn at px = 3x + 10, py = 3y + 7).</summary>
		public int TileX { get { return (X - 10) / 3; } set { X = value * 3 + 10; } }
		public int TileY { get { return (Y - 7) / 3; } set { Y = value * 3 + 7; } }

		public static List<MapNote> ParseAll(byte[] data)
		{
			var list = new List<MapNote>();
			if (data == null) return list;
			int n = Math.Min(MaxNotes, data.Length / RecordSize);
			for (int i = 0; i < n; i++)
			{
				int o = i * RecordSize;
				var sb = new StringBuilder();
				for (int k = 0; k < TextSize && data[o + k] != 0; k++) sb.Append((char)data[o + k]);
				list.Add(new MapNote { Text = sb.ToString(), X = (short)(data[o + 0x32] | data[o + 0x33] << 8), Y = (short)(data[o + 0x34] | data[o + 0x35] << 8) });
			}
			return list;
		}

		/// <summary>The block as SaveTheWords writes it: the notes in order, erased ones (x &lt; 0) dropped.
		/// Text past 49 characters is cut so every note ends inside its 0x32 bytes.</summary>
		public static byte[] ToBytes(List<MapNote> notes)
		{
			var keep = notes.FindAll(n => n.X >= 0);
			if (keep.Count > MaxNotes) keep.RemoveRange(MaxNotes, keep.Count - MaxNotes);
			var b = new byte[keep.Count * RecordSize];
			for (int i = 0; i < keep.Count; i++)
			{
				int o = i * RecordSize;
				string t = keep[i].Text ?? "";
				for (int k = 0; k < TextSize - 1 && k < t.Length; k++) b[o + k] = t[k] < 256 ? (byte)t[k] : (byte)'?';
				b[o + 0x32] = (byte)keep[i].X; b[o + 0x33] = (byte)(keep[i].X >> 8);
				b[o + 0x34] = (byte)keep[i].Y; b[o + 0x35] = (byte)(keep[i].Y >> 8);
			}
			return b;
		}
	}

	/// <summary>UW2's map scraps (item 0x139, "a bit of a map", obj/USEITEMS.C UseBook): reading one
	/// with the map (0x13A) in the inventory copies the automap stored as "level" 0x47 + its quality
	/// onto level owner's map, only the sections whose bits are set in the low byte of its link,
	/// with the notes in those sections (update_map_scraps), giving experience; the link then
	/// becomes 0x200, and reading it again opens the automap at that level.</summary>
	public static class MapScraps
	{
		public const int ScrapItem = 0x139, MapItem = 0x13A, FirstScrapLevel = 0x47;
		public static int ScrapLevel(int quality) { return FirstScrapLevel + quality; }

		public static string SectionsText(int mask)
		{
			if ((mask & 0xFF) == 0) return "none (already copied)";
			var parts = new List<string>();
			for (int s = 0; s < 8; s++) if ((mask >> s & 1) != 0) parts.Add(s == 0 ? "middle" : "sector " + s);
			return string.Join(", ", parts.ToArray());
		}
	}
}
