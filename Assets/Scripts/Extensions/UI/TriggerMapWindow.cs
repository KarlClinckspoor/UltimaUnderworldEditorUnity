using System;
using System.Collections.Generic;
using System.Linq;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Tools > Triggers and traps: every trigger of the shown level with where it lies (a
	/// square, or inside a lever or door), the square its chain runs on and the chain itself;
	/// then the traps no trigger reaches (fired by a door, a hack, a schedule, a
	/// conversation or the game's own code, or by nothing). Each entry selects its object.
	/// </summary>
	public static class TriggerMapWindow
	{
		public static void Show()
		{
			var ui = UiKit.FindUIManager();
			var w = UiKit.Window("Triggers and traps", 760, 620);
			var tools = UiKit.Row(w.Toolbar, 22);
			UiKit.Button(tools, "This level", () => Fill(w, ui), 90, 22);
			UiKit.Paragraph(w.Toolbar, "A trigger fires the trap its link names, and the chain runs on the trigger's target square (quality, owner), " +
				"not where the objects lie. Each trap passes on along its link unless it ends the chain. 'Select' centres the map on the object " +
				"(or on what holds it). Chapters 8-10 of the documentation explain each kind.", 10);
			Fill(w, ui);
		}

		static void Fill(Window w, UIManager ui)
		{
			UiKit.Clear(w.Content);
			int level = ui.CurrentLevel;
			var ld = MapCreator.LevelData[level - 1];
			var objects = ld.Objects.Where(o => o != null && o.CurrentLevel == level).ToList();
			var triggers = objects.Where(o => o.IsTrigger() && Placed(o)).OrderBy(o => o.CurrentAdress).ToList();
			var reached = new HashSet<StaticObject>();

			UiKit.Label(w.Content, LevArk.Game.LevelName(level) + ": " + triggers.Count + " triggers", 12, FontStyle.Bold);
			foreach (var t in triggers)
			{
				var chain = TrapLinks.Chain(t);
				var head = UiKit.Row(w.Content, 20);
				SelectButton(head, ui, t);
				UiKit.Label(head, string.Format("{0} [{1}] {2}  ->  runs on ({3}, {4})", t.Name, t.CurrentAdress, Where(t), t.Quality, t.Owner), 11, FontStyle.Normal, null, 640);
				if (chain.Count == 1)
				{
					var r = UiKit.Row(w.Content, 18);
					UiKit.Label(r, "", 11, FontStyle.Normal, null, 30);
					UiKit.Label(r, t.Special > 0 && !t.IsQuantity ? "links to an empty slot" : "links to nothing: it does nothing", 11, FontStyle.Italic, UiKit.WarnColor, 600);
				}
				for (int i = 1; i < chain.Count; i++)
				{
					var o = chain[i];
					reached.Add(o);
					var r = UiKit.Row(w.Content, 18);
					UiKit.Label(r, "", 11, FontStyle.Normal, null, 18 + 12 * Math.Min(i, 8));
					SelectButton(r, ui, o);
					string note = o.IsTrap() ? "" : o.IsTrigger() ? "  (a trigger: runs its own chain on its own square)" : "  (not a trap)";
					UiKit.Label(r, (i == 1 ? "fires " : "then ") + o.Name + " [" + o.CurrentAdress + "] " + Where(o) + note, 11, FontStyle.Normal, UiKit.DimTextColor, 560);
				}
			}

			// traps no trigger reaches
			var traps = objects.Where(o => o.IsTrap() && Placed(o) && !reached.Contains(o)).OrderBy(o => o.CurrentAdress).ToList();
			UiKit.Label(w.Content, traps.Count + " traps no trigger reaches", 12, FontStyle.Bold);
			if (traps.Count > 0)
				UiKit.Paragraph(w.Content, "A trap in a door fires when the door is used. Others are fired by a schedule (UW2), a conversation, a hack trap or the game's own code (chapter 10), or never. A chain that loops back is shown once.", 10, UiKit.DimTextColor);
			foreach (var o in traps)
			{
				var r = UiKit.Row(w.Content, 18);
				SelectButton(r, ui, o);
				var from = TrapLinks.LinkedFrom(o);
				string by = from.Count > 0 ? "  linked from " + string.Join(", ", from.Select(f => f.Name + " [" + f.CurrentAdress + "]").ToArray()) : "";
				UiKit.Label(r, o.Name + " [" + o.CurrentAdress + "] " + Where(o) + by, 11, FontStyle.Normal, null, 640);
			}
			w.Status.text = string.Format("{0}: {1} triggers, {2} traps in chains, {3} other traps.", LevArk.Game.LevelName(level), triggers.Count, reached.Count(o => o.IsTrap()), traps.Count);
		}

		/// <summary>On the map or inside something on it (not a free slot).</summary>
		static bool Placed(StaticObject o)
		{
			if (o.Tile != null) return true;
			var c = o.GetFirstContainer();
			return c != null && c != o && c.Tile != null;
		}

		static string Where(StaticObject o)
		{
			var c = o.GetContainer();
			if (c != null && !(c.IsTrap() || c.IsTrigger()))
				return "in " + c.Name + " [" + c.CurrentAdress + "]" + (c.Tile != null ? " at (" + c.Tile.Position.x + ", " + c.Tile.Position.y + ")" : "");
			if (o.Tile != null)
				return "at (" + o.Tile.Position.x + ", " + o.Tile.Position.y + ")";
			return "";
		}

		static void SelectButton(Transform row, UIManager ui, StaticObject o)
		{
			UiKit.Button(row, "Select", () => ui.SelectObject(o, true), 56, 18);
		}
	}
}
