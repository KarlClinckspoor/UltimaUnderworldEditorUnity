using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class SavedStatic
{
	public int CurrentAdress;
	public int CurrentLevel;
	public string Name;
	public int ObjectID;
	public int Flags;
	public bool IsEnchanted;
	public bool IsDoorOpen;
	public bool IsInvisible;
	public bool IsQuantity;
	public int XPos;
	public int YPos;
	public int ZPos;
	public int Direction;
	public int Quality;
	public int NextAdress;
	public int PrevAdress;
	public int Owner;
	public int Special;
	public AnimationOverlay Animation;

	public SavedStatic(StaticObject so)
	{
		CurrentAdress = so.CurrentAdress;
		CurrentLevel = so.CurrentLevel;
		Name = so.Name;
		ObjectID = so.ObjectID;
		Flags = so.Flags;
		IsEnchanted = so.IsEnchanted;
		IsDoorOpen = so.IsDoorOpen;
		IsInvisible = so.IsInvisible;
		IsQuantity = so.IsQuantity;
		XPos = so.XPos;
		YPos = so.YPos;
		ZPos = so.ZPos;
		Direction = so.Direction;
		Quality = so.Quality;
		NextAdress = so.NextAdress;
		PrevAdress = so.PrevAdress;
		Owner = so.Owner;
		Special = so.Special;
	}
}

public class SavedMobile : SavedStatic
{
	public int HP;      
	public int Goal;    
	public int GTarg;   
	public int Level;   
	public int Attitude;
	public int MobHeight;
	public int YHome;   
	public int XHome;   
	public int Heading; 
	public int Hunger;  
	public int Whoami;  
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

	public SavedMobile(MobileObject mo) : base(mo)
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
}

