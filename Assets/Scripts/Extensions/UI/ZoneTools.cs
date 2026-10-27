using System;
using System.Collections.Generic;
using System.IO;
using UltimateEditor.Uw1.Zoning;
using UnityEngine;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// The zoning session: the loaded ZoneSet, the file it lives in (uw1-zones.json next to
	/// LEV.ARK), the zone being painted, and the tile overlay colours. Painting goes through
	/// the editor's Sector mode (UIManager.ChangeTileSector).
	/// </summary>
	public static class ZoneTools
	{
		public const string FileName = "uw1-zones.json";

		public static ZoneSet Zones { get; private set; }
		public static string Path { get; private set; }
		public static bool Dirty { get; private set; }
		/// <summary>Zone id painted with the left button; 1 = unassigned (the right button always paints 1).</summary>
		public static int PaintZone = 2;
		public static bool OverlayOn = true;
		/// <summary>Sector mode paints zones while the Zones window is open; otherwise the editor's own sector colours.</summary>
		public static bool PaintEnabled;
		public static bool FillMode;
		public static event Action Changed;

		/// <summary>Forgets the zoning and its listeners (opening another game without restarting).</summary>
		public static void Reset()
		{
			Zones = null;
			Path = null;
			Dirty = false;
			PaintEnabled = false;
			FillMode = false;
			Changed = null;
		}

		public static string DefaultPath()
		{
			string folder = ExtensionsMenu.DataFolder;
			return string.IsNullOrEmpty(folder) ? null : System.IO.Path.Combine(folder, FileName);
		}

		public static void MarkDirty()
		{
			Dirty = true;
			if (Changed != null) Changed();
		}

		/// <summary>Loads the file next to LEV.ARK, or starts an empty zoning from the loaded tiles.</summary>
		public static string LoadOrNew()
		{
			Path = DefaultPath();
			if (Path != null && File.Exists(Path))
			{
				try
				{
					Zones = ZoneSet.Load(Path);
					InitUnpainted();
					Dirty = false;
					ApplyOverlay();
					if (Changed != null) Changed();
					return "Loaded " + Path + ": " + Zones.Zones.Count + " zones, " + Zones.Links.Count + " links.";
				}
				catch (Exception e) { return "Could not read " + Path + ": " + e.Message + " Started a new zoning instead (saving would overwrite the file)." + " " + New(); }
			}
			return New();
		}

		public static string New()
		{
			Zones = new ZoneSet { Source = "Started in the editor " + DateTime.Now.ToString("yyyy-MM-dd") };
			Zones.EnsureZone(ZoneSet.Unassigned).Name = "Open, not assigned yet";
			Zones.GetZone(ZoneSet.Unassigned).Status = "unassigned";
			InitUnpainted();
			Path = DefaultPath();
			Dirty = true;
			ApplyOverlay();
			if (Changed != null) Changed();
			return "New zoning: every open tile is unassigned.";
		}

		public static string LoadFrom(string path)
		{
			try
			{
				Zones = ZoneSet.Load(path);
				Path = path;
				InitUnpainted();
				Dirty = false;
				ApplyOverlay();
				if (Changed != null) Changed();
				return "Loaded " + path + ".";
			}
			catch (Exception e) { return "Could not read " + path + ": " + e.Message; }
		}

		public static string Save()
		{
			if (Zones == null) return "Nothing to save.";
			if (Path == null) Path = DefaultPath();
			if (Path == null) return "No game folder is open.";
			try
			{
				if (File.Exists(Path) && !File.Exists(Path + ".BKP")) File.Copy(Path, Path + ".BKP");
				Zones.Save(Path);
				Dirty = false;
				return "Saved " + Path + ".";
			}
			catch (Exception e) { return "Could not save: " + e.Message; }
		}

		/// <summary>Marks the open tiles of levels never painted as unassigned, from the loaded map.</summary>
		static void InitUnpainted()
		{
			if (MapCreator.LevelData == null) return;
			for (int l = 1; l <= ZoneSet.Levels && l <= MapCreator.LevelData.Count; l++)
			{
				var lv = MapCreator.LevelData[l - 1];
				if (lv == null) continue;
				int level = l;
				if (Zones.Tiles[l - 1] == null)
					Zones.InitLevelFromTiles(level, (x, y) => lv.MapTiles[x, y] != null && lv.MapTiles[x, y].TileType != TileType.Solid);
			}
		}

		/// <summary>Imports Gimp's indexed BMPs mapN.bmp (N = level - 1) from a folder.</summary>
		public static string ImportBmpFolder(string folder)
		{
			if (Zones == null) New();
			int done = 0;
			var errors = new List<string>();
			for (int l = 1; l <= ZoneSet.Levels; l++)
			{
				string p = System.IO.Path.Combine(folder, "map" + (l - 1) + ".bmp");
				if (!File.Exists(p)) continue;
				string err = Zones.ImportBmp(l, File.ReadAllBytes(p));
				if (err != null) errors.Add("map" + (l - 1) + ".bmp: " + err);
				else done++;
			}
			InitUnpainted();
			MarkDirty();
			ApplyOverlay();
			return "Imported " + done + " level maps" + (errors.Count > 0 ? "; " + string.Join("; ", errors.ToArray()) : ".");
		}

		// ---------------------------------------------------------------- painting

		/// <summary>Paints one tile (or flood fills from it) with PaintZone, or unassigns it with the right button.</summary>
		public static void Paint(MapTile tile, bool erase)
		{
			if (Zones == null || tile == null) return;
			if (tile.TileType == TileType.Solid) return;
			byte zone = (byte)(erase ? ZoneSet.Unassigned : Mathf.Clamp(PaintZone, 1, 255));
			int level = tile.Level;
			if (level < 1 || level > ZoneSet.Levels) return;
			SyncTile(tile);
			if (Zones.ZoneAt(level, tile.Position.x, tile.Position.y) == zone) return;
			if (FillMode)
			{
				Zones.Fill(level, tile.Position.x, tile.Position.y, zone);
				ApplyOverlay(level);
			}
			else
			{
				Zones.SetZone(level, tile.Position.x, tile.Position.y, zone);
				ApplyOverlay(tile);
			}
			if (zone > ZoneSet.Unassigned) Zones.EnsureZone(zone);
			MarkDirty();
		}

		/// <summary>
		/// Keeps the zone map in step with a tile whose type was edited: an opened tile becomes
		/// 'unassigned', a filled one 'solid'. Called by the painter and by the editor's tile edits.
		/// </summary>
		public static void SyncTile(MapTile tile)
		{
			if (Zones == null || tile == null || tile.Level < 1 || tile.Level > ZoneSet.Levels) return;
			byte z = Zones.ZoneAt(tile.Level, tile.Position.x, tile.Position.y);
			bool open = tile.TileType != TileType.Solid;
			if (open && z == ZoneSet.Solid) { Zones.SetZone(tile.Level, tile.Position.x, tile.Position.y, ZoneSet.Unassigned); MarkDirty(); }
			else if (!open && z != ZoneSet.Solid) { Zones.SetZone(tile.Level, tile.Position.x, tile.Position.y, ZoneSet.Solid); MarkDirty(); }
		}

		/// <summary>After the editor rebuilt a tile's meshes (new material instances), put the overlay back on it and its neighbours.</summary>
		public static void TileEdited(MapTile tile)
		{
			if (Zones == null || tile == null || MapCreator.LevelData == null) return;
			SyncTile(tile);
			var lv = tile.Level >= 1 && tile.Level <= MapCreator.LevelData.Count ? MapCreator.LevelData[tile.Level - 1] : null;
			ApplyOverlay(tile);
			if (lv == null) return;
			int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
			for (int k = 0; k < 4; k++)
			{
				int nx = tile.Position.x + dx[k], ny = tile.Position.y + dy[k];
				if (nx >= 0 && ny >= 0 && nx < 64 && ny < 64) ApplyOverlay(lv.MapTiles[nx, ny]);
			}
		}

		// ---------------------------------------------------------------- overlay

		public static void ApplyOverlay()
		{
			if (MapCreator.LevelData == null) return;
			for (int l = 1; l <= ZoneSet.Levels && l <= MapCreator.LevelData.Count; l++) ApplyOverlay(l);
		}

		public static void ApplyOverlay(int level)
		{
			if (MapCreator.LevelData == null || level < 1 || level > MapCreator.LevelData.Count) return;
			var lv = MapCreator.LevelData[level - 1];
			if (lv == null) return;
			for (int x = 0; x < 64; x++)
				for (int y = 0; y < 64; y++)
					ApplyOverlay(lv.MapTiles[x, y]);
		}

		public static void ApplyOverlay(MapTile tile)
		{
			if (tile == null) return;
			GameObject go;
			if (!MapCreator.MapTileToGO.TryGetValue(tile, out go) || !go) return;
			var mts = go.GetComponent<MapTileScript>();
			if (!mts) return;
			if (!OverlayOn || Zones == null || tile.TileType == TileType.Solid)
			{
				mts.SetOverlay(false);
				return;
			}
			int z = Zones.ZoneAt(tile.Level, tile.Position.x, tile.Position.y);
			if (z == ZoneSet.Solid) { mts.SetOverlay(false); return; }
			byte r, g, b;
			Zones.ColorOf(z, out r, out g, out b);
			mts.SetOverlay(true);
			// the shader multiplies the texture by the overlay colour: keep it bright enough to read
			var c = new Color(r / 255f, g / 255f, b / 255f, 1f);
			mts.SetOverlayColor(Color.Lerp(c, Color.white, 0.25f));
		}

		public static Color ColorOf(int zone)
		{
			byte r, g, b;
			if (Zones != null) Zones.ColorOf(zone, out r, out g, out b);
			else ZoneSet.DefaultColor(zone, out r, out g, out b);
			return new Color(r / 255f, g / 255f, b / 255f, 1f);
		}

		// ---------------------------------------------------------------- audit and solver with the editor's data

		public static AuditResult Audit()
		{
			var od = MapCreator.ObjectData;
			Func<int, int> b6 = id => od != null && od.CommonData != null && od.CommonData[id] != null && od.CommonData[id].RawData != null ? od.CommonData[id].RawData[6] : 0;
			Func<int, int> b7 = id => od != null && od.CommonData != null && od.CommonData[id] != null && od.CommonData[id].RawData != null ? od.CommonData[id].RawData[7] : 0;
			Func<int, string> npc = w => MapCreator.StringData != null ? MapCreator.StringData.GetNPCName(w) : "";
			return ZoneAudit.Run(Zones, MapCreator.LevelData, StaticObject.GetName, npc, b6, b7);
		}
	}
}
