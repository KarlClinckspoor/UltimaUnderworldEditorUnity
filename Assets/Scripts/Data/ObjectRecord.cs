using UltimateEditor.Uw1;

/// <summary>
/// An object's record bytes (8 for a static object, 0x1B for a mobile one) and the editor's
/// StaticObject / MobileObject fields, through the one field table, Uw1Schemas (include/object.h;
/// both games share the layout). The level reader and writer, the field inspector and the
/// properties panel's ranges all use this, where before each had its own shifts and masks and
/// they had drifted apart (review U9 / U16).
/// </summary>
public static class ObjectRecord
{
	public const int StaticSize = 8, MobileSize = 0x1B;

	static readonly RecordSchema S = Uw1Schemas.StaticObject, M = Uw1Schemas.MobileObject;

	public static readonly FieldDef Id = S["Item id"];
	/// <summary>The three flag bits 9-11 the editor keeps as one number.</summary>
	public static readonly FieldDef Flags = new FieldDef("Flags", 0, 9, 3, "Flag bits 9-11.");
	public static readonly FieldDef Enchanted = S["Enchanted"];
	/// <summary>Bit 13 (the editor's "door open"; include/object.h ID_DOORDIR).</summary>
	public static readonly FieldDef DoorDir = S["Tenacious"];
	public static readonly FieldDef Invisible = S["Invisible"];
	public static readonly FieldDef IsQuantity = S["Is quantity"];
	public static readonly FieldDef Z = S["Z"];
	public static readonly FieldDef Heading = S["Heading"];
	public static readonly FieldDef FineY = S["Fine Y"];
	public static readonly FieldDef FineX = S["Fine X"];
	public static readonly FieldDef Quality = S["Quality"];
	public static readonly FieldDef Next = S["Next"];
	public static readonly FieldDef Owner = S["Owner"];
	public static readonly FieldDef Link = S["Link"];

	public static readonly FieldDef HP = M["HP"];
	public static readonly FieldDef Goal = M["Goal"];
	public static readonly FieldDef GoalTarget = M["Goal target"];
	public static readonly FieldDef SavedGoal = M["Saved goal"];
	public static readonly FieldDef Attitude = M["Attitude"];
	public static readonly FieldDef HomeY = M["Home Y"];
	public static readonly FieldDef HomeX = M["Home X"];
	public static readonly FieldDef Whoami = M["Whoami"];
	// Shown by the editor, kept in the raw bytes and never written on their own
	static readonly FieldDef mobHeight = new FieldDef("Mob height", 0x0F, 6, 7, "Editor's MobHeight (bits of destination Y and attack frame).", null, Certainty.Unknown);
	static readonly FieldDef fineHeading = new FieldDef("Heading (4 bits)", 0x18, 0, 4, "Editor's Heading.", null, Certainty.Unknown);
	static readonly FieldDef hunger = new FieldDef("Hunger", 0x19, 0, 6, "Editor's Hunger.", null, Certainty.Unknown);

	/// <summary>The largest value an editor field holds, by the name of its input in the panel.
	/// False for a name that is not a record field.</summary>
	public static bool Range(string panelField, bool mobile, out int min, out int max)
	{
		FieldDef f = null;
		switch (panelField)
		{
			case "XPos": f = FineX; break;
			case "YPos": f = FineY; break;
			case "Direction": f = Heading; break;
			case "Flags": f = Flags; break;
			case "Height": f = Z; break;
			case "Quality": f = Quality; break;
			case "OwnerSpecial": f = Owner; break;
			case "QuantityLink": f = Link; break;
		}
		if (f == null && mobile)
			switch (panelField)
			{
				case "Attitude": f = Attitude; break;
				case "Goal": f = Goal; break;
				case "HomeX": f = HomeX; break;
				case "HomeY": f = HomeY; break;
				case "HP": f = HP; break;
				case "NPC": f = Whoami; break;
			}
		min = f == null ? 0 : f.Min;
		max = f == null ? 0 : (int)f.Max;
		return f != null;
	}

	/// <summary>Writes a value kept to the field's width, so it never spills into the next field.</summary>
	static void Put(FieldDef f, byte[] b, int value, int at)
	{
		f.Set(b, (int)(value & f.Max), at);
	}

	/// <summary>The record the game saves for the object.</summary>
	public static byte[] From(StaticObject so)
	{
		var mo = so as MobileObject;
		var b = new byte[mo != null ? MobileSize : StaticSize];
		Write(so, b, 0);
		return b;
	}

	/// <summary>Writes the object's record at b[at].</summary>
	public static void Write(StaticObject so, byte[] b, int at)
	{
		Put(Id, b, so.ObjectID, at);
		Put(Flags, b, so.Flags, at);
		Put(Enchanted, b, so.IsEnchanted ? 1 : 0, at);
		Put(DoorDir, b, so.IsDoorOpen ? 1 : 0, at);
		Put(Invisible, b, so.IsInvisible ? 1 : 0, at);
		Put(IsQuantity, b, so.IsQuantity ? 1 : 0, at);
		Put(Z, b, so.ZPos, at);
		Put(Heading, b, so.Direction, at);
		Put(FineY, b, so.YPos, at);
		Put(FineX, b, so.XPos, at);
		Put(Quality, b, so.Quality, at);
		Put(Next, b, so.NextAdress, at);
		Put(Owner, b, so.Owner, at);
		Put(Link, b, so.Special, at);
		var mo = so as MobileObject;
		if (mo == null || b.Length < at + MobileSize)
			return;
		// the bytes the editor keeps raw, then the fields it edits over them
		int[] raw = { mo.HP, mo.B9, mo.BA, mo.BB, mo.BC, mo.BD, mo.BE, mo.BF, mo.B10, mo.B11, mo.B12, mo.B13,
			mo.B14, mo.B15, mo.B16, mo.B17, mo.B18, mo.B19, mo.Whoami };
		for (int i = 0; i < raw.Length; i++)
			b[at + 8 + i] = (byte)raw[i];
		Put(HP, b, mo.HP, at);
		Put(Goal, b, mo.Goal, at);
		Put(GoalTarget, b, mo.GTarg, at);
		Put(Attitude, b, mo.Attitude, at);
		Put(HomeY, b, mo.YHome, at);
		Put(HomeX, b, mo.XHome, at);
		Put(Whoami, b, mo.Whoami, at);
	}

	/// <summary>Reads the record at b[at] into the object. With <paramref name="structure"/>
	/// (loading) the item id, the next link and the contents link are read too; without it (an
	/// edit of the record, the field inspector) they stay as they are, except a quantity's link.</summary>
	public static void Read(StaticObject so, byte[] b, int at, bool structure)
	{
		if (structure)
		{
			so.SetID(Id.Get(b, at));
			so.Name = StaticObject.GetName(so.ObjectID);
			so.NextAdress = Next.Get(b, at);
		}
		so.Flags = Flags.Get(b, at);
		so.IsEnchanted = Enchanted.Get(b, at) != 0;
		so.IsDoorOpen = DoorDir.Get(b, at) != 0;
		so.IsInvisible = Invisible.Get(b, at) != 0;
		so.IsQuantity = IsQuantity.Get(b, at) != 0;
		so.ZPos = Z.Get(b, at);
		so.Direction = Heading.Get(b, at);
		so.YPos = FineY.Get(b, at);
		so.XPos = FineX.Get(b, at);
		so.Quality = Quality.Get(b, at);
		so.Owner = Owner.Get(b, at);
		if (structure || so.IsQuantity)
			so.Special = Link.Get(b, at);
		var mo = so as MobileObject;
		if (mo == null || b.Length < at + MobileSize)
			return;
		int o = at + 8;
		mo.HP = HP.Get(b, at);
		mo.B9 = b[o + 1]; mo.BA = b[o + 2]; mo.BB = b[o + 3]; mo.BC = b[o + 4]; mo.BD = b[o + 5]; mo.BE = b[o + 6];
		mo.BF = b[o + 7]; mo.B10 = b[o + 8]; mo.B11 = b[o + 9]; mo.B12 = b[o + 10]; mo.B13 = b[o + 11];
		mo.B14 = b[o + 12]; mo.B15 = b[o + 13]; mo.B16 = b[o + 14]; mo.B17 = b[o + 15]; mo.B18 = b[o + 16];
		mo.B19 = b[o + 17]; mo.B1A = b[o + 18];
		mo.Goal = Goal.Get(b, at);
		mo.GTarg = GoalTarget.Get(b, at);
		mo.Level = SavedGoal.Get(b, at);
		mo.Attitude = Attitude.Get(b, at);
		mo.MobHeight = mobHeight.Get(b, at);
		mo.YHome = HomeY.Get(b, at);
		mo.XHome = HomeX.Get(b, at);
		mo.Heading = fineHeading.Get(b, at);
		mo.Hunger = hunger.Get(b, at);
		mo.Whoami = Whoami.Get(b, at);
	}
}
