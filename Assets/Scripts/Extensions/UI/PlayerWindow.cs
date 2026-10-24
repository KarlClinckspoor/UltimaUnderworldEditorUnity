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
		public static void Show()
		{
			string root = Path.GetDirectoryName(ExtensionsMenu.DataFolder.TrimEnd('/', '\\'));
			var saves = FindSaves(root);
			if (saves.Count == 0) throw new FileNotFoundException("No saved game (SAVE0-SAVE9 with a PLAYER.DAT) next to the DATA folder. The PLAYER.DAT in DATA is not a save.");
			var w = UiKit.Window("Character (a save's PLAYER.DAT)", 760, 600);
			var names = new List<string>();
			foreach (var s in saves) names.Add(Path.GetFileName(Path.GetDirectoryName(s)) + "/" + Path.GetFileName(s));
			var tools = UiKit.Row(w.Toolbar, 22);
			int current = 0;
			UiKit.Dropdown(tools, names, 0, i => { current = i; Load(w, saves[current]); }, 200);
			UiKit.Paragraph(w.Toolbar, "Saving keeps a one-time .BKP of the file. Vitality is written to the record and to the player object the game actually keeps. Derived values (maximum weight, armour) are recomputed by the game.", 10);
			Load(w, saves[0]);
		}

		static List<string> FindSaves(string root)
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
				p.SavedObjects, LevArk.Game.Kind == kind.Value ? GameProfile.For(kind.Value).LevelName(p.SavedLevel) : p.SavedLevel.ToString(), p.SavedX >> 3, p.SavedY >> 3);

			var top = UiKit.Row(w.Content, 22);
			UiKit.Button(top, "Save PLAYER.DAT", () =>
			{
				string bkp = path + ".BKP";
				if (!File.Exists(bkp)) File.Copy(path, bkp);
				File.WriteAllBytes(path, p.ToBytes());
				w.Status.text = "Saved " + path;
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
