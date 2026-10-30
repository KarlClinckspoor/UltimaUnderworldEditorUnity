using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UltimateEditor.Games;

/// <summary>
/// Every game file the editor writes goes through here (levels, pictures, strings,
/// conversations, object data, the tools' save, player and schedule files):
///
/// - A file is written only when its bytes change.
/// - It is written to a temporary file first and then put in place, so a failure (a full disk,
///   a crash) leaves the old file whole.
/// - The first time, the original is kept as name.BKP beside it, as the editor always did: the
///   shipped file, which mod export compares with. In a saved game the game itself rewrites the
///   files when it saves, so there the .BKP follows the game: when the file is no longer what
///   the editor last wrote, the game has saved since, and that version becomes the .BKP.
/// - Before each write, the file as it was is copied to "Editor backups" in the game's folder
///   (beside DATA, never inside it), keeping the last <see cref="Versions"/> versions of each
///   file (10 by default, set in Tools > Autosave, 0 to 1000).
///
/// While an autosave runs (<see cref="RunIn"/>), writes go instead to a copy of the
/// game in "Editor autosaves/date time", so autosave never changes the game's own files and the
/// copy can be opened as a game. The editor's record of what was saved (which pictures and
/// strings are on disk) is not updated by a snapshot (<see cref="Snapshot"/>).
/// </summary>
public static class GameWrite
{
	public const int DefaultVersions = 10, MaxVersions = 1000;
	const string PrefKey = "UltimateEditor.BackupVersions";
	public const string BackupsFolder = "Editor backups", AutosavesFolder = "Editor autosaves";
	const string WrittenFile = "written.txt";

	/// <summary>Older versions kept of each file, and autosave copies kept (0 to 1000).</summary>
	public static int Versions
	{
		get { return Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, DefaultVersions), 0, MaxVersions); }
		set { PlayerPrefs.SetInt(PrefKey, Mathf.Clamp(value, 0, MaxVersions)); PlayerPrefs.Save(); }
	}

	/// <summary>The autosave copy being written, or null.</summary>
	public static string SnapshotRoot { get; private set; }
	/// <summary>True while an autosave writes its copy: nothing may record the game's files as saved.</summary>
	public static bool Snapshot { get { return SnapshotRoot != null; } }

	/// <summary>The game's folder: the one holding DATA (and CRIT, the executable, the saves).</summary>
	public static string Root
	{
		get
		{
			string data = FileExplorer.GetUpperPath(DataReader.FilePath);
			return string.IsNullOrEmpty(data) ? null : FileExplorer.GetUpperPath(data);
		}
	}

	/// <summary>Writes a game file (see the class summary). Returns false when its bytes were
	/// already those, so nothing was written.</summary>
	public static bool Write(string path, byte[] data)
	{
		if (string.IsNullOrEmpty(path)) throw new ArgumentException("no path");
		path = Full(path);
		if (Snapshot)
		{
			string target = MapToSnapshot(path);
			Directory.CreateDirectory(Path.GetDirectoryName(target));
			if (File.Exists(target) && Same(File.ReadAllBytes(target), data)) return false;
			WriteAtomic(target, data);
			return true;
		}
		if (File.Exists(path) && Same(File.ReadAllBytes(path), data)) return false;
		Backup(path);
		WriteAtomic(path, data);
		RecordWritten(path, data);
		return true;
	}

	/// <summary>Writes a text file with Write.</summary>
	public static bool WriteText(string path, string text) { return Write(path, Encoding.ASCII.GetBytes(text ?? "")); }

	/// <summary>The .BKP original and a dated version of the file as it is now (nothing when it
	/// does not exist). Write calls it; call it yourself only before writing a file another way.</summary>
	public static void Backup(string path)
	{
		if (Snapshot) return;
		path = Full(path);
		if (!File.Exists(path)) return;
		string bkp = path + ".BKP";
		if (!File.Exists(bkp))
			File.Copy(path, bkp);
		else if (InSaveFolder(path) && ChangedSinceWritten(path))
			File.Copy(path, bkp, true);   // the game saved since the editor did
		KeepVersion(path);
	}

	// ------------------------------------------------------------- versions

	static void KeepVersion(string path)
	{
		int keep = Versions;
		string dir = BackupDir(path);
		if (keep <= 0 || dir == null) return;
		Directory.CreateDirectory(dir);
		string name = Path.GetFileName(path);
		string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
		string dest = Path.Combine(dir, name + "." + stamp);
		for (int n = 1; File.Exists(dest); n++) dest = Path.Combine(dir, name + "." + stamp + "-" + n);
		File.Copy(path, dest);
		Prune(dir, name + ".", keep);
	}

	/// <summary>Deletes the oldest files in dir whose names start with prefix, keeping keep.</summary>
	static void Prune(string dir, string prefix, int keep)
	{
		var list = new List<string>();
		foreach (var f in Directory.GetFiles(dir))
		{
			string n = Path.GetFileName(f);
			if (n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && n.Length > prefix.Length && char.IsDigit(n[prefix.Length])) list.Add(f);
		}
		list.Sort(StringComparer.Ordinal);   // the stamps sort by time
		for (int i = 0; i < list.Count - keep; i++)
			try { File.Delete(list[i]); } catch (Exception) { }
	}

	/// <summary>"Editor backups" with the file's folder under the game folder; null outside it.</summary>
	static string BackupDir(string path)
	{
		string rel = Relative(path);
		if (rel == null) return null;
		string relDir = Path.GetDirectoryName(rel);
		return Path.Combine(Path.Combine(Root, BackupsFolder), relDir ?? "");
	}

	// ------------------------------------------------------------- what the editor wrote

	static Dictionary<string, string> written;
	static string writtenRoot;

	static Dictionary<string, string> Written()
	{
		string root = Root;
		if (written != null && writtenRoot == root) return written;
		written = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		writtenRoot = root;
		if (root == null) return written;
		string f = Path.Combine(Path.Combine(root, BackupsFolder), WrittenFile);
		if (File.Exists(f))
			foreach (var line in File.ReadAllLines(f))
			{
				int tab = line.IndexOf('\t');
				if (tab > 0) written[line.Substring(0, tab)] = line.Substring(tab + 1);
			}
		return written;
	}

	static void RecordWritten(string path, byte[] data)
	{
		string rel = Relative(path);
		if (rel == null || !InSaveFolder(path)) return;
		var w = Written();
		w[rel] = Hash(data);
		try
		{
			string dir = Path.Combine(Root, BackupsFolder);
			Directory.CreateDirectory(dir);
			var lines = new List<string>();
			foreach (var kv in w) lines.Add(kv.Key + "\t" + kv.Value);
			File.WriteAllLines(Path.Combine(dir, WrittenFile), lines.ToArray());
		}
		catch (Exception e) { Debug.LogWarning("Could not record " + rel + ": " + e.Message); }
	}

	/// <summary>True when the editor wrote this file before and it has changed since.</summary>
	static bool ChangedSinceWritten(string path)
	{
		string rel = Relative(path), h;
		if (rel == null || !Written().TryGetValue(rel, out h)) return false;
		return h != Hash(File.ReadAllBytes(path));
	}

	static bool InSaveFolder(string path)
	{
		string dir = Path.GetFileName(Path.GetDirectoryName(path) ?? "");
		return GameSession.IsSaveFolderName(dir);
	}

	static string Hash(byte[] data)
	{
		using (var sha = SHA256.Create())
			return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
	}

	// ------------------------------------------------------------- autosave copies

	/// <summary>Makes an autosave copy: the game's DATA and CRIT folders, its executable and the
	/// open saved game are copied to "Editor autosaves/yyyy-MM-dd HH-mm-ss". Older copies beyond
	/// <see cref="Versions"/> (at least one) are deleted. Returns the copy's folder, to pass to
	/// <see cref="RunIn"/>.</summary>
	public static string NewSnapshot()
	{
		string root = Root;
		if (root == null) throw new InvalidOperationException("No game is open.");
		string all = Path.Combine(root, AutosavesFolder);
		string dir = Path.Combine(all, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
		for (int n = 1; Directory.Exists(dir); n++) dir = Path.Combine(all, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + " " + n);
		Directory.CreateDirectory(dir);
		foreach (var sub in Directory.GetDirectories(root))
		{
			string name = Path.GetFileName(sub).ToUpperInvariant();
			bool openSave = GameSession.SaveFolder != null && string.Equals(Full(sub).TrimEnd('/', '\\'), Full(GameSession.SaveFolder).TrimEnd('/', '\\'), StringComparison.OrdinalIgnoreCase);
			if (name == "DATA" || name == "CRIT" || openSave)
				CopyDir(sub, Path.Combine(dir, Path.GetFileName(sub)));
		}
		foreach (var f in Directory.GetFiles(root))
		{
			string name = Path.GetFileName(f).ToUpperInvariant();
			if (name == "UW.EXE" || name == "UW2.EXE") File.Copy(f, Path.Combine(dir, Path.GetFileName(f)));
		}
		PruneSnapshots(all, Math.Max(1, Versions));
		return dir;
	}

	/// <summary>Runs one step of a save with its writes going to the autosave copy made by
	/// <see cref="NewSnapshot"/>. Only for the length of the call: a tool window that saves
	/// while an autosave is between two steps writes the game's files as usual.</summary>
	public static void RunIn(string snapshot, Action step)
	{
		if (snapshot == null) { step(); return; }
		SnapshotRoot = snapshot;
		try { step(); }
		finally { SnapshotRoot = null; }
	}

	static void PruneSnapshots(string all, int keep)
	{
		var dirs = new List<string>(Directory.GetDirectories(all));
		dirs.Sort(StringComparer.Ordinal);
		for (int i = 0; i < dirs.Count - keep; i++)
			try { Directory.Delete(dirs[i], true); } catch (Exception) { }
	}

	static void CopyDir(string src, string dst)
	{
		Directory.CreateDirectory(dst);
		foreach (var f in Directory.GetFiles(src))
		{
			if (f.EndsWith(".BKP", StringComparison.OrdinalIgnoreCase)) continue;
			File.Copy(f, Path.Combine(dst, Path.GetFileName(f)));
		}
		foreach (var d in Directory.GetDirectories(src)) CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
	}

	static string MapToSnapshot(string path)
	{
		string rel = Relative(path);
		if (rel == null) throw new InvalidOperationException("An autosave can only write the game's own files: " + path);
		return Path.Combine(SnapshotRoot, rel);
	}

	// ------------------------------------------------------------- files

	/// <summary>Writes a temporary file beside path, then puts it in place.</summary>
	static void WriteAtomic(string path, byte[] data)
	{
		string tmp = path + ".uwedit-tmp";
		File.WriteAllBytes(tmp, data);
		if (File.Exists(path))
			File.Replace(tmp, path, null);
		else
			File.Move(tmp, path);
	}

	/// <summary>path relative to the game folder, or null when it is outside it.</summary>
	static string Relative(string path)
	{
		string root = Root;
		if (root == null) return null;
		root = Full(root).TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
		string full = Full(path);
		if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return null;
		return full.Substring(root.Length);
	}

	static string Full(string path) { return Path.GetFullPath(path.Replace('\\', '/')); }

	static bool Same(byte[] a, byte[] b)
	{
		if (a.Length != b.Length) return false;
		for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
		return true;
	}
}
