using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// UW1's wall and floor textures: W64.TR with its small copies W16.TR, and F32.TR with F16.TR.
/// Each .TR file is a header (type, picture size, count), an offset table and the pictures as
/// raw palette indices, size by size bytes each.
///
/// On saving, a pair of files is patched in place: a picture is written only when its texture
/// object was replaced (MapCreator.UpdateTexture), as UW2's T64.TR is (Uw2T64), so the
/// untouched pictures keep their bytes. The small copy is sampled from the new picture the
/// way the old writer did (DataWriter.ScaleDownTexture: every 4th or 2nd pixel from the bottom
/// left), and colours go through the editor's palette table (DataWriter.PaletteIndex), as
/// before. When textures were added or removed (MapCreator.AddTexture, RemoveTexture), the
/// count and offsets change, so the caller rewrites the whole pair instead.
/// </summary>
public static class Uw1TrFiles
{
	sealed class Pair
	{
		public string Name, SmallName, Path, SmallPath;
		public int Size, Scale;
		public byte[] Bytes, SmallBytes;
		public int[] Offsets, SmallOffsets;
		public Texture2D[] Original;
	}

	static Pair walls, floors;

	public static void Reset() { walls = null; floors = null; }

	/// <summary>Called at load (and after a whole-file rewrite) with the textures as read.</summary>
	public static void Remember(string dataDir, List<Texture2D> wallTex, List<Texture2D> floorTex)
	{
		walls = Read(dataDir, "W64.TR", "W16.TR", 64, 4, wallTex);
		floors = Read(dataDir, "F32.TR", "F16.TR", 32, 2, floorTex);
	}

	public static void RememberWalls(string dataDir, List<Texture2D> wallTex) { walls = Read(dataDir, "W64.TR", "W16.TR", 64, 4, wallTex); }
	public static void RememberFloors(string dataDir, List<Texture2D> floorTex) { floors = Read(dataDir, "F32.TR", "F16.TR", 32, 2, floorTex); }

	static Pair Read(string dataDir, string name, string small, int size, int scale, List<Texture2D> tex)
	{
		var p = new Pair { Name = name, SmallName = small, Size = size, Scale = scale, Original = tex.ToArray() };
		p.Path = UltimateEditor.Games.GameFiles.Resolve(dataDir + "/" + name);
		p.SmallPath = UltimateEditor.Games.GameFiles.Resolve(dataDir + "/" + small);
		if (!File.Exists(p.Path) || !File.Exists(p.SmallPath)) return null;
		p.Bytes = File.ReadAllBytes(p.Path);
		p.SmallBytes = File.ReadAllBytes(p.SmallPath);
		p.Offsets = Offsets(p.Bytes);
		p.SmallOffsets = Offsets(p.SmallBytes);
		return p;
	}

	/// <summary>The picture offsets of a .TR file (type byte, size byte, word count, long offsets).</summary>
	public static int[] Offsets(byte[] f)
	{
		if (f == null || f.Length < 4) return new int[0];
		int n = f[2] | f[3] << 8;
		var o = new int[n];
		for (int i = 0; i < n && 4 + i * 4 + 3 < f.Length; i++)
			o[i] = f[4 + i * 4] | f[5 + i * 4] << 8 | f[6 + i * 4] << 16 | f[7 + i * 4] << 24;
		return o;
	}

	/// <summary>Whether the walls (or floors) can be patched: the files were read and the
	/// texture count is still the file's. Otherwise the caller rewrites the pair.</summary>
	public static bool CanPatch(bool wallsPair, TextureContainer c)
	{
		var p = wallsPair ? walls : floors;
		return p != null && c != null && c.Count == p.Original.Length && p.Offsets.Length >= c.Count && p.SmallOffsets.Length >= c.Count;
	}

	/// <summary>Patches the replaced pictures of the walls (or floors) into their two files,
	/// through GameWrite. Returns the pictures written; problems go to report (and
	/// GrPictures.Problems, for the message after saving).</summary>
	public static int Save(bool wallsPair, TextureContainer c, List<string> report)
	{
		var p = wallsPair ? walls : floors;
		if (!CanPatch(wallsPair, c)) return 0;
		int written = 0;
		byte[] big = null, small = null;
		var done = new List<int>();
		for (int i = 0; i < c.Count; i++)
		{
			Texture2D tex = c.Textures[i];
			if (tex == null || ReferenceEquals(tex, p.Original[i])) continue;
			if (tex.width != p.Size || tex.height != p.Size)
			{
				GrPictures.Warn(report, p.Name + ": picture " + i + " is " + tex.width + "x" + tex.height + ", not " + p.Size + "x" + p.Size + "; not written");
				continue;
			}
			int at = p.Offsets[i], atSmall = p.SmallOffsets[i];
			int s = p.Size / p.Scale;
			if (at < 0 || at + p.Size * p.Size > p.Bytes.Length || atSmall < 0 || atSmall + s * s > p.SmallBytes.Length)
			{
				GrPictures.Warn(report, p.Name + ": picture " + i + " has no room in the file; not written");
				continue;
			}
			int[] raw = DataWriter.ToPaletteIndexes(tex);
			if (raw == null)
			{
				GrPictures.Warn(report, p.Name + ": the palette table (StreamingAssets/mapping.dat) is missing; not written");
				return written;
			}
			if (big == null) { big = (byte[])p.Bytes.Clone(); small = (byte[])p.SmallBytes.Clone(); }
			for (int k = 0; k < raw.Length; k++) big[at + k] = (byte)raw[k];
			int[] rawSmall = SampleDown(raw, p.Size, p.Scale);
			for (int k = 0; k < rawSmall.Length; k++) small[atSmall + k] = (byte)rawSmall[k];
			done.Add(i);
			written++;
		}
		if (written == 0) return 0;
		if (!Same(big, p.Bytes)) GameWrite.Write(p.Path, big);
		if (!Same(small, p.SmallBytes)) GameWrite.Write(p.SmallPath, small);
		report.Add(p.Name + " and " + p.SmallName + ": " + written + " picture(s) written");
		if (GameWrite.Snapshot) return written;   // an autosave copy: the game's own files are unchanged
		p.Bytes = big;
		p.SmallBytes = small;
		foreach (int i in done) p.Original[i] = c.Textures[i];
		return written;
	}

	/// <summary>The small copy of a picture (raw, top row first), as DataWriter.ScaleDownTexture
	/// and ConvertToRawTexture made it: the pixel at (x * scale, y * scale) counting from the
	/// bottom left of the picture.</summary>
	public static int[] SampleDown(int[] raw, int size, int scale)
	{
		int s = size / scale;
		var o = new int[s * s];
		for (int y = 0; y < s; y++)
			for (int x = 0; x < s; x++)
				o[(s - 1 - y) * s + x] = raw[(size - 1 - y * scale) * size + x * scale];
		return o;
	}

	static bool Same(byte[] a, byte[] b)
	{
		if (a == null || b == null || a.Length != b.Length) return false;
		for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
		return true;
	}
}
