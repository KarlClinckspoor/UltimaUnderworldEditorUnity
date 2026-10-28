using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UltimateEditor.Modding
{
	/// <summary>
	/// A .uwmod file: a zip holding mod.json, an optional README.md, screenshots and the payload
	/// the components name.
	/// </summary>
	public sealed class ModPackage
	{
		public const string Extension = ".uwmod";
		public const string ManifestName = "mod.json", ReadmeName = "README.md";

		public ModManifest Manifest;
		public readonly SortedDictionary<string, byte[]> Files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);

		public string Readme
		{
			get { byte[] b; return Files.TryGetValue(ReadmeName, out b) ? Encoding.UTF8.GetString(b) : ""; }
			set { if (string.IsNullOrEmpty(value)) Files.Remove(ReadmeName); else Files[ReadmeName] = Encoding.UTF8.GetBytes(value); }
		}

		public byte[] Payload(string path)
		{
			byte[] b;
			if (path == null || !Files.TryGetValue(path, out b)) throw new ModApplyException("the package has no " + (path ?? "(no path)"));
			return b;
		}

		public byte[] ToBytes()
		{
			var entries = new List<MiniZip.Entry>();
			entries.Add(new MiniZip.Entry(ManifestName, Encoding.UTF8.GetBytes(Manifest.ToJson())));
			foreach (var kv in Files) if (kv.Key != ManifestName) entries.Add(new MiniZip.Entry(kv.Key, kv.Value));
			return MiniZip.Write(entries);
		}

		public void Save(string path) { File.WriteAllBytes(path, ToBytes()); }

		public static ModPackage Load(string path) { return FromBytes(File.ReadAllBytes(path)); }

		public static ModPackage FromBytes(byte[] zip)
		{
			var p = new ModPackage();
			foreach (var e in MiniZip.Read(zip))
			{
				string name = e.Name.Replace('\\', '/');
				if (name.StartsWith("/") || name.Contains("../") || name.Contains(":")) throw new InvalidDataException("unsafe path in the package: " + e.Name);
				if (name == ManifestName) p.Manifest = ModManifest.Parse(Encoding.UTF8.GetString(e.Data));
				else p.Files[name] = e.Data;
			}
			if (p.Manifest == null) throw new InvalidDataException("the package has no mod.json");
			return p;
		}

		/// <summary>Problems with the package itself (payload it names but lacks, odd paths).</summary>
		public List<string> Check()
		{
			var r = new List<string>();
			var m = Manifest;
			if (string.IsNullOrEmpty(m.Id)) r.Add("the mod has no id");
			if (string.IsNullOrEmpty(m.Name)) r.Add("the mod has no name");
			if (m.Components.Count == 0) r.Add("the mod changes nothing");
			foreach (var c in m.Components)
				foreach (var path in PayloadOf(c))
					if (!Files.ContainsKey(path)) r.Add(c.Summary + ": the package lacks " + path);
			foreach (var s in m.Screenshots) if (!Files.ContainsKey(s)) r.Add("the package lacks the screenshot " + s);
			return r;
		}

		public static IEnumerable<string> PayloadOf(ModComponent c)
		{
			var a = c as ArkBlocksComponent;
			if (a != null) foreach (var b in a.Blocks) if (!b.Delete) yield return b.Data;
			var s = c as StringsComponent;
			if (s != null) yield return s.Data;
			var i = c as ImagesComponent;
			if (i != null) foreach (var p in i.Pictures) yield return p.Data;
			var f = c as FileComponent;
			if (f != null && !f.Delete) yield return f.Data;
			var sv = c as SaveComponent;
			if (sv != null) foreach (var x in sv.Files) yield return x.Data;
		}
	}
}
