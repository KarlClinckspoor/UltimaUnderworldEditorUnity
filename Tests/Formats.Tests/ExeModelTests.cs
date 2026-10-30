extern alias legacy;
using System;
using System.IO;
using UltimateEditor.Games;
using UltimateEditor.Uw1;
using Xunit;
using LU = legacy::UltimateEditor.Uw1;

namespace Formats.Tests
{
	/// <summary>
	/// The executables' built-in 3D models (Uw1ExeModels). Changed after the move, so checked here
	/// rather than for parity: concave faces are cut into triangles that cover them exactly (a fan
	/// from the first corner joined the ankh's arms to its foot), and the compact quads (do_compact_
	/// tmap, nodes A0h and A2h) take point numbers, not offsets.
	/// </summary>
	public class ExeModelTests
	{
		static Uw1ExeModels Load(string game)
		{
			string dir = GameData.Copy(game);
			return dir == null ? null : Uw1ExeModels.Load(File.ReadAllBytes(GameData.Exe(dir)));
		}

		static double Length(double[] v) { return Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]); }

		/// <summary>The area of a planar polygon: half its Newell normal's length.</summary>
		static double Area(Uw1ExeModels.Model m, int[] verts) { return Length(Uw1ExeModels.Normal(m, verts)) / 2; }

		/// <summary>The triangles' areas seen along the face's normal, each counted positive: equal
		/// to the face's area when they tile it, more when one spills over a notch (as the inside
		/// of a fan does). Measured along the normal so a slightly bent quad still adds up.</summary>
		static double TriangleSum(Uw1ExeModels.Model m, int[] face, int[][] tris)
		{
			var n = Uw1ExeModels.Normal(m, face);
			double len = Length(n), s = 0;
			if (len == 0) return 0;
			foreach (var t in tris)
			{
				var tn = Uw1ExeModels.Normal(m, t);
				s += Math.Abs(tn[0] * n[0] + tn[1] * n[1] + tn[2] * n[2]) / len / 2;
			}
			return s;
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Every_face_is_covered_exactly_by_its_triangles(string game)
		{
			var models = Load(game);
			if (models == null) return;
			int faces = 0;
			foreach (var m in models.Models)
			{
				Assert.True(m.Error == null, game + " model " + m.Index + ": " + m.Error);
				foreach (var f in m.Faces)
				{
					faces++;
					double area = Area(m, f.Vertices);
					Assert.True(f.Triangles.Length <= Math.Max(0, f.Vertices.Length - 2), game + " model " + m.Index + ": too many triangles");
					// the triangles' areas add up to the polygon's only if none spills outside it
					// or overlaps another (a fan over a notch counts the notch too); 1% for the few
					// bent quads of the boulders, whose two triangles do not lie in one plane
					Assert.True(Math.Abs(TriangleSum(m, f.Vertices, f.Triangles) - area) <= 1e-2 * Math.Max(area, 1e-4),
						string.Format("{0} model {1} ({2}): face of {3} corners, area {4}, triangles {5}", game, m.Index, m.Name, f.Vertices.Length, area, TriangleSum(m, f.Vertices, f.Triangles)));
				}
			}
			Assert.True(faces > 400, game + ": only " + faces + " faces");
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void The_ankh_has_concave_faces_a_fan_would_overfill(string game)
		{
			var models = Load(game);
			if (models == null) return;
			var ankh = models.Models[11];     // the shrine
			int concave = 0;
			foreach (var f in ankh.Faces)
			{
				if (f.Convex) continue;
				concave++;
				var fan = new int[f.Vertices.Length - 2][];
				for (int k = 1; k + 1 < f.Vertices.Length; k++)
					fan[k - 1] = new[] { f.Vertices[0], f.Vertices[k], f.Vertices[k + 1] };
				Assert.True(TriangleSum(ankh, f.Vertices, fan) > Area(ankh, f.Vertices) * 1.01, "a concave face a fan covers exactly");
			}
			// its front and back outlines, and the sides of the loop
			Assert.True(concave >= 2, "ankh concave faces: " + concave);
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Compact_quads_use_point_numbers(string game)
		{
			var models = Load(game);
			if (models == null) return;
			// the pillar is four compact quads; the lever, switch and writing models one each
			var pillar = models.Models[10];
			Assert.Equal(4, pillar.Faces.Count);
			foreach (var f in pillar.Faces)
			{
				Assert.Equal(4, f.Vertices.Length);
				Assert.Equal(4, new System.Collections.Generic.HashSet<int>(f.Vertices).Count);
				Assert.Equal(2, f.Triangles.Length);
			}
			foreach (int i in new[] { 16, 17, 18 })
				Assert.True(Area(models.Models[i], models.Models[i].Faces[0].Vertices) > 0.01, "model " + i + " has no area");
		}

		[GameFact("UW")]
		public void Other_faces_are_the_same_as_before()
		{
			string dir = GameData.Copy("UW");
			if (dir == null) return;
			byte[] exe = File.ReadAllBytes(GameData.Exe(dir));
			var before = LU.Uw1ExeModels.Load(exe);
			var now = Uw1ExeModels.Load(exe);
			for (int i = 0; i < 32; i++)
			{
				var a = before.Models[i]; var b = now.Models[i];
				Assert.Equal(a.Faces.Count, b.Faces.Count);
				for (int k = 0; k < a.Faces.Count; k++)
				{
					var fa = a.Faces[k].Vertices;
					// a compact quad used to read its four point numbers as offsets: all point 0
					if (Array.TrueForAll(fa, v => v == fa[0])) continue;
					Assert.Equal(fa, b.Faces[k].Vertices);
					Assert.Equal(a.Faces[k].Color, b.Faces[k].Color);
				}
			}
		}
	}
}
