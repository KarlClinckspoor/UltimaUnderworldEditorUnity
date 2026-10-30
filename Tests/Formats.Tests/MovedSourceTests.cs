using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Formats.Tests
{
	/// <summary>
	/// The files that moved into Assets/Scripts/Formats keep their .meta GUID, so Unity sees the
	/// same scripts (and scenes and prefabs keep their references). Their contents were pinned to
	/// the old files while the move was checked; now they change like any other code, and the
	/// parity and format tests check what they do instead.
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
		public void Moved_file_keeps_its_unity_guid(string now, string before)
		{
			Assert.True(File.Exists(Path.Combine(Repo.Scripts, "Formats", now)), now);
			Assert.Equal(Guid(Path.Combine(Repo.LegacyScripts, before + ".meta")), Guid(Path.Combine(Repo.Scripts, "Formats", now + ".meta")));
		}

		static string Guid(string meta)
		{
			foreach (var line in File.ReadAllLines(meta))
				if (line.StartsWith("guid: ")) return line.Substring(6).Trim();
			return null;
		}
	}
}
