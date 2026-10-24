using System;
using System.Collections.Generic;

namespace UltimateEditor.Uw1
{
	/// <summary>How sure the meaning of a field is.</summary>
	public enum Certainty
	{
		/// <summary>Read in the reference source (underworld-exhumed), which compiles to UW.EXE.</summary>
		Certain,
		/// <summary>Inferred from how the code uses it, or measured on the shipped data.</summary>
		Inferred,
		/// <summary>Nothing in the code explains it.</summary>
		Unknown
	}

	/// <summary>
	/// One bit field of a little-endian record: <see cref="Bits"/> bits starting at bit
	/// <see cref="Bit"/> of the little-endian value that starts at byte <see cref="Offset"/>.
	/// Fields may span up to four bytes.
	/// </summary>
	public sealed class FieldDef
	{
		public readonly string Name;
		public readonly int Offset;
		public readonly int Bit;
		public readonly int Bits;
		public readonly bool Signed;
		public readonly string Description;
		public readonly string Source;
		public readonly Certainty Certainty;

		public FieldDef(string name, int offset, int bit, int bits, string description, string source = null,
			Certainty certainty = Certainty.Certain, bool signed = false)
		{
			if (bits < 1 || bits > 32) throw new ArgumentOutOfRangeException("bits");
			if (bit + bits > 32) throw new ArgumentOutOfRangeException("bit");
			Name = name;
			Offset = offset;
			Bit = bit;
			Bits = bits;
			Signed = signed;
			Description = description;
			Source = source;
			Certainty = certainty;
		}

		/// <summary>Number of bytes the field touches, counting from <see cref="Offset"/>.</summary>
		public int ByteSpan { get { return (Bit + Bits + 7) / 8; } }

		public int Min { get { return Signed ? -(1 << (Bits - 1)) : 0; } }
		public long Max { get { return Signed ? (1L << (Bits - 1)) - 1 : (1L << Bits) - 1; } }

		public int Get(byte[] data, int recordStart = 0)
		{
			uint word = 0;
			int span = ByteSpan;
			for (int i = 0; i < span; i++)
				word |= (uint)data[recordStart + Offset + i] << (8 * i);
			uint mask = Bits == 32 ? 0xFFFFFFFFu : (1u << Bits) - 1;
			uint value = (word >> Bit) & mask;
			if (Signed && (value & (1u << (Bits - 1))) != 0)
				return (int)(value | ~mask);
			return (int)value;
		}

		/// <summary>Writes the field, keeping every other bit of the touched bytes.</summary>
		public void Set(byte[] data, int value, int recordStart = 0)
		{
			if (value < Min || value > Max)
				throw new ArgumentOutOfRangeException("value", string.Format("{0} must be {1}..{2}", Name, Min, Max));
			uint mask = Bits == 32 ? 0xFFFFFFFFu : (1u << Bits) - 1;
			int span = ByteSpan;
			uint word = 0;
			for (int i = 0; i < span; i++)
				word |= (uint)data[recordStart + Offset + i] << (8 * i);
			word = (word & ~(mask << Bit)) | (((uint)value & mask) << Bit);
			for (int i = 0; i < span; i++)
				data[recordStart + Offset + i] = (byte)(word >> (8 * i));
		}

		public string Location
		{
			get
			{
				if (Bit % 8 == 0 && Bits % 8 == 0)
					return Bits == 8 ? string.Format("0x{0:X2}", Offset) : string.Format("0x{0:X2}..0x{1:X2}", Offset, Offset + Bits / 8 - 1);
				return Bits == 1
					? string.Format("0x{0:X2} bit {1}", Offset, Bit)
					: string.Format("0x{0:X2} bits {1}-{2}", Offset, Bit, Bit + Bits - 1);
			}
		}

		public override string ToString()
		{
			return string.Format("{0} ({1}): {2}", Name, Location, Description);
		}
	}

	/// <summary>A record layout: its size and its fields.</summary>
	public sealed class RecordSchema
	{
		public readonly string Name;
		public readonly int Size;
		public readonly string Source;
		public readonly FieldDef[] Fields;

		public RecordSchema(string name, int size, string source, params FieldDef[] fields)
		{
			Name = name;
			Size = size;
			Source = source;
			Fields = fields;
			foreach (var f in fields)
				if (f.Offset + f.ByteSpan > size)
					throw new ArgumentException(string.Format("{0}.{1} runs past the record ({2} bytes)", name, f.Name, size));
		}

		public FieldDef this[string name]
		{
			get
			{
				foreach (var f in Fields)
					if (f.Name == name)
						return f;
				throw new KeyNotFoundException(name);
			}
		}

		/// <summary>Bits of the record no field describes, as (offset, mask) pairs.</summary>
		public List<KeyValuePair<int, int>> UncoveredBits()
		{
			var covered = new int[Size];
			foreach (var f in Fields)
				for (int b = 0; b < f.Bits; b++)
				{
					int abs = f.Offset * 8 + f.Bit + b;
					covered[abs / 8] |= 1 << (abs % 8);
				}
			var result = new List<KeyValuePair<int, int>>();
			for (int i = 0; i < Size; i++)
				if (covered[i] != 0xFF)
					result.Add(new KeyValuePair<int, int>(i, ~covered[i] & 0xFF));
			return result;
		}
	}
}
