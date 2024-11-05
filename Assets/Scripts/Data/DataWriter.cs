using System;
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
		BackupData("LEV.ARK");
		FileStream fs = new FileStream(DataReader.FilePath, FileMode.Create);
		int blockCount = levels.Count * 15;
		byte b1 = (byte)(blockCount & 0xFF);
		byte b2 = (byte)((blockCount & 0xFF00) >> 8);
		fs.WriteByte(b1);
		fs.WriteByte(b2);

		long currentOffset = 542;
		for (int i = 0; i < levels.Count; i++)
		{
			fs.Position = 2 + i * 4;
			SaveLevelOffset(fs, currentOffset);

			LevelData level = levels[i];
			SaveTiles(level.MapTiles, i + 1, currentOffset, fs);
			SaveObjects(level.Objects, i + 1, currentOffset, fs);
			SaveFreeObjectIndexes(level.FreeObjects, i + 1, currentOffset, fs);
			SaveActiveMobs(level.MobList, i + 1, currentOffset, fs);
			SaveFreeListStart(i + 1, currentOffset, level.ActiveMobs, level.MobileListStart, level.StaticListStart, fs);
			fs.WriteByte((byte)'w');
			fs.WriteByte((byte)'u');
			currentOffset = fs.Position;
		}
		for (int i = 0; i < levels.Count; i++)
		{
			fs.Position = 2 + (i + levels.Count) * 4;
			SaveLevelOffset(fs, currentOffset);

			LevelData level = levels[i];
			SaveAnimationOverlays(currentOffset, level, fs);

			currentOffset = fs.Position;
		}
		for (int i = 0; i < levels.Count; i++)
		{
			fs.Position = 2 + (i + levels.Count * 2) * 4;
			SaveLevelOffset(fs, currentOffset);

			LevelData level = levels[i];
			SaveFloorTextureMappings(i + 1, currentOffset, fs, level.FloorTextures);
			SaveWallTextureMappings(i + 1, currentOffset, fs, level.WallTextures);
			SaveDoorTextureMappings(i + 1, currentOffset, fs, level.DoorTextures);

			currentOffset = fs.Position;
		}
		fs.Close();
	}
	private static void SaveLevelOffset(FileStream fs, long offset)
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
		SaveTextures("F32.TR", textures.Floors, textures.Palettes[0], 2);
		SaveTextures("F16.TR", textures.Floors, textures.Palettes[0], 2, 2);
		SaveTextures("W64.TR", textures.Walls, textures.Palettes[0], 2);
		SaveTextures("W16.TR", textures.Walls, textures.Palettes[0], 2, 4);
		SaveTextures("DOORS.GR", textures.Doors, textures.Palettes[0], 1);
		SaveTextures("TMFLAT.GR", textures.Levers, textures.Palettes[0], 1);
		SaveTextures("GENHEAD.GR", textures.GenHeads, textures.Palettes[0], 1);
		SaveTextures("CHARHEAD.GR", textures.NPCHeads, textures.Palettes[0], 1, 0, 56 - textures.NPCHeads.Count);
		SaveObjectGraphics(textures.Objects);
		SaveOtherTextures(textures.Other, textures.Palettes[0]);
		SaveTerrainData(textures);
	}

	public static void SaveStringData(StringData strings)
	{
		string file = "STRINGS.PAK";
		BackupData(file);
		if (File.Exists(file))
			File.Delete(file);
		FileStream fs = new FileStream(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file, FileMode.Create);
		strings.SortBlocks();
		SaveHuffmanNodes(fs, strings.Nodes);
		SaveStringBlocks(fs, strings.Blocks, strings.Nodes);
		fs.Close();
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
		string file = "CNV.ARK";
		BackupData(file);
		if (File.Exists(file))
			File.Delete(file);
		FileStream fs = new FileStream(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file, FileMode.Create);
		SaveConversations(fs, convs);
		fs.Close();
		file = "BABGLOBS.DAT";
		BackupData(file);
		if (File.Exists(file))
			File.Delete(file);
		fs = new FileStream(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file, FileMode.Create);
		SaveBabglobs(fs, convs);
		fs.Close();
	}

	public static void SaveObjectDatas(ObjectData objDat)
	{
		SaveCommonData(objDat);
		SaveObjectData(objDat);
		SaveMonsterSpriteInfo(objDat);
	}

	public static void BackupData(string toBackup)
	{
		string originalFile = FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + toBackup;
		string backupFile = originalFile + ".BKP";
		if (File.Exists(backupFile))
			return;
		File.Copy(originalFile, backupFile);
	}

	#endregion

	#region Level

	public static void SaveTiles(MapTile[,] tiles, int level, long offset, FileStream fs)
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

	public static void SaveObjects(StaticObject[] objects, int level, long offset, FileStream fs)
	{
		//int levelIndex = DataReader.GetLevelIndex(level, fs);
		for (int i = 2; i < 256 + 768; i++)
		{
			if(i < 256)
			{
				//fs.Position = levelIndex + 16384 + i * 27;
				fs.Position = offset + 16384 + i * 27;
				if (objects[i] == null)
					ClearMobileObject(i, fs);
				else
					SaveMobileObject((MobileObject)objects[i], fs);
			}
			else
			{
				//fs.Position = levelIndex + 23296 + (i - 256) * 8;
				fs.Position = offset + 23296 + (i - 256) * 8;
				if (objects[i] == null)
					ClearStaticObject(i, fs);
				else
					SaveStaticObject(objects[i], fs);
			}
		}
	}

	public static void ClearStaticObject(int index, FileStream fs)
	{
		for (int i = 0; i < 8; i++)
			fs.WriteByte(0);
	}

	public static void ClearMobileObject(int index, FileStream fs)
	{
		for (int i = 0; i < 27; i++)
			fs.WriteByte(0);
	}

	public static void SaveTile(MapTile tile, FileStream fs)
	{
		//FileStream fs = new FileStream(Application.dataPath + "/write.txt", FileMode.OpenOrCreate);
		//Debug.Log("Save tile");
		byte a = (byte)((int)tile.TileType + (tile.FloorHeight << 4));
		byte b = (byte)((tile.FloorTexture << 2) + ((tile.IsAntimagic == true ? 1 : 0) << 6) + ((tile.IsDoor == true ? 1 : 0) << 7));
		//byte c = (byte)(tile.WallTexture + ((tile.ObjectAdress & 0x0F) << 4));
		//byte d = (byte)((tile.ObjectAdress & 0xFF0) >> 4);
		byte c = (byte)(tile.WallTexture + ((tile.ObjectAdress & 0x03) << 6));
		byte d = (byte)((tile.ObjectAdress & 0xFFC) >> 2);
		//Debug.LogFormat("tile.ObjectAdress & 0x300 : {0}, tile.ObjectAdress & 0x300 >> 2 : {1}, tile.WallTexture : {2}, tile.ObjectAdress : {3}, tile.ObjectAdress & 0xFF0 : {4}", (tile.ObjectAdress & 0x300), (tile.ObjectAdress & 0x300) >> 2, tile.WallTexture, tile.ObjectAdress, tile.ObjectAdress & 0xFF);
		fs.WriteByte(a);
		fs.WriteByte(b);
		fs.WriteByte(c);
		fs.WriteByte(d);

		//fs.Close();
	}

	private static void SaveStaticObject(StaticObject so, FileStream fs)
	{
		byte a = (byte)(so.ObjectID & 0xFF);
		byte b = (byte)(((so.ObjectID & 0x100) >> 8) + (so.Flags << 1) + ((so.IsEnchanted == true ? 1 : 0) << 4) + ((so.IsDoorOpen == true ? 1 : 0) << 5) + ((so.IsInvisible == true ? 1 : 0) << 6) + ((so.IsQuantity == true ? 1 : 0) << 7));
		byte c = (byte)(so.ZPos + ((so.Direction & 0x01) << 7));
		byte d = (byte)(((so.Direction & 0x06) >> 1) + (so.YPos << 2) + (so.XPos << 5));
		//byte e = (byte)(so.Quality + ((so.NextAdress & 0x0F) << 4));
		//byte f = (byte)((so.NextAdress & 0xFF0) >> 4);
		byte e = (byte)(so.Quality + ((so.NextAdress & 0x03) << 6));
		byte f = (byte)((so.NextAdress & 0xFFC) >> 2);
		//byte g = (byte)(so.Owner_Special + ((so.Quantity_Link_Special & 0x0F) << 4));
		//byte h = (byte)((so.Quantity_Link_Special & 0xFF0) >> 4);
		byte g = (byte)(so.Owner + ((so.Special & 0x03) << 6));
		byte h = (byte)((so.Special & 0xFFC) >> 2);

		//Debug.LogFormat("ZPos : {0}, Direction : {1}, Direction & 0x01 : {2}, Direction & 0x01 << 7 : {3}", so.ZPos, so.Direction, so.Direction & 0x01, (so.Direction & 0x01) << 7);
		fs.WriteByte(a);
		fs.WriteByte(b);
		fs.WriteByte(c);
		fs.WriteByte(d);
		fs.WriteByte(e);
		fs.WriteByte(f);
		fs.WriteByte(g);
		fs.WriteByte(h);
	}

	public static void SaveMobileObject(MobileObject mo, FileStream fs)
	{
		//FileStream fs = new FileStream(Application.dataPath + "/write.txt", FileMode.OpenOrCreate);
		SaveStaticObject(mo, fs);
		byte b8 = (byte)mo.HP;
		byte b9 = (byte)mo.B9;
		byte bA = (byte)mo.BA;
		byte bB = (byte)(mo.Goal + ((mo.GTarg & 0x0F) << 4));
		byte bC = (byte)(((mo.GTarg & 0xF0) >> 4) + (mo.BC & 0xF0));
		byte bD = (byte)mo.BD;
		byte bE = (byte)((mo.Attitude << 6) + (mo.BE & 0x03F));
		byte bF = (byte)mo.BF;
		byte b10 = (byte)mo.B10;
		byte b11 = (byte)mo.B11;
		byte b12 = (byte)mo.B12;
		byte b13 = (byte)mo.B13;
		byte b14 = (byte)mo.B14;
		byte b15 = (byte)mo.B15;
		byte b16 = (byte)((mo.B16 & 0x0F) + ((mo.YHome & 0x0F) << 4));
		byte b17 = (byte)(((mo.YHome & 0x30) >> 4) + (mo.XHome << 2));
		byte b18 = (byte)mo.B18;
		byte b19 = (byte)mo.B19;
		byte b1A = (byte)mo.Whoami;

		fs.WriteByte(b8);
		fs.WriteByte(b9);
		fs.WriteByte(bA);
		fs.WriteByte(bB);
		fs.WriteByte(bC);
		fs.WriteByte(bD);
		fs.WriteByte(bE);
		fs.WriteByte(bF);
		fs.WriteByte(b10);
		fs.WriteByte(b11);
		fs.WriteByte(b12);
		fs.WriteByte(b13);
		fs.WriteByte(b14);
		fs.WriteByte(b15);
		fs.WriteByte(b16);
		fs.WriteByte(b17);
		fs.WriteByte(b18);
		fs.WriteByte(b19);
		fs.WriteByte(b1A);

		//fs.Close();
	}


	public static void SaveFreeObjectIndexes(int[] freeList, int level, long offset, FileStream fs)
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

	public static void SaveActiveMobs(int[] mobList, int level, long offset, FileStream fs)
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

	public static void SaveFreeListStart(int level, long offset, int moba, int mob, int stat, FileStream fs)
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

	public static void SaveFloorTextureMappings(int level, long offset, FileStream fs, int[] floormap)
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

	public static void SaveWallTextureMappings(int level, long offset, FileStream fs, int[] wallmap)
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

	public static void SaveDoorTextureMappings(int level, long offset, FileStream fs, int[] doormap)
	{
		//int index = 289766 + (level - 1) * 122 + 58 * 2;
		long index = offset + 58 * 2;
		fs.Position = index;
		for (int i = 0; i < 6; i++)
		{
			fs.WriteByte((byte)doormap[i]);
		}
	}

	public static void SaveAnimationOverlays(long offset, LevelData levelData, FileStream fs)
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

	public static void SaveAnimationOverlay(AnimationOverlay anim, FileStream fs)
	{
		byte adr_a = (byte)(anim.Adress & 0xFF);
		byte adr_b = (byte)((anim.Adress & 0xFF00) >> 8);
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

	public static int[] SaveTextureOffsets(FileStream fs, string fileName, TextureContainer texCon, int scaleFactor = 0, int fakeTextures = 0)
	{
		if (texCon.Offset == -1)
			return null;

		//string path = FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + fileName;
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
			int offset = firstOffset + (scaleFactor > 0 ? texCon.Offset / (scaleFactor * 2) : texCon.Offset) * index;
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
		BackupData(file);
		FileStream fs = new FileStream(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file, FileMode.Create);
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
			SaveTexture(fs, file, floorOffsets[i], raw, textures.ATI);
		}
		fs.Close();
	}

	public static void SaveOtherTextures(TextureContainer other, Color[] palette)
	{
		string file = "TMOBJ.GR";
		BackupData(file);
		int oCount = 0, oFirst = 0;

		//FIXME : this does not save offsets ! maybe it's not necessary?
		int[] oOffsets = DataReader.ReadGraphicOffsets(file, ref oCount, ref oFirst);
		FileStream fs = new FileStream(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file, FileMode.Open);
		for (int i = 0; i < other.Count; i++)
		{
			int[] raw = ConvertToRawTexture(other.Textures[i], palette, false);
			int width = other.Textures[i].width;
			int height = other.Textures[i].height;
			int ratio = 1;
			if (i == 28 || i == 29) //graves
				ratio = 2;
			AdditionalTextureInfo ati = new AdditionalTextureInfo(true, width, height, 0, ratio);
			SaveTexture(fs, file, oOffsets[i], raw, ati);
		}
		fs.Close();
	}

	public static void SaveTexture(FileStream fs, string fileName, int offset, int[] rawTex, AdditionalTextureInfo ati)
	{
		if (rawTex == null)
		{
			Debug.LogWarningFormat("Trying to save null texture, type : {0}, offset : {1}", fileName, offset);
			return;
		}
		//string path = FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + fileName;
		//FileStream fs = new FileStream(path, FileMode.Open);
		fs.Position = offset;
		if(ati.Write)
		{
			fs.WriteByte(4);
			fs.WriteByte((byte)ati.Width);
			fs.WriteByte((byte)ati.Height);
			fs.WriteByte((byte)ati.SizeA);
			fs.WriteByte((byte)ati.SizeB);
		}		
		for (int i = 0; i < rawTex.Length; i++)
			fs.WriteByte((byte)rawTex[i]);
		//fs.Close();
	}

	public static void SaveObjectGraphics(TextureContainer objects)
	{
		string file = "OBJECTS.GR";
		BackupData(file);

		FileStream fs = new FileStream(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file, FileMode.Create);
		fs.WriteByte(1);
		int count = 464;
		byte count_a = (byte)(count & 0xFF);
		byte count_b = (byte)((count & 0xFF00) >> 8);
		fs.WriteByte(count_a);
		fs.WriteByte(count_b);
		long curOffset = (count+1) * 4 + 3;
		Dictionary<int, int> same = new Dictionary<int, int>();
		//for (int i = 320; i < objects.Count; i++)
		//{
		//	int[] A = objects.RawTextures[i];
		//	for (int j = i + 1; j < objects.Count; j++)
		//	{
		//		int[] B = objects.RawTextures[j];
		//		if (compareRaw(A, B))
		//			same[j] = i;
		//	}
		//}
		Dictionary<int, long> offsets = new Dictionary<int, long>();
		for (int i = 0; i < objects.Count; i++)
		{
			//Debug.LogFormat("Saving object graphic {0}", i);
			offsets[i] = curOffset;
			fs.Position = i * 4 + 3;
			if(same.ContainsKey(i))
			{
				long sameOffset = offsets[i];
				byte a2 = (byte)(sameOffset & 0xFF);
				byte b2 = (byte)((sameOffset & 0xFF00) >> 8);
				byte c2 = (byte)((sameOffset & 0xFF0000) >> 16);
				byte d2 = (byte)((sameOffset & 0xFF000000) >> 24);

				fs.WriteByte(a2);
				fs.WriteByte(b2);
				fs.WriteByte(c2);
				fs.WriteByte(d2);
				continue;
			}
			byte a = (byte)(curOffset & 0xFF);
			byte b = (byte)((curOffset & 0xFF00) >> 8);
			byte c = (byte)((curOffset & 0xFF0000) >> 16);
			byte d = (byte)((curOffset & 0xFF000000) >> 24);

			fs.WriteByte(a);
			fs.WriteByte(b);
			fs.WriteByte(c);
			fs.WriteByte(d);

			fs.Position = curOffset;
			List<int> nibbles = CompressToRLE(objects.RawTextures[i]);
			fs.WriteByte(8);
			fs.WriteByte((byte)objects.Textures[i].width);
			fs.WriteByte((byte)objects.Textures[i].height);
			fs.WriteByte((byte)objects.AuxPalettes[i]);
			byte nib_a = (byte)(nibbles.Count & 0xFF);
			byte nib_b = (byte)((nibbles.Count & 0xFF00) >> 8);
			fs.WriteByte(nib_a);
			fs.WriteByte(nib_b);
			SaveNibbles(fs, fs.Position, nibbles);
			curOffset = fs.Position;
		}
		for (int i = objects.Count; i < objects.Count + 4; i++)
		{
			fs.Position = i * 4 + 3;
			byte a = (byte)(curOffset & 0xFF);
			byte b = (byte)((curOffset & 0xFF00) >> 8);
			byte c = (byte)((curOffset & 0xFF0000) >> 16);
			byte d = (byte)((curOffset & 0xFF000000) >> 24);

			fs.WriteByte(a);
			fs.WriteByte(b);
			fs.WriteByte(c);
			fs.WriteByte(d);
		}
		fs.Close();
	}

	public static void SaveNibbles(FileStream fs, long offset, List<int> nibbles)
	{
		fs.Position = offset;
		for (int i = 0; i < nibbles.Count; i+=2)
		{
			int nib_a = nibbles[i] << 4;
			int nib_b = (i + 1) == nibbles.Count ? 0 : nibbles[i + 1];
			byte b = (byte)(nib_a + nib_b);
			fs.WriteByte(b);
		}
	}
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

	public static void SavePaletteMapping(int[] mapping)
	{
		string path = Application.streamingAssetsPath + "/mapping.dat";
		if(File.Exists(path))
		{
			//Check size
		}
		else
		{
			FileStream fs = new FileStream(path, FileMode.CreateNew);
			for (int m = 0; m < mapping.Length; m++)
				fs.WriteByte((byte)mapping[m]);
			fs.Close();
		}
	}

	/// <summary>
	/// This function is really only used once to generate mapping. It takes over 10 minutes to run!
	/// </summary>
	public static int[] MapPalette(Color[] palette)
	{
		int[] mapping = new int[256 * 256 * 256];

		int m = 0;
		Color32 toCompare;
		Color32 toCompareTo;
		Color32 currentClosest;
		int closestIndex;
		for (int r = 0; r < 256; r++)
		{
			for (int g = 0; g < 256; g++)
			{
				for (int b = 0; b < 256; b++)
				{
					toCompareTo = new Color32((byte)r, (byte)g, (byte)b, 255);
					currentClosest = palette[0];
					closestIndex = 0;
					for (int i = 0; i < 256; i++)
					{
						toCompare = palette[i];
						if(IsCloserToColorThan32(toCompare, toCompareTo, currentClosest))
						{
							currentClosest = palette[i];
							closestIndex = i;
						}
					}
					mapping[m] = closestIndex;
					m++;
				}
			}
		}
		return mapping;
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
	public static int[] ConvertToRawTexture(Texture2D tex, Color[] palette, bool reverse)
	{
		string path = Application.streamingAssetsPath + "/mapping.dat";
		if (!File.Exists(path))
			return null;
		FileStream fs = new FileStream(path, FileMode.Open);
		int size = tex.width * tex.height;
		int[] rawTex = new int[size];
		int index = 0;

		for (int y = 0; y < tex.height; y++)
		{
			for (int x = 0; x < tex.width; x++)
			{
				if (!reverse)
					index = size - (x + y * tex.width) - 1;
				else
					index = x + y * tex.height;
				Color32 col = tex.GetPixel(tex.width - x - 1, y);
				if (col.a == 0)
					rawTex[index] = 0;
				else
					rawTex[index] = DataReader.ReadPaletteMapping(fs, col.r, col.g, col.b);
			}
		}
		fs.Close();
		return rawTex;
	}
	/// <summary>
	/// DOES NOT WORK
	/// </summary>
	public static int[] ConvertToRawTextureFromAux(Texture2D tex, Color[] auxPalette, bool reverse)
	{
		int size = tex.width * tex.height;
		int[] rawTex = new int[size];
		int index = 0;

		for (int y = 0; y < tex.height; y++)
		{
			for (int x = 0; x < tex.width; x++)
			{
				if (!reverse)
					index = size - (x + y * tex.width) - 1;
				else
					index = x + y * tex.height;	//ULTRA BUG
				Color col = tex.GetPixel(tex.width - x - 1, y);
				if (col.a == 0)
					rawTex[index] = 0;
				else
				{
					int auxIndex = 0;
					for (int i = 0; i < auxPalette.Length; i++)
					{
						if (auxPalette[i] == col)
						{
							auxIndex = i;
							break;
						}
						else
							Debug.LogWarningFormat("Failed to find color {0} in aux palette", col);
					}
					rawTex[index] = auxIndex;
				}
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
	public static List<int> CompressToRLE(int[] rawTex)
	{
		List<int> nibbles = new List<int>();
		int ptr = 0;
		int cur = 0;
		int state = 0;
		List<Tuple<int, int>> counts = getCounts(rawTex, ref ptr);
		int safe = 1000;
		while(cur < counts.Count)
		{
			//Debug.LogFormat("CUR : {0}", cur);
			if(state == 0)
			{
				//Debug.Log("State : 0");
				int repeats = getRepeats(counts, cur);
				//Debug.LogFormat("repeats : {0}", repeats);
				if (repeats == 0)
				{
					//Debug.LogError("Repeats == 0");
					nibbles.Add(1);
				}
				else if (repeats == 1)
				{
					int count = counts[cur].Item1;
					int val = counts[cur].Item2;
					//Debug.LogFormat("<b>One repeat, writing count {0} and val {1}</b>", count, val);
					nibbles.AddRange(getCount(count));
					nibbles.Add(val);
				}
				else if (repeats > 1)
				{
					nibbles.Add(2);
					nibbles.AddRange(getCount(repeats));
					//Debug.LogFormat("<b>{0} repeats, writing 2 and {0}</b>", repeats);
					for (int i = cur; i < cur+repeats; i++)
					{
						int count = counts[i].Item1;
						int val = counts[i].Item2;
						//Debug.LogFormat("Repeat, writing count {0} and val {1}", count, val);
						nibbles.AddRange(getCount(count));
						nibbles.Add(val);
					}
				}
				cur += repeats;
				state = 1;
			}
			else if (state == 1)
			{
				//Debug.Log("State : 1");
				int small = getSmall(counts, cur);
				//Debug.LogFormat("small : {0}", small);
				int sum = 0;
				for (int i = cur; i < cur+small; i++)
				{
					sum += counts[i].Item1;
				}
				nibbles.AddRange(getCount(sum));
				//Debug.LogFormat("<b>Writing count {0}</b>", sum);
				for (int i = cur; i < cur + small; i++)
				{
					for (int j = 0; j < counts[i].Item1; j++)
					{
						nibbles.Add(counts[i].Item2);
						//Debug.LogFormat("Writing val {0}", counts[i].Item2);
					}
				}
				cur += small;
				state = 0;
			}
			safe--;
			if(safe==0)
			{
				Debug.LogError("Fail");
				break;
			}
		}

		//if(nibbles.Count % 2 == 0)
		if(state == 1)
		{
			nibbles[nibbles.Count - 2]++;
			nibbles.Add(1);
			nibbles.Add(0);
		}
		return nibbles;
	}
	private static int getRepeats(List<Tuple<int, int>> counts, int ptr)
	{
		int repeats = 0;
		for (int i = ptr; i < counts.Count; i++)
		{
			if (counts[i].Item1 > 2)
				repeats++;
			else
				return repeats;
		}
		return repeats;
	}
	private static int getSmall(List<Tuple<int, int>> counts, int ptr)
	{
		int small = 0;
		for (int i = ptr; i < counts.Count; i++)
		{
			if (counts[i].Item1 < 3)
				small++;
			else
				return small;
		}
		return small;
	}
	private static List<int> getCount(int count)
	{
		List<int> counts = new List<int>();

		if (count > 255)
		{
			counts.Add(0);
			counts.Add(0);
			counts.Add(0);
			int r1 = count / 256;
			count -= (r1 * 256);
			int r2 = count / 16;
			count -= (r2 * 16);
			counts.Add(r1);
			counts.Add(r2);
			counts.Add(count);
		}
		else if (count > 15)
		{
			counts.Add(0);
			int rest = count / 16;
			counts.Add(rest);
			count -= (16 * rest);
			counts.Add(count);
		}
		else
			counts.Add(count);
	
		return counts;
	}
	public static List<Tuple<int, int>> getCounts(int[] rawTex, ref int ptr)
	{
		List<Tuple<int, int>> counts = new List<Tuple<int, int>>();

		int count = 1;
		int cur = rawTex[ptr];
		while(ptr < rawTex.Length - 1)
		{
			ptr++;
			int next = rawTex[ptr];
			if(cur == next)
			{
				count++;
			}
			else
			{
				counts.Add(new Tuple<int, int>(count, cur));
				count = 1;
				cur = next;
			}
		}
		counts.Add(new Tuple<int, int>(count, cur));
		return counts;
	}
	
	private static List<int> getValsFromTo(int[] rawTex, int start, int end)
	{
		List<int> vals = new List<int>();
		for (int i = start; i < end; i++)
			vals.Add(rawTex[i]);
		return vals;
	}

	public static void ExportDefaultTextures()
	{
		ExportDefaultTextures("Export/Floors", DataReader.FloorTextures, DataReader.FloorTextures.Count);
		ExportDefaultTextures("Export/Walls", DataReader.WallTextures, DataReader.WallTextures.Count);
		ExportDefaultTextures("Export/Doors", DataReader.DoorTextures, DataReader.DoorTextures.Count);
		ExportDefaultTextures("Export/Levers", DataReader.LeverTextures, DataReader.LeverTextures.Count);
		ExportDefaultTextures("Export/Other", DataReader.LeverTextures, DataReader.LeverTextures.Count);
		ExportDefaultTextures("Export/Portraits", DataReader.Portraits, DataReader.Portraits.Count);
		ExportDefaultTextures("Export/Objects", DataReader.ObjectGraphics, DataReader.ObjectGraphics.Count);
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
		BackupData(file);
		FileStream fs = new FileStream(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file, FileMode.Create);

		for (int i = 0; i < 256; i++)
		{
			byte a = (byte)(texData.WallTypes[i] & 0xFF);
			byte b = (byte)((texData.WallTypes[i] & 0xFF00) >> 8);
			fs.WriteByte(a);
			fs.WriteByte(b);
		}
		for (int i = 0; i < 256; i++)
		{
			byte a = (byte)(texData.FloorTypes[i] & 0xFF);
			byte b = (byte)((texData.FloorTypes[i] & 0xFF00) >> 8);
			fs.WriteByte(a);
			fs.WriteByte(b);
		}
		fs.Close();
	}

	#endregion

	#region Strings

	public static List<int> ConvertString(string str, HuffmanNode[] nodes)
	{
		List<bool> curBits = new List<bool>();
		List<bool> bits = new List<bool>();

		for (int i = 0; i < str.Length + 1; i++)
		{
			char c;
			if(i == str.Length)
				c = '|';
			else
				c = str[i];
			HuffmanNode node = GetNode(nodes, c);
			if(node == null)
			{
				Debug.LogErrorFormat("Couldn't get node for char {0}, string \n{1}\ni : {2}", c, str, i);
				return null;
			}
			int current = node.Current;
			int nextParent = node.Parent;

			do
			{
				node = nodes[nextParent];
				nextParent = node.Parent;

				if (current == node.Right)
					curBits.Add(true);
				else
					curBits.Add(false);

				current = node.Current;
			}
			while (nextParent != 255);
			for (int j = curBits.Count - 1; j > -1; j--)
				bits.Add(curBits[j]);
			curBits = new List<bool>();
		}
		return ConvertString(nodes, bits);
	}

	private static List<int> ConvertString(HuffmanNode[] nodes, List<bool> bits)
	{
		int bit = 0;
		int cur = 0;
		List<int> convString = new List<int>();
		for (int i = 0; i < bits.Count; i++)
		{
			if (bit == 0)
			{
				if (i > 0)
					convString.Add(cur);
				bit = 8;
				cur = 0;
			}
			if (bits[i])
				cur |= 1 << (bit - 1);
			bit--;
		}
		convString.Add(cur);
		return convString;
	}

	public static HuffmanNode GetNode(HuffmanNode[] nodes, char c)
	{
		int i = HuffmanNode.GetNumber(c);
		if (i == -1)
			return null;

		return nodes[i];
	}

	/// <summary>
	/// Returns string block size
	/// </summary>
	public static int SaveStringBlock(FileStream fs, StringBlock block, int blockPos, HuffmanNode[] nodes)
	{
		fs.Position = blockPos;
		//First, write string count (int16)
		byte count_a = (byte)(block.StringCount & 0xFF);
		byte count_b = (byte)((block.StringCount & 0xFF00) >> 8);

		fs.WriteByte(count_a);
		fs.WriteByte(count_b);

		int currentOffset = 0;
		int firstStringOffset = 2 + (2 * block.StringCount);
		for (int i = 0; i < block.StringCount; i++)
		{
			//Debug.LogFormat("Saving string : {0}", block.Strings[i]);
			fs.Position = blockPos + 2 + i * 2;
			byte offset_a = (byte)(currentOffset & 0xFF);
			byte offset_b = (byte)((currentOffset & 0xFF00) >> 8);
			fs.WriteByte(offset_a);
			fs.WriteByte(offset_b);
			fs.Position = blockPos + firstStringOffset + currentOffset;
			List<int> rawString = ConvertString(block.Strings[i], nodes);
			if (rawString == null)
				break;
			for (int j = 0; j < rawString.Count; j++)
				fs.WriteByte((byte)rawString[j]);
			currentOffset += rawString.Count;
		}
		int offset = firstStringOffset + currentOffset;
		//Debug.LogFormat("Block {0} offset {1}", block.BlockNumber, offset);
		//if ((offset % 2) == 1)
		//{
		//	fs.WriteByte(0);
		//	offset++;
		//}
		return offset;
	}

	public static void SaveStringBlocks(FileStream fs, List<StringBlock> blocks, HuffmanNode[] nodes)
	{
		int start = 726;
		fs.Position = start;

		//Write block count - int16
		byte count_a = (byte)(blocks.Count & 0xFF);
		byte count_b = (byte)((blocks.Count & 0xFF00) >> 8);

		fs.WriteByte(count_a);
		fs.WriteByte(count_b);

		//Next, get the length of the block "table"
		int firstBlock = 2 + (6 * blocks.Count);
		int currentOffset = start + firstBlock;
		for (int i = 0; i < blocks.Count; i++)
		{
			//Debug.LogFormat("Saving string block {0}, cur offset : {1}", blocks[i].BlockNumber, currentOffset);
			fs.Position = start + 2 + i * 6;
			byte strings_a = (byte)(blocks[i].BlockNumber & 0xFF);
			byte strings_b = (byte)((blocks[i].BlockNumber & 0xFF00) >> 8);
			byte offset_a = (byte)(currentOffset & 0xFF);
			byte offset_b = (byte)((currentOffset & 0xFF00) >> 8);
			byte offset_c = (byte)((currentOffset & 0xFF0000) >> 16);
			byte offset_d = (byte)((currentOffset & 0xFF000000) >> 24);

			fs.WriteByte(strings_a); fs.WriteByte(strings_b);
			fs.WriteByte(offset_a); fs.WriteByte(offset_b); fs.WriteByte(offset_c); fs.WriteByte(offset_d);

			int blockOffset = SaveStringBlock(fs, blocks[i], currentOffset, nodes);
			currentOffset += blockOffset;
			
		}
	}
	public static long SaveHuffmanNodes(FileStream fs, HuffmanNode[] nodes)
	{
		fs.Position = 0;
		byte len_a = (byte)(nodes.Length & 0xFF);
		byte len_b = (byte)((nodes.Length & 0xFF00) >> 8);
		fs.WriteByte(len_a);
		fs.WriteByte(len_b);
		foreach (var node in nodes)
		{
			fs.WriteByte((byte)node.Char);
			fs.WriteByte((byte)node.Parent);
			fs.WriteByte((byte)node.Left);
			fs.WriteByte((byte)node.Right);
		}
		return fs.Position;
	}
	#endregion

	#region Conversations
	public static void SaveConversations(FileStream fs, ConversationData convs)
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
	public static long SaveConversation(FileStream fs, long offset, Conversation conv)
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

	public static void SaveBabglobs(FileStream fs, ConversationData convs)
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

	public static void SaveDefaultConversationFunctions(ConversationFunction[] funcs)
	{
		string fileName = Application.dataPath + "/Conversations/functions.dat";
		if (File.Exists(fileName))
			File.Delete(fileName);
		FileStream fs = new FileStream(fileName, FileMode.Create);
		BinaryFormatter bf = new BinaryFormatter();
		bf.Serialize(fs, funcs);
		fs.Close();
	}


	#endregion

	#region Object data

	public static void SaveCommonData(ObjectData objDat)
	{
		string file = "COMOBJ.DAT";
		BackupData(file);
		FileStream fs = new FileStream(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file, FileMode.Create);
		byte start_b_a = (byte)(objDat.CommonStart & 0xFF);
		byte start_b_b = (byte)((objDat.CommonStart & 0xFF00) >> 8);
		fs.WriteByte(start_b_a);
		fs.WriteByte(start_b_b);
		for (int i = 0; i < 512; i++)
		{
			CommonData com = objDat.CommonData[i];
			fs.WriteByte((byte)com.Height);
			byte b2_a = (byte)(com.Radius + (com.Type << 3) + (com.Mass << 4) & 0xFF);
			byte b2_b = (byte)((com.Mass >> 4) & 0xFF);
			fs.WriteByte(b2_a);
			fs.WriteByte(b2_b);
			byte b3 = (byte)(com.Flag0 + (com.Flag1 << 1) + (com.Flag2 << 2) + (com.Flag3 << 3) + (com.Flag4 << 4) + (com.Pickable << 5) + (com.Flag6 << 6) + (com.Container << 7));
			fs.WriteByte(b3);
			byte b4_a = (byte)(com.Value & 0xFF);
			byte b4_b = (byte)((com.Value & 0xFF00) >> 8);
			fs.WriteByte(b4_a);
			fs.WriteByte(b4_b);
			byte b6 = (byte)((com.RawData[6] & 0xF3) + (com.QualityClass << 2));
			fs.WriteByte(b6);
			byte b7 = (byte)((com.RawData[7] & 0x01) + (com.ObjectType << 1) + (com.Ownable << 7) + (com.UnkFlag1 << 6) + (com.PickupFlag << 5));
			fs.WriteByte(b7);
			fs.WriteByte((byte)com.RawData[8]);
			fs.WriteByte((byte)com.RawData[9]);
			byte bA = (byte)((com.RawData[10] & 0xE0) + com.QualityType + (com.LookDescription << 4));
			fs.WriteByte(bA);
		}
		fs.Close();
	}

	public static void SaveObjectData(ObjectData objDat)
	{
		string file = "OBJECTS.DAT";
		BackupData(file);
		FileStream fs = new FileStream(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + file, FileMode.Create);

		byte start_b_a = (byte)(objDat.StartValue & 0xFF);
		byte start_b_b = (byte)((objDat.StartValue & 0xFF00) >> 8);
		fs.WriteByte(start_b_a);
		fs.WriteByte(start_b_b);

		for (int i = 0; i < 16; i++)		
			SaveWeaponData(objDat.WeaponData[i], fs);
		for (int i = 0; i < 8; i++)		
			SaveProjectileData(objDat.ProjectileData[i], fs);		
		for (int i = 0; i < 8; i++)		
			SaveRangedData(objDat.RangedData[i], fs);
		for (int i = 0; i < 32; i++)		
			SaveArmourData(objDat.ArmourData[i], fs);
		for (int i = 0; i < 64; i++)		
			SaveMonsterData(objDat.MonsterData[i], fs);
		for (int i = 0; i < 16; i++)		
			SaveContainerData(objDat.ContainerData[i], fs);
		for (int i = 0; i < 8; i++)		
			SaveLightData(objDat.LightData[i], fs);
		for (int i = 0; i < 112; i++)		
			fs.WriteByte((byte)objDat.UnknownData[i]);		
		fs.Close();
	}
	public static void SaveWeaponData(WeaponData wd, FileStream fs)
	{
		fs.WriteByte((byte)wd.Slash);
		fs.WriteByte((byte)wd.Bash);
		fs.WriteByte((byte)wd.Stab);
		fs.WriteByte((byte)wd.Unk1);
		fs.WriteByte((byte)wd.Unk2);
		fs.WriteByte((byte)wd.Unk3);
		fs.WriteByte((byte)wd.Skill);
		fs.WriteByte((byte)wd.Durability);
	}
	public static void SaveProjectileData(ProjectileData pd, FileStream fs)
	{
		byte b2 = (byte)((pd.Damage << 1) + ((pd.Unk1 & 0x100) >> 8));
		byte b1 = (byte)(pd.Unk1 & 0xFF);
		fs.WriteByte(b1);
		fs.WriteByte(b2);
		fs.WriteByte((byte)pd.Unk2);
	}
	public static void SaveRangedData(RangedData rd, FileStream fs)
	{
		byte b1 = (byte)(rd.Ammo & 0xFF);
		byte b2 = (byte)((rd.Ammo & 0xFF00) >> 8);
		fs.WriteByte(b1);
		fs.WriteByte(b2);
		fs.WriteByte((byte)rd.Unk1);
	}
	public static void SaveArmourData(ArmourData ad, FileStream fs)
	{
		fs.WriteByte((byte)ad.Protection);
		fs.WriteByte((byte)ad.Durability);
		fs.WriteByte((byte)ad.Unk1);
		fs.WriteByte((byte)ad.Type);
	}
	public static void SaveContainerData(ContainerData cd, FileStream fs)
	{
		fs.WriteByte((byte)cd.Capacity);
		fs.WriteByte((byte)cd.Type);
		fs.WriteByte((byte)cd.Slots);
	}
	public static void SaveLightData(LightData ld, FileStream fs)
	{
		fs.WriteByte((byte)ld.Duration);
		fs.WriteByte((byte)ld.Brightness);
	}
	public static void SaveMonsterData(MonsterData md, FileStream fs)
	{
		fs.WriteByte((byte)md.Level);
		fs.WriteByte((byte)md.Unk0_1);
		fs.WriteByte((byte)md.Unk0_2);
		fs.WriteByte((byte)md.Unk0_3);
		byte health_a = (byte)(md.Health & 0xFF);
		byte health_b = (byte)((md.Health & 0xFF00) >> 8);
		fs.WriteByte(health_a);
		fs.WriteByte(health_b);
		fs.WriteByte((byte)md.Attack);
		fs.WriteByte((byte)md.Unk1);
		fs.WriteByte((byte)(md.HitDecal + (md.Remains << 4)));
		fs.WriteByte((byte)md.OwnerType);
		fs.WriteByte((byte)md.Passiveness);
		fs.WriteByte((byte)md.Unk2);
		fs.WriteByte((byte)md.Speed);
		byte unk3_a = (byte)(md.Unk3 & 0xFF);
		byte unk3_b = (byte)((md.Unk3 & 0xFF00) >> 8);
		fs.WriteByte(unk3_a);
		fs.WriteByte(unk3_b);
		fs.WriteByte((byte)md.Poison);
		fs.WriteByte((byte)md.MonsterType);
		fs.WriteByte((byte)md.EquipmentDamage);
		fs.WriteByte((byte)md.Unk4);
		fs.WriteByte((byte)md.Attack1Value);
		fs.WriteByte((byte)md.Attack1Damage);
		fs.WriteByte((byte)md.Attack1Chance);
		fs.WriteByte((byte)md.Attack2Value);
		fs.WriteByte((byte)md.Attack2Damage);
		fs.WriteByte((byte)md.Attack2Chance);
		fs.WriteByte((byte)md.Attack3Value);
		fs.WriteByte((byte)md.Attack3Damage);
		fs.WriteByte((byte)md.Attack3Chance);
		byte inv_unk1_a = (byte)(md.Inv_Unk1 & 0xFF);
		byte inv_unk1_b = (byte)((md.Inv_Unk1 & 0xFF00) >> 8);
		byte inv_unk1_c = (byte)((md.Inv_Unk1 & 0xFF0000) >> 16);
		byte inv_unk1_d = (byte)((md.Inv_Unk1 & 0xFF000000) >> 24);
		fs.WriteByte(inv_unk1_a);
		fs.WriteByte(inv_unk1_b);
		fs.WriteByte(inv_unk1_c);
		fs.WriteByte(inv_unk1_d);
		if (md.InventoryInfo[0] > 0)
			fs.WriteByte((byte)((md.Inventory[0] << 1) + 1));		
		else
			fs.WriteByte(0);
		if (md.InventoryInfo[1] > 0)
			fs.WriteByte((byte)((md.Inventory[1] << 1) + 1));
		else
			fs.WriteByte(0);
		if(md.Inventory[2] > 0)
		{			
			byte i2_a = (byte)(((md.Inventory[2] & 0x0F) << 4) + md.InventoryInfo[2]);
			byte i2_b = (byte)(md.Inventory[2] >> 4);
			fs.WriteByte(i2_a);
			fs.WriteByte(i2_b);
		}
		else
		{
			fs.WriteByte(0);
			fs.WriteByte(0);
		}
		if (md.Inventory[3] > 0)
		{
			byte i2_a = (byte)(((md.Inventory[3] & 0x0F) << 4) + md.InventoryInfo[3]);
			byte i2_b = (byte)(md.Inventory[3] >> 4);
			fs.WriteByte(i2_a);
			fs.WriteByte(i2_b);
		}
		else
		{
			fs.WriteByte(0);
			fs.WriteByte(0);
		}
		byte inv_unk2_a = (byte)(md.Inv_Unk2 & 0xFF);
		byte inv_unk2_b = (byte)((md.Inv_Unk2 & 0xFF00) >> 8);
		fs.WriteByte(inv_unk2_a);
		fs.WriteByte(inv_unk2_b);
		byte exp_a = (byte)(md.Experience & 0xFF);
		byte exp_b = (byte)((md.Experience & 0xFF00) >> 8);
		fs.WriteByte(exp_a);
		fs.WriteByte(exp_b);
		fs.WriteByte((byte)md.Unk5);
		fs.WriteByte((byte)md.Unk6);
		fs.WriteByte((byte)md.Unk7);
		fs.WriteByte((byte)md.Unk8);
		fs.WriteByte((byte)md.Unk9);
		fs.WriteByte((byte)md.Unk10);
	}
	public static void SaveMonsterSpriteInfo(ObjectData objDat)
	{
		string file = FileExplorer.GetUpperPath(FileExplorer.GetUpperPath(DataReader.FilePath)) + "/CRIT/ASSOC.ANM";
		string backupFile = file + ".BKP";
		if (!File.Exists(backupFile))
			File.Copy(file, backupFile);
		FileStream fs = new FileStream(file, FileMode.Open);
		fs.Position = 256;
		for (int i = 0; i < 64; i++)
		{
			fs.WriteByte((byte)objDat.MonsterData[i].SpriteID);
			fs.WriteByte((byte)objDat.MonsterData[i].AuxPalette);
		}
		fs.Close();
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
