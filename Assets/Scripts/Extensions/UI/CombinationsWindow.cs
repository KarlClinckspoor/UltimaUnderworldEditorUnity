using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>Edits DATA\CMB.DAT, the ten item combination rules (obj/COMBINE.C).</summary>
	public static class CombinationsWindow
	{
		public static void Show()
		{
			string path = ExtensionsMenu.FindDataFile("CMB.DAT");
			if (path == null) throw new FileNotFoundException("CMB.DAT is not in the DATA folder");
			var data = CmbDat.Parse(File.ReadAllBytes(path));
			var w = UiKit.Window("Item combinations (CMB.DAT)", 760, 420);
			var tools = UiKit.Row(w.Toolbar, 22);
			UiKit.Button(tools, "Save CMB.DAT", () =>
			{
				ExtensionsMenu.WriteDataFile(Path.GetFileName(path), data.ToBytes());
				var issues = data.Check();
				w.Status.text = "Saved." + (issues.Count > 0 ? " Warnings: " + string.Join(" ", issues.ToArray()) : "");
			}, 120, 22, UiKit.AccentColor);
			UiKit.Button(tools, "Reload", () => { w.Close(); Show(); }, 70, 22);
			UiKit.Button(tools, "Check", () =>
			{
				var issues = data.Check();
				w.Status.text = issues.Count == 0 ? "No problems found." : string.Join(" ", issues.ToArray());
			}, 70, 22);
			UiKit.Paragraph(w.Toolbar,
				"Using one item on the other (either order) in the inventory makes the output. Neither may be a stack or hold anything. A rule only works when at least one source is marked used up; the game scans all ten rules and takes the first match. Items are entered as hex ids (0x095) or picked from the list.", 10);
			for (int i = 0; i < CmbDat.Rules; i++)
				BuildRule(w, data, i);
		}

		static void BuildRule(Window w, CmbDat data, int i)
		{
			var row = UiKit.Row(w.Content, 22);
			UiKit.Label(row, (i + 1) + ".", 11, FontStyle.Bold, null, 20);
			ItemSlot(row, w, () => data.Entries[i].FirstItem, id => { var r = data.Entries[i]; r.First = (r.First & CmbDat.UsedUp) | id; data.Entries[i] = r; });
			UsedUp(row, () => data.Entries[i].FirstUsedUp, b => { var r = data.Entries[i]; r.First = (r.First & 0x1FF) | (b ? CmbDat.UsedUp : 0); data.Entries[i] = r; });
			UiKit.Label(row, "+", 12, FontStyle.Bold, null, 10);
			ItemSlot(row, w, () => data.Entries[i].SecondItem, id => { var r = data.Entries[i]; r.Second = (r.Second & CmbDat.UsedUp) | id; data.Entries[i] = r; });
			UsedUp(row, () => data.Entries[i].SecondUsedUp, b => { var r = data.Entries[i]; r.Second = (r.Second & 0x1FF) | (b ? CmbDat.UsedUp : 0); data.Entries[i] = r; });
			UiKit.Label(row, "=", 12, FontStyle.Bold, null, 10);
			ItemSlot(row, w, () => data.Entries[i].OutputItem, id => { var r = data.Entries[i]; r.Output = id; data.Entries[i] = r; });
		}

		static void UsedUp(Transform row, Func<bool> get, Action<bool> set)
		{
			UiKit.Cycle(row, new[] { "kept", "used up" }, get() ? 1 : 0, i => set(i == 1), 60);
		}

		static void ItemSlot(Transform row, Window w, Func<int> get, Action<int> set)
		{
			InputField input = null;
			Text name = null;
			Action refresh = () =>
			{
				int id = get();
				input.text = string.Format("0x{0:X3}", id);
				name.text = StaticObject.GetName(id);
			};
			input = UiKit.Input(row, "", s =>
			{
				int v;
				if (FieldInspectorWindow.TryParse(s, out v) && v >= 0 && v <= 0x1FF) set(v);
				else w.Status.text = "Item ids are 0x000-0x1FF";
				refresh();
			}, 48);
			name = UiKit.Label(row, "", 10, FontStyle.Normal, UiKit.DimTextColor, 110);
			UiKit.Button(row, "...", () =>
			{
				var ui = UiKit.FindUIManager();
				var ids = new List<int>();
				for (int k = 0; k < 0x1C0; k++) ids.Add(k);
				ui.CreateSpecifiedItemList(id => { set(id); refresh(); }, ids, "Pick an item", "Item for the combination");
			}, 24);
			refresh();
		}
	}
}
