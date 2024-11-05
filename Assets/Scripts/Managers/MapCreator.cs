using System;
using System.Collections.Generic;
using UnityEngine;

public enum TileType
{
	Solid,
	Open,
	OpenSE,
	OpenSW,
	OpenNE,
	OpenNW,
	SlopeUpN,
	SlopeUpS,
	SlopeUpE,
	SlopeUpW
}

public class MapCreator : MonoBehaviour {

	public GameObject MapTilePrefab;
	public GameObject StaticObjectPrefab;
	public static GameObject StaticObjectPrefabSt;

	public Sprite[] MapTileSprites;
	public static Sprite[] Sprites;

	public static float StepZ = 0.1f;
	public static float CeilingZ = 1.6f;
	public static Vector3 TileOffset = new Vector3(0.3f, 0.3f, 0);
	public static float TileSize = 0.3f;

	public Material TileMaterial;
	public Material SpriteMaterial;

	private static Material tileMaterial;
	private static Material spriteMaterial;

	public static List<LevelData> LevelData { get; private set; }
	public static TextureData TextureData { get; private set; }
	public static StringData StringData { get; private set; }
	public static ConversationData ConversationData { get; private set; }
	public static ObjectData ObjectData { get; private set; }
	public static GameData GameData { get; private set; }
	public static GameObject StartObject { get; private set; }

	public static Vector2Int GameStart = new Vector2Int(32, 2);

	public bool IsInitialized { get; private set; }

	public static Dictionary<MapTile, GameObject> MapTileToGO = new Dictionary<MapTile, GameObject>();
	public static Dictionary<StaticObject, GameObject> ObjectToGO = new Dictionary<StaticObject, GameObject>();

	#region Init, creating and clearing

	public void Initialize(List<LevelData> levData, TextureData texData, StringData strData, ConversationData convData, GameData gameData, ObjectData objData)
	{
		LevelData = levData;
		TextureData = texData;
		StringData = strData;
		ConversationData = convData;
		GameData = gameData;
		ObjectData = objData;

		for(int i = 0; i < LevelData.Count; i++)
		{
			LevelData level = LevelData[i];
			foreach (var anim in level.AnimationOverlays)
			{
				if(anim)
				{
					if(level.Objects[anim.Adress] != null)
						level.Objects[anim.Adress].Animation = anim;
				}
			}
		}
		StaticObjectPrefabSt = StaticObjectPrefab;
		tileMaterial = TileMaterial;
		spriteMaterial = SpriteMaterial;
		GameStart = new Vector2Int(32, 2);
		Sprites = Resources.LoadAll<Sprite>("ItemSheet");
		IsInitialized = true;
	}

	public void ClearAllData()
	{
		for (int i = 0; i < transform.childCount; i++)
			Destroy(transform.GetChild(i).gameObject);
		LevelData = null;
		MapTileToGO = new Dictionary<MapTile, GameObject>();

		IsInitialized = false;
	}
	public void CreateEmptyLevels(int count)
	{
		for (int i = 0; i < count; i++)
		{
			CreateEmptyLevel(i + 1);
		}
	}
	public void CreateEmptyLevel(int index)
	{
		index--;		//You can then say that you want to create empty level 1 - that is index 0.
		if (LevelData == null)
			LevelData = new List<LevelData>();
		if (index > LevelData.Count)
			index = LevelData.Count;

		MapTile[,] newLevelTiles = new MapTile[64, 64];
		Debug.LogFormat("Creating new level {0} at index {1}", index + 1, index);
		for (int x = 0; x < 64; x++)
		{
			for (int y = 0; y < 64; y++)
			{
				if (index == 0 && x == GameStart.x && y == GameStart.y)
					newLevelTiles[x, y] = new MapTile(TileType.Open, index + 1, x, y, 12, 0, 0);
				else
					newLevelTiles[x, y] = new MapTile(TileType.Solid, index + 1, x, y, 0, 0, 0);
			}
		}
		StaticObject[] newLevelObjects = new StaticObject[1024];
		int[] newLevelFloors = new int[10];
		int[] newLevelWalls = new int[48];
		int[] newLevelDoors = new int[6];
		int[] newLevelMemory = new int[1022];
		for (int i = 0; i < newLevelMemory.Length; i++)
			newLevelMemory[i] = i + 2;
		int[] newLevelMobs = new int[260];
		AnimationOverlay[] newLevelAnims = new AnimationOverlay[64];

		LevelData newLevel = new LevelData(index + 1, newLevelTiles, newLevelObjects, newLevelFloors, newLevelWalls, newLevelDoors, newLevelMemory, newLevelMobs, 253, 767, 0, newLevelAnims);
		if (LevelData.Count > 0)
		{
			List<LevelData> newLevelData = new List<LevelData>();
			for (int i = 0; i < LevelData.Count + 1; i++)
			{
				if(i == index)
					newLevelData.Add(newLevel);
				else if(i > index)
					newLevelData.Add(LevelData[i - 1]);
				else if(i < index)
					newLevelData.Add(LevelData[i]);
			}
			LevelData = newLevelData;
		}
		else
			LevelData.Add(newLevel);
	}

	public void ClearLevels()
	{
		LevelData.Clear();
		CreateEmptyLevels(9);
	}
	public void ClearConversations()
	{
		for (int i = 0; i < ConversationData.Conversations.Length; i++)
		{
			ConversationData.Conversations[i] = null;
		}
	}

	public GameObject[] CreateLevels()
	{
		if (LevelData == null)
			return null;

		GameObject[] levelGOs = new GameObject[LevelData.Count];

		for (int i = 1; i < LevelData.Count + 1; i++)
		{
			GameObject levelGO = new GameObject("Level_" + i);
			levelGO.transform.SetParent(transform);
			CreateLevel(i, levelGO.transform);
			levelGOs[i - 1] = levelGO;
		}
		return levelGOs;
	}

	public GameObject SpawnStartGO()
	{
		return SpawnStartGO(transform, GameStart);
	}

	private void CreateLevel(int level, Transform parentLevel)
	{
		CreateMapTiles(level, parentLevel);
		CreateStaticObjects(level, parentLevel);
		parentLevel.gameObject.SetActive(false);
	}

	#endregion

	#region Tiles

	private void CreateMapTiles(int level, Transform parentLevel)
	{
		GameObject tilesObj = new GameObject("LevelTiles");
		tilesObj.transform.SetParent(parentLevel);
		for (int x = 0; x < 64; x++)
		{
			for (int y = 0; y < 64; y++)
			{
				GameObject tileObj = CreateTile(level, LevelData[level - 1].MapTiles[x, y], tilesObj.transform, TileSize, TileOffset);
				MapTileScript mts = tileObj.GetComponent<MapTileScript>();
				if (x > 0)
					mts.WTile = LevelData[level - 1].MapTiles[x - 1, y];
				if (x < 63)
					mts.ETile = LevelData[level - 1].MapTiles[x + 1, y];
				if (y > 0)
					mts.STile = LevelData[level - 1].MapTiles[x, y - 1];
				if (y < 63)
					mts.NTile = LevelData[level - 1].MapTiles[x, y + 1];
			}
		}
	}

	private GameObject CreateTile(int level, MapTile tile, Transform parent, float size, Vector3 offset)
	{
		GameObject tileObject = CreateEmptyTile(tile, parent, "Tile_", offset, size);
		CreateFloor(level, tile, tileObject.transform, size, offset);
		CreateWalls(level, tile, tileObject.transform, size, offset);

		return tileObject;
	}

	public GameObject CreateFloor(int level, MapTile tile, Transform parent, float size, Vector3 offset, GameObject floor = null, bool reset = false)
	{
		if(floor == null)
			floor = new GameObject("Floor");
		floor.layer = 10;
		floor.transform.SetParent(parent);
		floor.transform.localPosition = Vector3.zero;
		MeshFilter mesh = floor.GetComponent<MeshFilter>();
		if (!mesh)
			mesh = floor.AddComponent<MeshFilter>();
		float height = GetTileHeight(tile);
		if (tile.TileType == TileType.Open || tile.TileType == TileType.SlopeUpE || tile.TileType == TileType.SlopeUpW || tile.TileType == TileType.SlopeUpN || tile.TileType == TileType.SlopeUpS)
			mesh.mesh = CreateSquareFloor(size, height, tile);
		else if (tile.TileType == TileType.Solid)
			mesh.mesh = CreateSquareFloor(size, 0, tile);
		else
			mesh.mesh = CreateFloorCorner(size, height, tile.TileType);
		MeshRenderer mr = floor.GetComponent<MeshRenderer>();
		if (!mr)
			mr = floor.AddComponent<MeshRenderer>();
		mr.material = TileMaterial;
		mr.material.SetColor("_Color", GetHeightColor(tile));
		if (tile.TileType == TileType.Solid)
			mr.material.SetFloat("_Transparency", 0);
		mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
		mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
		Texture2D tex = GetFloorTextureFromIndex(tile.FloorTexture, level);
		MeshCollider col = floor.GetComponent<MeshCollider>();
		if (!col)
			col = floor.AddComponent<MeshCollider>();
		if (reset)
			col.sharedMesh = mesh.mesh;
		mr.material.mainTexture = tex;
		parent.GetComponent<MapTileScript>().FloorObject = floor;
		return floor;
	}


	public GameObject CreateWalls(int level, MapTile tile, Transform parent, float size, Vector3 offset, GameObject wall = null)
	{
		//Debug.Log("CreateWalls");
		if (tile.TileType == TileType.Solid)
			return null;
		if (wall == null)
			wall = new GameObject("Wall");

		float height = GetTileHeight(tile);
		Mesh walls = CreateWalls(level, size, height, tile);		
		wall.transform.SetParent(parent);
		wall.transform.localPosition = Vector3.zero;
		
		MeshFilter mesh = wall.GetComponent<MeshFilter>();
		if (!mesh)
			mesh = wall.AddComponent<MeshFilter>();
		mesh.mesh = walls;
		MeshRenderer mr = wall.GetComponent<MeshRenderer>();
		if (!mr)
			mr = wall.AddComponent<MeshRenderer>();
		mr.material = TileMaterial;
		mr.material.SetColor("_Color", GetHeightColor(tile));
		mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
		mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

		Texture2D tex = GetWallTextureFromIndex(tile.WallTexture, level);
		mr.material.mainTexture = tex;
		parent.GetComponent<MapTileScript>().WallObject = wall;
		//Debug.Log("Created walls");
		return wall;
	}

	private GameObject CreateEmptyTile(MapTile tile, Transform parent, string objName, Vector3 offset, float size)
	{
		int x = tile.Position.x;
		int y = tile.Position.y;
		string name = objName + x + "/" + y + "_" + tile.TileType;
		GameObject tileObj = new GameObject(name);
		tileObj.transform.SetParent(parent);
		float nx = x * 0.60f;
		float ny = y * 0.60f;
		tileObj.transform.position = new Vector3(nx, ny, 0) + offset;
		MapTileScript mts = tileObj.AddComponent<MapTileScript>();
		mts.MapTile = tile;
		MapTileToGO[tile] = tileObj;
		return tileObj;
	}


	public void UpdateTileAndNeighbours(int level, MapTileScript mts, bool destroyWalls)
	{
		UpdateTileMesh(level, mts, destroyWalls);
		if (mts.ETile)
			UpdateTileMesh(level, MapTileToGO[mts.ETile].GetComponent<MapTileScript>(), false);
		if (mts.WTile)
			UpdateTileMesh(level, MapTileToGO[mts.WTile].GetComponent<MapTileScript>(), false);
		if (mts.STile)
			UpdateTileMesh(level, MapTileToGO[mts.STile].GetComponent<MapTileScript>(), false);
		if (mts.NTile)
			UpdateTileMesh(level,MapTileToGO[mts.NTile].GetComponent<MapTileScript>(), false);
	}

	private void UpdateTileMesh(int level, MapTileScript mts, bool destroyWalls)
	{
		if(mts.MapTile.TileType == TileType.Solid)
		{
			if(mts.WallObject)
				Destroy(mts.WallObject);
			CreateFloor(level, mts.MapTile, MapTileToGO[mts.MapTile].transform, TileSize, TileOffset, mts.FloorObject, true);
			mts.WallObject = null;
			return;
		}
		CreateFloor(level, mts.MapTile, MapTileToGO[mts.MapTile].transform, TileSize, TileOffset, mts.FloorObject, true);
		if (!destroyWalls)
		{
			//Debug.LogFormat("Update tile {0} mesh : Destroying WallObject and CreatingWalls", mts.MapTile);
			CreateWalls(level, mts.MapTile, MapTileToGO[mts.MapTile].transform, TileSize, TileOffset, mts.WallObject);
		}
		else
		{
			Destroy(mts.WallObject);
			mts.WallObject = null;
		}
	}

	#endregion

	#region Textures

	public static bool UpdateTexture(TextureType type, int oldIndex, Texture2D newTex)
	{
		if (type == TextureType.Floor)
			TextureData.Floors.Textures[oldIndex] = newTex;
		else if (type == TextureType.Wall)
			TextureData.Walls.Textures[oldIndex] = newTex;
		else if (type == TextureType.Door)
			TextureData.Doors.Textures[oldIndex] = newTex;
		else if (type == TextureType.Lever)
		{
			TextureData.Levers.Textures[oldIndex] = newTex;
			foreach (var level in LevelData)
			{
				foreach (var so in level.Objects)
				{
					if (so && so.ObjectID - 368 == oldIndex)
						SetGOSprite(so);
				}
			}
		}
		else if (type == TextureType.Other)
		{
			//CHECK SIZE!
			Texture2D curTex = TextureData.Other.Textures[oldIndex];
			if (newTex.width != curTex.width || newTex.height != curTex.height)
				return false;
			TextureData.Other.Textures[oldIndex] = newTex;
			foreach (var level in LevelData)
			{
				foreach (var so in level.Objects)
				{
					if (so && StaticObject.GetObjectType(so.ObjectID) == GetTypeFromOtherSpriteIndex(oldIndex))
						SetGOSprite(so);
				}
			}
		}
		else if (type == TextureType.Object)
		{
			Texture2D curTex = TextureData.Objects.Textures[oldIndex];
			if (newTex.width != curTex.width || newTex.height != curTex.height)
				return false;
			TextureData.Objects.Textures[oldIndex] = newTex;
			foreach (var level in LevelData)
			{
				foreach (var so in level.Objects)
				{
					if (so && so.ObjectID == oldIndex)
						SetGOSprite(so);
				}
			}
		}
		else if (type == TextureType.NPCHead)
		{
			Texture2D curTex = TextureData.NPCHeads.Textures[oldIndex];
			if (newTex.width != curTex.width || newTex.height != curTex.height)
				return false;
			TextureData.NPCHeads.Textures[oldIndex] = newTex;
			return true;
		}
		else if (type == TextureType.GenericHead)
		{
			Texture2D curTex = TextureData.GenHeads.Textures[oldIndex];
			if (newTex.width != curTex.width || newTex.height != curTex.height)
				return false;
			TextureData.GenHeads.Textures[oldIndex] = newTex;
			return true;
		}
		UpdateTileTextures(type, oldIndex);
		return true;
	}

	private static void UpdateTileTextures(TextureType texType, int loadedIndex)
	{
		UpdateTileTextures(texType, loadedIndex, loadedIndex);
	}
	private static void UpdateTileTextures(TextureType texType, int loadedIndex, int newIndex)
	{
		if (texType == TextureType.Floor)
		{
			foreach (var level in LevelData)
				for (int i = 0; i < level.FloorTextures.Length; i++)
					if (level.FloorTextures[i] == loadedIndex)
						SwapFloorTexture(level.Level, i, newIndex);
		}
		else if (texType == TextureType.Wall)
		{
			foreach (var level in LevelData)
				for (int i = 0; i < level.WallTextures.Length; i++)
					if (level.WallTextures[i] == loadedIndex)
						SwapWallTexture(level.Level, i, newIndex);
		}
		else if(texType == TextureType.Door)
		{
			foreach (var level in LevelData)
				for (int i = 0; i < level.DoorTextures.Length; i++)
					if (level.DoorTextures[i] == loadedIndex)
						SwapDoorTexture(level.Level, i, newIndex);
		}
	}

	public static bool AddTexture(TextureType texType, Texture2D tex)
	{
		if (texType == TextureType.Floor)
		{
			if (TextureData.Floors.Count > 255)
				return false;
			TextureData.Floors.Textures.Add(tex);
		}
		else if (texType == TextureType.Wall)
		{
			if (TextureData.Walls.Count > 255)
				return false;
			TextureData.Walls.Textures.Add(tex);
		}
		else if (texType == TextureType.Door)
		{
			if (TextureData.Doors.Count > 255)
				return false;
			TextureData.Doors.Textures.Add(tex);
		}
		else if (texType == TextureType.NPCHead)
		{
			if (TextureData.NPCHeads.Count > 55)
				return false;
			TextureData.NPCHeads.Textures.Add(tex);
		}
		return true;
	}

	public static bool RemoveTexture(TextureType texType)
	{
		if (texType == TextureType.Floor)
		{
			if (TextureData.Floors.Textures.Count == 52)
				return false;
			UpdateTileTextures(texType, TextureData.Floors.Count - 1, 0);
			TextureData.Floors.Textures.RemoveAt(TextureData.Floors.Count - 1);
		}
		else if (texType == TextureType.Wall)
		{
			if (TextureData.Walls.Textures.Count == 210)
				return false;
			UpdateTileTextures(texType, TextureData.Walls.Count - 1, 0);
			TextureData.Walls.Textures.RemoveAt(TextureData.Walls.Count - 1);
		}
		else if (texType == TextureType.Door)
		{
			if (TextureData.Doors.Textures.Count == 13)
				return false;
			UpdateTileTextures(texType, TextureData.Doors.Count - 1, 0);
			TextureData.Doors.Textures.RemoveAt(TextureData.Doors.Count - 1);
		}
		else if (texType == TextureType.NPCHead)
		{
			if (TextureData.NPCHeads.Textures.Count == 28)
				return false;
			TextureData.NPCHeads.Textures.RemoveAt(TextureData.NPCHeads.Count - 1);
		}
		return true;
	}

	public int GetTextureCount(TextureType texType)
	{
		if (texType == TextureType.Floor)
			return TextureData.Floors.Textures.Count;
		else if (texType == TextureType.Wall)
			return TextureData.Walls.Textures.Count;
		else if (texType == TextureType.Door)
			return TextureData.Doors.Textures.Count;
		else if (texType == TextureType.NPCHead)
			return TextureData.NPCHeads.Textures.Count;		

		return -1;
	}
	/// <summary>
	/// Used when you are swapping level texture (choosing another loaded texture for a level texture)
	/// </summary>
	public static void SwapTexture(TextureType type, int level, int levelIndex, int loadedIndex)
	{
		if (type == TextureType.Floor)
			SwapFloorTexture(level, levelIndex, loadedIndex);
		else if (type == TextureType.Wall)
			SwapWallTexture(level, levelIndex, loadedIndex);
		else if (type == TextureType.Door)
			SwapDoorTexture(level, levelIndex, loadedIndex);
	}
	public static void SwapFloorTexture(int level, int levelFloorIndex, int loadedFloorIndex)
	{
		LevelData[level - 1].FloorTextures[levelFloorIndex] = loadedFloorIndex;
		UpdateAllFloorTextures(levelFloorIndex, level);
	}
	public static void SwapWallTexture(int level, int levelWallIndex, int loadedWallIndex)
	{
		LevelData[level - 1].WallTextures[levelWallIndex] = loadedWallIndex;
		UpdateAllWallTextures(levelWallIndex, level);
	}
	public static void SwapDoorTexture(int level, int levelDoorIndex, int loadedDoorIndex)
	{
		LevelData[level - 1].DoorTextures[levelDoorIndex] = loadedDoorIndex;
		UpdateAllDoorTextures(levelDoorIndex, level);
	}

	/// <summary>
	/// Updates the GO textures of objects for LEVEL indexes (or door object ID's).
	/// So, if the index 0 of level 1 floor has changed from 7 (earth stuff) to 14 (something else) it goes through all floors,
	/// and if floor has index of 0 (the changed one) it swaps the texture for the current one that is under index 14.
	/// </summary>
	public static void UpdateAllFloorTextures(int indexChanged, int level)
	{
		for (int x = 0; x < 64; x++)
		{
			for (int y = 0; y < 64; y++)
			{
				MapTile tile = LevelData[level - 1].MapTiles[x, y];
				if (tile.FloorTexture == indexChanged)
					UpdateGOFloorTexture(tile, level);
			}
		}
	}
	/// <summary>
	/// Updates the GO textures of objects for LEVEL indexes (or door object ID's).
	/// So, if the index 0 of level 1 floor has changed from 7 (earth stuff) to 14 (something else) it goes through all floors,
	/// and if floor has index of 0 (the changed one) it swaps the texture for the current one that is under index 14.
	/// </summary>
	public static void UpdateAllWallTextures(int indexChanged, int level)
	{
		for (int x = 0; x < 64; x++)
		{
			for (int y = 0; y < 64; y++)
			{
				MapTile tile = LevelData[level - 1].MapTiles[x, y];
				if (tile.WallTexture == indexChanged)
				{
					UpdateGOWallTexture(tile, level);
					List<StaticObject> doors = tile.GetTileObjects((so) => { return so.IsDoor(); });
					if (doors == null)
						continue;
					foreach (var door in doors)
						UpdateGOFrameDoorTexture(door, level);
				}
			}
		}
		foreach (var so in LevelData[level - 1].Objects)
		{
			if (so && so.IsVerticalTexture()) //Vertical texture objects take textures from local list.
				SetGOSprite(so);
		}
	}
	/// <summary>
	/// Updates the GO textures of objects for LEVEL indexes (or door object ID's).
	/// So, if the index 0 of level 1 floor has changed from 7 (earth stuff) to 14 (something else) it goes through all floors,
	/// and if floor has index of 0 (the changed one) it swaps the texture for the current one that is under index 14.
	/// </summary>
	public static void UpdateAllDoorTextures(int indexChanged, int level)
	{
		foreach (var so in LevelData[level - 1].Objects)
		{
			if(so && so.IsDoor())
			{
				int currentIndex = so.ObjectID - 320;
				if(currentIndex < 6)
				{
					if(currentIndex == indexChanged)
						UpdateGODoorTexture(so, level);
				}
			}
		}
	}
	/// <summary>
	/// Used when you are changing single tile texture (like from properties)
	/// </summary>
	public static void UpdateGOTexture(MapTile tile, int level, TextureType type)
	{
		if (type == TextureType.Floor)
			UpdateGOFloorTexture(tile, level);
		else if (type == TextureType.Wall)
		{
			UpdateGOWallTexture(tile, level);
			List<StaticObject> tileObjects = tile.GetTileObjects();
			if (tileObjects == null)
				return;
			foreach (var tileObject in tileObjects)
			{
				if (tileObject.IsDoor())
					UpdateGOFrameDoorTexture(tileObject, level);
			}
		}
	
	}
	public static void UpdateGOFloorTexture(MapTile tile, int level)
	{
		GameObject tileGO = MapTileToGO[tile];
		MapTileScript mts = tileGO.GetComponent<MapTileScript>();
		MeshRenderer mr = mts.FloorObject.GetComponent<MeshRenderer>();
		mr.material.mainTexture = GetFloorTextureFromIndex(tile.FloorTexture, level);
	}
	public static void UpdateGOWallTexture(MapTile tile, int level)
	{
		GameObject tileGO = MapTileToGO[tile];
		MapTileScript mts = tileGO.GetComponent<MapTileScript>();
		if (mts.WallObject)
		{
			MeshRenderer mr = mts.WallObject.GetComponent<MeshRenderer>();
			mr.material.mainTexture = GetWallTextureFromIndex(tile.WallTexture, level);
		}
	}
	public static void UpdateGOFrameDoorTexture(StaticObject so, int level)
	{
		GameObject obj = ObjectToGO[so];
		StaticObjectScript sos = obj.GetComponent<StaticObjectScript>();
		if (sos.DoorFrameObject)
		{
			MeshRenderer mr = sos.DoorFrameObject.GetComponent<MeshRenderer>();
			mr.material.mainTexture = GetWallTextureFromIndex(so.Tile.WallTexture, level);
		}
	}
	public static void UpdateGODoorTexture(StaticObject so, int level)
	{
		GameObject obj = ObjectToGO[so];
		StaticObjectScript sos = obj.GetComponent<StaticObjectScript>();
		if(sos.DoorObject)
		{
			MeshRenderer mr = sos.DoorObject.GetComponent<MeshRenderer>();
			mr.material.mainTexture = so.GetDoorTexture();
		}
	}


	#endregion

	#region Mesh Generation

	private Vector3[] CreateFloorVerts(float size, float zoffs, TileType type)
	{
		float addN = 0.0f, addS = 0.0f, addE = 0.0f, addW = 0.0f;
		switch (type)
		{
			case TileType.SlopeUpN:
				addN -= StepZ;
				break;
			case TileType.SlopeUpS:
				addS -= StepZ;
				break;
			case TileType.SlopeUpE:
				addE -= StepZ;
				break;
			case TileType.SlopeUpW:
				addW -= StepZ;
				break;
			default:
				break;
		}
		float zA = zoffs + addS + addW;
		float zB = zoffs + addN + addW;
		float zC = zoffs + addN + addE;
		float zD = zoffs + addS + addE;
		Vector3[] verts = new Vector3[4];
		verts[0] = new Vector3(-size, -size, zA);		//SW
		verts[1] = new Vector3(-size, size, zB);		//NW
		verts[2] = new Vector3(size, size, zC);			//NE
		verts[3] = new Vector3(size, -size, zD);       //SE
		return verts;
	}

	private Vector3[] CreateWallVerts(float size, float floorz, float ceilz, Vector2Int dir, Vector2Int slopeDir)
	{
		Vector3[] verts = new Vector3[4];

		if (dir == Vector2Int.up)    //N wall
		{
			verts[0] = new Vector3(-size, size, floorz);
			verts[1] = new Vector3(size, size, floorz);
			verts[2] = new Vector3(-size, size, ceilz - (slopeDir == Vector2Int.left ? StepZ : 0));
			verts[3] = new Vector3(size, size, ceilz - (slopeDir == Vector2Int.right ? StepZ : 0));
		}
		else if (dir == Vector2Int.down)    //S wall
		{
			verts[0] = new Vector3(size, -size, floorz);
			verts[1] = new Vector3(-size, -size, floorz);
			verts[2] = new Vector3(size, -size, ceilz - (slopeDir == Vector2Int.right ? StepZ : 0));
			verts[3] = new Vector3(-size, -size, ceilz - (slopeDir == Vector2Int.left ? StepZ : 0));
		}
		else if (dir == Vector2Int.right)    //E wall
		{
			verts[0] = new Vector3(size, size, floorz);
			verts[1] = new Vector3(size, -size, floorz);
			verts[2] = new Vector3(size, size, ceilz - (slopeDir == Vector2Int.up ? StepZ : 0));
			verts[3] = new Vector3(size, -size, ceilz - (slopeDir == Vector2Int.down ? StepZ : 0));
		}
		else if (dir == Vector2Int.left)    //W wall
		{
			verts[0] = new Vector3(-size, -size, floorz);
			verts[1] = new Vector3(-size, size, floorz);
			verts[2] = new Vector3(-size, -size, ceilz - (slopeDir == Vector2Int.down ? StepZ : 0));
			verts[3] = new Vector3(-size, size, ceilz - (slopeDir == Vector2Int.up ? StepZ : 0));
		}
		return verts;
	}

	private Vector3[] CreateWallCornerVerts(float size, float floorz, float ceilz, Vector2Int dir)
	{
		Vector3[] verts = new Vector3[4];

		if (dir == new Vector2Int(1, 1))    //NE wall
		{
			verts[0] = new Vector3(-size, size, floorz);
			verts[1] = new Vector3(size, -size, floorz);
			verts[2] = new Vector3(-size, size, ceilz);
			verts[3] = new Vector3(size, -size, ceilz);
		}
		else if (dir == new Vector2Int(1, 0))    //SE wall
		{
			verts[0] = new Vector3(size, size, floorz);
			verts[1] = new Vector3(-size, -size, floorz);
			verts[2] = new Vector3(size, size, ceilz);
			verts[3] = new Vector3(-size, -size, ceilz);
		}
		else if (dir == new Vector2Int(0, 1))    //NW wall
		{
			verts[0] = new Vector3(-size, -size, floorz);
			verts[1] = new Vector3(size, size, floorz);
			verts[2] = new Vector3(-size, -size, ceilz);
			verts[3] = new Vector3(size, size, ceilz);
		}
		else if (dir == new Vector2Int(0, 0))    //SW wall
		{
			verts[0] = new Vector3(size, -size, floorz);
			verts[1] = new Vector3(-size, size, floorz);
			verts[2] = new Vector3(size, -size, ceilz);
			verts[3] = new Vector3(-size, size, ceilz);
		}
		return verts;
	}

	private Mesh CreateWallMesh(float size, float floorz, float ceilz, Vector2Int dir, Vector2Int slopeDir, bool corner = false)
	{
		Mesh wall = new Mesh();
		Vector3[] verts;
		if (!corner)
			verts = CreateWallVerts(size, floorz, ceilz, dir, slopeDir);
		else
			verts = CreateWallCornerVerts(size, floorz, ceilz, dir);
		int[] tris = new int[6];
		tris[0] = 0;
		tris[1] = 2;
		tris[2] = 1;

		tris[3] = 2;
		tris[4] = 3;
		tris[5] = 1;
		Vector3[] norms = new Vector3[4];
		if (!corner)
			for (int i = 0; i < 4; i++)
				norms[i] = new Vector3(-dir.x, -dir.y, 0);
		else
			for (int i = 0; i < 4; i++)
				norms[i] = new Vector3(dir.x == 0 ? -1 : 1, dir.y == 0 ? -1 : 1, 0);

		Vector2[] uvs = new Vector2[4];
		uvs[0] = Vector2.zero;
		uvs[1] = new Vector2(1.0f, 0);
		uvs[2] = new Vector2(0, 1.0f);
		uvs[3] = Vector2.one;

		wall.vertices = verts;
		wall.triangles = tris;
		wall.normals = norms;
		wall.uv = uvs;

		return wall;
	}

	private Mesh CreateSquareFloor(float size, float zoffs, MapTile tile)
	{
		Mesh floor = new Mesh();
		
		Vector3[] verts = CreateFloorVerts(size, zoffs, tile.TileType);

		Vector3[] floor_v = new Vector3[4];
		floor_v[0] = verts[0];
		floor_v[1] = verts[1];
		floor_v[2] = verts[2];
		floor_v[3] = verts[3];

		int[] floor_t = new int[6];
		floor_t[0] = 0;
		floor_t[1] = 1;
		floor_t[2] = 2;

		floor_t[3] = 2;
		floor_t[4] = 3;
		floor_t[5] = 0;

		Vector3[] floor_n = new Vector3[4];
		for (int i = 0; i < 4; i++)
			floor_n[i] = Vector3.back;

		Vector2[] floor_uv = new Vector2[4];
		floor_uv[1] = Vector2.zero;
		floor_uv[0] = new Vector2(0, 1.0f);
		floor_uv[3] = Vector2.one;
		floor_uv[2] = new Vector2(1.0f, 0);

		floor.vertices = floor_v;
		floor.triangles = floor_t;
		floor.normals = floor_n;
		floor.uv = floor_uv;

		return floor;
	}

	private Mesh CreateWalls(int level, float size, float floorz, MapTile tile)
	{
		List<Mesh> walls = new List<Mesh>();

		MapTile N = null, S = null, E = null, W = null;
		int x = tile.Position.x;
		int y = tile.Position.y;

		if (x > 0)
			W = LevelData[level - 1].MapTiles[x - 1, y];
		if (x < 63)
			E = LevelData[level - 1].MapTiles[x + 1, y];
		if (y > 0)
			S = LevelData[level - 1].MapTiles[x, y - 1];
		if (y < 63)
			N = LevelData[level - 1].MapTiles[x, y + 1];

		if (tile.TileType == TileType.OpenSW)
			walls.Add(CreateWallMesh(size, floorz, -CeilingZ, new Vector2Int(1, 1), Vector2Int.zero, true));
		else if (tile.TileType == TileType.OpenNW)
			walls.Add(CreateWallMesh(size, floorz, -CeilingZ, new Vector2Int(1, 0), Vector2Int.zero, true));
		else if (tile.TileType == TileType.OpenNE)
			walls.Add(CreateWallMesh(size, floorz, -CeilingZ, new Vector2Int(0, 0), Vector2Int.zero, true));
		else if (tile.TileType == TileType.OpenSE)
			walls.Add(CreateWallMesh(size, floorz, -CeilingZ, new Vector2Int(0, 1), Vector2Int.zero, true));

		if (W && (W.TileType == TileType.Open || W.TileType == TileType.OpenNE || W.TileType == TileType.OpenSE) && W.FloorHeight > tile.FloorHeight && tile.TileType != TileType.OpenNE && tile.TileType != TileType.OpenSE)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(W), Vector2Int.left, Vector2Int.zero));
		else if(W && W.TileType == TileType.SlopeUpW && W.FloorHeight > tile.FloorHeight + 1)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(W), Vector2Int.left, Vector2Int.zero));
		else if (W && W.TileType == TileType.SlopeUpN && W.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(W), Vector2Int.left, Vector2Int.up));
		else if (W && W.TileType == TileType.SlopeUpS && W.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(W), Vector2Int.left, Vector2Int.down));
		else if (W && W.TileType == TileType.SlopeUpE && W.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(W) - StepZ, Vector2Int.left, Vector2Int.zero));
		else if (!W || ((W && W.TileType == TileType.Solid || W.TileType == TileType.OpenNW || W.TileType == TileType.OpenSW) && tile.TileType != TileType.OpenNE && tile.TileType != TileType.OpenSE))
			walls.Add(CreateWallMesh(size, floorz, -CeilingZ, Vector2Int.left, Vector2Int.zero));

		if (E && (E.TileType == TileType.Open || E.TileType == TileType.OpenNW || E.TileType == TileType.OpenSW) && E.FloorHeight > tile.FloorHeight && tile.TileType != TileType.OpenSW && tile.TileType != TileType.OpenNW)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(E), Vector2Int.right, Vector2Int.zero));
		else if(E &&  E.TileType == TileType.SlopeUpE && E.FloorHeight > tile.FloorHeight + 1)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(E), Vector2Int.right, Vector2Int.zero));
		else if (E && E.TileType == TileType.SlopeUpN && E.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(E), Vector2Int.right, Vector2Int.up));
		else if (E && E.TileType == TileType.SlopeUpS && E.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(E), Vector2Int.right, Vector2Int.down));
		else if (E && E.TileType == TileType.SlopeUpW && E.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(E) - StepZ, Vector2Int.right, Vector2Int.zero));
		else if(!E || ((E && E.TileType == TileType.Solid || E.TileType == TileType.OpenNE || E.TileType == TileType.OpenSE) && tile.TileType != TileType.OpenNW && tile.TileType != TileType.OpenSW))
			walls.Add(CreateWallMesh(size, floorz, -CeilingZ, Vector2Int.right, Vector2Int.zero));

		if (S && (S.TileType == TileType.Open || S.TileType == TileType.OpenNW || S.TileType == TileType.OpenNE) && S.FloorHeight > tile.FloorHeight && tile.TileType != TileType.OpenNW && tile.TileType != TileType.OpenNE)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(S), Vector2Int.down, Vector2Int.zero));
		else if (S && S.TileType == TileType.SlopeUpS && S.FloorHeight > tile.FloorHeight + 1)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(S), Vector2Int.down, Vector2Int.zero));
		else if (S && S.TileType == TileType.SlopeUpE && S.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(S), Vector2Int.down, Vector2Int.right));
		else if (S && S.TileType == TileType.SlopeUpW && S.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(S), Vector2Int.down, Vector2Int.left));
		else if (S && S.TileType == TileType.SlopeUpN && S.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(S) - StepZ, Vector2Int.down, Vector2Int.zero));
		else if (!S || ((S && S.TileType == TileType.Solid || S.TileType == TileType.OpenSW || S.TileType == TileType.OpenSE) && tile.TileType != TileType.OpenNE && tile.TileType != TileType.OpenNW))
			walls.Add(CreateWallMesh(size, floorz, -CeilingZ, Vector2Int.down, Vector2Int.zero));

		if (N && (N.TileType == TileType.Open || N.TileType == TileType.OpenSE || N.TileType == TileType.OpenSW) && N.FloorHeight > tile.FloorHeight && tile.TileType != TileType.OpenSW && tile.TileType != TileType.OpenSE)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(N), Vector2Int.up, Vector2Int.zero));
		else if(N && N.TileType == TileType.SlopeUpN && N.FloorHeight > tile.FloorHeight + 1)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(N), Vector2Int.up, Vector2Int.zero));
		else if (N && N.TileType == TileType.SlopeUpE && N.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(N), Vector2Int.up, Vector2Int.right));
		else if (N && N.TileType == TileType.SlopeUpW && N.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(N), Vector2Int.up, Vector2Int.left));
		else if (N && N.TileType == TileType.SlopeUpS && N.FloorHeight >= tile.FloorHeight)
			walls.Add(CreateWallMesh(size, floorz, GetTileHeight(N) - StepZ, Vector2Int.up, Vector2Int.zero));
		else if (!N || ((N && N.TileType == TileType.Solid || N.TileType == TileType.OpenNE || N.TileType == TileType.OpenNW) && tile.TileType != TileType.OpenSE && tile.TileType != TileType.OpenSW))
			walls.Add(CreateWallMesh(size, floorz, -CeilingZ, Vector2Int.up, Vector2Int.zero));

		CombineInstance[] toCombine = new CombineInstance[walls.Count];
		for(int i = 0; i < walls.Count; i++)
		{
			Mesh wall = walls[i];
			CombineInstance ci = new CombineInstance();
			ci.mesh = wall;
			toCombine[i] = ci;
		}
		Mesh combined = new Mesh();
		combined.CombineMeshes(toCombine, true, false);

		return combined;
	}

	private Mesh CreateFloorCorner(float size, float zoffs, TileType tileType)
	{
		Mesh mesh = new Mesh();

		Vector3[] flatVerts = CreateFloorVerts(size, zoffs, tileType);
		Vector3[] verts = new Vector3[3];
		int[] tris = new int[3];
		Vector2[] uv = new Vector2[3];
		if (tileType == TileType.OpenSW)
		{
			verts[0] = flatVerts[0]; uv[0] = new Vector2(0, 0);
			verts[1] = flatVerts[1]; uv[1] = new Vector2(0, 1.0f);
			verts[2] = flatVerts[3]; uv[2] = new Vector2(1.0f, 0);
		}
		else if(tileType == TileType.OpenNW)
		{
			verts[0] = flatVerts[0]; uv[0] = new Vector2(0, 0);
			verts[1] = flatVerts[1]; uv[1] = new Vector2(0, 1.0f);
			verts[2] = flatVerts[2]; uv[2] = new Vector2(1.0f, 1.0f);
		}
		else if(tileType == TileType.OpenNE)
		{
			verts[0] = flatVerts[1]; uv[0] = new Vector2(0, 1.0f);
			verts[1] = flatVerts[2]; uv[1] = new Vector2(1.0f, 1.0f);
			verts[2] = flatVerts[3]; uv[2] = new Vector2(1.0f, 0);
		}
		else if(tileType == TileType.OpenSE)
		{
			verts[0] = flatVerts[0]; uv[0] = new Vector2(0, 0);
			verts[1] = flatVerts[2]; uv[1] = new Vector2(1.0f, 1.0f);
			verts[2] = flatVerts[3]; uv[2] = new Vector2(1.0f, 0);
		}
		tris[0] = 0;
		tris[1] = 1;
		tris[2] = 2;

		Vector3[] norms = new Vector3[3];
		for (int i = 0; i < 3; i++)
			norms[i] = Vector3.back;

		mesh.vertices = verts;
		mesh.triangles = tris;
		mesh.normals = norms;
		mesh.uv = uv;

		return mesh;
	}

	public static Mesh CreateDoorFrameMesh(float size, float floorz, float ceilz)
	{
		Mesh mesh = new Mesh();
		Vector3[] verts = new Vector3[8];
		//Vector3[] verts = new Vector3[4];
		float ds = (2 * size) / 7;
		float doorz = 4 * size / 3;
		Vector3 A = new Vector3(-3 * ds, 0, floorz);
		Vector3 B = new Vector3(-3 * ds, 0, ceilz);
		Vector3 C = new Vector3(4 * ds, 0, ceilz);
		Vector3 D = new Vector3(4 * ds, 0, floorz);
		Vector3 a = new Vector3(-ds, 0, floorz);
		Vector3 b = new Vector3(-ds, 0, floorz - doorz);
		Vector3 c = new Vector3(2 * ds, 0, floorz - doorz);
		Vector3 d = new Vector3(2 * ds, 0, floorz);
		verts[0] = A;
		verts[1] = B;
		verts[2] = C;
		verts[3] = D;
		verts[4] = a;
		verts[5] = b;
		verts[6] = c;
		verts[7] = d;

		int[] tris = new int[12 * 3];
		//int[] tris = new int[2 * 3];
		tris[0] = 0; tris[1] = 5; tris[2] = 4;
		tris[3] = 0; tris[4] = 1; tris[5] = 5;
		tris[6] = 1; tris[7] = 2; tris[8] = 5;
		tris[9] = 2; tris[10] = 6; tris[11] = 5;
		tris[12] = 2; tris[13] = 3; tris[14] = 6;
		tris[15] = 3; tris[16] = 7; tris[17] = 6;

		tris[18] = 0; tris[19] = 4; tris[20] = 5;
		tris[21] = 0; tris[22] = 5; tris[23] = 1;
		tris[24] = 1; tris[25] = 5; tris[26] = 2;
		tris[27] = 2; tris[28] = 5; tris[29] = 6;
		tris[30] = 3; tris[31] = 2; tris[32] = 6;
		tris[33] = 3; tris[34] = 6; tris[35] = 7;


		float uv_d = 1.0f / 7.0f;
		Vector2[] uvs = new Vector2[8];
		//Vector2[] uvs = new Vector2[4];
		uvs[0] = new Vector2(0, 0);
		uvs[1] = new Vector2(0, 1);
		uvs[2] = new Vector2(1, 1);
		uvs[3] = new Vector2(1, 0);

		doorz = floorz - doorz;

		uvs[4] = new Vector2(2 * uv_d, 0);
		uvs[5] = new Vector2(2 * uv_d, doorz / ceilz);
		uvs[6] = new Vector2(5 * uv_d, doorz / ceilz);
		uvs[7] = new Vector2(5 * uv_d, 0);


		Vector3[] norms = new Vector3[8];
		//Vector3[] norms = new Vector3[4];
		for (int i = 0; i < norms.Length; i++)
			norms[i] = Vector3.down;

		mesh.vertices = verts;
		mesh.triangles = tris;
		mesh.normals = norms;
		mesh.uv = uvs;

		return mesh;
	}
	public static Mesh CreateDoorMesh(float size, float floorz)
	{
		Mesh mesh = new Mesh();
		Vector3[] verts = new Vector3[4];
		float ds = (2 * size) / 7;
		float doorh = 4 * size / 3;

		Vector3 A = new Vector3(-ds, 0, floorz);
		Vector3 B = new Vector3(-ds, 0, floorz - doorh);
		Vector3 C = new Vector3(2 * ds, 0, floorz - doorh);
		Vector3 D = new Vector3(2 * ds, 0, floorz);
		verts[0] = A;
		verts[1] = B;
		verts[2] = C;
		verts[3] = D;

		int[] tris = new int[4 * 3];
		tris[0] = 0; tris[1] = 1; tris[2] = 3;
		tris[3] = 1; tris[4] = 2; tris[5] = 3;
		tris[6] = 3; tris[7] = 2; tris[8] = 0;
		tris[9] = 2; tris[10] = 1; tris[11] = 0;

		Vector2[] uvs = new Vector2[4];
		uvs[0] = new Vector2(0, 0);
		uvs[1] = new Vector2(0, 0.8125f);
		uvs[2] = new Vector2(1, 0.8125f);
		uvs[3] = new Vector2(1, 0);

		Vector3[] norms = new Vector3[4];
		for (int i = 0; i < norms.Length; i++)
			norms[i] = Vector3.down;

		mesh.vertices = verts;
		mesh.triangles = tris;
		mesh.normals = norms;
		mesh.uv = uvs;

		return mesh;

	}
	#endregion

	#region Objects

	private void CreateStaticObjects(int level, Transform parentLevel)
	{
		GameObject parent = new GameObject("LevelObjects");
		parent.transform.SetParent(parentLevel);
		for (int y = 0; y < 64; y++)
		{
			for (int x = 0; x < 64; x++)
			{
				MapTile tile = LevelData[level - 1].MapTiles[x, y];
				if (tile.ObjectAdress != 0)
				{
					int index = tile.ObjectAdress;
					//GameObject parent = MapTileToGO[tile];

					StaticObject staticObject = LevelData[level - 1].Objects[index];
					if (!staticObject)
						continue;
					GameObject staticObjectGO = null;
					float tileHeight = GetTileHeight(tile);
					try
					{
						staticObjectGO = SpawnGO(staticObject, parent);
					}
					catch(NullReferenceException)
					{
						Debug.LogWarningFormat("Failed to spawn object, index : {0}", index);
						continue;
					}
					StaticObject next = staticObject.GetNextObject();
					while (next)
					{
						GameObject nextGO = SpawnGO(next, parent);
						if(next.IsDoor())
							CreateDoorGO(next, nextGO.transform, level);
						next = next.GetNextObject();						
					}
					if (staticObject.IsDoor())
						CreateDoorGO(staticObject, staticObjectGO.transform, level);
				}
			}
		}
	}

	public static bool AddObject(MapTile tile, int level, StaticObject so = null)
	{
		if (so == null)
			so = new StaticObject(tile);

		int index = 0;
		if (so is MobileObject)
		{
			if (LevelData[level - 1].MobileListStart == 0)
				return false;
			index = GetNextFreeMobileIndex(level);
			LevelData[level - 1].MobileListStart--;
		}
		else if (so is StaticObject)
		{
			if (LevelData[level - 1].StaticListStart == 0)
				return false;
			index = GetNextFreeStaticIndex(level);
			LevelData[level - 1].StaticListStart--;
		}
		//Debug.LogFormat("Adding new item at index {0}", index);
		LevelData[level - 1].Objects[index] = so;
		so.CurrentAdress = index;
		if (so is MobileObject)
			ActivateMob((MobileObject)so, level);
		tile.AddObjectToTile(so);
		//so.MapPosition = tile.Position;
		GameObject obj = SpawnGO(so, MapTileToGO[tile]);
		ObjectToGO[so] = obj;
		return true;
	}
	public static StaticObject AddObject(MapTile tile, Vector2Int pos, int level, int id, StaticObject copy = null, bool inventory = false)
	{
		StaticObject obj = null;
		int index = 0;
		if (StaticObject.IsMonster(id))
		{
			if (LevelData[level - 1].MobileListStart == 0)
				return null;
			index = GetNextFreeMobileIndex(level);
			LevelData[level - 1].MobileListStart--;
			if (copy == null) obj = new MobileObject(tile, id, pos.x, pos.y); else obj = new MobileObject(tile, pos.x, pos.y, (MobileObject)copy);
		}
		else
		{
			if (LevelData[level - 1].StaticListStart == 0)
				return null;
			index = GetNextFreeStaticIndex(level);
			LevelData[level - 1].StaticListStart--;
			if (copy == null) obj = new StaticObject(tile, id, pos.x, pos.y); else obj = new StaticObject(tile, pos.x, pos.y, copy);
		}
		LevelData[level - 1].Objects[index] = obj;
		obj.CurrentAdress = index;
		if (obj.IsMonster())	//Activate mob 
			ActivateMob((MobileObject)obj, level);
		if (!inventory)			//If is world object (not in a container or something)
		{
			tile.AddObjectToTile(obj);
			GameObject objGO = SpawnGO(obj, MapTileToGO[tile]);
			ObjectToGO[obj] = objGO;
		}
		if(obj.IsAnimated())	//Add animation overlay if is animated
		{
			AnimationOverlay anim = new AnimationOverlay();
			anim.Adress = obj.CurrentAdress;
			anim.Duration = 65535;
			anim.X = obj.MapPosition.x;
			anim.Y = obj.MapPosition.y;
			obj.Animation = anim;
		}
		return obj;
	}

	public static bool RemoveObject(StaticObject so, bool removeFromContainer = false)
	{
		int level = so.CurrentLevel;
		int listStart = 0;
		//Update the free lists
		if (so is StaticObject)
		{
			listStart = LevelData[level - 1].StaticListStart + 254;
			if (listStart == 768)
				return false;
		}
		if (so is MobileObject)
		{
			listStart = LevelData[level - 1].MobileListStart;
			if (listStart == 254)   //Theoretically impossible (can't remove if no mobs in map)
				return false;
		}
		//Debug.LogFormat("List start at {0}, index {1}", listStart, LevelData[level - 1].FreeObjects[listStart]);

		//Destroy the unity game object of this object
		if (ObjectToGO.ContainsKey(so))
		{
			GameObject go = ObjectToGO[so];
			if (go)
				Destroy(go);
		}
		listStart++;
		int index = so.CurrentAdress;
		LevelData[level - 1].FreeObjects[listStart] = index;

		if (so is MobileObject)
		{
			MobileObject mo = (MobileObject)so;
			LevelData[level - 1].MobileListStart++;
			DeactivateMob(mo, level);
		}
		else
			LevelData[level - 1].StaticListStart++;

		//Remove from tile (if possible)
		so.Tile.RemoveObjectFromTile(so);
		//If is linked / container
		if (!so.IsQuantity)
		{
			//Remove all inventory too
			List<StaticObject> containedObjects = so.GetContainedObjects();
			foreach (var containedObject in containedObjects)
				RemoveObject(containedObject);
		}
		//If is within inventory, unregister from it
		if (removeFromContainer)
		{
			StaticObject container = so.GetContainer();
			if (container)
				container.RemoveFromContainer(so);
		}
		//If something was linking to it (trap / trigger) clear link
		for (int i = 0; i < LevelData[level - 1].Objects.Length; i++)
		{
			if(LevelData[level - 1].Objects[i])
			{
				StaticObject other = LevelData[level - 1].Objects[i];
				if(other.IsTrap() || other.IsTrigger())
				{
					if (other.IsQuantity && other.Special == so.CurrentAdress)
						other.Special = 0;
				}
			}
		}
		//Call additional info
		so.OnRemove?.Invoke(so);
		LevelData[level - 1].Objects[index] = null;
		Debug.LogFormat("Removed {0}, after, start at {1}, index {2}", so.Name, listStart, LevelData[level - 1].FreeObjects[listStart]);
		return true;
	}

	public static void SetNewPosition(StaticObject so, MapTile oldTile, MapTile newTile, Vector2Int offsets)
	{
		//pos = new Vector3((pos.x / 0.6f - Mathf.Floor(pos.x / 0.6f)) * 0.7f, (pos.y / 0.6f - Mathf.Floor(pos.y / 0.6f)) * 0.7f, 0);
		//so.XPos = (int)(pos.x * 10.0f);
		//so.YPos = (int)(pos.y * 10.0f);
		so.XPos = offsets.x;
		so.YPos = offsets.y;
		so.ZPos = newTile.FloorHeight * 8;
		if (so.ObjectID == 334)	//Heckin open portcullis
			so.ZPos += 24;
		if (newTile.IsSlope())
			so.ZPos += 8;
		bool removed = false;
		bool added = false;
		if (!(oldTile == newTile))
		{
			removed = oldTile.RemoveObjectFromTile(so);
			added = newTile.AddObjectToTile(so);
		}
		GameObject obj = ObjectToGO[so];
		so.OnMoved(newTile);
		SetGOPosition(so);
		if (so.IsDoor())
			SetDoorGOPosition(so);
		//Debug.LogFormat("New pos for {0} from tile {1} to tile {2}, at pos {3}, removed : {4}, added : {5}", so.Name, oldTile.Position, newTile.Position, offsets, removed, added);
	}

	public bool AddToContainer(StaticObject so, StaticObject container)
	{
		MapTile oldTile = so.Tile;
		bool added = container.AddToContainer(so);
		if (!added)
			return false;
		oldTile.RemoveObjectFromTile(so);
		GameObject toDestroy = ObjectToGO[so];
		ObjectToGO.Remove(so);
		Destroy(toDestroy);
		return true;
	}

	/// <summary>
	/// Not only for triggers, but for 'contained' traps, objects for create object traps
	/// </summary>
	public bool AddTrigger(StaticObject so, StaticObject trigger)
	{
		so.IsQuantity = false;
		bool added = AddToContainer(trigger, so);
		if (!added)
			return false;
		trigger.XPos = 3;
		trigger.YPos = 3;
		//FIXME : add qual & ownr for target X & Y?
		return true;
	}

	public static void ActivateMob(MobileObject mo, int level)
	{
		mo.ActiveMob = true;
		mo.MobListIndex = LevelData[level - 1].ActiveMobs;

		LevelData[level - 1].MobList[LevelData[level - 1].ActiveMobs] = mo.CurrentAdress;
		LevelData[level - 1].ActiveMobs++;
	}

	public static void DeactivateMob(MobileObject mo, int level)
	{
		LevelData[level - 1].MobList[mo.MobListIndex] = LevelData[level - 1].MobList[LevelData[level - 1].ActiveMobs];
		LevelData[level - 1].ActiveMobs--;

		mo.ActiveMob = false;
		mo.MobListIndex = -1;
	}

	#endregion

	#region GameObjects

	public static GameObject SpawnStartGO(Transform parent, Vector2Int mapPos)
	{
		GameObject obj = Instantiate(StaticObjectPrefabSt, parent);
		obj.name = "StartObject";
		SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
		sr.sprite = GetObjectSpriteFromID(127);
		sr.sortingOrder = 1;
		float startH = GetTileHeight( LevelData[0].MapTiles[mapPos.x, mapPos.y]);
		SetGOPosition(obj, mapPos, new Vector2Int(3, 3), startH);
		GameObject col = obj.transform.Find("Collider").gameObject;
		Destroy(col);
		return obj;
	}

	public static GameObject SpawnGO(StaticObject staticObject, GameObject parent)
	{
		GameObject obj = Instantiate(StaticObjectPrefabSt, parent.transform);
		ObjectToGO[staticObject] = obj;
		SetGOSprite(staticObject);
		SetGOPosition(staticObject);
		SetGODirection(staticObject);
		obj.GetComponent<StaticObjectScript>().StaticObject = staticObject;
		obj.name = staticObject.Name;
		obj.layer = 11;
		for (int i = 0; i < obj.transform.childCount; i++)
			obj.transform.GetChild(i).gameObject.layer = 11;
		return obj;
	}

	public static bool SetGOSprite(StaticObject so)
	{
		if (!ObjectToGO.ContainsKey(so))
			return false;
		GameObject go = ObjectToGO[so];
		SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
		sr.sprite = GetObjectSpriteFromID(so);
		sr.sortingOrder = 1;
		SpriteOutline outline = go.GetComponent<SpriteOutline>();
		outline.CreateOutline();
		return true;
	}

	public static bool SetGOPosition(StaticObject so)
	{
		if (!ObjectToGO.ContainsKey(so))
			return false;
		GameObject go = ObjectToGO[so];
		float nx = so.MapPosition.x * 0.60f + (so.XPos / 7.0f) * 0.60f;
		float ny = so.MapPosition.y * 0.60f + (so.YPos / 7.0f) * 0.60f;
		float nz = GetObjectHeight(so);
		go.transform.position = new Vector3(nx, ny, nz);
		return true;
	}

	public static void SetGOPosition(GameObject go, Vector2Int mapPos, Vector2Int tilePos)
	{
		float nx = mapPos.x * 0.60f + (tilePos.x / 7.0f) * 0.60f;
		float ny = mapPos.y * 0.60f + (tilePos.y / 7.0f) * 0.60f;
		go.transform.position = new Vector3(nx, ny, go.transform.position.z);
	}

	public static void SetGOPosition(GameObject go, Vector2Int tilePos, Vector2Int offsets, float height)
	{
		float nx = tilePos.x * 0.60f + (offsets.x / 7.0f) * 0.60f;
		float ny = tilePos.y * 0.60f + (offsets.y / 7.0f) * 0.60f;
		go.transform.position = new Vector3(nx, ny, height);
	}

	public static bool SetDoorGOPosition(StaticObject so)
	{
		if (!ObjectToGO.ContainsKey(so))
			return false;
		//Debug.Log("Set door go position");
		GameObject go = ObjectToGO[so];
		StaticObjectScript sos = go.GetComponent<StaticObjectScript>();
		if (sos.DoorContainerObject)
		{
			Destroy(sos.DoorContainerObject);
			CreateDoorGO(so, go.transform, so.CurrentLevel);
		}
		return true;
	}
	public static bool SetGODirection(StaticObject so, GameObject obj = null)
	{
		if (!ObjectToGO.ContainsKey(so))
			return false;
		if (obj == null)
			obj = ObjectToGO[so];
		GameObject dirObj = obj.transform.GetChild(2).gameObject;	//Should be Transform.Find
		if (so.IsItem() || so.IsTrigger())
			dirObj.SetActive(false);
		else if(so.IsTrap() && so.ObjectID != 386)
			dirObj.SetActive(false);
		else
		{
			dirObj.transform.localScale = new Vector3(1.75f, 1.75f, 1.0f);
			dirObj.transform.localEulerAngles = new Vector3(0, 0, GetObjectDirection(so));
		}
		if(so.IsDoor())
		{
			StaticObjectScript sos = obj.GetComponent<StaticObjectScript>();
			if(sos && sos.DoorContainerObject)
				sos.DoorContainerObject.transform.localEulerAngles = new Vector3(0, 0, GetObjectDirection(so));
		}
		return true;
	}

	public static bool SetGOHeight(StaticObject so)
	{
		if (!ObjectToGO.ContainsKey(so))
			return false;
		GameObject go = ObjectToGO[so];
		go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, GetObjectHeight(so));
		return true;
	}

	public static Vector2Int GetOffsetsFromWorldPos(Vector3 pos)
	{
		int x = (int)((pos.x / 0.6f - Mathf.Floor(pos.x / 0.6f)) * 10.0f);
		int y = (int)((pos.y / 0.6f - Mathf.Floor(pos.y / 0.6f)) * 10.0f);
		return new Vector2Int(x, y);
	}


	public static GameObject CreateDoorGO(StaticObject so, Transform parent, int level)
	{
		GameObject obj = new GameObject("DoorContainer");
		obj.transform.SetParent(parent);
		obj.transform.localPosition = Vector3.zero;
		StaticObjectScript sos = parent.GetComponent<StaticObjectScript>();

		float objHei = GetObjectHeight(so);
		//float floorZ = so.ObjectID >= 328 ? 0.4f : 0;
		if (so.ObjectID == 334)
			objHei += 0.30f;
		sos.DoorFrameObject = CreateDoorMeshGO(so, obj.transform, level, GetWallTextureFromIndex(so.Tile.WallTexture, level), () => CreateDoorFrameMesh(TileSize, 0, -CeilingZ - objHei), "DoorFrame");
		Texture2D doorTex = so.GetDoorTexture();
		if (doorTex)
			sos.DoorObject = CreateDoorMeshGO(so, obj.transform, level, doorTex, () => CreateDoorMesh(TileSize, 0), "Door");
		obj.transform.localEulerAngles = new Vector3(0, 0, GetObjectDirection(so));
		sos.DoorContainerObject = obj;
		if (so.ObjectID == 334)
			obj.transform.Translate(new Vector3(0, 0, 0.30f), Space.Self);
		return obj;
	}

	public static GameObject CreateVerticalTextureGO(StaticObject so, Transform parent, int level)
	{
		GameObject vertTexGO = new GameObject("VerticalTexture");
		vertTexGO.transform.SetParent(parent);
		vertTexGO.transform.localPosition = Vector3.zero;
		vertTexGO.transform.localScale = new Vector3(0.5f, 0.5f);
		SpriteRenderer sr = vertTexGO.AddComponent<SpriteRenderer>();
		sr.material = parent.GetComponent<SpriteRenderer>().material;
		sr.sortingLayerName = "Items";
		Texture2D tex = TextureData.Walls.Textures[LevelData[so.CurrentLevel - 1].WallTextures[so.Owner]];
		sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64.0f, 64.0f), new Vector2(0.5f, 0.5f));
		return vertTexGO;
	}

	public static GameObject CreateDoorMeshGO(StaticObject so, Transform parent, int level, Texture2D tex, Func<Mesh> meshFunc, string name)
	{
		//Debug.LogFormat("CreateDoorMeshGO");
		GameObject obj = new GameObject(name);
		obj.transform.SetParent(parent);
		obj.transform.localPosition = Vector3.zero;
		MeshFilter mesh = obj.AddComponent<MeshFilter>();
		//float objHei = GetObjectHeight(so);
		//mesh.mesh = CreateDoorFrameMesh(TileSize, 0, -CeilingZ - objHei);
		mesh.mesh = meshFunc();

		MeshRenderer mr = obj.AddComponent<MeshRenderer>();
		mr.material = tileMaterial;
		//mr.material.mainTexture = GetWallTextureFromIndex(so.Tile.WallTexture, level);
		mr.material.mainTexture = tex;
		mr.material.SetColor("_Color", GetHeightColor(so.Tile));
		mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
		mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;


		return obj;
	}

	#endregion

	#region Info
	//FIXME : add more tile types + BUG
	public Tuple<Vector2Int, int> GetNearestWall(MapTile tile, Vector2Int tilePos, int level)
	{
		Debug.LogFormat("Initial tilePos : {0}", tilePos);
		Vector2 relPos = tilePos - new Vector2(3.5f, 3.5f);
		Debug.LogFormat("relPos : {0}", relPos);
		if (Mathf.Abs(relPos.x) >= Mathf.Abs(relPos.y))
		{
			if (relPos.x > 0 && tile.Position.x < 63)
			{
				if (LevelData[level - 1].MapTiles[tile.Position.x + 1, tile.Position.y].TileType == TileType.Solid)
					return new Tuple<Vector2Int, int>(new Vector2Int(7, tilePos.y), 2);
				else if(LevelData[level - 1].MapTiles[tile.Position.x - 1, tile.Position.y].TileType == TileType.Solid)
					return new Tuple<Vector2Int, int>(new Vector2Int(0, tilePos.y), 6);
			}
			else if(relPos.x < 0 && tile.Position.x >= 0)
			{
				if (LevelData[level - 1].MapTiles[tile.Position.x - 1, tile.Position.y].TileType == TileType.Solid)
					return new Tuple<Vector2Int, int>(new Vector2Int(0, tilePos.y), 6);
				else if (LevelData[level - 1].MapTiles[tile.Position.x + 1, tile.Position.y].TileType == TileType.Solid)
					return new Tuple<Vector2Int, int>(new Vector2Int(7, tilePos.y), 2);
			}

		}
		else
		{
			if (relPos.y > 0 && tile.Position.y < 63)
			{
				if (LevelData[level - 1].MapTiles[tile.Position.x, tile.Position.y + 1].TileType == TileType.Solid)
					return new Tuple<Vector2Int, int>(new Vector2Int(tilePos.x, 7), 0);
				else if (LevelData[level - 1].MapTiles[tile.Position.x, tile.Position.y - 1].TileType == TileType.Solid)
					return new Tuple<Vector2Int, int>(new Vector2Int(tilePos.x, 0), 4);
			}
			else if(relPos.y < 0 && tile.Position.y >= 0)
			{
				if (LevelData[level - 1].MapTiles[tile.Position.x, tile.Position.y - 1].TileType == TileType.Solid)
					return new Tuple<Vector2Int, int>(new Vector2Int(tilePos.x, 0), 4);
				else if (LevelData[level - 1].MapTiles[tile.Position.x, tile.Position.y + 1].TileType == TileType.Solid)
					return new Tuple<Vector2Int, int>(new Vector2Int(tilePos.x, 7), 0);
			}
		}
		return new Tuple<Vector2Int, int>(tilePos, 0);
	}

	public GameObject GetObjectUnderMouse(EditorMode mode = EditorMode.Null)
	{
		return GetObjectUnderMouse(Input.mousePosition, mode);
	}

	public GameObject GetObjectUnderMouse(Vector3 pos, EditorMode mode = EditorMode.Null)
	{
		int layers = 0;
		if (mode == EditorMode.Object)
			layers = 1 << 11;
		else if (mode == EditorMode.Tile || mode == EditorMode.Texture || mode == EditorMode.Sector)
			layers = 1 << 10;
		else if (mode == EditorMode.Null)
			layers = (1 << 10) | (1 << 11);

		Ray ray = Camera.main.ScreenPointToRay(pos);
		RaycastHit hitInfo = new RaycastHit();
		bool hit = Physics.Raycast(ray, out hitInfo, float.MaxValue, layers);
		//Debug.LogFormat("GOUM, layers : {0}, hit : {1}", layers, hit);
		if (hit)
		{
			return hitInfo.transform.gameObject;
		}
		return null;
	}	

	public Vector3 GetWorldPosition(Vector3 from, bool onlyBase, bool getTilePos = false)
	{
		int layers = (onlyBase == true) ? (1 << 9) : (1 << 9) | (1 << 10);
		Vector3 hitPos = GetPosition(from, layers);
		if(hitPos != Vector3.zero)
		{
			if(getTilePos)
			{
				Vector3 tilePos = new Vector3(Mathf.Floor(hitPos.x / 0.6f), Mathf.Floor(hitPos.y / 0.6f), 0);
				return tilePos;
			}
		}
		return hitPos;
	}

	public Vector3 GetPositionFromCenter(bool onlyBase, bool getTilePos = false)
	{
		int layers = (onlyBase == true) ? (1 << 9) : (1 << 9) | (1 << 10);
		Vector3 center = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f, 0.0f);
		Vector3 hitPos = GetPosition(center, layers);
		if (hitPos != Vector3.zero)
		{
			if (getTilePos)
			{
				Vector3 tilePos = new Vector3(Mathf.Floor(hitPos.x / 0.6f), Mathf.Floor(hitPos.y / 0.6f), 0);
				return tilePos;
			}
		}
		return hitPos;
	}

	public Vector3 GetPosition(Vector3 from, int layers)
	{
		Ray ray = Camera.main.ScreenPointToRay(from);
		RaycastHit hitInfo = new RaycastHit();
		bool hit = false;
		hit = Physics.Raycast(ray, out hitInfo, float.MaxValue, layers);
		if (hit)
			return hitInfo.point;
		else
			return new Vector3(-1.0f, -1.0f);
	}


	public static int GetNextFreeStaticIndex(int level)
	{
		Debug.LogFormat("level : {0}, StaticListStart : {1}", level, LevelData[level - 1].StaticListStart);
		return LevelData[level - 1].FreeObjects[LevelData[level - 1].StaticListStart + 254];
	}

	public static int GetNextFreeMobileIndex(int level)
	{
		return LevelData[level - 1].FreeObjects[LevelData[level - 1].MobileListStart];
	}

	public static Sprite GetObjectSpriteFromID(StaticObject so)
	{
		if (so.IsVerticalTexture())
			return Sprite.Create(GetWallTextureFromIndex(so.Owner, so.CurrentLevel), new Rect(0, 0, 64.0f, 64.0f), new Vector2(0.5f, 0.5f), 250);
		else if (so.IsWriting())
			return Sprite.Create(TextureData.Other.Textures[20 + so.Flags], new Rect(0, 0, 16.0f, 16.0f), new Vector2(0.5f, 0.5f));
		else if (so.ObjectID == 353)    //Dial lever 1
			return Sprite.Create(TextureData.Other.Textures[4 + so.Flags], new Rect(0, 0, 16.0f, 16.0f), new Vector2(0.5f, 0.5f));
		else if (so.ObjectID == 354)	//Dial lever 2
			return Sprite.Create(TextureData.Other.Textures[12 + so.Flags], new Rect(0, 0, 16.0f, 16.0f), new Vector2(0.5f, 0.5f));
		return GetObjectSpriteFromID(so.ObjectID);
	}

	public static Sprite GetObjectSpriteFromID(int id)
	{
		if (id >= 368 && id < 384)      //Levers
			return Sprite.Create(TextureData.Levers.Textures[id - 368], new Rect(0, 0, 16.0f, 16.0f), new Vector2(0.5f, 0.5f));
		else if (id == 353)             //Dial lever 1
			return Sprite.Create(TextureData.Other.Textures[4], new Rect(0, 0, 16.0f, 16.0f), new Vector2(0.5f, 0.5f));
		else if (id == 354)             //Dial lever 2
			return Sprite.Create(TextureData.Other.Textures[12], new Rect(0, 0, 16.0f, 16.0f), new Vector2(0.5f, 0.5f));
		else if (id >= 465 && id < 470) //Additional DO traps
			return GetSpriteFromTexture(TextureData.Objects.Textures[387]);
		return GetSpriteFromTexture(TextureData.Objects.Textures[id]);
	}
	public static ObjectType GetTypeFromOtherSpriteIndex(int index)
	{
		if (index >= 20 && index < 28)
			return ObjectType.Writing;
		else if (index >= 4 && index < 12)
			return ObjectType.DialLever;
		else if (index >= 12 && index < 20)
			return ObjectType.DialLever;
		return ObjectType.Unknown;
	}
	public static Sprite GetObjectSpriteFromResources(int id)
	{
		return Sprites[id];
	}
	public static Sprite GetSpriteFromTexture(Texture2D tex)
	{
		return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
	}
	public static int GetAuxPalette(int id)
	{
		return TextureData.Objects.AuxPalettes[id];
	}
	public static Texture2D GetTexture(TextureType texType, int index)
	{
		if (texType == TextureType.Floor)
			return TextureData.Floors.Textures[index];
		else if (texType == TextureType.Wall)
			return TextureData.Walls.Textures[index];
		else if (texType == TextureType.Door)
			return TextureData.Doors.Textures[index];
		else if (texType == TextureType.Lever)
			return TextureData.Levers.Textures[index];
		else if (texType == TextureType.Other)
			return TextureData.Other.Textures[index];
		else if (texType == TextureType.GenericHead)
			return TextureData.GenHeads.Textures[index];
		else if (texType == TextureType.NPCHead)
			return TextureData.NPCHeads.Textures[index];
		else if (texType == TextureType.Object)
			return TextureData.Objects.Textures[index];
		return null;
	}
	public static Texture2D GetFloorTextureFromIndex(int index, int level)
	{
		//Debug.LogFormat("index : {0}, level : {1}", index, level);
		index = LevelData[level - 1].FloorTextures[index];
		return TextureData.Floors.Textures[index];
	}
	public static Texture2D GetWallTextureFromIndex(int index, int level)
	{
		//Debug.LogFormat("index : {0}, level : {1}", index, level);
		index = LevelData[level - 1].WallTextures[index];
		return TextureData.Walls.Textures[index];
	}
	public static Texture2D GetDoorTextureFromIndex(int index, int level)
	{
		index = LevelData[level - 1].DoorTextures[index];
		return TextureData.Doors.Textures[index];
	}
	public static int GetLevelTextureIndex(TextureType texType, int level, int i)
	{
		if (texType == TextureType.Floor)
			return LevelData[level - 1].FloorTextures[i];
		else if (texType == TextureType.Wall)
			return LevelData[level - 1].WallTextures[i];
		else if (texType == TextureType.Door)
			return LevelData[level - 1].DoorTextures[i];

		return -1;
	}
	public static float GetTileHeight(MapTile tile)
	{
		return (float)tile.FloorHeight * -StepZ;
	}

	public static float GetObjectHeight(StaticObject so)
	{
		return so.ZPos / 8.0f * -StepZ;
	}

	public static Color GetHeightColor(MapTile tile)
	{
		float c = tile.FloorHeight * 0.065f + 0.3f;
		return new Color(c, c, c);
	}

	public static float GetObjectDirection(StaticObject so)
	{
		return so.Direction * -45.0f;
	}

	public MapTile GetTile(Vector3 pos, int level)
	{
		if (pos.x < 0 || pos.x > 63 || pos.y < 0 || pos.y > 63)
			return null;

		return LevelData[level - 1].MapTiles[(int)pos.x, (int)pos.y];
	}

	public MapTile GetTile(int x, int y, int level)
	{
		if (x < 0 || x > 63 || y < 0 || y > 63)
			return null;

		return LevelData[level - 1].MapTiles[x, y];
	}

	public static Sector CreateNewSector(int levelIndex, string name)
	{
		LevelData lvl = LevelData[levelIndex];
		//lvl.Sectors.Add(new Sector(name));
		Sector sec = lvl.CreateNewSector(name);
		return sec;
	}

	public static Dictionary<string, int> GetNPCIDs()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		for (int i = 0; i < 256; i++)
		{
			if(ConversationData.Conversations[i] != null)
			{
				dict[StringData.GetNPCName(i)] = i;
			}
		}
		return dict;
	}

	public int GetLevelCount()
	{
		return LevelData.Count;
	}

	//public TileType[] GetTileTypes()
	//{
	//	return new TileType[] { TileType.Solid, TileType.Open, TileType.OpenSE, TileType.OpenSW, TileType.OpenNE, TileType.OpenNW, TileType.SlopeUpN, TileType.SlopeUpS, TileType.SlopeUpE, TileType.SlopeUpW };
	//}
	public string[] GetTileTypes()
	{
		return new string[] { "Solid", "Open", "Open S-E", "Open S-W", "Open N-E", "Open N-W", "Slope N", "Slope S", "Slope E", "Slope W" };
	}

	#endregion
}
