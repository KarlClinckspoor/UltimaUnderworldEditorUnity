using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>Edits DATA\SKILLS.DAT: each class's base attributes, bonus points and skill choices.</summary>
	public static class ClassSkillsWindow
	{
		public static void Show()
		{
			string path = ExtensionsMenu.FindDataFile("SKILLS.DAT");
			if (path == null) throw new FileNotFoundException("SKILLS.DAT is not in the DATA folder");
			var data = SkillsDat.Parse(File.ReadAllBytes(path));
			var w = UiKit.Window("Class skills and attributes (SKILLS.DAT)", 700, 600);
			var tools = UiKit.Row(w.Toolbar, 22);
			UiKit.Button(tools, "Save SKILLS.DAT", () =>
			{
				try
				{
					ExtensionsMenu.WriteDataFile(Path.GetFileName(path), data.ToBytes());
					w.Status.text = "Saved. A copy of the original is kept as SKILLS.DAT.BKP.";
				}
				catch (Exception e) { w.Status.text = "Not saved: " + e.Message; }
			}, 130, 22, UiKit.AccentColor);
			UiKit.Button(tools, "Reload", () => { w.Close(); Show(); }, 70, 22);
			UiKit.Paragraph(w.Toolbar,
				"A new character gets the class's three attributes, then its bonus points spread 1-4 at a time over random attributes (none above 30). " +
				"Then five skill entries in order: an empty entry gives nothing, a one-skill entry gives that skill, an entry with several asks the player to pick one (game/CHARGEN.C roll_stats, set_sklmnu).", 10);
			for (int c = 0; c < SkillsDat.Classes; c++)
				BuildClass(w, data, c);
		}

		static void BuildClass(Window w, SkillsDat data, int c)
		{
			var e = data.Entries[c];
			UiKit.Label(w.Content, SkillsDat.ClassNames[c], 13, FontStyle.Bold, null, -1, 20);
			var row = UiKit.Row(w.Content, 20);
			Num(row, "Strength", e.Strength, v => e.Strength = v, w);
			Num(row, "Dexterity", e.Dexterity, v => e.Dexterity = v, w);
			Num(row, "Intelligence", e.Intelligence, v => e.Intelligence = v, w);
			Num(row, "Bonus points", e.BonusPoints, v => e.BonusPoints = v, w);
			for (int k = 0; k < SkillsDat.EntriesPerClass; k++)
				BuildEntry(w, e, k);
		}

		static void Num(Transform row, string label, int value, Action<int> set, Window w)
		{
			UiKit.Label(row, label, 11, FontStyle.Normal, UiKit.DimTextColor, 80);
			UiKit.Input(row, value.ToString(), s =>
			{
				int v;
				if (FieldInspectorWindow.TryParse(s, out v) && v >= 0 && v <= 255) { set(v); w.Status.text = label + " = " + v; }
				else w.Status.text = label + ": enter 0-255";
			}, 40);
		}

		static void BuildEntry(Window w, SkillsDat.ClassEntry e, int k)
		{
			var row = UiKit.Row(w.Content, 20);
			var choice = e.Choices[k];
			var text = UiKit.Label(row, "", 11, FontStyle.Normal, UiKit.TextColor, 380);
			Action refresh = () =>
			{
				string kind = choice.Count == 0 ? "nothing" : choice.Count == 1 ? "given" : "pick one";
				var names = new List<string>();
				foreach (int s in choice) names.Add(s >= 0 && s < SkillsDat.Skills ? SkillsDat.SkillNames[s] : "?" + s);
				text.text = string.Format("Entry {0} ({1}): {2}", k + 1, kind, names.Count == 0 ? "-" : string.Join(", ", names.ToArray()));
			};
			refresh();
			var options = new List<string> { "Add skill..." };
			options.AddRange(SkillsDat.SkillNames);
			Dropdown dd = null;
			dd = UiKit.Dropdown(row, options, 0, i =>
			{
				if (i <= 0) return;
				int skill = i - 1;
				if (!choice.Contains(skill)) choice.Add(skill);
				dd.value = 0;
				refresh();
			}, 130);
			UiKit.Button(row, "Remove last", () =>
			{
				if (choice.Count > 0) choice.RemoveAt(choice.Count - 1);
				refresh();
			}, 90);
		}
	}
}
