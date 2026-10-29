using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Shows the 3D models built into UW.EXE (see <see cref="Uw1ExeModels"/>), drawn by a
	/// small software renderer into a texture so it needs no camera, layer or shader set-up.
	/// Faces use the model's palette colours (palette 0 of PALS.DAT); textured faces are drawn
	/// grey. Concave faces are filled through their triangles and hidden surfaces are removed
	/// per pixel (see RenderPixels). Drag the picture or use the buttons to turn it; Export
	/// writes a Wavefront OBJ.
	/// </summary>
	public static class ModelViewerWindow
	{
		public const int Size = 320;

		public static void Show()
		{
			string data = ExtensionsMenu.DataFolder;
			string exe = FindExe(data);
			if (exe == null) throw new FileNotFoundException("UW.EXE or UW2.EXE was not found next to the DATA folder");
			var models = Uw1ExeModels.Load(File.ReadAllBytes(exe));
			Color32[] palette = ReadPalette(ExtensionsMenu.FindDataFile("PALS.DAT"));

			var w = UiKit.Window("Built-in 3D models (" + Path.GetFileName(exe).ToUpperInvariant() + ")", 560, 520);
			var state = new State { Models = models, Palette = palette, Yaw = 35, Pitch = 25, Zoom = 1f };
			var top = UiKit.Row(w.Toolbar, 22);
			var names = new List<string>();
			foreach (var m in models.Models)
				names.Add(string.Format("{0:00} {1} ({2} faces)", m.Index, m.Name, m.Faces.Count));
			int first = models.Models.FindIndex(m => m.Faces.Count > 0);
			state.Index = Math.Max(0, first);

			// In a row: the content's layout force-expands widths, which would stretch the square.
			var pictureRow = UiKit.Row(w.Content, Size);
			var picture = UiKit.Rect("Picture", pictureRow);
			UiKit.Size(picture.gameObject, Size, Size);
			var raw = picture.gameObject.AddComponent<RawImage>();
			state.Texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
			raw.texture = state.Texture;
			var drag = picture.gameObject.AddComponent<ModelDrag>();
			var info = UiKit.Paragraph(w.Content, "", 11, UiKit.TextColor);

			Action redraw = () =>
			{
				var m = models.Models[state.Index];
				Render(state, m);
				info.text = string.Format("Model {0}: {1}. {2} vertices, {3} faces. Extents {4:0.##} x {5:0.##} x {6:0.##}. Data at file offset 0x{7:X}.{8}",
					m.Index, m.Name, m.Vertices.FindAll(v => v != null).Count, m.Faces.Count, m.ExtentX, m.ExtentY, m.ExtentZ, m.FileOffset,
					m.Error != null ? "\nDecoding stopped: " + m.Error : "");
			};
			drag.OnDragged = d => { state.Yaw += d.x * 0.6f; state.Pitch = Mathf.Clamp(state.Pitch - d.y * 0.6f, -89, 89); redraw(); };

			UiKit.Dropdown(top, names, state.Index, i => { state.Index = i; redraw(); }, 260);
			UiKit.Button(top, "<", () => { state.Yaw -= 15; redraw(); }, 24);
			UiKit.Button(top, ">", () => { state.Yaw += 15; redraw(); }, 24);
			UiKit.Button(top, "^", () => { state.Pitch = Mathf.Min(89, state.Pitch + 15); redraw(); }, 24);
			UiKit.Button(top, "v", () => { state.Pitch = Mathf.Max(-89, state.Pitch - 15); redraw(); }, 24);
			UiKit.Button(top, "+", () => { state.Zoom *= 1.25f; redraw(); }, 24);
			UiKit.Button(top, "-", () => { state.Zoom /= 1.25f; redraw(); }, 24);
			UiKit.Button(top, "Export OBJ", () =>
			{
				var m = models.Models[state.Index];
				string dir = Path.Combine(Path.GetDirectoryName(exe), "UW1Models");
				Directory.CreateDirectory(dir);
				string file = Path.Combine(dir, string.Format("{0:00}_{1}.obj", m.Index, Sanitize(m.Name)));
				File.WriteAllText(file, Uw1ExeModels.ToObj(m));
				w.Status.text = "Written " + file;
			}, 80);
			w.Status.text = string.Format("Model table at 0x{0:X}, data base 0x{1:X}, colour table at 0x{2:X} in {3}", models.TableOffset, models.BaseOffset, models.ColorTableOffset, Path.GetFileName(exe));
			redraw();
		}

		static string Sanitize(string s)
		{
			foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
			return s.Replace(' ', '_');
		}

		static string FindExe(string dataFolder)
		{
			if (dataFolder == null) return null;
			var parent = Directory.GetParent(dataFolder);
			if (parent == null) return null;
			// the game being edited decides which executable holds the models
			string want = LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW2 ? "UW2.EXE" : "UW.EXE";
			foreach (var f in Directory.GetFiles(parent.FullName))
				if (string.Equals(Path.GetFileName(f), want, StringComparison.OrdinalIgnoreCase))
					return f;
			return null;
		}

		static Color32[] ReadPalette(string pals)
		{
			var p = new Color32[256];
			if (pals != null && File.Exists(pals))
			{
				var d = File.ReadAllBytes(pals);
				for (int i = 0; i < 256 && i * 3 + 2 < d.Length; i++)
					p[i] = new Color32((byte)(d[i * 3] * 4), (byte)(d[i * 3 + 1] * 4), (byte)(d[i * 3 + 2] * 4), 255);
			}
			else
				for (int i = 0; i < 256; i++) p[i] = new Color32((byte)i, (byte)i, (byte)i, 255);
			return p;
		}

		sealed class State
		{
			public Uw1ExeModels Models;
			public Color32[] Palette;
			public Texture2D Texture;
			public int Index;
			public float Yaw, Pitch, Zoom;
		}

		static void Render(State s, Uw1ExeModels.Model m)
		{
			Upload(s.Texture, RenderPixels(m, s.Palette, s.Yaw, s.Pitch, s.Zoom));
		}

		/// <summary>
		/// Draws the model into Size x Size pixels, top row first. Each face is filled through
		/// its triangles (Uw1ExeModels.Face.Triangles), which follow concave outlines, against a
		/// depth buffer, so faces hide one another per pixel whatever their order; then its outline
		/// is drawn where it is not hidden. Faces are lit by how squarely they face the viewer.
		/// </summary>
		public static Color32[] RenderPixels(Uw1ExeModels.Model m, Color32[] palette, float yawDegrees, float pitchDegrees, float zoom)
		{
			var px = new Color32[Size * Size];
			var bg = new Color32(24, 26, 30, 255);
			for (int i = 0; i < px.Length; i++) px[i] = bg;

			// Bounding box of the defined vertices, to centre and scale the model.
			float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
			foreach (var v in m.Vertices)
			{
				if (v == null) continue;
				minX = Math.Min(minX, v[0]); maxX = Math.Max(maxX, v[0]);
				minY = Math.Min(minY, v[1]); maxY = Math.Max(maxY, v[1]);
				minZ = Math.Min(minZ, v[2]); maxZ = Math.Max(maxZ, v[2]);
			}
			if (minX == float.MaxValue) return px;
			float cx = (minX + maxX) / 2, cy = (minY + maxY) / 2, cz = (minZ + maxZ) / 2;
			float radius = Math.Max(1e-3f, (float)Math.Sqrt((maxX - minX) * (maxX - minX) + (maxY - minY) * (maxY - minY) + (maxZ - minZ) * (maxZ - minZ)) / 2);
			float scale = Size * 0.45f / radius * zoom;
			float yaw = yawDegrees * Mathf.Deg2Rad, pitch = pitchDegrees * Mathf.Deg2Rad;
			float cyw = (float)Math.Cos(yaw), syw = (float)Math.Sin(yaw), cp = (float)Math.Cos(pitch), sp = (float)Math.Sin(pitch);
			// x across the screen, y down it, z the depth away from the viewer
			Func<float, float, float, float[]> view = (x, y, z) =>
			{
				float x1 = x * cyw - y * syw, y1 = x * syw + y * cyw;          // turn about the vertical (z is up)
				float y2 = y1 * cp - z * sp, z2 = y1 * sp + z * cp;            // tilt
				return new[] { x1, -z2, y2 };
			};

			var proj = new float[m.Vertices.Count][];
			for (int i = 0; i < m.Vertices.Count; i++)
			{
				var v = m.Vertices[i];
				if (v == null) continue;
				var p = view(v[0] - cx, v[1] - cy, v[2] - cz);
				proj[i] = new[] { Size / 2f + p[0] * scale, Size / 2f + p[1] * scale, p[2] };
			}

			var depth = new float[Size * Size];
			for (int i = 0; i < depth.Length; i++) depth[i] = float.MaxValue;
			var edges = new List<KeyValuePair<Color32, int[]>>();
			foreach (var f in m.Faces)
			{
				if (f.Vertices.Length < 3) continue;
				Color32 baseColor = f.Color >= 0 ? palette[f.Color] : new Color32(150, 150, 150, 255);
				if (f.Textured) baseColor = new Color32(170, 160, 140, 255);
				var n = Uw1ExeModels.Normal(m, f.Vertices);
				var nv = view((float)n[0], (float)n[1], (float)n[2]);
				double len = Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
				float facing = len > 0 ? (float)(Math.Abs(nv[2]) / len) : 0;
				float light = 0.45f + 0.55f * facing;
				var col = new Color32((byte)(baseColor.r * light), (byte)(baseColor.g * light), (byte)(baseColor.b * light), 255);
				var tris = f.Triangles ?? new int[0][];
				foreach (var t in tris)
					FillTriangle(px, depth, proj[t[0]], proj[t[1]], proj[t[2]], col);
				edges.Add(new KeyValuePair<Color32, int[]>(new Color32((byte)(col.r / 2), (byte)(col.g / 2), (byte)(col.b / 2), 255), f.Vertices));
			}
			// outlines after every fill, each where nothing nearer covers it (the bias lets a
			// face's own edge win over the face)
			float bias = radius * 0.02f;
			foreach (var e in edges)
				for (int k = 0; k < e.Value.Length; k++)
					Line(px, depth, proj[e.Value[k]], proj[e.Value[(k + 1) % e.Value.Length]], e.Key, bias);
			return px;
		}

		static void Upload(Texture2D t, Color32[] px)
		{
			// Texture rows go bottom-up; the picture was drawn top-down.
			var flipped = new Color32[px.Length];
			for (int y = 0; y < Size; y++)
				Array.Copy(px, y * Size, flipped, (Size - 1 - y) * Size, Size);
			t.SetPixels32(flipped);
			t.Apply();
		}

		static void FillTriangle(Color32[] px, float[] depth, float[] a, float[] b, float[] c, Color32 col)
		{
			int minX = Math.Max(0, (int)Math.Floor(Math.Min(a[0], Math.Min(b[0], c[0]))));
			int maxX = Math.Min(Size - 1, (int)Math.Ceiling(Math.Max(a[0], Math.Max(b[0], c[0]))));
			int minY = Math.Max(0, (int)Math.Floor(Math.Min(a[1], Math.Min(b[1], c[1]))));
			int maxY = Math.Min(Size - 1, (int)Math.Ceiling(Math.Max(a[1], Math.Max(b[1], c[1]))));
			float area = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);
			if (Math.Abs(area) < 1e-6f) return;
			for (int y = minY; y <= maxY; y++)
				for (int x = minX; x <= maxX; x++)
				{
					float px0 = x + 0.5f, py0 = y + 0.5f;
					float w0 = (b[0] - px0) * (c[1] - py0) - (b[1] - py0) * (c[0] - px0);
					float w1 = (c[0] - px0) * (a[1] - py0) - (c[1] - py0) * (a[0] - px0);
					float w2 = (a[0] - px0) * (b[1] - py0) - (a[1] - py0) * (b[0] - px0);
					if (!((w0 >= 0 && w1 >= 0 && w2 >= 0) || (w0 <= 0 && w1 <= 0 && w2 <= 0))) continue;
					// the weights are twice the sub-areas: divided by the area they sum to one
					float z = (w0 * a[2] + w1 * b[2] + w2 * c[2]) / area;
					int i = y * Size + x;
					if (z >= depth[i]) continue;
					depth[i] = z;
					px[i] = col;
				}
		}

		static void Line(Color32[] px, float[] depth, float[] a, float[] b, Color32 col, float bias)
		{
			int steps = (int)Math.Max(1, Math.Ceiling(Math.Max(Math.Abs(b[0] - a[0]), Math.Abs(b[1] - a[1]))));
			for (int i = 0; i <= steps; i++)
			{
				float t = (float)i / steps;
				int x = (int)(a[0] + (b[0] - a[0]) * t), y = (int)(a[1] + (b[1] - a[1]) * t);
				if (x < 0 || x >= Size || y < 0 || y >= Size) continue;
				float z = a[2] + (b[2] - a[2]) * t;
				if (z - bias <= depth[y * Size + x]) px[y * Size + x] = col;
			}
		}
	}

	/// <summary>Reports pointer drags over the model picture.</summary>
	public sealed class ModelDrag : MonoBehaviour, UnityEngine.EventSystems.IDragHandler, UnityEngine.EventSystems.IBeginDragHandler
	{
		public Action<Vector2> OnDragged;
		public void OnBeginDrag(UnityEngine.EventSystems.PointerEventData e) { }
		public void OnDrag(UnityEngine.EventSystems.PointerEventData e) { if (OnDragged != null) OnDragged(e.delta); }
	}
}
