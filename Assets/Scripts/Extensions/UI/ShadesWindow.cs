using System;
using System.Collections.Generic;
using System.IO;
using UltimateEditor.Games;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Edits DATA\SHADES.DAT, the eight light levels (Core/Uw1SmallFiles.cs ShadesDat), both
	/// games, with a preview drawn from the game's own data: a wall and a floor texture at
	/// each distance, each pixel's colour index passed through LIGHT.DAT's map for that
	/// distance's shade (MONO.DAT at level 5) and then palette 0, as the renderer does; and the
	/// view grid ahead of the player, shaded the same way. Squares beyond the view radius are black.
	/// </summary>
	public static class ShadesWindow
	{
		const int Swatch = 40, GridCell = 9;

		sealed class State
		{
			public ShadesDat Shades;
			public int Row = 2;
			public byte[] Light, Mono;
			public Color[] Palette;
			public int[] WallRaw, FloorRaw;   // 32x32 colour indexes
			public Texture2D Strip, Grid;
			public Text Info;
			public Window W;
		}

		public static void Show()
		{
			string path = ExtensionsMenu.FindDataFile("SHADES.DAT");
			if (path == null) throw new FileNotFoundException("SHADES.DAT is not in the DATA folder");
			var st = new State { Shades = ShadesDat.Parse(File.ReadAllBytes(path)) };
			string light = ExtensionsMenu.FindDataFile("LIGHT.DAT"), mono = ExtensionsMenu.FindDataFile("MONO.DAT");
			if (light != null) st.Light = File.ReadAllBytes(light);
			if (mono != null) st.Mono = File.ReadAllBytes(mono);
			var td = MapCreator.TextureData;
			if (td != null && td.Palettes != null && td.Palettes.Count > 0) st.Palette = td.Palettes[0];
			PickTextures(st);

			var w = st.W = UiKit.Window("Light levels (SHADES.DAT)", 900, 700);
			var tools = UiKit.Row(w.Toolbar, 22);
			UiKit.Button(tools, "Save SHADES.DAT", () =>
			{
				ExtensionsMenu.WriteDataFile(Path.GetFileName(path), st.Shades.ToBytes());
				w.Status.text = "Saved. The game reads SHADES.DAT each time the light level changes.";
			}, 130, 22, UiKit.AccentColor);
			UiKit.Button(tools, "Reload", () => { w.Close(); Show(); }, 70, 22);
			var names = new List<string>(ShadesDat.LevelNames);
			UiKit.Label(tools, "Preview", 11, FontStyle.Normal, UiKit.DimTextColor, 50);
			UiKit.Dropdown(tools, names, st.Row, i => { st.Row = i; Redraw(st); }, 260);
			UiKit.Paragraph(w.Toolbar,
				"Each light level is one row. A square's shade (0 brightest to 14 darkest, 15 not drawn) comes from its distance: " +
				"min(14, max(0, isqrt(128 d²) × smooth_div / 64 + smooth_lowpass) + smooth_base), and 15 beyond the view radius (map/LIGHTING.C, 3d/VIEW3D.C preset_grid; the same in both games)." +
				(LevArk.Game.Kind == GameKind.UW2 ? " UW2 also gives some levels a minimum light level (DL.DAT)." : ""), 10);

			// the table
			var head = UiKit.Row(w.Content, 18);
			UiKit.Label(head, "light level", 10, FontStyle.Bold, UiKit.DimTextColor, 200);
			for (int c = 0; c < 6; c++)
			{
				var l = UiKit.Label(head, ShadesDat.Columns[c], 10, FontStyle.Bold, UiKit.DimTextColor, 90);
				UiKit.Tooltip(l.gameObject, ShadesDat.ColumnHelp[c]);
			}
			for (int r = 0; r < 8; r++)
			{
				int row = r;
				var line = UiKit.Row(w.Content, 20, 4);
				UiKit.Button(line, ShadesDat.LevelNames[r], () => { st.Row = row; Redraw(st); }, 200, 20);
				for (int c = 0; c < 6; c++)
				{
					int col = c;
					var input = UiKit.Input(line, st.Shades.Rows[r, c].ToString(), s =>
					{
						short v;
						if (short.TryParse(s, out v)) { st.Shades.Rows[row, col] = v; st.Row = row; Redraw(st); }
						else w.Status.text = "Values are 16-bit numbers.";
					}, 90);
					UiKit.Tooltip(input.gameObject, ShadesDat.ColumnHelp[c]);
				}
			}

			// the preview
			UiKit.Label(w.Content, "Preview: wall (top) and floor (bottom) at distance 0-15, then the view grid ahead (you are at the bottom middle)", 11, FontStyle.Bold);
			var stripRow = UiKit.Row(w.Content, Swatch * 2, 0);
			var strip = UiKit.Rect("Strip", stripRow);
			UiKit.Size(strip.gameObject, Swatch * 16, Swatch * 2);
			st.Strip = new Texture2D(32 * 16, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
			strip.gameObject.AddComponent<RawImage>().texture = st.Strip;
			var gridRow = UiKit.Row(w.Content, 17 * GridCell, 8);
			var grid = UiKit.Rect("Grid", gridRow);
			UiKit.Size(grid.gameObject, 33 * GridCell, 17 * GridCell);
			st.Grid = new Texture2D(33, 17, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
			grid.gameObject.AddComponent<RawImage>().texture = st.Grid;
			st.Info = UiKit.Paragraph(gridRow, "", 11, UiKit.TextColor);
			UiKit.Size(st.Info.gameObject, 420, -1);
			Redraw(st);
		}

		/// <summary>Takes the first level's first wall and floor textures as colour indexes of palette 0.</summary>
		static void PickTextures(State st)
		{
			var td = MapCreator.TextureData;
			if (td == null || st.Palette == null) return;
			var lev = MapCreator.LevelData != null && MapCreator.LevelData.Count > 0 ? MapCreator.LevelData[0] : null;
			int wi = lev != null && lev.WallTextures.Length > 0 ? lev.WallTextures[0] : 0;
			int fi = lev != null && lev.FloorTextures.Length > 0 ? lev.FloorTextures[0] : 0;
			if (wi < td.Walls.Count) st.WallRaw = ToIndexes(td.Walls.Textures[wi], st.Palette);
			if (fi < td.Floors.Count) st.FloorRaw = ToIndexes(td.Floors.Textures[fi], st.Palette);
		}

		static int[] ToIndexes(Texture2D tex, Color[] pal)
		{
			var lookup = new Dictionary<Color32, int>();
			for (int i = pal.Length - 1; i >= 0; i--) lookup[(Color32)pal[i]] = i;
			var r = new int[32 * 32];
			for (int y = 0; y < 32; y++)
				for (int x = 0; x < 32; x++)
				{
					Color32 c = tex.GetPixel(x * tex.width / 32, y * tex.height / 32);
					c.a = 255;
					int idx;
					r[y * 32 + x] = lookup.TryGetValue(c, out idx) ? idx : Nearest(c, pal);
				}
			return r;
		}

		static int Nearest(Color32 c, Color[] pal)
		{
			int best = 0, bd = int.MaxValue;
			for (int i = 0; i < pal.Length; i++)
			{
				Color32 p = pal[i];
				int d = (p.r - c.r) * (p.r - c.r) + (p.g - c.g) * (p.g - c.g) + (p.b - c.b) * (p.b - c.b);
				if (d < bd) { bd = d; best = i; }
			}
			return best;
		}

		/// <summary>A colour index seen at shade s: LIGHT.DAT (MONO.DAT at level 5) map s, then palette 0.</summary>
		static Color Lit(State st, int index, int shade)
		{
			if (shade >= 15 || st.Palette == null) return Color.black;
			var maps = st.Row == 5 && st.Mono != null ? st.Mono : st.Light;
			int i = maps != null && shade * 256 + index < maps.Length ? maps[shade * 256 + index] : index;
			return i < st.Palette.Length ? st.Palette[i] : Color.black;
		}

		static void Redraw(State st)
		{
			var shades = st.Shades.ShadeByDistance(st.Row);
			for (int d = 0; d < 16; d++)
				for (int y = 0; y < 32; y++)
					for (int x = 0; x < 32; x++)
					{
						Color top = st.WallRaw != null ? Lit(st, st.WallRaw[y * 32 + x], shades[d]) : Gray(shades[d]);
						Color bottom = st.FloorRaw != null ? Lit(st, st.FloorRaw[y * 32 + x], shades[d]) : Gray(shades[d]);
						st.Strip.SetPixel(d * 32 + x, 32 + y, top);
						st.Strip.SetPixel(d * 32 + x, y, bottom);
					}
			st.Strip.Apply();
			int sample = st.FloorRaw != null ? st.FloorRaw[16 * 32 + 16] : -1;
			for (int j = 0; j < 17; j++)
				for (int i = 0; i < 33; i++)
				{
					int s = st.Shades.CellShade(st.Row, 16 - i, j);
					st.Grid.SetPixel(i, j, sample >= 0 ? Lit(st, sample, s) : Gray(s));
				}
			st.Grid.Apply();
			var sb = new System.Text.StringBuilder();
			sb.Append(ShadesDat.LevelNames[st.Row]).Append("\nShade by distance: ");
			for (int d = 0; d < 16; d++) sb.Append(shades[d] == 15 ? "-" : shades[d].ToString()).Append(d < 15 ? " " : "");
			sb.AppendFormat("\nTextured up to shade {0}, flat after; smaller textures from shade {1}.", st.Shades.Rows[st.Row, 4] - 1, st.Shades.Rows[st.Row, 5]);
			string warn = st.Shades.Check(st.Row);
			if (warn.Length > 0) sb.Append("\n").Append(warn);
			if (st.Light == null) sb.Append("\nLIGHT.DAT not found: grey shading shown.");
			st.Info.text = sb.ToString();
		}

		static Color Gray(int shade) { float v = shade >= 15 ? 0 : 1f - shade / 15f; return new Color(v, v, v); }
	}
}
