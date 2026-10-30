using System;
using System.Collections.Generic;
using UltimateEditor.Games;
using UltimateEditor.Modding.Exe;

namespace UltimateEditor.Modding
{
	/// <summary>
	/// Works out a mod's type, what it does to games in progress, and what Exhumed needs to play
	/// it, from its components. Facts from the reference (abedegno/underworld-exhumed): a new
	/// game copies DATA\LEV.ARK (UW2 also SCD.ARK) into SAVE0 and builds the conversation
	/// variables from BABGLOBS.DAT; class skills (SKILLS.DAT) matter only in character creation;
	/// the start square is in the executable's code. Every other DATA, CRIT, CUTS and SOUND file
	/// is read during play, from saved games too.
	/// </summary>
	public static class ModClassifier
	{
		static readonly string[] NewGameFiles = { "DATA/LEV.ARK", "DATA/SCD.ARK", "DATA/BABGLOBS.DAT", "DATA/SKILLS.DAT" };

		public const string LevelMod = "Level mod", ContentMod = "Content mod", ExeTweak = "Executable tweak",
			SavedGame = "Saved game", TotalConversion = "Total conversion";

		public static void Classify(ModManifest m, Func<string, byte[]> original, ModPackage pkg)
		{
			bool level = false, content = false, exe = false, save = false;
			m.Flags = new ModFlags();
			var needs = new SortedSet<string>(StringComparer.Ordinal);
			foreach (var c in m.Components)
			{
				if (c is SaveComponent) { save = true; continue; }
				if (c is ExeParamComponent)
				{
					exe = true;
					m.Flags.NewGame = true;
					string f = ExeParams.ExhumedFeature(((ExeParamComponent)c).Param);
					if (f != null) needs.Add(f);
					continue;
				}
				if (c is ExeBytesComponent) { exe = true; needs.Add("exe-patch"); continue; }
				string rel = GameFolder.Normalize(c.File);
				if (Array.IndexOf(NewGameFiles, rel) >= 0)
				{
					m.Flags.NewGame = true;
					if (rel == "DATA/LEV.ARK" || rel == "DATA/SCD.ARK") level = true; else content = true;
				}
				else { m.Flags.ChangesSaves = true; content = true; }
				if (rel == "DATA/CNV.ARK" && c is ArkBlocksComponent && original != null && pkg != null)
				{
					var kind = m.Game == "uw2" ? GameKind.UW2 : GameKind.UW1;
					m.Flags.BreaksConversations.AddRange(VariableCountChanges((ArkBlocksComponent)c, original(rel), pkg, kind));
					m.Flags.NewConversations.AddRange(AddedConversations((ArkBlocksComponent)c));
				}
			}
			int kinds = (level ? 1 : 0) + (content ? 1 : 0) + (exe ? 1 : 0) + (save ? 1 : 0);
			if (kinds == 0) m.Type = "";
			else if (save && kinds == 1) m.Type = SavedGame;
			else if (kinds > 1 && !(exe && kinds == 2 && level)) m.Type = TotalConversion;
			else if (level) m.Type = LevelMod;   // a level mod may move the start square with it
			else if (content) m.Type = ContentMod;
			else m.Type = ExeTweak;
			m.ExhumedNeeds = new List<string>(needs);
		}

		/// <summary>Conversation slots whose saved-variable count (babl_nvars, word 0xC of the
		/// block; uw conv/BABL.C DoReadHeader) the mod changes.</summary>
		public static List<int> VariableCountChanges(ArkBlocksComponent c, byte[] originalArk, ModPackage pkg, GameKind game)
		{
			var r = new List<int>();
			if (originalArk == null) return r;
			var ar = Archive.Open(originalArk, game);
			foreach (var b in c.Blocks)
			{
				if (b.Delete || !ar.Has(b.Index)) continue;
				byte[] old = ar.Read(b.Index), neu = pkg.Payload(b.Data);
				if (old.Length < 0x10 || neu.Length < 0x10) continue;
				int n0 = old[0xC] | old[0xD] << 8, n1 = neu[0xC] | neu[0xD] << 8;
				if (n0 != n1) r.Add(b.Index);
			}
			return r;
		}

		/// <summary>Conversation slots the mod fills that the original has empty (their base is "absent").</summary>
		public static List<int> AddedConversations(ArkBlocksComponent c)
		{
			var r = new List<int>();
			foreach (var b in c.Blocks)
				if (!b.Delete && b.Base == "absent") r.Add(b.Index);
			return r;
		}

		/// <summary>What the player is told, one line each.</summary>
		public static List<string> Warnings(ModManifest m)
		{
			var w = new List<string>();
			if (m.Flags.NewGame) w.Add("Start a new game to see all of it: levels, class skills and the start square are set when a game starts.");
			if (m.Flags.ChangesSaves) w.Add("It also changes games in progress: strings, pictures, objects and conversations are read during play.");
			if (m.Flags.BreaksConversations.Count > 0)
				w.Add("Saved games will have the wrong conversation memory for conversation slot(s) " + string.Join(", ", m.Flags.BreaksConversations) + " (their variable count changed).");
			if (m.Flags.NewConversations.Count > 0)
				w.Add("Conversation slot(s) " + string.Join(", ", m.Flags.NewConversations) + " are new: games in progress keep no memory for them; start a new game.");
			return w;
		}
	}
}
