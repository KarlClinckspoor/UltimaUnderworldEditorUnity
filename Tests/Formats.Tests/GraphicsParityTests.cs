extern alias legacy;
extern alias current;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Xunit;
using L = legacy::DataReader;
using LW = legacy::DataWriter;
using C = current::DataReader;
using CW = current::DataWriter;

namespace Formats.Tests
{
	/// <summary>Pictures and palettes: the old DataReader/DataWriter code against the moved GraphicsCodec.</summary>
	public class GraphicsParityTests
	{
		static IEnumerable<string> PictureFiles(string dir)
		{
			return Directory.GetFiles(Path.Combine(dir, "DATA"))
				.Where(f => f.EndsWith(".GR", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".TR", StringComparison.OrdinalIgnoreCase))
				.Select(Path.GetFileName);
		}

		[Theory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Offsets_and_raw_pictures_are_the_same(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			L.FilePath = C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			int files = 0;
			foreach (var name in PictureFiles(dir))
			{
				int c1 = 0, f1 = 0, c2 = 0, f2 = 0;
				int[] o1 = L.ReadGraphicOffsets(name, ref c1, ref f1);
				int[] o2 = C.ReadGraphicOffsets(name, ref c2, ref f2);
				Assert.Equal(o1, o2); Assert.Equal(c1, c2); Assert.Equal(f1, f2);
				foreach (int start in new[] { 0, 5 })
				{
					var d1 = new Dictionary<int, Vector2Int>(); var d2 = new Dictionary<int, Vector2Int>();
					Same.Assert(L.ReadRawTextures(name, o1, 0, 0, start, true, d1), C.ReadRawTextures(name, o2, 0, 0, start, true, d2), name + " raw, own sizes");
					Same.Assert(d1, d2, name + " sizes");
					Same.Assert(L.ReadRawTextures(name, o1, 32, 32, start), C.ReadRawTextures(name, o2, 32, 32, start), name + " raw, 32x32");
				}
				files++;
			}
			Assert.True(files > 10);
		}

		[Theory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Four_bit_pictures_decode_and_encode_the_same(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			L.FilePath = C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			int count = 0, first = 0;
			int[] offs = C.ReadGraphicOffsets("OBJECTS.GR", ref count, ref first);
			string path = GameData.InData(dir, "OBJECTS.GR");
			using (var fs = File.OpenRead(path))
			{
				int n = 0;
				for (int i = 0; i < count && offs[i] < fs.Length; i++, n++)
				{
					var a1 = new legacy::AdditionalTextureInfo(true, 0, 0, 0, 0);
					var a2 = new AdditionalTextureInfo(true, 0, 0, 0, 0);
					int[] n1 = L.Read4BitGraphic(fs, offs[i], 6, i, a1);
					int[] n2 = C.Read4BitGraphic(fs, offs[i], 6, i, a2);
					Assert.Equal(n1, n2);
					Same.Assert(a1, a2, "picture " + i + " header");
					int[] r1 = L.Convert4BitToRawTex(n1, a1.Width, a1.Height);
					int[] r2 = C.Convert4BitToRawTex(n2, a2.Width, a2.Height);
					Assert.Equal(r1, r2);
					List<int> e1 = LW.CompressToRLE(r1), e2 = CW.CompressToRLE(r2);
					Assert.Equal(e1, e2);
					Same.Bytes(Write(s => LW.SaveNibbles(s, 0, e1)), Write(s => CW.SaveNibbles(s, 0, e2)), "nibbles of picture " + i);
					Same.Bytes(Write(s => LW.SaveTexture(s, "x", 3, r1, a1)), Write(s => CW.SaveTexture(s, "x", 3, r2, a2)), "SaveTexture of picture " + i);
					int p1 = 0, p2 = 0;
					Same.Assert(LW.getCounts(r1, ref p1), CW.getCounts(r2, ref p2), "run counts of picture " + i);
					Assert.Equal(p1, p2);
					Assert.Equal(L.Downscale(r1, a1.Width, a1.Height), C.Downscale(r2, a2.Width, a2.Height));
				}
				Assert.True(n > 400);
			}
		}

		[Theory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Palettes_are_the_same(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			L.FilePath = C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			string pals = GameData.InData(dir, "PALS.DAT");
			using (var f1 = File.OpenRead(pals))
			using (var f2 = File.OpenRead(pals))
				for (int i = 0; i < 8; i++) Assert.Equal(L.ReadPallette(f1, i), C.ReadPallette(f2, i));
			Same.Assert(L.ReadAuxPalettes(), C.ReadAuxPalettes(), "auxiliary palettes");
			using (var ms1 = new MemoryStream(Enumerable.Range(0, 70000).Select(i => (byte)(i * 7)).ToArray()))
			using (var ms2 = new MemoryStream(ms1.ToArray()))
				Assert.Equal(L.ReadPaletteMapping(ms1, 0, 1, 3), C.ReadPaletteMapping(ms2, 0, 1, 3));
		}

		/// <summary>The editor's whole picture load (every texture list, the palettes, TERRAIN.DAT), through Texture2D.</summary>
		[Theory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void LoadTextures_gives_the_same_textures(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			Application.dataPath = Path.Combine(dir, "unity-data");
			L.FilePath = C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			SetUiManager(typeof(L), typeof(legacy::UIManager));
			SetUiManager(typeof(C), typeof(current::UIManager));
			var a = L.LoadTextures();
			var b = C.LoadTextures();
			Assert.NotNull(b);
			Assert.True(b.Objects.Count > 400);
			Same.Assert(a, b, game + " TextureData");
		}

		static void SetUiManager(Type reader, Type ui)
		{
			var f = reader.GetField("uiManager", BindingFlags.NonPublic | BindingFlags.Static);
			f.SetValue(null, RuntimeHelpers.GetUninitializedObject(ui));
		}

		static byte[] Write(Action<Stream> w)
		{
			using (var ms = new MemoryStream()) { w(ms); return ms.ToArray(); }
		}
	}
}
