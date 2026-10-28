using UltimateEditor.Games;

namespace UltimateEditor.Modding.Exe
{
	/// <summary>
	/// The executables a mod can be installed into. Exhumed refuses any UW.EXE / UW2.EXE but the
	/// GOG release's (uwN/src/port/mem/fardata.c checks the size and CRC-32), and UltimaHacks'
	/// hacks are made for that release too (vF1.94S and vF1.99S).
	/// </summary>
	public static class ExeIdentity
	{
		public const int Uw1Size = 547248, Uw2Size = 675184;
		public const uint Uw1Crc = 0xF2BF2527u, Uw2Crc = 0xC824FB97u;

		public enum Kind { Unknown, GogOriginal, GogUltimaHacks }

		public static bool IsGogOriginal(byte[] exe, GameKind game)
		{
			int size = game == GameKind.UW2 ? Uw2Size : Uw1Size;
			uint crc = game == GameKind.UW2 ? Uw2Crc : Uw1Crc;
			return exe != null && exe.Length == size && Hashing.Crc32(exe) == crc;
		}

		public static string ExeName(GameKind game) { return game == GameKind.UW2 ? "UW2.EXE" : "UW.EXE"; }
	}
}
