using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Finds placed objects on every level (or one) by name, item id, category and flags,
	/// with <see cref="ItemFilter"/>; clicking a result switches to its level and selects it.
	/// </summary>
	public static class ItemFinderWindow
	{
		const int MaxRows = 400;
		static readonly string[] TriLabels = { "any", "yes", "no" };

		public static void Show()
		{
			var ui = UiKit.FindUIManager();
			var filter = new ItemFilter();
			var w = UiKit.Window("Find items", 720, 600);

			var r1 = UiKit.Row(w.Toolbar, 22);
			UiKit.Label(r1, "Name or 0x id", 11, FontStyle.Normal, UiKit.DimTextColor, 80);
			UiKit.Input(r1, "", s => { filter.NameContains = s.Trim(); Run(w, ui, filter); }, 160, false);
			var cats = new List<string> { "All categories" };
			foreach (var c in Uw1ItemCategories.Choices) cats.Add(c.Value);
			UiKit.Dropdown(r1, cats, 0, i => { filter.Categories = i == 0 ? ItemCategory.None : Uw1ItemCategories.Choices[i - 1].Key; Run(w, ui, filter); }, 220);
			var levels = new List<string> { "All levels" };
			for (int i = 1; i <= MapCreator.LevelData.Count; i++) levels.Add(LevArk.Game.LevelName(i));
			UiKit.Dropdown(r1, levels, 0, i => { filter.Level = i; Run(w, ui, filter); }, 100);

			var r2 = UiKit.Row(w.Toolbar, 22);
			Tri(r2, "Enchanted", v => filter.Enchanted = v, w, ui, filter);
			Tri(r2, "Invisible", v => filter.Invisible = v, w, ui, filter);
			Tri(r2, "Quantity", v => filter.Quantity = v, w, ui, filter);
			Tri(r2, "Owned", v => filter.Owned = v, w, ui, filter);
			Tri(r2, "In container", v => filter.InContainer = v, w, ui, filter);

			var r3 = UiKit.Row(w.Toolbar, 22);
			UiKit.Label(r3, "Quality", 11, FontStyle.Normal, UiKit.DimTextColor, 45);
			UiKit.Input(r3, "0", s => { int v; if (FieldInspectorWindow.TryParse(s, out v)) filter.MinQuality = v; Run(w, ui, filter); }, 36);
			UiKit.Label(r3, "to", 11, FontStyle.Normal, UiKit.DimTextColor, 16);
			UiKit.Input(r3, "63", s => { int v; if (FieldInspectorWindow.TryParse(s, out v)) filter.MaxQuality = v; Run(w, ui, filter); }, 36);
			UiKit.Button(r3, "Search", () => Run(w, ui, filter), 80, 20, UiKit.AccentColor);
			Run(w, ui, filter);
		}

		static void Tri(Transform row, string label, Action<Want> set, Window w, UIManager ui, ItemFilter filter)
		{
			UiKit.Label(row, label, 11, FontStyle.Normal, UiKit.DimTextColor, 64);
			UiKit.Cycle(row, TriLabels, 0, i => { set((Want)i); Run(w, ui, filter); }, 40);
		}

		public static List<KeyValuePair<ObjectView, StaticObject>> Collect()
		{
			var all = new List<KeyValuePair<ObjectView, StaticObject>>();
			for (int l = 0; l < MapCreator.LevelData.Count; l++)
			{
				var objects = MapCreator.LevelData[l].Objects;
				for (int i = 0; i < objects.Length; i++)
				{
					var so = objects[i];
					if (!so) continue;
					all.Add(new KeyValuePair<ObjectView, StaticObject>(View(so, l + 1, i), so));
				}
			}
			return all;
		}

		public static ObjectView View(StaticObject so, int level, int index)
		{
			bool inContainer = so.GetContainer() != null;
			return new ObjectView
			{
				Level = level, Index = index, Item = so.ObjectID, Name = so.GetFullName(),
				Enchanted = so.IsEnchanted, Invisible = so.IsInvisible, IsQuantity = so.IsQuantity,
				Quality = so.Quality, Owner = so.Owner, Link = so.Special, InContainer = inContainer,
				TileX = !inContainer && so.Tile != null ? so.Tile.Position.x : -1,
				TileY = !inContainer && so.Tile != null ? so.Tile.Position.y : -1,
			};
		}

		static void Run(Window w, UIManager ui, ItemFilter filter)
		{
			UiKit.Clear(w.Content);
			int shown = 0, total = 0;
			foreach (var kv in Collect())
			{
				if (!filter.Matches(kv.Key)) continue;
				total++;
				if (shown >= MaxRows) continue;
				shown++;
				var v = kv.Key; var so = kv.Value;
				string where = v.InContainer ? "in a container" : v.TileX >= 0 ? string.Format("tile {0},{1}", v.TileX, v.TileY) : "off the map";
				string text = string.Format("L{0}  #{1}  0x{2:X3}  {3}  ({4}{5}{6})", v.Level, v.Index, v.Item, v.Name, where,
					v.Enchanted ? ", enchanted" : "", v.Invisible ? ", invisible" : "");
				var b = UiKit.Button(w.Content, text, () => GoTo(ui, so), -1, 20);
				var t = b.GetComponentInChildren<Text>();
				if (t) t.alignment = TextAnchor.MiddleLeft;
			}
			w.Status.text = total > shown ? string.Format("{0} objects match; the first {1} are shown.", total, shown) : total + " objects match.";
		}

		public static void GoTo(UIManager ui, StaticObject so)
		{
			if (ui.CurrentLevel != so.CurrentLevel && ui.LevelSlider)
			{
				var slider = ui.LevelSlider.GetComponent<Slider>();
				if (slider) { slider.value = so.CurrentLevel; ui.ChangeLevelView(slider); }
			}
			if (!ui.SelectObject(so, true))
				ui.SetObjectProperties(so);
		}
	}
}
