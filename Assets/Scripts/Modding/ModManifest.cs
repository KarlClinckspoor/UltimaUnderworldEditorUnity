using System;
using System.Collections.Generic;
using System.Globalization;
using UltimateEditor.Uw1.Zoning;

namespace UltimateEditor.Modding
{
	/// <summary>
	/// mod.json, the manifest of a .uwmod package: who made the mod, the files it was made
	/// from (their hashes), and its components, each the changes to one file. The payload
	/// (block bytes, strings, pictures, whole files) lives next to it in the package; the
	/// components name their payload by its path there.
	///
	/// A mod is always the changes to the original files, never whole copies of them, so a
	/// package holds only what its author made. See claude/mod-manager-plan.md.
	/// </summary>
	public sealed class ModManifest
	{
		public const int CurrentFormat = 1;

		public int Format = CurrentFormat;
		public string Id = "";
		public string Name = "";
		public string Version = "1.0.0";
		/// <summary>"uw1" or "uw2".</summary>
		public string Game = "uw1";
		public List<string> Authors = new List<string>();
		public string Description = "";
		public string License = "";
		public string MadeWith = "";
		/// <summary>UTC, ISO 8601.</summary>
		public string Created = "";
		/// <summary>"gog-uw1", "gog-uw2" when every base file is the GOG release's, else "other".</summary>
		public string BaseSet = "other";
		/// <summary>Every file a component changes, with the hash of the original it was made from
		/// (relative path with '/', upper case, e.g. "DATA/LEV.ARK").</summary>
		public SortedDictionary<string, string> BaseFiles = new SortedDictionary<string, string>(StringComparer.Ordinal);
		public List<ModComponent> Components = new List<ModComponent>();
		public ModFlags Flags = new ModFlags();
		/// <summary>The label the type was worked out as (ModClassifier).</summary>
		public string Type = "";
		/// <summary>Features an Exhumed build must report (--features) to play this mod.</summary>
		public List<string> ExhumedNeeds = new List<string>();
		/// <summary>Exhumed enhancements the author recommends (passed with --enhance).</summary>
		public List<string> ExhumedRecommended = new List<string>();
		/// <summary>Package paths of screenshots (PNG).</summary>
		public List<string> Screenshots = new List<string>();

		public string GameName { get { return Game == "uw2" ? "Ultima Underworld II" : "Ultima Underworld"; } }

		// ---- JSON

		public string ToJson()
		{
			var o = new Dictionary<string, object>();
			o["format"] = Format;
			o["id"] = Id;
			o["name"] = Name;
			o["version"] = Version;
			o["game"] = Game;
			o["type"] = Type;
			o["authors"] = new List<object>(Authors);
			o["description"] = Description;
			o["license"] = License;
			o["made_with"] = MadeWith;
			o["created"] = Created;
			var b = new Dictionary<string, object>();
			b["set"] = BaseSet;
			var bf = new Dictionary<string, object>();
			foreach (var kv in BaseFiles) bf[kv.Key] = kv.Value;
			b["files"] = bf;
			o["base"] = b;
			var cl = new List<object>();
			foreach (var c in Components) cl.Add(c.ToJson());
			o["components"] = cl;
			o["flags"] = Flags.ToJson();
			var ex = new Dictionary<string, object>();
			ex["needs"] = new List<object>(ExhumedNeeds);
			ex["recommended_enhancements"] = new List<object>(ExhumedRecommended);
			o["exhumed"] = ex;
			o["screenshots"] = new List<object>(Screenshots);
			return MiniJson.Write(o, true) + "\n";
		}

		public static ModManifest Parse(string json)
		{
			var o = MiniJson.Obj(MiniJson.Parse(json));
			if (o == null) throw new FormatException("mod.json is not a JSON object");
			var m = new ModManifest();
			m.Format = MiniJson.Int(o, "format", 0);
			if (m.Format < 1 || m.Format > CurrentFormat)
				throw new FormatException("mod.json format " + m.Format + " is not supported (this program reads up to " + CurrentFormat + ")");
			m.Id = MiniJson.Str(o, "id");
			m.Name = MiniJson.Str(o, "name");
			m.Version = MiniJson.Str(o, "version");
			m.Game = MiniJson.Str(o, "game", "uw1");
			if (m.Game != "uw1" && m.Game != "uw2") throw new FormatException("mod.json game must be uw1 or uw2, not " + m.Game);
			m.Type = MiniJson.Str(o, "type");
			m.Authors = Strings(MiniJson.Get(o, "authors"));
			m.Description = MiniJson.Str(o, "description");
			m.License = MiniJson.Str(o, "license");
			m.MadeWith = MiniJson.Str(o, "made_with");
			m.Created = MiniJson.Str(o, "created");
			var b = MiniJson.Obj(MiniJson.Get(o, "base"));
			if (b != null)
			{
				m.BaseSet = MiniJson.Str(b, "set", "other");
				var bf = MiniJson.Obj(MiniJson.Get(b, "files"));
				if (bf != null) foreach (var kv in bf) m.BaseFiles[kv.Key] = kv.Value as string;
			}
			var cl = MiniJson.Arr(MiniJson.Get(o, "components"));
			if (cl != null) foreach (var c in cl) m.Components.Add(ModComponent.FromJson(MiniJson.Obj(c)));
			m.Flags = ModFlags.FromJson(MiniJson.Obj(MiniJson.Get(o, "flags")));
			var ex = MiniJson.Obj(MiniJson.Get(o, "exhumed"));
			if (ex != null)
			{
				m.ExhumedNeeds = Strings(MiniJson.Get(ex, "needs"));
				m.ExhumedRecommended = Strings(MiniJson.Get(ex, "recommended_enhancements"));
			}
			m.Screenshots = Strings(MiniJson.Get(o, "screenshots"));
			return m;
		}

		internal static List<string> Strings(object o)
		{
			var r = new List<string>();
			var a = MiniJson.Arr(o);
			if (a != null) foreach (var x in a) if (x != null) r.Add(x.ToString());
			return r;
		}

		internal static int Int(object o, int def = 0)
		{
			if (o == null) return def;
			if (o is double) return (int)(double)o;
			if (o is long) return (int)(long)o;
			if (o is int) return (int)o;
			int v;
			return int.TryParse(o.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : def;
		}
	}

	/// <summary>What installing the mod does to games in progress (ModClassifier sets these).</summary>
	public sealed class ModFlags
	{
		/// <summary>Only a new game sees all of it: LEV.ARK, SCD.ARK, BABGLOBS.DAT and SKILLS.DAT are
		/// read when a game starts, and the start square is set then.</summary>
		public bool NewGame;
		/// <summary>Saved games see some of it: the other files are read from DATA during play.</summary>
		public bool ChangesSaves;
		/// <summary>Conversations whose saved-variable count changed: a saved game's record for
		/// them has the old size (the game reads min(count, size) words).</summary>
		public List<int> BreaksConversations = new List<int>();
		/// <summary>Conversations the mod adds to empty slots: a saved game has no record for them
		/// (uw1 conv/BABL.C bab_get_globals), so their variables are neither set nor kept there.</summary>
		public List<int> NewConversations = new List<int>();

		public Dictionary<string, object> ToJson()
		{
			var o = new Dictionary<string, object>();
			o["new_game"] = NewGame;
			o["changes_saves"] = ChangesSaves;
			var l = new List<object>();
			foreach (var s in BreaksConversations) l.Add(s);
			o["breaks_conversations"] = l;
			var n = new List<object>();
			foreach (var s in NewConversations) n.Add(s);
			o["new_conversations"] = n;
			return o;
		}

		public static ModFlags FromJson(Dictionary<string, object> o)
		{
			var f = new ModFlags();
			if (o == null) return f;
			f.NewGame = MiniJson.Bool(o, "new_game");
			f.ChangesSaves = MiniJson.Bool(o, "changes_saves");
			var a = MiniJson.Arr(MiniJson.Get(o, "breaks_conversations"));
			if (a != null) foreach (var x in a) f.BreaksConversations.Add(ModManifest.Int(x));
			var nc = MiniJson.Arr(MiniJson.Get(o, "new_conversations"));
			if (nc != null) foreach (var x in nc) f.NewConversations.Add(ModManifest.Int(x));
			return f;
		}
	}
}
