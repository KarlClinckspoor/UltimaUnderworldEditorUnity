using System;
using UltimateEditor.Games;
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
		FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		int test = fs.ReadByte();
		bool valid = true;
		//if (test == 135)
		//	valid = true;
		fs.Close();
		return valid;
	}


	#endregion

	#region Level Data

	//TODO : To powinno brać string : ścieżkę do LEV.ARK.
	public static List<LevelData> LoadLevels()
	{
		if (!uiManager)
			return null;
		// Both games go through the archive layer now (Data/LevArk.cs): UW2's archive format and
		// compression, and a byte-exact re-save of unedited levels in both.
		return LevArk.Load(GameSession.LevelFile(FilePath));
	}


	public static StaticObject[] ReadStaticObjects(Stream fs, MapTile[,] mapData, int levelOffset, int level, int[] mobList, int mobCount)
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
							if (staticObjects[nextIndex])
							{
								// already read: the chain loops (or two chains join); the game
								// would walk it forever, so it is cut here
								Debug.LogWarningFormat("Level {0}, tile {1},{2}: object {3} links back to object {4}, already read; the link is cut", level, x, y, current.CurrentAdress, nextIndex);
								current.NextAdress = 0;
								break;
							}
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

	private static StaticObject ReadObject(Stream fs, int index, int offset, StaticObject so, StaticObject[] staticObjects, MapTile tile, int x, int y, int level, int[] mobList, int mobCount)
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

	private static void ReadContainer(Stream fs, int offset, StaticObject container, StaticObject[] staticObjects, MapTile tile, int x, int y, int level, int[] mobList, int mobCount)
	{
		int invIndex = 0;
		StaticObject inventory = null;
		StaticObject current = container;
		invIndex = container.Special;
		while (invIndex > 0)
		{
			if (staticObjects[invIndex])
			{
				// already read: the contents loop back (or are shared); cut, as above
				Debug.LogWarningFormat("Level {0}: the contents of object {1} link back to object {2}, already read; the link is cut", level, current.CurrentAdress, invIndex);
				if (current == container)
					container.Special = 0;
				else
					current.NextAdress = 0;
				break;
			}
			inventory = ReadObject(fs, invIndex, offset, inventory, staticObjects, tile, x, y, level, mobList, mobCount);
			inventory.PrevAdress = current.CurrentAdress;
			if (!inventory.IsQuantity)
				ReadContainer(fs, offset, inventory, staticObjects, tile, x, y, level, mobList, mobCount);
			invIndex = inventory.NextAdress;
			current = inventory;
		}
	}

	public static int GetNumberOfBlocks(Stream fs)
	{
		return fs.ReadByte() + fs.ReadByte() * 256;
	}

	public static int[] GetLevelOffsets(Stream fs, int levelCount)
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



	public static int GetLevelIndex(int level, Stream fs)
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


	public static MapTile[,] ReadMapData(int level, int offset, Stream fs)
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

	public static StaticObject ReadStaticObjectFromIndex(int level, int index, int offset, Stream fs, bool log = false)
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

	public static MobileObject ReadMobileObjectFromIndex(int level, int index, int offset, Stream fs)
	{
		//int start = GetLevelIndex(level, fs) + 16384;
		int start = offset + 16384;
		fs.Position = start + index * 27;

		MobileObject obj = new MobileObject();
		obj.CurrentAdress = index;
		obj.CurrentLevel = level;
		var b = new byte[ObjectRecord.MobileSize];
		fs.Read(b, 0, b.Length);
		ObjectRecord.Read(obj, b, 0, true);

		return obj;
	}

	/// <summary>Reads an object's 8 static bytes (ObjectRecord, the one field table).</summary>
	private static void ReadStaticObject(Stream fs, StaticObject obj, bool log = false)
	{
		var b = new byte[ObjectRecord.StaticSize];
		fs.Read(b, 0, b.Length);
		ObjectRecord.Read(obj, b, 0, true);
		if (log)
			Debug.LogFormat("[{0}]", System.BitConverter.ToString(b));
	}

	public static int[] ReadFreeObjectIndexes(int level, int offset, Stream fs)
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

	public static int[] ReadActiveMobs(int level, int offset, Stream fs)
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

	public static int ReadFreeListStart(int level, int offset, ListType whichList, Stream fs)
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


	public static int[] ReadFloorTextureMappings(int level, int offset, Stream fs)
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

	public static int[] ReadWallTextureMappings(int level, int offset, Stream fs)
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

	public static int[] ReadDoorTextureMappings(int level, int offset, Stream fs)
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

	public static AnimationOverlay[] ReadAnimationOverlays(int level, int offset, Stream fs)
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
			// Anim_Load (reference obj/EFFECT.C): the list ends at the first zero object index;
			// entries after it are stale and the game never runs them. The shipped file has such
			// stale entries (level 4's list is empty but holds 12), so stop here like the game.
			if (adress == 0)
				break;
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

		string filePath = GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/pals.dat");
		FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
		
		// The game decides the texture files (Extensions/Core/Games/GameProfile.cs).
		var game = UltimateEditor.Games.GameProfile.For(LevArk.DetectFor(FilePath));
		LevArk.Game = game;
		int startX, startY;
		MapCreator.ExeStart = UltimateEditor.Games.StartPosition.Load(FileExplorer.GetUpperPath(FilePath), game.Kind, out startX, out startY)
			? new Vector2Int(startX, startY) : (Vector2Int?)null;
		TextureContainer floors, walls;
		if (game.Kind == UltimateEditor.Games.GameKind.UW2)
		{
			// UW2: one list, T64.TR (64x64), for walls and floors. The floor copies are scaled
			// to 32x32 so everything that shows floors at UW1's size keeps working.
			int tCount = 0, tFirst = 0;
			int[] t64o = ReadGraphicOffsets("T64.TR", ref tCount, ref tFirst);
			List<int[]> t64raw = ReadRawTextures("T64.TR", t64o, 64, 64);
			List<Texture2D> t64tex = new List<Texture2D>();
			List<Texture2D> t32tex = new List<Texture2D>();
			for (int i = 0; i < t64raw.Count; i++)
			{
				t64tex.Add(ConvertToTexture("Tex64_" + i, t64raw[i], 64, 64, pals[0], true));
				t32tex.Add(ConvertToTexture("Floor32_" + i, Downscale(t64raw[i], 64, 64), 32, 32, pals[0], true));
			}
			Uw2T64.Remember(GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/T64.TR"), t64o, t64tex, t32tex, pals[0]);
			floors = new TextureContainer(t32tex, tFirst, 0, new AdditionalTextureInfo(false, 32, 32, -1, -1));
			walls = new TextureContainer(t64tex, tFirst, 0, new AdditionalTextureInfo(false, 64, 64, -1, -1));
		}
		else
		{
		int fCount = 0, fFirst = 0;
		int[] f32o = ReadGraphicOffsets("F32.TR", ref fCount, ref fFirst);
		List<int[]> f32raw = ReadRawTextures("F32.TR", f32o, 32, 32);
		List<Texture2D> f32tex = new List<Texture2D>();
		for (int i = 0; i < fCount; i++)
			f32tex.Add(ConvertToTexture("Floor32_" + i, f32raw[i], 32, 32, pals[0], true));
		floors = new TextureContainer(f32tex, fFirst, 0, new AdditionalTextureInfo(false, 32, 32, -1, -1));

		int wCount = 0, wFirst = 0;
		int[] w64o = ReadGraphicOffsets("W64.TR", ref wCount, ref wFirst);
		List<int[]> w64raw = ReadRawTextures("W64.TR", w64o, 64, 64);
		List<Texture2D> w64tex = new List<Texture2D>();
		for (int i = 0; i < wCount; i++)
			w64tex.Add(ConvertToTexture("Wall64_" + i, w64raw[i], 64, 64, pals[0], true));
		walls = new TextureContainer(w64tex, wFirst, 0, new AdditionalTextureInfo(false, 64, 64, -1, -1));
		}
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
		// UW1's heads are 34x34, UW2's 64x70: take each picture's size from its header
		var npcSizes = new Dictionary<int, Vector2Int>();
		List<int[]> npcRaw = ReadRawTextures("CHARHEAD.GR", npcOffsets, 0, 0, 5, true, npcSizes);
		//Debug.LogFormat("npcOffset count : {0}, pCount : {1}, npcRaw.Len : {2}", npcOffsets.Length, pCount, npcRaw.Count);
		List<Texture2D> npcTex = new List<Texture2D>();
		for (int i = 0; i < npcRaw.Count; i++)
			npcTex.Add(ConvertToTexture("NPC_Head_" + i, npcRaw[i], npcSizes[i].x, npcSizes[i].y, pals[0], true));
		TextureContainer npcheads = new TextureContainer(npcTex, pFirst, 5, new AdditionalTextureInfo(true, 34, 34, 132, 4));

		int iCount = 0, iFirst = 0;
		int[] itemOffsets = ReadGraphicOffsets("OBJECTS.GR", ref iCount, ref iFirst);
		fs = new FileStream(GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/OBJECTS.GR"), FileMode.Open, FileAccess.Read, FileShare.Read);
		List<Texture2D> itemsTex = new List<Texture2D>();
		List<int[]> rawTexs = new List<int[]>();
		List<int> objAuxPals = new List<int>();

		//string datadump = "";

		// UW1's file lists 464 pictures, the last 3 past its end; UW2's has all 464
		int iUsed = 0;
		while (iUsed < iCount && itemOffsets[iUsed] < fs.Length) iUsed++;
		for (int i = 0; i < iUsed; i++)
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

		{
			// Both games' pictures are written back by patching their files (Data/GrPictures.cs,
			// Data/Uw1TrFiles.cs, Data/Uw2T64.cs), so the files are remembered as read.
			// After fs.Close(): on Windows, OBJECTS.GR cannot be read again while that stream
			// (opened for writing too) holds it.
			string dataDir = FileExplorer.GetUpperPath(FilePath);
			GrPictures.Reset();
			if (game.Kind == UltimateEditor.Games.GameKind.UW1)
			{
				// UW1's 8-bit pictures go through the editor's palette table, as its old writers did
				GrPictures.ColorToIndex = DataWriter.PaletteIndex;
				Uw1TrFiles.Reset();
				Uw1TrFiles.Remember(dataDir, walls.Textures, floors.Textures);
			}
			GrPictures.Remember("DOORS.GR", GameFiles.Resolve(dataDir + "/DOORS.GR"), doortex, pals[0]);
			GrPictures.Remember("TMFLAT.GR", GameFiles.Resolve(dataDir + "/TMFLAT.GR"), levertex, pals[0]);
			GrPictures.Remember("TMOBJ.GR", GameFiles.Resolve(dataDir + "/TMOBJ.GR"), othertex, pals[0]);
			GrPictures.Remember("GENHEAD.GR", GameFiles.Resolve(dataDir + "/GENHEAD.GR"), portrTex, pals[0]);
			GrPictures.Remember("CHARHEAD.GR", GameFiles.Resolve(dataDir + "/CHARHEAD.GR"), npcTex, pals[0]);
			GrPictures.Remember("OBJECTS.GR", GameFiles.Resolve(dataDir + "/OBJECTS.GR"), itemsTex, pals[0]);
		}
		fs = new FileStream(GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/TERRAIN.DAT"), FileMode.Open, FileAccess.Read, FileShare.Read);
		int[] wallDat = new int[256];
		int[] florDat;
		for (int i = 0; i < 256; i++)		
			wallDat[i] = fs.ReadByte() + fs.ReadByte() * 256;
		if (game.Kind == UltimateEditor.Games.GameKind.UW2)
			florDat = wallDat;   // UW2: one word per T64.TR texture, walls and floors alike (TEXTMAPS.C Load_Terrains)
		else
		{
			florDat = new int[256];
			for (int i = 0; i < 256; i++)
				florDat[i] = fs.ReadByte() + fs.ReadByte() * 256;
		}
		
		fs.Close();

		texData = new TextureData(pals, auxPalsRaw, auxPals, floors, walls, doors, levers, other, genheads, npcheads, items, wallDat, florDat);
		return texData;
	}

	public static int[] Downscale(int[] raw, int width, int height) { return GraphicsCodec.Downscale(raw, width, height); }

	public static int[] ReadPallette(Stream fs, int index) { return GraphicsCodec.ReadPallette(fs, index); }

	public static List<int[]> ReadAuxPalettes() { return GraphicsCodec.ReadAuxPalettes(GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/ALLPALS.dat")); }

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

	public static int ReadPaletteMapping(Stream fs, int r, int g, int b) { return GraphicsCodec.ReadPaletteMapping(fs, r, g, b); }

	public static int[] ReadGraphicOffsets(string fileName, ref int count, ref int first) { return GraphicsCodec.ReadGraphicOffsets(GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/" + fileName), ref count, ref first); }

	public static int[] Read4BitGraphic(Stream fs, long offset, int startingOffset, int index, AdditionalTextureInfo ati) { return GraphicsCodec.Read4BitGraphic(fs, offset, startingOffset, index, ati); }

	public static int[] Read4BitGraphic(Stream fs, long offset, int width, int height, int count) { return GraphicsCodec.Read4BitGraphic(fs, offset, width, height, count); }

	public static int[] Convert4BitToRawTex(int[] nibbles, int width, int height) { return GraphicsCodec.Convert4BitToRawTex(nibbles, width, height); }

	public static List<int[]> ReadRawTextures(string fileName, int[] offsets, int width, int height, int startingOffset = 0, bool specificSize = false, Dictionary<int, Vector2Int> dict = null)
	{
		// The Formats assembly has no Vector2Int: it reports the sizes as (width, height) pairs
		var sizes = dict == null ? null : new Dictionary<int, GraphicsCodec.Size>();
		List<int[]> r = GraphicsCodec.ReadRawTextures(GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/" + fileName), offsets, width, height, startingOffset, specificSize, sizes);
		if (sizes != null) foreach (var kv in sizes) dict[kv.Key] = new Vector2Int(kv.Value.x, kv.Value.y);
		return r;
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
		for (int i = 0; i < level.FloorTextures.Length; i++)   // 10 in UW1, 16 in UW2
			list.Add(MapCreator.GetFloorTextureFromIndex(i, level.Level));
		return list;
	}
	public static List<Texture2D> GetLevelWalls(LevelData level)
	{
		List<Texture2D> list = new List<Texture2D>();
		for (int i = 0; i < level.WallTextures.Length; i++)    // 48 in UW1, 64 in UW2
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
		else if (texType == TextureType.NPCHead && LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW2)
		{
			// UW2's character heads (CHARHEAD.GR) are 64x70; its generic heads are UW1's size
			width = 64;
			height = 70;
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
		// nearest-neighbour scaling (a 34x34 head becomes UW2's 64x70; same size copies as before)
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				newTex.SetPixel(x, y, oldTex.GetPixel(x * oldTex.width / width, y * oldTex.height / height));
			}
		}
		newTex.Apply();
		return newTex;
	}

	#endregion

	#region Strings

	public static StringData LoadStrings()
	{
		FileStream fs = new FileStream(GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/STRINGS.PAK"), FileMode.Open, FileAccess.Read, FileShare.Read);
		Tuple<HuffmanNode[], long> pair = ReadHuffmanNodes(fs);
		Tuple<List<StringBlock>, Dictionary<int, int>> blockPair = ReadStringBlocks(fs, pair.Item2);
		for (int i = 0; i < blockPair.Item1.Count; i++)
		{
			StringBlock block = blockPair.Item1[i];
			ReadStrings(fs, block, pair.Item1);
		}
		fs.Close();
		StringData strData = new StringData(pair.Item1, blockPair.Item1, blockPair.Item2);
		HuffmanNode.SetTree(pair.Item1);   // the tree typed strings are checked against
		strData.TakeSnapshot();
		return strData;
	}

	public static Tuple<HuffmanNode[], long> ReadHuffmanNodes(Stream fs) { return StringsPak.ReadHuffmanNodes(fs); }

	public static Tuple<List<StringBlock>, Dictionary<int, int>> ReadStringBlocks(Stream fs, long startOffset) { return StringsPak.ReadStringBlocks(fs, startOffset); }

	public static List<string> ReadStrings(Stream fs, StringBlock block, HuffmanNode[] nodes, bool debug = false) { return StringsPak.ReadStrings(fs, block, nodes, debug); }

	public static char GetChar(Stream fs, HuffmanNode[] nodes, ref int raw, ref int bit, bool debug = false) { return StringsPak.GetChar(fs, nodes, ref raw, ref bit, debug); }

	#endregion

	#region Conversations

	public static ConversationData LoadConversations()
	{
		// Data/CnvArk.cs gives UW1's archive layout for either game (UW2's blocks decompressed).
		var image = CnvArk.LoadAsUw1Image(GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/CNV.ARK"));
		Stream fs = new MemoryStream(image, false);
		Conversation[] convs = LoadConversations(fs);
		ConversationData convData = new ConversationData(convs, ReadPrivateVariables());
		if (!Directory.Exists(Application.dataPath + "/Conversations"))
			Directory.CreateDirectory(Application.dataPath + "/Conversations");
		convData.DefaultFunctions = LevArk.Game.Kind == GameKind.UW2 ? Uw2ImportTemplate(convs) : ReadDefaultConversationFunctions();
		return convData;
	}

	/// <summary>
	/// UW2's import table for new conversations, from the game's own scripts: every shipped
	/// UW2 conversation imports the same 94 names with the same function ids; only the
	/// variables' addresses shift with the private globals. The compiler places the
	/// imported variables from address 1 (as functions.dat does for UW1), so the table is
	/// taken from a script with play_hunger at 1, or shifted to that.
	/// </summary>
	public static ConversationFunction[] Uw2ImportTemplate(Conversation[] convs)
	{
		Conversation best = null; int bestShift = int.MaxValue;
		foreach (var c in convs)
		{
			if (c == null || c.Functions == null) continue;
			int hunger = -1;
			foreach (var f in c.Functions) if (f.Name == "play_hunger") hunger = f.Id_Adress;
			if (hunger < 0) continue;
			int shift = Math.Abs(hunger - 1);
			if (best == null || shift < bestShift || (shift == bestShift && c.Functions.Length > best.Functions.Length)) { best = c; bestShift = shift; }
		}
		if (best == null) return null;   // no UW2 script to copy from; the compiler refuses (MapFunctionId)
		int delta = 0;
		foreach (var f in best.Functions) if (f.Name == "play_hunger") delta = 1 - f.Id_Adress;
		var r = new ConversationFunction[best.Functions.Length];
		for (int i = 0; i < r.Length; i++)
		{
			var f = best.Functions[i];
			r[i] = new ConversationFunction { NameLength = f.NameLength, Name = f.Name, Id_Adress = f.Type == ConversationFunction.Variable ? f.Id_Adress + delta : f.Id_Adress, Unk04 = f.Unk04, Type = f.Type, Return = f.Return };
		}
		return r;
	}

	public static Conversation[] LoadConversations(Stream fs)
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

		// CnvArk.Merge removes a loaded slot's block when the editor nulls the slot
		CnvArk.Loaded.Clear();
		for (int i = 0; i < convs.Length; i++) if (convs[i] != null) CnvArk.Loaded.Add(i);
		return convs;
	}

	public static Conversation LoadConversation(Stream fs, int offset, ConversationState state)
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
		FileInfo fi = new FileInfo(GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/BABGLOBS.DAT"));
		FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
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
		// read whole and closed at once: the stream used to stay open (read-write, unshared),
		// which could block opening the game a second time
		string filePath = Application.streamingAssetsPath + "/functions.dat";
		using (var stream = new MemoryStream(File.ReadAllBytes(filePath)))
		{
			BinaryFormatter bf = new BinaryFormatter();
			return (ConversationFunction[])bf.Deserialize(stream);
		}
	}

	#endregion

	#region Object Data



	public static void LoadCommonData(ObjectData objDat) { ObjectDataCodec.LoadCommonData(objDat, GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/COMOBJ.DAT")); }

	public static ObjectData LoadObjectData()
	{
		ObjectData od = new ObjectData();
		LoadCommonData(od);
		ObjectDataCodec.ReadObjectsDat(od, GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/OBJECTS.DAT"));
		ObjectDataCodec.ReadMonsterSprites(od, LevArk.Game.Kind, FileExplorer.GetUpperPath(FileExplorer.GetUpperPath(FilePath)));
		return od;
	}

	public static WeaponData[] ReadWeaponDatas(Stream fs) { return ObjectDataCodec.ReadWeaponDatas(fs); }

	public static WeaponData ReadWeaponData(Stream fs, int i) { return ObjectDataCodec.ReadWeaponData(fs, i); }
	public static ProjectileData[] ReadProjectileDatas(Stream fs) { return ObjectDataCodec.ReadProjectileDatas(fs); }
	public static ProjectileData ReadProjectileData(Stream fs, int i) { return ObjectDataCodec.ReadProjectileData(fs, i); }
	public static RangedData[] ReadRangedDatas(Stream fs) { return ObjectDataCodec.ReadRangedDatas(fs); }
	public static RangedData ReadRangedData(Stream fs, int i) { return ObjectDataCodec.ReadRangedData(fs, i); }
	public static ArmourData[] ReadArmourDatas(Stream fs) { return ObjectDataCodec.ReadArmourDatas(fs); }
	public static ArmourData ReadArmourData(Stream fs, int i) { return ObjectDataCodec.ReadArmourData(fs, i); }
	public static ContainerData[] ReadContainerDatas(Stream fs) { return ObjectDataCodec.ReadContainerDatas(fs); }
	public static ContainerData ReadContainerData(Stream fs, int i) { return ObjectDataCodec.ReadContainerData(fs, i); }
	public static LightData[] ReadLightDatas(Stream fs) { return ObjectDataCodec.ReadLightDatas(fs); }
	public static LightData ReadLightData(Stream fs, int i) { return ObjectDataCodec.ReadLightData(fs, i); }
	public static MonsterData[] ReadMonsterDatas(Stream fs) { return ObjectDataCodec.ReadMonsterDatas(fs); }
	public static MonsterData ReadMonsterData(Stream fs, int i) { return ObjectDataCodec.ReadMonsterData(fs, i); }

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
