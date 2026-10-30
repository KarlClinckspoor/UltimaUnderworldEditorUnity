extern alias legacy;
extern alias current;
using System.IO;
using Xunit;
using L = legacy::DataReader;
using LW = legacy::DataWriter;
using C = current::DataReader;
using CW = current::DataWriter;

namespace Formats.Tests
{
	/// <summary>OBJECTS.DAT, COMOBJ.DAT and the critter art table: the old code against ObjectDataCodec.</summary>
	public class ObjectDataParityTests
	{
		static void SetGame(string dir)
		{
			L.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			legacy::LevArk.Game = legacy::UltimateEditor.Games.GameProfile.For(legacy::LevArk.DetectFor(L.FilePath));
		}

		static void SetGameCurrent(string dir)
		{
			C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK");
			current::LevArk.Game = UltimateEditor.Games.GameProfile.For(current::LevArk.DetectFor(C.FilePath));
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Reading_gives_the_same_data(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			SetGame(dir); SetGameCurrent(dir);
			var a = L.LoadObjectData();
			var b = C.LoadObjectData();
			Assert.Equal(512, b.CommonData.Length);
			Same.Assert(a, b, game + " ObjectData");
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Writing_gives_the_same_files(string game)
		{
			string d1 = GameData.Copy(game), d2 = GameData.Copy(game);
			if (d1 == null) return;
			SetGame(d1); SetGameCurrent(d2);
			LW.SaveObjectDatas(L.LoadObjectData());
			CW.SaveObjectDatas(C.LoadObjectData());
			foreach (var rel in new[] { "DATA/OBJECTS.DAT", "DATA/COMOBJ.DAT", "CRIT/ASSOC.ANM", "CRIT/AS.AN" })
			{
				string p1 = Path.Combine(d1, rel), p2 = Path.Combine(d2, rel);
				Assert.Equal(File.Exists(p1), File.Exists(p2));
				if (File.Exists(p1)) Same.Bytes(File.ReadAllBytes(p1), File.ReadAllBytes(p2), game + " " + rel);
			}
			// the old writer always rewrote them, with .BKP copies; now an unchanged file is not written
			Assert.Empty(Directory.GetFiles(d2, "*.BKP", SearchOption.AllDirectories));
		}
	}
}
