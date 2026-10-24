using System;
using System.Collections.Generic;
using UltimateEditor.Uw1.Zoning;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Draws the zone graph: one column per level (a zone sits in the first level it has tiles
	/// on; zones spanning levels are marked), edges coloured by kind, one-way edges with a
	/// dot at the target end. Clicking a zone selects it for painting and the Links page.
	/// </summary>
	public static class ZoneGraphWindow
	{
		static Window open;
		const float ColumnWidth = 150, RowHeight = 26, NodeWidth = 130, NodeHeight = 20, Margin = 20;

		static readonly Dictionary<string, Color> KindColors = new Dictionary<string, Color>
		{
			{ "walk", new Color(0.75f, 0.75f, 0.75f) }, { "stairs", new Color(0.75f, 0.75f, 0.75f) },
			{ "door", new Color(0.95f, 0.80f, 0.35f) }, { "portcullis", new Color(0.95f, 0.65f, 0.25f) }, { "secret", new Color(0.80f, 0.50f, 0.90f) },
			{ "doorman", new Color(0.40f, 0.80f, 0.95f) }, { "drop", new Color(0.95f, 0.40f, 0.40f) }, { "climb", new Color(0.45f, 0.90f, 0.45f) },
			{ "teleport", new Color(0.60f, 0.60f, 1.00f) }, { "lever", new Color(0.95f, 0.95f, 0.55f) }, { "other", new Color(0.7f, 0.7f, 0.7f) },
		};

		public static void Show()
		{
			if (ZoneTools.Zones == null) ZoneTools.LoadOrNew();
			if (open != null) open.Close();
			var w = UiKit.Window("Zone graph", 900, 620);
			open = w;
			w.Closed += () => { if (open == w) open = null; };
			Build(w);
		}

		static void Build(Window w)
		{
			var zs = ZoneTools.Zones;
			UiKit.Clear(w.Toolbar);
			UiKit.Clear(w.Content);
			var legend = UiKit.Row(w.Toolbar, 18, 8);
			foreach (var kv in KindColors)
			{
				UiKit.Swatch(legend, kv.Value, 10);
				UiKit.Label(legend, kv.Key, 10, FontStyle.Normal, UiKit.DimTextColor, 52);
			}
			var legend2 = UiKit.Row(w.Toolbar, 18, 8);
			UiKit.Label(legend2, "A dot marks the end a one-way link arrives at. Dashed-looking thin lines are 'todo'. Click a zone to select it. Drag the window; scroll with the wheel.", 10, FontStyle.Normal, UiKit.DimTextColor, 700);
			UiKit.Button(legend2, "Redraw", () => Build(w), 60);

			// layout: columns by first level, rows in id order
			var byLevel = new Dictionary<int, List<Zone>>();
			var levelOf = new Dictionary<int, int>();
			foreach (var z in zs.Zones)
			{
				if (z.Id <= ZoneSet.Unassigned) continue;
				var levels = zs.LevelsOf(z.Id);
				int first = levels.Count > 0 ? levels[0] : 10;   // unpainted zones in a last column
				levelOf[z.Id] = first;
				List<Zone> list;
				if (!byLevel.TryGetValue(first, out list)) byLevel[first] = list = new List<Zone>();
				list.Add(z);
			}
			int maxRows = 1;
			foreach (var l in byLevel.Values) maxRows = Math.Max(maxRows, l.Count);
			float width = Margin * 2 + 10 * ColumnWidth, height = Margin * 2 + 30 + maxRows * RowHeight;

			// the scrolled content gets a fixed-size canvas; make the scroll rect go both ways
			var scroll = w.Content.parent.parent.GetComponent<ScrollRect>();
			if (scroll) scroll.horizontal = true;
			var canvas = UiKit.Rect("GraphCanvas", w.Content);
			UiKit.Size(canvas.gameObject, width, height);
			canvas.pivot = new Vector2(0, 1);
			var layout = w.Content.GetComponent<VerticalLayoutGroup>();
			if (layout) { layout.childForceExpandWidth = false; layout.childControlWidth = false; }
			// the body normally stretches to the window's width; give it the canvas's width so it scrolls sideways
			w.Content.anchorMin = new Vector2(0, 1);
			w.Content.anchorMax = new Vector2(0, 1);
			w.Content.pivot = new Vector2(0, 1);
			w.Content.sizeDelta = new Vector2(width + 16, w.Content.sizeDelta.y);

			var pos = new Dictionary<int, Vector2>();
			for (int col = 1; col <= 10; col++)
			{
				float x = Margin + (col - 1) * ColumnWidth;
				var title = UiKit.Label(canvas, col <= 9 ? "Level " + col : "Not painted", 12, FontStyle.Bold, UiKit.DimTextColor, NodeWidth);
				Place(title.rectTransform, x, -Margin, NodeWidth, 18);
				List<Zone> list;
				if (!byLevel.TryGetValue(col, out list)) continue;
				list.Sort((a, b) => a.Id.CompareTo(b.Id));
				for (int i = 0; i < list.Count; i++)
				{
					float y = -(Margin + 30 + i * RowHeight);
					pos[list[i].Id] = new Vector2(x + NodeWidth / 2, y - NodeHeight / 2);
				}
			}
			// edges first (under the nodes)
			foreach (var k in zs.Links)
			{
				Vector2 a, b;
				if (!pos.TryGetValue(k.From, out a) || !pos.TryGetValue(k.To, out b)) continue;
				Color c;
				if (!KindColors.TryGetValue(k.Kind, out c)) c = KindColors["other"];
				if (k.Confidence == "todo") c.a = 0.45f;
				Line(canvas, a, b, k.Confidence == "todo" ? 1f : 2f, c);
				if (!k.TwoWay) Dot(canvas, Vector2.Lerp(a, b, 0.82f), 7, c);
			}
			// nodes
			foreach (var z in zs.Zones)
			{
				Vector2 p;
				if (!pos.TryGetValue(z.Id, out p)) continue;
				var zone = z;
				var levels = zs.LevelsOf(z.Id);
				string label = z.Id + " " + Trim(z.Name, 16) + (levels.Count > 1 ? "  [L" + levels[0] + "-" + levels[levels.Count - 1] + "]" : "");
				var col = ZoneTools.ColorOf(z.Id);
				var bg = Color.Lerp(col, Color.black, 0.45f);
				var b = UiKit.Button(canvas, label, () => { ZoneWindow.SelectZone(zone.Id); if (open != null) open.Status.text = "Selected zone " + zone.Id + " " + zone.Name; }, NodeWidth, NodeHeight, bg);
				var t = b.GetComponentInChildren<Text>();
				if (t) { t.fontSize = 10; t.alignment = TextAnchor.MiddleLeft; }
				Place(b.GetComponent<RectTransform>(), p.x - NodeWidth / 2, p.y + NodeHeight / 2, NodeWidth, NodeHeight);
				var sw = UiKit.Swatch(canvas, col, 6);
				Place(sw.rectTransform, p.x - NodeWidth / 2 - 8, p.y + 3, 6, 6);
				if (z.Status == "unfinished" || z.Status == "unpainted")
				{
					var mark = UiKit.Label(canvas, "?", 11, FontStyle.Bold, UiKit.WarnColor, 10);
					Place(mark.rectTransform, p.x + NodeWidth / 2 + 2, p.y + NodeHeight / 2, 10, NodeHeight);
				}
				UiKit.Tooltip(b.gameObject, z.Name + (string.IsNullOrEmpty(z.Notes) ? "" : "\n" + z.Notes) + "\nLinks: " + CountLinks(zs, z.Id));
			}
			w.Status.text = zs.Zones.Count + " zones, " + zs.Links.Count + " links.";
		}

		static int CountLinks(ZoneSet zs, int id)
		{
			int n = 0;
			foreach (var k in zs.Links) if (k.From == id || k.To == id) n++;
			return n;
		}

		static string Trim(string s, int n) { return string.IsNullOrEmpty(s) ? "" : s.Length <= n ? s : s.Substring(0, n - 1) + "…"; }

		static void Place(RectTransform rt, float x, float y, float w, float h)
		{
			rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
			rt.pivot = new Vector2(0, 1);
			rt.anchoredPosition = new Vector2(x, y);
			rt.sizeDelta = new Vector2(w, h);
			var le = rt.GetComponent<LayoutElement>();
			if (le) le.ignoreLayout = true;
		}

		static void Line(Transform parent, Vector2 a, Vector2 b, float thickness, Color c)
		{
			var rt = UiKit.Rect("Edge", parent);
			var img = rt.gameObject.AddComponent<Image>();
			img.color = c;
			img.raycastTarget = false;
			var d = b - a;
			float len = d.magnitude;
			rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
			rt.pivot = new Vector2(0, 0.5f);
			rt.anchoredPosition = a;
			rt.sizeDelta = new Vector2(len, thickness);
			rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
		}

		static void Dot(Transform parent, Vector2 at, float size, Color c)
		{
			var img = UiKit.Swatch(parent, c, size);
			img.raycastTarget = false;
			Place(img.rectTransform, at.x - size / 2, at.y + size / 2, size, size);
		}
	}
}
