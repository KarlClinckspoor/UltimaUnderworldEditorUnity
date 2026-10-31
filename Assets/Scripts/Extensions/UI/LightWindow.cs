using System;
using System.IO;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// UW2's dark and lit areas within a level. Two things decide them:
	/// - DATA\DL.DAT, one byte per level (80): value % 10 is a light level, the level's
	///   minimum light; value >= 10 says on which squares it applies
	///   (uw2/src/game/PLAYDATA.C load_dl);
	/// - the light flag of each square, bit 8 of its tile word (MapTile.LightFlag). When the
	///   player enters a square whose flag differs from the last one, the minimum applies if
	///   (flag XOR value >= 10), otherwise there is none and only carried light counts
	///   (uw2/src/motion/PHYSICS.C change_GrSq). A minimum of 0 does nothing.
	/// For example level 1, Lord British's castle, has 14: light 4 on squares without the flag;
	/// its corridors have the flag, so they are dark.
	/// </summary>
	public static class LightWindow
	{
		/// <summary>While set, selecting a tile (tile mode) gives it this light flag.</summary>
		public static bool? Paint;

		static byte[] dl;
		static string dlPath;
		static LightFlagOverlay overlay;

		public static bool Available { get { return LevArk.Game.Kind == GameKind.UW2; } }

		public static void Show()
		{
			if (!Available) throw new InvalidOperationException("The light flag and DL.DAT are UW2's (UW1 has neither: its light comes from carried lights only).");
			dlPath = ExtensionsMenu.FindDataFile("DL.DAT");
			dl = dlPath != null ? File.ReadAllBytes(dlPath) : null;
			var ui = UiKit.FindUIManager();
			var w = UiKit.Window("Level light (UW2: DL.DAT and the squares' light flag)", 640, 420);
			UiKit.Paragraph(w.Toolbar,
				"A UW2 level can have a minimum light (DL.DAT): the light is at least that, whatever the player carries. " +
				"Each square's light flag says whether the minimum applies there: on squares WITHOUT the flag when the level's mode is 'unflagged', " +
				"on squares WITH it when the mode is 'flagged'. Elsewhere only carried light counts. This is why level 1's corridors are dark: " +
				"the castle has light 4 on unflagged squares and the corridors are flagged (motion/PHYSICS.C change_GrSq, game/PLAYDATA.C load_dl).", 10);
			w.Closed += () => { Paint = null; if (overlay) UnityEngine.Object.Destroy(overlay.gameObject); overlay = null; };
			Build(w, ui);
		}

		static void Build(Window w, UIManager ui)
		{
			UiKit.Clear(w.Content);
			int level = ui.CurrentLevel;
			UiKit.Label(w.Content, LevArk.Game.LevelName(level) + " (level " + level + ")", 12, FontStyle.Bold);
			if (dl == null)
				UiKit.Paragraph(w.Content, "DL.DAT is not in the DATA folder: the game then gives no level a minimum light.", 11, UiKit.WarnColor);
			else if (level - 1 < dl.Length)
			{
				int value = dl[level - 1];
				int min = value % 10;
				bool hi = value >= 10;
				var row = UiKit.Row(w.Content, 20);
				UiKit.Label(row, "Minimum light (0-9, 0 none)", 11, FontStyle.Normal, null, 170);
				UiKit.Input(row, min.ToString(), s =>
				{
					int v;
					if (int.TryParse(s, out v) && v >= 0 && v <= 9) { dl[level - 1] = (byte)(v + (dl[level - 1] >= 10 ? 10 : 0)); Build(w, ui); }
					else w.Status.text = "The minimum light is 0-9 (SHADES.DAT's levels; higher is brighter).";
				}, 40);
				UiKit.Label(row, "applies on", 11, FontStyle.Normal, null, 70);
				UiKit.Dropdown(row, new[] { "flagged squares", "unflagged squares" }, hi ? 1 : 0, i =>
				{
					dl[level - 1] = (byte)(dl[level - 1] % 10 + (i == 1 ? 10 : 0));
					Build(w, ui);
				}, 150);
				UiKit.Paragraph(w.Content, min == 0
					? "This level has no minimum light, so the light flags do nothing here: only carried light counts."
					: string.Format("On {0} squares the light is at least {1}; on {2} squares only carried light counts (dark without a light source). DL.DAT byte: {3}.",
						hi ? "unflagged" : "flagged", min, hi ? "flagged" : "unflagged", dl[level - 1]), 11);
				var save = UiKit.Row(w.Content, 22);
				UiKit.Button(save, "Save DL.DAT", () =>
				{
					ExtensionsMenu.WriteDataFile(Path.GetFileName(dlPath), dl);
					w.Status.text = "DL.DAT saved. The game reads it each time a level is entered.";
				}, 110, 22, UiKit.AccentColor);
			}

			UiKit.Label(w.Content, "Light flags of this level's squares", 12, FontStyle.Bold);
			var tools = UiKit.Row(w.Content, 22);
			UiKit.Button(tools, overlay ? "Hide the flagged squares" : "Show the flagged squares", () =>
			{
				if (overlay) { UnityEngine.Object.Destroy(overlay.gameObject); overlay = null; }
				else overlay = LightFlagOverlay.Create(ui);
				Build(w, ui);
			}, 170, 22);
			UiKit.Button(tools, "This level", () => Build(w, ui), 80, 22);
			int count = 0;
			var ld = MapCreator.LevelData[level - 1];
			foreach (var t in ld.MapTiles) if (t != null && t.LightFlag) count++;
			UiKit.Label(tools, count + " flagged", 11, FontStyle.Normal, UiKit.DimTextColor, 90);

			var paint = UiKit.Row(w.Content, 22);
			UiKit.Label(paint, "Tile mode clicks:", 11, FontStyle.Normal, null, 110);
			UiKit.Dropdown(paint, new[] { "select only", "set the flag", "clear the flag" }, Paint == null ? 0 : Paint.Value ? 1 : 2, i =>
			{
				Paint = i == 0 ? (bool?)null : i == 1;
				w.Status.text = Paint == null ? "" : "In tile mode, each tile you click gets the flag " + (Paint.Value ? "set." : "cleared.") + " Save the game to keep it.";
			}, 140);
			var sel = UiKit.Row(w.Content, 22);
			UiKit.Button(sel, "Toggle the selected tile", () =>
			{
				var t = TileProperties.LastSelected;
				if (t == null || TileProperties.LastSelectedLevel != level) { w.Status.text = "Select a tile of this level first."; return; }
				t.LightFlag = !t.LightFlag;
				Changed();
				w.Status.text = string.Format("({0}, {1}): light flag {2}.", t.Position.x, t.Position.y, t.LightFlag ? "set" : "cleared");
			}, 170, 22);
			UiKit.Paragraph(w.Content, "The flag is bit 8 of the tile, saved with the level (File > Save). The hover panel shows it as 'light flag'. " +
				"The player's light is redone only when he steps between squares whose flags differ, and when he changes his light source.", 10, UiKit.DimTextColor);
		}

		/// <summary>Called when a tile is selected in tile mode: paints its flag while painting.</summary>
		public static void OnTileSelected(MapTile tile)
		{
			if (Paint == null || tile == null || !Available) return;
			if (tile.LightFlag == Paint.Value) return;
			tile.LightFlag = Paint.Value;
			Changed();
		}

		public static void Changed()
		{
			if (overlay) overlay.Dirty = true;
		}
	}

	/// <summary>Translucent squares over the flagged tiles of the shown level.</summary>
	public sealed class LightFlagOverlay : MonoBehaviour
	{
		public bool Dirty = true;
		UIManager ui;
		int shownLevel;
		Sprite sprite;
		Texture2D tex;

		public static LightFlagOverlay Create(UIManager ui)
		{
			var go = new GameObject("Light flag overlay");
			var o = go.AddComponent<LightFlagOverlay>();
			o.ui = ui;
			o.tex = new Texture2D(1, 1);
			o.tex.SetPixel(0, 0, Color.white);
			o.tex.Apply();
			float ts = MapCreator.TileSize * 2.0f;
			o.sprite = Sprite.Create(o.tex, new Rect(0, 0, 1, 1), Vector2.zero, 1f / ts);
			return o;
		}

		void Update()
		{
			if (!ui || !MapCreator.IsLoaded) return;
			if (!Dirty && shownLevel == ui.CurrentLevel) return;
			Dirty = false;
			shownLevel = ui.CurrentLevel;
			for (int i = transform.childCount - 1; i >= 0; i--)
				Destroy(transform.GetChild(i).gameObject);
			var ld = MapCreator.LevelData[shownLevel - 1];
			float ts = MapCreator.TileSize * 2.0f;
			var color = new Color(0.15f, 0.45f, 1f, 0.5f);
			foreach (var t in ld.MapTiles)
			{
				if (t == null || !t.LightFlag || t.TileType == TileType.Solid) continue;
				var q = new GameObject("flag " + t.Position.x + "/" + t.Position.y);
				q.transform.SetParent(transform, false);
				q.transform.position = new Vector3(t.Position.x * ts, t.Position.y * ts, MapCreator.GetTileHeight(t) - 0.01f);
				var sr = q.AddComponent<SpriteRenderer>();
				sr.sprite = sprite;
				sr.color = color;
				if (MapCreator.OverlayMaterial) sr.material = MapCreator.OverlayMaterial;
				sr.sortingLayerName = "Items";	// as the crosshairs, which draw above the floor
				sr.sortingOrder = 0;
			}
		}

		void OnDestroy()
		{
			if (sprite) Destroy(sprite);
			if (tex) Destroy(tex);
		}
	}
}
