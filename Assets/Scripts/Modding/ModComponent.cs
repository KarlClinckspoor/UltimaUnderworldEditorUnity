using System;
using System.Collections.Generic;
using UltimateEditor.Uw1.Zoning;

namespace UltimateEditor.Modding
{
	/// <summary>One component of a mod: the changes to one file, or one change to the executable.</summary>
	public abstract class ModComponent
	{
		public const string ArkBlocksKind = "ark-blocks", StringsKind = "strings", BytesKind = "bytes", ImagesKind = "images",
			FileKind = "file", ExeParamKind = "exe-param", ExeBytesKind = "exe-bytes", SaveKind = "save";

		public abstract string Kind { get; }
		/// <summary>The game file it changes, relative to the game folder ("DATA/LEV.ARK"); null for exe-param and save.</summary>
		public string File;

		/// <summary>One line for lists: "LEV.ARK: 3 blocks".</summary>
		public abstract string Summary { get; }

		public Dictionary<string, object> ToJson()
		{
			var o = new Dictionary<string, object>();
			o["kind"] = Kind;
			if (File != null) o["file"] = File;
			WriteFields(o);
			return o;
		}

		protected abstract void WriteFields(Dictionary<string, object> o);
		protected abstract void ReadFields(Dictionary<string, object> o);

		public static ModComponent FromJson(Dictionary<string, object> o)
		{
			if (o == null) throw new FormatException("a component is not a JSON object");
			string kind = MiniJson.Str(o, "kind");
			ModComponent c;
			switch (kind)
			{
				case ArkBlocksKind: c = new ArkBlocksComponent(); break;
				case StringsKind: c = new StringsComponent(); break;
				case BytesKind: c = new BytesComponent(); break;
				case ImagesKind: c = new ImagesComponent(); break;
				case FileKind: c = new FileComponent(); break;
				case ExeParamKind: c = new ExeParamComponent(); break;
				case ExeBytesKind: c = new ExeBytesComponent(); break;
				case SaveKind: c = new SaveComponent(); break;
				default: throw new FormatException("unknown component kind \"" + kind + "\" (a newer mod format?)");
			}
			c.File = MiniJson.Get(o, "file") as string;
			c.ReadFields(o);
			return c;
		}

		protected static Dictionary<string, object> O(object o) { return MiniJson.Obj(o) ?? new Dictionary<string, object>(); }
		protected static List<object> A(object o) { return MiniJson.Arr(o) ?? new List<object>(); }
		protected static int I(Dictionary<string, object> o, string k, int def = 0) { return ModManifest.Int(MiniJson.Get(o, k), def); }
		protected static string S(Dictionary<string, object> o, string k, string def = null) { var v = MiniJson.Get(o, k); return v == null ? def : v.ToString(); }
	}

	/// <summary>Blocks of a .ARK archive, uncompressed. UW1 archives are offset tables; UW2's
	/// compress blocks with LZSS. Blocks the mod does not list stay as they are.</summary>
	public sealed class ArkBlocksComponent : ModComponent
	{
		public sealed class Block
		{
			public int Index;
			/// <summary>Hash of the original block's (uncompressed) bytes, or "absent".</summary>
			public string Base;
			/// <summary>Payload path of the new bytes; null when Delete.</summary>
			public string Data;
			public bool Delete;
		}

		public override string Kind { get { return ArkBlocksKind; } }
		/// <summary>"uw1" or "uw2": the archive's layout.</summary>
		public string Archive = "uw1";
		public List<Block> Blocks = new List<Block>();

		public override string Summary { get { return string.Format("{0}: {1} block{2}", File, Blocks.Count, Blocks.Count == 1 ? "" : "s"); } }

		protected override void WriteFields(Dictionary<string, object> o)
		{
			o["archive"] = Archive;
			var l = new List<object>();
			foreach (var b in Blocks)
			{
				var e = new Dictionary<string, object>();
				e["index"] = b.Index;
				e["base"] = b.Base;
				if (b.Delete) e["delete"] = true; else e["data"] = b.Data;
				l.Add(e);
			}
			o["blocks"] = l;
		}

		protected override void ReadFields(Dictionary<string, object> o)
		{
			Archive = S(o, "archive", "uw1");
			foreach (var x in A(MiniJson.Get(o, "blocks")))
			{
				var e = O(x);
				Blocks.Add(new Block { Index = I(e, "index"), Base = S(e, "base", Hashing.Absent), Data = S(e, "data"), Delete = MiniJson.Bool(e, "delete") });
			}
		}
	}

	/// <summary>STRINGS.PAK: changed, added and removed strings, by block and index. The payload
	/// (a JSON file) holds them; the file is re-encoded with its own Huffman tree, and blocks
	/// without changes keep their bytes.</summary>
	public sealed class StringsComponent : ModComponent
	{
		public override string Kind { get { return StringsKind; } }
		/// <summary>Payload path of the JSON changes (see StringChanges).</summary>
		public string Data;
		/// <summary>For the summary only: strings changed or added.</summary>
		public int Count;

		public override string Summary { get { return string.Format("{0}: {1} string{2}", File, Count, Count == 1 ? "" : "s"); } }
		protected override void WriteFields(Dictionary<string, object> o) { o["data"] = Data; o["count"] = Count; }
		protected override void ReadFields(Dictionary<string, object> o) { Data = S(o, "data"); Count = I(o, "count"); }
	}

	/// <summary>The changes of StringsComponent, one per block.</summary>
	public sealed class StringChanges
	{
		public sealed class BlockChange
		{
			public int Block;
			/// <summary>The block was not in the original file / is removed by the mod.</summary>
			public bool Added, Removed;
			/// <summary>Number of strings in the original block, and in the mod's.</summary>
			public int BaseCount, Count;
			public List<StringSet> Set = new List<StringSet>();
			/// <summary>Hashes of the original strings the mod drops (those from Count on, or all of
			/// a removed block), so applying checks them like the ones it changes. Empty in
			/// packages made before it was added.</summary>
			public List<string> RemovedBase = new List<string>();
		}

		public sealed class StringSet
		{
			public int Index;
			/// <summary>Hash of the original text, or "absent" for a string past the original count.</summary>
			public string Base;
			public string Text;
		}

		public List<BlockChange> Blocks = new List<BlockChange>();

		public int StringCount { get { int n = 0; foreach (var b in Blocks) n += b.Set.Count; return n; } }

		public string ToJson()
		{
			var l = new List<object>();
			foreach (var b in Blocks)
			{
				var e = new Dictionary<string, object>();
				e["block"] = b.Block;
				if (b.Added) e["added"] = true;
				if (b.Removed) e["removed"] = true;
				e["base_count"] = b.BaseCount;
				e["count"] = b.Count;
				var s = new List<object>();
				foreach (var x in b.Set)
				{
					var d = new Dictionary<string, object>();
					d["index"] = x.Index;
					d["base"] = x.Base;
					d["text"] = x.Text;
					s.Add(d);
				}
				e["set"] = s;
				if (b.RemovedBase.Count > 0)
				{
					var rb = new List<object>();
					foreach (var h in b.RemovedBase) rb.Add(h);
					e["removed_base"] = rb;
				}
				l.Add(e);
			}
			var o = new Dictionary<string, object>();
			o["blocks"] = l;
			return MiniJson.Write(o, true) + "\n";
		}

		public static StringChanges Parse(string json)
		{
			var r = new StringChanges();
			var o = MiniJson.Obj(MiniJson.Parse(json));
			if (o == null) throw new FormatException("string changes are not a JSON object");
			foreach (var x in MiniJson.Arr(MiniJson.Get(o, "blocks")) ?? new List<object>())
			{
				var e = MiniJson.Obj(x);
				var b = new BlockChange
				{
					Block = ModManifest.Int(MiniJson.Get(e, "block")),
					Added = MiniJson.Bool(e, "added"),
					Removed = MiniJson.Bool(e, "removed"),
					BaseCount = ModManifest.Int(MiniJson.Get(e, "base_count")),
					Count = ModManifest.Int(MiniJson.Get(e, "count")),
				};
				foreach (var y in MiniJson.Arr(MiniJson.Get(e, "set")) ?? new List<object>())
				{
					var d = MiniJson.Obj(y);
					b.Set.Add(new StringSet { Index = ModManifest.Int(MiniJson.Get(d, "index")), Base = MiniJson.Str(d, "base", Hashing.Absent), Text = MiniJson.Str(d, "text") });
				}
				foreach (var h in MiniJson.Arr(MiniJson.Get(e, "removed_base")) ?? new List<object>())
					b.RemovedBase.Add(h as string);
				r.Blocks.Add(b);
			}
			return r;
		}
	}

	/// <summary>Byte ranges of a file whose length the mod keeps (OBJECTS.DAT, palettes, ...).</summary>
	public sealed class BytesComponent : ModComponent
	{
		public sealed class Range
		{
			public int Offset;
			/// <summary>Hex of the original bytes and of the new ones (same length).</summary>
			public string Base, Data;
		}

		public override string Kind { get { return BytesKind; } }
		/// <summary>The file's length, which stays.</summary>
		public int Length;
		public List<Range> Ranges = new List<Range>();

		public int ByteCount { get { int n = 0; foreach (var r in Ranges) n += r.Data.Length / 2; return n; } }
		public override string Summary { get { return string.Format("{0}: {1} byte{2} in {3} range{4}", File, ByteCount, ByteCount == 1 ? "" : "s", Ranges.Count, Ranges.Count == 1 ? "" : "s"); } }

		protected override void WriteFields(Dictionary<string, object> o)
		{
			o["length"] = Length;
			var l = new List<object>();
			foreach (var r in Ranges)
			{
				var e = new Dictionary<string, object>();
				e["offset"] = r.Offset;
				e["base"] = r.Base;
				e["data"] = r.Data;
				l.Add(e);
			}
			o["ranges"] = l;
		}

		protected override void ReadFields(Dictionary<string, object> o)
		{
			Length = I(o, "length");
			foreach (var x in A(MiniJson.Get(o, "ranges")))
			{
				var e = O(x);
				Ranges.Add(new Range { Offset = I(e, "offset"), Base = S(e, "base", ""), Data = S(e, "data", "") });
			}
		}
	}

	/// <summary>Pictures of a .GR or .TR file, by index. The file is rebuilt from its pictures in
	/// order, the unchanged ones byte for byte.</summary>
	public sealed class ImagesComponent : ModComponent
	{
		public sealed class Picture
		{
			public int Index;
			/// <summary>Hash of the original picture record (its bytes between two offsets), or "absent".</summary>
			public string Base;
			public string Data;
		}

		public override string Kind { get { return ImagesKind; } }
		/// <summary>Number of pictures in the original and in the mod's file.</summary>
		public int BaseCount, Count;
		public List<Picture> Pictures = new List<Picture>();
		/// <summary>Hashes of the original pictures the mod drops (from Count on), checked when
		/// applying. Empty in packages made before it was added.</summary>
		public List<string> RemovedBase = new List<string>();

		public override string Summary { get { return string.Format("{0}: {1} picture{2}", File, Pictures.Count, Pictures.Count == 1 ? "" : "s"); } }

		protected override void WriteFields(Dictionary<string, object> o)
		{
			o["base_count"] = BaseCount;
			o["count"] = Count;
			var l = new List<object>();
			foreach (var p in Pictures)
			{
				var e = new Dictionary<string, object>();
				e["index"] = p.Index;
				e["base"] = p.Base;
				e["data"] = p.Data;
				l.Add(e);
			}
			o["pictures"] = l;
			if (RemovedBase.Count > 0)
			{
				var rb = new List<object>();
				foreach (var h in RemovedBase) rb.Add(h);
				o["removed_base"] = rb;
			}
		}

		protected override void ReadFields(Dictionary<string, object> o)
		{
			BaseCount = I(o, "base_count");
			Count = I(o, "count");
			foreach (var x in A(MiniJson.Get(o, "pictures")))
			{
				var e = O(x);
				Pictures.Add(new Picture { Index = I(e, "index"), Base = S(e, "base", Hashing.Absent), Data = S(e, "data") });
			}
			foreach (var h in A(MiniJson.Get(o, "removed_base"))) RemovedBase.Add(h as string);
		}
	}

	/// <summary>A whole file: a new one (base "absent"), one removed, or one whose format has no
	/// finer kind (a sound, a cutscene) and whose length changed.</summary>
	public sealed class FileComponent : ModComponent
	{
		public override string Kind { get { return FileKind; } }
		public string Base;
		public string Data;
		public bool Delete;

		public override string Summary { get { return File + (Delete ? ": removed" : Base == Hashing.Absent ? ": new file" : ": replaced"); } }

		protected override void WriteFields(Dictionary<string, object> o)
		{
			o["base"] = Base;
			if (Delete) o["delete"] = true; else o["data"] = Data;
		}

		protected override void ReadFields(Dictionary<string, object> o)
		{
			Base = S(o, "base", Hashing.Absent);
			Data = S(o, "data");
			Delete = MiniJson.Bool(o, "delete");
		}
	}

	/// <summary>A named setting that lives in the executable (Exe/ExeParams lists them).</summary>
	public sealed class ExeParamComponent : ModComponent
	{
		public override string Kind { get { return ExeParamKind; } }
		public string Param;
		public Dictionary<string, object> Value = new Dictionary<string, object>();

		public int GetInt(string k, int def = 0) { return ModManifest.Int(MiniJson.Get(Value, k), def); }

		public override string Summary
		{
			get
			{
				if (Param == Exe.ExeParams.StartSquare) return string.Format("start square ({0}, {1})", GetInt("x"), GetInt("y"));
				return Param;
			}
		}

		protected override void WriteFields(Dictionary<string, object> o) { o["param"] = Param; o["value"] = Value; }
		protected override void ReadFields(Dictionary<string, object> o)
		{
			Param = S(o, "param", "");
			Value = new Dictionary<string, object>();
			// JSON numbers read as double; keep whole ones as int, as they were written
			foreach (var kv in O(MiniJson.Get(o, "value")))
				Value[kv.Key] = kv.Value is double && (double)kv.Value == Math.Floor((double)kv.Value) && Math.Abs((double)kv.Value) < int.MaxValue ? (object)(int)(double)kv.Value : kv.Value;
		}
	}

	/// <summary>Changed bytes of the executable, with locations that survive UltimaHacks moving
	/// an overlay (Exe/MzExe): the segment and the offset in it, plus context bytes.</summary>
	public sealed class ExeBytesComponent : ModComponent
	{
		public sealed class Patch
		{
			/// <summary>FBOV segment of an overlay (its code is moved by UltimaHacks), or -1 for the load module.</summary>
			public int Segment = -1;
			/// <summary>Offset in the overlay's code, or in the load module.</summary>
			public int Offset;
			/// <summary>Where it is in the original executable.</summary>
			public int FileOffset;
			/// <summary>Hex: original bytes, new bytes (same length), and up to 16 bytes before and after.</summary>
			public string Base, Data, Before, After;
		}

		public override string Kind { get { return ExeBytesKind; } }
		public List<Patch> Patches = new List<Patch>();

		public override string Summary { get { int n = 0; foreach (var p in Patches) n += p.Data.Length / 2; return string.Format("{0}: {1} byte{2} patched", File, n, n == 1 ? "" : "s"); } }

		protected override void WriteFields(Dictionary<string, object> o)
		{
			var l = new List<object>();
			foreach (var p in Patches)
			{
				var e = new Dictionary<string, object>();
				e["segment"] = p.Segment;
				e["offset"] = p.Offset;
				e["file_offset"] = p.FileOffset;
				e["base"] = p.Base;
				e["data"] = p.Data;
				e["before"] = p.Before;
				e["after"] = p.After;
				l.Add(e);
			}
			o["patches"] = l;
		}

		protected override void ReadFields(Dictionary<string, object> o)
		{
			foreach (var x in A(MiniJson.Get(o, "patches")))
			{
				var e = O(x);
				Patches.Add(new Patch { Segment = I(e, "segment", -1), Offset = I(e, "offset"), FileOffset = I(e, "file_offset"), Base = S(e, "base", ""), Data = S(e, "data", ""), Before = S(e, "before", ""), After = S(e, "after", "") });
			}
		}
	}

	/// <summary>A saved game (a "mini mod"): the files of one SAVEn folder, installed into a slot.</summary>
	public sealed class SaveComponent : ModComponent
	{
		public sealed class SaveFile
		{
			public string Name;
			public string Data;
		}

		public override string Kind { get { return SaveKind; } }
		/// <summary>The slot it came from (1-4), a suggestion.</summary>
		public int Slot;
		/// <summary>The save's description (its DESC file).</summary>
		public string Description = "";
		public List<SaveFile> Files = new List<SaveFile>();
		/// <summary>DATA files the save was made with (STRINGS.PAK, CNV.ARK, OBJECTS.DAT...), by hash.</summary>
		public SortedDictionary<string, string> DependsOn = new SortedDictionary<string, string>(StringComparer.Ordinal);

		public override string Summary { get { return "saved game \"" + Description + "\" (" + Files.Count + " files)"; } }

		protected override void WriteFields(Dictionary<string, object> o)
		{
			o["slot"] = Slot;
			o["description"] = Description;
			var l = new List<object>();
			foreach (var f in Files)
			{
				var e = new Dictionary<string, object>();
				e["name"] = f.Name;
				e["data"] = f.Data;
				l.Add(e);
			}
			o["files"] = l;
			var d = new Dictionary<string, object>();
			foreach (var kv in DependsOn) d[kv.Key] = kv.Value;
			o["depends_on"] = d;
		}

		protected override void ReadFields(Dictionary<string, object> o)
		{
			Slot = I(o, "slot");
			Description = S(o, "description", "");
			foreach (var x in A(MiniJson.Get(o, "files")))
			{
				var e = O(x);
				Files.Add(new SaveFile { Name = S(e, "name"), Data = S(e, "data") });
			}
			foreach (var kv in O(MiniJson.Get(o, "depends_on"))) DependsOn[kv.Key] = kv.Value as string;
		}
	}
}
