using System;
using System.Collections.Generic;
using System.IO;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// The saved games next to DATA (Data/GameSession.cs): their descriptions, which one the
	/// editor has open, opening another, and each save's conversation variables (BGLOBALS.DAT).
	/// The levels of a save are edited in the main view after opening it; its character in
	/// Tools > Character in a save.
	/// </summary>
	public static class SaveSlotsWindow
	{
		public static void Show()
		{
			var slots = GameSession.FindSlots(ExtensionsMenu.DataFolder);
			var w = UiKit.Window("Saved games", 780, 560);
			UiKit.Paragraph(w.Toolbar,
				"Open: " + (GameSession.IsSave ? GameSession.SaveName + " (its LEV.ARK; strings, conversations and tables from DATA)" : "DATA (what a new game starts with)") + ". " +
				"A save has its own LEV.ARK (with the automap and notes), PLAYER.DAT, BGLOBALS.DAT, DESC" + (LevArk.Game.Kind == GameKind.UW2 ? " and SCD.ARK" : "") +
				". SAVE0 is the game's working copy, overwritten when a game is loaded or started. Choosing a save's LEV.ARK in File > Open also opens it.", 10);
			if (slots.Count == 0) { UiKit.Label(w.Content, "No SAVE folders next to DATA.", 11, FontStyle.Italic, UiKit.DimTextColor); return; }
			foreach (var s in slots) BuildSlot(w, s);
		}

		static void BuildSlot(Window w, GameSession.Slot s)
		{
			var row = UiKit.Row(w.Content, 22, 4);
			bool open = GameSession.IsSave && string.Equals(Path.GetFullPath(s.Folder), Path.GetFullPath(GameSession.SaveFolder), StringComparison.OrdinalIgnoreCase);
			UiKit.Label(row, s.Name + (open ? " (open)" : ""), 11, FontStyle.Bold, open ? UiKit.WarnColor : (Color?)null, 90);
			string descPath = GameFiles.Resolve(Path.Combine(s.Folder, "DESC"));
			string desc = s.Description;
			UiKit.Input(row, desc, t => desc = t, 230, false);
			UiKit.Button(row, "Save name", () =>
			{
				GameSession.WriteDesc(descPath, desc);
				w.Status.text = s.Name + ": description saved" + (desc.Length > GameSession.DescMax ? " (cut to the game's 30 characters)." : ".");
			}, 80, 20);
			UiKit.Button(row, "Open", () =>
			{
				string lev = GameFiles.Resolve(Path.Combine(s.Folder, "LEV.ARK"));
				if (!File.Exists(lev)) { w.Status.text = s.Name + " has no LEV.ARK."; return; }
				var uim = UiKit.FindUIManager();
				if (!uim) return;
				w.Close();
				// with data loaded this asks first, then reopens the editor's scene on the save
				GameOpener.OpenLevArk(uim, lev);
			}, 50, 20);
			UiKit.Button(row, "Character", () => PlayerWindow.Show(s.Folder), 70, 20);
			UiKit.Button(row, "Conversation variables", () => ShowGlobals(s), 150, 20);
			UiKit.Label(w.Content, string.Format("   {0}{1}{2}", s.HasLevels ? "LEV.ARK" : "no LEV.ARK", s.HasPlayer ? ", PLAYER.DAT" : "",
				s.Written != DateTime.MinValue ? ", written " + s.Written.ToString("yyyy-MM-dd HH:mm") : ""), 10, FontStyle.Normal, UiKit.DimTextColor);
		}

		/// <summary>The save's BGLOBALS.DAT: each conversation's private variables as the save has them.</summary>
		static void ShowGlobals(GameSession.Slot s)
		{
			string path = GameFiles.Resolve(Path.Combine(s.Folder, "BGLOBALS.DAT"));
			if (!File.Exists(path)) throw new FileNotFoundException(s.Name + " has no BGLOBALS.DAT");
			var g = BGlobals.Parse(File.ReadAllBytes(path), true);
			// DATA's BABGLOBS.DAT gives the sizes the conversations expect now
			BGlobals data = null;
			string babPath = ExtensionsMenu.FindDataFile("BABGLOBS.DAT");
			if (babPath != null) data = BGlobals.Parse(File.ReadAllBytes(babPath), false);

			var w = UiKit.Window("Conversation variables of " + s.Name + " (BGLOBALS.DAT)", 760, 560);
			var names = new List<string>();
			foreach (var r in g.Records)
			{
				var d = data != null ? data.Find(r.Slot) : null;
				names.Add(string.Format("{0} {1} ({2} words{3})", r.Slot, NpcName(r.Slot) ?? "", r.Words.Length,
					d != null && d.Words.Length != r.Words.Length ? ", DATA has " + d.Words.Length : ""));
			}
			if (names.Count == 0) { UiKit.Label(w.Content, "No records.", 11, FontStyle.Italic, UiKit.DimTextColor); return; }
			int current = 0;
			var tools = UiKit.Row(w.Toolbar, 22);
			UiKit.Dropdown(tools, names, 0, i => { current = i; Fill(w, g, data, current); }, 330);
			UiKit.Button(tools, "Save BGLOBALS.DAT", () =>
			{
				GameSession.BackupOnce(path);
				File.WriteAllBytes(path, g.ToBytes(true));
				w.Status.text = "Saved " + path;
			}, 140, 22, UiKit.AccentColor);
			UiKit.Paragraph(w.Toolbar, "The variables a conversation keeps between talks (the game reads at most as many as the script declares). They are shown by index, in the order the script declares them.", 10);
			Fill(w, g, data, 0);
		}

		static void Fill(Window w, BGlobals g, BGlobals data, int index)
		{
			UiKit.Clear(w.Content);
			var r = g.Records[index];
			var d = data != null ? data.Find(r.Slot) : null;
			if (d != null && d.Words.Length != r.Words.Length)
			{
				var row = UiKit.Row(w.Content, 22);
				UiKit.Label(row, string.Format("DATA's BABGLOBS.DAT gives this conversation {0} words; the save has {1}.", d.Words.Length, r.Words.Length), 11, FontStyle.Normal, UiKit.WarnColor, 420);
				UiKit.Button(row, "Resize to " + d.Words.Length, () => { g.Resize(r.Slot, d.Words.Length); Fill(w, g, data, index); }, 110, 20);
			}
			for (int i = 0; i < r.Words.Length; i += 4)
			{
				var row = UiKit.Row(w.Content, 20, 4);
				for (int k = i; k < Math.Min(i + 4, r.Words.Length); k++)
				{
					int idx = k;
					UiKit.Label(row, VarName(idx), 10, FontStyle.Normal, UiKit.DimTextColor, 90);
					UiKit.Input(row, r.Words[idx].ToString(), t => { int v; if (int.TryParse(t, out v) && v >= -32768 && v <= 65535) r.Words[idx] = (short)v; else w.Status.text = "Variables are 16-bit numbers."; }, 60);
				}
			}
		}

		static string VarName(int i) { return "var " + i; }

		static string NpcName(int slot)
		{
			var sd = MapCreator.StringData;
			if (sd == null || sd.Blocks == null) return null;
			foreach (var b in sd.Blocks)
				if (b.BlockNumber == 7) return slot >= 0 && slot < b.Strings.Count ? b.Strings[slot] : null;
			return null;
		}
	}
}
