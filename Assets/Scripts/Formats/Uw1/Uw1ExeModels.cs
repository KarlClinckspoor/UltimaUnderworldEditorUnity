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
			m.Faces.Add(new Face { Vertices = verts, Color = color, Textured = textured });
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
						int n = r.U16(), first = r.U16();
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
						int refv = r.Vert(); r.U16(); int nv = r.Vert();
						var p = Get(m, refv);
						// Reaches up to the ceiling in the game; shown at the model's height here.
						Store(m, nv, p[0], p[1], Math.Max(p[2], m.ExtentZ));
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
					case 0x00A0: case 0x00D2:
					{
						if (cmd == 0x00A0) r.U16();
						var v = new int[4];
						for (int i = 0; i < 4; i++) v[i] = r.U8() >> 3;
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
				sb.Append("f");
				foreach (int v in f.Vertices) sb.Append(' ').Append(map[v]);
				sb.Append('\n');
			}
			return sb.ToString();
		}
	}
}
