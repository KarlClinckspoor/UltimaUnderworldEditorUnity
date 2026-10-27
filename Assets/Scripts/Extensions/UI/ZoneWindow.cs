using System;
using System.Collections.Generic;
using System.Globalization;
using UltimateEditor.Uw1.Zoning;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Tools > Zones: the zoning editor. Pages: Zones (list, paint selection, names, colours,
	/// unassigned tiles), Links (connections of the selected zone), Items (placements),
	/// Audit (the zoning against LEV.ARK), Solver (start to goal). Painting itself happens on
	/// the map in Sector mode: left button paints the selected zone, right button unassigns,
	/// Fill mode floods the connected area.
	/// </summary>
	public static class ZoneWindow
	{
		static Window open;
		static string page = "Zones";
		static int selectedZone = 2;
		static string lastReport = "";
		static readonly SolverOptions solverOptions = new SolverOptions { AllowBash = true, AllowSpells = true, LockpickSkill = -1 };
		static int lockpickSkill = 0;
		static AuditResult lastAudit;
		static Text unassignedLabel, paintLabel;
		static bool rebuildQueued;

		public static int SelectedZone { get { return selectedZone; } }

		/// <summary>Forgets the last results (opening another game without restarting).</summary>
		public static void ResetState()
		{
			open = null;
			lastReport = "";
			lastAudit = null;
			unassignedLabel = null;
			paintLabel = null;
			rebuildQueued = false;
		}

		public static void Show()
		{
			if (open != null) open.Close();
			var w = UiKit.Window("Zones (randomizer logic)", 760, 600);
			open = w;
			w.Closed += () => { if (open == w) { open = null; ZoneTools.Changed -= OnChanged; ZoneTools.PaintEnabled = false; } };
			ZoneTools.Changed += OnChanged;
			ZoneTools.PaintEnabled = true;
			if (ZoneTools.Zones == null) w.Status.text = ZoneTools.LoadOrNew();
			Build(w);
		}

		/// <summary>A paint stroke only refreshes the two counters; structural changes rebuild (deferred, never inside an input's deselect).</summary>
		static void OnChanged()
		{
			if (open == null) return;
			if (unassignedLabel) unassignedLabel.text = UnassignedText(ZoneTools.Zones);
			if (paintLabel) paintLabel.text = PaintText();
		}

		public static void SelectZone(int id)
		{
			selectedZone = id;
			ZoneTools.PaintZone = id;
			if (open != null) QueueBuild(open, false);
		}

		/// <summary>Rebuilds on the next frame, so a click that triggered it is not lost.</summary>
		static void QueueBuild(Window w, bool toolbar)
		{
			if (rebuildQueued) return;
			rebuildQueued = true;
			var runner = w.Root.GetComponent<DeferredCall>() ?? w.Root.AddComponent<DeferredCall>();
			runner.Run(() => { rebuildQueued = false; if (open == w) Build(w, toolbar); });
		}

		static string PaintText() { return "Painting zone " + ZoneTools.PaintZone + " (Sector mode: left button paints, right unassigns)"; }

		static void Build(Window w, bool rebuildToolbar = true)
		{
			var zs = ZoneTools.Zones;
			if (rebuildToolbar)
			{
				UiKit.Clear(w.Toolbar);
				var file = UiKit.Row(w.Toolbar, 20);
				UiKit.Button(file, "Load", () => { w.Status.text = ZoneTools.LoadOrNew(); Build(w); }, 50);
				UiKit.Button(file, "Save", () => { w.Status.text = ZoneTools.Save(); }, 50, 20, UiKit.AccentColor);
				UiKit.Button(file, "New from tiles", () => { w.Status.text = ZoneTools.New(); Build(w); }, 100);
				var folder = UiKit.Input(file, ExtensionsMenu.DataFolder ?? "", null, 240, false);
				UiKit.Tooltip(folder.gameObject, "Folder holding Gimp's indexed map0.bmp .. map8.bmp (pixel index = zone id).");
				UiKit.Button(file, "Import BMPs", () => { w.Status.text = ZoneTools.ImportBmpFolder(folder.text); Build(w); }, 90);
				UiKit.Button(file, "Graph", () => ZoneGraphWindow.Show(), 60);

				var pages = UiKit.Row(w.Toolbar, 20);
				foreach (var name in new[] { "Zones", "Links", "Items", "Audit", "Solver", "Help" })
				{
					string n = name;
					UiKit.Button(pages, n, () => { page = n; Build(w); }, 62, 20, page == n ? UiKit.AccentColor : (Color?)null);
				}
				UiKit.Label(pages, "", 11, FontStyle.Normal, null, 6);
				UiKit.Check(pages, "Overlay", ZoneTools.OverlayOn, v => { ZoneTools.OverlayOn = v; ZoneTools.ApplyOverlay(); }, 78);
				UiKit.Check(pages, "Fill mode", ZoneTools.FillMode, v => ZoneTools.FillMode = v, 84);
				var paintRow = UiKit.Row(w.Toolbar, 18);
				paintLabel = UiKit.Label(paintRow, PaintText(), 11, FontStyle.Bold, UiKit.WarnColor, -1);
			}
			UiKit.Clear(w.Content);
			if (zs == null) { UiKit.Paragraph(w.Content, "Open a level file first.", 12, UiKit.TextColor); return; }
			switch (page)
			{
				case "Zones": BuildZones(w, zs); break;
				case "Links": BuildLinks(w, zs); break;
				case "Items": BuildItems(w, zs); break;
				case "Audit": BuildAudit(w, zs); break;
				case "Solver": BuildSolver(w, zs); break;
				default: BuildHelp(w); break;
			}
		}

		// ---------------------------------------------------------------- Zones

		static void BuildZones(Window w, ZoneSet zs)
		{
			// unassigned reminder
			int totalUnassigned = 0;
			var parts = new List<string>();
			for (int l = 1; l <= ZoneSet.Levels; l++)
			{
				var counts = zs.CountsOnLevel(l);
				int n;
				if (zs.Tiles[l - 1] == null) { parts.Add("L" + l + ": not painted"); continue; }
				if (counts.TryGetValue(ZoneSet.Unassigned, out n) && n > 0) { parts.Add("L" + l + ": " + n); totalUnassigned += n; }
			}
			var remind = UiKit.Row(w.Content, 20);
			unassignedLabel = UiKit.Label(remind, UnassignedText(zs), 11, FontStyle.Bold, totalUnassigned > 0 ? UiKit.WarnColor : UiKit.TextColor, 560);
			UiKit.Button(remind, "Paint 'unassigned'", () => SelectZone(ZoneSet.Unassigned), 120);
			UiKit.Paragraph(w.Content, "Unassigned tiles show magenta on the map. Select a zone below (or add one), switch the editor to Sector mode and paint; Fill mode floods the connected area of whatever is under the cursor.", 10);

			var head = UiKit.Row(w.Content, 18);
			UiKit.Label(head, "", 10, FontStyle.Normal, null, 16);
			UiKit.Label(head, "id", 10, FontStyle.Bold, UiKit.DimTextColor, 28);
			UiKit.Label(head, "name", 10, FontStyle.Bold, UiKit.DimTextColor, 220);
			UiKit.Label(head, "tiles / levels", 10, FontStyle.Bold, UiKit.DimTextColor, 110);
			UiKit.Label(head, "status", 10, FontStyle.Bold, UiKit.DimTextColor, 80);

			var tilesOf = new Dictionary<int, int>();
			var levelsOf = new Dictionary<int, List<int>>();
			for (int l = 1; l <= ZoneSet.Levels; l++)
				foreach (var kv in zs.CountsOnLevel(l))
				{
					int c; tilesOf.TryGetValue(kv.Key, out c); tilesOf[kv.Key] = c + kv.Value;
					List<int> ls; if (!levelsOf.TryGetValue(kv.Key, out ls)) levelsOf[kv.Key] = ls = new List<int>();
					ls.Add(l);
				}
			var statuses = new[] { "done", "unfinished", "unpainted", "unassigned" };
			foreach (var zone in zs.Zones)
			{
				if (zone.Id == ZoneSet.Solid) continue;
				var z = zone;
				var row = UiKit.Row(w.Content, 20);
				UiKit.Swatch(row, ZoneTools.ColorOf(z.Id));
				var idb = UiKit.Button(row, z.Id.ToString(), () => SelectZone(z.Id), 28, 20, z.Id == selectedZone ? UiKit.AccentColor : (Color?)null);
				UiKit.Tooltip(idb.gameObject, "Select for painting and for the Links page.");
				UiKit.Input(row, z.Name, s => { if (s != z.Name) { z.Name = s; ZoneTools.MarkDirty(); } }, 220, false);
				int tiles; tilesOf.TryGetValue(z.Id, out tiles);
				List<int> lv; levelsOf.TryGetValue(z.Id, out lv);
				UiKit.Label(row, tiles + (lv == null ? "" : "  L" + string.Join(",", lv.ConvertAll(x => x.ToString()).ToArray())), 10, FontStyle.Normal, UiKit.DimTextColor, 110);
				if (z.Id != ZoneSet.Unassigned)
				{
					int si = Array.IndexOf(statuses, z.Status);
					if (si < 0) { si = 1; z.Status = statuses[1]; }
					UiKit.Cycle(row, statuses, si, i => { z.Status = statuses[i]; ZoneTools.MarkDirty(); }, 80);
					UiKit.Button(row, "Colour", () => { CycleColor(z); QueueBuild(w, false); }, 50);
					UiKit.Button(row, "Notes", () => NotesPopup(z), 46);
					UiKit.Button(row, "Links", () => { selectedZone = z.Id; page = "Links"; Build(w); }, 44);
					UiKit.Button(row, "Merge into...", () => MergePopup(w, z), 86);
				}
			}
			var add = UiKit.Row(w.Content, 22);
			var newName = UiKit.Input(add, "", null, 220, false);
			UiKit.Button(add, "Add zone", () =>
			{
				int id = zs.NextZoneId();
				if (id < 0) { w.Status.text = "No free zone id (2-255)."; return; }
				var z = zs.EnsureZone(id);
				z.Name = string.IsNullOrEmpty(newName.text) ? "Zone " + id : newName.text;
				z.Status = "unfinished";
				SelectZone(id);
				ZoneTools.MarkDirty();
				w.Status.text = "Zone " + id + " added and selected for painting.";
				QueueBuild(w, false);
			}, 80, 20, UiKit.AccentColor);
			UiKit.Label(add, "(the next free id is " + zs.NextZoneId() + ")", 10, FontStyle.Normal, UiKit.DimTextColor, 200);
		}

		static string UnassignedText(ZoneSet zs)
		{
			if (zs == null) return "";
			int total = 0;
			var parts = new List<string>();
			for (int l = 1; l <= ZoneSet.Levels; l++)
			{
				if (zs.Tiles[l - 1] == null) { parts.Add("L" + l + ": not painted"); continue; }
				int n;
				if (zs.CountsOnLevel(l).TryGetValue(ZoneSet.Unassigned, out n) && n > 0) { parts.Add("L" + l + ": " + n); total += n; }
			}
			return total == 0 && parts.Count == 0 ? "Every open tile has a zone." : "Open tiles without a zone: " + total + "  (" + string.Join(", ", parts.ToArray()) + ")";
		}

		static void CycleColor(Zone z)
		{
			// step through a few hues around the default; '' returns to the default
			byte r, g, b;
			ZoneSet.DefaultColor(z.Id, out r, out g, out b);
			var options = new List<string> { "", "#E6194B", "#3CB44B", "#FFE119", "#4363D8", "#F58231", "#911EB4", "#46F0F0", "#F032E6", "#BCF60C", "#FABEBE", "#008080", "#E6BEFF", "#9A6324", "#FFFAC8", "#800000", "#AAFFC3", "#808000", "#FFD8B1", "#000075", "#808080" };
			int i = options.IndexOf(z.Color ?? "");
			z.Color = options[(i + 1) % options.Count];
			ZoneTools.MarkDirty();
			ZoneTools.ApplyOverlay();
		}

		static void MultiLine(InputField input)
		{
			input.lineType = InputField.LineType.MultiLineNewline;
			input.textComponent.alignment = TextAnchor.UpperLeft;
			input.textComponent.verticalOverflow = VerticalWrapMode.Truncate;   // so long notes scroll instead of spilling
			var ph = input.placeholder as Text;
			if (ph) ph.verticalOverflow = VerticalWrapMode.Truncate;
		}

		static void NotesPopup(Zone z)
		{
			var p = UiKit.Window("Zone " + z.Id + ": " + z.Name, 480, 260);
			UiKit.Label(p.Content, "Notes (what is here, open questions):", 11, FontStyle.Bold);
			var input = UiKit.Input(p.Content, z.Notes, s => { z.Notes = s; ZoneTools.MarkDirty(); }, -1, false, 150);
			MultiLine(input);
			var row = UiKit.Row(p.Content, 20);
			UiKit.Label(row, "Levels (text):", 11, FontStyle.Normal, null, 90);
			UiKit.Input(row, z.Levels, s => { z.Levels = s; ZoneTools.MarkDirty(); }, 120, false);
			UiKit.Label(row, "Colour #RRGGBB:", 11, FontStyle.Normal, null, 100);
			UiKit.Input(row, z.Color, s => { z.Color = s.Trim(); ZoneTools.MarkDirty(); ZoneTools.ApplyOverlay(); }, 80, false);
		}

		static void MergePopup(Window w, Zone z)
		{
			var p = UiKit.Window("Merge zone " + z.Id + " into another", 420, 120);
			var row = UiKit.Row(p.Content, 20);
			UiKit.Label(row, "Target zone id:", 11, FontStyle.Normal, null, 100);
			var target = UiKit.Input(row, "", null, 60);
			UiKit.Button(row, "Merge", () =>
			{
				int t;
				if (!int.TryParse(target.text, out t) || t == z.Id || t < ZoneSet.Unassigned || t > 255) { p.Status.text = "Enter another zone id."; return; }
				ZoneTools.Zones.Merge(z.Id, t);
				ZoneTools.MarkDirty();
				ZoneTools.ApplyOverlay();
				p.Close();
				Build(w);
				w.Status.text = "Zone " + z.Id + " merged into " + t + " (tiles, links and items moved).";
			}, 70, 20, UiKit.WarnColor);
			UiKit.Paragraph(p.Content, "Every tile of zone " + z.Id + " becomes the target; links and items are redirected; the zone's notes are appended to the target's.", 10);
		}

		// ---------------------------------------------------------------- Links

		static void BuildLinks(Window w, ZoneSet zs)
		{
			var zone = zs.GetZone(selectedZone);
			var head = UiKit.Row(w.Content, 20);
			UiKit.Label(head, "Links of zone", 11, FontStyle.Bold, null, 80);
			var pick = UiKit.Input(head, selectedZone.ToString(), s => { int v; if (int.TryParse(s, out v) && v != selectedZone) { selectedZone = v; QueueBuild(w, false); } }, 50);
			UiKit.Label(head, zone == null ? "(no such zone)" : zone.Name, 11, FontStyle.Normal, UiKit.DimTextColor, 300);
			UiKit.Check(head, "all zones", selectedZone == 0, v => { selectedZone = v ? 0 : ZoneTools.PaintZone; Build(w, false); }, 90);

			var cols = UiKit.Row(w.Content, 18);
			UiKit.Label(cols, "id", 10, FontStyle.Bold, UiKit.DimTextColor, 26);
			UiKit.Label(cols, "from", 10, FontStyle.Bold, UiKit.DimTextColor, 40);
			UiKit.Label(cols, "dir", 10, FontStyle.Bold, UiKit.DimTextColor, 36);
			UiKit.Label(cols, "to", 10, FontStyle.Bold, UiKit.DimTextColor, 40);
			UiKit.Label(cols, "kind", 10, FontStyle.Bold, UiKit.DimTextColor, 74);
			UiKit.Label(cols, "requires", 10, FontStyle.Bold, UiKit.DimTextColor, 230);
			UiKit.Label(cols, "sure?", 10, FontStyle.Bold, UiKit.DimTextColor, 56);

			foreach (var link in zs.Links)
			{
				if (selectedZone != 0 && link.From != selectedZone && link.To != selectedZone) continue;
				var k = link;
				var row = UiKit.Row(w.Content, 20, 4);
				UiKit.Label(row, k.Id.ToString(), 10, FontStyle.Normal, UiKit.DimTextColor, 26);
				UiKit.Input(row, k.From.ToString(), s => { int v; if (int.TryParse(s, out v)) { k.From = v; zs.EnsureZone(v); ZoneTools.MarkDirty(); } }, 40);
				UiKit.Cycle(row, new[] { "->", "<->" }, k.TwoWay ? 1 : 0, i => { k.TwoWay = i == 1; ZoneTools.MarkDirty(); }, 36);
				UiKit.Input(row, k.To.ToString(), s => { int v; if (int.TryParse(s, out v)) { k.To = v; zs.EnsureZone(v); ZoneTools.MarkDirty(); } }, 40);
				int ki = Math.Max(0, Array.IndexOf(ZoneLink.Kinds, k.Kind));
				UiKit.Cycle(row, ZoneLink.Kinds, ki, i => { k.Kind = ZoneLink.Kinds[i]; ZoneTools.MarkDirty(); }, 74);
				var req = UiKit.Input(row, k.Requires, s =>
				{
					string err;
					if (Requirement.TryParse(s, out err) == null) { w.Status.text = "Requirement '" + s + "': " + err; return; }
					k.Requires = s.Trim(); ZoneTools.MarkDirty(); w.Status.text = "";
				}, 230, false);
				UiKit.Tooltip(req.gameObject, string.Join("\n", Requirement.LanguageHelp));
				UiKit.Cycle(row, new[] { "certain", "todo" }, k.Confidence == "todo" ? 1 : 0, i => { k.Confidence = i == 1 ? "todo" : "certain"; ZoneTools.MarkDirty(); }, 56);
				var notes = UiKit.Button(row, "?", () => LinkNotesPopup(k), 20, 20, string.IsNullOrEmpty(k.Notes) ? (Color?)null : UiKit.AccentColor);
				UiKit.Tooltip(notes.gameObject, string.IsNullOrEmpty(k.Notes) ? "Notes" : k.Notes);
				UiKit.Button(row, "X", () => { zs.Links.Remove(k); ZoneTools.MarkDirty(); QueueBuild(w, false); }, 20, 20, new Color(0.55f, 0.2f, 0.2f));
			}

			var add = UiKit.Row(w.Content, 22);
			UiKit.Label(add, "New link from", 11, FontStyle.Normal, null, 80);
			var from = UiKit.Input(add, selectedZone > 0 ? selectedZone.ToString() : "", null, 40);
			UiKit.Label(add, "to", 11, FontStyle.Normal, null, 16);
			var to = UiKit.Input(add, "", null, 40);
			UiKit.Button(add, "Add", () =>
			{
				int a, b;
				if (!int.TryParse(from.text, out a) || !int.TryParse(to.text, out b) || a == b) { w.Status.text = "Enter two different zone ids."; return; }
				zs.Links.Add(new ZoneLink { Id = zs.NextLinkId(), From = a, To = b, Kind = "walk", Requires = "free", TwoWay = true, Confidence = "todo" });
				zs.EnsureZone(a); zs.EnsureZone(b);
				ZoneTools.MarkDirty();
				Build(w, false);
			}, 50, 20, UiKit.AccentColor);
			UiKit.Paragraph(w.Content, "Hover a 'requires' field for the language. Confidence 'todo' marks an open question; the solver can be told to ignore those links.", 10);
		}

		static void LinkNotesPopup(ZoneLink k)
		{
			var p = UiKit.Window("Link " + k.Id + ": " + k.From + (k.TwoWay ? " <-> " : " -> ") + k.To, 480, 220);
			var input = UiKit.Input(p.Content, k.Notes, s => { k.Notes = s; ZoneTools.MarkDirty(); }, -1, false, 120);
			MultiLine(input);
			var row = UiKit.Row(p.Content, 20);
			UiKit.Label(row, k.Level > 0 ? string.Format("Anchor: L{0} ({1},{2})", k.Level, k.X, k.Y) : "No anchor tile.", 11, FontStyle.Normal, UiKit.DimTextColor, 200);
			UiKit.Button(row, "Set anchor = selected tile", () =>
			{
				var ui = UiKit.FindUIManager();
				var sel = ui && ui.InputManager ? ui.InputManager.SelectedObject : null;
				var mts = sel ? sel.GetComponent<MapTileScript>() : null;
				MapTile t = mts ? mts.MapTile : null;
				if (t == null) { p.Status.text = "Select a tile on the map first (Tile mode)."; return; }
				k.Level = t.Level; k.X = t.Position.x; k.Y = t.Position.y;
				ZoneTools.MarkDirty();
				p.Status.text = string.Format("Anchor set to L{0} ({1},{2}).", k.Level, k.X, k.Y);
			}, 170);
		}

		// ---------------------------------------------------------------- Items (placements)

		static void BuildItems(Window w, ZoneSet zs)
		{
			UiKit.Paragraph(w.Content, "What a zone gives the player once reached: key:N, item:name, talisman:name, flag:name, npc:name. 'Requires' makes it conditional (Judy's key of Love needs item:picture_tom). The Audit page can add the keys, talismans, quest items and NPCs it finds in LEV.ARK.", 10);
			var cols = UiKit.Row(w.Content, 18);
			UiKit.Label(cols, "zone", 10, FontStyle.Bold, UiKit.DimTextColor, 40);
			UiKit.Label(cols, "grants", 10, FontStyle.Bold, UiKit.DimTextColor, 170);
			UiKit.Label(cols, "requires", 10, FontStyle.Bold, UiKit.DimTextColor, 200);
			UiKit.Label(cols, "notes", 10, FontStyle.Bold, UiKit.DimTextColor, 200);
			foreach (var pl in zs.Placements)
			{
				if (selectedZone != 0 && pl.Zone != selectedZone && !ShowAllItems) continue;
				var p = pl;
				var row = UiKit.Row(w.Content, 20, 4);
				UiKit.Input(row, p.Zone.ToString(), s => { int v; if (int.TryParse(s, out v)) { p.Zone = v; ZoneTools.MarkDirty(); } }, 40);
				UiKit.Input(row, p.Grants, s => { p.Grants = Requirement.Normalize(s); ZoneTools.MarkDirty(); }, 170, false);
				UiKit.Input(row, p.Requires, s => { p.Requires = s.Trim(); ZoneTools.MarkDirty(); }, 200, false);
				var n = UiKit.Input(row, p.Notes, s => { p.Notes = s; ZoneTools.MarkDirty(); }, 200, false);
				UiKit.Tooltip(n.gameObject, p.Notes + (string.IsNullOrEmpty(p.Source) ? "" : "\n(source: " + p.Source + ")"));
				UiKit.Button(row, "X", () => { zs.Placements.Remove(p); ZoneTools.MarkDirty(); QueueBuild(w, false); }, 20, 20, new Color(0.55f, 0.2f, 0.2f));
			}
			var add = UiKit.Row(w.Content, 22);
			UiKit.Check(add, "show all zones", ShowAllItems, v => { ShowAllItems = v; Build(w, false); }, 120);
			UiKit.Label(add, "Add to zone", 11, FontStyle.Normal, null, 70);
			var zoneIn = UiKit.Input(add, selectedZone > 0 ? selectedZone.ToString() : "", null, 40);
			var tok = UiKit.Input(add, "", null, 170, false);
			UiKit.Button(add, "Add", () =>
			{
				int z;
				if (!int.TryParse(zoneIn.text, out z) || string.IsNullOrEmpty(tok.text)) { w.Status.text = "Enter a zone id and a token such as key:5."; return; }
				zs.Placements.Add(new Placement { Zone = z, Grants = Requirement.Normalize(tok.text) });
				ZoneTools.MarkDirty();
				Build(w, false);
			}, 50, 20, UiKit.AccentColor);
		}
		static bool ShowAllItems = true;

		// ---------------------------------------------------------------- Audit

		static void BuildAudit(Window w, ZoneSet zs)
		{
			var row = UiKit.Row(w.Content, 22);
			UiKit.Button(row, "Run audit against LEV.ARK", () =>
			{
				try
				{
					lastAudit = ZoneTools.Audit();
					lastReport = lastAudit.Report(zs);
				}
				catch (Exception e) { lastReport = "Audit failed: " + e; }
				Build(w, false);
			}, 170, 20, UiKit.AccentColor);
			UiKit.Button(row, "Add suggested items", () =>
			{
				if (lastAudit == null) { w.Status.text = "Run the audit first."; return; }
				var sugg = ZoneAudit.SuggestPlacements(zs, lastAudit);
				zs.Placements.AddRange(sugg);
				ZoneTools.MarkDirty();
				w.Status.text = sugg.Count + " placements added from the map (source 'levark').";
			}, 140);
			UiKit.Button(row, "Copy report to clipboard", () => { GUIUtility.systemCopyBuffer = lastReport ?? ""; w.Status.text = "Copied."; }, 150);
			UiKit.Paragraph(w.Content, "Checks the zoning against the loaded levels: open tiles without a zone, zones in several pieces, level changes (teleport traps) and the zones at both ends, every door between two zones with its lock as the game reads it (key number, pick difficulty, breakable), and where the keys, talismans, quest items and named creatures are.", 10);
			if (!string.IsNullOrEmpty(lastReport)) Report(w, lastReport);
		}

		// ---------------------------------------------------------------- Solver

		static void BuildSolver(Window w, ZoneSet zs)
		{
			var o = solverOptions;
			var opts = UiKit.Row(w.Content, 20);
			UiKit.Check(opts, "bashing doors", o.AllowBash, v => o.AllowBash = v, 110);
			UiKit.Check(opts, "spells (levitate, jump...)", o.AllowSpells, v => o.AllowSpells = v, 170);
			UiKit.Check(opts, "trust 'todo' links", o.TrustTodoLinks, v => o.TrustTodoLinks = v, 130);
			UiKit.Label(opts, "Pick Lock skill (0 = none):", 11, FontStyle.Normal, null, 150);
			UiKit.Input(opts, lockpickSkill.ToString(), s => { int v; if (int.TryParse(s, out v)) lockpickSkill = v; }, 40);
			var se = UiKit.Row(w.Content, 20);
			UiKit.Label(se, "Start zone", 11, FontStyle.Normal, null, 70);
			UiKit.Input(se, zs.StartZone.ToString(), s => { int v; if (int.TryParse(s, out v)) { zs.StartZone = v; ZoneTools.MarkDirty(); } }, 40);
			UiKit.Label(se, "Goal zone", 11, FontStyle.Normal, null, 70);
			UiKit.Input(se, zs.GoalZone.ToString(), s => { int v; if (int.TryParse(s, out v)) { zs.GoalZone = v; ZoneTools.MarkDirty(); } }, 40);
			UiKit.Label(se, "Start with (tokens, comma separated):", 11, FontStyle.Normal, null, 200);
			UiKit.Input(se, string.Join(", ", o.StartTokens.ToArray()), s =>
			{
				o.StartTokens.Clear();
				foreach (var t in s.Split(',')) if (t.Trim().Length > 0) o.StartTokens.Add(t.Trim());
			}, 200, false);
			var run = UiKit.Row(w.Content, 22);
			UiKit.Button(run, "Solve", () =>
			{
				o.LockpickSkill = lockpickSkill <= 0 ? -1 : lockpickSkill;
				try { lastReport = new ZoneSolver(zs, o).Solve(zs.StartZone, zs.GoalZone).Report(zs); }
				catch (Exception e) { lastReport = "Solver failed: " + e; }
				Build(w, false);
			}, 80, 20, UiKit.AccentColor);
			UiKit.Button(run, "Copy report to clipboard", () => { GUIUtility.systemCopyBuffer = lastReport ?? ""; w.Status.text = "Copied."; }, 150);
			UiKit.Paragraph(w.Content, "Walks the zone graph from the start with nothing in hand: each sphere is what becomes reachable next and what it grants. Then: the route to the goal, what blocks the rest, tokens that no zone grants, and zones joined by a free two-way link (candidates to merge).", 10);
			if (!string.IsNullOrEmpty(lastReport)) Report(w, lastReport);
		}

		static void Report(Window w, string text)
		{
			// long reports in chunks: a single Text has a vertex limit
			var lines = text.Split('\n');
			var sb = new System.Text.StringBuilder();
			int count = 0;
			foreach (var line in lines)
			{
				sb.Append(line).Append('\n');
				if (++count >= 40)
				{
					UiKit.Paragraph(w.Content, sb.ToString(), 10, UiKit.TextColor);
					sb.Length = 0; count = 0;
				}
			}
			if (sb.Length > 0) UiKit.Paragraph(w.Content, sb.ToString(), 10, UiKit.TextColor);
		}

		// ---------------------------------------------------------------- Help

		/// <summary>Runs an action on the next frame.</summary>
		public sealed class DeferredCall : MonoBehaviour
		{
			readonly List<Action> pending = new List<Action>();
			public void Run(Action a) { pending.Add(a); }
			void LateUpdate()
			{
				if (pending.Count == 0) return;
				var run = pending.ToArray();
				pending.Clear();
				foreach (var a in run) a();
			}
		}

		static void BuildHelp(Window w)
		{
			UiKit.Paragraph(w.Content,
				"ZONES are areas the player moves in freely. Every open tile of every level gets a zone id (1 = open but not assigned yet, shown magenta). " +
				"Paint in the editor's Sector mode: left button paints the selected zone, right button sets 'unassigned'; Fill mode floods a connected area. " +
				"The file is " + ZoneTools.FileName + " next to LEV.ARK (JSON: zones, links, items, and the tile ids as 64 rows of hex pairs per level). " +
				"'Import BMPs' reads Gimp's indexed map0.bmp..map8.bmp (pixel index = zone id).\n\n" +
				"LINKS say how to get from one zone to another: kind (door, drop, teleport...), one- or two-way, and a requirement in a small language:\n" +
				string.Join("\n", Requirement.LanguageHelp) + "\n\n" +
				"ITEMS (placements) say what a zone grants: keys, items, flags, NPCs to talk to; optionally only when a requirement holds.\n\n" +
				"AUDIT compares the zoning with LEV.ARK: unassigned tiles, zone pieces, level changes, doors between zones with their locks (key number, pick difficulty z: 3z skill needed, 14 needs 31, 15 never; breakable unless the item's COMOBJ quality class is 3 or bit 13 is set), and keys/talismans/NPCs by zone.\n\n" +
				"SOLVER walks the graph from the start zone: spheres, the route to the goal, what is blocked and why, tokens nobody grants, zones that could be merged.\n\n" +
				"GRAPH draws the zones by level with their links; click a zone to select it.", 11, UiKit.TextColor);
		}
	}
}
