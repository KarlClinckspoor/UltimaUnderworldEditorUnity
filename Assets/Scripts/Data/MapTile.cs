using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTile {

	public Vector2Int Position;

	public int Level;

	public TileType TileType;
	public int FloorHeight;

	public int FloorTexture;
	public int WallTexture;

	public bool IsAntimagic;
	public bool IsDoor;

	public int ObjectAdress;

	public long FileAdress;

	public MapTile() { }
	public MapTile(TileType type, int level, int x, int y, int h, int floorTex, int wallTex)
	{
		TileType = type;
		Level = level;
		Position = new Vector2Int(x, y);
		FloorHeight = h;
		FloorTexture = floorTex;
		WallTexture = wallTex;
	}
	public MapTile(SavedTile tile)
	{
		Position = new Vector2Int(tile.X, tile.Y);
		Level = tile.Level;
		TileType = tile.TileType;
		FloorHeight = tile.FloorHeight;
		FloorTexture = tile.FloorTexture;
		WallTexture = tile.WallTexture;
		IsAntimagic = tile.IsAntimagic;
		IsDoor = tile.IsDoor;
		ObjectAdress = tile.ObjectAdress;
	}

	public StaticObject GetLastObject()
	{
		//Debug.Log("Getting last object from tile " + Position);
		if (ObjectAdress == 0)
			return null;
		//Debug.LogFormat("Object adress : {0}", ObjectAdress);
		StaticObject next = MapCreator.LevelData[Level - 1].Objects[ObjectAdress];
		StaticObject current = null;
		while (next)
		{
			current = next;
			next = next.GetNextObject();
		}
		//if (current)
		//	Debug.LogFormat("Last object in tile : {0}", current.Name);
		//else
		//	Debug.Log("Current is null");
		return current;
	}

	public List<StaticObject> GetTileObjects(System.Func<StaticObject, bool> condition = null)
	{
		if (ObjectAdress == 0)
			return null;
		List<StaticObject> objects = new List<StaticObject>();

		StaticObject next = MapCreator.LevelData[Level - 1].Objects[ObjectAdress];
		StaticObject current = null;
		while(next)
		{
			current = next;
			next = next.GetNextObject();
			if (condition != null)
			{
				if (condition(current))
					objects.Add(current);
			}
			else
				objects.Add(current);
		}
		return objects;
	}

	public void UpdateObjectsHeight(bool ignoreTrapsTrigs = true)
	{

		System.Func<StaticObject, bool> condition = null;
		if(ignoreTrapsTrigs)
		{
			condition = (so) =>
			{
				if (so.IsTrap() || so.IsTrigger())
					return false;
				return true;
			};
		}
		List<StaticObject> objects = GetTileObjects(condition);
		if (objects == null)
			return;
		//Debug.LogFormat("Objects in tile : {0}", objects.Count);
		int floorHei = FloorHeight * 8;
		foreach (var so in objects)
		{
			so.ZPos = floorHei;
			MapCreator.SetGOHeight(so);
		}
	}

	public bool AddObjectToTile(StaticObject so)
	{
		StaticObject last = GetLastObject();
		//Debug.LogFormat("Trying to add object {0} to tile {1}", so.Name, Position);
		if(last)
		{
			if (last.NextAdress != 0)
			{
				Debug.LogErrorFormat("Last object has something in next adress : {0}", last.NextAdress);
				return false;
			}
			//Debug.LogFormat("Tile has already item, last found : {0}", last.Name);
			last.NextAdress = so.CurrentAdress;
			so.PrevAdress = last.CurrentAdress;
		}
		else
		{
			ObjectAdress = so.CurrentAdress;
			so.PrevAdress = 0;
		}
		so.MapPosition = Position;
		so.Tile = this;
		return true;
	}
	public bool AddObjectToTile(StaticObject so, int tx, int ty)
	{
		if (AddObjectToTile(so))
		{
			so.XPos = tx;
			so.YPos = ty;
			return true;
		}
		else
			return false;
	}
	public bool AddObjectToTile(StaticObject so, StaticObject evaded)
	{
		int x = evaded.XPos - 1;
		if (x < 0)
			x += 2;
		int y = evaded.YPos - 1;
		if (y < 0)
			y += 2;
		return AddObjectToTile(so, x, y);
	}

	public bool RemoveObjectFromTile(StaticObject so)
	{
		//Debug.LogFormat("Tile {0} obj adress : {1}", Position, ObjectAdress);
		if (ObjectAdress == 0)
			return false;

		StaticObject next = MapCreator.LevelData[Level - 1].Objects[ObjectAdress];
		//Debug.LogFormat("Trying to remove {0} from tile {1}", so.Name, Position);
		if (next == so)
		{
			next = next.GetNextObject();
			if(next)
			{
				//This is the first object on tile.
				ObjectAdress = next.CurrentAdress;
				next.PrevAdress = 0;
			}
			else
			{
				//This is the only object on tile.
				ObjectAdress = 0;
			}
			so.NextAdress = 0;
			return true;
		}
		else
		{
			StaticObject parent = null;
			while (next)
			{
				parent = next;
				next = next.GetNextObject();
				if(next == so)
				{
					//This object is on tile, but it is not first on list.
					StaticObject child = next.GetNextObject();
					if(child)
					{
						parent.NextAdress = child.CurrentAdress;
						child.PrevAdress = parent.CurrentAdress;
					}
					else
					{
						parent.NextAdress = 0;
					}
					so.NextAdress = 0;
					return true;
				}
			}
			//Debug.Log("Object not found");
			return false;
		}	
	}

	public void SetNewTexture(TextureType texType, int index)
	{
		if (texType == TextureType.Floor)
			FloorTexture = index;
		else if (texType == TextureType.Wall)
			WallTexture = index;
	}

	public string GetInfo()
	{
		string info = "";

		info += "Map tile, position : " + Position + "\n";
		info += "Tile type : " + TileType + "\n";
		info += "Floor height : " + FloorHeight + "\n";
		info += "Floor texture : " + FloorTexture + " / " + DataReader.ToHex(FloorTexture) + "\n";
		info += "Wall texture : " + WallTexture + " / " + DataReader.ToHex(WallTexture) + "\n";
		info += "Is antimagic : " + IsAntimagic + "\n";
		info += "Is door : " + IsDoor + "\n";
		info += "Object adress : " + ObjectAdress + " / " + DataReader.ToHex(ObjectAdress) + "\n";

		return info;
	}

	public bool IsSlope()
	{
		if (TileType == TileType.SlopeUpE || TileType == TileType.SlopeUpN || TileType == TileType.SlopeUpS || TileType == TileType.SlopeUpW)
			return true;
		return false;
	}

	public bool IsOpen()
	{
		if (TileType != TileType.Solid)
			return true;
		return false;
	}

	public override string ToString()
	{
		return "Tile " + Position.ToString();
	}

	public static implicit operator bool(MapTile t)
	{
		if (t == null)
			return false;
		return true;
	}
}
