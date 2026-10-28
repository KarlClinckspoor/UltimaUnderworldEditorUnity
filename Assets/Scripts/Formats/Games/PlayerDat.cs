using System;
using System.IO;
using System.Text;

namespace UltimateEditor.Games
{
	/// <summary>
	/// A saved game's PLAYER.DAT, both games (reference docs/FORMATS.md "PLAYER.DAT"; uw2
	/// game/PLAYDATA.C, inv/INVSAVE.C, sys/MISCUTIL.C; uw1 the same files). Layout: a key byte
	/// (name[0] xor 0xAA), the player record enciphered (UW1 0xD2 bytes, UW2 0x37D), a plain
	/// word (saved objects + 1), then the plain inventory workspace. The shipped DATA\PLAYER.DAT
	/// is not a save and fits neither layout.
	/// </summary>
	public sealed class PlayerDat
	{
		public readonly GameKind Game;
		/// <summary>The record, deciphered. Offsets 0-0x5D mean the same in both games.</summary>
		public readonly byte[] Record;
		/// <summary>Everything after the record (the count word and the workspace), as stored.</summary>
		public readonly byte[] Rest;

		public int RecordSize { get { return Record.Length; } }

		PlayerDat(GameKind game, byte[] record, byte[] rest) { Game = game; Record = record; Rest = rest; }

		public static int RecordSizeFor(GameKind g) { return g == GameKind.UW2 ? 0x37D : 0xD2; }

		/// <summary>The game this file belongs to, by its length (1 + record + 2 + 0x5B + 8 * count); null if neither fits.</summary>
		public static GameKind? Detect(byte[] file)
		{
			foreach (GameKind g in new[] { GameKind.UW2, GameKind.UW1 })
			{
				int rec = RecordSizeFor(g);
				if (file.Length < 1 + rec + 2 + 0x5B) continue;
				int count = file[1 + rec] | file[2 + rec] << 8;
				if (file.Length == 1 + rec + 2 + 0x5B + 8 * count) return g;
			}
			return null;
		}

		public static PlayerDat Parse(byte[] file, GameKind game)
		{
			int rec = RecordSizeFor(game);
			if (file.Length < 1 + rec + 2 + 0x5B) throw new InvalidDataException("PLAYER.DAT is too short for " + game + " (" + file.Length + " bytes)");
			var enc = new byte[rec];
			Array.Copy(file, 1, enc, 0, rec);
			var plain = Cipher(enc, file[0], game, false);
			var rest = new byte[file.Length - 1 - rec];
			Array.Copy(file, 1 + rec, rest, 0, rest.Length);
			return new PlayerDat(game, plain, rest);
		}

		/// <summary>The file: the key is the name's first letter xor 0xAA, as save_player_data makes it.</summary>
		public byte[] ToBytes()
		{
			byte key = (byte)(Record[0] ^ 0xAA);
			var enc = Cipher(Record, key, Game, true);
			var f = new byte[1 + enc.Length + Rest.Length];
			f[0] = key;
			Array.Copy(enc, 0, f, 1, enc.Length);
			Array.Copy(Rest, 0, f, 1 + enc.Length, Rest.Length);
			return f;
		}

		/// <summary>
		/// The record cipher, in blocks of 0x50 bytes. UW1: byte i of a block xor (key + 3(i + 1)).
		/// UW2: a table from build_xor_table, then out[0] = in[0] ^ t[0], out[i] = in[i] ^ (t[i] +
		/// out[i-1] + in[i-1]) with in the source and out the result, so the routine is its own
		/// inverse given which side is plain; blocks under 2 bytes are left as they are.
		/// </summary>
		public static byte[] Cipher(byte[] src, byte key, GameKind game, bool encipher)
		{
			var dst = new byte[src.Length];
			if (game == GameKind.UW1)
			{
				for (int i = 0; i < src.Length; i++) dst[i] = (byte)(src[i] ^ (key + 3 * ((i % 0x50) + 1)));
				return dst;
			}
			var t = Uw2Table(key);
			for (int start = 0; start < src.Length; start += 0x50)
			{
				int n = Math.Min(0x50, src.Length - start);
				if (n < 2) { Array.Copy(src, start, dst, start, n); continue; }
				dst[start] = (byte)(src[start] ^ t[0]);
				for (int i = 1; i < n; i++)
					dst[start + i] = (byte)(src[start + i] ^ (t[i] + dst[start + i - 1] + src[start + i - 1]));
			}
			return dst;
		}

		/// <summary>build_xor_table (uw2 sys/MISCUTIL.C).</summary>
		public static byte[] Uw2Table(byte key)
		{
			var t = new byte[0x50];
			byte k = (byte)(key + 0x71);
			for (int i = 0x3C; i < 0x50; i++) t[i] = k = (byte)(k + i + 2);
			for (int i = 0; i < 0x50; i++) t[i] = k = (byte)(k + 6);
			for (int i = 0; i < 0x10; i++) t[i * 5] = k = (byte)(k + 7);
			for (int i = 0; i < 4; i++) t[i * 12] = k = (byte)(k + 0x29);
			for (int i = 0; i < 11; i++) t[i * 7] = k = (byte)(k + 0x49);
			return t;
		}

		// ---- the shared part of the record (both player.h agree to 0x5D) ----

		public static readonly string[] SkillNames = { "Attack", "Defense", "Unarmed", "Sword", "Axe", "Mace", "Missile", "Mana", "Lore", "Casting",
			"Traps", "Search", "Track", "Sneak", "Repair", "Charm", "Picklock", "Acrobat", "Appraise", "Swimming" };

		public string Name
		{
			get
			{
				int n = 0;
				while (n < 0x1E && Record[n] != 0) n++;
				var sb = new StringBuilder();
				for (int i = 0; i < n; i++) sb.Append((char)Record[i]);   // the game's bytes, one char each
				return sb.ToString();
			}
			set
			{
				var b = new byte[(value ?? "").Length];
				for (int i = 0; i < b.Length; i++) b[i] = value[i] < 256 ? (byte)value[i] : (byte)'?';
				if (b.Length == 0) throw new ArgumentException("The name cannot be empty (its first letter is the cipher key)");
				for (int i = 0; i < 0x1E; i++) Record[i] = i < b.Length && i < 0x1D ? b[i] : (byte)0;
			}
		}

		public int Strength { get { return Record[0x1E]; } set { Record[0x1E] = B(value); } }
		public int Dexterity { get { return Record[0x1F]; } set { Record[0x1F] = B(value); } }
		public int Intelligence { get { return Record[0x20]; } set { Record[0x20] = B(value); } }
		public int Skill(int i) { return Record[0x21 + i]; }
		public void SetSkill(int i, int v) { Record[0x21 + i] = B(v); }
		public int MaxVitality { get { return Record[0x36]; } set { Record[0x36] = B(value); } }
		public int Mana { get { return Record[0x37]; } set { Record[0x37] = B(value); } }
		public int MaxMana { get { return Record[0x38]; } set { Record[0x38] = B(value); } }
		public int Hunger { get { return Record[0x39]; } set { Record[0x39] = B(value); } }
		public int Fatigue { get { return Record[0x3A]; } set { Record[0x3A] = B(value); } }
		public int Level { get { return Record[0x3D]; } set { Record[0x3D] = B(value); } }
		/// <summary>Experience in tenths, as stored.</summary>
		public long ExperienceTenths
		{
			get { return (long)(uint)(Record[0x4E] | Record[0x4F] << 8 | Record[0x50] << 16 | Record[0x51] << 24); }
			set { Record[0x4E] = (byte)value; Record[0x4F] = (byte)(value >> 8); Record[0x50] = (byte)(value >> 16); Record[0x51] = (byte)(value >> 24); }
		}
		public int SkillPoints { get { return Record[0x52]; } set { Record[0x52] = B(value); } }
		// Position (record 0x54-0x5D, save_player_data): PN.x and PN.y in 1/256 of a square,
		// so the square is the high byte (player_setup puts the player at square * 256 + 0x80);
		// PN.z with 0x40 a floor step (hgt_val); PlayerFacing, a 16-bit angle whose top 3 bits
		// are the heading (0 north); PlayerLevel, 1-based.
		public int SavedLevel { get { return (short)(Record[0x5C] | Record[0x5D] << 8); } }
		public int SavedX { get { return (short)(Record[0x54] | Record[0x55] << 8); } }
		public int SavedY { get { return (short)(Record[0x56] | Record[0x57] << 8); } }
		public int SavedZ { get { return (short)(Record[0x58] | Record[0x59] << 8); } }
		public int SavedFacing { get { return (ushort)(Record[0x5A] | Record[0x5B] << 8); } }
		public int TileX { get { return (SavedX >> 8) & 0x3F; } }
		public int TileY { get { return (SavedY >> 8) & 0x3F; } }
		public int Heading { get { return (SavedFacing >> 13) & 7; } }

		/// <summary>hgt_val (map/MAP.C): a tile height's z. Heights 14 and 15 give 0 in the game's table.</summary>
		public static readonly int[] HeightZ = { 0x000, 0x040, 0x080, 0x0C0, 0x100, 0x140, 0x180, 0x1C0, 0x200, 0x240, 0x280, 0x2C0, 0x300, 0x340, 0, 0, 0x400 };

		/// <summary>
		/// Moves the player, as player_setup places a new player: the middle of square (x, y)
		/// of level, at the tile's floor (plus 0x20 on a slope), facing heading (0-7, 0 north).
		/// The workspace's player object gets the same square, height and heading, so the two
		/// copies the game reads agree.
		/// </summary>
		public void SetPosition(int level, int x, int y, int tileHeight, bool slope, int heading)
		{
			if (x < 0 || x > 63 || y < 0 || y > 63) throw new ArgumentOutOfRangeException("x/y", "squares are 0-63");
			int z = HeightZ[Math.Max(0, Math.Min(16, tileHeight))] + (slope ? 0x20 : 0);
			int fx = (x << 8) + 0x80, fy = (y << 8) + 0x80, facing = (heading & 7) << 13;
			W(0x54, fx); W(0x56, fy); W(0x58, z); W(0x5A, facing); W(0x5C, level);
			// the workspace player object: pos word at +2 (z 0-6 = PN.z >> 3, heading 7-9),
			// home word at +0x16 (y bits 4-9, x bits 10-15); Rest[0..1] is the count word
			const int obj = 2;
			if (Rest.Length >= obj + 0x18)
			{
				int pos = Rest[obj + 2] | Rest[obj + 3] << 8;
				pos = (pos & ~0x3FF) | ((z >> 3) & 0x7F) | ((heading & 7) << 7);
				Rest[obj + 2] = (byte)pos; Rest[obj + 3] = (byte)(pos >> 8);
				int home = Rest[obj + 0x16] | Rest[obj + 0x17] << 8;
				home = (home & 0xF) | ((y & 0x3F) << 4) | ((x & 0x3F) << 10);
				Rest[obj + 0x16] = (byte)home; Rest[obj + 0x17] = (byte)(home >> 8);
			}
		}

		void W(int at, int v) { Record[at] = (byte)v; Record[at + 1] = (byte)(v >> 8); }

		/// <summary>
		/// Current vitality. The game keeps the workspace player object's hp byte (it overwrites
		/// the record's health byte on loading), so both are set.
		/// </summary>
		public int Vitality
		{
			get { return Rest.Length > 2 + 8 ? Rest[2 + 8] : Record[0x35]; }
			set { Record[0x35] = B(value); if (Rest.Length > 2 + 8) Rest[2 + 8] = B(value); }
		}

		/// <summary>Saved objects in the workspace (the count word less one).</summary>
		public int SavedObjects { get { return Math.Max(0, (Rest[0] | Rest[1] << 8) - 1); } }

		/// <summary>The record key check: true when byte 0 of the file equals the name's first letter xor 0xAA.</summary>
		public static bool KeyMatches(byte[] file, GameKind game)
		{
			var p = Parse(file, game);
			return (byte)(p.Record[0] ^ 0xAA) == file[0];
		}

		static byte B(int v)
		{
			if (v < 0 || v > 255) throw new ArgumentOutOfRangeException("value", "must be 0-255");
			return (byte)v;
		}
	}
}
