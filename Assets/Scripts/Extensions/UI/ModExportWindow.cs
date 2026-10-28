using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UltimateEditor.Games;
using UltimateEditor.Modding;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Tools > Export as mod: packs the changes made to this game folder into a .uwmod package
	/// for the mod manager (KarlClinckspoor/UltimaUnderworldsModManager). The package holds the
	/// changes, not the files: the changed archive blocks, strings, pictures and byte ranges, the
	/// start square, byte patches of the executable, new files. The originals are the editor's
	/// .BKP backups (made before its first save of each file), or another folder with the
	/// unmodified game. A saved game can be packed on its own as a "mini mod".
	///
	/// The work is in the Underworld.Modding assembly (Assets/Scripts/Modding), which the mod
	/// manager shares; this window only gathers the details.
	/// </summary>
	public static class ModExportWindow
	{
		/// <summary>Exhumed's enhancements (docs/ENHANCEMENTS.md), and the game each needs (0 both).</summary>
		static readonly string[] Enhancements = { "skip-intro", "wrap-menu", "fast-panels", "free-heading", "subtitles", "skill-messages", "perspective", "full-sprites", "wide-pitch", "mouse-look", "invert-look", "modern-keys", "rune-keys", "tidy-objects", "restore-at-rest" };
		static readonly int[] EnhancementGame = { 0, 0, 0, 0, 2, 2, 0, 0, 0, 0, 0, 0, 0, 1, 0 };

		sealed class State
		{
			public string GameRoot, BaseFolder = "";
			public GameKind Game;
			public string Name = "", Version = "1.0.0", Authors = "", Id = "", Description = "", License = "", Output = "";
			public bool IdEdited;
			public readonly HashSet<string> Recommended = new HashSet<string>();
			public int Source;   // 0: .BKP backups, 1: another folder
			public ModDiffResult Diff;
			public int SaveSlot = -1;
		}

		static State last;

		public static void Show()
		{
			string data = ExtensionsMenu.DataFolder;
			if (data == null) throw new InvalidOperationException("Open the game first (File > Open game...).");
			string root = Path.GetDirectoryName(Path.GetFullPath(data).TrimEnd('/', '\\'));
			var s = last != null && last.GameRoot == root ? last : new State { GameRoot = root, Game = LevArk.Game.Kind };
			last = s;
			if (string.IsNullOrEmpty(s.Output)) s.Output = Path.Combine(Path.GetDirectoryName(root) ?? root, "UW Mods");
			var w = UiKit.Window("Export as mod (.uwmod)", 820, 640);
			UiKit.Paragraph(w.Toolbar,
				"Packs what changed in " + root + " into a mod package for the mod manager. Save your edits first (the export reads the files on disk). " +
				"Only the changes go in the package: archive blocks, strings, pictures, byte ranges, the start square and byte patches of the executable, new files. " +
				"The originals are the editor's .BKP backups, or a folder with the unmodified game.", 10);
			Build(w, s);
		}

		static void Build(Window w, State s)
		{
			UiKit.Clear(w.Content);
			var c = w.Content;

			UiKit.Label(c, "1. The originals", 12, FontStyle.Bold);
			var r = UiKit.Row(c, 20);
			UiKit.Cycle(r, new[] { "the .BKP backups", "another folder:" }, s.Source, i => { s.Source = i; s.Diff = null; Build(w, s); }, 150);
			if (s.Source == 1) UiKit.Input(r, s.BaseFolder, t => { s.BaseFolder = t.Trim(); s.Diff = null; }, 520, false);
			UiKit.Paragraph(c, s.Source == 0
				? "Each file the editor saved has a one-time copy of its original next to it (LEV.ARK.BKP, UW.EXE.BKP...); files without one count as unchanged."
				: "A folder holding the unmodified game (UW.EXE or UW2.EXE, DATA, CRIT, CUTS, SOUND), such as a second copy of the GOG install.", 10, UiKit.DimTextColor);

			UiKit.Label(c, "2. What changed", 12, FontStyle.Bold);
			var r2 = UiKit.Row(c, 22);
			UiKit.Button(r2, "Find the changes", () => { Scan(w, s); Build(w, s); }, 140, 22, UiKit.AccentColor);
			if (s.Diff != null)
			{
				var d = s.Diff;
				if (d.Components.Count == 0) UiKit.Label(c, "Nothing changed.", 11, FontStyle.Italic, UiKit.DimTextColor);
				foreach (var comp in d.Components) UiKit.Label(c, "   " + comp.Summary, 11);
				foreach (var p in d.Problems) UiKit.Label(c, "   Problem: " + p, 11, FontStyle.Bold, UiKit.WarnColor);
				foreach (var n in d.Notes) UiKit.Label(c, "   Note: " + n, 10, FontStyle.Normal, UiKit.DimTextColor);
				if (d.Components.Count > 0)
				{
					var m = Preview(s);
					UiKit.Label(c, "   Type: " + m.Type + (m.ExhumedNeeds.Count > 0 ? "   (Exhumed needs: " + string.Join(", ", m.ExhumedNeeds) + ")" : ""), 11, FontStyle.Bold);
					foreach (var warn in ModClassifier.Warnings(m)) UiKit.Label(c, "   " + warn, 10, FontStyle.Normal, UiKit.WarnColor);
				}
			}

			UiKit.Label(c, "3. About the mod", 12, FontStyle.Bold);
			Field(c, "Name", s.Name, t => { s.Name = t; if (!s.IdEdited) { s.Id = ModBuilder.IdFrom(FirstAuthor(s), t); Build(w, s); } });
			Field(c, "Author(s)", s.Authors, t => { s.Authors = t; if (!s.IdEdited) { s.Id = ModBuilder.IdFrom(FirstAuthor(s), s.Name); Build(w, s); } }, "comma separated");
			Field(c, "Id", s.Id, t => { s.Id = t.Trim(); s.IdEdited = true; }, "unique, e.g. karl.gate-of-dawn");
			Field(c, "Version", s.Version, t => s.Version = t.Trim());
			Field(c, "Licence", s.License, t => s.License = t, "e.g. CC BY 4.0");
			var dr = UiKit.Row(c, 64);
			UiKit.Label(dr, "Description", 11, FontStyle.Normal, null, 90);
			var desc = UiKit.Input(dr, s.Description, t => s.Description = t, 600, false, 64);
			desc.lineType = InputField.LineType.MultiLineNewline;
			UiKit.Label(c, "Exhumed enhancements to recommend:", 11);
			RectTransform er = null;
			for (int i = 0, n = 0; i < Enhancements.Length; i++)
			{
				int g = EnhancementGame[i];
				if (g != 0 && g != (s.Game == GameKind.UW2 ? 2 : 1)) continue;
				if (n++ % 5 == 0) er = UiKit.Row(c, 20, 4);
				string e = Enhancements[i];
				UiKit.Check(er, e, s.Recommended.Contains(e), v => { if (v) s.Recommended.Add(e); else s.Recommended.Remove(e); }, 140);
			}

			UiKit.Label(c, "4. Export", 12, FontStyle.Bold);
			Field(c, "Folder", s.Output, t => s.Output = t.Trim());
			var xr = UiKit.Row(c, 22);
			UiKit.Button(xr, "Export the changes", () => w.Status.text = ExportChanges(s), 160, 22, UiKit.AccentColor);

			var slots = GameSession.FindSlots(Path.Combine(s.GameRoot, "DATA")).Where(x => !x.IsWorkingCopy && x.HasLevels && x.HasPlayer).ToList();
			if (slots.Count > 0)
			{
				var sr = UiKit.Row(c, 22);
				UiKit.Label(sr, "or a saved game:", 11, FontStyle.Normal, null, 110);
				var names = slots.Select(x => x.Name + (string.IsNullOrEmpty(x.Description) ? "" : " - " + x.Description)).ToList();
				if (s.SaveSlot < 0 || s.SaveSlot >= slots.Count) s.SaveSlot = 0;
				UiKit.Cycle(sr, names.ToArray(), s.SaveSlot, i => s.SaveSlot = i, 260);
				UiKit.Button(sr, "Export the saved game", () => w.Status.text = ExportSave(s, slots[s.SaveSlot]), 170, 22);
			}
		}

		static void Field(Transform parent, string label, string value, Action<string> set, string hint = null)
		{
			var r = UiKit.Row(parent, 20);
			UiKit.Label(r, label, 11, FontStyle.Normal, null, 90);
			UiKit.Input(r, value, t => set(t), 420, false);
			if (hint != null) UiKit.Label(r, hint, 10, FontStyle.Italic, UiKit.DimTextColor, 220);
		}

		static string FirstAuthor(State s) { return (s.Authors ?? "").Split(',').Select(a => a.Trim()).FirstOrDefault(a => a.Length > 0) ?? ""; }

		static IFileSource Original(State s)
		{
			if (s.Source == 0) return new BackupBaseFolder(s.GameRoot);
			if (string.IsNullOrEmpty(s.BaseFolder) || !Directory.Exists(s.BaseFolder)) throw new DirectoryNotFoundException("The folder with the original game was not found: " + s.BaseFolder);
			if (string.Equals(Path.GetFullPath(s.BaseFolder).TrimEnd('/', '\\'), s.GameRoot.TrimEnd('/', '\\'), StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("That is the game folder itself; choose a folder with the unmodified game.");
			return new GameFolder(s.BaseFolder);
		}

		static void Scan(Window w, State s)
		{
			try
			{
				s.Diff = ModDiff.Compare(Original(s), new GameFolder(s.GameRoot), s.Game);
				w.Status.text = s.Diff.Components.Count + " change(s) found.";
			}
			catch (Exception e)
			{
				s.Diff = null;
				w.Status.text = "Error: " + e.Message;
				Debug.LogException(e);
			}
		}

		static ModManifest Details(State s)
		{
			var m = new ModManifest
			{
				Id = string.IsNullOrEmpty(s.Id) ? ModBuilder.IdFrom(FirstAuthor(s), s.Name) : s.Id,
				Name = s.Name.Trim(),
				Version = string.IsNullOrEmpty(s.Version) ? "1.0.0" : s.Version,
				Game = s.Game == GameKind.UW2 ? "uw2" : "uw1",
				Description = s.Description ?? "",
				License = s.License ?? "",
				MadeWith = "UltimaUnderworldEditor " + Application.version,
			};
			foreach (var a in (s.Authors ?? "").Split(',')) if (a.Trim().Length > 0) m.Authors.Add(a.Trim());
			foreach (var e in Enhancements) if (s.Recommended.Contains(e)) m.ExhumedRecommended.Add(e);
			return m;
		}

		static ModManifest Preview(State s)
		{
			var pkg = ModBuilder.FromDiff(s.Diff, Details(s), SafeOriginal(s));
			return pkg.Manifest;
		}

		static IFileSource SafeOriginal(State s) { try { return Original(s); } catch (Exception) { return null; } }

		static string ExportChanges(State s)
		{
			try
			{
				if (string.IsNullOrEmpty(s.Name.Trim())) return "Give the mod a name first.";
				var original = Original(s);
				var current = new GameFolder(s.GameRoot);
				s.Diff = ModDiff.Compare(original, current, s.Game);
				if (s.Diff.Problems.Count > 0) return "Not exported: " + s.Diff.Problems[0] + (s.Diff.Problems.Count > 1 ? " (and " + (s.Diff.Problems.Count - 1) + " more; Find the changes lists them)" : "");
				if (s.Diff.Components.Count == 0) return "Nothing changed: there is nothing to export.";
				var pkg = ModBuilder.FromDiff(s.Diff, Details(s), original);
				pkg.Readme = Readme(pkg.Manifest);
				var bad = ModBuilder.Verify(pkg, original, current);
				if (bad.Count > 0) return "Not exported, the package would not give back these files: " + string.Join("; ", bad);
				return Save(s, pkg, pkg.Manifest.Id + "-" + pkg.Manifest.Version);
			}
			catch (Exception e) { Debug.LogException(e); return "Error: " + e.Message; }
		}

		static string ExportSave(State s, GameSession.Slot slot)
		{
			try
			{
				if (string.IsNullOrEmpty(s.Name.Trim())) return "Give the mod a name first.";
				var m = Details(s);
				var pkg = new ModPackage { Manifest = m };
				int n = slot.Name.Length == 5 ? slot.Name[4] - '0' : 1;
				var comp = SaveMods.Export(slot.Folder, new GameFolder(s.GameRoot), s.Game, n, pkg.Files);
				m.Components.Add(comp);
				m.Created = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture);
				m.BaseFiles = new SortedDictionary<string, string>(comp.DependsOn, StringComparer.Ordinal);
				m.BaseSet = KnownFiles.SetOf(s.Game, m.BaseFiles);
				ModClassifier.Classify(m, null, pkg);
				pkg.Readme = Readme(m);
				return Save(s, pkg, m.Id + "-" + slot.Name.ToLowerInvariant() + "-" + m.Version);
			}
			catch (Exception e) { Debug.LogException(e); return "Error: " + e.Message; }
		}

		static string Save(State s, ModPackage pkg, string baseName)
		{
			var problems = pkg.Check();
			if (problems.Count > 0) return "Not exported: " + string.Join("; ", problems);
			Directory.CreateDirectory(s.Output);
			string path = Path.Combine(s.Output, Safe(baseName) + ModPackage.Extension);
			byte[] bytes = pkg.ToBytes();
			File.WriteAllBytes(path, bytes);
			return string.Format("Exported {0} ({1}, {2:N0} KB, {3} component(s)).", path, pkg.Manifest.Type, (bytes.Length + 1023) / 1024, pkg.Manifest.Components.Count);
		}

		static string Safe(string name)
		{
			foreach (char ch in Path.GetInvalidFileNameChars()) name = name.Replace(ch, '-');
			return name;
		}

		static string Readme(ModManifest m)
		{
			var sb = new System.Text.StringBuilder();
			sb.Append("# ").Append(m.Name).Append(' ').Append(m.Version).Append("\n\n");
			if (m.Authors.Count > 0) sb.Append("By ").Append(string.Join(", ", m.Authors)).Append(". ");
			sb.Append(m.Type).Append(" for ").Append(m.GameName).Append(".\n\n");
			if (!string.IsNullOrEmpty(m.Description)) sb.Append(m.Description.Trim()).Append("\n\n");
			foreach (var w in ModClassifier.Warnings(m)) sb.Append("- ").Append(w).Append('\n');
			sb.Append("\n## Changes\n\n");
			foreach (var c in m.Components) sb.Append("- ").Append(c.Summary).Append('\n');
			if (m.ExhumedRecommended.Count > 0) sb.Append("\nRecommended Exhumed enhancements: ").Append(string.Join(", ", m.ExhumedRecommended)).Append(".\n");
			sb.Append("\nInstall it with the Ultima Underworlds Mod Manager.\n");
			return sb.ToString();
		}
	}
}
