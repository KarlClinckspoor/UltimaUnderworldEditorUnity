using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UltimateEditor.Games;

namespace UltimateEditor.Modding
{
	/// <summary>
	/// Saved games as mini mods. A save is a SAVEn folder next to DATA (n = 1-4; SAVE0 is the
	/// game in progress): LEV.ARK with the automap and notes, PLAYER.DAT, BGLOBALS.DAT and DESC,
	/// in UW2 also SCD.ARK.
	/// </summary>
	public static class SaveMods
	{
		public static readonly string[] Uw1Files = { "LEV.ARK", "PLAYER.DAT", "BGLOBALS.DAT", "DESC" };
		public static readonly string[] Uw2Files = { "LEV.ARK", "PLAYER.DAT", "BGLOBALS.DAT", "DESC", "SCD.ARK" };
		/// <summary>DATA files a save is read with.</summary>
		public static readonly string[] DependsOnFiles = { "DATA/STRINGS.PAK", "DATA/CNV.ARK", "DATA/OBJECTS.DAT", "DATA/COMOBJ.DAT" };

		public static string[] FilesFor(GameKind g) { return g == GameKind.UW2 ? Uw2Files : Uw1Files; }

		/// <summary>The save in <paramref name="saveDir"/> as a component, its payload added to
		/// <paramref name="payload"/>. <paramref name="game"/> gives DATA's files (for the dependencies).</summary>
		public static SaveComponent Export(string saveDir, IFileSource game, GameKind kind, int slot, IDictionary<string, byte[]> payload)
		{
			var c = new SaveComponent { Slot = slot };
			var folder = new GameFolder(saveDir);
			foreach (var name in FilesFor(kind))
			{
				string p = folder.Resolve(name);
				if (p == null)
				{
					if (name == "DESC") continue;
					throw new FileNotFoundException("the save has no " + name, Path.Combine(saveDir, name));
				}
				byte[] b = File.ReadAllBytes(p);
				string data = "payload/save/" + name;
				payload[data] = b;
				c.Files.Add(new SaveComponent.SaveFile { Name = name, Data = data });
				if (name == "DESC") c.Description = Encoding.ASCII.GetString(b).TrimEnd('\0', '\r', '\n', ' ');
			}
			if (game != null)
				foreach (var d in DependsOnFiles)
				{
					var b = game.Read(d);
					if (b != null) c.DependsOn[d] = Hashing.Sha256(b);
				}
			return c;
		}

		/// <summary>Problems installing a save with these DATA files: DATA files that are not the
		/// ones it was made with, and conversation records of the wrong size.</summary>
		public static List<string> Check(SaveComponent c, ModPackage pkg, IFileSource game, GameKind kind)
		{
			var r = new List<string>();
			foreach (var kv in c.DependsOn)
			{
				var b = game.Read(kv.Key);
				if (b == null) r.Add(kv.Key + " is missing");
				else if (Hashing.Sha256(b) != kv.Value) r.Add(kv.Key + " is not the one the save was made with (another mod, or none?)");
			}
			var bg = c.Files.Find(f => f.Name == "BGLOBALS.DAT");
			byte[] cnv = game.Read("DATA/CNV.ARK");
			if (bg != null && cnv != null)
			{
				var g = BGlobals.Parse(pkg.Payload(bg.Data), true);
				var ar = Archive.Open(cnv, kind);
				foreach (var rec in g.Records)
				{
					if (!ar.Has(rec.Slot)) continue;
					byte[] blk = ar.Read(rec.Slot);
					if (blk.Length < 0x10) continue;
					int n = blk[0xC] | blk[0xD] << 8;
					if (n != rec.Words.Length) r.Add(string.Format("conversation {0} keeps {1} variables in the save but its script has {2}", rec.Slot, rec.Words.Length, n));
				}
			}
			return r;
		}

		/// <summary>Writes the save into a slot folder (made if needed), replacing what was there.</summary>
		public static void Install(SaveComponent c, ModPackage pkg, string slotDir)
		{
			Directory.CreateDirectory(slotDir);
			var folder = new GameFolder(slotDir);
			var names = new List<string>(Uw1Files); names.AddRange(Uw2Files);
			foreach (var f in c.Files)
				if (f.Name == null || names.IndexOf(f.Name.ToUpperInvariant()) < 0) throw new ModApplyException("a saved game has no file " + f.Name);
			foreach (var f in c.Files)
			{
				string p = GameFolder.Inside(slotDir, folder.Resolve(f.Name) ?? Path.Combine(slotDir, f.Name.ToUpperInvariant()));
				string tmp = p + ".uwmod-tmp";
				File.WriteAllBytes(tmp, pkg.Payload(f.Data));
				if (File.Exists(p)) File.Delete(p);
				File.Move(tmp, p);
			}
		}
	}
}
