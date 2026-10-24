using System;
using System.Collections.Generic;
using System.IO;
using UltimateEditor.Games;

/// <summary>
/// CNV.ARK for both games. The editor's conversation reader and writer speak UW1's archive
/// format (a word count, then long offsets); this class hands them that format in memory
/// and, on saving, writes back through the game's own archive format only the
/// conversations whose bytes changed, so the rest stay as they were (UW2's compressed
/// blocks included).
/// </summary>
public static class CnvArk
{
	static Archive archive;
	static string path;

	/// <summary>The archive as a UW1-format image (UW2's blocks decompressed), for DataReader.LoadConversations.</summary>
	public static byte[] LoadAsUw1Image(string cnvPath)
	{
		path = cnvPath;
		var kind = LevArk.Game.Kind;
		archive = Archive.Load(cnvPath, kind);
		if (kind == GameKind.UW1) return archive.ToArray();
		int n = archive.Count;
		var ms = new MemoryStream();
		ms.WriteByte((byte)n); ms.WriteByte((byte)(n >> 8));
		ms.Write(new byte[4 * n], 0, 4 * n);
		var offsets = new uint[n];
		for (int b = 0; b < n; b++)
		{
			var data = archive.Read(b);
			if (data == null || data.Length == 0) continue;
			offsets[b] = (uint)ms.Length;
			ms.Write(data, 0, data.Length);
		}
		var img = ms.ToArray();
		for (int b = 0; b < n; b++)
		{
			img[2 + 4 * b] = (byte)offsets[b]; img[3 + 4 * b] = (byte)(offsets[b] >> 8);
			img[4 + 4 * b] = (byte)(offsets[b] >> 16); img[5 + 4 * b] = (byte)(offsets[b] >> 24);
		}
		return img;
	}

	/// <summary>Slots the editor read a conversation from (DataReader.LoadConversations).</summary>
	public static readonly HashSet<int> Loaded = new HashSet<int>();

	/// <summary>Blocks the last save wrote or removed.</summary>
	public static readonly List<int> LastWritten = new List<int>();

	/// <summary>
	/// Takes what the editor's writer produced (UW1 format) and writes each changed block
	/// through the game's archive; returns the bytes to store as CNV.ARK.
	/// </summary>
	public static byte[] Merge(byte[] uw1Image)
	{
		LastWritten.Clear();
		if (archive == null) return uw1Image;
		var written = new Uw1Archive(uw1Image);
		int n = Math.Min(written.Count, archive.Count);
		for (int b = 0; b < n; b++)
		{
			var now = written.Read(b);
			var old = archive.Read(b);
			if (now == null && old == null) continue;
			if (now == null)
			{
				// Deleted or cleared in the editor (UIManager's delete list, ClearConversations):
				// remove the block, as the old writer did. A slot the editor could not read
				// (absent from Loaded) is kept.
				if (old != null && Loaded.Contains(b)) { archive.Delete(b); LastWritten.Add(b); }
				continue;
			}
			if (old != null && Same(old, now)) continue;
			archive.Write(b, now);
			LastWritten.Add(b);
			Loaded.Add(b);   // a block written now is the editor's, so a later delete removes it
		}
		return archive.ToArray();
	}

	static bool Same(byte[] a, byte[] b)
	{
		if (a.Length != b.Length) return false;
		for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
		return true;
	}
}
