using System;
using System.Collections.Generic;

public class LevelData {

	public int Level;
	public MapTile[,] MapTiles;
	public StaticObject[] Objects;
	public int[] FloorTextures;
	public int[] WallTextures;
	public int[] DoorTextures;
	public int[] FreeObjects;
	public int[] MobList;
	public int MobileListStart;
	public int StaticListStart;
	public int ActiveMobs;
	public AnimationOverlay[] AnimationOverlays;

	//Randomizer stuff
	public List<Sector> Sectors;
	public event Action<Sector, int> OnSectorAdded;
	public event Action<Sector, int> OnSectorRemoved;

	public LevelData(int level, MapTile[,] mapTiles, StaticObject[] objects, int[] floorTextures, int[] wallTextures, int[] doorTextures, int[] freeObjects, int[] mobList, int mobileStart, int staticStart, int activeMobs, AnimationOverlay[] anims)
	{
		Level = level;
		MapTiles = mapTiles;
		Objects = objects;
		FloorTextures = floorTextures;
		WallTextures = wallTextures;
		DoorTextures = doorTextures;
		FreeObjects = freeObjects;
		MobList = mobList;
		MobileListStart = mobileStart;
		StaticListStart = staticStart;
		ActiveMobs = activeMobs;
		AnimationOverlays = anims;

		Sectors = new List<Sector>();
	}

	public Sector CreateNewSector(string name)
	{
		if (Sectors.Find(s => s.Name == name) != null)
			return null;
		Sector sec = new Sector(name, Level);
		Sectors.Add(sec);
		OnSectorAdded?.Invoke(sec, Level);
		return sec;
	}

	public Sector GetSector(string name) => Sectors.Find(s => s.Name == name);

	public bool RemoveSector(string name)
	{
		Sector sec = Sectors.Find(s => s.Name == name);
		if (sec == null)
			return false;
		Sectors.Remove(sec);
		OnSectorRemoved(sec, Level);		
		return true;
	}

	public void LoadLevel(SavedLevel sl)
	{
		MapTiles = new MapTile[64, 64];
		for (int x = 0; x < 64; x++)
			for (int y = 0; y < 64; y++)
				MapTiles[x, y] = new MapTile(sl.Tiles[x, y]);
		Objects = new StaticObject[1024];
		for (int i = 0; i < sl.Objects.Length; i++)
		{
			SavedStatic ss = sl.Objects[i];
			if(ss != null)
			{
				if (ss is SavedMobile)
					Objects[i] = new MobileObject(ss as SavedMobile);
				else
					Objects[i] = new StaticObject(ss);
			}
		}
		AnimationOverlays = new AnimationOverlay[sl.Anims.Length];
		for (int i = 0; i < sl.Anims.Length; i++)
		{
			AnimationOverlays[i] = new AnimationOverlay()
			{
				Unk1 = sl.Anims[i].Unk1,
				Adress = sl.Anims[i].Adress,
				Duration = sl.Anims[i].Duration,
				X = sl.Anims[i].X,
				Y = sl.Anims[i].Y
			};
		}
		foreach (var anim in AnimationOverlays)
		{
			if (anim)
			{
				if (Objects[anim.Adress] != null)
					Objects[anim.Adress].Animation = anim;
			}
		}
	}
}
public class SavedLevel
{
	public int Level;
	public SavedTile[,] Tiles;
	public SavedStatic[] Objects;
	public AnimationOverlay[] Anims;

	public SavedLevel(LevelData ld)
	{
		Level = ld.Level;
		Tiles = new SavedTile[64, 64];
		for (int x = 0; x < 64; x++)
		{
			for (int y = 0; y < 64; y++)
			{
				Tiles[x, y] = new SavedTile(ld.MapTiles[x, y]);
			}
		}
		Objects = new SavedStatic[ld.Objects.Length];
		for (int i = 0; i < ld.Objects.Length; i++)
		{
			StaticObject so = ld.Objects[i];
			if (so)
			{
				if (so is MobileObject)
					Objects[i] = new SavedMobile(so as MobileObject);
				else
					Objects[i] = new SavedStatic(so);
			}
		}
		Anims = new AnimationOverlay[ld.AnimationOverlays.Length];
		for (int i = 0; i < ld.AnimationOverlays.Length; i++)
		{
			if (ld.AnimationOverlays[i] != null)
			{
				Anims[i] = new AnimationOverlay()
				{
					Unk1 = ld.AnimationOverlays[i].Unk1,
					Adress = ld.AnimationOverlays[i].Adress,
					Duration = ld.AnimationOverlays[i].Duration,
					X = ld.AnimationOverlays[i].X,
					Y = ld.AnimationOverlays[i].Y
				};
			}
		}
	}	
}

public class AnimationOverlay
{
	public int Unk1;
	public int Adress;
	public int Duration;
	public int X;
	public int Y;



	public static implicit operator bool(AnimationOverlay t)
	{
		if (t == null)
			return false;
		return true;
	}

	public override string ToString()
	{
		return string.Format("Adress : {0}, Duration : {1}, X : {2}, Y : {3}", Adress, Duration, X, Y);
	}
}
