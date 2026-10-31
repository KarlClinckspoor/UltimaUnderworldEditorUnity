extern alias current;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UltimateEditor.Games;
using UltimateEditor.Uw1;
using UnityEngine;
using Xunit;
using C = current::DataReader;
using CLevArk = current::LevArk;
using CMap = current::MapCreator;
using CObj = current::StaticObject;
using W = current::UltimateEditor.View.WalkGeometry;
using WO = current::UltimateEditor.View.WalkObjects;

namespace Formats.Tests
{
	/// <summary>The walk-around view's level geometry and object classes (Extensions/View).</summary>
	public class WalkViewTests
	{
		static List<current::LevelData> Load(string game)
		{
			string dir = GameData.Copy(game);
			Application.dataPath = Path.Combine(dir, "unity-data");
			Application.streamingAssetsPath = Path.Combine(Repo.Root, "Assets", "StreamingAssets");
			C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			typeof(C).GetField("uiManager", BindingFlags.NonPublic | BindingFlags.Static)
				.SetValue(null, RuntimeHelpers.GetUninitializedObject(typeof(current::UIManager)));
			var levels = CLevArk.Load(C.FilePath);
			typeof(CMap).GetProperty("LevelData").SetValue(null, levels);
			CMap.ObjectToGO = new Dictionary<CObj, GameObject>();
			return levels;
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Every_level_builds_closed_finite_geometry(string game)
		{
			if (!GameData.Has(game)) return;
			foreach (var ld in Load(game).Where(l => l.Present))
			{
				var parts = W.Build(ld.MapTiles, true);
				Assert.Contains(parts.Keys, k => k.StartsWith("floor:"));
				Assert.Contains(parts.Keys, k => k.StartsWith("wall:"));
				foreach (var p in parts.Values)
				{
					Assert.Equal(0, p.T.Count % 3);
					Assert.Equal(p.V.Count, p.UV.Count);
					Assert.Equal(p.V.Count, p.C.Count);
					Assert.All(p.T, i => Assert.InRange(i, 0, p.V.Count - 1));
					Assert.All(p.V, v =>
					{
						Assert.InRange(v.x, 0f, 64f);
						Assert.InRange(v.z, 0f, 64f);
						Assert.InRange(v.y, 0f, W.Ceiling);
					});
				}
			}
		}

		[Fact]
		public void Walls_stand_where_the_floor_steps_and_against_rock_only()
		{
			// a 3x3 map: rock round an open square at height 4, its east neighbour at height 6
			var tiles = new current::MapTile[4, 3];
			for (int x = 0; x < 4; x++)
				for (int y = 0; y < 3; y++)
					tiles[x, y] = new current::MapTile(current::TileType.Solid, 1, x, y, 0, 0, 0);
			tiles[1, 1] = new current::MapTile(current::TileType.Open, 1, 1, 1, 4, 0, 1);
			tiles[2, 1] = new current::MapTile(current::TileType.Open, 1, 2, 1, 6, 0, 2);
			var parts = W.Build(tiles, false);
			// tile (1,1): rock on three sides (3 full walls) and the step up east (1 wall)
			Assert.Equal(4 * 6, parts["wall:1"].T.Count);
			// tile (2,1): rock on three sides; its west neighbour is lower, so no wall there
			Assert.Equal(3 * 6, parts["wall:2"].T.Count);
			var step = parts["wall:1"].V.Where(v => Math.Abs(v.x - 2f) < 1e-5).Select(v => v.y).ToList();
			Assert.Contains(6 * W.Step, step);
			Assert.Contains(W.Ceiling, parts["wall:2"].V.Select(v => v.y));
		}

		[Fact]
		public void Diagonal_and_sloped_tiles_have_the_right_open_part_and_floor()
		{
			Assert.True(W.PointOpen(current::TileType.OpenSE, 0.9f, 0.1f));
			Assert.False(W.PointOpen(current::TileType.OpenSE, 0.1f, 0.9f));
			Assert.True(W.PointOpen(current::TileType.OpenNE, 0.9f, 0.9f));
			Assert.False(W.PointOpen(current::TileType.OpenNE, 0.1f, 0.1f));
			var slope = new current::MapTile(current::TileType.SlopeUpN, 1, 0, 0, 3, 0, 0);
			Assert.Equal(3 * W.Step, W.FloorAt(slope, 0.5f, 0f), 3);
			Assert.Equal(4 * W.Step, W.FloorAt(slope, 0.5f, 1f), 3);
			Assert.Equal(new[] { 3, 3, 4, 4 }, W.CornerHeights(slope));
		}

		[Theory]
		[InlineData(0x170, WO.Kind.TmapLever, 0x14)]
		[InlineData(0x16E, WO.Kind.TmapWall, 0x16)]
		[InlineData(0x166, WO.Kind.Model, 0x12)]
		[InlineData(0x165, WO.Kind.Model, 0x13)]
		[InlineData(0x140, WO.Kind.Door, 0x0E)]
		[InlineData(0x147, WO.Kind.Door, 0x0F)]
		[InlineData(0x190, WO.Kind.None, -1)]
		[InlineData(0x1A0, WO.Kind.None, -1)]
		[InlineData(0x040, WO.Kind.Critter, -1)]
		[InlineData(0x0A0, WO.Kind.Sprite, -1)]
		public void Objects_are_drawn_as_the_game_draws_them(int item, WO.Kind kind, int model)
		{
			if (!GameData.Has("UW")) return;
			var render = WO.ReadRenderTypes(File.ReadAllBytes(Path.Combine(GameData.Root, "UW", "DATA", "COMOBJ.DAT")));
			int m;
			Assert.Equal(kind, WO.Classify(item, render, false, out m));
			Assert.Equal(model, m);
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void The_built_in_models_turn_into_triangles(string game)
		{
			if (!GameData.Has(game)) return;
			string exe = Path.Combine(GameData.Root, game, game == "UW2" ? "UW2.EXE" : "UW.EXE");
			var models = Uw1ExeModels.Load(File.ReadAllBytes(exe));
			foreach (int i in new[] { 0x01, 0x0E, 0x14, 0x16, 0x18, 0x1B })
			{
				var plain = new current::UltimateEditor.View.MeshPart();
				var tex = new current::UltimateEditor.View.MeshPart();
				WO.AddModel(models.Models[i], new Vector3(5, 1, 5), 90f, null, plain, tex, Color.white);
				Assert.True(plain.T.Count + tex.T.Count > 0, "model " + i);
				Assert.All(tex.UV, uv => { Assert.InRange(uv.x, -0.001f, 1.001f); Assert.InRange(uv.y, -0.001f, 1.001f); });
			}
			// the writing's texture is TMOBJ 20 + flags (DRAWOBJ.C: rect_cols for model 0x12)
			bool floor;
			Assert.Equal(game == "UW2" ? 0x14 + 0x10 + 3 : 20 + 0x10 + 3, WO.ModelTexture(0x12, 3, game == "UW2", out floor));
			Assert.False(floor);
		}
	}
}
