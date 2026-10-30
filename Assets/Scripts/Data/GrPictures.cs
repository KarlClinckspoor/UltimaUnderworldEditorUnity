using System;
using System.Collections.Generic;
using System.IO;
using UltimateEditor.Games;
using UnityEngine;

/// <summary>
/// Saves the .GR pictures of both games: DOORS.GR, TMFLAT.GR (levers and switches), TMOBJ.GR
/// (wall decals), GENHEAD.GR and CHARHEAD.GR (portraits) and OBJECTS.GR. A file is rebuilt
/// with only the replaced pictures re-encoded (GrFile.Rebuild), so everything else keeps its
/// bytes and empty pictures stay empty; a file with no replaced picture is not written at all.
/// A picture counts as replaced when its texture object was swapped (MapCreator.UpdateTexture),
/// as for the textures (Uw2T64, Uw1TrFiles).
///
/// UW1 can also add and remove doors and character heads (MapCreator.AddTexture,
/// RemoveTexture), which changes the file's list: DataWriter rewrites such a file whole and
/// calls Remember again (CanPatch says when). 8-bit pictures map colours through
/// <see cref="ColorToIndex"/> when it is set (UW1: the editor's palette table, as UW1's old
/// writer did), otherwise to the palette's own colour or the nearest one (UW2).
/// </summary>
public static class GrPictures
{
	sealed class Tracked
	{
		public string Path;
		public byte[] Bytes;
		public Texture2D[] Original;
	}

	static readonly Dictionary<string, Tracked> files = new Dictionary<string, Tracked>();
	static Color[] palette;

	/// <summary>Palette index of an opaque colour for 8-bit pictures, or null for the palette's
	/// own colour or the nearest one.</summary>
	public static Func<Color32, int> ColorToIndex;

	/// <summary>The .GR files this class writes, with the texture list each is shown as.</summary>
	public static KeyValuePair<string, TextureContainer>[] Files(TextureData t)
	{
		return new[]
		{
			new KeyValuePair<string, TextureContainer>("DOORS.GR", t.Doors),
			new KeyValuePair<string, TextureContainer>("TMFLAT.GR", t.Levers),
			new KeyValuePair<string, TextureContainer>("TMOBJ.GR", t.Other),
			new KeyValuePair<string, TextureContainer>("GENHEAD.GR", t.GenHeads),
			new KeyValuePair<string, TextureContainer>("CHARHEAD.GR", t.NPCHeads),
			new KeyValuePair<string, TextureContainer>("OBJECTS.GR", t.Objects),
		};
	}

	/// <summary>Whether the file can be patched: it was read, and its list has the length it
	/// had then (no picture added or removed).</summary>
	public static bool CanPatch(string fileName, TextureContainer c)
	{
		Tracked tr;
		return c != null && files.TryGetValue(fileName, out tr) && tr.Bytes != null && c.Count == tr.Original.Length;
	}

	/// <summary>What the last Save could not write, for the message after saving.</summary>
	public static readonly List<string> Problems = new List<string>();

	public static void Reset()
	{
		files.Clear();
		Problems.Clear();
		palette = null;
		ColorToIndex = null;
	}

	/// <summary>Called at load with the textures as read and the main palette.</summary>
	public static void Remember(string fileName, string path, List<Texture2D> textures, Color[] pal)
	{
		palette = pal;
		files[fileName] = new Tracked
		{
			Path = path,
			Bytes = File.Exists(path) ? File.ReadAllBytes(path) : null,
			Original = textures.ToArray(),
		};
	}

	/// <summary>Writes the files that have replaced pictures. backup is called with a file
	/// name before that file is written; skip names files the caller writes itself. Returns
	/// one line per file written or problem found. Problems is not cleared here: the caller
	/// clears it once per save (DataWriter.SaveTextureData).</summary>
	public static List<string> Save(TextureData t, ICollection<string> skip = null)
	{
		var report = new List<string>();
		// one file failing (unreadable, in use) must not stop the rest of the save
		foreach (var f in Files(t))
		{
			if (skip != null && skip.Contains(f.Key)) continue;
			try { SaveFile(f.Key, f.Value, f.Key == "OBJECTS.GR", report); }
			catch (Exception e) { Warn(report, f.Key + ": not written (" + e.Message + ")"); }
		}
		return report;
	}

	static void SaveFile(string name, TextureContainer c, bool objects, List<string> report)
	{
		Tracked tr;
		if (c == null || !files.TryGetValue(name, out tr) || tr.Bytes == null) return;
		int[] o = GrFile.Offsets(tr.Bytes);
		int n = o.Length - 1;
		if (c.Count > tr.Original.Length)
			Warn(report, name + ": pictures added after the first " + tr.Original.Length + " are not written (the game's file has a fixed list)");
		if (c.Count < tr.Original.Length)
			Warn(report, name + ": " + (tr.Original.Length - c.Count) + " removed picture(s) stay in the file (the game's file has a fixed list)");
		var replace = new Dictionary<int, GrFile.Picture>();
		int limit = Math.Min(c.Count, Math.Min(tr.Original.Length, n));
		for (int i = 0; i < limit; i++)
		{
			Texture2D tex = c.Textures[i];
			if (ReferenceEquals(tex, tr.Original[i]) || tex == null) continue;
			GrFile.Picture old = GrFile.Decode(tr.Bytes, o, i);   // null: the slot was empty
			int type = old != null ? old.Type : (objects ? 8 : 4);
			if (old != null && (tex.width != old.Width || tex.height != old.Height))
			{
				Warn(report, name + ": picture " + i + " is " + tex.width + "x" + tex.height + ", the game's is " + old.Width + "x" + old.Height + "; not written");
				continue;
			}
			var p = new GrFile.Picture { Type = type, Width = tex.width, Height = tex.height };
			if (type == 4)
				p.Pixels = ToIndexes(tex);
			else
			{
				// 4-bit pictures (objects): the auxiliary palette indices the converter made
				if (!objects || c.RawTextures == null || c.AuxPalettes == null || i >= c.RawTextures.Count || i >= c.AuxPalettes.Count
					|| c.RawTextures[i] == null || c.RawTextures[i].Length != tex.width * tex.height)
				{
					Warn(report, name + ": picture " + i + " has no 4-bit pixels to write; not written");
					continue;
				}
				p.Aux = c.AuxPalettes[i];
				p.Pixels = (int[])c.RawTextures[i].Clone();
			}
			replace[i] = p;
		}
		if (replace.Count == 0) return;
		byte[] now = GrFile.Rebuild(tr.Bytes, replace);
		GameWrite.Write(tr.Path, now);
		report.Add(name + ": " + replace.Count + " picture(s) written");
		if (GameWrite.Snapshot) return;   // an autosave copy: the game's own file is unchanged
		tr.Bytes = now;
		// only the pictures written count as saved; skipped ones are reported again next time
		foreach (var i in replace.Keys) tr.Original[i] = c.Textures[i];
	}

	public static void Warn(List<string> report, string line)
	{
		report.Add(line);
		Problems.Add(line);
	}

	/// <summary>Palette indices, top row first: the inverse of DataReader.ConvertToTexture(...,
	/// reverse: true). A transparent pixel is 0; a colour maps to its palette entry, or the
	/// nearest one when the palette does not have it.</summary>
	static int[] ToIndexes(Texture2D tex)
	{
		int w = tex.width, h = tex.height;
		var raw = new int[w * h];
		var lookup = new Dictionary<Color32, int>();
		for (int i = palette.Length - 1; i >= 1; i--) lookup[(Color32)palette[i]] = i;
		Color32[] px = tex.GetPixels32();   // bottom row first
		var map = ColorToIndex;
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				Color32 c = px[x + (h - 1 - y) * w];
				int k = c.a == 0 ? 0 : map != null ? map(c) : -1;
				raw[x + y * w] = k >= 0 ? k : Nearest(c, lookup);   // no table: the nearest colour
			}
		return raw;
	}

	static int Nearest(Color32 c, Dictionary<Color32, int> lookup)
	{
		var opaque = new Color32(c.r, c.g, c.b, 255);
		int idx;
		if (lookup.TryGetValue(opaque, out idx)) return idx;
		int best = 1, bestD = int.MaxValue;
		for (int i = 1; i < palette.Length; i++)
		{
			Color32 p = palette[i];
			int dr = p.r - c.r, dg = p.g - c.g, db = p.b - c.b;
			int d = dr * dr * 3 + dg * dg * 4 + db * db * 2;
			if (d < bestD) { bestD = d; best = i; }
		}
		lookup[opaque] = best;
		return best;
	}
}
