extern alias legacy;
using System.IO;
using System.Linq;
using UltimateEditor.Games;
using UltimateEditor.Uw1;
using Xunit;
using LG = legacy::UltimateEditor.Games;
using LU = legacy::UltimateEditor.Uw1;

namespace Formats.Tests
{
	/// <summary>The smaller files whose code moved unchanged: run both on the shipped files.</summary>
	public class SmallFormatsParityTests
	{
		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Picture_files_decode_and_rebuild_the_same(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			foreach (var path in Directory.GetFiles(Path.Combine(dir, "DATA"), "*.GR"))
			{
				byte[] f = File.ReadAllBytes(path);
				if (f.Length < 3 || f[0] != GrFile.TypeByte) continue;
				int[] o = GrFile.Offsets(f);
				Assert.Equal(LG.GrFile.Offsets(f), o);
				for (int i = 0; i + 1 < o.Length; i++)
				{
					if (o[i] >= f.Length || o[i + 1] > f.Length) continue;
					object pa = null, pb = null;
					try { pa = LG.GrFile.Decode(f, o, i); } catch (System.Exception e) { pa = e.GetType().Name; }
					try { pb = GrFile.Decode(f, o, i); } catch (System.Exception e) { pb = e.GetType().Name; }
					Same.Assert(pa, pb, Path.GetFileName(path) + " picture " + i);
				}
				if (GrFile.Contiguous(f, o))
					Same.Bytes(LG.GrFile.Rebuild(f, new System.Collections.Generic.Dictionary<int, LG.GrFile.Picture>()), GrFile.Rebuild(f, new System.Collections.Generic.Dictionary<int, GrFile.Picture>()), Path.GetFileName(path) + " rebuilt");
			}
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Exe_start_square_is_the_same(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			byte[] exe = File.ReadAllBytes(GameData.Exe(dir));
			var kind = game == "UW2" ? GameKind.UW2 : GameKind.UW1;
			int x1, y1, x2, y2;
			bool r1 = LG.StartPosition.Read(exe, game == "UW2" ? LG.GameKind.UW2 : LG.GameKind.UW1, out x1, out y1);
			bool r2 = StartPosition.Read(exe, kind, out x2, out y2);
			Assert.True(r2);
			Assert.Equal(r1, r2); Assert.Equal(x1, x2); Assert.Equal(y1, y2);
			if (kind == GameKind.UW1) Assert.Equal((0x20, 2), (x2, y2));
			else Assert.Equal((0x13, 0x30), (x2, y2));
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Small_data_files_parse_and_write_the_same(string game)
		{
			string dir = GameData.Copy(game);
			if (dir == null) return;
			byte[] skills = File.ReadAllBytes(GameData.InData(dir, "SKILLS.DAT"));
			byte[] cmb = File.ReadAllBytes(GameData.InData(dir, "CMB.DAT"));
			byte[] shades = File.ReadAllBytes(GameData.InData(dir, "SHADES.DAT"));
			byte[] babg = File.ReadAllBytes(GameData.InData(dir, "BABGLOBS.DAT"));
			var s = SkillsDat.Parse(skills);
			Same.Assert(LU.SkillsDat.Parse(skills), s, "SKILLS.DAT");
			Assert.Equal(LU.SkillsDat.Parse(skills).ToBytes(), s.ToBytes());
			var c = CmbDat.Parse(cmb);
			Same.Assert(LU.CmbDat.Parse(cmb), c, "CMB.DAT");
			Assert.Equal(LU.CmbDat.Parse(cmb).ToBytes(), c.ToBytes());
			var sh = ShadesDat.Parse(shades);
			Same.Assert(LU.ShadesDat.Parse(shades), sh, "SHADES.DAT");
			Assert.Equal(shades, sh.ToBytes());
			var g = BGlobals.Parse(babg, false);
			Same.Assert(LG.BGlobals.Parse(babg, false), g, "BABGLOBS.DAT");
			Assert.Equal(babg, g.ToBytes(false));
			// DATA's PLAYER.DAT is not a saved game (UW1 and UW2 ship 301 bytes); both must refuse it alike
			string pd = GameData.InData(dir, "PLAYER.DAT");
			var kind = game == "UW2" ? GameKind.UW2 : GameKind.UW1;
			var lkind = game == "UW2" ? LG.GameKind.UW2 : LG.GameKind.UW1;
			if (pd != null)
			{
				byte[] p = File.ReadAllBytes(pd);
				Assert.Equal(Outcome(() => LG.PlayerDat.Parse(p, lkind)), Outcome(() => PlayerDat.Parse(p, kind)));
				Assert.Equal(LG.PlayerDat.Detect(p).HasValue, PlayerDat.Detect(p).HasValue);
			}
			// the ciphers, on a made-up record of the right size
			int n = PlayerDat.RecordSizeFor(kind);
			Assert.Equal(LG.PlayerDat.RecordSizeFor(lkind), n);
			var rec = Enumerable.Range(0, n).Select(i => (byte)(i * 37 + 11)).ToArray();
			foreach (byte key in new byte[] { 0xAA ^ (byte)'K', 0x55, 0 })
			{
				Assert.Equal(LG.PlayerDat.Cipher(rec, key, lkind, true), PlayerDat.Cipher(rec, key, kind, true));
				Assert.Equal(LG.PlayerDat.Cipher(rec, key, lkind, false), PlayerDat.Cipher(rec, key, kind, false));
			}
		}

		static string Outcome(System.Func<object> f)
		{
			try { var r = f(); return r == null ? "null" : "ok"; }
			catch (System.Exception e) { return e.GetType().Name + ": " + e.Message; }
		}
	}
}
