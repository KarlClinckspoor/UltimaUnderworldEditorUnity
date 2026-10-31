using System;
using System.Collections.Generic;
using System.IO;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Edits the character in a saved game's PLAYER.DAT (Games/PlayerDat.cs), either game:
	/// name, attributes, skills, vitality, mana, level, experience and skill points. The
	/// saves are the SAVEn folders next to DATA (UW2's SAVE0 is the game in progress).
	/// </summary>
	public static class PlayerWindow
	{
		public static void Show() { Show(GameSession.SaveFolder); }

		/// <summary>Opens the window on the save in <paramref name="saveFolder"/> when given (the open save by default).</summary>
		public static void Show(string saveFolder)
		{
			string root = Path.GetDirectoryName(ExtensionsMenu.DataFolder.TrimEnd('/', '\\'));
			var saves = FindSaves(root);
			if (saves.Count == 0) throw new FileNotFoundException("No saved game (SAVE0-SAVE9 with a PLAYER.DAT) next to the DATA folder. The PLAYER.DAT in DATA is not a save.");
			var w = UiKit.Window("Character (a save's PLAYER.DAT)", 760, 600);
			var names = new List<string>();
			foreach (var s in saves) names.Add(Path.GetFileName(Path.GetDirectoryName(s)) + "/" + Path.GetFileName(s));
			var tools = UiKit.Row(w.Toolbar, 22);
			int current = 0;
			if (saveFolder != null)
				for (int i = 0; i < saves.Count; i++)
					if (string.Equals(Path.GetFullPath(Path.GetDirectoryName(saves[i])), Path.GetFullPath(saveFolder.TrimEnd('/', '\\')), StringComparison.OrdinalIgnoreCase)) current = i;
			UiKit.Dropdown(tools, names, current, i => { current = i; Load(w, saves[current]); }, 200);
			UiKit.Paragraph(w.Toolbar, "Saving keeps a one-time .BKP of the file. Vitality is written to the record and to the player object the game actually keeps. Derived values (maximum weight, armour) are recomputed by the game.", 10);
			Load(w, saves[current]);
		}

		internal static List<string> FindSaves(string root)
		{
			var list = new List<string>();
			if (root == null || !Directory.Exists(root)) return list;
			foreach (var dir in Directory.GetDirectories(root))
			{
				string n = Path.GetFileName(dir).ToUpperInvariant();
				if (n.Length != 5 || !n.StartsWith("SAVE") || !char.IsDigit(n[4])) continue;
				string f = GameFiles.Resolve(Path.Combine(dir, "PLAYER.DAT"));
				if (File.Exists(f)) list.Add(f);
			}
			list.Sort(StringComparer.OrdinalIgnoreCase);
			return list;
		}

		static void Load(Window w, string path)
		{
			UiKit.Clear(w.Content);
			var bytes = File.ReadAllBytes(path);
			var kind = PlayerDat.Detect(bytes);
			if (kind == null) { w.Status.text = Path.GetFileName(path) + ": " + bytes.Length + " bytes fits neither game's save layout."; return; }
			var p = PlayerDat.Parse(bytes, kind.Value);
			w.Status.text = string.Format("{0} save; {1} objects carried; saved on level {2} at ({3}, {4}).", kind.Value == GameKind.UW2 ? "UW2" : "UW1",
				p.SavedObjects, LevArk.Game.Kind == kind.Value ? GameProfile.For(kind.Value).LevelName(p.SavedLevel) : p.SavedLevel.ToString(), p.TileX, p.TileY);

			var top = UiKit.Row(w.Content, 22);
			UiKit.Button(top, "Save PLAYER.DAT", () =>
			{
				GameWrite.Write(path, p.ToBytes());
				w.Status.text = "Saved " + path;
				var ui = UiKit.FindUIManager();
				if (ui && ui.MapCreator.IsInitialized && GameSession.IsSave) MapCreator.RefreshStartMarker(ui.CurrentLevel);
			}, 130, 22, UiKit.AccentColor);
			UiKit.Button(top, "Reload", () => Load(w, path), 70, 22);

			var row = UiKit.Row(w.Content, 20);
			UiKit.Label(row, "Name", 11, FontStyle.Bold, null, 110);
			UiKit.Input(row, p.Name, s => { try { p.Name = s; } catch (Exception e) { w.Status.text = e.Message; } }, 200, false);

			Number(w, "Strength", () => p.Strength, v => p.Strength = v);
			Number(w, "Dexterity", () => p.Dexterity, v => p.Dexterity = v);
			Number(w, "Intelligence", () => p.Intelligence, v => p.Intelligence = v);
			Number(w, "Vitality", () => p.Vitality, v => p.Vitality = v);
			Number(w, "Max vitality", () => p.MaxVitality, v => p.MaxVitality = v);
			Number(w, "Mana", () => p.Mana, v => p.Mana = v);
			Number(w, "Max mana", () => p.MaxMana, v => p.MaxMana = v);
			Number(w, "Level", () => p.Level, v => p.Level = v);
			Number(w, "Skill points", () => p.SkillPoints, v => p.SkillPoints = v);
			var ex = UiKit.Row(w.Content, 20);
			UiKit.Label(ex, "Experience", 11, FontStyle.Bold, null, 110);
			UiKit.Input(ex, (p.ExperienceTenths / 10.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), s =>
			{
				double v;
				if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) && v >= 0 && v * 10 < uint.MaxValue)
					p.ExperienceTenths = (long)Math.Round(v * 10);
				else w.Status.text = "Experience is a number (stored in tenths).";
			}, 100);
			// heights come from the loaded levels only when they are this save's
			bool sameSave = GameSession.IsSave && string.Equals(System.IO.Path.GetFullPath(System.IO.Path.GetDirectoryName(path)), System.IO.Path.GetFullPath(GameSession.SaveFolder), StringComparison.OrdinalIgnoreCase);
			Position(w, p, kind.Value, sameSave);
			UiKit.Label(w.Content, "Skills", 12, FontStyle.Bold);
			for (int i = 0; i < 20; i += 2)
			{
				var r = UiKit.Row(w.Content, 20);
				for (int k = i; k < i + 2; k++)
				{
					int s = k;
					UiKit.Label(r, PlayerDat.SkillNames[s], 11, FontStyle.Normal, null, 110);
					UiKit.Input(r, p.Skill(s).ToString(), t => { int v; if (int.TryParse(t, out v) && v >= 0 && v <= 255) p.SetSkill(s, v); else w.Status.text = "Skills are 0-255 (the game's own go to 30)."; }, 50);
					UiKit.Label(r, "", 11, FontStyle.Normal, null, 40);
				}
			}
		}

		/// <summary>Where the save puts the player: level, square and heading (PlayerDat.SetPosition).</summary>
		static void Position(Window w, PlayerDat p, GameKind kind, bool levelsLoaded)
		{
			UiKit.Label(w.Content, "Position", 12, FontStyle.Bold);
			int level = p.SavedLevel, x = p.TileX, y = p.TileY, heading = p.Heading;
			var row = UiKit.Row(w.Content, 20);
			UiKit.Label(row, "Level", 11, FontStyle.Normal, null, 40);
			var lv = UiKit.Input(row, level.ToString(), s => int.TryParse(s, out level), 40);
			UiKit.Label(row, "Square x, y", 11, FontStyle.Normal, null, 70);
			var xi = UiKit.Input(row, x.ToString(), s => int.TryParse(s, out x), 36);
			var yi = UiKit.Input(row, y.ToString(), s => int.TryParse(s, out y), 36);
			UiKit.Label(row, "Heading (0 N, 2 E, 4 S, 6 W)", 11, FontStyle.Normal, null, 170);
			var hi = UiKit.Input(row, heading.ToString(), s => int.TryParse(s, out heading), 30);
			var row2 = UiKit.Row(w.Content, 20);
			UiKit.Button(row2, "Use the selected tile", () =>
			{
				var t = TileProperties.LastSelected;
				if (t == null) { w.Status.text = "Select a tile (tile mode) or an object on the floor (object mode) first."; return; }
				level = TileProperties.LastSelectedLevel; x = t.Position.x; y = t.Position.y;
				lv.text = level.ToString(); xi.text = x.ToString(); yi.text = y.ToString();
			}, 150);
			UiKit.Button(row2, "Set position", () =>
			{
				int slots = GameProfile.For(kind).LevelSlots;
				if (level < 1 || level > slots || x < 1 || x > 62 || y < 1 || y > 62 || heading < 0 || heading > 7)
				{ w.Status.text = "Level 1-" + slots + ", squares 1-62, heading 0-7."; return; }
				// the floor height comes from the level as the editor has it loaded
				int h = 0; bool slope = false; string note = "";
				var lev = MapCreator.LevelData != null && level <= MapCreator.LevelData.Count ? MapCreator.LevelData[level - 1] : null;
				if (levelsLoaded && lev != null && lev.Present && LevArk.Game.Kind == kind)
				{
					var t = lev.MapTiles[x, y];
					if (t.TileType == TileType.Solid) { w.Status.text = "That square is solid rock."; return; }
					h = t.FloorHeight; slope = (int)t.TileType >= 6;
				}
				else note = " (level not loaded here, so z is set for floor height 0: open this save to place the player at the right height)";
				p.SetPosition(level, x, y, h, slope, heading);
				w.Status.text = string.Format("Position set to {0} ({1}, {2}), heading {3}{4}. Save PLAYER.DAT to keep it.", GameProfile.For(kind).LevelName(level), x, y, heading, note);
			}, 100);
		}

		static void Number(Window w, string label, Func<int> get, Action<int> set)
		{
			var row = UiKit.Row(w.Content, 20);
			UiKit.Label(row, label, 11, FontStyle.Bold, null, 110);
			UiKit.Input(row, get().ToString(), s =>
			{
				int v;
				if (int.TryParse(s, out v) && v >= 0 && v <= 255) set(v);
				else w.Status.text = label + " is 0-255.";
			}, 60);
		}
	}
}
