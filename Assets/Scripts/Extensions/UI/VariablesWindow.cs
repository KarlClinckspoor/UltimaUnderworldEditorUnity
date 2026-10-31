using System;
using System.Collections.Generic;
using System.IO;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Tools > Quest flags and variables: the game state a save keeps in its PLAYER.DAT that
	/// traps, conversations and the game's code read and set (chapter 10 of the documentation).
	/// UW1 (uw1/src/include/player.h): quests 0-31 a bit each at 0x65, quests 32-35 bytes at
	/// 0x69, the 64 game variables (set and check variable traps) bytes at 0x70.
	/// UW2 (uw2/src/include/player.h): quests 0-127 a bit each, four to each long at 0x66
	/// (bit quest &amp; 3 of long quest &gt;&gt; 2; conv/BABLHACK.C get_quest), quests 128-143
	/// bytes at 0xE6, 256 numbered variables words at 0xF9, 16 X clocks bytes at 0x36D.
	/// The conversations' own variables are BGLOBALS.DAT (Tools > Saved games).
	/// </summary>
	public static class VariablesWindow
	{
		static readonly Dictionary<int, string> Uw1Quests = new Dictionary<int, string>
		{
			{ 0, "Murgo freed" }, { 1, "talked to Hagbard" }, { 2, "met Dr. Owl" }, { 3, "may speak to King Ketchaval" },
			{ 4, "Goldthirst's gazer killed" }, { 5, "Garamon: find the talismans" }, { 6, "friend of the lizardmen" },
			{ 7, "conversation 24, Murgo" }, { 8, "Bronus's book for Morlock" }, { 9, "find Gurstang" },
			{ 10, "where Zak is, for Delanrey" }, { 11, "Rodrick killed" },
			{ 32, "Knight of the Crux (1 seek Dorna, 2 the writ of Lorne, 3 writ found, 4 armoury open)" },
		};
		static readonly Dictionary<int, string> Uw2QuestBytes = new Dictionary<int, string>
		{
			{ 128, "lines of power cut (a bit each)" }, { 129, "pit fights won-lost" }, { 130, "blackrock gems used (a bit each)" },
			{ 133, "Jospur's debt (pit fights unpaid)" }, { 134, "Killorn's countdown" }, { 135, "bloodworms killed (sewers)" },
			{ 141, "worlds visited (a bit each)" }, { 143, "cutscene to play after a conversation, plus one" },
		};
		static readonly string[] Uw2Clocks = { "time of day (0-71)", "castle plot", "Nystul and the gems", "djinn capture", "", "", "", "", "", "", "", "", "", "", "most kills in the pits", "changed (counts X clock events)" };

		public static void Show()
		{
			string root = Path.GetDirectoryName(ExtensionsMenu.DataFolder.TrimEnd('/', '\\'));
			var saves = PlayerWindow.FindSaves(root);
			if (saves.Count == 0) throw new FileNotFoundException("No saved game (SAVE0-SAVE9 with a PLAYER.DAT) next to the DATA folder. A new game starts with every flag and variable at 0.");
			var w = UiKit.Window("Quest flags and variables (a save's PLAYER.DAT)", 820, 640);
			var names = new List<string>();
			int current = 0;
			for (int i = 0; i < saves.Count; i++)
			{
				names.Add(Path.GetFileName(Path.GetDirectoryName(saves[i])));
				if (GameSession.IsSave && string.Equals(Path.GetFullPath(Path.GetDirectoryName(saves[i])), Path.GetFullPath(GameSession.SaveFolder.TrimEnd('/', '\\')), StringComparison.OrdinalIgnoreCase)) current = i;
			}
			var tools = UiKit.Row(w.Toolbar, 22);
			bool onlySet = false;
			UiKit.Dropdown(tools, names, current, i => { current = i; Load(w, saves[current], onlySet); }, 120);
			UiKit.Check(tools, "Only non-zero", onlySet, b => { onlySet = b; Load(w, saves[current], onlySet); }, 120);
			UiKit.Paragraph(w.Toolbar, "What traps (set and check variable), conversations (get_quest, set_quest) and the game's code keep in the save. " +
				"Edit, then Save PLAYER.DAT (a .BKP copy is kept). Conversations' own variables are in Tools > Saved games.", 10);
			Load(w, saves[current], onlySet);
		}

		static void Load(Window w, string path, bool onlySet)
		{
			UiKit.Clear(w.Content);
			var bytes = File.ReadAllBytes(path);
			var kind = PlayerDat.Detect(bytes);
			if (kind == null) { w.Status.text = Path.GetFileName(path) + " fits neither game's save layout."; return; }
			var p = PlayerDat.Parse(bytes, kind.Value);
			bool uw2 = kind.Value == GameKind.UW2;
			var r = p.Record;

			var top = UiKit.Row(w.Content, 22);
			UiKit.Button(top, "Save PLAYER.DAT", () =>
			{
				GameWrite.Write(path, p.ToBytes());
				w.Status.text = "Saved " + path;
			}, 130, 22, UiKit.AccentColor);
			UiKit.Button(top, "Reload", () => Load(w, path, onlySet), 70, 22);

			// quest flags
			int flags = uw2 ? 128 : 32;
			UiKit.Label(w.Content, "Quest flags 0-" + (flags - 1) + (uw2 ? " (set variable traps: variable 0x100 + n)" : " (set variable trap with z 0; conversations reach only 0-15 as the game is written)"), 12, FontStyle.Bold);
			RectTransform row = null;
			int inRow = 0;
			for (int q = 0; q < flags; q++)
			{
				int at = uw2 ? 0x66 + (q >> 2) * 4 : 0x65 + q / 8;
				int bit = uw2 ? q & 3 : q % 8;
				bool on = (r[at] & (1 << bit)) != 0;
				if (onlySet && !on) continue;
				if (row == null || inRow == 4) { row = UiKit.Row(w.Content, 20, 4); inRow = 0; }
				inRow++;
				string name;
				if (!uw2 && Uw1Quests.TryGetValue(q, out name)) name = q + " " + name; else name = q.ToString();
				int a = at, b = bit;
				UiKit.Check(row, name, on, v => { r[a] = (byte)(v ? r[a] | (1 << b) : r[a] & ~(1 << b)); }, 190);
			}

			// quest bytes
			int first = uw2 ? 128 : 32, count = uw2 ? 16 : 4, qbAt = uw2 ? 0xE6 : 0x69;
			UiKit.Label(w.Content, "Quests " + first + "-" + (first + count - 1) + " (a byte each" + (uw2 ? "; variable 0x180 + n - 128" : "") + ")", 12, FontStyle.Bold);
			row = null; inRow = 0;
			for (int i = 0; i < count; i++)
			{
				int at = qbAt + i, q = first + i;
				if (onlySet && r[at] == 0) continue;
				if (row == null || inRow == 2) { row = UiKit.Row(w.Content, 20, 4); inRow = 0; }
				inRow++;
				string name;
				if (uw2 ? Uw2QuestBytes.TryGetValue(q, out name) : Uw1Quests.TryGetValue(q, out name)) { } else name = "";
				ByteField(w, row, q + (name.Length > 0 ? " " + name : ""), r, at, 330);
			}

			// numbered / game variables
			int vars = uw2 ? 256 : 64, vAt = uw2 ? 0xF9 : 0x70;
			UiKit.Label(w.Content, uw2 ? "Numbered variables 0-255 (set and check variable traps; traps keep 8 bits)" : "Game variables 1-63 (set and check variable traps with z = n; 6 bits, 0-63)", 12, FontStyle.Bold);
			row = null; inRow = 0;
			for (int i = 0; i < vars; i++)
			{
				int at = uw2 ? vAt + i * 2 : vAt + i;
				int v = uw2 ? r[at] | r[at + 1] << 8 : r[at];
				if (onlySet && v == 0) continue;
				if (row == null || inRow == 8) { row = UiKit.Row(w.Content, 20, 4); inRow = 0; }
				inRow++;
				int a = at, n = i;
				UiKit.Label(row, n.ToString(), 10, FontStyle.Normal, UiKit.DimTextColor, 26);
				UiKit.Input(row, v.ToString(), s =>
				{
					int x;
					int max = uw2 ? 65535 : 63;	// UW1 traps keep 6 bits
					if (!int.TryParse(s, out x) || x < 0 || x > max) { w.Status.text = "Variable " + n + " is 0-" + max + "."; return; }
					r[a] = (byte)x;
					if (uw2) r[a + 1] = (byte)(x >> 8);
				}, 54);
			}

			if (uw2)
			{
				UiKit.Label(w.Content, "X clocks 0-15 (variable 0x190 + n; the schedules read them)", 12, FontStyle.Bold);
				row = null; inRow = 0;
				for (int i = 0; i < 16; i++)
				{
					int at = 0x36D + i;
					if (onlySet && r[at] == 0) continue;
					if (row == null || inRow == 2) { row = UiKit.Row(w.Content, 20, 4); inRow = 0; }
					inRow++;
					ByteField(w, row, i + (Uw2Clocks[i].Length > 0 ? " " + Uw2Clocks[i] : ""), r, at, 330);
				}
			}
			else
			{
				UiKit.Label(w.Content, "Other", 12, FontStyle.Bold);
				row = UiKit.Row(w.Content, 20, 4);
				ByteField(w, row, "talismans left (0xFF once the last is gone)", r, 0x6D, 330);
			}
			w.Status.text = (uw2 ? "UW2" : "UW1") + " save " + Path.GetFileName(Path.GetDirectoryName(path)) + ".";
		}

		static void ByteField(Window w, Transform row, string label, byte[] r, int at, float width)
		{
			UiKit.Label(row, label, 11, FontStyle.Normal, null, width - 60);
			UiKit.Input(row, r[at].ToString(), s =>
			{
				int x;
				if (int.TryParse(s, out x) && x >= 0 && x <= 255) r[at] = (byte)x;
				else w.Status.text = label + " is 0-255.";
			}, 54);
		}
	}
}
