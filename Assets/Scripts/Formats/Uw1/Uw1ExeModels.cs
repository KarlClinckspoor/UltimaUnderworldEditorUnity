using System;
using System.Collections.Generic;
using System.IO;

namespace UltimateEditor.Uw1
{
	/// <summary>
	/// The 3D models built into UW.EXE (doors, benches, boulders, the shrine, the moongate...).
	/// They are bytecode for the renderer's model interpreter, in the 3D renderer's far data
	/// (seg051 in underworld-exhumed; the interpreter is uw1/src/3d/INTERP.ASM, whose node
	/// meanings are not traced there). The node layout follows UW-Formats section 8
	/// (Underworld Adventures). This decoder only collects geometry for display: it draws
	/// every face and ignores the visibility tests.
	///
	/// Finding the data: a table of 32 word offsets that starts with b6 4a 06 40; the model
	/// data is at table + 0x8E + offset. Each model has a colour table of up to three palette
	/// indexes (4-byte entries, zero-terminated) found by its first entries ec 00 00 ?? eb 00 00.
	/// </summary>
	public sealed class Uw1ExeModels
	{
		public static readonly string[] Names =
		{
			"(empty)", "Door frame", "Bridge", "Bench", "Lotus Turbo Esprit", "Small boulder", "Medium boulder", "Large boulder",
			"Arrow", "Beam", "Pillar", "Shrine", "(unknown 0x0C)", "Painting (UW2)", "Door", "Secret door",
			"Texture map (lever, buttons)", "Texture map (8-way switch)", "Texture map (writing)", "Gravestone", "Texture map (0x16E)", "(empty)", "Texture map (0x16F)", "Moongate",
			"Table", "Chest", "Nightstand", "Barrel", "Chair", "Bed (UW2)", "Blackrock gem (UW2)", "Shelf (UW2)"
		};

		public sealed class Face
		{
			public int[] Vertices;
			public int Color;          // palette index, -1 when unknown
			public bool Textured;
			/// <summary>The face cut into triangles (vertex numbers, three at a time) that cover
			/// exactly the polygon, concave or not (see <see cref="Triangulate"/>).</summary>
			public int[][] Triangles;
			/// <summary>False when the polygon has a notch (the ankh's outline, for one).</summary>
			public bool Convex;
		}

		public sealed class Model
		{
			public int Index;
			public string Name;
			public long FileOffset;
			public float ExtentX, ExtentY, ExtentZ;
			public readonly List<float[]> Vertices = new List<float[]>(); // x, y, z (z up); null for unused slots
			public readonly List<Face> Faces = new List<Face>();
			public readonly List<string> Log = new List<string>();
			public string Error;
		}

		public long TableOffset { get; private set; }
		public long BaseOffset { get; private set; }
		public long ColorTableOffset { get; private set; }
		public readonly List<Model> Models = new List<Model>();

		static readonly byte[] TableSignature = { 0xB6, 0x4A, 0x06, 0x40 };
		const int BaseDelta = 0x8E;
		// UW2 (UW-Formats 8): table starting d4 64 aa 59, data at table + 0x9A, colour words based
		// at 0x2680, and a colour table of 5-byte entries (low nibble: count, then 3 colours, then
		// a byte) 0x1439A bytes after the table in the documented build.
		static readonly byte[] Uw2TableSignature = { 0xD4, 0x64, 0xAA, 0x59 };
		const int Uw2BaseDelta = 0x9A;
		const long Uw2ColorDelta = 0x6908A - 0x54CF0;
		int colorBase = 0x2920;   // UW1: colour words are offsets into the renderer's data
		bool uw2;

		public bool IsUw2 { get { return uw2; } }

		public static Uw1ExeModels Load(byte[] exe)
		{
			var m = new Uw1ExeModels();
			m.TableOffset = Find(exe, TableSignature, null);
			if (m.TableOffset >= 0)
			{
				m.BaseOffset = m.TableOffset + BaseDelta;
				m.ColorTableOffset = Find(exe, new byte[] { 0xEC, 0, 0, 0, 0xEB, 0, 0 }, new[] { true, true, true, false, true, true, true });
			}
			else
			{
				m.TableOffset = Find(exe, Uw2TableSignature, null);
				if (m.TableOffset < 0)
					throw new InvalidDataException("No model table found (looked for b6 4a 06 40 for UW1 and d4 64 aa 59 for UW2). Is this UW.EXE or UW2.EXE?");
				m.uw2 = true;
				m.colorBase = 0x2680;
				m.BaseOffset = m.TableOffset + Uw2BaseDelta;
				m.ColorTableOffset = m.TableOffset + Uw2ColorDelta < exe.Length ? m.TableOffset + Uw2ColorDelta : -1;
			}
			for (int i = 0; i < 32; i++)
			{
				int rel = exe[m.TableOffset + i * 2] | exe[m.TableOffset + i * 2 + 1] << 8;
				var model = new Model { Index = i, Name = Names[i], FileOffset = m.BaseOffset + rel };
				try
				{
					m.Decode(exe, model);
				}
				catch (Exception e)
				{
					model.Error = e.Message;
				}
				m.Models.Add(model);
			}
			return m;
		}

		static long Find(byte[] d, byte[] pat, bool[] care)
		{
			for (long i = 0; i + pat.Length <= d.Length; i++)
			{
				bool ok = true;
				for (int k = 0; k < pat.Length && ok; k++)
					if ((care == null || care[k]) && d[i + k] != pat[k]) ok = false;
				if (ok) return i;
			}
			return -1;
		}

		int[] ColorsOf(byte[] exe, int model)
		{
			if (ColorTableOffset < 0) return new int[0];
			var list = new List<int>();
			if (uw2)
			{
				long e = ColorTableOffset + model * 5;
				if (e + 4 >= exe.Length) return new int[0];
				int count = exe[e] & 0x0F;
				for (int k = 0; k < count && k < 3; k++) list.Add(exe[e + 1 + k]);
				return list.ToArray();
			}
			long o = ColorTableOffset + model * 4;
			for (int k = 0; k < 3 && o + k < exe.Length; k++)
			{
				if (exe[o + k] == 0) break;
				list.Add(exe[o + k]);
			}
			return list.ToArray();
		}

		sealed class Reader
		{
			public readonly byte[] D; public long P;
			public Reader(byte[] d, long p) { D = d; P = p; }
			public int U16() { if (P + 1 >= D.Length) throw new InvalidDataException("model data runs off the file"); int v = D[P] | D[P + 1] << 8; P += 2; return v; }
			public int S16() { return (short)U16(); }
			public int U8() { if (P >= D.Length) throw new InvalidDataException("model data runs off the file"); return D[P++]; }
			public float Fixed() { return S16() / 256f; }
			public int Vert() { return U16() >> 3; }
		}

		void Decode(byte[] exe, Model model)
		{
			var r = new Reader(exe, model.FileOffset);
			r.U16(); r.U16();                       // unknown long
			model.ExtentX = r.Fixed(); model.ExtentY = r.Fixed(); model.ExtentZ = r.Fixed();
			int[] colors = ColorsOf(exe, model.Index);
			int color = colors.Length > 0 ? colors[0] : -1;
			var visited = new HashSet<long>();
			ParseList(r, model, colors, ref color, visited, 0);
		}

		static void Store(Model m, int index, float x, float y, float z)
		{
			if (index < 0 || index > 4096) throw new InvalidDataException("vertex number " + index);
			while (m.Vertices.Count <= index) m.Vertices.Add(null);
			m.Vertices[index] = new[] { x, y, z };
		}

		static float[] Get(Model m, int index)
		{
			if (index < 0 || index >= m.Vertices.Count || m.Vertices[index] == null)
				throw new InvalidDataException("vertex " + index + " used before it is defined");
			return m.Vertices[index];
		}

		int MapColor(int word, int[] colors)
		{
			if (colors.Length == 0) return -1;
			int i = ((word - colorBase) >> 1) % colors.Length;
			if (i < 0) i += colors.Length;
			return colors[i];
		}

		void AddFace(Model m, int[] verts, int color, bool textured)
		{
			foreach (int v in verts) Get(m, v);
			bool convex;
			var tris = Triangulate(m, verts, out convex);
			m.Faces.Add(new Face { Vertices = verts, Color = color, Textured = textured, Triangles = tris, Convex = convex });
		}

		/// <summary>The face's normal (Newell's method: sound for any planar polygon, concave or
		/// with collinear corners), not normalised; zero for a degenerate face.</summary>
		public static double[] Normal(Model m, int[] verts)
		{
			double nx = 0, ny = 0, nz = 0;
			for (int i = 0; i < verts.Length; i++)
			{
				var a = m.Vertices[verts[i]]; var b = m.Vertices[verts[(i + 1) % verts.Length]];
				nx += (a[1] - b[1]) * (a[2] + b[2]);
				ny += (a[2] - b[2]) * (a[0] + b[0]);
				nz += (a[0] - b[0]) * (a[1] + b[1]);
			}
			return new[] { nx, ny, nz };
		}

		/// <summary>
		/// Cuts a polygon into triangles by ear clipping in its own plane. The game fills its
		/// polygons by scanlines, so a face may be concave: the ankh's front and back are each
		/// one outline of more than twenty corners. A fan from the first corner, the usual short
		/// cut, fills the notches of such a face (it joined the ankh's arms to its foot).
		/// Corners in a straight line are dropped without a triangle. A polygon that is not
		/// simple, where no ear can be found, gets a fan for what is left.
		/// </summary>
		public static int[][] Triangulate(Model m, int[] verts, out bool convex)
		{
			int n = verts.Length;
			convex = true;
			var result = new List<int[]>();
			if (n < 3) return result.ToArray();
			// project onto the plane of the two axes the normal is least along
			var nrm = Normal(m, verts);
			int drop = Math.Abs(nrm[0]) >= Math.Abs(nrm[1]) && Math.Abs(nrm[0]) >= Math.Abs(nrm[2]) ? 0 : Math.Abs(nrm[1]) >= Math.Abs(nrm[2]) ? 1 : 2;
			int ua = drop == 0 ? 1 : 0, va = drop == 2 ? 1 : 2;
			var u = new double[n]; var v = new double[n];
			double minU = double.MaxValue, maxU = double.MinValue, minV = double.MaxValue, maxV = double.MinValue, area = 0;
			for (int i = 0; i < n; i++)
			{
				var p = m.Vertices[verts[i]];
				u[i] = p[ua]; v[i] = p[va];
				minU = Math.Min(minU, u[i]); maxU = Math.Max(maxU, u[i]); minV = Math.Min(minV, v[i]); maxV = Math.Max(maxV, v[i]);
			}
			for (int i = 0; i < n; i++) { int j = (i + 1) % n; area += u[i] * v[j] - u[j] * v[i]; }
			double size = Math.Max(maxU - minU, maxV - minV);
			double eps = 1e-9 * size * size;
			double sign = area < 0 ? -1 : 1;     // make the polygon counter-clockwise in (u, v)
			Func<int, int, int, double> cross = (a, b, c) => sign * ((u[b] - u[a]) * (v[c] - v[a]) - (v[b] - v[a]) * (u[c] - u[a]));
			for (int i = 0; i < n; i++)
				if (cross((i + n - 1) % n, i, (i + 1) % n) < -eps) { convex = false; break; }

			var left = new List<int>();
			for (int i = 0; i < n; i++) left.Add(i);
			while (left.Count > 3)
			{
				bool cut = false;
				int c = left.Count;
				// corners in a straight line first: they need no triangle
				for (int k = 0; k < c && !cut; k++)
				{
					int a = left[(k + c - 1) % c], b = left[k], d = left[(k + 1) % c];
					if (Math.Abs(cross(a, b, d)) <= eps) { left.RemoveAt(k); cut = true; }
				}
				for (int k = 0; k < c && !cut; k++)
				{
					int a = left[(k + c - 1) % c], b = left[k], d = left[(k + 1) % c];
					if (cross(a, b, d) <= eps) continue;          // a reflex corner is no ear
					bool empty = true;
					foreach (int q in left)
					{
						if (q == a || q == b || q == d) continue;
						if (Same(u, v, q, a, eps) || Same(u, v, q, b, eps) || Same(u, v, q, d, eps)) continue;
						if (cross(a, b, q) >= -eps && cross(b, d, q) >= -eps && cross(d, a, q) >= -eps) { empty = false; break; }
					}
					if (!empty) continue;
					result.Add(new[] { verts[a], verts[b], verts[d] });
					left.RemoveAt(k);
					cut = true;
				}
				if (!cut)
				{
					for (int k = 1; k + 1 < left.Count; k++)
						result.Add(new[] { verts[left[0]], verts[left[k]], verts[left[k + 1]] });
					return result.ToArray();
				}
			}
			if (left.Count == 3 && Math.Abs(cross(left[0], left[1], left[2])) > eps)
				result.Add(new[] { verts[left[0]], verts[left[1]], verts[left[2]] });
			return result.ToArray();
		}

		static bool Same(double[] u, double[] v, int a, int b, double eps)
		{
			double du = u[a] - u[b], dv = v[a] - v[b];
			return du * du + dv * dv <= eps;
		}

		void ParseList(Reader r, Model m, int[] colors, ref int color, HashSet<long> visited, int depth)
		{
			if (depth > 32) throw new InvalidDataException("sort/submodel nesting too deep");
			if (!visited.Add(r.P)) return;  // a list already parsed (shared subtree)
			for (int guard = 0; guard < 10000; guard++)
			{
				long at = r.P;
				int cmd = r.U16();
				switch (cmd)
				{
					case 0x0000: return;
					case 0x0006:
					case 0x000C: case 0x000E: case 0x0010:
					{
						int fixedCount = cmd == 0x0006 ? 6 : 4;
						for (int i = 0; i < fixedCount; i++) r.S16();
						int left = r.U16(); long leftAt = r.P + (short)left;
						int right = r.U16(); long rightAt = r.P + (short)right;
						long here = r.P;
						r.P = leftAt; ParseList(r, m, colors, ref color, visited, depth + 1);
						r.P = rightAt; ParseList(r, m, colors, ref color, visited, depth + 1);
						r.P = here;
						break;
					}
					case 0x0012: case 0x002E: case 0x00B2: r.U16(); break;
					case 0x0014: case 0x0016: r.U16(); r.U16(); r.U16(); break;
					case 0x0040: case 0x0044: case 0x00D6: break;
					case 0x004A: r.S16(); r.S16(); r.S16(); break;   // translation (doors)
					case 0x0058: for (int i = 0; i < 7; i++) r.U16(); break;
					case 0x005E: case 0x0060: case 0x0062: for (int i = 0; i < 5; i++) r.U16(); break;
					case 0x0064: case 0x0066: case 0x0068: for (int i = 0; i < 3; i++) r.U16(); break;
					case 0x0078: for (int i = 0; i < 5; i++) r.U16(); break;   // model centre
					case 0x007A:
					{
						float x = r.Fixed(), y = r.Fixed(), z = r.Fixed();
						Store(m, r.Vert(), x, y, z);
						break;
					}
					case 0x0082:
					{
						int n = r.U16(), first = r.Vert();   // a point offset (times 8), as in every other node
						for (int i = 0; i < n; i++)
						{
							float x = r.Fixed(), y = r.Fixed(), z = r.Fixed();
							Store(m, first + i, x, y, z);
						}
						break;
					}
					case 0x0086: case 0x0088: case 0x008A:
					{
						int refv = r.Vert(); float d = r.Fixed(); int nv = r.Vert();
						var p = Get(m, refv);
						Store(m, nv, p[0] + (cmd == 0x0086 ? d : 0), p[1] + (cmd == 0x008A ? d : 0), p[2] + (cmd == 0x0088 ? d : 0));
						break;
					}
					case 0x008C:
					{
						// do_addres (INTERP.ASM): destination = point a + point b. The shipped models
						// (door frame, pillar) always add point 256, which no model defines: the
						// renderer's, reaching up to the ceiling. Drawn up to one square's height
						// here: the door frame's lintel and the pillar stand 1.0 high.
						int refv = r.Vert(); int add = r.Vert(); int nv = r.Vert();
						var p = Get(m, refv);
						var q = add < m.Vertices.Count ? m.Vertices[add] : null;
						if (q != null) Store(m, nv, p[0] + q[0], p[1] + q[1], p[2] + q[2]);
						else Store(m, nv, p[0], p[1], Math.Max(p[2], 1f));   // one square up (a square is 1.0)
						break;
					}
					case 0x00C6:
					{
						// do_subres (INTERP.ASM): destination = point a - point b
						var p = Get(m, r.Vert()); var q = Get(m, r.Vert());
						Store(m, r.Vert(), p[0] - q[0], p[1] - q[1], p[2] - q[2]);
						break;
					}
					case 0x0090: case 0x0092: case 0x0094:
					{
						float a = r.Fixed(), b = r.Fixed(); int refv = r.Vert(); int nv = r.Vert();
						var p = Get(m, refv);
						if (cmd == 0x0090) Store(m, nv, p[0] + a, p[1], p[2] + b);
						else if (cmd == 0x0092) Store(m, nv, p[0] + a, p[1] + b, p[2]);
						else Store(m, nv, p[0], p[1] + a, p[2] + b);
						break;
					}
					case 0x007E:
					{
						int n = r.U16(); var v = new int[n];
						for (int i = 0; i < n; i++) v[i] = r.Vert();
						AddFace(m, v, color, false);
						break;
					}
					case 0x00A8: case 0x00B4: case 0x00CE:
					{
						if (cmd == 0x00A8) r.U16();
						int n = r.U16(); var v = new int[n];
						for (int i = 0; i < n; i++) { v[i] = r.Vert(); r.U16(); r.U16(); }
						AddFace(m, v, color, true);
						break;
					}
					case 0x00A0: case 0x00A2: case 0x00D2:
					{
						// do_compact_tmap / _wtmap / _map (TMAPOPS.ASM): a bitmap slot (UW2's
						// do_compact_map takes the context's instead), then four point NUMBERS as
						// bytes, not offsets: the handler multiplies them by 8 itself.
						if (cmd != 0x00D2 || !uw2) r.U16();
						var v = new int[4];
						for (int i = 0; i < 4; i++) v[i] = r.U8();
						AddFace(m, v, color, true);
						break;
					}
					case 0x00BA:
					{
						color = MapColor(r.U16(), colors);
						int rel = r.U16();
						long here = r.P;
						r.P = here + (short)rel;
						ParseList(r, m, colors, ref color, visited, depth + 1);
						r.P = here;
						break;
					}
					case 0x00BC: color = MapColor(r.U16(), colors); r.U16(); break;
					case 0x00BE: r.U16(); r.U16(); break;
					case 0x00D4:
					{
						int n = r.U16(); color = MapColor(r.U16(), colors);
						for (int i = 0; i < n; i++) { r.U16(); r.U8(); }
						if ((n & 1) != 0) r.U8();
						break;
					}
					default:
						throw new InvalidDataException(string.Format("unknown node {0:X4} at file offset 0x{1:X}", cmd, at));
				}
			}
			throw new InvalidDataException("node list does not end");
		}

		/// <summary>Writes a model as Wavefront OBJ text (y up), for use in other tools.</summary>
		public static string ToObj(Model m)
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine("# UW1 built-in model " + m.Index + ": " + m.Name);
			var map = new Dictionary<int, int>();
			for (int i = 0; i < m.Vertices.Count; i++)
			{
				var v = m.Vertices[i];
				if (v == null) continue;
				map[i] = map.Count + 1;
				sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "v {0} {1} {2}\n", v[0], v[2], v[1]);
			}
			foreach (var f in m.Faces)
			{
				// A concave face goes out as its triangles: many programs would fan an n-gon.
				var polys = f.Convex || f.Triangles == null ? new[] { f.Vertices } : f.Triangles;
				foreach (var poly in polys)
				{
					sb.Append("f");
					foreach (int v in poly) sb.Append(' ').Append(map[v]);
					sb.Append('\n');
				}
			}
			return sb.ToString();
		}
	}
}
