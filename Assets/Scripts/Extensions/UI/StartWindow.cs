using System;
using System.IO;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// The square a new game starts on. It is a constant in the executable (the main menu's
	/// player_setup call; Games/StartPosition.cs), always on level 1, not in LEV.ARK. A saved
	/// game's position is in its PLAYER.DAT (Tools > Character in a save).
	/// </summary>
	public static class StartWindow
	{
		public static void Show()
		{
			var game = LevArk.Game.Kind;
			string exe = StartPosition.ExePath(ExtensionsMenu.DataFolder, game);
			if (exe == null) throw new FileNotFoundException((game == GameKind.UW2 ? "UW2.EXE" : "UW.EXE") + " was not found next to the DATA folder");
			var bytes = File.ReadAllBytes(exe);
			int x, y;
			if (!StartPosition.Read(bytes, game, out x, out y))
				throw new InvalidDataException("The start position call was not found in " + Path.GetFileName(exe) + " (another version of the game?)");
			var w = UiKit.Window("New game start (" + Path.GetFileName(exe) + ")", 560, 260);
			UiKit.Paragraph(w.Toolbar, "A new game puts the player in the middle of this square of level 1, on its floor. The game sets it in code (" +
				(game == GameKind.UW2 ? "player_setup(0x13, 0x30)" : "player_setup(0x20, 2)") + " in the main menu), so it is changed in the executable; a .BKP copy is kept. " +
				"The level is always 1. Saved games keep their own position (Tools > Character in a save).", 10);
			var row = UiKit.Row(w.Content, 20);
			UiKit.Label(row, "Square x, y", 11, FontStyle.Bold, null, 90);
			var xi = UiKit.Input(row, x.ToString(), s => int.TryParse(s, out x), 40);
			var yi = UiKit.Input(row, y.ToString(), s => int.TryParse(s, out y), 40);
			var row2 = UiKit.Row(w.Content, 22);
			UiKit.Button(row2, "Use the selected tile", () =>
			{
				var t = TilePanel.LastTile;
				if (t == null) { w.Status.text = "Select a tile on level 1 first (tile mode)."; return; }
				if (TilePanel.LastTileLevel != 1) { w.Status.text = "A new game always starts on level 1."; return; }
				x = t.Position.x; y = t.Position.y; xi.text = x.ToString(); yi.text = y.ToString();
			}, 150, 22);
			UiKit.Button(row2, "Write to the executable", () =>
			{
				// new games use DATA's level 1; with a save open the loaded level 1 is the save's
				var lev = !GameSession.IsSave && MapCreator.LevelData != null && MapCreator.LevelData.Count > 0 ? MapCreator.LevelData[0] : null;
				if (x < 1 || x > 62 || y < 1 || y > 62) { w.Status.text = "Squares 1-62."; return; }
				if (lev != null && lev.MapTiles[x, y].TileType == TileType.Solid) { w.Status.text = "That square of level 1 is solid rock."; return; }
				var b = File.ReadAllBytes(exe);
				if (!StartPosition.Patch(b, game, x, y)) { w.Status.text = "The call was not found any more."; return; }
				GameWrite.Write(exe, b);
				MapCreator.ExeStart = new Vector2Int(x, y);
				w.Status.text = string.Format("New games now start at ({0}, {1}) of level 1.", x, y);
			}, 170, 22, UiKit.AccentColor);
			UiKit.Button(row2, "Original", () =>
			{
				x = game == GameKind.UW2 ? 0x13 : 0x20; y = game == GameKind.UW2 ? 0x30 : 2;
				xi.text = x.ToString(); yi.text = y.ToString();
			}, 70, 22);
		}
	}
}
