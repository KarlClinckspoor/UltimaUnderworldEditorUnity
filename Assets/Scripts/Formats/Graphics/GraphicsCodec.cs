using System;
using System.Collections.Generic;
using System.IO;

namespace UltimateEditor.Games
{
	/// <summary>
	/// Pictures and palettes, both games: PALS.DAT and ALLPALS.DAT, the .GR/.TR offset tables,
	/// raw 8-bit pictures, and the 4-bit run-length pictures of OBJECTS.GR (UW-Formats 3.2).
	///
	/// These are DataReader's and DataWriter's picture routines, moved here unchanged so they
	/// build without Unity; DataReader and DataWriter call them. The changes: the routines that
	/// opened a file by name in DATA take its full path (DataReader resolves it as before),
	/// ReadRawTextures reports picture sizes as <see cref="Size"/> (DataReader turns them into
	/// Vector2Int), and logging goes through FormatsLog.
	/// </summary>
	public static class GraphicsCodec
	{
		/// <summary>A picture's width and height (DataReader's Vector2Int, without Unity).</summary>
		public struct Size
		{
			public int x, y;
			public Size(int x, int y) { this.x = x; this.y = y; }
		}

		/// <summary>Nearest-neighbour halving of a raw (palette index) picture.</summary>
		public static int[] Downscale(int[] raw, int width, int height)
		{
			int w = width / 2, h = height / 2;
			var r = new int[w * h];
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					r[y * w + x] = raw[(y * 2) * width + x * 2];
			return r;
		}
		public static int[] ReadPallette(Stream fs, int index)
		{
			int[] pal = new int[256 * 3];
			//string filePath = GameFiles.Resolve(FileExplorer.GetUpperPath(FilePath) + "/pals.dat");
			//FileStream fs = new FileStream(filePath, FileMode.Open);
			for (int i = 0; i < 256 * 3; i++)
			{
				pal[i] = fs.ReadByte();
			}
			//fs.Close();
			return pal;
		}
		/// <param name="filePath">ALLPALS.DAT (DataReader passes DATA's, resolved)</param>
		public static List<int[]> ReadAuxPalettes(string filePath)
		{
			FileStream fs = new FileStream(filePath, FileMode.Open);

			List<int[]> auxpals = new List<int[]>();
			// 32 palettes: the game reads 0x200 bytes (reference gfx/LOADGR.C:428; the file is 513
			// bytes in both games). UW2's objects use palette 31.
			for (int p = 0; p < 32; p++)
			{
				int[] auxpal = new int[16];
				for (int i = 0; i < 16; i++)
				{
					int b = fs.ReadByte();
					auxpal[i] = b < 0 ? 0 : b;
				}
				auxpals.Add(auxpal);
			}
			fs.Close();
			return auxpals;
		}
		public static int ReadPaletteMapping(Stream fs, int r, int g, int b)
		{
			fs.Position = r * 256 * 256 + g * 256 + b;
			return fs.ReadByte();
		}
		/// <param name="filePath">the picture file (DataReader resolves the name in DATA)</param>
		public static int[] ReadGraphicOffsets(string filePath, ref int count, ref int first)
		{
			FileStream fs = new FileStream(filePath, FileMode.Open);
			int start = fs.ReadByte();
			int size = 0;
			if (start == 2)
				size = fs.ReadByte();

			int c_a = fs.ReadByte();
			int c_b = fs.ReadByte();

			count = c_a + c_b * 256;
		
			int[] offsets = new int[count];
			first = start + 2;
			for (int i = 0; i < count; i++)
			{
				fs.Position = start + 2 + i * 4;
				int a = fs.ReadByte();
				int b = fs.ReadByte();
				int c = fs.ReadByte();
				int d = fs.ReadByte();

				int offset = a + (b * 256) + (c * 256 * 256) + (d * 256 * 256 * 256);
				offsets[i] = offset;
			}
			fs.Close();
			return offsets;
		}
		public static int[] Read4BitGraphic(Stream fs, long offset, int startingOffset, int index, AdditionalTextureInfo ati)
		{
			fs.Position = offset + 1;
			ati.Width = fs.ReadByte();
			ati.Height = fs.ReadByte();
			ati.AuxPalette = fs.ReadByte();
			ati.SizeA = fs.ReadByte();
			ati.SizeB = fs.ReadByte();
			int size = ati.SizeA + ati.SizeB * 256;

			//Debug.LogFormat("Index : {0}, size : {1}, fs.Position : {2}", index, size, fs.Position);
			return Read4BitGraphic(fs, fs.Position, ati.Width, ati.Height, size);
		}
		public static int[] Read4BitGraphic(Stream fs, long offset, int width, int height, int count)
		{
			int i = 0;
			int[] rawGr = new int[count];
			while (count > 1)
			{
				int n = fs.ReadByte();
				int n1 = (n & 0xF0) >> 4;
				int n2 = n & 0x0F;
				rawGr[i] = n1;
				rawGr[i + 1] = n2;

				i += 2;
				count -= 2;
			}
			return rawGr;
		}
		public static int[] Convert4BitToRawTex(int[] nibbles, int width, int height)
		{
			int[] rawImg = new int[width * height];
			int state = 0;
			int ptr = 0;
			int count = 0;
			int repeatCount = 0;
			int curNibble = 0;
			int curPixel = 0;
			//Debug.LogFormat("size : {0}, nibbles.Length : {1}", width * height, nibbles.Length);
			while(ptr < nibbles.Length || curPixel < rawImg.Length)
			{
				if (ptr >= nibbles.Length - 1)
					break;
				//Debug.LogFormat("state : {0}, nibble : {1}, ptr : {2}, count : {3}, pixel : {4}, repeatCount : {5}, nibbles.Length : {6}", state, curNibble, ptr, count, curPixel, repeatCount, nibbles.Length);
				if(state == 0)		//Start
				{
					count = getCount(nibbles, ref ptr);
					if(count == 1)
						state = 2;
					else if (count == 2)
						repeatCount = getCount(nibbles, ref ptr) - 1;
					else
						state = 1;
				}
				else if(state == 1)	//Repeat
				{
					curNibble = getNibble(nibbles, ref ptr);
					if(width * height - curPixel < count)
						count = width * height - curPixel;
					for (int i = 0; i < count; i++)
						rawImg[curPixel++] = curNibble;
					if(repeatCount == 0)
						state = 2;
					else
					{
						state = 0;
						repeatCount--;
					}
				}
				else if(state == 2)	//Run
				{
					count = getCount(nibbles, ref ptr);
					if(width * height - curPixel < count)
						count = width * height - curPixel;
					for (int i = 0; i < count; i++)
					{
						curNibble = getNibble(nibbles, ref ptr);
						rawImg[curPixel++] = curNibble;
					}
					state = 0;
				}
			}
			return rawImg;
		}
		private static int getCount(int[] nibbles, ref int ptr)
		{
			int n1;
			int n2;
			int n3;
			int count = 0;

			n1 = getNibble(nibbles, ref ptr);
			count = n1;

			if (count == 0)
			{
				n1 = getNibble(nibbles, ref ptr);
				n2 = getNibble(nibbles, ref ptr);
				count = (n1 << 4) | n2;
			}
			if (count == 0)
			{
				n1 = getNibble(nibbles, ref ptr);
				n2 = getNibble(nibbles, ref ptr);
				n3 = getNibble(nibbles, ref ptr);
				count = (((n1 << 4) | n2) << 4) | n3;
			}
			return count;
		}
		private static int getNibble(int[] nibbles, ref int ptr)
		{
			int nibble = nibbles[ptr];
			ptr++;
			return nibble;
		}
		/// <param name="filePath">the picture file (DataReader resolves the name in DATA)</param>
		public static List<int[]> ReadRawTextures(string filePath, int[] offsets, int width, int height, int startingOffset = 0, bool specificSize = false, Dictionary<int, Size> dict = null)
		{
			if (offsets == null)
				return null;
			int count = offsets.Length;
			List<int[]> textures = new List<int[]>();
			FileStream fs = new FileStream(filePath, FileMode.Open);
			for (int i = 0; i < count; i++)
			{
				if (!specificSize)
					fs.Position = offsets[i] + startingOffset;
				else
				{
					fs.Position = offsets[i] + 1;
					width = fs.ReadByte();
					height = fs.ReadByte();
					fs.Position = offsets[i] + startingOffset;
					dict[i] = new Size(width, height);
				}
				int[] texture = new int[width * height];
				for (int j = 0; j < width * height; j++)
				{
					texture[j] = fs.ReadByte();
					if(texture[j] == -1)
					{
						fs.Close();
						return textures;
					}
				}
				textures.Add(texture);
			}
			fs.Close();
			return textures;
		}
		public static void SaveTexture(Stream fs, string fileName, int offset, int[] rawTex, AdditionalTextureInfo ati)
		{
			if (rawTex == null)
			{
				FormatsLog.Warning(string.Format("Trying to save null texture, type : {0}, offset : {1}", fileName, offset));
				return;
			}
			//string path = GameFiles.Resolve(FileExplorer.GetUpperPath(DataReader.FilePath) + "/" + fileName);
			//FileStream fs = new FileStream(path, FileMode.Open);
			fs.Position = offset;
			if(ati.Write)
			{
				fs.WriteByte(4);
				fs.WriteByte((byte)ati.Width);
				fs.WriteByte((byte)ati.Height);
				fs.WriteByte((byte)ati.SizeA);
				fs.WriteByte((byte)ati.SizeB);
			}		
			for (int i = 0; i < rawTex.Length; i++)
				fs.WriteByte((byte)rawTex[i]);
			//fs.Close();
		}
		public static void SaveNibbles(Stream fs, long offset, List<int> nibbles)
		{
			fs.Position = offset;
			for (int i = 0; i < nibbles.Count; i+=2)
			{
				int nib_a = nibbles[i] << 4;
				int nib_b = (i + 1) == nibbles.Count ? 0 : nibbles[i + 1];
				byte b = (byte)(nib_a + nib_b);
				fs.WriteByte(b);
			}
		}
		public static List<int> CompressToRLE(int[] rawTex)
		{
			List<int> nibbles = new List<int>();
			int ptr = 0;
			int cur = 0;
			int state = 0;
			List<Tuple<int, int>> counts = getCounts(rawTex, ref ptr);
			int safe = 1000;
			while(cur < counts.Count)
			{
				//Debug.LogFormat("CUR : {0}", cur);
				if(state == 0)
				{
					//Debug.Log("State : 0");
					int repeats = getRepeats(counts, cur);
					//Debug.LogFormat("repeats : {0}", repeats);
					if (repeats == 0)
					{
						//Debug.LogError("Repeats == 0");
						nibbles.Add(1);
					}
					else if (repeats == 1)
					{
						int count = counts[cur].Item1;
						int val = counts[cur].Item2;
						//Debug.LogFormat("<b>One repeat, writing count {0} and val {1}</b>", count, val);
						nibbles.AddRange(getCount(count));
						nibbles.Add(val);
					}
					else if (repeats > 1)
					{
						nibbles.Add(2);
						nibbles.AddRange(getCount(repeats));
						//Debug.LogFormat("<b>{0} repeats, writing 2 and {0}</b>", repeats);
						for (int i = cur; i < cur+repeats; i++)
						{
							int count = counts[i].Item1;
							int val = counts[i].Item2;
							//Debug.LogFormat("Repeat, writing count {0} and val {1}", count, val);
							nibbles.AddRange(getCount(count));
							nibbles.Add(val);
						}
					}
					cur += repeats;
					state = 1;
				}
				else if (state == 1)
				{
					//Debug.Log("State : 1");
					int small = getSmall(counts, cur);
					//Debug.LogFormat("small : {0}", small);
					int sum = 0;
					for (int i = cur; i < cur+small; i++)
					{
						sum += counts[i].Item1;
					}
					nibbles.AddRange(getCount(sum));
					//Debug.LogFormat("<b>Writing count {0}</b>", sum);
					for (int i = cur; i < cur + small; i++)
					{
						for (int j = 0; j < counts[i].Item1; j++)
						{
							nibbles.Add(counts[i].Item2);
							//Debug.LogFormat("Writing val {0}", counts[i].Item2);
						}
					}
					cur += small;
					state = 0;
				}
				safe--;
				if(safe==0)
				{
					FormatsLog.Error("Fail");
					break;
				}
			}

			//if(nibbles.Count % 2 == 0)
			if(state == 1)
			{
				nibbles[nibbles.Count - 2]++;
				nibbles.Add(1);
				nibbles.Add(0);
			}
			return nibbles;
		}
		private static int getRepeats(List<Tuple<int, int>> counts, int ptr)
		{
			int repeats = 0;
			for (int i = ptr; i < counts.Count; i++)
			{
				if (counts[i].Item1 > 2)
					repeats++;
				else
					return repeats;
			}
			return repeats;
		}
		private static int getSmall(List<Tuple<int, int>> counts, int ptr)
		{
			int small = 0;
			for (int i = ptr; i < counts.Count; i++)
			{
				if (counts[i].Item1 < 3)
					small++;
				else
					return small;
			}
			return small;
		}
		private static List<int> getCount(int count)
		{
			List<int> counts = new List<int>();

			if (count > 255)
			{
				counts.Add(0);
				counts.Add(0);
				counts.Add(0);
				int r1 = count / 256;
				count -= (r1 * 256);
				int r2 = count / 16;
				count -= (r2 * 16);
				counts.Add(r1);
				counts.Add(r2);
				counts.Add(count);
			}
			else if (count > 15)
			{
				counts.Add(0);
				int rest = count / 16;
				counts.Add(rest);
				count -= (16 * rest);
				counts.Add(count);
			}
			else
				counts.Add(count);
	
			return counts;
		}
		public static List<Tuple<int, int>> getCounts(int[] rawTex, ref int ptr)
		{
			List<Tuple<int, int>> counts = new List<Tuple<int, int>>();

			int count = 1;
			int cur = rawTex[ptr];
			while(ptr < rawTex.Length - 1)
			{
				ptr++;
				int next = rawTex[ptr];
				if(cur == next)
				{
					count++;
				}
				else
				{
					counts.Add(new Tuple<int, int>(count, cur));
					count = 1;
					cur = next;
				}
			}
			counts.Add(new Tuple<int, int>(count, cur));
			return counts;
		}
		private static List<int> getValsFromTo(int[] rawTex, int start, int end)
		{
			List<int> vals = new List<int>();
			for (int i = start; i < end; i++)
				vals.Add(rawTex[i]);
			return vals;
		}
	}
}
