using System;
using System.Collections.Generic;
using UltimateEditor.Games;

namespace UltimateEditor.Modding.Exe
{
	/// <summary>
	/// The named settings a mod can make in the executable. Each knows how to read itself from
	/// an executable, write itself into one (original DOS or UltimaHacks), and what Exhumed
	/// needs instead, since Exhumed runs compiled code and never the executable's.
	/// </summary>
	public static class ExeParams
	{
		/// <summary>The square a new game starts on (uw1 ui/MAINMENU.C player_setup(0x20, 2, 1),
		/// uw2 player_setup(0x13, 0x30, -1); found by StartPosition's code signature).</summary>
		public const string StartSquare = "start-square";

		/// <summary>The Exhumed feature (reported by --features) that plays each parameter.</summary>
		public static string ExhumedFeature(string param)
		{
			return param == StartSquare ? "start-square" : null;
		}

		/// <summary>Parameters that differ between two executables of the game.</summary>
		public static List<ExeParamComponent> Diff(byte[] baseExe, byte[] modExe, GameKind game)
		{
			var r = new List<ExeParamComponent>();
			int bx, by, mx, my;
			if (StartPosition.Read(baseExe, game, out bx, out by) && StartPosition.Read(modExe, game, out mx, out my) && (bx != mx || by != my))
			{
				var c = new ExeParamComponent { Param = StartSquare, File = ExeIdentity.ExeName(game) };
				c.Value["x"] = mx;
				c.Value["y"] = my;
				// the square it was made from: applying refuses an executable another mod moved
				c.Value["base_x"] = bx;
				c.Value["base_y"] = by;
				r.Add(c);
			}
			return r;
		}

		/// <summary>The file offsets a parameter's bytes occupy (so the byte diff leaves them out).</summary>
		public static IEnumerable<int> Bytes(byte[] exe, GameKind game, string param)
		{
			if (param == StartSquare)
			{
				int at = StartPosition.Find(exe, game);
				if (at >= 0) { yield return at + 3; yield return at + 5; }
			}
		}

		/// <summary>Writes the parameter into an executable (DOS, with or without UltimaHacks).</summary>
		public static void Apply(byte[] exe, GameKind game, ExeParamComponent c)
		{
			if (c.Param == StartSquare)
			{
				int cx, cy;
				if (c.Value.ContainsKey("base_x") && StartPosition.Read(exe, game, out cx, out cy) && (cx != c.GetInt("base_x") || cy != c.GetInt("base_y")))
					throw new ModApplyException(string.Format("the start square is ({0}, {1}), the mod was made from one at ({2}, {3}) (another mod moved it?)", cx, cy, c.GetInt("base_x"), c.GetInt("base_y")));
				if (!StartPosition.Patch(exe, game, c.GetInt("x"), c.GetInt("y")))
					throw new ModApplyException("the start square's code was not found in " + ExeIdentity.ExeName(game) + " (another release?)");
				return;
			}
			throw new ModApplyException("unknown executable setting \"" + c.Param + "\"");
		}
	}
}
