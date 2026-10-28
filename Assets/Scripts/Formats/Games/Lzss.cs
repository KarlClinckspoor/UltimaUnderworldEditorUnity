using System;
using System.Collections.Generic;

namespace UltimateEditor.Games
{
	/// <summary>
	/// UW2's archive compression: Haruhiko Okumura's LZSS of 1989, as the reference's
	/// uw2/src/sys/LZSS.C has it (4096-byte ring filled with spaces, writing from 4078,
	/// matches of 3 to 18 bytes, a binary search tree for the encoder). The encoder is the
	/// same code line for line, so it produces the same bytes as the game's put_arc.
	/// ACLZW.C adds a long, the uncompressed length, in front of the stream.
	/// </summary>
	public static class Lzss
	{
		const int N = 4096, F = 18, Threshold = 2, Nil = N;

		/// <summary>Decodes a stream until its bytes run out (the game ignores the stored length, ACLZW.C).</summary>
		public static byte[] Decode(byte[] src, int offset, int count)
		{
			var outBuf = new List<byte>(count * 3);
			var text = new byte[N];
			for (int i = 0; i < N - F; i++) text[i] = 0x20;
			int r = N - F;
			int flags = 0;
			int p = offset, end = offset + count;
			while (true)
			{
				if (((flags >>= 1) & 256) == 0)
				{
					if (p >= end) break;
					flags = src[p++] | 0xFF00;
				}
				if ((flags & 1) != 0)
				{
					if (p >= end) break;
					byte c = src[p++];
					outBuf.Add(c);
					text[r++] = c;
					r &= N - 1;
				}
				else
				{
					if (p >= end) break;
					int i = src[p++];
					if (p >= end) break;
					int j = src[p++];
					i |= (j & 0xF0) << 4;
					j = (j & 0x0F) + Threshold;
					for (int k = 0; k <= j; k++)
					{
						byte c = text[(i + k) & (N - 1)];
						outBuf.Add(c);
						text[r++] = c;
						r &= N - 1;
					}
				}
			}
			return outBuf.ToArray();
		}

		/// <summary>A compressed block as the archive stores it: the long length, then the stream.</summary>
		public static byte[] DecodeBlock(byte[] stored)
		{
			if (stored.Length < 4) return new byte[0];
			return Decode(stored, 4, stored.Length - 4);
		}

		/// <summary>
		/// Encodes like CompressLZW_disk. Returns null when compression saves nothing (the
		/// game then stores the block plain), else the stream without the length prefix.
		/// </summary>
		public static byte[] Encode(byte[] src)
		{
			var e = new Encoder();
			return e.Run(src);
		}

		/// <summary>Encodes and prefixes the length, as ac_shrink_disk writes it; null if compression saves nothing.</summary>
		public static byte[] EncodeBlock(byte[] src)
		{
			var stream = Encode(src);
			if (stream == null) return null;
			var block = new byte[stream.Length + 4];
			block[0] = (byte)src.Length; block[1] = (byte)(src.Length >> 8);
			block[2] = (byte)(src.Length >> 16); block[3] = (byte)(src.Length >> 24);
			Array.Copy(stream, 0, block, 4, stream.Length);
			return block;
		}

		sealed class Encoder
		{
			readonly byte[] textBuf = new byte[N + F - 1];
			readonly int[] lson = new int[N + 1];
			readonly int[] rson = new int[N + 257];
			readonly int[] dad = new int[N + 1];
			int matchPosition, matchLength;
			byte[] src;
			int srcPos, srcLeft;

			int Getc()
			{
				if (srcLeft == 0) return -1;
				srcLeft--;
				return src[srcPos++];
			}

			void InsertNode(int r)
			{
				int cmp = 1;
				int keyAt = r;
				int p = N + 1 + textBuf[keyAt];
				rson[r] = lson[r] = Nil;
				matchLength = 0;
				for (;;)
				{
					if (cmp >= 0)
					{
						if (rson[p] != Nil) p = rson[p];
						else { rson[p] = r; dad[r] = p; return; }
					}
					else
					{
						if (lson[p] != Nil) p = lson[p];
						else { lson[p] = r; dad[r] = p; return; }
					}
					int i;
					for (i = 1; i < F; i++)
						if ((cmp = textBuf[keyAt + i] - textBuf[p + i]) != 0)
							break;
					if (i > matchLength)
					{
						matchPosition = p;
						if ((matchLength = i) >= F)
							break;
					}
				}
				dad[r] = dad[p];
				lson[r] = lson[p];
				rson[r] = rson[p];
				dad[lson[p]] = r;
				dad[rson[p]] = r;
				if (rson[dad[p]] == p) rson[dad[p]] = r;
				else lson[dad[p]] = r;
				dad[p] = Nil;
			}

			void DeleteNode(int p)
			{
				int q;
				if (dad[p] == Nil) return;
				if (rson[p] == Nil) q = lson[p];
				else if (lson[p] == Nil) q = rson[p];
				else
				{
					q = lson[p];
					if (rson[q] != Nil)
					{
						do { q = rson[q]; } while (rson[q] != Nil);
						rson[dad[q]] = lson[q];
						dad[lson[q]] = dad[q];
						lson[q] = lson[p];
						dad[lson[p]] = q;
					}
					rson[q] = rson[p];
					dad[rson[p]] = q;
				}
				dad[q] = dad[p];
				if (rson[dad[p]] == p) rson[dad[p]] = q;
				else lson[dad[p]] = q;
				dad[p] = Nil;
			}

			public byte[] Run(byte[] input)
			{
				src = input; srcPos = 0; srcLeft = input.Length;
				var outBuf = new List<byte>(input.Length);
				int textsize = 0, codesize = 0;
				for (int i = N + 1; i <= N + 256; i++) rson[i] = Nil;
				for (int i = 0; i < N; i++) dad[i] = Nil;
				var codeBuf = new byte[17];
				codeBuf[0] = 0;
				int codeBufPtr = 1;
				int mask = 1;
				int s = 0, r = N - F;
				for (int i = s; i < r; i++) textBuf[i] = 0x20;
				int len, c;
				for (len = 0; len < F && (c = Getc()) != -1; len++) textBuf[r + len] = (byte)c;
				if ((textsize = len) == 0) return null;
				for (int i = 1; i <= F; i++) InsertNode(r - i);
				InsertNode(r);
				do
				{
					if (matchLength > len) matchLength = len;
					if (matchLength <= Threshold)
					{
						matchLength = 1;
						codeBuf[0] |= (byte)mask;
						codeBuf[codeBufPtr++] = textBuf[r];
					}
					else
					{
						codeBuf[codeBufPtr++] = (byte)matchPosition;
						codeBuf[codeBufPtr++] = (byte)(((matchPosition >> 4) & 0xF0) | (matchLength - (Threshold + 1)));
					}
					if ((mask = (mask << 1) & 0xFF) == 0)
					{
						for (int i = 0; i < codeBufPtr; i++) outBuf.Add(codeBuf[i]);
						codesize += codeBufPtr;
						codeBuf[0] = 0;
						codeBufPtr = mask = 1;
					}
					int lastMatchLength = matchLength;
					int k;
					for (k = 0; k < lastMatchLength && (c = Getc()) != -1; k++)
					{
						DeleteNode(s);
						textBuf[s] = (byte)c;
						if (s < F - 1) textBuf[s + N] = (byte)c;
						s = (s + 1) & (N - 1);
						r = (r + 1) & (N - 1);
						InsertNode(r);
					}
					textsize += k;
					while (k++ < lastMatchLength)
					{
						DeleteNode(s);
						s = (s + 1) & (N - 1);
						r = (r + 1) & (N - 1);
						if (--len != 0) InsertNode(r);
					}
				} while (len > 0);
				if (codeBufPtr > 1)
				{
					for (int i = 0; i < codeBufPtr; i++) outBuf.Add(codeBuf[i]);
					codesize += codeBufPtr;
				}
				return textsize > codesize ? outBuf.ToArray() : null;
			}
		}
	}
}
