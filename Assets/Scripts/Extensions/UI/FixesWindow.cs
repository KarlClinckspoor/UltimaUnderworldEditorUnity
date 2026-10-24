using System;
using System.Collections.Generic;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// The Fixes tab (Data/LevelFixes.cs): the game's own consistency rules checked on the level
	/// blocks the editor would save, each problem with its fix; and the item cleaner, which
	/// lists what a filter would remove before removing it through MapCreator.RemoveObject.
	/// Both games.
	/// </summary>
	public static class FixesWindow
	{
		const int MaxRows = 300;

		public static void Show()
		{
			var ui = UiKit.FindUIManager();
			var w = UiKit.Window("Fixes", 820, 620);
			int level = 0;   // 0 all
			var tools = UiKit.Row(w.Toolbar, 22);
			var levels = new List<string> { "All levels" };
			for (int i = 1; i <= MapCreator.LevelData.Count; i++) levels.Add(LevArk.Game.LevelName(i) + (MapCreator.LevelData[i - 1].Present ? "" : " (empty)"));
			UiKit.Dropdown(tools, levels, ui ? ui.CurrentLevel : 0, i => level = i, 260);
			UiKit.Button(tools, "Check", () => RunChecks(w, ui, level), 70, 22, UiKit.AccentColor);
			UiKit.Button(tools, "Item cleaner...", () => Cleaner(ui), 110, 22);
			UiKit.Paragraph(w.Toolbar,
				"Checks what the game itself requires (obj/OBJECTS.C Obj_ListOkay; the active critter list), on the level as it would be saved: every object either free or reached from a tile exactly once; " +
				"critters on the map on the active list and nothing else; trigger links and trap chains; items under the floor. Fixes change the editor's data; save as usual.", 10);
			level = ui ? ui.CurrentLevel : 0;
			RunChecks(w, ui, level);
		}

		static void RunChecks(Window w, UIManager ui, int level)
		{
			UiKit.Clear(w.Content);
			var issues = new List<LevelFixes.Issue>();
			for (int l = 1; l <= MapCreator.LevelData.Count; l++)
				if (level == 0 || level == l)
					issues.AddRange(LevelFixes.Check(MapCreator.LevelData[l - 1], LevArk.Game.Kind));
			int fixable = issues.FindAll(i => i.Fix != null).Count;
			var head = UiKit.Row(w.Content, 22);
			UiKit.Label(head, issues.Count == 0 ? "No problems found." : string.Format("{0} problems, {1} with a fix.", issues.Count, fixable), 12, FontStyle.Bold, null, 300);
			if (fixable > 0)
				UiKit.Button(head, "Fix all (except items under the floor)", () =>
				{
					// rebuilds last: freeing objects first lets the rebuilt lists include them
					foreach (var i in issues) if (i.Fix != null && i.Kind != "Below floor" && !i.Text.StartsWith("rebuild")) i.Fix();
					foreach (var i in issues) if (i.Fix != null && i.Text.StartsWith("rebuild")) i.Fix();
					RunChecks(w, ui, level);
				}, 260, 22, UiKit.WarnColor);
			int shown = 0;
			foreach (var issue in issues)
			{
				if (shown++ >= MaxRows) break;
				var row = UiKit.Row(w.Content, 20, 4);
				var t = UiKit.Label(row, issue.ToString(), 10, FontStyle.Normal, null, 600);
				t.horizontalOverflow = HorizontalWrapMode.Wrap;
				var it = issue;
				if (it.Object >= 0 && it.Level >= 1 && MapCreator.LevelData[it.Level - 1].Objects[it.Object] != null)
					UiKit.Button(row, "Select", () => ItemFinderWindow.GoTo(ui, MapCreator.LevelData[it.Level - 1].Objects[it.Object]), 50, 20);
				if (it.Fix != null)
					UiKit.Button(row, "Fix", () => { it.Fix(); RunChecks(w, ui, level); }, 40, 20, UiKit.AccentColor);
			}
			w.Status.text = issues.Count > MaxRows ? "The first " + MaxRows + " are shown." : "";
		}

		// ---- item cleaner

		public sealed class CleanFilter
		{
			public int Level;                         // 0 all
			public int X0 = 0, Y0 = 0, X1 = 63, Y1 = 63;
			public ItemCategory Categories = ItemCategory.Scenery;
			public bool IncludeContainers;            // things inside chests and NPCs too
			public string Name = "";
		}

		static void Cleaner(UIManager ui)
		{
			var f = new CleanFilter { Level = ui ? ui.CurrentLevel : 1 };
			var w = UiKit.Window("Item cleaner", 760, 600);
			var r1 = UiKit.Row(w.Toolbar, 22);
			var levels = new List<string> { "All levels" };
			for (int i = 1; i <= MapCreator.LevelData.Count; i++) levels.Add(LevArk.Game.LevelName(i));
			UiKit.Dropdown(r1, levels, f.Level, i => f.Level = i, 220);
			var cats = new List<string>();
			foreach (var c in Uw1ItemCategories.Choices) cats.Add(c.Value);
			int scenery = Array.FindIndex(Uw1ItemCategories.Choices, c => c.Key == ItemCategory.Scenery);
			UiKit.Dropdown(r1, cats, Math.Max(0, scenery), i => f.Categories = Uw1ItemCategories.Choices[i].Key, 220);
			UiKit.Label(r1, "name", 10, FontStyle.Normal, UiKit.DimTextColor, 30);
			UiKit.Input(r1, "", s => f.Name = s.Trim(), 120, false);
			var r2 = UiKit.Row(w.Toolbar, 22);
			UiKit.Label(r2, "Area x0 y0 x1 y1", 10, FontStyle.Normal, UiKit.DimTextColor, 90);
			UiKit.Input(r2, "0", s => int.TryParse(s, out f.X0), 30);
			UiKit.Input(r2, "0", s => int.TryParse(s, out f.Y0), 30);
			UiKit.Input(r2, "63", s => int.TryParse(s, out f.X1), 30);
			UiKit.Input(r2, "63", s => int.TryParse(s, out f.Y1), 30);
			UiKit.Check(r2, "also inside containers and NPCs", false, b => f.IncludeContainers = b, 220);
			List<StaticObject> found = null;
			UiKit.Button(r2, "Preview", () => found = Preview(w, ui, f), 70, 22, UiKit.AccentColor);
			UiKit.Button(r2, "Remove listed", () =>
			{
				if (found == null || found.Count == 0) { w.Status.text = "Preview first."; return; }
				int n = 0;
				foreach (var so in found)
					if (so != null && MapCreator.LevelData[so.CurrentLevel - 1].Objects[so.CurrentAdress] == so && MapCreator.RemoveObject(so, true)) n++;
				w.Status.text = n + " objects removed. Run Fixes > Check to confirm the levels.";
				found = Preview(w, ui, f);
			}, 110, 22, UiKit.WarnColor);
			UiKit.Paragraph(w.Toolbar, "Never listed: anything a trap or trigger refers to (create-object templates, delete targets, inventory-trap items), keys and quest items, critters, objects with a conversation, locks, doors, traps and triggers, and containers that still hold something.", 10);
		}

		static List<StaticObject> Preview(Window w, UIManager ui, CleanFilter f)
		{
			UiKit.Clear(w.Content);
			var list = Select(f);
			int shown = 0;
			foreach (var so in list)
			{
				if (shown++ >= MaxRows) break;
				var o = so;
				var t = o.GetFirstContainer() ?? o;
				string text = string.Format("L{0}  #{1}  {2}  tile {3},{4}{5}", o.CurrentLevel, o.CurrentAdress, StaticObject.GetName(o.ObjectID), t.Tile.Position.x, t.Tile.Position.y, o.GetContainer() != null ? "  in " + StaticObject.GetName(o.GetContainer().ObjectID) : "");
				var b = UiKit.Button(w.Content, text, () => ItemFinderWindow.GoTo(ui, o), -1, 20);
				var tx = b.GetComponentInChildren<Text>();
				if (tx) tx.alignment = TextAnchor.MiddleLeft;
			}
			w.Status.text = list.Count + " objects would be removed" + (list.Count > MaxRows ? " (the first " + MaxRows + " shown)." : ".");
			return list;
		}

		/// <summary>What the cleaner would remove for this filter.</summary>
		public static List<StaticObject> Select(CleanFilter f)
		{
			var list = new List<StaticObject>();
			for (int l = 1; l <= MapCreator.LevelData.Count; l++)
			{
				if (f.Level != 0 && f.Level != l) continue;
				var ld = MapCreator.LevelData[l - 1];
				if (!ld.Present) continue;
				var referenced = Referenced(ld);
				foreach (var so in ld.Objects)
				{
					if (so == null || so.CurrentAdress < 2) continue;
					if (!Removable(so, referenced)) continue;
					if ((Uw1ItemCategories.Of(so.ObjectID) & f.Categories) == 0) continue;
					if (f.Name.Length > 0 && StaticObject.GetName(so.ObjectID).IndexOf(f.Name, StringComparison.OrdinalIgnoreCase) < 0) continue;
					var container = so.GetContainer();
					if (container != null && !f.IncludeContainers) continue;
					var top = so.GetFirstContainer() ?? so;
					var tile = top.Tile;
					if (tile == null) continue;
					if (tile.Position.x < f.X0 || tile.Position.x > f.X1 || tile.Position.y < f.Y0 || tile.Position.y > f.Y1) continue;
					list.Add(so);
				}
			}
			return list;
		}

		/// <summary>Objects that traps and triggers link to, and everything inside them (templates, targets).</summary>
		static HashSet<int> Referenced(LevelData ld)
		{
			var set = new HashSet<int>();
			foreach (var o in ld.Objects)
			{
				if (o == null || o.ObjectID < 0x180 || o.ObjectID >= 0x1C0 || o.Special <= 0 || o.Special >= ld.Objects.Length) continue;
				var stack = new Stack<int>();
				stack.Push(o.Special);
				while (stack.Count > 0)
				{
					int i = stack.Pop();
					if (i <= 0 || i >= ld.Objects.Length || !set.Add(i)) continue;
					var x = ld.Objects[i];
					if (x == null) continue;
					if (!x.IsQuantity && x.Special > 0) stack.Push(x.Special);
					if (x.NextAdress > 0 && x.GetContainer() != null) stack.Push(x.NextAdress);
				}
			}
			// a delete object trap's target is found by square: keep everything it names
			foreach (var o in ld.Objects)
				if (o != null && o.ObjectID == 0x18B && o.IsQuantity && o.Special > 0) set.Add(o.Special);
			return set;
		}

		static bool Removable(StaticObject so, HashSet<int> referenced)
		{
			if (referenced.Contains(so.CurrentAdress)) return false;
			if (so.IsMonster() || so.IsDoor() || so.IsLock()) return false;
			if (so.ObjectID >= 0x180) return false;   // traps, triggers, animated objects
			var cat = Uw1ItemCategories.Of(so.ObjectID);
			if ((cat & (ItemCategory.Key | ItemCategory.QuestKey | ItemCategory.Quest | ItemCategory.Lock)) != 0) return false;
			if (!so.IsQuantity && so.Special > 0) return false;   // holds something (a container's contents, a trap, a lock)
			var mo = so as MobileObject;
			if (mo != null && mo.Whoami > 0) return false;
			return true;
		}
	}
}
