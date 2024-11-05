using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class SavedTile
{
	public int X;
	public int Y;
	public int Level;

	public TileType TileType;
	public int FloorHeight;

	public int FloorTexture;
	public int WallTexture;

	public bool IsAntimagic;
	public bool IsDoor;

	public int ObjectAdress;

	public SavedTile(MapTile tile)
	{
		X = tile.Position.x;
		Y = tile.Position.y;
		Level = tile.Level;
		TileType = tile.TileType;
		FloorHeight = tile.FloorHeight;
		FloorTexture = tile.FloorTexture;
		WallTexture = tile.WallTexture;
		IsAntimagic = tile.IsAntimagic;
		IsDoor = tile.IsDoor;
		ObjectAdress = tile.ObjectAdress;
	}
}

