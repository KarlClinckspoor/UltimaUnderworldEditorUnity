using System;
using System.Collections.Generic;
using System.Text;

namespace UltimateEditor.Games
{
	/// <summary>
	/// One level's map block, kept as its bytes with typed accessors (reference
	/// include/level.h, docs/FORMATS.md "The level block"). Both games share the layout up to
	/// 0x7C08; UW2's block goes on with 64 animation overlays and 64 timer words (0x7E08).
	/// Editing goes through the accessors, so whatever the editor does not model stays as it was.
	/// </summary>
	public sealed class LevelBlock
	{
		public const int TilesAt = 0x0000, MobilesAt = 0x4000, StaticsAt = 0x5B00;
		public const int MobFreeAt = 0x7300, StaticFreeAt = 0x74FC, ActiveAt = 0x7AFC;
		public const int ActiveCountAt = 0x7C00, MobTopAt = 0x7C02, StaticTopAt = 0x7C04, MagicAt = 0x7C06;
		public const int Uw2OverlaysAt = 0x7C08, Uw2TimersAt = 0x7D88;
		public const int Magic = 0x7577;
		public const int MobileSize = 0x1B, StaticSize = 8, Objects = 1024, Mobiles = 256;

		public readonly byte[] Data;
		public readonly GameKind Game;

		public LevelBlock(byte[] data, GameKind game)
		{
			Data = data;
			Game = game;
		}

		public int W16(int o) { return Data[o] | Data[o + 1] << 8; }
		public short S16(int o) { return (short)W16(o); }
		public void Put16(int o, int v) { Data[o] = (byte)v; Data[o + 1] = (byte)(v >> 8); }

		public bool MagicOk { get { return Data.Length >= MagicAt + 2 && W16(MagicAt) == Magic; } }

		// ---- tiles
		public int TileWord0(int x, int y) { return W16(TilesAt + (y * 64 + x) * 4); }
		public int TileWord1(int x, int y) { return W16(TilesAt + (y * 64 + x) * 4 + 2); }
		public int TileType(int x, int y) { return TileWord0(x, y) & 0xF; }
		public int TileFirstObject(int x, int y) { return TileWord1(x, y) >> 6; }

		// ---- objects
		public static int ObjectOffset(int index) { return index < Mobiles ? MobilesAt + index * MobileSize : StaticsAt + (index - Mobiles) * StaticSize; }
		public int Item(int i) { return W16(ObjectOffset(i)) & 0x1FF; }
		public bool IsQuantity(int i) { return (W16(ObjectOffset(i)) & 0x8000) != 0; }
		public int Next(int i) { return W16(ObjectOffset(i) + 4) >> 6; }
		public int Link(int i) { return W16(ObjectOffset(i) + 6) >> 6; }
		public int Owner(int i) { return W16(ObjectOffset(i) + 6) & 0x3F; }
		public int Quality(int i) { return W16(ObjectOffset(i) + 4) & 0x3F; }
		public int Z(int i) { return W16(ObjectOffset(i) + 2) & 0x7F; }
		public int Whoami(int i) { return i < Mobiles ? Data[ObjectOffset(i) + 0x1A] : 0; }

		// ---- free lists and active list
		public int MobileTop { get { return S16(MobTopAt); } }
		public int StaticTop { get { return S16(StaticTopAt); } }
		public int ActiveCount { get { return W16(ActiveCountAt); } }
		public int MobileFree(int k) { return W16(MobFreeAt + 2 * k); }
		public int StaticFree(int k) { return W16(StaticFreeAt + 2 * k); }
		public int Active(int k) { return Data[ActiveAt + k]; }

		// ---- UW2's overlays and timers (inside the block)
		public int OverlayCount
		{
			get
			{
				if (Game != GameKind.UW2) return 0;
				int n = 0;
				while (n < 64 && (W16(Uw2OverlaysAt + n * 6) >> 6) != 0) n++;
				return n;
			}
		}
		public int TimerCount
		{
			get
			{
				if (Game != GameKind.UW2) return 0;
				int n = 0;
				while (n < 64 && W16(Uw2TimersAt + n * 2) != 0) n++;
				return n;
			}
		}

		/// <summary>
		/// The reference's consistency check (Obj_ListOkay, obj/OBJECTS.C): every index on a free
		/// stack or reached from a tile list (next links, contents of non-quantity objects) exactly
		/// once; free + reached mobiles = 255, statics = 768.
		/// </summary>
		public string Check(out bool ok)
		{
			var counts = new int[Objects];
			var sb = new StringBuilder();
			ok = MagicOk;
			if (!MagicOk) sb.Append("bad magic; ");
			int nMobileFree = MobileTop + 1, nStaticFree = StaticTop + 1;
			int dupFree = 0;
			for (int k = 0; k < nMobileFree && k < 254; k++) { int i = MobileFree(k); if (i < Objects && counts[i]++ > 0) dupFree++; }
			for (int k = 0; k < nStaticFree && k < 768; k++) { int i = StaticFree(k); if (i < Objects && counts[i]++ > 0) dupFree++; }
			int reachedMobile = 0, reachedStatic = 0, twice = 0;
			for (int y = 0; y < 64; y++)
				for (int x = 0; x < 64; x++)
				{
					int head = TileFirstObject(x, y);
					if (head > 0) Walk(head, counts, ref reachedMobile, ref reachedStatic, ref twice, 0);
				}
			// object 1 is the player: off the tile lists in a level file, counted apart by the game (GrSq < 0)
			if (counts[1] == 0) { counts[1] = 1; reachedMobile++; }
			var unreferenced = new List<int>();
			for (int i = 2; i < Objects; i++) if (counts[i] == 0) unreferenced.Add(i);
			bool mobOk = nMobileFree + reachedMobile == 0xFF, statOk = nStaticFree + reachedStatic == 768;
			if (dupFree > 0 || twice > 0 || !mobOk || !statOk) ok = false;
			sb.AppendFormat("free {0} mobile + {1} static; reached {2} mobile + {3} static; duplicates on free stacks {4}; reached twice {5}; unreferenced {6}{7}",
				nMobileFree, nStaticFree, reachedMobile, reachedStatic, dupFree, twice, unreferenced.Count,
				unreferenced.Count > 0 ? " (" + string.Join(" ", unreferenced.GetRange(0, Math.Min(8, unreferenced.Count)).ConvertAll(v => v.ToString()).ToArray()) + (unreferenced.Count > 8 ? " ..." : "") + ")" : "");
			if (!mobOk) sb.Append("; mobile total " + (nMobileFree + reachedMobile) + " != 255");
			if (!statOk) sb.Append("; static total " + (nStaticFree + reachedStatic) + " != 768");
			return sb.ToString();
		}

		void Walk(int i, int[] counts, ref int mob, ref int stat, ref int twice, int depth)
		{
			int guard = 0;
			while (i > 0 && i < Objects && guard++ < 1100)
			{
				if (++counts[i] > 1) { twice++; if (counts[i] > 10) return; }
				if (!IsQuantity(i) && Link(i) > 0 && depth < 64) Walk(Link(i), counts, ref mob, ref stat, ref twice, depth + 1);
				if (i < Mobiles) mob++; else stat++;
				i = Next(i);
			}
		}

		/// <summary>Zeroes every free object record, as UW2's Map_Save does through ObjCrunch before saving.</summary>
		public void CrunchFreeRecords()
		{
			for (int k = 0; k <= MobileTop && k < 254; k++) Array.Clear(Data, ObjectOffset(MobileFree(k)), MobileSize);
			for (int k = 0; k <= StaticTop && k < 768; k++) Array.Clear(Data, ObjectOffset(StaticFree(k)), StaticSize);
		}
	}
}
