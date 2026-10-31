using System;
using System.Collections.Generic;
using UltimateEditor.Games;
using UltimateEditor.Uw1;
using UnityEngine;

namespace UltimateEditor.View
{
	/// <summary>
	/// How the game draws each object in the 3D view (uw1/src/3d/DRAWOBJ.C do_obj and its UW2
	/// twin), from the render field of COMOBJ.DAT: 0 a sprite, 1 a critter, 2 a built-in 3D
	/// model (or a door for item &amp; 0x3F below 0x10), 3 a texture-mapped model (levers and
	/// switches, 0x170-0x17F, take model 0x14 and a TMFLAT picture; the others model 0x16 and
	/// the wall texture named by their owner).
	/// </summary>
	public static class WalkObjects
	{
		public enum Kind { None, Sprite, Critter, Model, Door, TmapLever, TmapWall }

		/// <summary>The model for each render-2 item, by item &amp; 0x3F less 0x10 (rect_sub).</summary>
		static readonly int[] RectSubUw1 =
		{
			0x03, 0x08, 0x08, 0x07, 0x07, 0x06, 0x05, 0x0B,
			0x18, 0x09, 0x17, 0x1B, 0x1C, 0x19, 0x1A, 0x04,
			0x0A, 0x10, 0x11, -1, 0x02, 0x13, 0x12, -1,
			-1, -1, -1, -1, -1, -1, -1, -1
		};
		static readonly int[] RectSubUw2 =
		{
			0x03, 0x08, 0x08, 0x07, 0x07, 0x06, 0x05, 0x0B,
			0x18, 0x09, 0x17, 0x1B, 0x1C, 0x19, 0x1A, 0x04,
			0x0A, 0x10, 0x11, 0x0D, 0x02, 0x13, 0x12, 0x1D,
			0x1E, 0x1F, -1, -1, -1, -1, -1, -1
		};

		/// <summary>UW1 rect_cols: flags, three colours (the third is, for a textured model, the
		/// first texture in bits 0-4 and the count less one in bits 5-7).</summary>
		static readonly byte[,] RectColsUw1 =
		{
			{ 0x01, 0xEC, 0x00, 0x00 }, { 0x21, 0xEB, 0x00, 0x00 }, { 0x11, 0xEC, 0x00, 0x3E }, { 0x01, 0xE4, 0x00, 0x00 },
			{ 0x02, 0xB6, 0xB0, 0x00 }, { 0x02, 0x64, 0x6C, 0x00 }, { 0x02, 0x64, 0x6C, 0x00 }, { 0x02, 0x64, 0x6C, 0x00 },
			{ 0x42, 0xE8, 0xB8, 0x00 }, { 0x01, 0xE4, 0x00, 0x00 }, { 0x19, 0xE4, 0x00, 0x60 }, { 0x03, 0xA3, 0xA4, 0xA6 },
			{ 0x01, 0x68, 0x00, 0x00 }, { 0x01, 0x68, 0x00, 0x00 }, { 0x11, 0xEC, 0x00, 0x00 }, { 0x21, 0xEC, 0x00, 0x00 },
			{ 0x51, 0xB0, 0x00, 0xE4 }, { 0x51, 0xB0, 0x00, 0xEC }, { 0x11, 0xB0, 0x00, 0xF4 }, { 0x11, 0x6A, 0x00, 0x3C },
			{ 0x51, 0xB0, 0x00, 0x00 }, { 0x11, 0xB0, 0x00, 0x00 }, { 0x21, 0xB0, 0x00, 0x00 }, { 0x83, 0x00, 0x02, 0x04 },
			{ 0x02, 0xE4, 0x68, 0x00 }, { 0x02, 0xE6, 0x68, 0x00 }, { 0x01, 0xE4, 0x00, 0x00 }, { 0x02, 0xE4, 0x6A, 0x00 },
			{ 0x03, 0xE6, 0x6A, 0x71 }, { 0x03, 0xE2, 0x62, 0xC4 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 },
		};
		/// <summary>UW2 rect_cols: flags, three colours, first texture, number of textures.</summary>
		static readonly byte[,] RectColsUw2 =
		{
			{ 0x01, 0x8F, 0x00, 0x00, 0x00 }, { 0x21, 0x93, 0x00, 0x00, 0x00 }, { 0x11, 0x8F, 0x00, 0x1E, 0x02 }, { 0x01, 0x8C, 0x00, 0x00, 0x00 },
			{ 0x02, 0x21, 0x1B, 0x00, 0x00 }, { 0x02, 0xC6, 0xCC, 0x00, 0x00 }, { 0x02, 0xC6, 0xCC, 0x00, 0x00 }, { 0x02, 0xC6, 0xCC, 0x00, 0x00 },
			{ 0x43, 0x88, 0x23, 0x56, 0x00 }, { 0x01, 0x8C, 0x00, 0x00, 0x00 }, { 0x19, 0x8C, 0x00, 0x00, 0x04 }, { 0x03, 0xB9, 0xBA, 0xBC, 0x00 },
			{ 0x01, 0xCA, 0x00, 0x00, 0x00 }, { 0x11, 0x8D, 0x00, 0x2A, 0x06 }, { 0x11, 0x8F, 0x00, 0x00, 0x00 }, { 0x21, 0x8F, 0x00, 0x00, 0x00 },
			{ 0x51, 0x1B, 0x00, 0x04, 0x08 }, { 0x51, 0x1B, 0x00, 0x0C, 0x08 }, { 0x11, 0x1B, 0x00, 0x14, 0x08 }, { 0x11, 0xCB, 0x00, 0x1C, 0x02 },
			{ 0x51, 0x1B, 0x00, 0x00, 0x00 }, { 0x11, 0x1B, 0x00, 0x00, 0x00 }, { 0x21, 0x1B, 0x00, 0x00, 0x00 }, { 0x83, 0x00, 0x02, 0x04, 0x00 },
			{ 0x12, 0x8C, 0xCA, 0x20, 0x04 }, { 0x02, 0x8E, 0xCA, 0x00, 0x00 }, { 0x01, 0x8C, 0x00, 0x00, 0x00 }, { 0x02, 0x8C, 0xCB, 0x8E, 0x00 },
			{ 0x11, 0x8F, 0x00, 0x26, 0x04 }, { 0x04, 0x31, 0xC6, 0x52, 0x4D }, { 0x02, 0xCC, 0x02, 0x00, 0x00 }, { 0x12, 0x15, 0x35, 0x24, 0x02 },
		};

		/// <summary>COMOBJ.DAT's render field for each item (11 bytes an item after a 2-byte
		/// header, byte 9 bits 0-1; include/object.h struct ComObj).</summary>
		public static int[] ReadRenderTypes(byte[] comobj)
		{
			var r = new int[512];
			for (int i = 0; i < 512; i++)
			{
				int at = 2 + i * 11 + 9;
				r[i] = at < comobj.Length ? comobj[at] & 3 : 0;
			}
			return r;
		}

		/// <summary>How item is drawn; model is the built-in model for Model, Door's leaf, and the tmaps.</summary>
		public static Kind Classify(int item, int[] render, bool uw2, out int model)
		{
			model = -1;
			if (item >= 0x180 && item < 0x1C0)
				return Kind.None;   // traps and triggers are not drawn
			int type = render != null && item < render.Length ? render[item] : 0;
			switch (type)
			{
				case 1: return Kind.Critter;
				case 2:
					int low = item & 0x3F;
					if (low >> 4 == 0)
					{
						if (item < 0x140 || item >= 0x150) return Kind.None;   // the moving door animation (0x1CF) is skipped
						int door = item & 7;
						model = door == 7 ? 0x0F : 0x0E;
						return Kind.Door;
					}
					int idx = low - 0x10;
					int[] sub = uw2 ? RectSubUw2 : RectSubUw1;
					if (idx < 0 || idx >= sub.Length || sub[idx] < 0) return Kind.None;
					model = sub[idx];
					return Kind.Model;
				case 3:
					if (((item >> 4) & 3) == 3) { model = 0x14; return Kind.TmapLever; }
					model = 0x16;
					return Kind.TmapWall;
				default: return Kind.Sprite;
			}
		}

		/// <summary>
		/// The texture a model's textured faces take when the object does not name one (do_rect):
		/// a TMOBJ-range number (0-0x0F TMFLAT, 0x10-0x2F TMOBJ, 0x30 on doors), or a floor texture
		/// for the bridge (model 2) when its flags pass the model's texture count. -1: none.
		/// </summary>
		public static int ModelTexture(int model, int flags, bool uw2, out bool floorTexture)
		{
			floorTexture = false;
			if (model < 0 || model >= 32) return -1;
			if (uw2)
			{
				if ((RectColsUw2[model, 0] & 0x10) == 0) return -1;
				int ntex = RectColsUw2[model, 4], first = RectColsUw2[model, 3];
				if (ntex == 0) return -1;
				if (model == 2 && flags >= ntex) { floorTexture = true; return flags - ntex; }
				return first + 0x10 + flags % ntex;
			}
			else
			{
				if ((RectColsUw1[model, 0] & 0x10) == 0) return -1;
				int first = RectColsUw1[model, 3], count = (first >> 5) + 1;
				if (model == 2 && flags >= count) { floorTexture = true; return flags - count; }
				return (first & 0x1F) + 0x10 + flags % count;
			}
		}

		/// <summary>
		/// The model's faces in tiles (x east, y up, z north), turned clockwise by heading eighths
		/// of a turn about the object's origin: coloured faces in plain (palette colours), textured
		/// faces in textured, mapped across each face's extent. Every face is drawn from both sides.
		/// topTo above 0: corners at height 1 (where the decoder stops the door frame and the pillar,
		/// which in the game reach the ceiling) are raised to that height above the origin.
		/// </summary>
		public static void AddModel(Uw1ExeModels.Model m, Vector3 origin, float headingDegrees, Color[] palette, MeshPart plain, MeshPart textured, Color tint, float topTo = -1f)
		{
			if (m == null) return;
			Quaternion rot = Quaternion.Euler(0f, headingDegrees, 0f);
			foreach (var f in m.Faces)
			{
				if (f.Triangles == null || f.Vertices == null || f.Vertices.Length < 3) continue;
				bool ok = true;
				foreach (int vi in f.Vertices)
					if (vi < 0 || vi >= m.Vertices.Count || m.Vertices[vi] == null) { ok = false; break; }
				if (!ok) continue;
				Color col = tint;
				if (!f.Textured)
				{
					Color pc = f.Color >= 0 && palette != null && f.Color < palette.Length ? palette[f.Color] : new Color(0.6f, 0.6f, 0.6f);
					col = new Color(pc.r * tint.r, pc.g * tint.g, pc.b * tint.b, 1f);
				}
				// texture coordinates: the face's own plane, across its extent
				Func<int, Vector2> uv = null;
				if (f.Textured)
				{
					double[] n = Uw1ExeModels.Normal(m, f.Vertices);
					bool flat = n != null && Math.Abs(n[2]) >= Math.Abs(n[0]) && Math.Abs(n[2]) >= Math.Abs(n[1]);
					// horizontal axis along the face: perpendicular to the normal in the floor plane
					float ax = 1f, ay = 0f;
					if (!flat && n != null)
					{
						double len = Math.Sqrt(n[0] * n[0] + n[1] * n[1]);
						if (len > 1e-9) { ax = (float)(-n[1] / len); ay = (float)(n[0] / len); }
					}
					float umin = float.MaxValue, umax = float.MinValue, vmin = float.MaxValue, vmax = float.MinValue;
					Func<float[], Vector2> raw = p => flat ? new Vector2(p[0], p[1]) : new Vector2(p[0] * ax + p[1] * ay, p[2]);
					foreach (int vi in f.Vertices)
					{
						var q = raw(m.Vertices[vi]);
						umin = Math.Min(umin, q.x); umax = Math.Max(umax, q.x); vmin = Math.Min(vmin, q.y); vmax = Math.Max(vmax, q.y);
					}
					float du = Math.Max(1e-5f, umax - umin), dv = Math.Max(1e-5f, vmax - vmin);
					uv = vi => { var q = raw(m.Vertices[vi]); return new Vector2((q.x - umin) / du, (q.y - vmin) / dv); };
				}
				MeshPart p = f.Textured ? textured : plain;
				if (p == null) continue;
				foreach (var tri in f.Triangles)
				{
					if (tri == null || tri.Length < 3) continue;
					var a = World(m.Vertices[tri[0]], origin, rot, topTo);
					var b = World(m.Vertices[tri[1]], origin, rot, topTo);
					var c = World(m.Vertices[tri[2]], origin, rot, topTo);
					p.Tri(a, b, c,
						uv != null ? uv(tri[0]) : Vector2.zero, uv != null ? uv(tri[1]) : Vector2.zero, uv != null ? uv(tri[2]) : Vector2.zero, col);
				}
			}
		}

		static Vector3 World(float[] p, Vector3 origin, Quaternion rot, float topTo)
		{
			// the model's x east, y north (towards the wall for wall objects), z up
			float z = topTo > 0 && p[2] >= 0.999f ? topTo : p[2];
			return origin + rot * new Vector3(p[0], z, p[1]);
		}
	}
}
