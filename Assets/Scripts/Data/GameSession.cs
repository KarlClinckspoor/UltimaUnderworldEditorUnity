using System;
using System.Collections.Generic;
using System.IO;
using UltimateEditor.Games;

/// <summary>
/// Which game files the editor works on. A saved game (SAVE1..SAVE4 next to DATA, and the
/// game's working copy SAVE0) holds its own LEV.ARK, PLAYER.DAT, BGLOBALS.DAT and DESC, and
/// in UW2 SCD.ARK; everything else (strings, conversations, object tables, graphics) is read
/// from DATA (uw1/uw2 game/UWEDIT.C, inv/INVSAVE.C, docs/FORMATS.md). Choosing a save's
/// LEV.ARK, anywhere a LEV.ARK is chosen, opens that save with DATA for the rest.
/// </summary>
public static class GameSession
{
	/// <summary>The LEV.ARK being edited: DATA's, or a save's.</summary>
	public static string LevArkPath;
	/// <summary>The save folder being edited, or null for DATA.</summary>
	public static string SaveFolder;

	public static bool IsSave { get { return SaveFolder != null; } }
	public static string SaveName { get { return SaveFolder == null ? null : Path.GetFileName(SaveFolder.TrimEnd('/', '\\')); } }

	/// <summary>
	/// Takes the LEV.ARK the user chose. In a SAVEn folder with a DATA folder beside it, the
	/// save's LEV.ARK is edited and DataReader.FilePath points at DATA's LEV.ARK, so the
	/// other files are found where the game keeps them. Returns the path for DataReader.FilePath.
	/// </summary>
	public static string Choose(string chosenLevArk)
	{
		LevArkPath = chosenLevArk;
		SaveFolder = null;
		if (string.IsNullOrEmpty(chosenLevArk)) return chosenLevArk;
		string dir = Path.GetDirectoryName(Path.GetFullPath(chosenLevArk));
		if (!IsSaveFolderName(Path.GetFileName(dir))) return chosenLevArk;
		string data = GameFiles.Resolve(Path.Combine(Path.GetDirectoryName(dir), "DATA"));
		string dataLev = GameFiles.Resolve(Path.Combine(data, "LEV.ARK"));
		if (!File.Exists(dataLev)) return chosenLevArk;   // no DATA beside it: treat as a plain folder
		SaveFolder = dir;
		return dataLev;
	}

	/// <summary>The LEV.ARK to load: the save's when one is open.</summary>
	public static string LevelFile(string dataReaderPath)
	{
		if (LevArkPath != null && File.Exists(LevArkPath) && (IsSave || string.Equals(Path.GetFullPath(LevArkPath), Path.GetFullPath(dataReaderPath), StringComparison.OrdinalIgnoreCase)))
			return LevArkPath;
		return dataReaderPath;
	}

	/// <summary>A file of the open save (PLAYER.DAT, BGLOBALS.DAT, SCD.ARK, DESC), or null when DATA is open.</summary>
	public static string SaveFile(string name)
	{
		return SaveFolder == null ? null : GameFiles.Resolve(Path.Combine(SaveFolder, name));
	}

	public static bool IsSaveFolderName(string name)
	{
		if (name == null) return false;
		name = name.ToUpperInvariant();
		return name.Length == 5 && name.StartsWith("SAVE") && char.IsDigit(name[4]);
	}

	public sealed class Slot
	{
		public string Folder, Name, Description;
		public bool HasLevels, HasPlayer, IsWorkingCopy;
		public DateTime Written;
	}

	/// <summary>The SAVEn folders next to DATA (DATA's parent is the game folder).</summary>
	public static List<Slot> FindSlots(string dataFolder)
	{
		var list = new List<Slot>();
		if (string.IsNullOrEmpty(dataFolder)) return list;
		string root = Path.GetDirectoryName(Path.GetFullPath(dataFolder).TrimEnd('/', '\\'));
		if (root == null || !Directory.Exists(root)) return list;
		foreach (var dir in Directory.GetDirectories(root))
		{
			string n = Path.GetFileName(dir);
			if (!IsSaveFolderName(n)) continue;
			string lev = GameFiles.Resolve(Path.Combine(dir, "LEV.ARK"));
			var s = new Slot
			{
				Folder = dir, Name = n.ToUpperInvariant(),
				HasLevels = File.Exists(lev),
				HasPlayer = File.Exists(GameFiles.Resolve(Path.Combine(dir, "PLAYER.DAT"))),
				IsWorkingCopy = n.EndsWith("0"),
				Written = File.Exists(lev) ? File.GetLastWriteTime(lev) : DateTime.MinValue,
			};
			string desc = GameFiles.Resolve(Path.Combine(dir, "DESC"));
			s.Description = File.Exists(desc) ? ReadDesc(desc) : "";
			list.Add(s);
		}
		list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
		return list;
	}

	/// <summary>DESC: the save's description, raw text with no terminator (GAMEWRAP.C SaveGame).</summary>
	public static string ReadDesc(string path)
	{
		var b = File.ReadAllBytes(path);
		var c = new char[b.Length];
		int n = 0;
		foreach (var x in b) { if (x == 0) break; c[n++] = (char)x; }
		return new string(c, 0, n);
	}

	public static void WriteDesc(string path, string text)
	{
		var b = new byte[(text ?? "").Length];
		for (int i = 0; i < b.Length; i++) b[i] = text[i] < 256 ? (byte)text[i] : (byte)'?';
		BackupOnce(path);
		File.WriteAllBytes(path, b);
	}

	/// <summary>The editor's one-time .BKP copy, for any file by full path.</summary>
	public static void BackupOnce(string path)
	{
		if (File.Exists(path) && !File.Exists(path + ".BKP")) File.Copy(path, path + ".BKP");
	}
}
