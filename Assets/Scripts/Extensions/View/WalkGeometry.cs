using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateEditor.View
{
	/// <summary>Vertices, texture coordinates, colours and triangles of one mesh being built.</summary>
	public sealed class MeshPart
	{
		public readonly List<Vector3> V = new List<Vector3>();
		public readonly List<Vector2> UV = new List<Vector2>();
		public readonly List<Color> C = new List<Color>();
		public readonly List<int> T = new List<int>();

		public int Add(Vector3 v, Vector2 uv, Color c)
		{
			V.Add(v); UV.Add(uv); C.Add(c);
			return V.Count - 1;
		}

		public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Color col)
		{
			int i = Add(a, ua, col), j = Add(b, ub, col), k = Add(c, uc, col);
			T.Add(i); T.Add(j); T.Add(k);
		}

		/// <summary>A quad a-b-c-d (in order round its edge) as two triangles.</summary>
		public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Color col)
		{
			int i = Add(a, ua, col), j = Add(b, ub, col), k = Add(c, uc, col), l = Add(d, ud, col);
			T.Add(i); T.Add(j); T.Add(k);
			T.Add(i); T.Add(k); T.Add(l);
		}
	}

	/// <summary>
	/// The level as the game builds it, for the walk-around view, in tiles: x east, z north, y up.
	/// A floor height step is a quarter of a tile (the renderer's 0x40 units against 0x100 a
	/// tile; most corridors, at height 12, are one tile high) and the ceiling is height 16
	/// (uw1/src/3d/GRIDDB.C grdb_elem). Slopes rise one step across the tile; a diagonal tile is
	/// open in the half its name gives, with a wall along the diagonal. A wall is drawn on the
	/// open side: up to the neighbour's floor, or to the ceiling against rock.
	/// </summary>
	public static class WalkGeometry
	{
		public const float Step = 0.25f;
		public const int CeilingHeight = 16;
		public static float Ceiling { get { return CeilingHeight * Step; } }

		// corners: 0 SW, 1 SE, 2 NE, 3 NW
		static readonly Vector2[] CornerXY = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };

		/// <summary>The floor height of each corner (in steps): a slope's high side is one step up.</summary>
		public static int[] CornerHeights(MapTile t)
		{
			int h = t.FloorHeight;
			switch (t.TileType)
			{
				case TileType.SlopeUpN: return new[] { h, h, h + 1, h + 1 };
				case TileType.SlopeUpS: return new[] { h + 1, h + 1, h, h };
				case TileType.SlopeUpE: return new[] { h, h + 1, h + 1, h };
				case TileType.SlopeUpW: return new[] { h + 1, h, h, h + 1 };
				default: return new[] { h, h, h, h };
			}
		}

		// edges: W, E, S, N as (corner a, corner b, dx, dy, neighbour's corner for a, for b)
		static readonly int[][] Edges =
		{
			new[] { 0, 3, -1, 0, 1, 2 },
			new[] { 1, 2, 1, 0, 0, 3 },
			new[] { 0, 1, 0, -1, 3, 2 },
			new[] { 3, 2, 0, 1, 0, 1 },
		};

		/// <summary>Whether the tile is open along edge e (0 W, 1 E, 2 S, 3 N).</summary>
		public static bool EdgeOpen(TileType type, int e)
		{
			switch (type)
			{
				case TileType.Solid: return false;
				case TileType.OpenSE: return e == 1 || e == 2;
				case TileType.OpenSW: return e == 0 || e == 2;
				case TileType.OpenNE: return e == 1 || e == 3;
				case TileType.OpenNW: return e == 0 || e == 3;
				default: return true;
			}
		}

		/// <summary>Whether point (fx, fy), 0-1 within the tile, is in its open part.</summary>
		public static bool PointOpen(TileType type, float fx, float fy)
		{
			switch (type)
			{
				case TileType.Solid: return false;
				case TileType.OpenSE: return fx >= fy;          // below the SW-NE diagonal
				case TileType.OpenNW: return fx <= fy;
				case TileType.OpenSW: return fx + fy <= 1f;     // below the NW-SE diagonal
				case TileType.OpenNE: return fx + fy >= 1f;
				default: return true;
			}
		}

		/// <summary>The floor height (in tiles) at a point of the tile (fx, fy 0-1).</summary>
		public static float FloorAt(MapTile t, float fx, float fy)
		{
			float h = t.FloorHeight;
			switch (t.TileType)
			{
				case TileType.SlopeUpN: h += fy; break;
				case TileType.SlopeUpS: h += 1 - fy; break;
				case TileType.SlopeUpE: h += fx; break;
				case TileType.SlopeUpW: h += 1 - fx; break;
			}
			return h * Step;
		}

		static Vector3 P(int x, int y, int corner, float height)
		{
			var c = CornerXY[corner];
			return new Vector3(x + c.x, height, y + c.y);
		}

		/// <summary>
		/// Builds the level's surfaces, one part per texture: "floor:n" (the level's floor texture
		/// n), "wall:n" (wall texture n, the open tile's own) and "ceiling".
		/// </summary>
		public static Dictionary<string, MeshPart> Build(MapTile[,] tiles, bool ceiling)
		{
			var parts = new Dictionary<string, MeshPart>();
			Func<string, MeshPart> part = key =>
			{
				MeshPart p;
				if (!parts.TryGetValue(key, out p)) parts[key] = p = new MeshPart();
				return p;
			};
			int w = tiles.GetLength(0), d = tiles.GetLength(1);
			Color floorCol = Color.white, ceilCol = new Color(0.8f, 0.8f, 0.8f), wallNS = Color.white, wallEW = new Color(0.82f, 0.82f, 0.82f), wallDiag = new Color(0.9f, 0.9f, 0.9f);
			for (int x = 0; x < w; x++)
				for (int y = 0; y < d; y++)
				{
					MapTile t = tiles[x, y];
					if (t == null || t.TileType == TileType.Solid)
						continue;
					int[] h = CornerHeights(t);
					// floor and ceiling
					var floor = part("floor:" + t.FloorTexture);
					int[] poly = FloorCorners(t.TileType);
					for (int i = 1; i + 1 < poly.Length; i++)
					{
						int a = poly[0], b = poly[i], c = poly[i + 1];
						floor.Tri(P(x, y, a, h[a] * Step), P(x, y, b, h[b] * Step), P(x, y, c, h[c] * Step),
							UvFloor(x, y, a), UvFloor(x, y, b), UvFloor(x, y, c), floorCol);
						if (ceiling)
							part("ceiling").Tri(P(x, y, a, Ceiling), P(x, y, b, Ceiling), P(x, y, c, Ceiling),
								UvFloor(x, y, a), UvFloor(x, y, b), UvFloor(x, y, c), ceilCol);
					}
					var wall = part("wall:" + t.WallTexture);
					// the diagonal wall
					int da = -1, db = -1;
					if (t.TileType == TileType.OpenSE || t.TileType == TileType.OpenNW) { da = 0; db = 2; }
					else if (t.TileType == TileType.OpenSW || t.TileType == TileType.OpenNE) { da = 3; db = 1; }
					if (da >= 0)
						WallQuad(wall, x, y, da, db, h[da] * Step, h[db] * Step, Ceiling, Ceiling, 0f, 1f, wallDiag);
					// the four edges
					for (int e = 0; e < 4; e++)
					{
						if (!EdgeOpen(t.TileType, e))
							continue;
						var ed = Edges[e];
						int a = ed[0], b = ed[1], nx = x + ed[2], ny = y + ed[3];
						float botA = h[a] * Step, botB = h[b] * Step, topA, topB;
						MapTile n = nx >= 0 && ny >= 0 && nx < w && ny < d ? tiles[nx, ny] : null;
						int opposite = e ^ 1;
						if (n == null || !EdgeOpen(n.TileType, opposite))
						{
							topA = topB = Ceiling;
						}
						else
						{
							int[] nh = CornerHeights(n);
							topA = Math.Max(botA, nh[ed[4]] * Step);
							topB = Math.Max(botB, nh[ed[5]] * Step);
							if (topA <= botA && topB <= botB)
								continue;
						}
						// texture u runs with the world axis along the edge, so walls line up
						float u0 = e < 2 ? y + CornerXY[a].y : x + CornerXY[a].x;
						float u1 = e < 2 ? y + CornerXY[b].y : x + CornerXY[b].x;
						WallQuad(wall, x, y, a, b, botA, botB, topA, topB, u0, u1, e < 2 ? wallEW : wallNS);
					}
				}
			return parts;
		}

		/// <summary>The corners of the tile's floor, round its edge.</summary>
		public static int[] FloorCorners(TileType type)
		{
			switch (type)
			{
				case TileType.OpenSE: return new[] { 0, 1, 2 };
				case TileType.OpenSW: return new[] { 0, 1, 3 };
				case TileType.OpenNE: return new[] { 3, 1, 2 };
				case TileType.OpenNW: return new[] { 0, 2, 3 };
				default: return new[] { 0, 1, 2, 3 };
			}
		}

		static Vector2 UvFloor(int x, int y, int corner)
		{
			var c = CornerXY[corner];
			return new Vector2(x + c.x, y + c.y);
		}

		/// <summary>A wall from corner a to corner b of tile (x, y), bottom to top at each end
		/// (in tiles); its texture is one tile high, placed by absolute height.</summary>
		static void WallQuad(MeshPart p, int x, int y, int a, int b, float botA, float botB, float topA, float topB, float u0, float u1, Color col)
		{
			p.Quad(P(x, y, a, botA), P(x, y, b, botB), P(x, y, b, topB), P(x, y, a, topA),
				new Vector2(u0, botA), new Vector2(u1, botB), new Vector2(u1, topB), new Vector2(u0, topA), col);
		}

		/// <summary>Whether a viewer of radius r at (px, pz), in tiles, stands in open space whose
		/// floor is at most maxRise above floorNow; the floor there comes out in floorAt.</summary>
		public static bool CanStand(MapTile[,] tiles, float px, float pz, float r, float floorNow, float maxRise, out float floorAt)
		{
			floorAt = floorNow;
			float best = -1f;
			// the centre decides the floor; the four points r away must be open too
			float[,] probes = { { 0, 0 }, { r, 0 }, { -r, 0 }, { 0, r }, { 0, -r } };
			for (int i = 0; i < probes.GetLength(0); i++)
			{
				float qx = px + probes[i, 0], qz = pz + probes[i, 1];
				int tx = Mathf.FloorToInt(qx), tz = Mathf.FloorToInt(qz);
				if (tx < 0 || tz < 0 || tx >= tiles.GetLength(0) || tz >= tiles.GetLength(1)) return false;
				MapTile t = tiles[tx, tz];
				float fx = qx - tx, fz = qz - tz;
				if (t == null || !PointOpen(t.TileType, fx, fz)) return false;
				float f = FloorAt(t, fx, fz);
				if (f > floorNow + maxRise) return false;
				if (i == 0) best = f;
			}
			floorAt = best;
			return true;
		}
	}
}
