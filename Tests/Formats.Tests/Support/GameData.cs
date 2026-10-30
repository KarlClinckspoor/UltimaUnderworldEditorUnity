using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]   // the editor's readers keep static state

namespace Formats.Tests
{
	/// <summary>
	/// The game files the tests run on. Set UW_GAMEDATA to a folder holding UW (UW.EXE, DATA, CRIT...)
	/// and/or UW2; without it the repository's own gamedata folder is used. A test that needs a game
	/// that is not there is reported as skipped (<see cref="GameFactAttribute"/>,
	/// <see cref="GameTheoryAttribute"/>), unless UW_REQUIRE_GAMEDATA=1, which makes it fail instead.
	///
	/// Each test works on a fresh copy, with the editor's .BKP backups put back in place of the
	/// files they back up, so it sees the shipped files and never changes the originals. The
	/// copies are deleted when the test run ends (and any left by a run that was killed, at the
	/// start of the next one).
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

		/// <summary>True when UW_REQUIRE_GAMEDATA=1: missing game files fail the tests instead of skipping them.</summary>
		public static bool Required { get { return Environment.GetEnvironmentVariable("UW_REQUIRE_GAMEDATA") == "1"; } }

		/// <summary>Why a test for this game (null: either game) is skipped, or null when it can run.</summary>
		public static string SkipReason(string game)
		{
			if (Required) return null;
			bool ok = game == null ? Has("UW") || Has("UW2") : Has(game);
			if (ok) return null;
			return "no " + (game == null ? "UW or UW2" : game) + " game files in " + Root + " (set UW_GAMEDATA)";
		}

		static readonly string TempRoot = Path.Combine(Path.GetTempPath(), "uwtests");
		static readonly string RunRoot = Path.Combine(TempRoot, System.Diagnostics.Process.GetCurrentProcess().Id + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
		static bool cleanupHooked;

		/// <summary>Copies go in a folder per test process. The folders of processes that have
		/// ended (killed, or exited before ProcessExit could delete them) are deleted here.</summary>
		static void HookCleanup()
		{
			if (cleanupHooked) return;
			cleanupHooked = true;
			if (Directory.Exists(TempRoot))
				foreach (var d in Directory.GetDirectories(TempRoot))
				{
					string name = Path.GetFileName(d);
					int dash = name.IndexOf('-');
					int pid;
					if (dash > 0 && int.TryParse(name.Substring(0, dash), out pid) && Running(pid)) continue;
					TryDelete(d);
				}
			AppDomain.CurrentDomain.ProcessExit += (s, e) => TryDelete(RunRoot);
		}

		static bool Running(int pid)
		{
			try { return !System.Diagnostics.Process.GetProcessById(pid).HasExited; }
			catch (Exception) { return false; }
		}

		static void TryDelete(string dir)
		{
			try { Directory.Delete(dir, true); } catch (Exception) { }
		}

		public static IEnumerable<object[]> Available()
		{
			foreach (var g in Games) if (Has(g)) yield return new object[] { g };
			// xUnit needs at least one row; "none" makes each test return at once
			if (!Has("UW") && !Has("UW2")) yield return new object[] { "none" };
		}

		/// <summary>A fresh, writable copy of the game folder with the shipped files (null: no such game).</summary>
		public static string Copy(string game)
		{
			if (!Has(game))
			{
				if (Required && game != "none") throw new DirectoryNotFoundException("UW_REQUIRE_GAMEDATA is set but " + Path.Combine(Root, game, "DATA") + " is missing");
				return null;
			}
			HookCleanup();
			string dst = Path.Combine(RunRoot, game + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
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

	/// <summary>A test that needs game files: skipped when there are none (see <see cref="GameData"/>).
	/// With a game name, it needs that game.</summary>
	public sealed class GameFactAttribute : FactAttribute
	{
		public GameFactAttribute(string game = null)
		{
			string why = GameData.SkipReason(game);
			if (why != null) Skip = why;
		}
	}

	/// <summary>A theory over <see cref="GameData.Available"/>: skipped when neither game is there.</summary>
	public sealed class GameTheoryAttribute : TheoryAttribute
	{
		public GameTheoryAttribute()
		{
			string why = GameData.SkipReason(null);
			if (why != null) Skip = why;
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
