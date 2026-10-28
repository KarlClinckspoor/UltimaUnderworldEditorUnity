using System;
using System.IO;

namespace UltimateEditor.Games
{
	/// <summary>
	/// OBJECTS.DAT, COMOBJ.DAT and the critters' art table (UW1 CRIT\ASSOC.ANM, UW2 CRIT\AS.AN).
	///
	/// DataReader's and DataWriter's object-data routines, moved here unchanged so they build
	/// without Unity; DataReader and DataWriter call them. The changes: the routines take the
	/// file's path (or the game folder) and the game, where they read DataReader.FilePath and
	/// LevArk.Game; DataReader.LoadObjectData is split into ReadObjectsDat and
	/// ReadMonsterSprites; backups stay in DataWriter.
	/// </summary>
	public static class ObjectDataCodec
	{
		/// <param name="filePath">COMOBJ.DAT</param>
		public static void LoadCommonData(ObjectData objDat, string filePath)
		{
			FileStream fs = new FileStream(filePath, FileMode.Open);
			int val_a = fs.ReadByte();
			int val_b = fs.ReadByte();
			objDat.CommonStart = val_a + val_b * 256;
			objDat.CommonData = new CommonData[512];
			for (int i = 0; i < 512; i++)
			{
				CommonData comDat = new CommonData();
				int[] raw = new int[11];
				raw[0] = fs.ReadByte();	//Height
				raw[1] = fs.ReadByte();	//Mass, radius, type
				raw[2] = fs.ReadByte();
				raw[3] = fs.ReadByte();	//Flags
				raw[4] = fs.ReadByte();	//Value
				raw[5] = fs.ReadByte();
				raw[6] = fs.ReadByte(); //Quality + ?
				raw[7] = fs.ReadByte();	//Type + ownable + ?
				raw[8] = fs.ReadByte();	//?
				raw[9] = fs.ReadByte();	//?
				raw[10] = fs.ReadByte();//Quality (?) + look + ?

				comDat.RawData = raw;

				comDat.Height = raw[0];
				int b1 = raw[1] + raw[2] * 256;
				comDat.Radius = b1 & 0x07;
				comDat.Type = (b1 >> 3) & 0x01;
				comDat.Mass = b1 >> 4;

				comDat.Flag0 = raw[3] & 0x01;
				comDat.Flag1 = (raw[3] >> 1) & 0x01;
				comDat.Flag2 = (raw[3] >> 2) & 0x01;
				comDat.Flag3 = (raw[3] >> 3) & 0x01;
				comDat.Flag4 = (raw[3] >> 4) & 0x01;
				comDat.Pickable = (raw[3] >> 5) & 0x01;
				comDat.Flag6 = (raw[3] >> 6) & 0x01;
				comDat.Container = (raw[3] >> 7) & 0x01;

				comDat.Value = raw[4] + raw[5] * 256;

				comDat.QualityClass = (raw[6] >> 2) & 0x03;

				comDat.ObjectType = (raw[7] >> 1) & 0x0F;
				comDat.PickupFlag = (raw[7] >> 5) & 0x01;
				comDat.UnkFlag1 = (raw[7] >> 6) & 0x01;
				comDat.Ownable = (raw[7] >> 7) & 0x01;

				comDat.QualityType = raw[10] & 0x0F;
				comDat.LookDescription = (raw[10] >> 4) & 0x01;

				objDat.CommonData[i] = comDat;
			}

			fs.Close();
		}
		/// <summary>OBJECTS.DAT (DataReader.LoadObjectData's first half).</summary>
		public static void ReadObjectsDat(ObjectData od, string filePath)
		{
			FileStream fs = new FileStream(filePath, FileMode.Open);
			int val_a = fs.ReadByte();
			int val_b = fs.ReadByte();
			od.StartValue = val_a + val_b * 256;
			od.WeaponData = ReadWeaponDatas(fs);
			od.ProjectileData = ReadProjectileDatas(fs);
			od.RangedData = ReadRangedDatas(fs);
			od.ArmourData = ReadArmourDatas(fs);
			od.MonsterData = ReadMonsterDatas(fs);
			od.ContainerData = ReadContainerDatas(fs);
			od.LightData = ReadLightDatas(fs);
			od.UnknownData = new int[112];
			for (int i = 0; i < 112; i++)		
				od.UnknownData[i] = fs.ReadByte();		
			fs.Close();
		}

		/// <summary>The critters' art pages and palettes (DataReader.LoadObjectData's second half):
		/// UW2's CRIT\\AS.AN, UW1's CRIT\\ASSOC.ANM. gameFolder is DATA's parent.</summary>
		public static void ReadMonsterSprites(ObjectData od, GameKind kind, string gameFolder)
		{
			FileStream fs;
			if (kind == UltimateEditor.Games.GameKind.UW2)
			{
				// UW2: CRIT\AS.AN, 64 entries of (page, palette); the art file of page p is CRpp.xx with
				// pp in octal (uw2 critter/CRPAGES.C). No names are stored, so the files' names stand in.
				string asan = GameFiles.Resolve(gameFolder + "/CRIT/AS.AN");
				byte[] b = File.ReadAllBytes(asan);
				od.MonsterSpriteNames = new string[32];
				for (int i = 0; i < 32; i++) od.MonsterSpriteNames[i] = "CR" + Convert.ToString(i, 8).PadLeft(2, '0');
				for (int i = 0; i < 64; i++)
				{
					od.MonsterData[i].SpriteID = b[2 * i];
					od.MonsterData[i].AuxPalette = b[2 * i + 1];
				}
				return;
			}
			fs = new FileStream(GameFiles.Resolve(gameFolder + "/CRIT/ASSOC.ANM"), FileMode.Open);
			od.MonsterSpriteNames = new string[32];
			for (int i = 0; i < 32; i++)
			{
				string s = "";
				for (int j = 0; j < 8; j++)
				{
					s += (char)fs.ReadByte();
				}
				od.MonsterSpriteNames[i] = s;
			}
			for (int i = 0; i < 64; i++)
			{
				od.MonsterData[i].SpriteID = fs.ReadByte();
				od.MonsterData[i].AuxPalette = fs.ReadByte();
			}
			fs.Close();
		}
		public static WeaponData[] ReadWeaponDatas(Stream fs)
		{
			WeaponData[] wds = new WeaponData[16];
			for (int i = 0; i < 16; i++)
				wds[i] = ReadWeaponData(fs, i);
			return wds;
		}
		public static WeaponData ReadWeaponData(Stream fs, int i)
		{
			WeaponData wd = new WeaponData();
			wd.Slash = fs.ReadByte();
			wd.Bash = fs.ReadByte();
			wd.Stab = fs.ReadByte();
			wd.Unk1 = fs.ReadByte();
			wd.Unk2 = fs.ReadByte();
			wd.Unk3 = fs.ReadByte();
			wd.Skill = fs.ReadByte();
			wd.Durability = fs.ReadByte();
			wd.ObjectID = i;
			return wd;
		}
		public static ProjectileData[] ReadProjectileDatas(Stream fs)
		{
			ProjectileData[] pds = new ProjectileData[8];
			for (int i = 0; i < 8; i++)
				pds[i] = ReadProjectileData(fs, i);
			return pds;
		}
		public static ProjectileData ReadProjectileData(Stream fs, int i)
		{
			ProjectileData pd = new ProjectileData();
			int a = fs.ReadByte();
			int b = fs.ReadByte();
			int c = fs.ReadByte();

			// include/object.h struct MissileInfo: damage, type, signed ammo (here: the damage
			// type, as minus its value). It was read as a word with the damage in bits 9-15,
			// which showed the type's upper bits as the damage.
			pd.Damage = a;
			pd.Unk1 = b;
			pd.Unk2 = c;
			return pd;
		}
		public static RangedData[] ReadRangedDatas(Stream fs)
		{
			RangedData[] rds = new RangedData[8];
			for (int i = 0; i < 8; i++)
				rds[i] = ReadRangedData(fs, i);
			return rds;
		}
		public static RangedData ReadRangedData(Stream fs, int i)
		{
			RangedData rd = new RangedData();
			int a = fs.ReadByte();
			int b = fs.ReadByte();
			int c = fs.ReadByte();

			// struct MissileInfo again: damage, type, then the ammunition (item - 0x10,
			// COMBAT.C:555). It was read as Ammo = the first word, Unk1 = the ammunition byte.
			rd.Unk1 = a;
			rd.Type = b;
			rd.Ammo = c;
			return rd;
		}
		public static ArmourData[] ReadArmourDatas(Stream fs)
		{
			ArmourData[] ads = new ArmourData[32];
			for (int i = 0; i < 32; i++)
				ads[i] = ReadArmourData(fs, i);
			return ads;
		}
		public static ArmourData ReadArmourData(Stream fs, int i)
		{
			ArmourData ad = new ArmourData();
			ad.Protection = fs.ReadByte();
			ad.Durability = fs.ReadByte();
			ad.Unk1 = fs.ReadByte();
			ad.Type = fs.ReadByte();
			return ad;
		}
		public static ContainerData[] ReadContainerDatas(Stream fs)
		{
			ContainerData[] cds = new ContainerData[16];
			for (int i = 0; i < 16; i++)
				cds[i] = ReadContainerData(fs, i);
			return cds;
		}
		public static ContainerData ReadContainerData(Stream fs, int i)
		{
			ContainerData cd = new ContainerData();
			cd.Capacity = fs.ReadByte();
			cd.Type = fs.ReadByte();
			cd.Slots = fs.ReadByte();
			return cd;
		}
		public static LightData[] ReadLightDatas(Stream fs)
		{
			LightData[] lds = new LightData[8];
			for (int i = 0; i < 8; i++)
				lds[i] = ReadLightData(fs, i);
			return lds;
		}
		public static LightData ReadLightData(Stream fs, int i)
		{
			LightData ld = new LightData();
			ld.Duration = fs.ReadByte();
			ld.Brightness = fs.ReadByte();
			return ld;
		}
		public static MonsterData[] ReadMonsterDatas(Stream fs)
		{
			MonsterData[] mds = new MonsterData[64];
			for (int i = 0; i < 64; i++)
				mds[i] = ReadMonsterData(fs, i);
			return mds;
		}
		public static MonsterData ReadMonsterData(Stream fs, int i)
		{
			MonsterData md = new MonsterData();
			md.Level = fs.ReadByte();
			md.Unk0_1 = fs.ReadByte();
			md.Unk0_2 = fs.ReadByte();
			md.Unk0_3 = fs.ReadByte();
			md.Health = fs.ReadByte() + fs.ReadByte() * 256;
			md.Attack = fs.ReadByte();
			md.Unk1 = fs.ReadByte();
			int rem = fs.ReadByte();
			md.Remains = (rem & 0xF0) >> 4;
			md.HitDecal = rem & 0x0F;
			md.OwnerType = fs.ReadByte();
			md.Passiveness = fs.ReadByte();
			md.Unk2 = fs.ReadByte();
			md.Speed = fs.ReadByte();
			md.Unk3 = fs.ReadByte() + fs.ReadByte() * 256;
			md.Poison = fs.ReadByte();
			md.MonsterType = fs.ReadByte();
			md.EquipmentDamage = fs.ReadByte();
			md.Unk4 = fs.ReadByte();
			md.Attack1Value = fs.ReadByte();
			md.Attack1Damage = fs.ReadByte();
			md.Attack1Chance = fs.ReadByte();
			md.Attack2Value = fs.ReadByte();
			md.Attack2Damage = fs.ReadByte();
			md.Attack2Chance = fs.ReadByte();
			md.Attack3Value = fs.ReadByte();
			md.Attack3Damage = fs.ReadByte();
			md.Attack3Chance = fs.ReadByte();   //28

			//Debug.LogFormat("Monster {0} has remains {1} and hit type {2}", StaticObject.GetName(i + 64), md.Remains, md.HitDecal);

			md.Inv_Unk1 = fs.ReadByte() + fs.ReadByte() * 256 + fs.ReadByte() * 256 * 256 + fs.ReadByte() * 256 * 256 * 256; 

			int inv1 = fs.ReadByte();
			int inv2 = fs.ReadByte();
			int inv3 = fs.ReadByte() + fs.ReadByte() * 256;
			int inv4 = fs.ReadByte() + fs.ReadByte() * 256; //38
			md.Inventory = new int[4];
			md.InventoryInfo = new int[4];
			// Bit 0 present, bits 1-7 the item (uw2 critter.h arms[]). The item is kept when the
			// bit is clear: UW2 reads arms[0].item as the missile a critter fires either way, and
			// two of its creatures have an item without the bit.
			md.Inventory[0] = inv1 >> 1;
			md.InventoryInfo[0] = inv1 & 1;
			md.Inventory[1] = inv2 >> 1;
			md.InventoryInfo[1] = inv2 & 1;
			if (inv3 != 0)
			{
				md.Inventory[2] = inv3 >> 4;
				md.InventoryInfo[2] = inv3 & 0x0F;
				//Debug.LogFormat("Monster <b>{0}</b> has inventory <b>{1}</b> and chance ? <b>{2}</b>", StaticObject.GetMonsterName(i), StaticObject.GetName(md.Inventory[2]), md.InventoryInfo[2]);
			}
			if (inv4 != 0)
			{
				md.Inventory[3] = inv4 >> 4;
				md.InventoryInfo[3] = inv4 & 0x0F;
				//Debug.LogFormat("Monster <b>{0}</b> has inventory <b>{1}</b> and chance ? <b>{2}</b>", StaticObject.GetMonsterName(i), StaticObject.GetName(md.Inventory[3]), md.InventoryInfo[3]);
			}

			md.Inv_Unk2 = fs.ReadByte() + fs.ReadByte() * 256;


			md.Experience = fs.ReadByte() + fs.ReadByte() * 256;
			md.Unk5 = fs.ReadByte();
			md.Unk6 = fs.ReadByte();
			md.Unk7 = fs.ReadByte();
			md.Unk8 = fs.ReadByte();
			md.Unk9 = fs.ReadByte();
			md.Unk10 = fs.ReadByte();

			return md;
		}
		/// <param name="filePath">COMOBJ.DAT (DataWriter backs it up first)</param>
		public static void SaveCommonData(ObjectData objDat, string filePath)
		{
			FileStream fs = new FileStream(filePath, FileMode.Create);
			byte start_b_a = (byte)(objDat.CommonStart & 0xFF);
			byte start_b_b = (byte)((objDat.CommonStart & 0xFF00) >> 8);
			fs.WriteByte(start_b_a);
			fs.WriteByte(start_b_b);
			for (int i = 0; i < 512; i++)
			{
				CommonData com = objDat.CommonData[i];
				fs.WriteByte((byte)com.Height);
				byte b2_a = (byte)(com.Radius + (com.Type << 3) + (com.Mass << 4) & 0xFF);
				byte b2_b = (byte)((com.Mass >> 4) & 0xFF);
				fs.WriteByte(b2_a);
				fs.WriteByte(b2_b);
				byte b3 = (byte)(com.Flag0 + (com.Flag1 << 1) + (com.Flag2 << 2) + (com.Flag3 << 3) + (com.Flag4 << 4) + (com.Pickable << 5) + (com.Flag6 << 6) + (com.Container << 7));
				fs.WriteByte(b3);
				byte b4_a = (byte)(com.Value & 0xFF);
				byte b4_b = (byte)((com.Value & 0xFF00) >> 8);
				fs.WriteByte(b4_a);
				fs.WriteByte(b4_b);
				byte b6 = (byte)((com.RawData[6] & 0xF3) + (com.QualityClass << 2));
				fs.WriteByte(b6);
				byte b7 = (byte)((com.RawData[7] & 0x01) + (com.ObjectType << 1) + (com.Ownable << 7) + (com.UnkFlag1 << 6) + (com.PickupFlag << 5));
				fs.WriteByte(b7);
				fs.WriteByte((byte)com.RawData[8]);
				fs.WriteByte((byte)com.RawData[9]);
				byte bA = (byte)((com.RawData[10] & 0xE0) + com.QualityType + (com.LookDescription << 4));
				fs.WriteByte(bA);
			}
			fs.Close();
		}
		/// <param name="filePath">OBJECTS.DAT (DataWriter backs it up first)</param>
		public static void SaveObjectData(ObjectData objDat, string filePath)
		{
			FileStream fs = new FileStream(filePath, FileMode.Create);

			byte start_b_a = (byte)(objDat.StartValue & 0xFF);
			byte start_b_b = (byte)((objDat.StartValue & 0xFF00) >> 8);
			fs.WriteByte(start_b_a);
			fs.WriteByte(start_b_b);

			for (int i = 0; i < 16; i++)		
				SaveWeaponData(objDat.WeaponData[i], fs);
			for (int i = 0; i < 8; i++)		
				SaveProjectileData(objDat.ProjectileData[i], fs);		
			for (int i = 0; i < 8; i++)		
				SaveRangedData(objDat.RangedData[i], fs);
			for (int i = 0; i < 32; i++)		
				SaveArmourData(objDat.ArmourData[i], fs);
			for (int i = 0; i < 64; i++)		
				SaveMonsterData(objDat.MonsterData[i], fs);
			for (int i = 0; i < 16; i++)		
				SaveContainerData(objDat.ContainerData[i], fs);
			for (int i = 0; i < 8; i++)		
				SaveLightData(objDat.LightData[i], fs);
			for (int i = 0; i < 112; i++)		
				fs.WriteByte((byte)objDat.UnknownData[i]);		
			fs.Close();
		}
		public static void SaveWeaponData(WeaponData wd, Stream fs)
		{
			fs.WriteByte((byte)wd.Slash);
			fs.WriteByte((byte)wd.Bash);
			fs.WriteByte((byte)wd.Stab);
			fs.WriteByte((byte)wd.Unk1);
			fs.WriteByte((byte)wd.Unk2);
			fs.WriteByte((byte)wd.Unk3);
			fs.WriteByte((byte)wd.Skill);
			fs.WriteByte((byte)wd.Durability);
		}
		public static void SaveProjectileData(ProjectileData pd, Stream fs)
		{
			fs.WriteByte((byte)pd.Damage);
			fs.WriteByte((byte)pd.Unk1);
			fs.WriteByte((byte)pd.Unk2);
		}
		public static void SaveRangedData(RangedData rd, Stream fs)
		{
			fs.WriteByte((byte)rd.Unk1);
			fs.WriteByte((byte)rd.Type);
			fs.WriteByte((byte)rd.Ammo);
		}
		public static void SaveArmourData(ArmourData ad, Stream fs)
		{
			fs.WriteByte((byte)ad.Protection);
			fs.WriteByte((byte)ad.Durability);
			fs.WriteByte((byte)ad.Unk1);
			fs.WriteByte((byte)ad.Type);
		}
		public static void SaveContainerData(ContainerData cd, Stream fs)
		{
			fs.WriteByte((byte)cd.Capacity);
			fs.WriteByte((byte)cd.Type);
			fs.WriteByte((byte)cd.Slots);
		}
		public static void SaveLightData(LightData ld, Stream fs)
		{
			fs.WriteByte((byte)ld.Duration);
			fs.WriteByte((byte)ld.Brightness);
		}
		public static void SaveMonsterData(MonsterData md, Stream fs)
		{
			fs.WriteByte((byte)md.Level);
			fs.WriteByte((byte)md.Unk0_1);
			fs.WriteByte((byte)md.Unk0_2);
			fs.WriteByte((byte)md.Unk0_3);
			byte health_a = (byte)(md.Health & 0xFF);
			byte health_b = (byte)((md.Health & 0xFF00) >> 8);
			fs.WriteByte(health_a);
			fs.WriteByte(health_b);
			fs.WriteByte((byte)md.Attack);
			fs.WriteByte((byte)md.Unk1);
			fs.WriteByte((byte)(md.HitDecal + (md.Remains << 4)));
			fs.WriteByte((byte)md.OwnerType);
			fs.WriteByte((byte)md.Passiveness);
			fs.WriteByte((byte)md.Unk2);
			fs.WriteByte((byte)md.Speed);
			byte unk3_a = (byte)(md.Unk3 & 0xFF);
			byte unk3_b = (byte)((md.Unk3 & 0xFF00) >> 8);
			fs.WriteByte(unk3_a);
			fs.WriteByte(unk3_b);
			fs.WriteByte((byte)md.Poison);
			fs.WriteByte((byte)md.MonsterType);
			fs.WriteByte((byte)md.EquipmentDamage);
			fs.WriteByte((byte)md.Unk4);
			fs.WriteByte((byte)md.Attack1Value);
			fs.WriteByte((byte)md.Attack1Damage);
			fs.WriteByte((byte)md.Attack1Chance);
			fs.WriteByte((byte)md.Attack2Value);
			fs.WriteByte((byte)md.Attack2Damage);
			fs.WriteByte((byte)md.Attack2Chance);
			fs.WriteByte((byte)md.Attack3Value);
			fs.WriteByte((byte)md.Attack3Damage);
			fs.WriteByte((byte)md.Attack3Chance);
			byte inv_unk1_a = (byte)(md.Inv_Unk1 & 0xFF);
			byte inv_unk1_b = (byte)((md.Inv_Unk1 & 0xFF00) >> 8);
			byte inv_unk1_c = (byte)((md.Inv_Unk1 & 0xFF0000) >> 16);
			byte inv_unk1_d = (byte)((md.Inv_Unk1 & 0xFF000000) >> 24);
			fs.WriteByte(inv_unk1_a);
			fs.WriteByte(inv_unk1_b);
			fs.WriteByte(inv_unk1_c);
			fs.WriteByte(inv_unk1_d);
			fs.WriteByte((byte)((md.Inventory[0] << 1) | (md.InventoryInfo[0] > 0 ? 1 : 0)));
			fs.WriteByte((byte)((md.Inventory[1] << 1) | (md.InventoryInfo[1] > 0 ? 1 : 0)));
			if(md.Inventory[2] > 0)
			{			
				byte i2_a = (byte)(((md.Inventory[2] & 0x0F) << 4) + md.InventoryInfo[2]);
				byte i2_b = (byte)(md.Inventory[2] >> 4);
				fs.WriteByte(i2_a);
				fs.WriteByte(i2_b);
			}
			else
			{
				fs.WriteByte(0);
				fs.WriteByte(0);
			}
			if (md.Inventory[3] > 0)
			{
				byte i2_a = (byte)(((md.Inventory[3] & 0x0F) << 4) + md.InventoryInfo[3]);
				byte i2_b = (byte)(md.Inventory[3] >> 4);
				fs.WriteByte(i2_a);
				fs.WriteByte(i2_b);
			}
			else
			{
				fs.WriteByte(0);
				fs.WriteByte(0);
			}
			byte inv_unk2_a = (byte)(md.Inv_Unk2 & 0xFF);
			byte inv_unk2_b = (byte)((md.Inv_Unk2 & 0xFF00) >> 8);
			fs.WriteByte(inv_unk2_a);
			fs.WriteByte(inv_unk2_b);
			byte exp_a = (byte)(md.Experience & 0xFF);
			byte exp_b = (byte)((md.Experience & 0xFF00) >> 8);
			fs.WriteByte(exp_a);
			fs.WriteByte(exp_b);
			fs.WriteByte((byte)md.Unk5);
			fs.WriteByte((byte)md.Unk6);
			fs.WriteByte((byte)md.Unk7);
			fs.WriteByte((byte)md.Unk8);
			fs.WriteByte((byte)md.Unk9);
			fs.WriteByte((byte)md.Unk10);
		}
		/// <summary>gameFolder is DATA's parent; the critter file is backed up once (.BKP).</summary>
		public static void SaveMonsterSpriteInfo(ObjectData objDat, GameKind kind, string gameFolder)
		{
			if (kind == UltimateEditor.Games.GameKind.UW2)
			{
				// UW2: CRIT\AS.AN, 64 entries of (page, palette), nothing else in the file
				string asan = GameFiles.Resolve(gameFolder + "/CRIT/AS.AN");
				if (!File.Exists(asan + ".BKP")) File.Copy(asan, asan + ".BKP");
				byte[] b = File.ReadAllBytes(asan);
				for (int i = 0; i < 64 && 2 * i + 1 < b.Length; i++)
				{
					b[2 * i] = (byte)objDat.MonsterData[i].SpriteID;
					b[2 * i + 1] = (byte)objDat.MonsterData[i].AuxPalette;
				}
				File.WriteAllBytes(asan, b);
				return;
			}
			string file = GameFiles.Resolve(gameFolder + "/CRIT/ASSOC.ANM");
			string backupFile = file + ".BKP";
			if (!File.Exists(backupFile))
				File.Copy(file, backupFile);
			FileStream fs = new FileStream(file, FileMode.Open);
			fs.Position = 256;
			for (int i = 0; i < 64; i++)
			{
				fs.WriteByte((byte)objDat.MonsterData[i].SpriteID);
				fs.WriteByte((byte)objDat.MonsterData[i].AuxPalette);
			}
			fs.Close();
		}
	}
}
