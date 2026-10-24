using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Edits UW2's DATA\SCD.ARK, the 16 schedules (block 0 by the time of day, block n by
	/// X clock n). Each row: time, level (0xFF any, 0xF6 + n world n), once, event (negative:
	/// disabled, run only by a test-variables row above it) and 11 parameter bytes, described
	/// as uw2 event/SCDEVENT.C reads them. A new game copies DATA's SCD.ARK into SAVE0; a game
	/// in progress uses its own copy.
	/// </summary>
	public static class ScheduleWindow
	{
		static int shownBlock;

		public static void Show()
		{
			if (LevArk.Game.Kind != GameKind.UW2 && GameProfile.Detect(ExtensionsMenu.DataFolder) != GameKind.UW2) throw new InvalidOperationException("SCD.ARK belongs to Ultima Underworld II; the loaded game is " + LevArk.Game.Name);
			string path = ExtensionsMenu.FindDataFile("SCD.ARK");
			if (path == null) throw new FileNotFoundException("SCD.ARK is not in the DATA folder");
			var arc = ScdArk.Open(path);
			var blocks = ScdArk.Read(arc);
			var w = UiKit.Window("Schedules (SCD.ARK)", 1040, 600);

			var tools = UiKit.Row(w.Toolbar, 22);
			var names = new List<string>();
			for (int b = 0; b < blocks.Count; b++)
				names.Add(string.Format("{0} {1} ({2} rows)", b, b == 0 ? "time of day" : "X clock " + b, blocks[b].Rows.Count));
			UiKit.Dropdown(tools, names, Mathf.Clamp(shownBlock, 0, blocks.Count - 1), i => { shownBlock = i; Fill(w, blocks); }, 220);
			UiKit.Button(tools, "Add row", () =>
			{
				var blk = blocks[shownBlock];
				var r = new ScdRow { Time = blk.Rows.Count > 0 ? blk.Rows[blk.Rows.Count - 1].Time : 0, Level = 0xFF, Event = 0 };
				blk.Rows.Add(r);
				Fill(w, blocks);
				w.Status.text = "Row added at the end. Rows run in order of time; keep them sorted.";
			}, 80, 22);
			UiKit.Button(tools, "Sort by time", () =>
			{
				var blk = blocks[shownBlock];
				// Stable, so a test row keeps the disabled rows that follow it when they share its time
				var sorted = new List<ScdRow>(blk.Rows);
				var keyed = new List<KeyValuePair<int, ScdRow>>();
				for (int i = 0; i < sorted.Count; i++) keyed.Add(new KeyValuePair<int, ScdRow>(i, sorted[i]));
				keyed.Sort((a, b) => a.Value.Time != b.Value.Time ? a.Value.Time.CompareTo(b.Value.Time) : a.Key.CompareTo(b.Key));
				blk.Rows.Clear();
				foreach (var k in keyed) blk.Rows.Add(k.Value);
				Fill(w, blocks);
			}, 90, 22);
			UiKit.Button(tools, "Save SCD.ARK", () =>
			{
				var problems = Check(blocks);
				var written = ScdArk.Write(arc, blocks);
				if (written.Count == 0) { w.Status.text = "Nothing changed." + problems; return; }
				ExtensionsMenu.WriteDataFile(Path.GetFileName(path), arc.ToArray());
				w.Status.text = "Saved blocks " + string.Join(", ", written.ConvertAll(x => x.ToString()).ToArray()) + "." + problems;
			}, 110, 22, UiKit.AccentColor);
			UiKit.Button(tools, "Reload", () => { w.Close(); Show(); }, 70, 22);
			UiKit.Paragraph(w.Toolbar,
				"Time is in the block's clock (block 0: 72 steps a day). Level: a level number, 255 any level, 246 + n world n. " +
				"Event: 1 goal, 2 teleport, 3 kill, 4 quest bit, 5 scheduled triggers, 7 special case, 8 attitude, 9 set variable, 10 test variables, 11 remove; " +
				"negative is disabled (a test row runs the disabled rows after it when it passes). Parameters are row bytes 5-15 in hex. " +
				"New games copy this file into SAVE0; a saved game keeps its own.", 10);
			Fill(w, blocks);
		}

		static string Check(List<ScdBlock> blocks)
		{
			var sb = new StringBuilder();
			for (int b = 0; b < blocks.Count; b++)
			{
				var rows = blocks[b].Rows;
				for (int i = 1; i < rows.Count; i++)
					if (rows[i].Time < rows[i - 1].Time) { sb.AppendFormat(" Block {0} row {1} is earlier than the row before it.", b, i); break; }
			}
			return sb.ToString();
		}

		static void Fill(Window w, List<ScdBlock> blocks)
		{
			UiKit.Clear(w.Content);
			var blk = blocks[shownBlock];
			var head = UiKit.Row(w.Content, 18);
			UiKit.Label(head, "#", 10, FontStyle.Bold, UiKit.DimTextColor, 28);
			UiKit.Label(head, "time", 10, FontStyle.Bold, UiKit.DimTextColor, 50);
			UiKit.Label(head, "level", 10, FontStyle.Bold, UiKit.DimTextColor, 44);
			UiKit.Label(head, "", 10, FontStyle.Bold, UiKit.DimTextColor, 64);
			UiKit.Label(head, "event", 10, FontStyle.Bold, UiKit.DimTextColor, 44);
			UiKit.Label(head, "parameters (bytes 5-15)", 10, FontStyle.Bold, UiKit.DimTextColor, 230);
			UiKit.Label(head, "what it does", 10, FontStyle.Bold, UiKit.DimTextColor, -1);
			if (blk.Rows.Count == 0) UiKit.Label(w.Content, "This schedule has no rows.", 11, FontStyle.Italic, UiKit.DimTextColor);
			for (int i = 0; i < blk.Rows.Count; i++) BuildRow(w, blocks, blk, i);
		}

		static void BuildRow(Window w, List<ScdBlock> blocks, ScdBlock blk, int i)
		{
			var r = blk.Rows[i];
			var row = UiKit.Row(w.Content, 20, 4);
			Text desc = null;
			Action refresh = () => { if (desc) desc.text = ScdRow.LevelText(r.Level) + ": " + r.Describe(NpcName); };
			UiKit.Label(row, i.ToString(), 10, FontStyle.Normal, UiKit.DimTextColor, 28);
			UiKit.Input(row, r.Time.ToString(), s => { int v; if (int.TryParse(s, out v) && v >= 0 && v <= 0xFFFF) r.Time = v; refresh(); }, 50);
			UiKit.Input(row, r.Level.ToString(), s => { int v; if (int.TryParse(s, out v) && v >= 0 && v <= 255) r.Level = v; refresh(); }, 44);
			UiKit.Check(row, "once", r.Once, b => { r.Once = b; refresh(); }, 64);
			UiKit.Input(row, r.Event.ToString(), s => { int v; if (int.TryParse(s, out v) && v >= -128 && v <= 127) r.Event = v; refresh(); }, 44);
			UiKit.Input(row, Hex(r.Bytes, 5, 11), s =>
			{
				var b = ParseHex(s);
				if (b != null && b.Length == 11) Array.Copy(b, 0, r.Bytes, 5, 11);
				else w.Status.text = "Parameters are 11 hex bytes, e.g. 00 01 02 ...";
				refresh();
			}, 230, false);
			desc = UiKit.Label(row, "", 10, FontStyle.Normal, null, -1);
			refresh();
			UiKit.Button(row, "^", () => { if (i > 0) { blk.Rows.RemoveAt(i); blk.Rows.Insert(i - 1, r); Fill(w, blocks); } }, 20);
			UiKit.Button(row, "v", () => { if (i < blk.Rows.Count - 1) { blk.Rows.RemoveAt(i); blk.Rows.Insert(i + 1, r); Fill(w, blocks); } }, 20);
			UiKit.Button(row, "+", () => { blk.Rows.Insert(i + 1, r.Clone()); Fill(w, blocks); }, 20);
			UiKit.Button(row, "x", () => { blk.Rows.RemoveAt(i); Fill(w, blocks); }, 20, 20, UiKit.WarnColor);
		}

		static string NpcName(int whoami)
		{
			var sd = MapCreator.StringData;
			if (sd == null || sd.Blocks == null) return null;
			foreach (var b in sd.Blocks)
				if (b.BlockNumber == 7) return whoami >= 0 && whoami < b.Strings.Count ? b.Strings[whoami] : null;
			return null;
		}

		public static string Hex(byte[] b, int from, int count)
		{
			var sb = new StringBuilder();
			for (int i = 0; i < count; i++) { if (i > 0) sb.Append(' '); sb.Append(b[from + i].ToString("X2")); }
			return sb.ToString();
		}

		public static byte[] ParseHex(string s)
		{
			var parts = (s ?? "").Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			var r = new byte[parts.Length];
			for (int i = 0; i < parts.Length; i++)
			{
				int v;
				if (!int.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, null, out v) || v < 0 || v > 255) return null;
				r[i] = (byte)v;
			}
			return r;
		}
	}
}
