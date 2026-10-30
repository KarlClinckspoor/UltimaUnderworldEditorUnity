using System;
using System.IO;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// File > Open: the user picks the game's UW.EXE or UW2.EXE (a LEV.ARK still works), then
	/// what to edit: the game's DATA, which every new game starts from, or one of the saved
	/// games beside it (Data/GameSession.cs). Data loads at once. With data already loaded,
	/// the editor asks first and then reloads its scene, so another game or save opens
	/// without restarting the program.
	/// </summary>
	public static class GameOpener
	{
		public const string Filter =
			"Ultima Underworld I or II (UW.EXE, UW2.EXE)|UW.EXE;UW2.EXE|" +
			"Level archive (LEV.ARK)|LEV.ARK|" +
			"Programs (*.EXE)|*.EXE|" +
			"All files (*.*)|*.*";

		static string pending;
		static Window chooser;

		/// <summary>The LEV.ARK to load as soon as the reloaded scene starts, once.</summary>
		public static string TakePending()
		{
			string p = pending;
			pending = null;
			return p;
		}

		/// <summary>Shows the system's file dialog on Windows, the editor's own explorer elsewhere.</summary>
		public static void Browse(UIManager ui)
		{
			string start = StartFolder();
			if (NativeFileDialog.Available)
			{
				string chosen;
				try { chosen = NativeFileDialog.OpenFile("Open Ultima Underworld: choose UW.EXE or UW2.EXE in the game's folder", Filter, start); }
				catch (Exception e)
				{
					Debug.LogWarning("The Windows file dialog failed, using the editor's own: " + e.Message);
					ui.CreateInAppFileExplorer(start);
					return;
				}
				if (!string.IsNullOrEmpty(chosen)) Picked(ui, chosen);
				return;
			}
			ui.CreateInAppFileExplorer(start);
		}

		/// <summary>The game folder of the data last opened (the folder that holds DATA), or the editor's folder.</summary>
		public static string StartFolder()
		{
			string p = DataReader.FilePath;
			try
			{
				if (!string.IsNullOrEmpty(p) && File.Exists(p))
				{
					string dir = Path.GetDirectoryName(Path.GetFullPath(p));
					string name = Path.GetFileName(dir) ?? "";
					if (name.Equals("DATA", StringComparison.OrdinalIgnoreCase) || GameSession.IsSaveFolderName(name))
						dir = Path.GetDirectoryName(dir);
					if (Directory.Exists(dir)) return Slashes(dir);
				}
			}
			catch (Exception) { }
			return Slashes(Path.GetDirectoryName(Application.dataPath));
		}

		/// <summary>What the user chose in a file dialog: a game program or a LEV.ARK.</summary>
		public static void Picked(UIManager ui, string path)
		{
			path = Slashes(path);
			string name = (Path.GetFileName(path) ?? "").ToUpperInvariant();
			if (name == "LEV.ARK")
			{
				OpenLevArk(ui, path);
				return;
			}
			if (name.EndsWith(".EXE"))
			{
				string game = Slashes(Path.GetDirectoryName(path));
				string data = Slashes(GameFiles.Resolve(game + "/DATA"));
				string lev = Slashes(GameFiles.Resolve(data + "/LEV.ARK"));
				if (!File.Exists(lev))
				{
					ui.SpawnPopupMessage("There is no DATA\\LEV.ARK next to " + Path.GetFileName(path) + ".\nChoose UW.EXE or UW2.EXE in the folder where the game is installed (the one that holds DATA).");
					return;
				}
				ChooseWhatToEdit(ui, game, data, lev);
				return;
			}
			ui.SpawnPopupMessage("Choose the game's UW.EXE or UW2.EXE (or a LEV.ARK).");
		}

		/// <summary>The game's DATA and its saved games, to pick from.</summary>
		static void ChooseWhatToEdit(UIManager ui, string gameFolder, string data, string dataLev)
		{
			if (chooser != null && chooser.Root) chooser.Close();
			var profile = GameProfile.For(GameProfile.Detect(data));
			var w = UiKit.Window("Open " + profile.Name, 480, 330);
			chooser = w;
			UiKit.Label(w.Toolbar, gameFolder, 11, FontStyle.Normal, UiKit.DimTextColor, -1, 16);
			UiKit.Paragraph(w.Toolbar, "Edit the game's own data, which every new game starts from, or a saved game. A saved game has its own levels, character and conversation variables; strings, conversations, objects and graphics always come from DATA.", 11);
			UiKit.Button(w.Content, "Game data (DATA)", () => { w.Close(); OpenLevArk(ui, dataLev); }, 440, 24);
			int saves = 0;
			foreach (var s in GameSession.FindSlots(data))
			{
				if (!s.HasLevels) continue;
				saves++;
				string lev = Slashes(GameFiles.Resolve(s.Folder + "/LEV.ARK"));
				string label = s.Name + (s.IsWorkingCopy ? " (the game's working copy)" : "") + ": "
					+ (string.IsNullOrEmpty(s.Description) ? "(no description)" : s.Description)
					+ (s.Written != DateTime.MinValue ? ", " + s.Written.ToString("yyyy-MM-dd HH:mm") : "");
				var b = UiKit.Button(w.Content, label, () => { w.Close(); OpenLevArk(ui, lev); }, 440, 22);
				var t = b.GetComponentInChildren<UnityEngine.UI.Text>();
				if (t) t.alignment = TextAnchor.MiddleLeft;
				if (s.IsWorkingCopy)
					UiKit.Tooltip(b.gameObject, "SAVE0 is where the game keeps the game being played; it is overwritten when you play or load a game.");
			}
			w.Status.text = saves == 0 ? "No saved games next to DATA." : saves + " saved game(s).";
		}

		/// <summary>Opens a LEV.ARK (DATA's or a save's): at once when nothing is loaded, otherwise after asking.</summary>
		public static void OpenLevArk(UIManager ui, string levArk)
		{
			levArk = Slashes(levArk);
			if (ui.IsLoading)
			{
				ui.SpawnPopupMessage("Wait until the game has finished loading.");
				return;
			}
			// reloading the scene now would stop the save between two files
			if (ui.IsSaving)
			{
				ui.SpawnPopupMessage("Wait until saving has finished.");
				return;
			}
			if (ui.MapCreator != null && ui.MapCreator.IsInitialized)
			{
				Confirm(ui, levArk);
				return;
			}
			// a load that failed may have left part of its game behind: start from a fresh scene
			if (ui.LoadAttempted)
			{
				Reopen(levArk);
				return;
			}
			DataWriter.SavePathToFile(levArk);
			DataReader.FilePath = Slashes(GameSession.Choose(levArk));
			ui.LoadData();
		}

		static void Confirm(UIManager ui, string levArk)
		{
			var w = UiKit.Window("Open another game", 440, 190);
			w.MakeModal();	// autosave waits: it must not write what may be about to be thrown away
			UiKit.Paragraph(w.Content, "Open " + Describe(levArk) + "?\nThe data loaded now will be closed. Changes you have not saved are lost.", 12, UiKit.TextColor);
			var row = UiKit.Row(w.Content, 24);
			UiKit.Button(row, "Save, then open", () => { w.Close(); ui.SaveThen(() => Reopen(levArk)); }, 130, 22);
			UiKit.Button(row, "Open without saving", () =>
			{
				if (ui.IsSaving) { ui.SpawnPopupMessage("Wait until saving has finished."); return; }
				w.Close();
				Reopen(levArk);
			}, 150, 22);
			UiKit.Button(row, "Cancel", w.Close, 80, 22);
		}

		static string Describe(string levArk)
		{
			string dir = Path.GetDirectoryName(levArk) ?? "";
			string name = Path.GetFileName(dir) ?? "";
			if (GameSession.IsSaveFolderName(name)) return "the saved game " + name.ToUpperInvariant() + " (" + Path.GetDirectoryName(dir) + ")";
			return "the game data in " + dir;
		}

		/// <summary>Reloads the scene; the new scene's UIManager clears the old game's static state
		/// (ResetSession) and loads levArk. The statics stay until then, because the old scene
		/// keeps running until the end of this frame.</summary>
		public static void Reopen(string levArk)
		{
			DataWriter.SavePathToFile(levArk);
			pending = levArk;
			if (chooser != null && chooser.Root) chooser.Close();
			chooser = null;
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		/// <summary>Every static the loaded game left behind: data, caches, and handlers or
		/// objects that belong to the scene about to be replaced.</summary>
		public static void ResetSession()
		{
			MapCreator.ResetStatics();
			LevArk.Reset();
			CnvArk.Reset();
			Uw2T64.Reset();
			GrPictures.Reset();
			Uw1TrFiles.Reset();
			StartPosition.ExeX = null;
			StartPosition.ExeY = null;
			HuffmanNode.SetTree(null);
			TilePanel.LastTile = null;
			TilePanel.LastTileLevel = 0;
			UIManager.ItemDropdownPrefab = null;
			ZoneTools.Reset();
			ZoneWindow.ResetState();
			GameSession.LevArkPath = null;
			GameSession.SaveFolder = null;
			chooser = null;
		}

		public static string Slashes(string path) { return path == null ? null : path.Replace('\\', '/'); }
	}
}
