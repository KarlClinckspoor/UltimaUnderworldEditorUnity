using System;
using System.IO;

namespace UltimateEditor.Games
{
	public enum GameKind { UW1, UW2 }

	/// <summary>
	/// What differs between the two games' data, in one place (reference docs/FORMATS.md and
	/// docs/UW1-UW2-DIFFERENCES.md). Everything that reads or writes game files asks the
	/// profile instead of using UW1's numbers.
	/// </summary>
	public sealed class GameProfile
	{
		public GameKind Kind { get; private set; }
		public string Name { get; private set; }

		// LEV.ARK
		public int ArchiveBlocks { get; private set; }      // 135 / 320
		public int LevelSlots { get; private set; }         // 9 / 80
		public int LevelBlockSize { get; private set; }     // 0x7C08 / 0x7E08
		public int TextureBlockSize { get; private set; }   // 0x7A / 0x86
		public int TextureBlockBase { get; private set; }   // level + 0x11 / level + 0x4F
		public int OverlayBlockBase { get; private set; }   // level + 8 / -1 (inside the level block)
		public int AutomapBlockBase { get; private set; }   // level + 0x1A / level + 0x9F
		public int NotesBlockBase { get; private set; }     // level + 0x23 / level + 0xEF
		public int LevelsPerWorld { get; private set; }     // 9 / 8

		// textures
		public int WallTextures { get; private set; }       // 48 / 64 (UW2: one list for walls and floors)
		public int FloorTextures { get; private set; }      // 10 / 64
		public string WallTextureFile { get; private set; } // W64.TR / T64.TR
		public string FloorTextureFile { get; private set; }// F32.TR / T64.TR

		// other files
		public int Palettes { get; private set; }           // 8 / 11
		public int PlayerRecordSize { get; private set; }   // 0xD2 / 0x37D
		public bool HasSchedules { get { return Kind == GameKind.UW2; } }
		public bool ArchivesCompressed { get { return Kind == GameKind.UW2; } }

		public static readonly GameProfile UW1 = new GameProfile
		{
			Kind = GameKind.UW1, Name = "Ultima Underworld",
			ArchiveBlocks = 135, LevelSlots = 9, LevelBlockSize = 0x7C08, TextureBlockSize = 0x7A,
			TextureBlockBase = 0x11, OverlayBlockBase = 8, AutomapBlockBase = 0x1A, NotesBlockBase = 0x23, LevelsPerWorld = 9,
			WallTextures = 48, FloorTextures = 10, WallTextureFile = "W64.TR", FloorTextureFile = "F32.TR",
			Palettes = 8, PlayerRecordSize = 0xD2,
		};

		public static readonly GameProfile UW2 = new GameProfile
		{
			Kind = GameKind.UW2, Name = "Ultima Underworld II",
			ArchiveBlocks = 320, LevelSlots = 80, LevelBlockSize = 0x7E08, TextureBlockSize = 0x86,
			TextureBlockBase = 0x4F, OverlayBlockBase = -1, AutomapBlockBase = 0x9F, NotesBlockBase = 0xEF, LevelsPerWorld = 8,
			WallTextures = 64, FloorTextures = 64, WallTextureFile = "T64.TR", FloorTextureFile = "T64.TR",
			Palettes = 11, PlayerRecordSize = 0x37D,
		};

		public static GameProfile For(GameKind k) { return k == GameKind.UW2 ? UW2 : UW1; }

		/// <summary>Block numbers of a 1-based level.</summary>
		public int LevelBlock(int level) { return level - 1; }
		public int TextureBlock(int level) { return level + TextureBlockBase; }
		public int OverlayBlock(int level) { return OverlayBlockBase < 0 ? -1 : level + OverlayBlockBase; }
		public int AutomapBlock(int level) { return level + AutomapBlockBase; }
		public int NotesBlock(int level) { return level + NotesBlockBase; }

		/// <summary>UW2's worlds: (level - 1) / 8 (Britannia, Prison Tower, Killorn Keep, Ice Caverns, Talorus, Scintillus Academy, Tomb of Praecor Loth, Pits of Carnage, Ethereal Void).</summary>
		public static readonly string[] Uw2Worlds = { "Britannia", "Prison Tower", "Killorn Keep", "Ice Caverns", "Talorus", "Scintillus Academy", "Tomb of Praecor Loth", "Pits of Carnage", "Ethereal Void", "(unused)" };

		public string LevelName(int level)
		{
			if (Kind == GameKind.UW1) return "Level " + level;
			int w = (level - 1) / LevelsPerWorld, l = (level - 1) % LevelsPerWorld + 1;
			return string.Format("{0} {1} ({2})", w < Uw2Worlds.Length ? Uw2Worlds[w] : "World " + w, l, level);
		}

		/// <summary>
		/// Tells the games apart by their files: UW2 has T64.TR and SCD.ARK and no W64.TR;
		/// failing that, a LEV.ARK whose long after the count is 0 with four tables that fit is UW2's.
		/// </summary>
		public static GameKind Detect(string dataFolder)
		{
			if (Exists(dataFolder, "T64.TR") || Exists(dataFolder, "SCD.ARK")) return GameKind.UW2;
			if (Exists(dataFolder, "W64.TR") || Exists(dataFolder, "F32.TR")) return GameKind.UW1;
			string lev = Find(dataFolder, "LEV.ARK");
			if (lev != null)
			{
				var head = new byte[6];
				using (var fs = File.OpenRead(lev)) fs.Read(head, 0, 6);
				int n = head[0] | head[1] << 8;
				uint l = (uint)(head[2] | head[3] << 8 | head[4] << 16 | head[5] << 24);
				if (n == 320 && l == 0) return GameKind.UW2;
			}
			return GameKind.UW1;
		}

		static bool Exists(string folder, string name) { return Find(folder, name) != null; }

		/// <summary>A file in the folder, matching the name case-insensitively (the games' names are upper case on DOS).</summary>
		public static string Find(string folder, string name)
		{
			if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return null;
			foreach (var f in Directory.GetFiles(folder))
				if (string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)) return f;
			return null;
		}
	}
}
