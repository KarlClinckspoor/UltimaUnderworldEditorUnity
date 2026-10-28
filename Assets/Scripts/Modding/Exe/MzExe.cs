using System;
using System.Collections.Generic;
using System.IO;

namespace UltimateEditor.Modding.Exe
{
	/// <summary>
	/// The layout of UW.EXE / UW2.EXE: an MZ load module followed by Borland's FBOV overlays.
	/// Each overlay segment has a stub in the load module whose header says where its code
	/// lies in the file (from the end of the FBOV header), how long it is, and how long its
	/// relocation table is. UltimaHacks grows an overlay by copying its code to the end of the
	/// file and pointing the stub there, so a code offset is portable as (segment, offset) but
	/// not as a file offset. Layout as UltimaPatcher reads it (MzHeader, FbovHeader,
	/// SegmentTableEntry, OverlayStub, the relocation tables).
	/// </summary>
	public sealed class MzExe
	{
		public sealed class Segment
		{
			public int Index;
			/// <summary>Paragraph of the segment in the load module.</summary>
			public int Base;
			public int Flags, StartOffset, EndOffset;
			public bool IsCode { get { return (Flags & 1) != 0; } }
			public bool IsOverlay { get { return (Flags & 2) != 0; } }
			/// <summary>Where the segment (for an overlay, its stub) starts in the file.</summary>
			public int StartInFile;
			/// <summary>Overlays only: where the code is, its length, and its relocation table (offsets in the code).</summary>
			public int OverlayStart = -1, CodeSize, RelocationTableStart, RelocationTableLength;
			public List<int> Relocations = new List<int>();
		}

		public readonly byte[] File;
		public readonly int LoadModuleStart, MzFileSize, FbovEnd;
		/// <summary>Load-module relocations, as offsets in the load module of the word DOS fixes up.</summary>
		public readonly List<int> LoadModuleRelocations = new List<int>();
		public readonly List<Segment> Segments = new List<Segment>();

		const int Paragraph = 16, FbovHeaderLength = 16, SegmentEntryLength = 8;

		public MzExe(byte[] file)
		{
			File = file;
			if (file.Length < 0x1C || file[0] != 'M' || file[1] != 'Z') throw new InvalidDataException("not an MZ executable");
			int lastPage = U16(2), pages = U16(4), relocCount = U16(6), headerParas = U16(8), relocStart = U16(0x18);
			MzFileSize = pages * 512 - (lastPage == 0 ? 0 : 512 - lastPage);
			LoadModuleStart = headerParas * Paragraph;
			for (int i = 0; i < relocCount; i++)
			{
				int off = U16(relocStart + 4 * i), seg = U16(relocStart + 4 * i + 2);
				LoadModuleRelocations.Add(seg * Paragraph + off);
			}
			if (MzFileSize + FbovHeaderLength > file.Length || file[MzFileSize] != 'F' || file[MzFileSize + 1] != 'B' || file[MzFileSize + 2] != 'O' || file[MzFileSize + 3] != 'V')
				throw new InvalidDataException("no FBOV overlay header after the load module");
			FbovEnd = MzFileSize + FbovHeaderLength;
			int tableStart = (int)U32(MzFileSize + 8), count = (int)U32(MzFileSize + 12);
			for (int i = 0; i < count; i++)
			{
				int e = tableStart + i * SegmentEntryLength;
				var s = new Segment { Index = i, Base = U16(e), EndOffset = U16(e + 2), Flags = U16(e + 4), StartOffset = U16(e + 6) };
				s.StartInFile = LoadModuleStart + s.Base * Paragraph;
				if (s.IsOverlay)
				{
					if (file[s.StartInFile] != 0xCD) throw new InvalidDataException("overlay stub " + i + " does not start with INT (0xCD)");
					s.OverlayStart = FbovEnd + (int)U32(s.StartInFile + 4);
					s.CodeSize = U16(s.StartInFile + 8);
					s.RelocationTableLength = U16(s.StartInFile + 0x0A);
					s.RelocationTableStart = s.OverlayStart + s.CodeSize;
					for (int r = 0; r + 1 < s.RelocationTableLength; r += 2) s.Relocations.Add(U16(s.RelocationTableStart + r));
				}
				Segments.Add(s);
			}
		}

		/// <summary>A place in the executable, kept in a form that survives UltimaHacks.</summary>
		public struct Location
		{
			/// <summary>Overlay segment, -1 for the load module, -2 for anything else (headers, tables).</summary>
			public int Segment;
			public int Offset;
			public override string ToString() { return Segment == -1 ? string.Format("load module +0x{0:X}", Offset) : Segment == -2 ? string.Format("file 0x{0:X}", Offset) : string.Format("overlay {0} +0x{1:X}", Segment, Offset); }
		}

		public Location Locate(int fileOffset)
		{
			foreach (var s in Segments)
				if (s.IsOverlay && fileOffset >= s.OverlayStart && fileOffset < s.OverlayStart + s.CodeSize)
					return new Location { Segment = s.Index, Offset = fileOffset - s.OverlayStart };
			if (fileOffset >= LoadModuleStart && fileOffset < MzFileSize) return new Location { Segment = -1, Offset = fileOffset - LoadModuleStart };
			return new Location { Segment = -2, Offset = fileOffset };
		}

		/// <summary>The file offset of a location in this executable, or -1 when it has no such place.</summary>
		public int FileOffset(Location l)
		{
			if (l.Segment == -1) return LoadModuleStart + l.Offset < MzFileSize ? LoadModuleStart + l.Offset : -1;
			if (l.Segment == -2) return l.Offset < File.Length ? l.Offset : -1;
			if (l.Segment < 0 || l.Segment >= Segments.Count) return -1;
			var s = Segments[l.Segment];
			if (!s.IsOverlay || l.Offset >= s.CodeSize) return -1;
			return s.OverlayStart + l.Offset;
		}

		/// <summary>Where DOS (load module) or the overlay manager (overlay code) adds a segment to a
		/// word: file offsets of the first byte of each such word.</summary>
		public HashSet<int> RelocatedWords()
		{
			var r = new HashSet<int>();
			foreach (var a in LoadModuleRelocations) r.Add(LoadModuleStart + a);
			foreach (var s in Segments)
				if (s.IsOverlay) foreach (var a in s.Relocations) r.Add(s.OverlayStart + a);
			return r;
		}

		/// <summary>True when one of the bytes [offset, offset + count) belongs to a relocated word.</summary>
		public bool TouchesRelocation(int offset, int count, HashSet<int> relocated = null)
		{
			relocated = relocated ?? RelocatedWords();
			for (int i = offset - 1; i < offset + count; i++) if (relocated.Contains(i)) return true;
			return false;
		}

		/// <summary>The bytes of a live code region holding the location (the load module, or the
		/// overlay's current code), for a context search: [start, end) in the file.</summary>
		public void Region(Location l, out int start, out int end)
		{
			if (l.Segment == -1) { start = LoadModuleStart; end = MzFileSize; return; }
			if (l.Segment >= 0 && l.Segment < Segments.Count && Segments[l.Segment].IsOverlay)
			{
				var s = Segments[l.Segment];
				start = s.OverlayStart; end = s.OverlayStart + s.CodeSize; return;
			}
			start = 0; end = File.Length;
		}

		int U16(int p) { return File[p] | File[p + 1] << 8; }
		uint U32(int p) { return (uint)(File[p] | File[p + 1] << 8 | File[p + 2] << 16 | File[p + 3] << 24); }
	}
}
