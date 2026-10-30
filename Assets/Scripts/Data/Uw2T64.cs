using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// UW2's T64.TR, the one texture list for walls and floors (256 pictures of 64x64, an offset
/// table of 264). The editor shows the pictures as Walls (64x64) and, scaled down, as Floors
/// (32x32). On saving, the file is patched in place: a picture is written only when its
/// texture object was replaced (MapCreator.UpdateTexture), from the wall if that changed and
/// otherwise from the floor (scaled up). Pixels map to palette 0 exactly when the colour is
/// in it and to the nearest colour otherwise; index 0 is the transparent colour.
/// </summary>
public static class Uw2T64
{
	public static string Path;
	static byte[] file;
	static int[] offsets;
	static Texture2D[] walls, floors;
	static Color[] palette;

	/// <summary>Forgets the open file (opening another game without restarting).</summary>
	public static void Reset()
	{
		Path = null; file = null; offsets = null; walls = null; floors = null; palette = null;
	}

	public static void Remember(string path, int[] offs, List<Texture2D> wallTex, List<Texture2D> floorTex, Color[] pal)
	{
		Path = path;
		file = File.Exists(path) ? File.ReadAllBytes(path) : null;
		offsets = offs;
		walls = wallTex.ToArray();
		floors = floorTex.ToArray();
		palette = pal;
	}

	/// <summary>The file as written, and the pictures written (changed): only those count as
	/// saved, so one skipped now (wrong size) is tried and reported again next time.</summary>
	public static void Saved(byte[] now, TextureContainer w, TextureContainer f, List<int> changed)
	{
		file = now;
		foreach (int i in changed)
		{
			if (i < walls.Length && i < w.Count) walls[i] = w.Textures[i];
			if (i < floors.Length && i < f.Count) floors[i] = f.Textures[i];
		}
	}

	/// <summary>The new file, or null when nothing was remembered; changed lists the pictures written.</summary>
	public static byte[] Build(TextureContainer w, TextureContainer f, out List<int> changed, out List<string> warnings)
	{
		changed = new List<int>();
		warnings = new List<string>();
		if (file == null || offsets == null) return null;
		var outFile = (byte[])file.Clone();
		int n = System.Math.Min(walls.Length, offsets.Length);
		if (w.Count > walls.Length || f.Count > floors.Length)
			warnings.Add("textures added after the first " + walls.Length + " are not written (T64.TR holds a fixed list)");
		var lookup = new Dictionary<Color32, int>();
		for (int i = palette.Length - 1; i >= 1; i--) lookup[(Color32)palette[i]] = i;
		for (int i = 0; i < n; i++)
		{
			Texture2D src = null; int size = 64;
			if (i < w.Count && !ReferenceEquals(w.Textures[i], walls[i])) src = w.Textures[i];
			else if (i < f.Count && !ReferenceEquals(f.Textures[i], floors[i])) { src = f.Textures[i]; size = 32; }
			if (src == null) continue;
			if (src.width != size || src.height != size)
			{
				warnings.Add("picture " + i + " is " + src.width + "x" + src.height + ", not " + size + "x" + size + "; skipped");
				continue;
			}
			var raw = ToRaw(src, size, lookup);
			if (size == 32) raw = Upscale(raw, 32);
			int at = offsets[i];
			if (at < 0 || at + 4096 > outFile.Length) { warnings.Add("picture " + i + " has no room in the file; skipped"); continue; }
			bool diff = false;
			for (int k = 0; k < 4096; k++) if (outFile[at + k] != (byte)raw[k]) { diff = true; break; }
			if (!diff) continue;
			for (int k = 0; k < 4096; k++) outFile[at + k] = (byte)raw[k];
			changed.Add(i);
		}
		return outFile;
	}

	/// <summary>The inverse of DataReader.ConvertToTexture(..., reverse: true).</summary>
	static int[] ToRaw(Texture2D tex, int size, Dictionary<Color32, int> lookup)
	{
		var raw = new int[size * size];
		for (int y = 0; y < size; y++)
			for (int x = 0; x < size; x++)
			{
				Color32 c = tex.GetPixel(x, size - y - 1);
				raw[x + y * size] = c.a == 0 ? 0 : Nearest(c, lookup);
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

	static int[] Upscale(int[] raw, int size)
	{
		int s2 = size * 2;
		var r = new int[s2 * s2];
		for (int y = 0; y < s2; y++)
			for (int x = 0; x < s2; x++)
				r[x + y * s2] = raw[x / 2 + (y / 2) * size];
		return r;
	}
}
