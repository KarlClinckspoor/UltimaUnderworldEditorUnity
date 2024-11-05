using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Newtonsoft.Json;

public enum GameType
{
	Null,
	UW1,
	UW2
}

public static class DataReader {

	public static string FilePath;
	public static int[] LevelOffsets = new int[9];

	public static List<Texture2D> FloorTextures;
	public static List<Texture2D> WallTextures;
	public static List<Texture2D> DoorTextures;
	public static List<Texture2D> LeverTextures;
	public static List<Texture2D> OtherTextures;
	public static List<Texture2D> Portraits;
	public static List<Texture2D> ObjectGraphics;

	private static UIManager uiManager;

	#region Init and checks

	public static void Init(UIManager ui)
	{
		uiManager = ui;
	}

	public static string GetPathFromFile()
	{
		string appPath = Application.dataPath + "/config.cfg";
		bool exists = File.Exists(appPath);
		if (!exists)
			return Application.dataPath;
		try
		{
			StreamReader sr = new StreamReader(appPath);
			string path = sr.ReadToEnd();
			sr.Close();
			return path;
		}
		catch (Exception)
		{
			return Application.dataPath;
		}
	}

	public static bool ValidateFileFull(string path)
	{
		return (!string.IsNullOrEmpty(path) && ValidateFilePath(path) && ValidateFile(path));
	}

	public static bool ValidateFilePath(string path)
	{
		return File.Exists(path);
	}

	public static bool ValidateFile(string path)
	{
		FileStream fs = new FileStream(path, FileMode.Open);
		int test = fs.ReadByte();
		bool valid = true;
		//if (test == 135)
		//	valid = true;
		fs.Close();
		return valid;
	}

	public static GameType GetGameType(string path)
	{
		FileStream fs = new FileStream(path, FileMode.Open);
		int test = fs.ReadByte();
		GameType gameType = GameType.Null;
		if (test == 135)
			gameType = GameType.UW1;
		fs.Close();
		return gameType;
	}

	#endregion

	#region Level Data

	//TODO : To powinno brać string : ścieżkę do LEV.ARK.
	public static List<LevelData> LoadLevels()
	{
		if (!uiManager)
			return null;
		FileStream fs = new FileStream(FilePath, FileMode.Open);
		int blockCount = GetNumberOfBlocks(fs);
		int levelCount = blockCount / 15;
		//LevelData[] levelData = new LevelData[levelCount];
		List<LevelData> levelData = new List<LevelData>();
		int[] offsets = GetLevelOffsets(fs, levelCount);

		for (int i = 1; i < levelCount + 1; i++)
		{
			int levelOffset = offsets[i - 1];
			MapTile[,] mapData = ReadMapData(i, levelOffset, fs);
			int mobCount = ReadFreeListStart(i, levelOffset, ListType.MobActive, fs);
			int freeMob = ReadFreeListStart(i, levelOffset, ListType.Mobile, fs);
			int freeStat = ReadFreeListStart(i, levelOffset, ListType.Static, fs);
			int[] free = ReadFreeObjectIndexes(i, levelOffset, fs);
			int[] mobList = ReadActiveMobs(i, levelOffset, fs);

			StaticObject[] staticObjects = ReadStaticObjects(fs, mapData, levelOffset, i, mobList, mobCount);
		
			int animationOffset = offsets[i - 1 + levelCount];
			AnimationOverlay[] anims = ReadAnimationOverlays(i, animationOffset, fs);
			int textureOffset = offsets[i - 1 + (levelCount * 2)];

			int[] floortex = ReadFloorTextureMappings(i, textureOffset, fs);
			int[] walltex = ReadWallTextureMappings(i, textureOffset, fs);
			int[] doortex = ReadDoorTextureMappings(i, textureOffset, fs);

			LevelData level = new LevelData(i, mapData, staticObjects, floortex, walltex, doortex, free, mobList, freeMob, freeStat, mobCount, anims);
			//levelData[i - 1] = level;
			levelData.Add(level);
		}
		fs.Close();
		return levelData;
	}

	private static StaticObject[] ReadStaticObjects(FileStream fs, MapTile[,] mapData, int levelOffset, int level, int[] mobList, int mobCount)
	{
		StaticObject[] staticObjects = new StaticObject[1024];
		for (int y = 0; y < 64; y++)
		{
			for (int x = 0; x < 64; x++)
			{
				MapTile tile = mapData[x, y];
				if (tile.ObjectAdress > 0)
				{
					int index = tile.ObjectAdress;
					StaticObject mainObj = null;
					mainObj = ReadObject(fs, index, levelOffset, mainObj, staticObjects, tile, x, y, level, mobList, mobCount);

					int nextIndex = 0;
					StaticObject next = null;
					StaticObject current = mainObj;
					if (mainObj.NextAdress > 0)
					{
						nextIndex = mainObj.NextAdress;
						while (nextIndex > 0)
						{
							next = ReadObject(fs, nextIndex, levelOffset, next, staticObjects, tile, x, y, level, mobList, mobCount);
							next.PrevAdress = current.CurrentAdress;
							if (!next.IsQuantity)
								ReadContainer(fs, levelOffset, next, staticObjects, tile, x, y, level, mobList, mobCount);
							nextIndex = next.NextAdress;
							current = next;
						}
					}
					if (!mainObj.IsQuantity)
						ReadContainer(fs, levelOffset, mainObj, staticObjects, tile, x, y, level, mobList, mobCount);
				}
			}
		}
		return staticObjects;
	}

	private static StaticObject ReadObject(FileStream fs, int index, int offset, StaticObject so, StaticObject[] staticObjects, MapTile tile, int x, int y, int level, int[] mobList, int mobCount)
	{
		if (index < 256)
		{
			so = ReadMobileObjectFromIndex(level, index, offset, fs);
			for (int i = 0; i < mobCount; i++)
			{
				if (mobList[i] == index)
				{
					MobileObject mo = (MobileObject)so;
					mo.ActiveMob = true;
					mo.MobListIndex = i;
					break;
				}
			}
		}
		else
			so = ReadStaticObjectFromIndex(level, index, offset, fs);
		so.MapPosition = new Vector2Int(x, y);
		so.Tile = tile;
		if (staticObjects[index])
			Debug.LogWarning("Overwriting object at index " + index + " level " + level);
		staticObjects[index] = so;
		return so;
	}

	private static void ReadContainer(FileStream fs, int offset, StaticObject container, StaticObject[] staticObjects, MapTile tile, int x, int y, int level, int[] mobList, int mobCount)
	{
		int invIndex = 0;
		StaticObject inventory = null;
		StaticObject current = container;
		invIndex = container.Special;
		while (invIndex > 0)
		{
			inventory = ReadObject(fs, invIndex, offset, inventory, staticObjects, tile, x, y, level, mobList, mobCount);
			inventory.PrevAdress = current.CurrentAdress;
			if (!inventory.IsQuantity)
				ReadContainer(fs, offset, inventory, staticObjects, tile, x, y, level, mobList, mobCount);
			invIndex = inventory.NextAdress;
			current = inventory;
		}
	}

	public static int GetNumberOfBlocks(FileStream fs)
	{
		return fs.ReadByte() + fs.ReadByte() * 256;
	}

	public static int[] GetLevelOffsets(FileStream fs, int levelCount)
	{
		int[] offsets = new int[levelCount * 3];
		for (int i = 0; i < levelCount * 3; i++)
		{
			int a = fs.ReadByte();
			int b = fs.ReadByte();
			int c = fs.ReadByte();
			int d = fs.ReadByte();

			int offset = a + b * 256 + c * 256 * 256 + d * 256 * 256 * 256;
			offsets[i] = offset;
		}
		return offsets;
	}



	public static int GetLevelIndex(int level, FileStream fs)
	{
		int start = (level - 1) * 4 + 2;
		fs.Position = start;
		int a = fs.ReadByte();
		int b = fs.ReadByte();
		int c = fs.ReadByte();

		int index = a + b * 256 + c * 256 * 256;
		LevelOffsets[level - 1] = index;
		return index;
	}


	public static MapTile[,] ReadMapData(int level, int offset, FileStream fs)
	{
		//int index = GetLevelIndex(level, fs);

		//FileStream fs = new FileStream(FilePath, FileMode.Open);
		fs.Position = offset;

		MapTile[,] tiles = new MapTile[64, 64];
		for (int i = 0; i < 4096; i++)
		{
			int x = i % 64;
			int y = i / 64;

			int loc = i * 4;
			MapTile tile = new MapTile();
			fs.Position = offset + loc;
			tile.FileAdress = fs.Position;
			//RawData[i % 64,i / 64] = (fs.ReadByte() & 0xF);
			int a = fs.ReadByte();
			int b = fs.ReadByte();
			int c = fs.ReadByte();
			int d = fs.ReadByte();


			tile.Position = new Vector2Int(x, y);
			tile.TileType = (TileType)(a & 0x0F);
			tile.FloorHeight = (a & 0xF0) >> 4;
			tile.FloorTexture = (b & 0x3C) >> 2;
			tile.IsAntimagic = ((b & 0x40) >> 6) == 1;
			tile.IsDoor = ((b & 0x80) >> 7) == 1;
			tile.WallTexture = (c & 0x3F);
			//tile.ObjectAdress = ((c & 0xC0) >> 4) + (d << 4);
			tile.ObjectAdress = ((c & 0xC0) >> 6) + (d << 2);
			//tile.ObjectAdress = (((c & 0xC0) >> 6) * 16 * 16) + d;

			tile.Level = level;
			tiles[x, y] = tile;
		}
		//fs.Close();
		return tiles;
	}

	public static StaticObject ReadStaticObjectFromIndex(int level, int index, int offset, FileStream fs, bool log = false)
	{
		index -= 256;
		//int start = GetLevelIndex(level, fs) + 23296;
		int start = offset + 23296;

		fs.Position = start + index * 8;
		StaticObject obj = new StaticObject();
		obj.CurrentAdress = index + 256;
		obj.CurrentLevel = level;
		ReadStaticObject(fs, obj, log);
		if (log)
			Debug.LogFormat("Read object at index : {0}, start : {1}, name : {2}", index, ToHex(start + index * 8), obj.Name);
		return obj;
	}

	public static MobileObject ReadMobileObjectFromIndex(int level, int index, int offset, FileStream fs)
	{
		//int start = GetLevelIndex(level, fs) + 16384;
		int start = offset + 16384;
		fs.Position = start + index * 27;

		MobileObject obj = new MobileObject();
		obj.CurrentAdress = index;
		obj.CurrentLevel = level;
		ReadStaticObject(fs, obj);
		ReadMobileObject(fs, obj);

		return obj;
	}

	private static void ReadStaticObject(FileStream fs, StaticObject obj, bool log = false)
	{
		int b0 = fs.ReadByte();
		int b1 = fs.ReadByte();
		int b2 = fs.ReadByte();
		int b3 = fs.ReadByte();
		int b4 = fs.ReadByte();
		int b5 = fs.ReadByte();
		int b6 = fs.ReadByte();
		int b7 = fs.ReadByte();


		int id = b0 + ((b1 & 0x01) << 8);
		obj.SetID(id);
		obj.Flags = ((b1 & 0x0E) >> 1);
		obj.IsEnchanted = ((b1 & 0x10) >> 4) == 1;
		obj.IsDoorOpen = ((b1 & 0x20) >> 5) == 1;
		obj.IsInvisible = ((b1 & 0x40) >> 6) == 1;
		obj.IsQuantity = ((b1 & 0x80) >> 7) == 1;

		//Debug.LogFormat("fs.position : {5}, a : {0}, b : {1}, (b & 0x01) : {2}, (b & 0x01) << 8 : {3}, a + ((b & 0x01) << 8 : {4}", ToHex(a), ToHex(b), ToHex(b & 0x01), ToHex((b & 0x01) << 8), ToHex(a + ((b & 0x01) << 8)), fs.Position);
		obj.Name = StaticObject.GetName(obj.ObjectID);
		obj.ZPos = (b2 & 0x7F);
		obj.Direction = ((b2 & 0x80) >> 7) + ((b3 & 0x03) << 1);
		obj.YPos = (b3 & 0x1C) >> 2;
		obj.XPos = (b3 & 0xE0) >> 5;
		obj.Quality = (b4 & 0x3F);
		//obj.NextAdress = ((b4 & 0xC0) >> 4) + (b5 << 4);
		obj.NextAdress = ((b4 & 0xC0) >> 6) + (b5 << 2);
		//obj.NextAdress = (((b4 & 0xC0) >> 6) * 16 * 16) + b5;
		obj.Owner = (b6 & 0x3F);
		//obj.Quantity_Link_Special = ((b6 & 0xC0) >> 4) + (b7 << 4);
		obj.Special = ((b6 & 0xC0) >> 6) + (b7 << 2);
		//obj.Quantity_Link_Special = (((b6 & 0xC0) >> 6) * 16 * 16) + b7;
		if (log)
			Debug.LogFormat("[{0}][{1}][{2}][{3}][{4}][{5}][{6}][{7}]", ToHex(b0), ToHex(b1), ToHex(b2), ToHex(b3), ToHex(b4), ToHex(b5), ToHex(b6), ToHex(b7));
	}
	private static void ReadMobileObject(FileStream fs, MobileObject obj)
	{
		int b8 = fs.ReadByte();
		int b9 = fs.ReadByte();
		int bA = fs.ReadByte();
		int bB = fs.ReadByte();
		int bC = fs.ReadByte();
		int bD = fs.ReadByte();
		int bE = fs.ReadByte();
		int bF = fs.ReadByte();
		int b10 = fs.ReadByte();
		int b11 = fs.ReadByte();
		int b12 = fs.ReadByte();
		int b13 = fs.ReadByte();
		int b14 = fs.ReadByte();
		int b15 = fs.ReadByte();
		int b16 = fs.ReadByte();
		int b17 = fs.ReadByte();
		int b18 = fs.ReadByte();
		int b19 = fs.ReadByte();
		int b1A = fs.ReadByte();


		obj.HP = b8;
		obj.Goal = (bB & 0x0F);
		obj.GTarg = ((bB & 0xF0) >> 4) + ((bC & 0x0F) << 4);
		obj.Level = (bD & 0x0F);
		obj.Attitude = (bE & 0xC0) >> 6;
		obj.MobHeight = ((bF & 0xC0) >> 6) + ((b10 & 0x1F) << 2);
		obj.YHome = ((b16 & 0xF0) >> 4) + ((b17 & 0x03) << 4);
		obj.XHome = ((b17 & 0xFC) >> 2);
		obj.Heading = (b18 & 0x0F);
		obj.Hunger = (b19 & 0x3F);
		obj.Whoami = b1A;

		obj.B9 = b9;
		obj.BA = bA;    //Animation state ?
		obj.BB = bB;    //Goal + GTarg
		obj.BC = bC;    //GTarg
		obj.BD = bD;
		obj.BE = bE;    //Attitude
		obj.BF = bF;
		obj.B10 = b10;
		obj.B11 = b11;
		obj.B12 = b12;
		obj.B13 = b13;  //? 1 (0)
		obj.B14 = b14;  //? 2 (132)
		obj.B15 = b15;  //? 3 (0 / 32)
		obj.B16 = b16;  //YHome
		obj.B17 = b17;  //XHome + YHome
		obj.B18 = b18;
		obj.B19 = b19;  //Hunger?
		obj.B1A = b1A;
	}

	public static int[] ReadFreeObjectIndexes(int level, int offset, FileStream fs)
	{
		//int index = GetLevelIndex(level, fs) + 29440;
		int start = offset + 29440;
		//FileStream fs = new FileStream(FilePath, FileMode.Open);

		int max = 768 + 254;
		int[] free = new int[max];

		for (int i = 0; i < max; i++)
		{
			int loc = i * 2;
			fs.Position = start + loc;
			int a = fs.ReadByte();
			int b = fs.ReadByte();

			free[i] = b * 16 * 16 + a;
		}
		//fs.Close();
		return free;
	}

	public static int[] ReadActiveMobs(int level, int offset, FileStream fs)
	{
		//int index = GetLevelIndex(level, fs) + 31484;
		int start = offset + 31484;
		int[] mobs = new int[260];

		for (int i = 0; i < 260; i++)
		{
			fs.Position = start + i;
			mobs[i] = fs.ReadByte();
		}
		return mobs;
	}

	public enum ListType
	{
		MobActive = 0,
		Mobile = 2,
		Static = 4
	}

	public static int ReadFreeListStart(int level, int offset, ListType whichList, FileStream fs)
	{
		//int index = 31744 + GetLevelIndex(level, fs) + (int)whichList;
		int start = offset + 31744 + (int)whichList;

		//FileStream fs = new FileStream(FilePath, FileMode.Open);
		fs.Position = start;
		int a = fs.ReadByte();
		int b = fs.ReadByte();
		//fs.Close();
		return b * 16 * 16 + a;
	}


	public static int[] ReadFloorTextureMappings(int level, int offset, FileStream fs)
	{
		//int index = 289766 + (level - 1) * 122 + 48 * 2;
		int index = offset + 48 * 2;
		fs.Position = index;
		int[] texmap = new int[10];
		for (int i = 0; i < 10; i++)
		{
			int a = fs.ReadByte();
			int b = fs.ReadByte();
			texmap[i] = b * 16 * 16 + a;
		}
		return texmap;
	}

	public static int[] ReadWallTextureMappings(int level, int offset, FileStream fs)
	{
		//int index = 289766 + (level - 1) * 122;
		int index = offset;
		fs.Position = index;
		int[] texmap = new int[48];
		for (int i = 0; i < 48; i++)
		{
			int a = fs.ReadByte();
			int b = fs.ReadByte();
			texmap[i] = b * 16 * 16 + a;
		}
		return texmap;
	}

	public static int[] ReadDoorTextureMappings(int level, int offset, FileStream fs)
	{
		int index = offset + 58 * 2;
		fs.Position = index;
		int[] texmap = new int[6];
		for (int i = 0; i < 6; i++)
		{
			texmap[i] = fs.ReadByte();
		}
		return texmap;
	}

	public static AnimationOverlay[] ReadAnimationOverlays(int level, int offset, FileStream fs)
	{
		AnimationOverlay[] anims = new AnimationOverlay[64];
		fs.Position = offset;
		for (int i = 0; i < 64; i++)
		{
			int byt1_a = fs.ReadByte();
			int byt1_b = fs.ReadByte();
			int byt1 = byt1_a + byt1_b * 256;
			

			int byt2_a = fs.ReadByte();
			int byt2_b = fs.ReadByte();

			int byt3 = fs.ReadByte();
			int byt4 = fs.ReadByte();
			//Debug.LogFormat("raw data : {0}, {1}, {2}, {3}, {4}, {5}", ToHex(byt1_a), ToHex(byt1_b), ToHex(byt2_a), ToHex(byt2_b), ToHex(byt3), ToHex(byt4));
			int adress = (byt1 >> 6) & 0x3FF;
			if(adress > 0)
			{
				AnimationOverlay anim = new AnimationOverlay();
				anim.Adress = adress;
				anim.Duration = byt2_a + byt2_b * 256;
				anim.X = byt3;
				anim.Y = byt4;
				anims[i] = anim;
			}
		}
		return anims;
	}

	#endregion

	#region Textures

	public static void LoadTexturesFromResources()
	{
		//FIXME : move this to after external 
		FloorTextures = new List<Texture2D>(Resources.LoadAll<Texture2D>("Textures/Floors"));
		WallTextures = new List<Texture2D>(Resources.LoadAll<Texture2D>("Textures/Walls"));
		DoorTextures = new List<Texture2D>(Resources.LoadAll<Texture2D>("Textures/Doors"));
		LeverTextures = new List<Texture2D>(Resources.LoadAll<Texture2D>("Textures/Levers"));
		OtherTextures = new List<Texture2D>(Resources.LoadAll<Texture2D>("Textures/Other"));
		Portraits = new List<Texture2D>(Resources.LoadAll<Texture2D>("Textures/Heads"));
		ObjectGraphics = new List<Texture2D>(Resources.LoadAll<Texture2D>("Objects"));

		LoadExternalTextures("/Textures/Floors", FloorTextures, new Vector2Int(32, 32));
		LoadExternalTextures("/Textures/Walls", WallTextures, new Vector2Int(64, 64));
		LoadExternalTextures("/Textures/Doors", DoorTextures, new Vector2Int(32, 64));
		LoadExternalTextures("/Textures/Levers", LeverTextures, new Vector2Int(16, 16));
		LoadExternalTextures("/Textures/Other", OtherTextures);
		LoadExternalTextures("/Textures/Heads", Portraits, new Vector2Int(34, 34));
		LoadExternalTextures("/Textures/Objects", ObjectGraphics);

		foreach (var floor in FloorTextures)
			floor.filterMode = FilterMode.Point;
		foreach (var wall in WallTextures)
			wall.filterMode = FilterMode.Point;
		foreach (var door in DoorTextures)
			door.filterMode = FilterMode.Point;
		foreach (var lever in LeverTextures)
			lever.filterMode = FilterMode.Point;
		foreach (var head in Portraits)
			head.filterMode = FilterMode.Point;
		foreach (var other in OtherTextures)
			other.filterMode = FilterMode.Point;
		foreach (var other in ObjectGraphics)
			other.filterMode = FilterMode.Point;
	}

	private static void LoadExternalTextures(string path, List<Texture2D> targetList, Vector2Int dimensions = default)
	{
		path = Application.dataPath + path;
		bool dirExists = Directory.Exists(path);
		//Debug.LogFormat("Path : {0}", path);
		if (dirExists)
		{
			//Debug.Log("Dir exists");
			DirectoryInfo di = new DirectoryInfo(path);
			foreach (var file in di.GetFiles())
			{
				if (file.Extension == ".png")
				{
					Texture2D newTex = new Texture2D(1, 1);
					if (dimensions != default)
						newTex = new Texture2D(dimensions.x, dimensions.y);
					bool conversion = ImageConversion.LoadImage(newTex, File.ReadAllBytes(file.FullName));
					if (conversion)
					{
						//Debug.Log("Conversion");
						targetList.Add(newTex);
					}
				}
			}
		}
		else
			Directory.CreateDirectory(path);
	}

	public static TextureData LoadTextures()
	{
		if (!uiManager)
			return null;
		TextureData texData = null;
		LoadTexturesFromResources();

		string filePath = FileExplorer.GetUpperPath(FilePath) + "/pals.dat";
		FileStream fs = new FileStream(filePath, FileMode.Open);
		List<Color[]> pals = new List<Color[]>();
		for (int i = 0; i < 7; i++)
		{
			int[] rawPal = ReadPallette(fs, i);
			Color[] pal = CreateColorPallette(rawPal);
			pals.Add(pal);
		}
		fs.Close();
		List<int[]> auxPalsRaw = ReadAuxPalettes();
		List<Color[]> auxPals = new List<Color[]>();
		for (int i = 0; i < auxPalsRaw.Count; i++)
			auxPals.Add(CreateAuxPalette(auxPalsRaw[i], pals[0]));
		
		int fCount = 0, fFirst = 0;
		int[] f32o = ReadGraphicOffsets("F32.TR", ref fCount, ref fFirst);
		List<int[]> f32raw = ReadRawTextures("F32.TR", f32o, 32, 32);
		List<Texture2D> f32tex = new List<Texture2D>();
		for (int i = 0; i < fCount; i++)
			f32tex.Add(ConvertToTexture("Floor32_" + i, f32raw[i], 32, 32, pals[0], true));
		TextureContainer floors = new TextureContainer(f32tex, fFirst, 0, new AdditionalTextureInfo(false, 32, 32, -1, -1));

		int wCount = 0, wFirst = 0;
		int[] w64o = ReadGraphicOffsets("W64.TR", ref wCount, ref wFirst);
		List<int[]> w64raw = ReadRawTextures("W64.TR", w64o, 64, 64);
		List<Texture2D> w64tex = new List<Texture2D>();
		for (int i = 0; i < wCount; i++)
			w64tex.Add(ConvertToTexture("Wall64_" + i, w64raw[i], 64, 64, pals[0], true));
		TextureContainer walls = new TextureContainer(w64tex, wFirst, 0, new AdditionalTextureInfo(false, 64, 64, -1, -1));

		int dCount = 0, dFirst = 0;
		int[] dooro = ReadGraphicOffsets("DOORS.GR", ref dCount, ref dFirst);
		List<int[]> doorraw = ReadRawTextures("DOORS.GR", dooro, 32, 64, 5);
		List<Texture2D> doortex = new List<Texture2D>(dooro.Length);
		for (int i = 0; i < dCount; i++)
			doortex.Add(ConvertToTexture("Door_" + i, doorraw[i], 32, 64, pals[0], true));
		TextureContainer doors = new TextureContainer(doortex, dFirst, 5, new AdditionalTextureInfo(true, 32, 64, 0, 8));

		int lCount = 0, lFirst = 0;
		int[] levero = ReadGraphicOffsets("TMFLAT.GR", ref lCount, ref lFirst);
		List<int[]> leverraw = ReadRawTextures("TMFLAT.GR", levero, 16, 16, 5);
		List<Texture2D> levertex = new List<Texture2D>(levero.Length);
		for (int i = 0; i < lCount; i++)
			levertex.Add(ConvertToTexture("Lever_" + i, leverraw[i], 16, 16, pals[0], true));
		TextureContainer levers = new TextureContainer(levertex, lFirst, 5, new AdditionalTextureInfo(true, 16, 16, 0, 1));

		int oCount = 0, oFirst = 0;
		int[] othero = ReadGraphicOffsets("TMOBJ.GR", ref oCount, ref oFirst);
		Dictionary<int, Vector2Int> dict = new Dictionary<int, Vector2Int>();
		List<int[]> otherraw = ReadRawTextures("TMOBJ.GR", othero, 0, 0, 5, true, dict);
		List<Texture2D> othertex = new List<Texture2D>(othero.Length);
		for (int i = 0; i < othero.Length; i++)
			othertex.Add(ConvertToTexture("Other_" + i, otherraw[i], dict[i].x, dict[i].y, pals[0], true));
		TextureContainer other = new TextureContainer(othertex, oFirst, 5, null);

		int pCount = 0, pFirst = 0;		
		int[] portrOffsets = ReadGraphicOffsets("GENHEAD.GR", ref pCount, ref pFirst);
		List<int[]> portrRaw = ReadRawTextures("GENHEAD.GR", portrOffsets, 34, 34, 5);
		List<Texture2D> portrTex = new List<Texture2D>();
		for (int i = 0; i < portrRaw.Count; i++)
			portrTex.Add(ConvertToTexture("Generic_Head_" + i, portrRaw[i], 34, 34, pals[0], true));
		TextureContainer genheads = new TextureContainer(portrTex, pFirst, 5, new AdditionalTextureInfo(true, 34, 34, 132, 4));
		
		int[] npcOffsets = ReadGraphicOffsets("CHARHEAD.GR", ref pCount, ref pFirst);
		List<int[]> npcRaw = ReadRawTextures("CHARHEAD.GR", npcOffsets, 34, 34, 5);
		//Debug.LogFormat("npcOffset count : {0}, pCount : {1}, npcRaw.Len : {2}", npcOffsets.Length, pCount, npcRaw.Count);
		List<Texture2D> npcTex = new List<Texture2D>();
		for (int i = 0; i < npcRaw.Count; i++)
			npcTex.Add(ConvertToTexture("NPC_Head_" + i, npcRaw[i], 34, 34, pals[0], true));
		TextureContainer npcheads = new TextureContainer(npcTex, pFirst, 5, new AdditionalTextureInfo(true, 34, 34, 132, 4));

		int iCount = 0, iFirst = 0;
		int[] itemOffsets = ReadGraphicOffsets("OBJECTS.GR", ref iCount, ref iFirst);
		fs = new FileStream(FileExplorer.GetUpperPath(FilePath) + "/OBJECTS.GR", FileMode.Open);
		List<Texture2D> itemsTex = new List<Texture2D>();
		List<int[]> rawTexs = new List<int[]>();
		List<int> objAuxPals = new List<int>();

		//string datadump = "";

		for (int i = 0; i < 461; i++)	//Forced 460
		{
			AdditionalTextureInfo ati = new AdditionalTextureInfo(true, 0, 0, 0, 0);
			int[] nibbles = Read4BitGraphic(fs, itemOffsets[i], 6, i, ati);

			//string test = "\n" + i.ToString() + ", count : " + nibbles.Length + "\n";
			//foreach (var nib in nibbles)
			//	test += nib + " ";
			//test += "\n";
			//Debug.Log(test);
			
			//Debug.LogFormat("<b>Converting {0}</b>", i);
			int[] rawGr = Convert4BitToRawTex(nibbles, ati.Width, ati.Height);
			rawTexs.Add(rawGr);
			itemsTex.Add(ConvertToTexture("Item_" + i, rawGr, ati.Width, ati.Height, pals[0], true, auxPalsRaw[ati.AuxPalette]));
			objAuxPals.Add(ati.AuxPalette);

			//List<int> newNibs = DataWriter.CompressToRLE(rawGr);
			//foreach (var nib in newNibs)
			//	test += nib + " ";
			//test += "\n";
			//test += "\n";

			//datadump += test;
		}
		//DataWriter.SaveStringToTxt(datadump, "objects.txt");
		//DataWriter.ExportDefaultTextures("Export/ObjectsTest", itemsTex, 1);
		TextureContainer items = new TextureContainer(itemsTex, iFirst, 6, null);
		items.RawTextures = rawTexs;
		items.AuxPalettes = objAuxPals;

		fs.Close();
		fs = new FileStream(FileExplorer.GetUpperPath(FilePath) + "/TERRAIN.DAT", FileMode.Open);
		int[] wallDat = new int[256];
		int[] florDat = new int[256];
		for (int i = 0; i < 256; i++)		
			wallDat[i] = fs.ReadByte() + fs.ReadByte() * 256;
		for (int i = 0; i < 256; i++)
			florDat[i] = fs.ReadByte() + fs.ReadByte() * 256;
		
		fs.Close();

		texData = new TextureData(pals, auxPalsRaw, auxPals, floors, walls, doors, levers, other, genheads, npcheads, items, wallDat, florDat);
		return texData;
	}

	public static int[] ReadPallette(FileStream fs, int index)
	{
		int[] pal = new int[256 * 3];
		//string filePath = FileExplorer.GetUpperPath(FilePath) + "/pals.dat";
		//FileStream fs = new FileStream(filePath, FileMode.Open);
		for (int i = 0; i < 256 * 3; i++)
		{
			pal[i] = fs.ReadByte();
		}
		//fs.Close();
		return pal;
	}

	public static List<int[]> ReadAuxPalettes()
	{
		string filePath = FileExplorer.GetUpperPath(FilePath) + "/ALLPALS.dat";
		FileStream fs = new FileStream(filePath, FileMode.Open);

		List<int[]> auxpals = new List<int[]>();
		for (int p = 0; p < 31; p++)
		{
			int[] auxpal = new int[16];
			for (int i = 0; i < 16; i++)
			{
				auxpal[i] = fs.ReadByte();
			}
			auxpals.Add(auxpal);
		}
		return auxpals;
	}

	public static Color[] CreateColorPallette(int[] oldPal)
	{
		Color[] newPal = new Color[256];
		for (int i = 0; i < 256 * 3; i += 3)
		{
			int r = oldPal[i];
			int g = oldPal[i + 1];
			int b = oldPal[i + 2];

			Color col = new Color(r / 63.0f, g / 63.0f, b / 63.0f);
			newPal[i / 3] = col;
		}
		return newPal;
	}
	public static Color[] CreateAuxPalette(int[] auxPal, Color[] pal)
	{
		Color[] newPal = new Color[16];
		for (int i = 0; i < 16; i++)
			newPal[i] = pal[auxPal[i]];
		return newPal;
	}

	public static int ReadPaletteMapping(FileStream fs, int r, int g, int b)
	{
		fs.Position = r * 256 * 256 + g * 256 + b;
		return fs.ReadByte();
	}

	public static int[] ReadGraphicOffsets(string fileName, ref int count, ref int first)
	{
		string filePath = FileExplorer.GetUpperPath(FilePath) + "/" + fileName;
		FileStream fs = new FileStream(filePath, FileMode.Open);
		int start = fs.ReadByte();
		int size = 0;
		if (start == 2)
			size = fs.ReadByte();

		int c_a = fs.ReadByte();
		int c_b = fs.ReadByte();

		count = c_a + c_b * 256;
		
		int[] offsets = new int[count];
		first = start + 2;
		for (int i = 0; i < count; i++)
		{
			fs.Position = start + 2 + i * 4;
			int a = fs.ReadByte();
			int b = fs.ReadByte();
			int c = fs.ReadByte();
			int d = fs.ReadByte();

			int offset = a + (b * 256) + (c * 256 * 256) + (d * 256 * 256 * 256);
			offsets[i] = offset;
		}
		fs.Close();
		return offsets;
	}

	public static int[] Read4BitGraphic(FileStream fs, long offset, int startingOffset, int index, AdditionalTextureInfo ati)
	{
		fs.Position = offset + 1;
		ati.Width = fs.ReadByte();
		ati.Height = fs.ReadByte();
		ati.AuxPalette = fs.ReadByte();
		ati.SizeA = fs.ReadByte();
		ati.SizeB = fs.ReadByte();
		int size = ati.SizeA + ati.SizeB * 256;

		//Debug.LogFormat("Index : {0}, size : {1}, fs.Position : {2}", index, size, fs.Position);
		return Read4BitGraphic(fs, fs.Position, ati.Width, ati.Height, size);
	}

	public static int[] Read4BitGraphic(FileStream fs, long offset, int width, int height, int count)
	{
		int i = 0;
		int[] rawGr = new int[count];
		while (count > 1)
		{
			int n = fs.ReadByte();
			int n1 = (n & 0xF0) >> 4;
			int n2 = n & 0x0F;
			rawGr[i] = n1;
			rawGr[i + 1] = n2;

			i += 2;
			count -= 2;
		}
		return rawGr;
	}

	public static int[] Convert4BitToRawTex(int[] nibbles, int width, int height)
	{
		int[] rawImg = new int[width * height];
		int state = 0;
		int ptr = 0;
		int count = 0;
		int repeatCount = 0;
		int curNibble = 0;
		int curPixel = 0;
		//Debug.LogFormat("size : {0}, nibbles.Length : {1}", width * height, nibbles.Length);
		while(ptr < nibbles.Length || curPixel < rawImg.Length)
		{
			if (ptr >= nibbles.Length - 1)
				break;
			//Debug.LogFormat("state : {0}, nibble : {1}, ptr : {2}, count : {3}, pixel : {4}, repeatCount : {5}, nibbles.Length : {6}", state, curNibble, ptr, count, curPixel, repeatCount, nibbles.Length);
			if(state == 0)		//Start
			{
				count = getCount(nibbles, ref ptr);
				if(count == 1)
					state = 2;
				else if (count == 2)
					repeatCount = getCount(nibbles, ref ptr) - 1;
				else
					state = 1;
			}
			else if(state == 1)	//Repeat
			{
				curNibble = getNibble(nibbles, ref ptr);
				if(width * height - curPixel < count)
					count = width * height - curPixel;
				for (int i = 0; i < count; i++)
					rawImg[curPixel++] = curNibble;
				if(repeatCount == 0)
					state = 2;
				else
				{
					state = 0;
					repeatCount--;
				}
			}
			else if(state == 2)	//Run
			{
				count = getCount(nibbles, ref ptr);
				if(width * height - curPixel < count)
					count = width * height - curPixel;
				for (int i = 0; i < count; i++)
				{
					curNibble = getNibble(nibbles, ref ptr);
					rawImg[curPixel++] = curNibble;
				}
				state = 0;
			}
		}
		return rawImg;
	}
	private static int getCount(int[] nibbles, ref int ptr)
	{
		int n1;
		int n2;
		int n3;
		int count = 0;

		n1 = getNibble(nibbles, ref ptr);
		count = n1;

		if (count == 0)
		{
			n1 = getNibble(nibbles, ref ptr);
			n2 = getNibble(nibbles, ref ptr);
			count = (n1 << 4) | n2;
		}
		if (count == 0)
		{
			n1 = getNibble(nibbles, ref ptr);
			n2 = getNibble(nibbles, ref ptr);
			n3 = getNibble(nibbles, ref ptr);
			count = (((n1 << 4) | n2) << 4) | n3;
		}
		return count;
	}
	private static int getNibble(int[] nibbles, ref int ptr)
	{
		int nibble = nibbles[ptr];
		ptr++;
		return nibble;
	}

	public static List<int[]> ReadRawTextures(string fileName, int[] offsets, int width, int height, int startingOffset = 0, bool specificSize = false, Dictionary<int, Vector2Int> dict = null)
	{
		if (offsets == null)
			return null;
		int count = offsets.Length;
		List<int[]> textures = new List<int[]>();
		string filePath = FileExplorer.GetUpperPath(FilePath) + "/" + fileName;
		FileStream fs = new FileStream(filePath, FileMode.Open);
		for (int i = 0; i < count; i++)
		{
			if (!specificSize)
				fs.Position = offsets[i] + startingOffset;
			else
			{
				fs.Position = offsets[i] + 1;
				width = fs.ReadByte();
				height = fs.ReadByte();
				fs.Position = offsets[i] + startingOffset;
				dict[i] = new Vector2Int(width, height);
			}
			int[] texture = new int[width * height];
			for (int j = 0; j < width * height; j++)
			{
				texture[j] = fs.ReadByte();
				if(texture[j] == -1)
				{
					fs.Close();
					return textures;
				}
			}
			textures.Add(texture);
		}
		fs.Close();
		return textures;
	}

	public static Texture2D ConvertToTexture(string name, int[] rawtex, int width, int height, Color[] pal, bool reverse, int[] auxpal = null)
	{
		Texture2D tex = new Texture2D(width, height);
		tex.filterMode = FilterMode.Point;
		tex.name = name;
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				Color col = Color.black;
				int index = x + y * width;
				if (rawtex[index] == 0)
				{
					col = new Color(0, 0, 0, 0);
				}
				else
				{
					try
					{
						if(auxpal != null)
						{
							int palIndex = rawtex[index];
							col = pal[auxpal[palIndex]];
						}
						else
							col = pal[rawtex[index]];
					}
					catch (Exception e)
					{
						Debug.LogFormat("WTF Happened? x : {0}, y : {1}, index : {2}, rawtex[index] : {3}, name : {4}, width : {5}, height : {6}", x, y, index, rawtex[index], name, width, height);
						col = new Color(0, 0, 0, 0);
					}
				}

				if (reverse)
					tex.SetPixel(x, height - y - 1, col);
				else
					tex.SetPixel(x, y, col);
			}
		}
		tex.Apply();
		return tex;
	}


	public static List<Texture2D> GetLevelFloors(LevelData level)
	{
		List<Texture2D> list = new List<Texture2D>();
		for (int i = 0; i < 10; i++)
			list.Add(MapCreator.GetFloorTextureFromIndex(i, level.Level));
		return list;
	}
	public static List<Texture2D> GetLevelWalls(LevelData level)
	{
		List<Texture2D> list = new List<Texture2D>();
		for (int i = 0; i < 48; i++)
			list.Add(MapCreator.GetWallTextureFromIndex(i, level.Level));
		return list;
	}
	public static List<Texture2D> GetLevelDoors(LevelData level)
	{
		List<Texture2D> list = new List<Texture2D>();
		for (int i = 0; i < 6; i++)
			list.Add(MapCreator.GetDoorTextureFromIndex(i, level.Level));
		return list;
	}

	public static Texture2D GetResourceTexture(TextureType texType, int index)
	{
		Texture2D tex = null;
		if (texType == TextureType.Floor && index > -1 && index < FloorTextures.Count)
			tex = FloorTextures[index];
		else if (texType == TextureType.Wall && index > -1 && index < WallTextures.Count)
			tex = WallTextures[index];
		else if (texType == TextureType.Door && index > -1 && index < DoorTextures.Count)
			tex = DoorTextures[index];
		else if (texType == TextureType.Lever && index > -1 && index < LeverTextures.Count)
			tex = LeverTextures[index];
		else if (texType == TextureType.Other && index > -1 && index < OtherTextures.Count)
			tex = OtherTextures[index];
		else if ((texType == TextureType.GenericHead || texType == TextureType.NPCHead ) && index > -1 && index < Portraits.Count)
			tex = Portraits[index];
		else if (texType == TextureType.Object && index > -1 && index < ObjectGraphics.Count)
			tex = ObjectGraphics[index];
		//else if (texType == TextureType.Object && index > -1 && index < MapCreator.TextureData.Objects.Textures.Count)
		//	tex = MapCreator.TextureData.Objects.Textures[index];

		if (tex == null)
			return null;
		return ForceTextureSize(texType, tex);
	}

	public static Texture2D ForceTextureSize(TextureType texType, Texture2D oldTex)
	{
		if (texType == TextureType.Other || texType == TextureType.Object)
			return oldTex;

		int width = 0;
		int height = 0;
		if (texType == TextureType.Floor)
		{
			width = 32;
			height = 32;
		}
		else if (texType == TextureType.Wall)
		{
			width = 64;
			height = 64;
		}
		else if (texType == TextureType.NPCHead || texType == TextureType.GenericHead)
		{
			width = 34;
			height = 34;
		}
		else if (texType == TextureType.Door)
		{
			width = 32;
			height = 64;
		}
		else if (texType == TextureType.Lever)
		{
			width = 16;
			height = 16;
		}

		Texture2D newTex = new Texture2D(width, height);
		newTex.name = oldTex.name;
		newTex.filterMode = FilterMode.Point;
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				newTex.SetPixel(x, y, oldTex.GetPixel(x, y));
			}
		}
		newTex.Apply();
		return newTex;
	}

	#endregion

	#region Strings

	public static StringData LoadStrings()
	{
		FileStream fs = new FileStream(FileExplorer.GetUpperPath(FilePath) + "/STRINGS.PAK", FileMode.Open);
		Tuple<HuffmanNode[], long> pair = ReadHuffmanNodes(fs);
		Tuple<List<StringBlock>, Dictionary<int, int>> blockPair = ReadStringBlocks(fs, pair.Item2);
		for (int i = 0; i < blockPair.Item1.Count; i++)
		{
			StringBlock block = blockPair.Item1[i];
			ReadStrings(fs, block, pair.Item1);
		}
		fs.Close();
		StringData strData = new StringData(pair.Item1, blockPair.Item1, blockPair.Item2);
		return strData;
	}

	public static Tuple<HuffmanNode[], long> ReadHuffmanNodes(FileStream fs)
	{
		fs.Position = 0;

		int a = fs.ReadByte();
		int b = fs.ReadByte();

		int nodeCount = a + b * 256;

		HuffmanNode[] nodes = new HuffmanNode[nodeCount];
		//Debug.Log("Node count  " + nodeCount);
		for (int i = 0; i < nodeCount; i++)
		{
			int _char = fs.ReadByte();
			int parent = fs.ReadByte();
			int left = fs.ReadByte();
			int right = fs.ReadByte();
			nodes[i] = new HuffmanNode(_char, i, parent, left, right);
			//Debug.LogFormat("Node {0} : {1}", i, nodes[i]);
		}
		Tuple<HuffmanNode[], long> pair = new Tuple<HuffmanNode[], long>(nodes, fs.Position);
		return pair;
	}

	public static Tuple<List<StringBlock>, Dictionary<int, int>> ReadStringBlocks(FileStream fs, long startOffset)
	{
		fs.Position = startOffset;

		int a = fs.ReadByte();
		int b = fs.ReadByte();

		int blockCount = a + b * 256;
		List<StringBlock> blocks = new List<StringBlock>();
		Dictionary<int, int> blockDict = new Dictionary<int, int>();

		for (int i = 0; i < blockCount; i++)
		{
			int blk_a = fs.ReadByte();
			int blk_b = fs.ReadByte();
			int blk_c = fs.ReadByte();
			int blk_d = fs.ReadByte();
			int blk_e = fs.ReadByte();
			int blk_f = fs.ReadByte();

			StringBlock block = new StringBlock();
			block = new StringBlock();
			block.BlockNumber = blk_a + blk_b * 256;
			block.Offset = blk_c + blk_d * 256 + blk_e * 256 * 256 + blk_f * 256 * 256 * 256;
			blockDict[block.BlockNumber] = i;
			blocks.Add(block);
			//Debug.LogFormat("String block {0}, block number {1}", i, blocks[i].BlockNumber);
		}
		for (int i = 0; i < blockCount; i++)
		{
			fs.Position = blocks[i].Offset;
			int blk2_a = fs.ReadByte();
			int blk2_b = fs.ReadByte();

			blocks[i].StringCount = blk2_a + blk2_b * 256;
			blocks[i].Offsets = new List<int>();
			for (int j = 0; j < blocks[i].StringCount; j++)
			{
				int blk3_a = fs.ReadByte();
				int blk3_b = fs.ReadByte();
				int offset = blk3_a + blk3_b * 256;
				blocks[i].Offsets.Add(offset);

				//Debug.LogFormat("string block {0} string {1} offset : {2}", i, j, offset);
			}
		}

		return new Tuple<List<StringBlock>, Dictionary<int, int>>(blocks, blockDict);
	}

	public static List<string> ReadStrings(FileStream fs, StringBlock block, HuffmanNode[] nodes, bool debug = false)
	{
		if (block == null)
			return null;

		block.Strings = new List<string>();
		for (int i = 0; i < block.StringCount; i++)
		{
			fs.Position = block.Offset + 2 + (2 * block.StringCount) + block.Offsets[i];
			string curString = "";
			char curChar = default(char);

			int safe = 500;
			int raw = 0;
			int bit = 0;
			while (true)
			{
				curChar = GetChar(fs, nodes, ref raw, ref bit, debug);
				if (curChar == '|')
				{
					block.Strings.Add(curString);
					break;
				}
				curString += curChar;

				safe--;
				if (safe == 0)
				{
					Debug.LogErrorFormat("Failed to get string, fs.Position : {0}, i : {1}, block : {2}, curChar : {43 curString : {4}", fs.Position, i, block, curChar, curString);
					break;
				}
			}
			//if (i == block.StringCount - 1)
			//{
			//	Debug.LogFormat("Last string ({0}) of block {1}, offset : {2}, string : {3}", i, block.BlockNumber, ToHex(fs.Position - 1), block.Strings[i]);
			//}
		}
		return block.Strings;
	}

	public static char GetChar(FileStream fs, HuffmanNode[] nodes, ref int raw, ref int bit, bool debug = false)
	{
		HuffmanNode curNode = nodes[nodes.Length - 1];
		//Debug.Log("nodes.Length - 1 " + (nodes.Length - 1));

		int safe = 100;

		while (curNode.Left != 255 && curNode.Right != 255)
		{

			if (bit == 0)
			{
				raw = fs.ReadByte();
				bit = 8;
			}
			//Debug.LogFormat("curNode : {0}, raw : {1}({3}), bit : {2}, fs.Position (after) : {4}", curNode, raw, bit, ToHex(raw), ToHex(fs.Position));
			if (((raw & 0x80) >> 7) == 1)
			{
				curNode = nodes[curNode.Right];
				//Debug.Log("right");
			}
			else
			{
				curNode = nodes[curNode.Left];
				//Debug.Log("left");
			}

			bit--;
			raw = raw << 1;

			safe--;
			if (safe == 0)
			{
				Debug.LogErrorFormat("Failed to get char, curNode {0}, raw {1}, bit {2}, fs.Position {3}", curNode, raw, bit, fs.Position);
				break;
			}
		}
		if (debug)
		{
			Debug.LogFormat("Got char : {0}[{2}], node {1}", (char)curNode.Char, curNode, curNode.Char);
		}
		return (char)curNode.Char;
	}

	#endregion

	#region Conversations

	public static ConversationData LoadConversations()
	{
		FileStream fs = new FileStream(FileExplorer.GetUpperPath(FilePath) + "/CNV.ARK", FileMode.Open);
		Conversation[] convs = LoadConversations(fs);
		ConversationData convData = new ConversationData(convs, ReadPrivateVariables());
		if (!Directory.Exists(Application.dataPath + "/Conversations"))
			Directory.CreateDirectory(Application.dataPath + "/Conversations");
		convData.DefaultFunctions = ReadDefaultConversationFunctions();
		return convData;
	}

	public static Conversation[] LoadConversations(FileStream fs)
	{
		int cc_a = fs.ReadByte();
		int cc_b = fs.ReadByte();

		int convCount = cc_a + cc_b * 256;

		Conversation[] convs = new Conversation[convCount];

		for (int i = 0; i < convCount; i++)
		{
			fs.Position = 2 + i * 4;
			int co_a = fs.ReadByte();
			int co_b = fs.ReadByte();
			int co_c = fs.ReadByte();
			int co_d = fs.ReadByte();

			int convOffset = co_a + co_b * 256 + co_c * 256 * 256 + co_d * 256 * 256 * 256;

			Conversation conv = null;
			bool editorFile = File.Exists(ConversationEditor.GetConversationFile(i));
			if(editorFile)
			{
				//Debug.LogFormat("Loading conversation, found editor file, conv offset : {0}", convOffset);
				if (convOffset == 0)
				{
					conv = new Conversation(ConversationState.Unconverted);
				}
				else
					conv = LoadConversation(fs, convOffset, ConversationState.Converted);
			}
			else
			{
				if (convOffset == 0)
					continue;
				else
					conv = LoadConversation(fs, convOffset, ConversationState.Uneditable);
			}
			conv.Slot = i;
			convs[i] = conv;
		}

		return convs;
	}

	public static Conversation LoadConversation(FileStream fs, int offset, ConversationState state)
	{
		fs.Position = offset;

		Conversation conv = new Conversation(state);

		int co_u1a = fs.ReadByte();
		int co_u1b = fs.ReadByte();
		int co_u2a = fs.ReadByte();
		int co_u2b = fs.ReadByte();
		int co_csa = fs.ReadByte();
		int co_csb = fs.ReadByte();
		int co_u3a = fs.ReadByte();
		int co_u3b = fs.ReadByte();
		int co_u4a = fs.ReadByte();
		int co_u4b = fs.ReadByte();
		int co_sba = fs.ReadByte();
		int co_sbb = fs.ReadByte();
		int co_msa = fs.ReadByte();
		int co_msb = fs.ReadByte();
		int co_iga = fs.ReadByte();
		int co_igb = fs.ReadByte();

		conv.Unk00 = co_u1a + co_u1b * 256;
		conv.Unk02 = co_u2a + co_u2b * 256;
		conv.CodeSize = co_csa + co_csb * 256;
		conv.Unk06 = co_u3a + co_u3b * 256;
		conv.Unk08 = co_u4a + co_u4b * 256;
		conv.StringBlock = co_sba + co_sbb * 256;
		conv.MemorySlots = co_msa + co_msb * 256;
		conv.ImportedFunctionsAndVariables = co_iga + co_igb * 256;

		conv.Functions = new ConversationFunction[conv.ImportedFunctionsAndVariables];
		for (int i = 0; i < conv.ImportedFunctionsAndVariables; i++)
		{
			ConversationFunction convFunc = new ConversationFunction();

			int cf_len_a = fs.ReadByte();
			int cf_len_b = fs.ReadByte();

			int convFuncLen = cf_len_a + cf_len_b * 256;
			convFunc.NameLength = convFuncLen;
			string funcName = "";
			for (int j = 0; j < convFuncLen; j++)
			{
				char c = (char)fs.ReadByte();
				funcName += c;
			}
			convFunc.Name = funcName;

			int cf_id_a = fs.ReadByte();
			int cf_id_b = fs.ReadByte();
			int cf_unk_a = fs.ReadByte();
			int cf_unk_b = fs.ReadByte();
			int cf_typ_a = fs.ReadByte();
			int cf_typ_b = fs.ReadByte();
			int cf_ret_a = fs.ReadByte();
			int cf_ret_b = fs.ReadByte();
			convFunc.Id_Adress = cf_id_a + cf_id_b * 256;
			convFunc.Unk04 = cf_unk_a + cf_unk_b * 256;
			convFunc.Type = cf_typ_a + cf_typ_b * 256;
			if (convFunc.Type == ConversationFunction.Variable)
				conv.ImportedVariables++;
			convFunc.Return = cf_ret_a + cf_ret_b * 256;

			conv.Functions[i] = convFunc;
		}
		if (conv.ImportedVariables != 31)
			Debug.LogWarningFormat("Conversation {0} has invalid imported variable count {1}", conv.Slot, conv.ImportedVariables);
		int maxId = int.MinValue;
		for (int i = 0; i < conv.Functions.Length; i++)
		{
			if(conv.Functions[i].Type == ConversationFunction.Variable)
			{
				if (conv.Functions[i].Id_Adress > maxId)
					maxId = conv.Functions[i].Id_Adress;
			}
		}
		conv.FirstMemorySlot = maxId + 1;
		conv.Code = new List<int>();
		for (int i = 0; i < conv.CodeSize; i++)
		{
			int int_a = fs.ReadByte();
			int int_b = fs.ReadByte();

			conv.Code.Add(int_a + int_b * 256);
		}
		//conv.SetPrivateGlobals(conv.MemorySlots);
		conv.SetSavedVariables(conv.MemorySlots - conv.FirstMemorySlot);
		return conv;
	}

	public static Dictionary<int, int> ReadPrivateVariables()
	{
		FileInfo fi = new FileInfo(FileExplorer.GetUpperPath(FilePath) + "/BABGLOBS.DAT");
		FileStream fs = new FileStream(fi.FullName, FileMode.Open);
		Dictionary<int, int> privVars = new Dictionary<int, int>();

		while(fs.Position < fi.Length)
		{
			int key_a = fs.ReadByte();
			int key_b = fs.ReadByte();
			int val_a = fs.ReadByte();
			int val_b = fs.ReadByte();

			int key = key_a + key_b * 256;
			int val = val_a + val_b * 256;

			privVars[key] = val;
		}

		fs.Close();
		return privVars;
	}

	public static ConversationFunction[] ReadDefaultConversationFunctions()
	{
		string filePath = Application.streamingAssetsPath + "/functions.dat";
		Stream stream = File.Open(filePath, FileMode.Open);
		BinaryFormatter bf = new BinaryFormatter();
		ConversationFunction[] funcs = (ConversationFunction[])bf.Deserialize(stream);
		//Debug.LogFormat("Def conv func : {0}", funcs);
		//foreach (var fun in funcs)
		//{
		//	Debug.LogFormat("fun {0}", fun.Name);
		//}
		return funcs;
	}

	#endregion

	#region Object Data



	public static void LoadCommonData(ObjectData objDat)
	{
		FileStream fs = new FileStream(FileExplorer.GetUpperPath(FilePath) + "/COMOBJ.DAT", FileMode.Open);
		int val_a = fs.ReadByte();
		int val_b = fs.ReadByte();
		objDat.CommonStart = val_a + val_b * 256;
		objDat.CommonData = new CommonData[512];
		for (int i = 0; i < 512; i++)
		{
			CommonData comDat = new CommonData();
			int[] raw = new int[11];
			raw[0] = fs.ReadByte();	//Height
			raw[1] = fs.ReadByte();	//Mass, radius, type
			raw[2] = fs.ReadByte();
			raw[3] = fs.ReadByte();	//Flags
			raw[4] = fs.ReadByte();	//Value
			raw[5] = fs.ReadByte();
			raw[6] = fs.ReadByte(); //Quality + ?
			raw[7] = fs.ReadByte();	//Type + ownable + ?
			raw[8] = fs.ReadByte();	//?
			raw[9] = fs.ReadByte();	//?
			raw[10] = fs.ReadByte();//Quality (?) + look + ?

			comDat.RawData = raw;

			comDat.Height = raw[0];
			int b1 = raw[1] + raw[2] * 256;
			comDat.Radius = b1 & 0x07;
			comDat.Type = (b1 >> 3) & 0x01;
			comDat.Mass = b1 >> 4;

			comDat.Flag0 = raw[3] & 0x01;
			comDat.Flag1 = (raw[3] >> 1) & 0x01;
			comDat.Flag2 = (raw[3] >> 2) & 0x01;
			comDat.Flag3 = (raw[3] >> 3) & 0x01;
			comDat.Flag4 = (raw[3] >> 4) & 0x01;
			comDat.Pickable = (raw[3] >> 5) & 0x01;
			comDat.Flag6 = (raw[3] >> 6) & 0x01;
			comDat.Container = (raw[3] >> 7) & 0x01;

			comDat.Value = raw[4] + raw[5] * 256;

			comDat.QualityClass = (raw[6] >> 2) & 0x03;

			comDat.ObjectType = (raw[7] >> 1) & 0x0F;
			comDat.PickupFlag = (raw[7] >> 5) & 0x01;
			comDat.UnkFlag1 = (raw[7] >> 6) & 0x01;
			comDat.Ownable = (raw[7] >> 7) & 0x01;

			comDat.QualityType = raw[10] & 0x0F;
			comDat.LookDescription = (raw[10] >> 4) & 0x01;

			objDat.CommonData[i] = comDat;
		}

		fs.Close();
	}

	public static ObjectData LoadObjectData()
	{
		ObjectData od = new ObjectData();
		LoadCommonData(od);
		FileStream fs = new FileStream(FileExplorer.GetUpperPath(FilePath) + "/OBJECTS.DAT", FileMode.Open);
		int val_a = fs.ReadByte();
		int val_b = fs.ReadByte();
		od.StartValue = val_a + val_b * 256;
		od.WeaponData = ReadWeaponDatas(fs);
		od.ProjectileData = ReadProjectileDatas(fs);
		od.RangedData = ReadRangedDatas(fs);
		od.ArmourData = ReadArmourDatas(fs);
		od.MonsterData = ReadMonsterDatas(fs);
		od.ContainerData = ReadContainerDatas(fs);
		od.LightData = ReadLightDatas(fs);
		od.UnknownData = new int[112];
		for (int i = 0; i < 112; i++)		
			od.UnknownData[i] = fs.ReadByte();		
		fs.Close();
		fs = new FileStream(FileExplorer.GetUpperPath(FileExplorer.GetUpperPath(FilePath)) + "/CRIT/ASSOC.ANM", FileMode.Open);
		od.MonsterSpriteNames = new string[32];
		for (int i = 0; i < 32; i++)
		{
			string s = "";
			for (int j = 0; j < 8; j++)
			{
				s += (char)fs.ReadByte();
			}
			od.MonsterSpriteNames[i] = s;
		}
		for (int i = 0; i < 64; i++)
		{
			od.MonsterData[i].SpriteID = fs.ReadByte();
			od.MonsterData[i].AuxPalette = fs.ReadByte();
		}
		fs.Close();
		return od;
	}

	public static WeaponData[] ReadWeaponDatas(FileStream fs)
	{
		WeaponData[] wds = new WeaponData[16];
		for (int i = 0; i < 16; i++)
			wds[i] = ReadWeaponData(fs, i);
		return wds;
	}

	public static WeaponData ReadWeaponData(FileStream fs, int i)
	{
		WeaponData wd = new WeaponData();
		wd.Slash = fs.ReadByte();
		wd.Bash = fs.ReadByte();
		wd.Stab = fs.ReadByte();
		wd.Unk1 = fs.ReadByte();
		wd.Unk2 = fs.ReadByte();
		wd.Unk3 = fs.ReadByte();
		wd.Skill = fs.ReadByte();
		wd.Durability = fs.ReadByte();
		wd.ObjectID = i;
		return wd;
	}
	public static ProjectileData[] ReadProjectileDatas(FileStream fs)
	{
		ProjectileData[] pds = new ProjectileData[8];
		for (int i = 0; i < 8; i++)
			pds[i] = ReadProjectileData(fs, i);
		return pds;
	}
	public static ProjectileData ReadProjectileData(FileStream fs, int i)
	{
		ProjectileData pd = new ProjectileData();
		int a = fs.ReadByte();
		int b = fs.ReadByte();
		int c = fs.ReadByte();

		int dam = a + b * 256;
		pd.Damage = (dam >> 9) & 0x7F;
		pd.Unk1 = dam & 0x1FF;
		pd.Unk2 = c;
		return pd;
	}
	public static RangedData[] ReadRangedDatas(FileStream fs)
	{
		RangedData[] rds = new RangedData[8];
		for (int i = 0; i < 8; i++)
			rds[i] = ReadRangedData(fs, i);
		return rds;
	}
	public static RangedData ReadRangedData(FileStream fs, int i)
	{
		RangedData rd = new RangedData();
		int a = fs.ReadByte();
		int b = fs.ReadByte();
		int c = fs.ReadByte();

		rd.Ammo = a + b * 256;
		rd.Unk1 = c;
		return rd;
	}
	public static ArmourData[] ReadArmourDatas(FileStream fs)
	{
		ArmourData[] ads = new ArmourData[32];
		for (int i = 0; i < 32; i++)
			ads[i] = ReadArmourData(fs, i);
		return ads;
	}
	public static ArmourData ReadArmourData(FileStream fs, int i)
	{
		ArmourData ad = new ArmourData();
		ad.Protection = fs.ReadByte();
		ad.Durability = fs.ReadByte();
		ad.Unk1 = fs.ReadByte();
		ad.Type = fs.ReadByte();
		return ad;
	}
	public static ContainerData[] ReadContainerDatas(FileStream fs)
	{
		ContainerData[] cds = new ContainerData[16];
		for (int i = 0; i < 16; i++)
			cds[i] = ReadContainerData(fs, i);
		return cds;
	}
	public static ContainerData ReadContainerData(FileStream fs, int i)
	{
		ContainerData cd = new ContainerData();
		cd.Capacity = fs.ReadByte();
		cd.Type = fs.ReadByte();
		cd.Slots = fs.ReadByte();
		return cd;
	}
	public static LightData[] ReadLightDatas(FileStream fs)
	{
		LightData[] lds = new LightData[8];
		for (int i = 0; i < 8; i++)
			lds[i] = ReadLightData(fs, i);
		return lds;
	}
	public static LightData ReadLightData(FileStream fs, int i)
	{
		LightData ld = new LightData();
		ld.Duration = fs.ReadByte();
		ld.Brightness = fs.ReadByte();
		return ld;
	}
	public static MonsterData[] ReadMonsterDatas(FileStream fs)
	{
		MonsterData[] mds = new MonsterData[64];
		for (int i = 0; i < 64; i++)
			mds[i] = ReadMonsterData(fs, i);
		return mds;
	}
	public static MonsterData ReadMonsterData(FileStream fs, int i)
	{
		MonsterData md = new MonsterData();
		md.Level = fs.ReadByte();
		md.Unk0_1 = fs.ReadByte();
		md.Unk0_2 = fs.ReadByte();
		md.Unk0_3 = fs.ReadByte();
		md.Health = fs.ReadByte() + fs.ReadByte() * 256;
		md.Attack = fs.ReadByte();
		md.Unk1 = fs.ReadByte();
		int rem = fs.ReadByte();
		md.Remains = (rem & 0xF0) >> 4;
		md.HitDecal = rem & 0x0F;
		md.OwnerType = fs.ReadByte();
		md.Passiveness = fs.ReadByte();
		md.Unk2 = fs.ReadByte();
		md.Speed = fs.ReadByte();
		md.Unk3 = fs.ReadByte() + fs.ReadByte() * 256;
		md.Poison = fs.ReadByte();
		md.MonsterType = fs.ReadByte();
		md.EquipmentDamage = fs.ReadByte();
		md.Unk4 = fs.ReadByte();
		md.Attack1Value = fs.ReadByte();
		md.Attack1Damage = fs.ReadByte();
		md.Attack1Chance = fs.ReadByte();
		md.Attack2Value = fs.ReadByte();
		md.Attack2Damage = fs.ReadByte();
		md.Attack2Chance = fs.ReadByte();
		md.Attack3Value = fs.ReadByte();
		md.Attack3Damage = fs.ReadByte();
		md.Attack3Chance = fs.ReadByte();   //28

		//Debug.LogFormat("Monster {0} has remains {1} and hit type {2}", StaticObject.GetName(i + 64), md.Remains, md.HitDecal);

		md.Inv_Unk1 = fs.ReadByte() + fs.ReadByte() * 256 + fs.ReadByte() * 256 * 256 + fs.ReadByte() * 256 * 256 * 256; 

		int inv1 = fs.ReadByte();
		int inv2 = fs.ReadByte();
		int inv3 = fs.ReadByte() + fs.ReadByte() * 256;
		int inv4 = fs.ReadByte() + fs.ReadByte() * 256; //38
		md.Inventory = new int[4];
		md.InventoryInfo = new int[4];
		if ((inv1 & 0x01) == 1)
		{
			md.Inventory[0] = inv1 >> 1;
			md.InventoryInfo[0] = 1;
		}
		if ((inv2 & 0x01) == 1)
		{
			md.Inventory[1] = inv2 >> 1;
			md.InventoryInfo[1] = 1;
		}
		if (inv3 != 0)
		{
			md.Inventory[2] = inv3 >> 4;
			md.InventoryInfo[2] = inv3 & 0x0F;
			//Debug.LogFormat("Monster <b>{0}</b> has inventory <b>{1}</b> and chance ? <b>{2}</b>", StaticObject.GetMonsterName(i), StaticObject.GetName(md.Inventory[2]), md.InventoryInfo[2]);
		}
		if (inv4 != 0)
		{
			md.Inventory[3] = inv4 >> 4;
			md.InventoryInfo[3] = inv4 & 0x0F;
			//Debug.LogFormat("Monster <b>{0}</b> has inventory <b>{1}</b> and chance ? <b>{2}</b>", StaticObject.GetMonsterName(i), StaticObject.GetName(md.Inventory[3]), md.InventoryInfo[3]);
		}

		md.Inv_Unk2 = fs.ReadByte() + fs.ReadByte() * 256;


		md.Experience = fs.ReadByte() + fs.ReadByte() * 256;
		md.Unk5 = fs.ReadByte();
		md.Unk6 = fs.ReadByte();
		md.Unk7 = fs.ReadByte();
		md.Unk8 = fs.ReadByte();
		md.Unk9 = fs.ReadByte();
		md.Unk10 = fs.ReadByte();

		return md;
	}

	#endregion

	public static object ReadFromJson(string path)
	{
		try
		{
			JsonSerializer js = new JsonSerializer();
			StreamReader sr = new StreamReader(path);
			JsonReader jr = new JsonTextReader(sr);
			object obj = js.Deserialize(jr);
			sr.Close();
			jr.Close();
			return obj;
		}
		catch (Exception e)
		{
			Debug.LogWarning($"Failed to read from json file : {path}\nexception : {e}\n{e.StackTrace}");
			return default;
		}
	}

	public static string ToHex(int value)
	{
		return string.Format("{0:X}", value);
	}

	public static string ToHex(long value)
	{
		return string.Format("{0:X}", value);
	}

	public static string ToBin(int value)
	{
		return Convert.ToString(value, 2);
	}
}
