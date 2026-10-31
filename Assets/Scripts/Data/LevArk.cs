using System;
using System.Collections.Generic;
using System.IO;
using UltimateEditor.Games;

/// <summary>
/// LEV.ARK for both games through the archive layer (Extensions/Core/Games): each level's
/// block is read into memory and the editor's existing readers run on it at offset 0; a
/// save patches the original block with the editor's writers and hands it back to the
/// archive only if it changed. Unchanged levels stay byte for byte as they were, and a
/// changed one is written the way the game's own put_arc writes it.
/// </summary>
public static class LevArk
{
	/// <summary>The game of the open data (set by Load).</summary>
	public static GameProfile Game = GameProfile.UW1;
	public static Archive Archive { get; private set; }
	public static string Path { get; private set; }

	/// <summary>Blocks the last save wrote (for the status message and the tests).</summary>
	public static readonly List<int> LastWritten = new List<int>();

	/// <summary>Forgets the open archive and the save hooks (opening another game without restarting).</summary>
	public static void Reset()
	{
		Game = GameProfile.UW1;
		Archive = null;
		Path = null;
		LastWritten.Clear();
		BeforeSave = null;
	}

	public static GameKind DetectFor(string levArkPath)
	{
		// A relative path has an empty directory name; resolve it so the folder's files can be seen
		string full = string.IsNullOrEmpty(levArkPath) ? levArkPath : System.IO.Path.GetFullPath(levArkPath);
		return GameProfile.Detect(System.IO.Path.GetDirectoryName(full));
	}

	public static List<LevelData> Load(string path)
	{
		Path = path;
		Game = GameProfile.For(DetectFor(path));
		Archive = Archive.Load(path, Game.Kind);
		int slots = Game.Kind == GameKind.UW1 ? Math.Max(1, Archive.Count / 15) : Game.LevelSlots;
		var levels = new List<LevelData>(slots);
		for (int l = 1; l <= slots; l++) levels.Add(ReadLevel(l));
		// Where each level block starts in the file, for the objects' "file index". UW2's
		// blocks are usually compressed, so there the index is the offset inside the block.
		DataReader.LevelOffsets = new int[slots];
		for (int l = 1; l <= slots; l++)
		{
			int b = Game.LevelBlock(l);
			var a1 = Archive as Uw1Archive;
			var a2 = Archive as Uw2Archive;
			if (a1 != null && a1.Has(b)) DataReader.LevelOffsets[l - 1] = (int)a1.Offset(b);
			else if (a2 != null && a2.Has(b) && !a2.Compressed(b)) DataReader.LevelOffsets[l - 1] = (int)a2.Offset(b);
		}
		return levels;
	}

	static LevelData ReadLevel(int l)
	{
		byte[] block = Archive.Read(Game.LevelBlock(l));
		byte[] overlayBlock = Game.Kind == GameKind.UW2 ? null : Archive.Read(Game.OverlayBlock(l));
		byte[] tex = Archive.Read(Game.TextureBlock(l));
		return LevelFrom(l, block, overlayBlock, tex);
	}

	/// <summary>A level read from its blocks (the level block, UW1's overlay block, the texture
	/// block), as at load. Undo uses it to put a level back as it was (UI/Command.cs).</summary>
	public static LevelData LevelFrom(int l, byte[] block, byte[] overlayBlock, byte[] tex)
	{
		bool present = block != null && block.Length >= LevelBlock.MagicAt + 2;
		byte[] data = present ? block : EmptyBlock(Game);
		var ms = new MemoryStream(data, false);
		MapTile[,] tiles = DataReader.ReadMapData(l, 0, ms);
		int mobCount = DataReader.ReadFreeListStart(l, 0, DataReader.ListType.MobActive, ms);
		int freeMob = DataReader.ReadFreeListStart(l, 0, DataReader.ListType.Mobile, ms);
		int freeStat = DataReader.ReadFreeListStart(l, 0, DataReader.ListType.Static, ms);
		// an empty stack's top is stored as -1 (0xFFFF); keep it signed
		if (freeMob >= 0x8000) freeMob -= 0x10000;
		if (freeStat >= 0x8000) freeStat -= 0x10000;
		int[] free = DataReader.ReadFreeObjectIndexes(l, 0, ms);
		int[] mobList = DataReader.ReadActiveMobs(l, 0, ms);
		StaticObject[] objects = DataReader.ReadStaticObjects(ms, tiles, 0, l, mobList, mobCount);

		AnimationOverlay[] anims;
		if (Game.Kind == GameKind.UW2)
			anims = data.Length >= LevelBlock.Uw2OverlaysAt + 0x180 ? DataReader.ReadAnimationOverlays(l, LevelBlock.Uw2OverlaysAt, ms) : new AnimationOverlay[64];
		else
		{
			// Anim_Load: a block of another length loads as no overlays
			anims = overlayBlock != null && overlayBlock.Length == 0x180 ? DataReader.ReadAnimationOverlays(l, 0, new MemoryStream(overlayBlock, false)) : new AnimationOverlay[64];
		}

		int[] floors, walls, doors;
		ReadTextureBlock(tex, out floors, out walls, out doors);

		var ld = new LevelData(l, tiles, objects, floors, walls, doors, free, mobList, freeMob, freeStat, mobCount, anims);
		ld.Present = present;
		ld.SourceBlock = present ? block : null;
		ld.SourceTextureBlock = tex;
		ld.SourceOverlayBlock = overlayBlock;
		return ld;
	}

	/// <summary>UW1: 48 walls, 10 floors, 6 door bytes (0x7A). UW2: 64 textures for both, 6 door bytes (0x86).</summary>
	static void ReadTextureBlock(byte[] tex, out int[] floors, out int[] walls, out int[] doors)
	{
		if (Game.Kind == GameKind.UW2)
		{
			walls = new int[64];
			floors = new int[16];
			doors = new int[6];
			for (int i = 0; i < 64; i++) walls[i] = tex != null && tex.Length >= 2 * i + 2 ? tex[2 * i] | tex[2 * i + 1] << 8 : i;   // init_txtlib: TxmID[i] = i
			for (int i = 0; i < 16; i++) floors[i] = walls[i];   // a tile's 4-bit floor indexes the same list (map.h)
			for (int i = 0; i < 6; i++) doors[i] = tex != null && tex.Length > 0x80 + i ? tex[0x80 + i] : i;
			return;
		}
		if (tex == null || tex.Length < 0x7A)
		{
			walls = new int[48]; floors = new int[10]; doors = new int[6];
			for (int i = 0; i < 48; i++) walls[i] = i;
			for (int i = 0; i < 10; i++) floors[i] = i;
			for (int i = 0; i < 6; i++) doors[i] = i;
			return;
		}
		var ms = new MemoryStream(tex, false);
		floors = DataReader.ReadFloorTextureMappings(0, 0, ms);
		walls = DataReader.ReadWallTextureMappings(0, 0, ms);
		doors = DataReader.ReadDoorTextureMappings(0, 0, ms);
	}

	/// <summary>
	/// A level with nothing in it, as the game sets one up (obj/OBJECTS.C Obj_Init): every
	/// tile solid, every object free (mobile 2-255, static 256-1023, tops 253 and 767), magic set.
	/// </summary>
	public static byte[] EmptyBlock(GameProfile game)
	{
		var b = new byte[game.LevelBlockSize];
		for (int i = 2; i < 0x400; i++)
		{
			int at = LevelBlock.MobFreeAt + (i - 2) * 2;
			b[at] = (byte)i; b[at + 1] = (byte)(i >> 8);
		}
		b[LevelBlock.MobTopAt] = 253; b[LevelBlock.MobTopAt + 1] = 0;
		b[LevelBlock.StaticTopAt] = 0xFF; b[LevelBlock.StaticTopAt + 1] = 2;   // 767
		b[LevelBlock.MagicAt] = 0x77; b[LevelBlock.MagicAt + 1] = 0x75;
		return b;
	}

	// ---------------------------------------------------------------- saving

	public static void Save(List<LevelData> levels)
	{
		if (Archive == null) throw new InvalidOperationException("LEV.ARK was not loaded through LevArk.");
		if (BeforeSave != null) BeforeSave();   // open tool windows put their pending blocks in
		LastWritten.Clear();
		for (int i = 0; i < levels.Count; i++)
		{
			var ld = levels[i];
			if (ld == null || !ld.Present) continue;
			int l = i + 1;

			byte[] block = BuildLevelBlock(ld, l);
			WriteIfChanged(Game.LevelBlock(l), block);
			ld.SourceBlock = block;

			if (Game.Kind == GameKind.UW1)
			{
				byte[] ov = BuildUw1OverlayBlock(ld);
				if (ov != null) { WriteIfChanged(Game.OverlayBlock(l), ov); ld.SourceOverlayBlock = ov; }
			}
			byte[] tex = BuildTextureBlock(ld);
			WriteIfChanged(Game.TextureBlock(l), tex);
			ld.SourceTextureBlock = tex;
		}
		// written (with the .BKP and a dated version) only when its bytes changed
		GameWrite.Write(Path, Archive.ToBytes());
	}

	/// <summary>The level block a save would write for this level now (for checks; nothing is written).</summary>
	public static byte[] PreviewBlock(LevelData ld)
	{
		return BuildLevelBlock(ld, ld.Level);
	}

	/// <summary>Runs at the start of Save, so a tool window can put pending changes into the archive.</summary>
	public static event Action BeforeSave;

	// ---- automap and map notes (AutomapWindow): kept in the archive, written with it ----

	public static byte[] ReadAutomap(int level) { return ReadAux(Game.AutomapBlock(level)); }
	public static byte[] ReadNotes(int level) { return ReadAux(Game.NotesBlock(level)); }

	static byte[] ReadAux(int block)
	{
		if (Archive == null || block < 0 || block >= Archive.Count || !Archive.Has(block)) return null;
		return Archive.Read(block);
	}

	/// <summary>Puts an automap (0x1000 bytes) into the archive in memory if it differs; null removes the block. True if changed.</summary>
	public static bool WriteAutomap(int level, byte[] map) { return WriteAux(Game.AutomapBlock(level), map); }
	/// <summary>Puts a notes block (0x36 bytes a note) into the archive in memory if it differs. True if changed.</summary>
	public static bool WriteNotes(int level, byte[] notes) { return WriteAux(Game.NotesBlock(level), notes); }

	static bool WriteAux(int block, byte[] data)
	{
		if (Archive == null) throw new InvalidOperationException("LEV.ARK was not loaded through LevArk.");
		var old = ReadAux(block);
		if (data == null)
		{
			if (old == null) return false;
			Archive.Delete(block);
			return true;
		}
		if (old != null && Same(old, data)) return false;
		Archive.Write(block, data);
		return true;
	}

	/// <summary>Writes the archive as it is in memory (levels as last saved, plus automap and note
	/// changes), keeping the editor's one-time .BKP.</summary>
	public static void SaveArchiveNow()
	{
		GameWrite.Write(Path, Archive.ToBytes());
	}

	static void WriteIfChanged(int block, byte[] data)
	{
		var old = Archive.Read(block);
		if (old != null && Same(old, data)) return;
		Archive.Write(block, data);
		LastWritten.Add(block);
	}

	static bool Same(byte[] a, byte[] b)
	{
		if (a.Length != b.Length) return false;
		for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
		return true;
	}

	public static byte[] BuildLevelBlock(LevelData ld, int l)
	{
		byte[] block = (byte[])(ld.SourceBlock ?? EmptyBlock(Game)).Clone();
		byte[] source = ld.SourceBlock;
		var ms = new MemoryStream(block, true);
		DataWriter.SaveTiles(ld.MapTiles, l, 0, ms);
		// Free records keep their bytes here; UW2's Map_Save zeroes them (ObjCrunch), which is
		// done below only if the level changed, so an unedited level stays as shipped.
		DataWriter.SaveObjects(ld.Objects, l, 0, ms, source != null);
		DataWriter.SaveFreeObjectIndexes(ld.FreeObjects, l, 0, ms);
		DataWriter.SaveActiveMobs(ld.MobList, l, 0, ms);
		DataWriter.SaveFreeListStart(l, 0, ld.ActiveMobs, ld.MobileListStart, ld.StaticListStart, ms);
		block[LevelBlock.MagicAt] = 0x77; block[LevelBlock.MagicAt + 1] = 0x75;
		// the tile light bits (word 0 bits 8-9) are MapTile.LightBits, read and written with the tile
		if (Game.Kind == GameKind.UW2)
		{
			WriteUw2Overlays(ld, block, source);
			WriteUw2Timers(ld, block, source);
			if (source == null || !Same(block, source))
			{
				// ObjCrunch zeroes the free records only after the object lists pass the check
				// (MAP.C Map_Save); a failing level is saved with its free records as they were
				bool ok;
				var lb = new LevelBlock(block, GameKind.UW2);
				string report = lb.Check(out ok);
				if (ok) lb.CrunchFreeRecords();
				else UnityEngine.Debug.LogWarning("Level " + l + ": the object lists fail the game's check (" + report + "); free records left as they were. The game will ask before saving this level.");
			}
		}
		return block;
	}

	/// <summary>The live overlay entries the editor has, as (link word, frames, x, y), in object order.</summary>
	static List<int[]> LiveOverlays(LevelData ld)
	{
		var list = new List<int[]>();
		foreach (var obj in ld.Objects)
			if (obj && obj.Animation)
				list.Add(new[] { (obj.Animation.Adress & 0x3FF) << 6, obj.Animation.Duration & 0xFFFF, obj.Animation.X & 0xFF, obj.Animation.Y & 0xFF });
		if (list.Count > 64) list.RemoveRange(64, list.Count - 64);
		return list;
	}

	static List<int[]> LiveOverlaysIn(byte[] b, int at)
	{
		var list = new List<int[]>();
		for (int i = 0; i < 64; i++)
		{
			int o = at + i * 6;
			int link = b[o] | b[o + 1] << 8;
			if ((link >> 6) == 0) break;
			list.Add(new[] { link, b[o + 2] | b[o + 3] << 8, b[o + 4], b[o + 5] });
		}
		return list;
	}

	/// <summary>Same entries regardless of order.</summary>
	static bool SameSet(List<int[]> a, List<int[]> b)
	{
		if (a.Count != b.Count) return false;
		var keys = new List<string>();
		foreach (var e in a) keys.Add(string.Join(",", Array.ConvertAll(e, x => x.ToString())));
		foreach (var e in b)
		{
			string k = string.Join(",", Array.ConvertAll(e, x => x.ToString()));
			if (!keys.Remove(k)) return false;
		}
		return true;
	}

	static void PutOverlays(byte[] b, int at, List<int[]> entries, bool zeroRest)
	{
		for (int i = 0; i < 64; i++)
		{
			int o = at + i * 6;
			if (i < entries.Count)
			{
				var e = entries[i];
				b[o] = (byte)e[0]; b[o + 1] = (byte)(e[0] >> 8);
				b[o + 2] = (byte)e[1]; b[o + 3] = (byte)(e[1] >> 8);
				b[o + 4] = (byte)e[2]; b[o + 5] = (byte)e[3];
			}
			else if (zeroRest || i == entries.Count)
				for (int k = 0; k < 6; k++) b[o + k] = 0;
		}
	}

	/// <summary>
	/// UW2 keeps 64 overlays inside the level block. If the editor's live set equals the
	/// file's, the bytes stay as they were; otherwise the list is written in object order and
	/// the rest zeroed, as UW2's Anim_Save does.
	/// </summary>
	static void WriteUw2Overlays(LevelData ld, byte[] block, byte[] source)
	{
		var live = LiveOverlays(ld);
		if (source != null && SameSet(live, LiveOverlaysIn(source, LevelBlock.Uw2OverlaysAt))) return;
		PutOverlays(block, LevelBlock.Uw2OverlaysAt, live, true);
	}

	static bool IsTimerTrigger(StaticObject o) { return o != null && (o.ObjectID == 0x1A9 || o.ObjectID == 0x1B9); }

	/// <summary>
	/// UW2's 64 timer words (object indexes of the timer triggers, EFFECT.C timerlist). The game
	/// never adds to the list (add_timer_obj has no caller), so the editor keeps it: entries
	/// whose object is still a timer trigger stay in their order, timer triggers not listed
	/// are added, the rest is zeroed. An unchanged list leaves the bytes alone.
	/// </summary>
	static void WriteUw2Timers(LevelData ld, byte[] block, byte[] source)
	{
		int at = LevelBlock.Uw2TimersAt;
		var old = new List<int>();
		if (source != null && source.Length >= at + 128)
			for (int i = 0; i < 64; i++)
			{
				int v = source[at + 2 * i] | source[at + 2 * i + 1] << 8;
				if (v == 0) break;
				old.Add(v);
			}
		var list = new List<int>();
		foreach (int v in old)
			if (v < ld.Objects.Length && IsTimerTrigger(ld.Objects[v]) && !list.Contains(v)) list.Add(v);
		for (int i = 0; i < ld.Objects.Length; i++)
			if (IsTimerTrigger(ld.Objects[i]) && !list.Contains(i)) list.Add(i);
		if (list.Count > 64)
		{
			UnityEngine.Debug.LogWarning("Level " + ld.Level + ": " + list.Count + " timer triggers; the game keeps 64, the rest will not fire.");
			list.RemoveRange(64, list.Count - 64);
		}
		bool same = list.Count == old.Count;
		for (int i = 0; same && i < list.Count; i++) same = list[i] == old[i];
		if (same && source != null) return;
		for (int i = 0; i < 64; i++)
		{
			int v = i < list.Count ? list[i] : 0;
			block[at + 2 * i] = (byte)v; block[at + 2 * i + 1] = (byte)(v >> 8);
		}
	}

	/// <summary>
	/// UW1's overlay block (0x180): unchanged set, unchanged bytes. Otherwise written in object
	/// order, zeroing only the entry after the list, as UW1's Anim_Save does (stale entries after it stay).
	/// </summary>
	public static byte[] BuildUw1OverlayBlock(LevelData ld)
	{
		var src = ld.SourceOverlayBlock;
		var live = LiveOverlays(ld);
		if (src != null && src.Length == 0x180 && SameSet(live, LiveOverlaysIn(src, 0))) return src;
		var b = src != null && src.Length == 0x180 ? (byte[])src.Clone() : new byte[0x180];
		PutOverlays(b, 0, live, false);
		return b;
	}

	static byte[] BuildTextureBlock(LevelData ld)
	{
		if (Game.Kind == GameKind.UW2)
		{
			var b = ld.SourceTextureBlock != null && ld.SourceTextureBlock.Length == 0x86 ? (byte[])ld.SourceTextureBlock.Clone() : new byte[0x86];
			// One table of 64: the floor list is a view of its first 16 entries. Whichever list
			// the user changed (against the block as read) wins for those entries.
			for (int i = 0; i < 64; i++)
			{
				int src = b[2 * i] | b[2 * i + 1] << 8;
				int v = ld.WallTextures[i];
				if (v == src && i < ld.FloorTextures.Length) v = ld.FloorTextures[i];
				b[2 * i] = (byte)v; b[2 * i + 1] = (byte)(v >> 8);
			}
			for (int i = 0; i < 6; i++) b[0x80 + i] = (byte)ld.DoorTextures[i];
			return b;
		}
		var t = ld.SourceTextureBlock != null && ld.SourceTextureBlock.Length == 0x7A ? (byte[])ld.SourceTextureBlock.Clone() : new byte[0x7A];
		var ms = new MemoryStream(t, true);
		DataWriter.SaveWallTextureMappings(ld.Level, 0, ms, ld.WallTextures);
		DataWriter.SaveFloorTextureMappings(ld.Level, 0, ms, ld.FloorTextures);
		DataWriter.SaveDoorTextureMappings(ld.Level, 0, ms, ld.DoorTextures);
		return t;
	}
}
