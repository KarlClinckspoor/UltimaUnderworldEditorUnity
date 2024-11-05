using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureData {

	public List<Color[]> Palettes;
	public List<int[]> AuxPalettesRaw;
	public List<Color[]> AuxPalettes;

	public TextureContainer Floors;
	public int[] FloorTypes;
	public TextureContainer Walls;
	public int[] WallTypes;
	public TextureContainer Doors;
	public TextureContainer Levers;
	public TextureContainer Other;

	public TextureContainer GenHeads;
	public TextureContainer NPCHeads;

	public TextureContainer Objects;

	public TextureData(List<Color[]> pals, List<int[]> auxPalsRaw, List<Color[]> auxPals, TextureContainer floors, TextureContainer walls, TextureContainer doors, TextureContainer levers, TextureContainer other, TextureContainer genHeads, TextureContainer npcHeads, TextureContainer objs, int[] wallDat, int[] florDat)
	{
		Palettes = pals;
		AuxPalettesRaw = auxPalsRaw;
		AuxPalettes = auxPals;

		Floors = floors;
		FloorTypes = florDat;
		Walls = walls;
		WallTypes = wallDat;
		Doors = doors;
		Levers = levers;
		Other = other;

		GenHeads = genHeads;
		NPCHeads = npcHeads;

		Objects = objs;
	}

	public string[] GetAuxPalettes()
	{
		string[] apals = new string[AuxPalettes.Count];
		for (int i = 0; i < AuxPalettes.Count; i++)
			apals[i] = "Palette " + (i + 1);
		return apals;
	}
	public Dictionary<string, int> GetTerrainTypes()
	{
		Dictionary<string, int> dict = new Dictionary<string, int>();
		dict[GetTerrainType(0)] = 0;
		dict[GetTerrainType(2)] = 2;
		dict[GetTerrainType(3)] = 3;
		dict[GetTerrainType(4)] = 4;
		dict[GetTerrainType(5)] = 5;
		dict[GetTerrainType(6)] = 6;
		dict[GetTerrainType(7)] = 7;
		dict[GetTerrainType(8)] = 8;
		dict[GetTerrainType(9)] = 9;
		dict[GetTerrainType(10)] = 10;
		dict[GetTerrainType(11)] = 11;
		dict[GetTerrainType(16)] = 16;
		dict[GetTerrainType(32)] = 32;
		return dict;
	}
	public string GetTerrainType(int id)
	{
		switch (id)
		{
			case 0:
				return "Normal";
			case 2:
				return "Ankh";
			case 3:
				return "Stairs up";
			case 4:
				return "Stairs down";
			case 5:
				return "Pipe";
			case 6:
				return "Grating";
			case 7:
				return "Drain";
			case 8:
				return "Princess";
			case 9:
				return "Window";
			case 10:
				return "Tapestry";
			case 11:
				return "Door";
			case 16:
				return "Water";
			case 32:
				return "Lava";
			default:
				return "INVALID";
		}
	}
}

public class TextureContainer
{
	public List<Texture2D> Textures;
	public List<int[]> RawTextures;
	public List<int> AuxPalettes;
	public int FirstOffset;
	public int ATIOffset;
	public AdditionalTextureInfo ATI;

	public int Count { get { return Textures.Count; } }
	public int Offset { get { return ATI.Width * ATI.Height + ATIOffset; } }

	public List<string> origData = new List<string>();
	public List<string> newData = new List<string>();

	public TextureContainer(List<Texture2D> t, int first, int atio, AdditionalTextureInfo ati)
	{
		Textures = t;
		FirstOffset = first;
		ATIOffset = atio;
		ATI = ati;
	}
}


public class AdditionalTextureInfo
{
	public bool Write;
	public int Width;
	public int Height;
	public int AuxPalette;
	public int SizeA;
	public int SizeB;

	public AdditionalTextureInfo(bool write, int w, int h, int sa, int sb)
	{
		Write = write;
		Width = w;
		Height = h;
		SizeA = sa;
		SizeB = sb;
	}
}