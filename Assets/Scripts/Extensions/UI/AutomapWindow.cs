using System;
using System.Collections.Generic;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Edits the automap and the map notes LEV.ARK keeps per level (Games/Automap.cs), both
	/// games, and UW2's map scraps: which stored map a scrap copies, onto which level, and which
	/// of the map's eight sections. Changes go into the archive in memory when another level is
	/// picked, the window closes or "Save LEV.ARK now" is pressed; the editor's own Save writes
	/// them as well.
	/// </summary>
	public static class AutomapWindow
	{
		const int Cell = 6;

		enum Tool { Seen, KnownUnseen, Erase, Note }
		static readonly string[] toolNames = { "Paint: seen", "Paint: known, not seen", "Paint: erase", "Click: add note" };

		sealed class State
		{
			public int Level = 1;
			public AutomapLevel Map;
			public List<MapNote> Notes;
			public bool MapDirty, NotesDirty, MapExisted, DeleteBlocks;
			public Tool Tool;
			public int Brush = 1;
			public int SectionMask = -1;      // highlighted map scrap sections, -1 none
			public Texture2D Texture;
			public Text Info;
			public RectTransform NotesPanel;
			public Window Window;
		}

		public static void Show()
		{
			if (LevArk.Archive == null) throw new InvalidOperationException("Load the game data first: the automap lives in LEV.ARK.");
			var st = new State();
			var w = st.Window = UiKit.Window("Automap and map notes (LEV.ARK)", 860, 680);
			int count = MapCreator.LevelData != null ? MapCreator.LevelData.Count : LevArk.Game.LevelSlots;
			st.Level = Mathf.Clamp(UiKit.FindUIManager() ? UiKit.FindUIManager().CurrentLevel : 1, 1, count);

			var tools = UiKit.Row(w.Toolbar, 22);
			var levels = new List<string>();
			var scrapMaps = ScrapMapLevels();
			for (int l = 1; l <= count; l++)
			{
				string n = LevArk.Game.LevelName(l);
				string s;
				if (scrapMaps.TryGetValue(l, out s)) n += " - map scrap " + s;
				bool has = LevArk.ReadAutomap(l) != null || LevArk.ReadNotes(l) != null;
				levels.Add((has ? "* " : "  ") + n);
			}
			UiKit.Dropdown(tools, levels, st.Level - 1, i => { Apply(st); Load(st, i + 1); }, 300);
			UiKit.Cycle(tools, toolNames, 0, i => st.Tool = (Tool)i, 170);
			UiKit.Cycle(tools, new[] { "Brush 1", "Brush 3", "Brush 5" }, 0, i => st.Brush = 1 + 2 * i, 70);
			UiKit.Button(tools, "Save LEV.ARK now", () =>
			{
				Apply(st);
				LevArk.SaveArchiveNow();
				w.Status.text = "LEV.ARK saved (automap and notes, and the levels as last saved).";
			}, 130, 22, UiKit.AccentColor);

			var tools2 = UiKit.Row(w.Toolbar, 22);
			UiKit.Button(tools2, "Reveal from the level", () => { RevealAll(st); }, 150, 22);
			UiKit.Button(tools2, "All known, not seen", () => { st.Map.MakeUnseen(0, 0, 63, 63); st.MapDirty = true; Redraw(st); }, 140, 22);
			UiKit.Button(tools2, "Clear map", () => { st.Map.Clear(0, 0, 63, 63); st.MapDirty = true; Redraw(st); }, 80, 22);
			UiKit.Button(tools2, "Delete map and notes", () =>
			{
				st.Map = new AutomapLevel(); st.Notes.Clear(); st.MapDirty = st.NotesDirty = true;
				st.MapExisted = false; st.DeleteBlocks = true; Redraw(st); FillNotes(st);
			}, 150, 22, UiKit.WarnColor);
			UiKit.Paragraph(w.Toolbar,
				"A byte per tile: 0 never seen, 1-9 seen (the tile type), A-F known but not seen. Seen tiles take the level's tile type and floor terrain, as the game's map does. " +
				"Notes are written where clicked (the game allows 46 characters). In a saved game this is the save's LEV.ARK; in DATA it is what a new game starts with." +
				(LevArk.Game.Kind == GameKind.UW2 ? " UW2 keeps the maps that map scraps copy as \"levels\" 0x47 + the scrap's quality (72 and up)." : ""), 10);

			var mapRow = UiKit.Row(w.Content, 64 * Cell, 8);
			var pic = UiKit.Rect("Automap", mapRow);
			UiKit.Size(pic.gameObject, 64 * Cell, 64 * Cell);
			var raw = pic.gameObject.AddComponent<RawImage>();
			st.Texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
			raw.texture = st.Texture;
			var paint = pic.gameObject.AddComponent<MapPaint>();
			paint.OnTile = (x, y, first) => OnTile(st, x, y, first);
			var side = UiKit.Column(mapRow, 4);
			UiKit.Size(side.gameObject, 400 * UiKit.Scale, -1);
			st.Info = UiKit.Paragraph(side, "", 11, UiKit.TextColor);
			if (LevArk.Game.Kind == GameKind.UW2) BuildScraps(st, side);

			UiKit.Label(w.Content, "Notes", 12, FontStyle.Bold);
			st.NotesPanel = UiKit.Column(w.Content, 2);
			Action before = () => Apply(st);
			LevArk.BeforeSave += before;
			w.Closed += () => { Apply(st); LevArk.BeforeSave -= before; if (st.Texture) UnityEngine.Object.Destroy(st.Texture); };
			Load(st, st.Level);
		}

		/// <summary>Map scrap levels (0x47 + quality) referenced by scrap objects, with what they cover.</summary>
		static Dictionary<int, string> ScrapMapLevels()
		{
			var d = new Dictionary<int, string>();
			if (LevArk.Game.Kind != GameKind.UW2 || MapCreator.LevelData == null) return d;
			foreach (var ld in MapCreator.LevelData)
			{
				if (ld == null || !ld.Present) continue;
				foreach (var o in ld.Objects)
					if (o != null && o.ObjectID == MapScraps.ScrapItem)
					{
						int l = MapScraps.ScrapLevel(o.Quality);
						if (!d.ContainsKey(l)) d[l] = "(quality " + o.Quality + ", for " + LevArk.Game.LevelName(o.Owner) + ")";
					}
			}
			return d;
		}

		static void BuildScraps(State st, Transform side)
		{
			UiKit.Label(side, "Map scraps (item 0x139)", 12, FontStyle.Bold);
			UiKit.Paragraph(side, "Reading one with the map in the pack copies the stored map 0x47 + quality onto the owner level, only the sections ticked, with the notes there, and gives experience. Click a scrap to see its sections on its stored map.", 10, UiKit.DimTextColor);
			if (MapCreator.LevelData == null) return;
			foreach (var ld in MapCreator.LevelData)
			{
				if (ld == null || !ld.Present) continue;
				foreach (var o in ld.Objects)
				{
					if (o == null || o.ObjectID != MapScraps.ScrapItem) continue;
					var so = o;
					var row = UiKit.Row(side, 20, 3);
					UiKit.Button(row, string.Format("L{0} #{1}", ld.Level, so.CurrentAdress), () =>
					{
						Apply(st);
						st.SectionMask = so.Special & 0xFF;
						Load(st, MapScraps.ScrapLevel(so.Quality), true);
						st.Window.Status.text = string.Format("Scrap #{0} on {1}: the map stored as level {2}, copied onto {3}; sections: {4}.",
							so.CurrentAdress, LevArk.Game.LevelName(ld.Level), MapScraps.ScrapLevel(so.Quality), LevArk.Game.LevelName(so.Owner), MapScraps.SectionsText(so.Special & 0xFF));
					}, 70);
					for (int s = 0; s < 8; s++)
					{
						int bit = 1 << s;
						UiKit.Check(row, s == 0 ? "M" : s.ToString(), (so.Special & bit) != 0, b =>
						{
							// keep the high bits (0x200 marks the scrap as not yet copied when the low byte is set)
							so.Special = b ? so.Special | bit | 0x200 : so.Special & ~bit;
							if (st.SectionMask >= 0) { st.SectionMask = so.Special & 0xFF; Redraw(st); }
						}, 34);
					}
				}
			}
		}

		static void Load(State st, int level, bool keepSections = false)
		{
			st.Level = level;
			if (!keepSections) st.SectionMask = -1;
			var m = LevArk.ReadAutomap(level);
			st.MapExisted = m != null;
			st.Map = new AutomapLevel(m);
			st.Notes = MapNote.ParseAll(LevArk.ReadNotes(level));
			st.MapDirty = st.NotesDirty = st.DeleteBlocks = false;
			Redraw(st);
			FillNotes(st);
		}

		/// <summary>Puts the shown level's changes into the archive in memory.</summary>
		static void Apply(State st)
		{
			if (st.Map == null) return;
			bool changed = false;
			// "Delete map and notes" removes the blocks (a new game then starts without them);
			// otherwise an emptied map or note list is written as the game writes it
			bool del = st.DeleteBlocks && !HasAny(st.Map) && st.Notes.Count == 0;
			if (st.MapDirty)
				changed |= LevArk.WriteAutomap(st.Level, del ? null : st.Map.Tiles);
			if (st.NotesDirty)
				changed |= LevArk.WriteNotes(st.Level, del ? null : MapNote.ToBytes(st.Notes));
			st.MapDirty = st.NotesDirty = st.DeleteBlocks = false;
			if (changed && st.Window != null && st.Window.Status) st.Window.Status.text = LevArk.Game.LevelName(st.Level) + ": automap/notes changed (not on disk until saved).";
		}

		static bool HasAny(AutomapLevel m) { foreach (var b in m.Tiles) if (b != 0) return true; return false; }

		static void RevealAll(State st)
		{
			int src = st.Level;
			// a scrap's stored map shows the level the scrap is for
			if (LevArk.Game.Kind == GameKind.UW2 && MapCreator.LevelData != null && st.Level >= MapScraps.FirstScrapLevel)
				foreach (var ld in MapCreator.LevelData)
					if (ld != null && ld.Present)
						foreach (var o in ld.Objects)
							if (o != null && o.ObjectID == MapScraps.ScrapItem && MapScraps.ScrapLevel(o.Quality) == st.Level) src = o.Owner;
			var lev = src >= 1 && MapCreator.LevelData != null && src <= MapCreator.LevelData.Count ? MapCreator.LevelData[src - 1] : null;
			if (lev == null || !lev.Present) { st.Window.Status.text = "No level " + src + " to take the map from."; return; }
			st.Map.Reveal(LevArk.Game.Kind, 0, 0, 63, 63, (x, y) => (int)lev.MapTiles[x, y].TileType, (x, y) => Terrain(lev, x, y));
			st.MapDirty = true;
			st.Window.Status.text = "Revealed from " + LevArk.Game.LevelName(src) + ".";
			Redraw(st);
		}

		static int Terrain(LevelData lev, int x, int y)
		{
			var td = MapCreator.TextureData;
			int f = lev.MapTiles[x, y].FloorTexture;
			if (td == null || td.FloorTypes == null || f < 0 || f >= lev.FloorTextures.Length) return 0;
			int g = lev.FloorTextures[f];
			return g >= 0 && g < td.FloorTypes.Length ? td.FloorTypes[g] : 0;
		}

		static void OnTile(State st, int x, int y, bool first)
		{
			if (st.Tool == Tool.Note)
			{
				if (!first) return;
				if (st.Notes.Count >= MapNote.MaxNotes) { st.Window.Status.text = "A level holds at most 100 notes."; return; }
				var n = new MapNote { Text = "NOTE" };
				n.TileX = x; n.TileY = y;
				st.Notes.Add(n);
				st.NotesDirty = true;
				FillNotes(st);
				Redraw(st);
				return;
			}
			int r = st.Brush / 2;
			var lev = MapCreator.LevelData != null && st.Level <= MapCreator.LevelData.Count ? MapCreator.LevelData[st.Level - 1] : null;
			for (int yy = y - r; yy <= y + r; yy++)
				for (int xx = x - r; xx <= x + r; xx++)
				{
					if (xx < 0 || yy < 0 || xx > 63 || yy > 63) continue;
					if (st.Tool == Tool.Erase) st.Map[xx, yy] = 0;
					else if (st.Tool == Tool.KnownUnseen)
					{
						int type = lev != null && lev.Present ? (int)lev.MapTiles[xx, yy].TileType : 1;
						st.Map[xx, yy] = (byte)((st.Map[xx, yy] & 0xF0) | AutomapLevel.UnseenCode[type & 0xF]);
					}
					else
					{
						if (lev != null && lev.Present)
							st.Map[xx, yy] = AutomapLevel.SeenCode(LevArk.Game.Kind, (int)lev.MapTiles[xx, yy].TileType, Terrain(lev, xx, yy));
						else st.Map[xx, yy] = 1;
					}
				}
			st.MapDirty = true;
			Redraw(st);
		}

		static void Redraw(State st)
		{
			var tex = st.Texture;
			bool uw2 = LevArk.Game.Kind == GameKind.UW2;
			for (int y = 0; y < 64; y++)
				for (int x = 0; x < 64; x++)
				{
					byte c = st.Map[x, y];
					int t = c & 0xF;
					Color col;
					if (t == 0) col = new Color(0.08f, 0.08f, 0.09f);
					else if (t >= 0xA) col = new Color(0.32f, 0.32f, 0.34f);
					else
					{
						col = t == 1 ? new Color(0.82f, 0.74f, 0.58f) : t <= 5 ? new Color(0.72f, 0.64f, 0.5f) : new Color(0.66f, 0.6f, 0.52f);
						int terr = uw2 ? (c >> 6) & 3 : (c >> 4) & 3;
						if (terr == 1) col = new Color(0.35f, 0.5f, 0.85f);
						else if (terr == 2) col = new Color(0.85f, 0.35f, 0.2f);
						else if (terr == 3) col = new Color(0.6f, 0.85f, 0.9f);
						int code = uw2 ? (c >> 4) & 3 : (c >> 6) & 3;
						if (code == 1) col = new Color(0.55f, 0.35f, 0.2f);
					}
					if (st.SectionMask >= 0 && ((st.SectionMask >> AutomapLevel.MapSection(x, y)) & 1) == 0)
						col = Color.Lerp(col, Color.black, 0.65f);
					tex.SetPixel(x, y, col);
				}
			foreach (var n in st.Notes)
				if (n.X >= 0 && n.TileX >= 0 && n.TileX < 64 && n.TileY >= 0 && n.TileY < 64)
					tex.SetPixel(n.TileX, n.TileY, new Color(1f, 0.85f, 0.1f));
			tex.Apply();
			if (st.Info)
				st.Info.text = string.Format("{0}{1}\nSeen tiles: {2}. Notes: {3}.{4}", LevArk.Game.LevelName(st.Level),
					st.MapExisted ? "" : " (no automap block yet)", st.Map.CountSeen(), st.Notes.Count,
					st.SectionMask >= 0 ? "\nDark: outside the scrap's sections (" + MapScraps.SectionsText(st.SectionMask) + ")." : "");
		}

		static void FillNotes(State st)
		{
			UiKit.Clear(st.NotesPanel);
			if (st.Notes.Count == 0) UiKit.Label(st.NotesPanel, "No notes on this level.", 11, FontStyle.Italic, UiKit.DimTextColor);
			for (int i = 0; i < st.Notes.Count; i++)
			{
				var n = st.Notes[i];
				var row = UiKit.Row(st.NotesPanel, 20, 4);
				UiKit.Input(row, n.Text, s =>
				{
					if (s.Length > MapNote.TextSize - 1) s = s.Substring(0, MapNote.TextSize - 1);
					if (s != n.Text) { n.Text = s; st.NotesDirty = true; }
					if (s.Length > MapNote.GameMaxChars) st.Window.Status.text = "Longer than the game lets the player type (46); it still shows.";
				}, 360, false);
				UiKit.Label(row, "tile", 10, FontStyle.Normal, UiKit.DimTextColor, 24);
				UiKit.Input(row, n.TileX.ToString(), s => { int v; if (int.TryParse(s, out v) && v >= 0 && v < 64) { n.TileX = v; st.NotesDirty = true; Redraw(st); } }, 34);
				UiKit.Input(row, n.TileY.ToString(), s => { int v; if (int.TryParse(s, out v) && v >= 0 && v < 64) { n.TileY = v; st.NotesDirty = true; Redraw(st); } }, 34);
				UiKit.Button(row, "x", () => { st.Notes.Remove(n); st.NotesDirty = true; FillNotes(st); Redraw(st); }, 20, 20, UiKit.WarnColor);
			}
		}
	}

	/// <summary>Turns clicks and drags on the map picture into tile coordinates (row 0 at the bottom, as the map is drawn).</summary>
	public sealed class MapPaint : MonoBehaviour, IPointerDownHandler, IDragHandler
	{
		public Action<int, int, bool> OnTile;

		public void OnPointerDown(PointerEventData e) { Hit(e, true); }
		public void OnDrag(PointerEventData e) { Hit(e, false); }

		void Hit(PointerEventData e, bool first)
		{
			var rt = (RectTransform)transform;
			Vector2 local;
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out local)) return;
			var r = rt.rect;
			int x = Mathf.FloorToInt((local.x - r.xMin) / r.width * 64);
			int y = Mathf.FloorToInt((local.y - r.yMin) / r.height * 64);
			if (x < 0 || y < 0 || x > 63 || y > 63) return;
			if (OnTile != null) OnTile(x, y, first);
		}
	}
}
