using System;
using UltimateEditor.Games;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Newtonsoft.Json;

public static class DataWriter {

	#region Main

	//From DataWriter(DunBase)
	public static void WriteToJson(object obj, string filePath)
	{
		using (StreamWriter sw = new StreamWriter(filePath))
		{
			using (JsonTextWriter jtw = new JsonTextWriter(sw))
			{
				jtw.Formatting = Formatting.Indented;
				jtw.Indentation = 1;
				jtw.IndentChar = '\t';
				JsonSerializer js = new JsonSerializer();
				js.Formatting = Formatting.Indented;
				js.Serialize(jtw, obj);
			}
		}
	}

	public static void SavePathToFile(string path)
	{
		string appPath = Application.dataPath + "/config.cfg";
		StreamWriter sw = new StreamWriter(appPath);

		foreach (var letter in path)
			sw.Write(letter);
		sw.Close();
	}

	public static void SaveStringToTxt(string toSave, string fileName)
	{
		string appPath = Application.dataPath + "/" + fileName;
		StreamWriter sw = new StreamWriter(appPath);

		sw.Write(toSave);
		sw.Close();
	}

	public static void SaveLevelData(List<LevelData> levels)
	{
		// the LEV.ARK being edited: DATA's or a save's (Data/GameSession.cs). Data/LevArk.cs
		// patches each level's original block, and the file is written (through GameWrite)
		// only when a block changed.
		LevArk.Save(levels);
	}

	private static void SaveLevelOffset(Stream fs, long offset)
	{
		byte b1 = (byte)(offset & 0xFF);
		byte b2 = (byte)((offset & 0xFF00) >> 8);
		byte b3 = (byte)((offset & 0xFF0000) >> 16);
		byte b4 = (byte)((offset & 0xFF000000) >> 24);

		fs.WriteByte(b1);
		fs.WriteByte(b2);
		fs.WriteByte(b3);
		fs.WriteByte(b4);
	}

	public static void SaveTextureData(TextureData textures)
	{
		// Both games write only the pictures that were replaced: UW2 patches T64.TR (Uw2T64),
		// UW1 its four .TR files (Uw1TrFiles), and both rebuild a .GR file with only its
		// replaced pictures re-encoded (GrPictures). A file with no replaced picture is not
		// written, so it keeps the shipped bytes.
		GrPictures.Problems.Clear();
		var report = new List<string>();
		if (LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW2)
		{
			List<int> changed; List<string> warnings;
			var t64 = Uw2T64.Build(textures.Walls, textures.Floors, out changed, out warnings);
			foreach (var w in warnings) GrPictures.Warn(report, "T64.TR: " + w);
			if (t64 != null && changed.Count > 0)
			{
				GameWrite.Write(Uw2T64.Path, t64);
				if (!GameWrite.Snapshot)
					Uw2T64.Saved(t64, textures.Walls, textures.Floors, changed);
				report.Add("T64.TR: " + changed.Count + " picture(s) written");
			}
			report.AddRange(GrPictures.Save(textures));
		}
		else
		{
			// UW1 can add and remove walls, floors, doors and character heads, which changes a
			// file's list and offsets: such a file is written whole, by the old writers.
			string dataDir = FileExplorer.GetUpperPath(DataReader.FilePath);
			// Without the palette table the whole-file writers would write the headers and
			// offsets and none of the pictures, which the game can't load; such files are left.
			bool table = PaletteMapping != null;
			if (!table)
				GrPictures.Warn(report, "The palette table (StreamingAssets/mapping.dat) is missing: files whose number of pictures changed were not written");
			if (Uw1TrFiles.CanPatch(true, textures.Walls))
				Uw1TrFiles.Save(true, textures.Walls, report);
			else if (table)
			{
				SaveTextures("W64.TR", textures.Walls, textures.Palettes[0], 2);
				SaveTextures("W16.TR", textures.Walls, textures.Palettes[0], 2, 4);
				if (!GameWrite.Snapshot)
					Uw1TrFiles.RememberWalls(dataDir, textures.Walls.Textures);
				report.Add("W64.TR and W16.TR: written whole (the number of textures changed)");
			}
			if (Uw1TrFiles.CanPatch(false, textures.Floors))
				Uw1TrFiles.Save(false, textures.Floors, report);
			else if (table)
			{
				SaveTextures("F32.TR", textures.Floors, textures.Palettes[0], 2);
				SaveTextures("F16.TR", textures.Floors, textures.Palettes[0], 2, 2);
				if (!GameWrite.Snapshot)
					Uw1TrFiles.RememberFloors(dataDir, textures.Floors.Textures);
				report.Add("F32.TR and F16.TR: written whole (the number of textures changed)");
			}
			var whole = new List<string>();
			if (table && !GrPictures.CanPatch("DOORS.GR", textures.Doors))
			{
				SaveTextures("DOORS.GR", textures.Doors, textures.Palettes[0], 1);
				if (!GameWrite.Snapshot) GrPictures.Remember("DOORS.GR", GameFiles.Resolve(dataDir + "/DOORS.GR"), textures.Doors.Textures, textures.Palettes[0]);
				whole.Add("DOORS.GR");
			}
			if (table && !GrPictures.CanPatch("CHARHEAD.GR", textures.NPCHeads))
			{
				SaveTextures("CHARHEAD.GR", textures.NPCHeads, textures.Palettes[0], 1, 0, 56 - textures.NPCHeads.Count);
				if (!GameWrite.Snapshot) GrPictures.Remember("CHARHEAD.GR", GameFiles.Resolve(dataDir + "/CHARHEAD.GR"), textures.NPCHeads.Textures, textures.Palettes[0]);
				whole.Add("CHARHEAD.GR");
			}
			foreach (var f in whole) report.Add(f + ": written whole (the number of pictures changed)");
			report.AddRange(GrPictures.Save(textures, whole));
		}
		foreach (var line in report) Debug.Log(line);
		SaveTerrainData(textures);
	}
	public static void SaveStringData(StringData strings)
	{
		string file = "STRINGS.PAK";
		string path = GameFiles.Resolve(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file);
		// Nothing changed since the strings were read or last saved: the file is as they are.
		if (File.Exists(path) && !strings.ChangedSinceSnapshot())
			return;
		strings.SortBlocks();
		// Check every string before touching the file: a character the Huffman tree cannot
		// encode used to stop the block half written, leaving garbage offsets behind it.
		foreach (var block in strings.Blocks)
			for (int i = 0; i < block.StringCount; i++)
				if (!StringsPak.CanEncode(block.Strings[i], strings.Nodes))
					throw new InvalidDataException(string.Format("String {0} of block {1} has a character STRINGS.PAK cannot encode:\n{2}", i, block.BlockNumber, block.Strings[i]));
		var fs = new MemoryStream();
		long tableStart = SaveHuffmanNodes(fs, strings.Nodes);
		SaveStringBlocks(fs, strings.Blocks, strings.Nodes, tableStart);
		GameWrite.Write(path, fs.ToArray());
		if (!GameWrite.Snapshot)
			strings.TakeSnapshot();
		//StreamWriter sw = new StreamWriter(Application.dataPath + "/strings.txt");
		//foreach (var block in strings.Blocks)
		//{
		//	for (int i = 0; i < block.StringCount; i++)
		//	{
		//		sw.WriteLine(block.BlockNumber + "=" + i + "=" + block.Strings[i]);
		//	}
		//}
		//sw.Close();
	}

	public static void SaveConversationData(ConversationData convs)
	{
		string data = FileExplorer.GetUpperPath(DataReader.FilePath);
		// The writer produces UW1's layout in memory; CnvArk writes the changed blocks through
		// the game's own archive format (UW2: compressed) and leaves the others as they were.
		var image = new MemoryStream();
		SaveConversations(image, convs);
		GameWrite.Write(GameFiles.Resolve(data + "/CNV.ARK"), CnvArk.Merge(image.ToArray()));
		var globs = new MemoryStream();
		SaveBabglobs(globs, convs);
		GameWrite.Write(GameFiles.Resolve(data + "/BABGLOBS.DAT"), globs.ToArray());
	}

	public static void SaveObjectDatas(ObjectData objDat)
	{
		SaveCommonData(objDat);
		SaveObjectData(objDat);
		SaveMonsterSpriteInfo(objDat);
	}

	static bool SameBytes(byte[] a, byte[] b)
	{
		if (a.Length != b.Length) return false;
		for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
		return true;
	}

	/// <summary>The .BKP original and a dated version of a DATA file, before it is written by
	/// other means than GameWrite.Write (which does this itself).</summary>
	public static void BackupData(string toBackup)
	{
		GameWrite.Backup(GameFiles.Resolve(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + toBackup));
	}

	#endregion

	#region Level

	public static void SaveTiles(MapTile[,] tiles, int level, long offset, Stream fs)
	{
		//int levelIndex = DataReader.GetLevelIndex(level, fs);
		for (int y = 0; y < 64; y++)
		{
			for (int x = 0; x < 64; x++)
			{
				//fs.Position = levelIndex + (y * 64 + x) * 4;
				fs.Position = offset + (y * 64 + x) * 4;
				SaveTile(tiles[x, y], fs);
			}
		}
	}

	/// <param name="keepFree">Leave the records of slots without an object as they are (UW1's
	/// game keeps their stale bytes) instead of zeroing them (UW2's ObjCrunch zeroes them).</param>
	public static void SaveObjects(StaticObject[] objects, int level, long offset, Stream fs, bool keepFree = false)
	{
		//int levelIndex = DataReader.GetLevelIndex(level, fs);
		for (int i = 2; i < 256 + 768; i++)
		{
			if(i < 256)
			{
				//fs.Position = levelIndex + 16384 + i * 27;
				fs.Position = offset + 16384 + i * 27;
				if (objects[i] == null)
				{
					if (!keepFree) ClearMobileObject(i, fs);
				}
				else
					SaveMobileObject((MobileObject)objects[i], fs);
			}
			else
			{
				//fs.Position = levelIndex + 23296 + (i - 256) * 8;
				fs.Position = offset + 23296 + (i - 256) * 8;
				if (objects[i] == null)
				{
					if (!keepFree) ClearStaticObject(i, fs);
				}
				else
					SaveStaticObject(objects[i], fs);
			}
		}
	}

	public static void ClearStaticObject(int index, Stream fs)
	{
		for (int i = 0; i < 8; i++)
			fs.WriteByte(0);
	}

	public static void ClearMobileObject(int index, Stream fs)
	{
		for (int i = 0; i < 27; i++)
			fs.WriteByte(0);
	}

	public static void SaveTile(MapTile tile, Stream fs)
	{
		//FileStream fs = new FileStream(Application.dataPath + "/write.txt", FileMode.OpenOrCreate);
		//Debug.Log("Save tile");
		// Every field is masked to its width, so a value out of range can't spill into the next.
		byte a = (byte)(((int)tile.TileType & 0x0F) + ((tile.FloorHeight & 0x0F) << 4));
		byte b = (byte)((tile.LightBits & 0x03) + ((tile.FloorTexture & 0x0F) << 2) + ((tile.IsAntimagic == true ? 1 : 0) << 6) + ((tile.IsDoor == true ? 1 : 0) << 7));
		//byte c = (byte)(tile.WallTexture + ((tile.ObjectAdress & 0x0F) << 4));
		//byte d = (byte)((tile.ObjectAdress & 0xFF0) >> 4);
		byte c = (byte)((tile.WallTexture & 0x3F) + ((tile.ObjectAdress & 0x03) << 6));
		byte d = (byte)((tile.ObjectAdress & 0xFFC) >> 2);
		//Debug.LogFormat("tile.ObjectAdress & 0x300 : {0}, tile.ObjectAdress & 0x300 >> 2 : {1}, tile.WallTexture : {2}, tile.ObjectAdress : {3}, tile.ObjectAdress & 0xFF0 : {4}", (tile.ObjectAdress & 0x300), (tile.ObjectAdress & 0x300) >> 2, tile.WallTexture, tile.ObjectAdress, tile.ObjectAdress & 0xFF);
		fs.WriteByte(a);
		fs.WriteByte(b);
		fs.WriteByte(c);
		fs.WriteByte(d);

		//fs.Close();
	}

	/// <summary>The object's 8 static bytes, every field kept to its width (ObjectRecord), so a
	/// value out of range can't spill into the next field.</summary>
	private static void SaveStaticObject(StaticObject so, Stream fs)
	{
		var b = new byte[ObjectRecord.StaticSize];
		ObjectRecord.Write(so, b, 0);
		fs.Write(b, 0, b.Length);
	}

	public static void SaveMobileObject(MobileObject mo, Stream fs)
	{
		var b = ObjectRecord.From(mo);
		fs.Write(b, 0, b.Length);
	}


	public static void SaveFreeObjectIndexes(int[] freeList, int level, long offset, Stream fs)
	{
		//int index = DataReader.GetLevelIndex(level, fs) + 29440;
		long index = offset + 29440;
		int max = 768 + 254;

		for (int i = 0; i < max; i++)
		{
			int loc = i * 2;
			fs.Position = index + loc;
			byte a = (byte)(freeList[i] & 0xFF);
			byte b = (byte)((freeList[i] & 0xFF00) >> 8);
			fs.WriteByte(a);
			fs.WriteByte(b);
		}
	}

	public static void SaveActiveMobs(int[] mobList, int level, long offset, Stream fs)
	{
		//int index = DataReader.GetLevelIndex(level, fs) + 31484;
		long index = offset + 31484;

		for (int i = 0; i < 260; i++)
		{
			fs.Position = index + i;
			byte a = (byte)mobList[i];
			fs.WriteByte(a);
		}
	}

	public static void SaveFreeListStart(int level, long offset, int moba, int mob, int stat, Stream fs)
	{
		//int index = 31744 + DataReader.GetLevelIndex(level, fs);
		long index = offset + 31744;

		fs.Position = index;

		byte e = (byte)(moba & 0xFF);
		byte f = (byte)((moba & 0xFF00) >> 8);
		byte a = (byte)(mob & 0xFF);
		byte b = (byte)((mob & 0xFF00) >> 8);
		byte c = (byte)(stat & 0xFF);
		byte d = (byte)((stat & 0xFF00) >> 8);

		fs.WriteByte(e);
		fs.WriteByte(f);
		fs.WriteByte(a);
		fs.WriteByte(b);
		fs.WriteByte(c);
		fs.WriteByte(d);
	}

	public static void SaveFloorTextureMappings(int level, long offset, Stream fs, int[] floormap)
	{
		//int index = 289766 + (level - 1) * 122 + 48 * 2;
		long index = offset + 48 * 2;
		fs.Position = index;
		for (int i = 0; i < 10; i++)
		{
			byte a = (byte)(floormap[i] & 0xFF);
			byte b = (byte)((floormap[i] & 0xFF00) >> 8);
			fs.WriteByte(a);
			fs.WriteByte(b);
		}
	}

	public static void SaveWallTextureMappings(int level, long offset, Stream fs, int[] wallmap)
	{
		//int index = 289766 + (level - 1) * 122;
		long index = offset;
		fs.Position = index;
		for (int i = 0; i < 48; i++)
		{
			byte a = (byte)(wallmap[i] & 0xFF);
			byte b = (byte)((wallmap[i] & 0xFF00) >> 8);
			fs.WriteByte(a);
			fs.WriteByte(b);
		}
	}

	public static void SaveDoorTextureMappings(int level, long offset, Stream fs, int[] doormap)
	{
		//int index = 289766 + (level - 1) * 122 + 58 * 2;
		long index = offset + 58 * 2;
		fs.Position = index;
		for (int i = 0; i < 6; i++)
		{
			fs.WriteByte((byte)doormap[i]);
		}
	}

	public static void SaveAnimationOverlays(long offset, LevelData levelData, Stream fs)
	{
		fs.Position = offset;
		int count = 0;
		foreach (var obj in levelData.Objects)
		{
			if(obj && obj.Animation)
			{
				SaveAnimationOverlay(obj.Animation, fs);
				count++;
			}
			if (count == 64)
				break;
		}
		fs.Position = offset + 384;
	}

	public static void SaveAnimationOverlay(AnimationOverlay anim, Stream fs)
	{
		// struct Anim.link: object index in bits 6-15 (reference object.h, FORMATS.md);
		// the reader takes (word >> 6) & 0x3FF, so shift back. Bits 0-5 are 0 in the shipped file.
		int link = (anim.Adress & 0x3FF) << 6;
		byte adr_a = (byte)(link & 0xFF);
		byte adr_b = (byte)((link & 0xFF00) >> 8);
		byte dur_a = (byte)(anim.Duration & 0xFF);
		byte dur_b = (byte)((anim.Duration & 0xFF00) >> 8);
		fs.WriteByte(adr_a);
		fs.WriteByte(adr_b);
		fs.WriteByte(dur_a);
		fs.WriteByte(dur_b);
		fs.WriteByte((byte)anim.X);
		fs.WriteByte((byte)anim.Y);
	}

	#endregion

	#region Textures

	public static int[] SaveTextureOffsets(Stream fs, string fileName, TextureContainer texCon, int scaleFactor = 0, int fakeTextures = 0)
	{
		if (texCon.Offset == -1)
			return null;

		//string path = GameFiles.Resolve(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + fileName);
		//FileStream fs = new FileStream(path, FileMode.Open);
		//Debug.LogFormat("SaveTextureOffsets, fakeTex : {0}, count : {1}", fakeTextures, texCon.Count);
		int firstOffset = texCon.FirstOffset + (texCon.Count + fakeTextures + 1) * 4;
		int[] offsets = new int[texCon.Count + fakeTextures];
		for (int i = 0; i < texCon.Count + fakeTextures + 1; i++)
		{
			int index = i;
			if (i >= texCon.Count)
				index = texCon.Count;
			
			fs.Position = texCon.FirstOffset + i * 4;
			// a small copy is scale * scale times smaller (this divided by scale * 2, which
			// doubled W16.TR with gaps; F16's scale of 2 hid it)
			int offset = firstOffset + (scaleFactor > 0 ? texCon.Offset / (scaleFactor * scaleFactor) : texCon.Offset) * index;
			//Debug.LogFormat("offset : {0}, i : {1}, index : {2}", offset, i, index);
			byte a = (byte)(offset & 0xFF);
			byte b = (byte)((offset & 0xFF00) >> 8);
			byte c = (byte)((offset & 0xFF0000) >> 16);
			byte d = (byte)((offset & 0xFF000000) >> 24);

			fs.WriteByte(a);
			fs.WriteByte(b);
			fs.WriteByte(c);
			fs.WriteByte(d);

			if(i < texCon.Count)
				offsets[i] = offset;
		}
		//fs.Close();
		return offsets;
	}

	public static void SaveTextures(string file, TextureContainer textures, Color[] palette, int type, int scaleFactor = 0, int fakeTextures = 0, bool reverse = false)
	{
		var fs = new MemoryStream();
		//fs.Position = textures.FirstOffset - 2;
		//fs.WriteByte((byte)textures.Count);
		fs.WriteByte((byte)type);
		int count = textures.Count + fakeTextures;
		byte count_a = (byte)(count & 0xFF);
		byte count_b = (byte)((count & 0xFF00) >> 8);
		float size = scaleFactor > 0 ? textures.ATI.Width * (1.0f / scaleFactor) : textures.ATI.Width;
		if(type == 2)	//Always same size for all textures, and square
			fs.WriteByte((byte)size);		
		fs.WriteByte(count_a);
		fs.WriteByte(count_b);
		int[] floorOffsets = SaveTextureOffsets(fs, file, textures, scaleFactor, fakeTextures);
		for (int i = 0; i < textures.Textures.Count; i++)
		{
			Texture2D tex = scaleFactor > 0 ? ScaleDownTexture(textures.Textures[i], scaleFactor) : textures.Textures[i];	//Only for floors and walls
			int[] raw = ConvertToRawTexture(tex, palette, reverse);
			if (scaleFactor > 0)
				UnityEngine.Object.Destroy(tex);	// the scaled copy is only for this file
			SaveTexture(fs, file, floorOffsets[i], raw, textures.ATI);
		}
		GameWrite.Write(GameFiles.Resolve(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file), fs.ToArray());
	}


	public static void SaveTexture(Stream fs, string fileName, int offset, int[] rawTex, AdditionalTextureInfo ati) { GraphicsCodec.SaveTexture(fs, fileName, offset, rawTex, ati); }


	public static void SaveNibbles(Stream fs, long offset, List<int> nibbles) { GraphicsCodec.SaveNibbles(fs, offset, nibbles); }
	private static bool compareRaw(int[] A, int[] B)
	{
		if (A.Length != B.Length)
			return false;
		for (int i = 0; i < A.Length; i++)
		{
			if (A[i] != B[i])
				return false;
		}
		return true;
	}

	/// <summary>
	/// This cuts the texture, no compression
	/// </summary>
	public static Texture2D ScaleDownTexture(Texture2D baseTex, int scaleFactor)
	{
		int width = baseTex.width / scaleFactor;
		int height = baseTex.height / scaleFactor;
		Texture2D scaledTex = new Texture2D(width, height);
		scaledTex.filterMode = FilterMode.Point;
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				Color pix = baseTex.GetPixel(x * scaleFactor, y * scaleFactor);
				scaledTex.SetPixel(x, y, pix);
			}
		}
		scaledTex.Apply();
		return scaledTex;
	}



	public static bool IsCloserToColorThan32(Color32 toCompare, Color32 toCompareTo, Color32 currentClosest)
	{
		//Debug.LogFormat("Comparing colors, to compare : {0}, to compare to : {1}, current closest : {2}, new sum : {3}, old sum : {4}", toCompare, toCompareTo, currentClosest, newSum, oldSum);
		if (GetHueDifferance32(0, toCompare, toCompareTo) + GetHueDifferance32(1, toCompare, toCompareTo) + GetHueDifferance32(2, toCompare, toCompareTo) < GetHueDifferance32(0, currentClosest, toCompareTo) + GetHueDifferance32(1, currentClosest, toCompareTo) + GetHueDifferance32(2, currentClosest, toCompareTo))
			return true;
		return false;
	}
	public static bool IsCloserToColorThan(Color toCompare, Color toCompareTo, Color currentClosest)
	{
		//Debug.LogFormat("Comparing colors, to compare : {0}, to compare to : {1}, current closest : {2}, new sum : {3}, old sum : {4}", toCompare, toCompareTo, currentClosest, newSum, oldSum);
		if (GetHueDifferance(0, toCompare, toCompareTo) + GetHueDifferance(1, toCompare, toCompareTo) + GetHueDifferance(2, toCompare, toCompareTo) < GetHueDifferance(0, currentClosest, toCompareTo) + GetHueDifferance(1, currentClosest, toCompareTo) + GetHueDifferance(2, currentClosest, toCompareTo))
			return true;
		return false;
	}
	public static int GetHueDifferance32(int hue, Color32 toCompare, Color32 toCompareTo)
	{
		if (hue == 0)
			return Mathf.Abs(toCompareTo.r - toCompare.r);
		else if (hue == 1)
			return Mathf.Abs(toCompareTo.g - toCompare.g);
		else if (hue == 2)
			return Mathf.Abs(toCompareTo.b - toCompare.b);

		return 0;
	}
	public static float GetHueDifferance(int hue, Color toCompare, Color toCompareTo)
	{
		if (hue == 0)
			return Mathf.Abs(toCompareTo.r - toCompare.r);
		else if (hue == 1)
			return Mathf.Abs(toCompareTo.g - toCompare.g);
		else if (hue == 2)
			return Mathf.Abs(toCompareTo.b - toCompare.b);

		return 0;
	}
	static byte[] paletteMapping;

	/// <summary>The editor's palette table (StreamingAssets/mapping.dat): the palette 0 index of
	/// every 24-bit colour, 16 MB, read once. It used to be read with a seek per pixel, which
	/// made a UW1 save take seconds. Null when the file is missing.</summary>
	static byte[] PaletteMapping
	{
		get
		{
			if (paletteMapping == null)
			{
				string path = Application.streamingAssetsPath + "/mapping.dat";
				if (File.Exists(path))
					paletteMapping = File.ReadAllBytes(path);
			}
			return paletteMapping;
		}
	}

	/// <summary>Palette 0 index of an opaque colour, through the editor's palette table; -1 when
	/// the table is missing.</summary>
	public static int PaletteIndex(Color32 c)
	{
		var map = PaletteMapping;
		return map == null ? -1 : map[c.r * 65536 + c.g * 256 + c.b];
	}

	/// <summary>A picture's palette indices, top row first (as the .TR and .GR files hold them):
	/// transparent pixels are 0, the others go through the palette table. Null when the table
	/// is missing.</summary>
	public static int[] ToPaletteIndexes(Texture2D tex)
	{
		var map = PaletteMapping;
		if (map == null) return null;
		int w = tex.width, h = tex.height;
		var raw = new int[w * h];
		Color32[] px = tex.GetPixels32();	// bottom row first
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				Color32 c = px[x + (h - 1 - y) * w];
				raw[x + y * w] = c.a == 0 ? 0 : map[c.r * 65536 + c.g * 256 + c.b];
			}
		return raw;
	}

	public static int[] ConvertToRawTexture(Texture2D tex, Color[] palette, bool reverse)
	{
		var map = PaletteMapping;
		if (map == null)
			return null;
		int size = tex.width * tex.height;
		int[] rawTex = new int[size];
		int index = 0;
		Color32[] px = tex.GetPixels32();	// row by row from the bottom, as GetPixel(x, y) counts

		for (int y = 0; y < tex.height; y++)
		{
			for (int x = 0; x < tex.width; x++)
			{
				if (!reverse)
					index = size - (x + y * tex.width) - 1;
				else
					index = x + y * tex.height;
				Color32 col = px[(tex.width - x - 1) + y * tex.width];
				if (col.a == 0)
					rawTex[index] = 0;
				else
					rawTex[index] = map[col.r * 65536 + col.g * 256 + col.b];
			}
		}
		return rawTex;
	}
	public static Texture2D ConvertToAuxPalette(Texture2D tex, Color[] auxPal, int[] rawTex, bool reverse)
	{
		Texture2D newTex = new Texture2D(tex.width, tex.height);
		newTex.filterMode = FilterMode.Point;
		int size = tex.width * tex.height;
		Color[] pixels = new Color[size];
		int index = 0;
		int[] reversed = new int[size];
		for (int y = 0; y < tex.height; y++)
		{
			for (int x = 0; x < tex.width; x++)
			{
				if (!reverse)
					index = size - (x + y * tex.width) - 1;
				else
					index = x + y * tex.width;

				Color toCompare = tex.GetPixel(x, y);
				Color curClosest = auxPal[0];
				int curIndex = 0;
				if (toCompare.a == 0)
				{
					try
					{
						pixels[index] = new Color(0, 0, 0, 0);
					}
					catch
					{
						Debug.LogWarningFormat("WTF, x : {0}, y : {1}, index : {2}, size : {3}, width : {4}, height : {5}", x, y, index, size, tex.width, tex.height);
					}
					
				}
				else
				{
					for (int i = 0; i < auxPal.Length; i++)
					{
						Color toCompareTo = auxPal[i];
						if (IsCloserToColorThan(toCompareTo, toCompare, curClosest))
						{
							curClosest = auxPal[i];
							curIndex = i;
						}
					}
					//Debug.LogFormat("Setting pixel {0}/{1} color to {2}, that is index {3}", x, y, curClosest, curIndex);
					pixels[index] = curClosest;
					reversed[index] = curIndex;
				}
			}
		}
		int row = tex.height - 1;
		int col = 0;
		for (int i = 0; i < size; i++)
		{
			int j = row * tex.width + col;
			rawTex[i] = reversed[j];

			col++;
			if(col > tex.width - 1)
			{
				col = 0;
				row--;
			}
		}
		newTex.SetPixels(pixels);
		newTex.Apply();
		return newTex;
	}
	public static List<int> CompressToRLE(int[] rawTex) { return GraphicsCodec.CompressToRLE(rawTex); }
	public static List<Tuple<int, int>> getCounts(int[] rawTex, ref int ptr) { return GraphicsCodec.getCounts(rawTex, ref ptr); }
	

	public static void ExportDefaultTextures()
	{
		Export("Export/Floors", DataReader.FloorTextures);
		Export("Export/Walls", DataReader.WallTextures);
		Export("Export/Doors", DataReader.DoorTextures);
		Export("Export/Levers", DataReader.LeverTextures);
		Export("Export/Other", DataReader.OtherTextures);
		Export("Export/Portraits", DataReader.Portraits);
		Export("Export/Objects", DataReader.ObjectGraphics);
	}

	static void Export(string path, List<Texture2D> textures)
	{
		if (textures != null)
			ExportDefaultTextures(path, textures, textures.Count);
	}

	public static void ExportDefaultTextures(string path, List<Texture2D> resources, int count)
	{
		path = Application.dataPath + "/" + path;
		bool dirExists = Directory.Exists(path);
		if (!dirExists)
			Directory.CreateDirectory(path);

		for(int i = 0; i < count; i++)
		{
			Texture2D tex = resources[i];
			if (!tex) continue;
			string fileName = path + "/" + tex.name + ".png";
			byte[] png = ImageConversion.EncodeToPNG(tex);
			if(File.Exists(fileName))
				File.Delete(fileName);
			File.WriteAllBytes(fileName, png);
		}
	}

	public static void SaveTerrainData(TextureData texData)
	{
		string file = "TERRAIN.DAT";
		string path = GameFiles.Resolve(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file);
		var fs = new MemoryStream();

		for (int i = 0; i < 256; i++)
		{
			byte a = (byte)(texData.WallTypes[i] & 0xFF);
			byte b = (byte)((texData.WallTypes[i] & 0xFF00) >> 8);
			fs.WriteByte(a);
			fs.WriteByte(b);
		}
		// UW2's file is the one table of 256 words (one per T64.TR texture)
		for (int i = 0; i < 256 && LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW1; i++)
		{
			byte a = (byte)(texData.FloorTypes[i] & 0xFF);
			byte b = (byte)((texData.FloorTypes[i] & 0xFF00) >> 8);
			fs.WriteByte(a);
			fs.WriteByte(b);
		}
		// written only when a value changed, so an unchanged file keeps its date
		GameWrite.Write(path, fs.ToArray());
	}

	#endregion

	#region Strings

	public static List<int> ConvertString(string str, HuffmanNode[] nodes) { return StringsPak.ConvertString(str, nodes); }


	public static HuffmanNode GetNode(HuffmanNode[] nodes, char c) { return StringsPak.GetNode(nodes, c); }

	public static int SaveStringBlock(Stream fs, StringBlock block, int blockPos, HuffmanNode[] nodes) { return StringsPak.SaveStringBlock(fs, block, blockPos, nodes); }

	public static void SaveStringBlocks(Stream fs, List<StringBlock> blocks, HuffmanNode[] nodes, long tableStart) { StringsPak.SaveStringBlocks(fs, blocks, nodes, tableStart); }
	public static long SaveHuffmanNodes(Stream fs, HuffmanNode[] nodes) { return StringsPak.SaveHuffmanNodes(fs, nodes); }
	#endregion

	#region Conversations
	public static void SaveConversations(Stream fs, ConversationData convs)
	{
		fs.Position = 0;
		int total = 256 + 64;
		byte total_a = (byte)(total & 0xFF);
		byte total_b = (byte)((total & 0xFF00) >> 8);
		fs.WriteByte(total_a);
		fs.WriteByte(total_b);
		long offset = total * 4 + 2;
		for (int i = 0; i < convs.Conversations.Length; i++)
		{
			Conversation conv = convs.Conversations[i];
			fs.Position = 2 + i * 4;
			long writeOffset = (conv && conv.State != ConversationState.Unconverted) ? offset : 0;
			byte wo_a = (byte)(writeOffset & 0xFF);
			byte wo_b = (byte)((writeOffset & 0xFF00) >> 8);
			byte wo_c = (byte)((writeOffset & 0xFF0000) >> 16);
			byte wo_d = (byte)((writeOffset & 0xFF000000) >> 24);
			fs.WriteByte(wo_a);
			fs.WriteByte(wo_b);
			fs.WriteByte(wo_c);
			fs.WriteByte(wo_d);
			if (conv && conv.State != ConversationState.Unconverted)
			{
				offset = SaveConversation(fs, offset, conv);
			}
			
		}
	}
	public static long SaveConversation(Stream fs, long offset, Conversation conv)
	{
		fs.Position = offset;

		byte unk00_a = (byte)(conv.Unk00 & 0xFF);
		byte unk00_b = (byte)((conv.Unk00 & 0xFF00) >> 8);
		byte unk02_a = (byte)(conv.Unk02 & 0xFF);
		byte unk02_b = (byte)((conv.Unk02 & 0xFF00) >> 8);
		byte codeSize_a = (byte)(conv.CodeSize & 0xFF);
		byte codeSize_b = (byte)((conv.CodeSize & 0xFF00) >> 8);
		byte unk06_a = (byte)(conv.Unk06 & 0xFF);
		byte unk06_b = (byte)((conv.Unk06 & 0xFF00) >> 8);
		byte unk08_a = (byte)(conv.Unk08 & 0xFF);
		byte unk08_b = (byte)((conv.Unk08 & 0xFF00) >> 8);
		byte stringBlock_a = (byte)(conv.StringBlock & 0xFF);
		byte stringBlock_b = (byte)((conv.StringBlock & 0xFF00) >> 8);
		byte memorySlots_a = (byte)(conv.MemorySlots & 0xFF);
		byte memorySlots_b = (byte)((conv.MemorySlots & 0xFF00) >> 8);
		byte importedGlobs_a = (byte)(conv.ImportedFunctionsAndVariables & 0xFF);
		byte importedGlobs_b = (byte)((conv.ImportedFunctionsAndVariables & 0xFF00) >> 8);

		fs.WriteByte(unk00_a);
		fs.WriteByte(unk00_b);
		fs.WriteByte(unk02_a);
		fs.WriteByte(unk02_b);
		fs.WriteByte(codeSize_a);
		fs.WriteByte(codeSize_b);
		fs.WriteByte(unk06_a);
		fs.WriteByte(unk06_b);
		fs.WriteByte(unk08_a);
		fs.WriteByte(unk08_b);
		fs.WriteByte(stringBlock_a);
		fs.WriteByte(stringBlock_b);
		fs.WriteByte(memorySlots_a);
		fs.WriteByte(memorySlots_b);
		fs.WriteByte(importedGlobs_a);
		fs.WriteByte(importedGlobs_b);
		if(conv.Functions == null)
		{
			Debug.LogErrorFormat("Conversation functions empty, conv : {0}", conv.Slot);
		}
		for (int i = 0; i < conv.Functions.Length; i++)
		{
			ConversationFunction cf = conv.Functions[i];
			byte len_a = (byte)(cf.NameLength & 0xFF);
			byte len_b = (byte)((cf.NameLength & 0xFF00) >> 8);
			fs.WriteByte(len_a);
			fs.WriteByte(len_b);
			for (int j = 0; j < cf.NameLength; j++)
			{
				fs.WriteByte((byte)cf.Name[j]);
			}
			byte id_a = (byte)(cf.Id_Adress & 0xFF);
			byte id_b = (byte)((cf.Id_Adress & 0xFF00) >> 8);
			byte unk_a = (byte)(cf.Unk04 & 0xFF);
			byte unk_b = (byte)((cf.Unk04 & 0xFF00) >> 8);
			byte typ_a = (byte)(cf.Type & 0xFF);
			byte typ_b = (byte)((cf.Type & 0xFF00) >> 8);
			byte ret_a = (byte)(cf.Return & 0xFF);
			byte ret_b = (byte)((cf.Return & 0xFF00) >> 8);
			fs.WriteByte(id_a);
			fs.WriteByte(id_b);
			fs.WriteByte(unk_a);
			fs.WriteByte(unk_b);
			fs.WriteByte(typ_a);
			fs.WriteByte(typ_b);
			fs.WriteByte(ret_a);
			fs.WriteByte(ret_b);
		}
		for (int i = 0; i < conv.CodeSize; i++)
		{
			byte code_a = (byte)(conv.Code[i] & 0xFF);
			byte code_b = (byte)((conv.Code[i] & 0xFF00) >> 8);
			fs.WriteByte(code_a);
			fs.WriteByte(code_b);
		}
		return fs.Position;
	}

	public static void SaveBabglobs(Stream fs, ConversationData convs)
	{
		fs.Position = 0;
		for (int i = 0; i < convs.Conversations.Length; i++)
		{
			Conversation conv = convs.Conversations[i];
			if(conv && conv.State != ConversationState.Unconverted)
			{
				byte slot_a = (byte)(conv.Slot & 0xFF);
				byte slot_b = (byte)((conv.Slot & 0xFF00) >> 8);
				byte vars_a = (byte)(conv.MemorySlots & 0xFF);
				byte vars_b = (byte)((conv.MemorySlots & 0xFF00) >> 8);
				fs.WriteByte(slot_a);
				fs.WriteByte(slot_b);
				fs.WriteByte(vars_a);
				fs.WriteByte(vars_b);
			}
		}
	}

	#endregion

	#region Object data

	public static void SaveCommonData(ObjectData objDat)
	{
		GameWrite.Write(GameFiles.Resolve(FileExplorer.GetUpperPath(DataReader.FilePath) + "/COMOBJ.DAT"), ObjectDataCodec.BuildCommonData(objDat));
	}

	public static void SaveObjectData(ObjectData objDat)
	{
		GameWrite.Write(GameFiles.Resolve(FileExplorer.GetUpperPath(DataReader.FilePath) + "/OBJECTS.DAT"), ObjectDataCodec.BuildObjectData(objDat));
	}
	public static void SaveWeaponData(WeaponData wd, Stream fs) { ObjectDataCodec.SaveWeaponData(wd, fs); }
	public static void SaveProjectileData(ProjectileData pd, Stream fs) { ObjectDataCodec.SaveProjectileData(pd, fs); }
	public static void SaveRangedData(RangedData rd, Stream fs) { ObjectDataCodec.SaveRangedData(rd, fs); }
	public static void SaveArmourData(ArmourData ad, Stream fs) { ObjectDataCodec.SaveArmourData(ad, fs); }
	public static void SaveContainerData(ContainerData cd, Stream fs) { ObjectDataCodec.SaveContainerData(cd, fs); }
	public static void SaveLightData(LightData ld, Stream fs) { ObjectDataCodec.SaveLightData(ld, fs); }
	public static void SaveMonsterData(MonsterData md, Stream fs) { ObjectDataCodec.SaveMonsterData(md, fs); }
	public static void SaveMonsterSpriteInfo(ObjectData objDat)
	{
		var f = ObjectDataCodec.BuildMonsterSpriteFile(objDat, LevArk.Game.Kind, FileExplorer.GetUpperPath(FileExplorer.GetUpperPath(DataReader.FilePath)));
		if (f.HasValue) GameWrite.Write(f.Value.Key, f.Value.Value);
	}

	#endregion

	#region Debug
	//public static void SaveStaticObject(StaticObject so, string path)
	//{
	//	FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
	//	SaveStaticObject(so, fs);
	//	fs.Close();
	//}

	#endregion
}
