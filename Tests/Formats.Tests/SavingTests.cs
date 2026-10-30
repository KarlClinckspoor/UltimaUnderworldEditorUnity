extern alias current;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Xunit;
using C = current::DataReader;
using CW = current::DataWriter;
using GW = current::GameWrite;

namespace Formats.Tests
{
	/// <summary>
	/// Saving (review package 4): files are written only when their bytes change, through a
	/// temporary file, with the .BKP original and dated versions in "Editor backups"; a saved
	/// game's .BKP follows the game; an autosave writes a copy of the game and leaves the game's
	/// files and the editor's record of them alone.
	/// </summary>
	public class SavingTests
	{
		static string Setup(string game)
		{
			string dir = GameData.Copy(game);
			Application.dataPath = Path.Combine(dir, "unity-data");
			Application.streamingAssetsPath = Path.Combine(Repo.Root, "Assets", "StreamingAssets");
			C.FilePath = Path.Combine(dir, "DATA", "LEV.ARK").Replace('\\', '/');
			typeof(C).GetField("uiManager", BindingFlags.NonPublic | BindingFlags.Static)
				.SetValue(null, RuntimeHelpers.GetUninitializedObject(typeof(current::UIManager)));
			current::GameSession.Choose(C.FilePath);
			GW.Versions = GW.DefaultVersions;
			return dir;
		}

		static string[] Versions(string dir, string relDir, string name)
		{
			string d = Path.Combine(dir, GW.BackupsFolder, relDir);
			return Directory.Exists(d) ? Directory.GetFiles(d, name + ".*").OrderBy(f => f).ToArray() : new string[0];
		}

		[GameFact("UW")]
		public void A_file_is_written_only_when_it_changes_with_its_original_and_versions_kept()
		{
			string dir = Setup("UW");
			string path = GameData.InData(dir, "SHADES.DAT");
			byte[] original = File.ReadAllBytes(path);

			Assert.False(GW.Write(path, original));
			Assert.False(File.Exists(path + ".BKP"));
			Assert.Empty(Versions(dir, "DATA", Path.GetFileName(path)));

			byte[] a = (byte[])original.Clone(); a[0] ^= 1;
			Assert.True(GW.Write(path, a));
			Assert.Equal(a, File.ReadAllBytes(path));
			Assert.Equal(original, File.ReadAllBytes(path + ".BKP"));
			var v = Versions(dir, "DATA", Path.GetFileName(path));
			Assert.Single(v);
			Assert.Equal(original, File.ReadAllBytes(v[0]));
			Assert.False(File.Exists(path + ".uwedit-tmp"));

			// more versions than kept: the oldest go
			GW.Versions = 2;
			for (int i = 0; i < 4; i++) { a[1] = (byte)i; GW.Write(path, a); }
			Assert.Equal(2, Versions(dir, "DATA", Path.GetFileName(path)).Length);
			Assert.Equal(original, File.ReadAllBytes(path + ".BKP"));   // the original stays
		}

		[GameFact("UW")]
		public void A_saved_games_original_follows_the_game()
		{
			string dir = Setup("UW");
			string slot = Path.Combine(dir, "SAVE1");
			Directory.CreateDirectory(slot);
			string file = Path.Combine(slot, "PLAYER.DAT");
			File.WriteAllBytes(file, new byte[] { 1, 1, 1 });
			GW.Write(file, new byte[] { 2, 2, 2 });                    // the editor's edit
			Assert.Equal(new byte[] { 1, 1, 1 }, File.ReadAllBytes(file + ".BKP"));
			File.WriteAllBytes(file, new byte[] { 3, 3, 3 });          // then the game saves
			GW.Write(file, new byte[] { 4, 4, 4 });                    // and the editor edits again
			Assert.Equal(new byte[] { 3, 3, 3 }, File.ReadAllBytes(file + ".BKP"));
			GW.Write(file, new byte[] { 5, 5, 5 });                    // an edit after its own: no change
			Assert.Equal(new byte[] { 3, 3, 3 }, File.ReadAllBytes(file + ".BKP"));
		}

		[GameTheory, MemberData(nameof(GameData.Available), MemberType = typeof(GameData))]
		public void Saving_unchanged_data_writes_nothing_and_makes_no_backups(string game)
		{
			if (!GameData.Has(game)) return;
			string dir = Setup(game);
			var before = Directory.GetFiles(Path.Combine(dir, "DATA")).ToDictionary(f => f, File.ReadAllBytes);
			var levels = current::LevArk.Load(current::GameSession.LevelFile(C.FilePath));
			// as MapCreator.Initialize does: the animation overlays belong to their objects
			foreach (var ld in levels)
				foreach (var anim in ld.AnimationOverlays)
					if (anim && ld.Objects[anim.Adress] != null) ld.Objects[anim.Adress].Animation = anim;
			CW.SaveLevelData(levels);
			CW.SaveObjectDatas(C.LoadObjectData());
			CW.SaveStringData(C.LoadStrings());
			foreach (var kv in before)
				Same.Bytes(kv.Value, File.ReadAllBytes(kv.Key), Path.GetFileName(kv.Key));
			Assert.Empty(Directory.GetFiles(dir, "*.BKP", SearchOption.AllDirectories));
			Assert.False(Directory.Exists(Path.Combine(dir, GW.BackupsFolder)));
		}

		[GameFact("UW")]
		public void An_autosave_writes_a_copy_and_leaves_the_game_and_the_record_alone()
		{
			string dir = Setup("UW");
			string strings = GameData.InData(dir, "STRINGS.PAK");
			byte[] original = File.ReadAllBytes(strings);
			var sd = C.LoadStrings();
			sd.Blocks[2].Strings[1] += " (edited)";

			string copy = GW.NewSnapshot();
			Assert.StartsWith(Path.Combine(dir, GW.AutosavesFolder), copy);
			Assert.True(File.Exists(Path.Combine(copy, "DATA", Path.GetFileName(strings))));
			GW.RunIn(copy, () => CW.SaveStringData(sd));
			Assert.False(GW.Snapshot);

			Same.Bytes(original, File.ReadAllBytes(strings), "the game's STRINGS.PAK");
			Assert.False(File.Exists(strings + ".BKP"));
			Assert.NotEqual(original, File.ReadAllBytes(Path.Combine(copy, "DATA", Path.GetFileName(strings))));
			// the edit is still unsaved as far as the game's files go
			Assert.True(sd.ChangedSinceSnapshot());
			CW.SaveStringData(sd);
			Assert.Equal(File.ReadAllBytes(Path.Combine(copy, "DATA", Path.GetFileName(strings))), File.ReadAllBytes(strings));
			Assert.True(File.Exists(strings + ".BKP"));

			// as many copies as versions are kept
			GW.Versions = 2;
			for (int i = 0; i < 3; i++) GW.NewSnapshot();
			Assert.Equal(2, Directory.GetDirectories(Path.Combine(dir, GW.AutosavesFolder)).Length);
		}
	}
}
