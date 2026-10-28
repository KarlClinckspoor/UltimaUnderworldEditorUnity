using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Formats.Tests
{
	/// <summary>
	/// The files that moved into Assets/Scripts/Formats unchanged must be the old files, and the
	/// classes cut out of a larger file must appear in it as they were. (The routines taken out
	/// of DataReader and DataWriter changed a little and are checked by running them instead.)
	/// </summary>
	public class MovedSourceTests
	{
		/// <summary>(new path under Formats, old path under Scripts)</summary>
		public static IEnumerable<object[]> WholeFiles()
		{
			foreach (var f in new[] { "Archive", "Automap", "BGlobals", "EnchantmentEffects", "GameFiles", "GameProfile", "GrFile", "LevelBlock", "Lzss", "PlayerDat", "StartPosition" })
				yield return new object[] { "Games/" + f + ".cs", "Extensions/Core/Games/" + f + ".cs" };
			foreach (var f in new[] { "Uw1ExeModels", "Uw1Fields", "Uw1Schemas", "Uw1SmallFiles" })
				yield return new object[] { "Uw1/" + f + ".cs", "Extensions/Core/" + f + ".cs" };
			yield return new object[] { "Json/MiniJson.cs", "Extensions/Core/Zoning/MiniJson.cs" };
		}

		[Theory, MemberData(nameof(WholeFiles))]
		public void Whole_file_is_unchanged(string now, string before)
		{
			string a = Text(Path.Combine(Repo.LegacyScripts, before));
			string b = Text(Path.Combine(Repo.Scripts, "Formats", now));
			Assert.Equal(a, b);
			// and the .meta keeps its GUID, so Unity sees the same script
			Assert.Equal(Text(Path.Combine(Repo.LegacyScripts, before + ".meta")), Text(Path.Combine(Repo.Scripts, "Formats", now + ".meta")));
		}

		public static IEnumerable<object[]> CutClasses()
		{
			yield return new object[] { "Strings/HuffmanNode.cs", "Data/StringData.cs" };
			yield return new object[] { "Graphics/AdditionalTextureInfo.cs", "Data/TextureData.cs" };
			yield return new object[] { "Objects/ObjectData.cs", "Data/StaticObject.cs" };
		}

		[Theory, MemberData(nameof(CutClasses))]
		public void Cut_classes_are_unchanged(string now, string before)
		{
			string old = Text(Path.Combine(Repo.LegacyScripts, before));
			string body = WithoutHeader(Text(Path.Combine(Repo.Scripts, "Formats", now)));
			Assert.Contains(body, old);
		}

		[Fact]
		public void StringBlock_changes_only_the_logging_and_the_npc_name()
		{
			string old = Text(Path.Combine(Repo.LegacyScripts, "Data/StringData.cs"));
			string body = WithoutHeader(Text(Path.Combine(Repo.Scripts, "Formats/Strings/StringBlock.cs")));
			// undo the three documented edits; the rest must be the old class
			body = body.Replace("FormatsLog.Info(string.Format(\"Adding string {0} to block {1}, count : {2}\", str, BlockNumber, Strings.Count));",
				"Debug.LogFormat(\"Adding string {0} to block {1}, count : {2}\", str, BlockNumber, Strings.Count);");
			body = body.Replace("return \"Conversation (\" + (NpcName != null ? NpcName(i - 3584) : \"slot \" + (i - 3584)) + \")\";",
				"return \"Conversation (\" + MapCreator.StringData.GetNPCName(i - 3584) + \")\";");
			int hook = body.IndexOf("\t/// <summary>The NPC name of a conversation slot");
			int next = body.IndexOf("\tpublic static string GetDescription(int i)");
			Assert.True(hook > 0 && next > hook);
			body = body.Remove(hook, next - hook);
			Assert.Contains(body, old);
		}

		static string Text(string path)
		{
			var s = File.ReadAllText(path, Encoding.UTF8);
			if (s.Length > 0 && s[0] == '﻿') s = s.Substring(1);
			return s.Replace("\r\n", "\n");
		}

		/// <summary>The file without its leading comment and using lines.</summary>
		static string WithoutHeader(string s)
		{
			var lines = new List<string>(s.Split('\n'));
			while (lines.Count > 0 && (lines[0].StartsWith("//") || lines[0].StartsWith("using ") || lines[0].Trim() == "")) lines.RemoveAt(0);
			return string.Join("\n", lines).TrimEnd('\n');
		}
	}
}
