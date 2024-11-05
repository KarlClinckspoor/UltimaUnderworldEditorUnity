using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileObject : StaticObject {

	public int HP;      //8
	//Stand - 0, 7, 11, 12
	//Move - 1
	//Wander - 2, 4, 8
	//Attack - 5, 9
	//Flee - 6
	//Follow - 3
	//Talk - 10
	//Petrified - 15
	//13, 14 - ?
	public int Goal;    //B
	public int GTarg;	//B+C
	public int Level;   //D

	//0 - hostile, 1 - upset, 2 - mellow, 3 - friendly
	public int Attitude;//E
	public int MobHeight;//F
	public int YHome;	//16
	public int XHome;	//16
	public int Heading;	//18
	public int Hunger;	//19
	public int Whoami;	//1A

	public int B9;
	public int BA;
	public int BB;
	public int BC;
	public int BD;
	public int BE;
	public int BF;
	public int B10;
	public int B11;
	public int B12;
	public int B13;
	public int B14;
	public int B15;
	public int B16;
	public int B17;
	public int B18;
	public int B19;
	public int B1A;

	public bool ActiveMob;
	public int MobListIndex;

	public MobileObject() { }

	public MobileObject(MapTile tile) : this(tile, 64, 3, 3) { }
	
	public MobileObject(MapTile tile, int id, int x, int y)
	{
		ObjectID = id;
		XPos = x;
		YPos = y;

		ZPos = tile.FloorHeight * 8;
		Tile = tile;
		MapPosition = tile.Position;
		Name = GetName(ObjectID);
		CurrentLevel = tile.Level;

		HP = GetDefaultHP(id);
		Goal = 4;
		XHome = tile.Position.x;
		YHome = tile.Position.y;
		Quality = tile.Position.x;
		Owner = tile.Position.y;
		//Temporary - unknown meaning but 90% of UW creatures have these.
		B14 = 132;
		B15 = 32;
		//
	}

	public MobileObject(MapTile tile, int x, int y, MobileObject copy) : this(copy)
	{
		XPos = x;
		YPos = y;

		ZPos = tile.FloorHeight * 8;
		Tile = tile;
		MapPosition = tile.Position;
		CurrentLevel = tile.Level;

		XHome = tile.Position.x;
		YHome = tile.Position.y;
		Quality = tile.Position.x;
		Owner = tile.Position.y;
	}

	public MobileObject(MobileObject copy) : base(copy)
	{
		HP = copy.HP;

		Goal = copy.Goal;
		GTarg = copy.GTarg;
		Level = copy.Level;

		Attitude = copy.Attitude;
		YHome = copy.YHome;
		XHome = copy.XHome;
		Heading = copy.Heading;
		Hunger = copy.Hunger;
		Whoami = copy.Whoami;

		B9 = copy.B9;
		BA = copy.BA;
		BB = copy.BB;
		BC = copy.BC;
		BD = copy.BD;
		BE = copy.BE;
		BF = copy.BF;
		B10 = copy.B10;
		B11 = copy.B11;
		B12 = copy.B12;
		B13 = copy.B13;
		B14 = copy.B14;
		B15 = copy.B15;
		B16 = copy.B16;
		B17 = copy.B17;
		B18 = copy.B18;
		B19 = copy.B19;
		B1A = copy.B1A;
	}

	public MobileObject(SavedMobile mo) : base(mo)
	{
		HP = mo.HP;
		Goal = mo.Goal;
		GTarg = mo.GTarg;
		Level = mo.Level;
		Attitude = mo.Attitude;
		MobHeight = mo.MobHeight;
		YHome = mo.YHome;
		XHome = mo.XHome;
		Heading = mo.Heading;
		Hunger = mo.Hunger;
		Whoami = mo.Whoami;
		B9 = mo.B9;
		BA = mo.BA;
		BB = mo.BB;
		BC = mo.BC;
		BD = mo.BD;
		BE = mo.BE;
		BF = mo.BF;
		B10 = mo.B10;
		B11 = mo.B11;
		B12 = mo.B12;
		B13 = mo.B13;
		B14 = mo.B14;
		B15 = mo.B15;
		B16 = mo.B16;
		B17 = mo.B17;
		B18 = mo.B18;
		B19 = mo.B19;
		B1A = mo.B1A;
	}

	public override void OnMoved(MapTile newTile)
	{
		Tile = newTile;
		Quality = Tile.Position.x;
		Owner = Tile.Position.y;
		XHome = Tile.Position.x;
		YHome = Tile.Position.y;
	}

	public override string GetFullName()
	{
		if (Whoami > 0)
			return string.Format("{0} ({1})", GetNPCName(), GetName());
		return GetName();
	}

	public string GetNPCName()
	{
		if (Whoami == 0)
			return "INVALID";
		return MapCreator.StringData.GetNPCName(Whoami);
	}

	public static string[] GetAttitudes()
	{
		string[] atts = new string[4];
		atts[0] = "Hostile";
		atts[1] = "Upset";
		atts[2] = "Mellow";
		atts[3] = "Friendly";
		return atts;
	}

	public static string[] GetGoals()
	{
		string[] goals = new string[13];
		goals[0] = "Stand (0)";
		goals[1] = "Go to...";
		goals[2] = "Wander (2)";
		goals[3] = "Follow target";
		goals[4] = "Wander (4)";
		goals[5] = "Attack target";
		goals[6] = "Flee target";
		goals[7] = "Stand (7)";
		goals[8] = "Wander(8)";
		goals[9] = "Attack target";
		goals[10] = "Awaiting conversation";
		goals[11] = "Stand (11)";
		goals[12] = "Stand (12)";
		return goals;
	}
	public static int GetDefaultHP(int monsterId)
	{
		if (monsterId >= 64)
			monsterId -= 64;
		return MapCreator.ObjectData.MonsterData[monsterId].Health;
	}

	public override int GetFileIndex()
	{
		return DataReader.LevelOffsets[CurrentLevel - 1] + 16384 + CurrentAdress * 27;
	}
}
