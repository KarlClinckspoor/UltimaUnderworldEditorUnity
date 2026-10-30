namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Converts the editor's StaticObject / MobileObject to the record bytes the game uses
	/// and back, with exactly the packing of DataReader.ReadStaticObject / ReadMobileObject
	/// and DataWriter.SaveStaticObject / SaveMobileObject, so a field edited through a
	/// <see cref="Uw1Schemas"/> field lands where the save writes it.
	/// </summary>
	public static class ObjectBytes
	{
		public static byte[] From(StaticObject so)
		{
			var mo = so as MobileObject;
			var b = new byte[mo != null ? 0x1B : 8];
			b[0] = (byte)(so.ObjectID & 0xFF);
			b[1] = (byte)(((so.ObjectID & 0x100) >> 8) + ((so.Flags & 7) << 1) + ((so.IsEnchanted ? 1 : 0) << 4)
				+ ((so.IsDoorOpen ? 1 : 0) << 5) + ((so.IsInvisible ? 1 : 0) << 6) + ((so.IsQuantity ? 1 : 0) << 7));
			b[2] = (byte)((so.ZPos & 0x7F) + ((so.Direction & 0x01) << 7));
			b[3] = (byte)(((so.Direction & 0x06) >> 1) + ((so.YPos & 7) << 2) + ((so.XPos & 7) << 5));
			b[4] = (byte)((so.Quality & 0x3F) + ((so.NextAdress & 0x03) << 6));
			b[5] = (byte)((so.NextAdress & 0x3FC) >> 2);
			b[6] = (byte)((so.Owner & 0x3F) + ((so.Special & 0x03) << 6));
			b[7] = (byte)((so.Special & 0x3FC) >> 2);
			if (mo != null)
			{
				b[0x08] = (byte)mo.HP;
				b[0x09] = (byte)mo.B9;
				b[0x0A] = (byte)mo.BA;
				b[0x0B] = (byte)((mo.Goal & 0x0F) + ((mo.GTarg & 0x0F) << 4));
				b[0x0C] = (byte)(((mo.GTarg & 0xF0) >> 4) + (mo.BC & 0xF0));
				b[0x0D] = (byte)mo.BD;
				b[0x0E] = (byte)(((mo.Attitude & 0x03) << 6) + (mo.BE & 0x3F));
				b[0x0F] = (byte)mo.BF;
				b[0x10] = (byte)mo.B10;
				b[0x11] = (byte)mo.B11;
				b[0x12] = (byte)mo.B12;
				b[0x13] = (byte)mo.B13;
				b[0x14] = (byte)mo.B14;
				b[0x15] = (byte)mo.B15;
				b[0x16] = (byte)((mo.B16 & 0x0F) + ((mo.YHome & 0x0F) << 4));
				b[0x17] = (byte)(((mo.YHome & 0x30) >> 4) + ((mo.XHome & 0x3F) << 2));
				b[0x18] = (byte)mo.B18;
				b[0x19] = (byte)mo.B19;
				b[0x1A] = (byte)mo.Whoami;
			}
			return b;
		}

		/// <summary>
		/// Copies the record back into the object's fields. The item id, the next link and
		/// (for a non-quantity object) the contents link are list structure and are not taken
		/// from the record.
		/// </summary>
		public static void To(StaticObject so, byte[] b)
		{
			so.Flags = (b[1] & 0x0E) >> 1;
			so.IsEnchanted = (b[1] & 0x10) != 0;
			so.IsDoorOpen = (b[1] & 0x20) != 0;
			so.IsInvisible = (b[1] & 0x40) != 0;
			so.IsQuantity = (b[1] & 0x80) != 0;
			so.ZPos = b[2] & 0x7F;
			so.Direction = ((b[2] & 0x80) >> 7) + ((b[3] & 0x03) << 1);
			so.YPos = (b[3] & 0x1C) >> 2;
			so.XPos = (b[3] & 0xE0) >> 5;
			so.Quality = b[4] & 0x3F;
			so.Owner = b[6] & 0x3F;
			if (so.IsQuantity)
				so.Special = ((b[6] & 0xC0) >> 6) + (b[7] << 2);
			var mo = so as MobileObject;
			if (mo != null && b.Length >= 0x1B)
			{
				mo.HP = b[8];
				mo.B9 = b[9]; mo.BA = b[0xA]; mo.BB = b[0xB]; mo.BC = b[0xC]; mo.BD = b[0xD]; mo.BE = b[0xE];
				mo.BF = b[0xF]; mo.B10 = b[0x10]; mo.B11 = b[0x11]; mo.B12 = b[0x12]; mo.B13 = b[0x13];
				mo.B14 = b[0x14]; mo.B15 = b[0x15]; mo.B16 = b[0x16]; mo.B17 = b[0x17]; mo.B18 = b[0x18];
				mo.B19 = b[0x19]; mo.B1A = b[0x1A];
				mo.Goal = b[0xB] & 0x0F;
				mo.GTarg = ((b[0xB] & 0xF0) >> 4) + ((b[0xC] & 0x0F) << 4);
				mo.Level = b[0xD] & 0x0F;
				mo.Attitude = (b[0xE] & 0xC0) >> 6;
				mo.MobHeight = ((b[0xF] & 0xC0) >> 6) + ((b[0x10] & 0x1F) << 2);
				mo.YHome = ((b[0x16] & 0xF0) >> 4) + ((b[0x17] & 0x03) << 4);
				mo.XHome = (b[0x17] & 0xFC) >> 2;
				mo.Heading = b[0x18] & 0x0F;
				mo.Hunger = b[0x19] & 0x3F;
				mo.Whoami = b[0x1A];
			}
		}

		/// <summary>Fields the inspector shows but must not change (list structure).</summary>
		public static bool IsStructural(FieldDef f, StaticObject so)
		{
			if (f.Name == "Item id" || f.Name == "Next") return true;
			if (f.Name == "Link" && !so.IsQuantity) return true;
			return false;
		}
	}
}
