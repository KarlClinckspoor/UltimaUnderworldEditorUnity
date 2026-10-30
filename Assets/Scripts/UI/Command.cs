using System;
using System.Collections.Generic;
using UnityEngine;

public class Command {

	public string Description { get; protected set; }

	public virtual void Do() { }
	public virtual void Undo() { }
}

/// <summary>
/// An undoable edit of a level's objects (moving, deleting, putting in or taking out of a
/// container), kept as snapshots instead of a hand-written inverse: the level block as it
/// would be saved (tiles, objects, free lists, active critters, UW2's overlays), UW1's overlay
/// block, and the text string blocks a deleted book or writing clears. The old commands undid
/// each action by hand and left the lists wrong (a restored bag could contain itself).
///
/// Undo is a three-way merge: each byte (and text string) that is still as the action left it
/// goes back to how it was before; anything edited since keeps that edit. The level is then
/// read back from the merged block as at load and rebuilt in the scene (MapCreator.ReplaceLevel),
/// so every object is a fresh one: selections and open panels on that level are closed.
/// </summary>
public class SnapshotCommand : Command
{
	readonly int level;
	readonly Action action;
	State before, after;

	sealed class State
	{
		public byte[] Block, Overlay;
		public Dictionary<int, List<string>> Texts;
	}

	public SnapshotCommand(int level, string description, Action action)
	{
		this.level = level;
		this.action = action;
		Description = description;
	}

	public override void Do()
	{
		before = Capture(level);
		action();
		after = Capture(level);
	}

	public override void Undo()
	{
		if (before == null || after == null || MapCreator.LevelData == null || level < 1 || level > MapCreator.LevelData.Count)
			return;
		var now = Capture(level);
		byte[] block = Merge(before.Block, after.Block, now.Block);
		byte[] overlay = Merge(before.Overlay, after.Overlay, now.Overlay);
		MergeTexts(before.Texts, after.Texts);

		var old = MapCreator.LevelData[level - 1];
		var ld = LevArk.LevelFrom(level, block, overlay, old.SourceTextureBlock);
		// what the snapshot does not cover stays as it is now
		ld.FloorTextures = old.FloorTextures;
		ld.WallTextures = old.WallTextures;
		ld.DoorTextures = old.DoorTextures;
		ld.Present = old.Present;
		ld.SourceBlock = old.SourceBlock;
		ld.SourceTextureBlock = old.SourceTextureBlock;
		ld.SourceOverlayBlock = old.SourceOverlayBlock;
		ld.Sectors = old.Sectors;
		MapCreator.ReplaceLevel(level, ld);
	}

	static State Capture(int level)
	{
		var ld = MapCreator.LevelData[level - 1];
		var s = new State();
		s.Block = LevArk.BuildLevelBlock(ld, level);
		s.Overlay = LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW1 ? LevArk.BuildUw1OverlayBlock(ld) : null;
		s.Texts = new Dictionary<int, List<string>>();
		var sd = MapCreator.StringData;
		if (sd != null)
			foreach (var b in sd.Blocks)
				if (b.BlockNumber < 0x0E00)   // the game's own blocks (books, writings, text traps...), not conversations
					s.Texts[b.BlockNumber] = new List<string>(b.Strings);
		return s;
	}

	/// <summary>Each byte still as after takes its value from before; the rest stay as now.</summary>
	static byte[] Merge(byte[] before, byte[] after, byte[] now)
	{
		if (now == null) return before;
		if (before == null || after == null || before.Length != now.Length || after.Length != now.Length) return now;
		var r = (byte[])now.Clone();
		for (int i = 0; i < r.Length; i++)
			if (now[i] == after[i]) r[i] = before[i];
		return r;
	}

	/// <summary>The same merge for the text strings, in place in StringData.</summary>
	static void MergeTexts(Dictionary<int, List<string>> before, Dictionary<int, List<string>> after)
	{
		var sd = MapCreator.StringData;
		if (sd == null) return;
		foreach (var b in sd.Blocks)
		{
			List<string> was, then;
			if (!before.TryGetValue(b.BlockNumber, out was) || !after.TryGetValue(b.BlockNumber, out then)) continue;
			for (int i = 0; i < b.Strings.Count && i < was.Count && i < then.Count; i++)
				if (b.Strings[i] == then[i]) b.Strings[i] = was[i];
		}
	}
}
