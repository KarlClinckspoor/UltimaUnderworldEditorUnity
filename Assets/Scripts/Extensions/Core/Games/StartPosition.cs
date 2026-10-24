using System;
using System.IO;

namespace UltimateEditor.Games
{
	/// <summary>
	/// Where a new game puts the player. It is not in LEV.ARK: the main menu's "create
	/// character" code calls player_setup with constants, on level 1 (uw1 ui/MAINMENU.C:
	/// player_setup(0x20, 2, 1); uw2 ui/MAINMENU.C: player_setup(0x13, 0x30, -1)), and
	/// player_setup puts the player in the middle of that square at its floor height
	/// (motion/PHYSICS.C). In the executables the call starts with three byte pushes
	/// (6A how, 6A y, 6A x) followed by a far call (9A), which this finds and patches. A saved
	/// game keeps the position in PLAYER.DAT instead (Games/PlayerDat.cs).
	/// </summary>
	public static class StartPosition
	{
		/// <summary>The x, y read from the executable at load, if found (MapCreator.GameStart uses it).</summary>
		public static int? ExeX, ExeY;

		/// <summary>Offset of the three pushes in the executable, or -1 (not found or not unique).
		/// Any x and y are accepted, so an already patched executable is found too.</summary>
		public static int Find(byte[] exe, GameKind game)
		{
			byte how = game == GameKind.UW2 ? (byte)0xFF : (byte)0x01;
			var all = new System.Collections.Generic.List<int>();
			var original = new System.Collections.Generic.List<int>();
			for (int i = 0; i + 7 <= exe.Length; i++)
			{
				if (exe[i] != 0x6A || exe[i + 1] != how || exe[i + 2] != 0x6A || exe[i + 4] != 0x6A || exe[i + 6] != 0x9A) continue;
				if (exe[i + 3] > 63 || exe[i + 5] > 63) continue;
				// after the far call: add sp, 6 and the push 0 of do_level_hacks(1, 0) (both games)
				if (i + 16 > exe.Length || exe[i + 11] != 0x83 || exe[i + 12] != 0xC4 || exe[i + 13] != 0x06 || exe[i + 14] != 0x6A || exe[i + 15] != 0x00) continue;
				all.Add(i);
				if (Original(exe, i, game)) original.Add(i);
			}
			if (original.Count == 1) return original[0];
			// patched before: accepted only with the shipped executable's call target, so another
			// version's code is never taken for it
			if (original.Count == 0 && all.Count == 1 && CallTargetKnown(exe, all[0], game)) return all[0];
			return -1;
		}

		/// <summary>The far call's target in the shipped executables (UW.EXE d1 06 48 00, UW2.EXE d8 0a 48 00).</summary>
		static bool CallTargetKnown(byte[] exe, int i, GameKind game)
		{
			var t = game == GameKind.UW2 ? new byte[] { 0xD8, 0x0A, 0x48, 0x00 } : new byte[] { 0xD1, 0x06, 0x48, 0x00 };
			for (int k = 0; k < 4; k++) if (exe[i + 7 + k] != t[k]) return false;
			return true;
		}

		static bool Original(byte[] exe, int i, GameKind game)
		{
			return game == GameKind.UW2 ? exe[i + 3] == 0x30 && exe[i + 5] == 0x13 : exe[i + 3] == 0x02 && exe[i + 5] == 0x20;
		}

		public static bool Read(byte[] exe, GameKind game, out int x, out int y)
		{
			int at = Find(exe, game);
			x = y = 0;
			if (at < 0) return false;
			y = exe[at + 3]; x = exe[at + 5];
			return true;
		}

		/// <summary>Writes x, y (1-62) into the executable's bytes; returns false if the call was not found.</summary>
		public static bool Patch(byte[] exe, GameKind game, int x, int y)
		{
			if (x < 1 || x > 62 || y < 1 || y > 62) throw new ArgumentOutOfRangeException("x/y", "the start square must be 1-62");
			int at = Find(exe, game);
			if (at < 0) return false;
			exe[at + 3] = (byte)y; exe[at + 5] = (byte)x;
			return true;
		}

		/// <summary>UW.EXE or UW2.EXE in the game folder (DATA's parent).</summary>
		public static string ExePath(string dataFolder, GameKind game)
		{
			if (string.IsNullOrEmpty(dataFolder)) return null;
			string root = Path.GetDirectoryName(Path.GetFullPath(dataFolder).TrimEnd('/', '\\'));
			if (root == null) return null;
			string p = GameFiles.Resolve(Path.Combine(root, game == GameKind.UW2 ? "UW2.EXE" : "UW.EXE"));
			return File.Exists(p) ? p : null;
		}

		/// <summary>Reads the start square from the executable next to DATA into ExeX/ExeY.</summary>
		public static void Load(string dataFolder, GameKind game)
		{
			ExeX = ExeY = null;
			try
			{
				string p = ExePath(dataFolder, game);
				if (p == null) return;
				int x, y;
				if (Read(File.ReadAllBytes(p), game, out x, out y)) { ExeX = x; ExeY = y; }
			}
			catch (Exception) { }   // unreadable executable: the default start is shown
		}
	}
}
