using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]   // the editor's readers keep static state

namespace Formats.Tests
{
	/// <summary>
	/// The game files the tests run on. Set UW_GAMEDATA to a folder holding UW (UW.EXE, DATA, CRIT...)
	/// and/or UW2; without it the repository's own gamedata folder is used. A game that is not
	/// there is skipped, so the tests also run (and pass trivially) without any game files.
	///
	/// Each test works on a fresh copy, with the editor's .BKP backups put back in place of the
	/// files they back up, so it sees the shipped files and never changes the originals.
	/// </summary>
	public static class GameData
	{
		public static readonly string[] Games = { "UW", "UW2" };

		public static string Root
		{
			get
			{
				string env = Environment.GetEnvironmentVariable("UW_GAMEDATA");
				if (!string.IsNullOrEmpty(env)) return env;
				return Path.Combine(Repo.Root, "gamedata");
			}
		}

		public static bool Has(string game) { return Directory.Exists(Path.Combine(Root, game, "DATA")); }

		public static IEnumerable<object[]> Available()
		{
			foreach (var g in Games) if (Has(g)) yield return new object[] { g };
			// xUnit needs at least one row; "none" makes each test return at once
			if (!Has("UW") && !Has("UW2")) yield return new object[] { "none" };
		}

		/// <summary>A fresh, writable copy of the game folder with the shipped files (null: no such game).</summary>
		public static string Copy(string game)
		{
			if (!Has(game)) return null;
			string dst = Path.Combine(Path.GetTempPath(), "uwtests", game + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
			CopyDir(Path.Combine(Root, game), dst);
			foreach (var bkp in Directory.GetFiles(dst, "*.BKP", SearchOption.AllDirectories))
			{
				string orig = bkp.Substring(0, bkp.Length - 4);
				File.Copy(bkp, orig, true);
				File.Delete(bkp);
			}
			return dst;
		}

		static void CopyDir(string src, string dst)
		{
			Directory.CreateDirectory(dst);
			foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)));
			foreach (var d in Directory.GetDirectories(src)) CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
		}

		/// <summary>The file in DATA, matched without case.</summary>
		public static string InData(string gameDir, string name)
		{
			string data = Path.Combine(gameDir, "DATA");
			foreach (var f in Directory.GetFiles(data))
				if (string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)) return f;
			return null;
		}

		public static string Exe(string gameDir)
		{
			foreach (var f in Directory.GetFiles(gameDir))
			{
				string n = Path.GetFileName(f).ToUpperInvariant();
				if (n == "UW.EXE" || n == "UW2.EXE") return f;
			}
			return null;
		}
	}

	public static class Repo
	{
		/// <summary>The repository root: the first folder above the tests that holds Assets and Tests.</summary>
		public static string Root
		{
			get
			{
				var d = new DirectoryInfo(AppContext.BaseDirectory);
				while (d != null && !(Directory.Exists(Path.Combine(d.FullName, "Assets")) && Directory.Exists(Path.Combine(d.FullName, "Tests"))))
					d = d.Parent;
				if (d == null) throw new DirectoryNotFoundException("repository root not found above " + AppContext.BaseDirectory);
				return d.FullName;
			}
		}

		public static string Scripts { get { return Path.Combine(Root, "Assets", "Scripts"); } }
		public static string LegacyScripts { get { return Path.Combine(Root, "Tests", "Legacy", "obj", "legacy-src", "Assets", "Scripts"); } }
	}
}
