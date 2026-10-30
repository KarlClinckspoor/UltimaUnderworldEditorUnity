using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StringData
{
	public HuffmanNode[] Nodes;
	public List<StringBlock> Blocks;
	public Dictionary<int, int> BlockDictionary;

	public StringData(HuffmanNode[] nodes, List<StringBlock> blocks, Dictionary<int, int> blockDict)
	{
		Nodes = nodes;
		Blocks = blocks;
		BlockDictionary = blockDict;
		// StringBlock (Formats assembly) cannot see MapCreator; give it the NPC names
		StringBlock.NpcName = slot => MapCreator.StringData.GetNPCName(slot);
	}

	// The strings as last read or saved (block number, then its strings), so a save can tell
	// whether STRINGS.PAK needs writing at all (DataWriter.SaveStringData).
	List<KeyValuePair<int, string[]>> snapshot;

	/// <summary>Remembers the strings as they are now: called after reading and after saving.</summary>
	public void TakeSnapshot()
	{
		snapshot = new List<KeyValuePair<int, string[]>>(Blocks.Count);
		foreach (var b in Blocks)
			snapshot.Add(new KeyValuePair<int, string[]>(b.BlockNumber, Take(b)));
	}

	static string[] Take(StringBlock b)
	{
		int n = b.Strings == null ? 0 : System.Math.Min(b.StringCount, b.Strings.Count);
		var s = new string[n];
		for (int i = 0; i < n; i++) s[i] = b.Strings[i];
		if (b.Strings != null && b.StringCount > b.Strings.Count) System.Array.Resize(ref s, b.StringCount);   // keeps the count in the comparison
		return s;
	}

	/// <summary>Whether any block, string or string count differs from the last snapshot (true
	/// when none was taken). Block order does not count: the file is written sorted.</summary>
	public bool ChangedSinceSnapshot()
	{
		if (snapshot == null || snapshot.Count != Blocks.Count) return true;
		var byNumber = new Dictionary<int, string[]>();
		foreach (var kv in snapshot)
		{
			if (byNumber.ContainsKey(kv.Key)) return true;
			byNumber[kv.Key] = kv.Value;
		}
		foreach (var b in Blocks)
		{
			string[] was;
			if (!byNumber.TryGetValue(b.BlockNumber, out was)) return true;
			string[] now = Take(b);
			if (now.Length != was.Length) return true;
			for (int i = 0; i < now.Length; i++)
				if (!string.Equals(now[i], was[i], System.StringComparison.Ordinal)) return true;
		}
		return false;
	}

	public void SortBlocks()
	{
		Blocks.Sort((a, b) => a.BlockNumber.CompareTo(b.BlockNumber));
		for (int i = 0; i < Blocks.Count; i++)
		{
			BlockDictionary[Blocks[i].BlockNumber] = i;
		}
	}

	public StringBlock AddNewBlock(int convSlot)
	{
		StringBlock block = new StringBlock();
		block.Strings = new List<string>();
		int blockNumber = block.BlockNumber = StringBlock.GetConversationBlock(convSlot);
		if (BlockDictionary.ContainsKey(blockNumber))
		{
			Blocks[BlockDictionary[blockNumber]] = block;
			return block;
		}
		//block.Offset & block.Offsets are not needed for exporting
		Blocks.Add(block);
		int newBlockIndex = Blocks.Count - 1;
		BlockDictionary[blockNumber] = newBlockIndex;
		Debug.LogFormat("Adding new block, conversation slot : {0}, block number : {1}", convSlot, blockNumber);
		return block;
	}

	public StringBlock GetMantraBlock() => Blocks[1];
	public StringBlock GetScrollBlock() => Blocks[2];
	public StringBlock GetObjectNameBlock() => Blocks[3];
	public StringBlock GetKeyBlock() => Blocks[4];
	public StringBlock GetNPCNameBlock() => Blocks[6];
	public StringBlock GetWritingBlock() => Blocks[7];
	public StringBlock GetTextTrapBlock() => Blocks[8];
	public StringBlock GetWallFloorBlock() => Blocks[9];

	public int GetNextEmptyTextTrapString()
	{
		StringBlock sb = GetTextTrapBlock();
		for (int i = 0; i < sb.Strings.Count; i++)
		{
			if (sb.Strings[i] == "")
				return i;
		}
		return -1;
	}
	public bool ClearTextTrapString(StaticObject so)
	{
		int level = so.Quality * 32;
		int index = level + so.Owner;
		StringBlock sb = GetTextTrapBlock();
		if (index > -1 && index < sb.Strings.Count)
		{
			sb.Strings[index] = "";
			return true;
		}
		else
			return false;
	}
	public bool SetTextTrapString(StaticObject so, string newStr)
	{
		newStr += "\n";
		int level = so.Quality * 32;
		int index = level + so.Owner;
		StringBlock sb = GetTextTrapBlock();
		if (index > -1 && index < sb.Strings.Count)
		{
			sb.Strings[index] = newStr;
			return true;
		}
		else
			return false;
	}
	public string GetTextTrapString(StaticObject so)
	{
		int level = so.Quality * 32;
		int index = level + so.Owner;
		StringBlock sb = GetTextTrapBlock();
		if (index > -1 && index < sb.Strings.Count)
			return sb.Strings[index];
		return "INVALID";
	}
	public int GetNextEmptyWritingSlot()
	{
		StringBlock sb = GetWritingBlock();
		for (int i = 64; i < 352; i++)
		{
			if (sb.Strings[i] == "")
				return i;
		}
		return -1;
	}
	public int GetNextEmptyGraveSlot()
	{
		StringBlock sb = GetWritingBlock();
		for (int i = 1; i < 64; i++)
		{
			if (sb.Strings[i] == "")
				return i;
		}
		return -1;
	}
	public int GetNextEmptyScrollSlot()
	{
		StringBlock sb = GetScrollBlock();
		for (int i = 0; i < sb.Strings.Count; i++)	//NOTSURE: if adding more than 512 is possible
		{
			if (sb.Strings[i] == "")
				return i;
		}
		return -1;
	}
	public bool ClearWritingSlot(StaticObject so)
	{
		StringBlock sb = GetWritingBlock();
		int index = so.Special - 512;
		if (index >= 64 && index < 352)
		{
			sb.Strings[index] = "";
			return true;
		}
		else
			return false;
	}
	public bool ClearGraveSlot(StaticObject so)
	{
		StringBlock sb = GetWritingBlock();
		int index = so.Special - 512;
		if(index >= 1 && index < 64)
		{
			sb.Strings[index] = "";
			return true;
		}
		else
			return false;
	}
	public bool ClearScrollSlot(StaticObject so)
	{
		StringBlock sb = GetScrollBlock();
		if (so.IsEnchanted)
			return false;
		int index = so.Special - 512;
		if (index >= 1 && index < sb.Strings.Count)
		{
			sb.Strings[index] = "";
			return true;
		}
		else
			return false;
	}
	public bool SetWriting(StaticObject so, string newStr)
	{
		StringBlock sb = GetWritingBlock();
		int index = so.Special - 512;
		if (index >= 64 && index < 352)
		{
			sb.Strings[index] = newStr;
			return true;
		}
		else
			return false;
	}
	public bool SetGrave(StaticObject so, string newStr)
	{
		if(newStr == "")		
			newStr = "Something indecipherable";
		StringBlock sb = GetWritingBlock();
		int index = so.Special - 512;
		if (index >= 1 && index < 64)
		{
			sb.Strings[index] = newStr;
			return true;
		}
		else
			return false;
	}
	public bool SetScroll(StaticObject so, string newStr)
	{
		if (newStr == "")
			newStr = "Blank pages";
		StringBlock sb = GetScrollBlock();
		int index = so.Special - 512;
		if(index >= 1 && index < sb.Strings.Count)
		{
			sb.Strings[index] = newStr;
			return true;
		}
		else
			return false;
	}
	public string GetWriting(StaticObject so)
	{
		StringBlock sb = GetWritingBlock();
		int index = so.Special - 512;
		if (index >= 64 && index < 352)
			return sb.Strings[index];
		else
			return "INVALID";
	}
	public string GetGrave(StaticObject so)
	{
		StringBlock sb = GetWritingBlock();
		int index = so.Special - 512;
		if (index >= 1 && index < 64)
			return sb.Strings[index];
		else
			return "INVALID or grave picture";
	}
	public string GetScroll(StaticObject so)
	{
		StringBlock sb = GetScrollBlock();
		int index = so.Special - 512;
		if (index >= 1 && index < sb.Strings.Count)
			return sb.Strings[index];
		else
			return "INVALID";
	}
	public string GetNPCName(int whoami)
	{
		if (whoami > -1 && whoami < 256)
			return GetNPCNameBlock().Strings[whoami + 16];
		return "";
	}
	public void SetNPCName(int slot, string name)
	{
		if (slot > -1 && slot < 256)
			GetNPCNameBlock().Strings[slot + 16] = name;
	}
	public string GetSkillName(int index)
	{
		if (index > 19 || index < 0)
			return "invalid";
		return Blocks[1].Strings[index + 31];
	}
	public string GetMantraDescription(int index)
	{
		if (index < 20)
			return GetSkillName(index);
		if (index == 20)
			return "Cup of Wonder";
		if (index == 21)
			return "Tri part key";
		if (index == 22)
			return "Unknown";
		if (index == 23)
			return "Offensive";
		if (index == 24)
			return "Magic";
		if (index == 25)
			return "Miscellaneous";

		return "invalid";
	}
	public string[] GetRaces()
	{
		string[] races = new string[29];
		for (int i = 0; i < 29; i++)
			races[i] = GetRace(i);
		return races;
	}

	public string GetRace(int owner)
	{
		switch (owner)
		{
			case 0: return "none";
			case 1: return "rotworm";
			case 2: return "slug";
			case 3: return "bat";
			case 4: return "rat";
			case 5: return "spider";
			case 6: return "green goblin";
			case 7: return "skeleton";
			case 8: return "imp";
			case 9: return "gray goblin";
			case 10: return "mountainman";
			case 11: return "lizardman";
			case 12: return "lurker";
			case 13: return "knight";
			case 14: return "headless";
			case 15: return "troll";
			case 16: return "ghost";
			case 17: return "ghoul";
			case 18: return "gazer";
			case 19: return "mage";
			case 20: return "golem";
			case 21: return "shadow beast";
			case 22: return "reaper";
			case 23: return "fire elemental";
			case 24: return "wisp";
			case 25: return "Tyball";
			case 26: return "Slasher";
			case 27: return "outcast";
			case 28: return "creature";
			default:
				return "invalid";
		}
	}
	public bool IsRace(int i)
	{
		if (i > -1 && i < 29)
			return true;
		return false;
	}

	public void RemoveBlock(int blockNumber)
	{
		if (BlockDictionary.ContainsKey(blockNumber))
		{
			int blockIndex = BlockDictionary[blockNumber];
			Blocks.RemoveAt(blockIndex);
			BlockDictionary.Remove(blockNumber);
			Debug.LogFormat("Removing block, block number : {0}", blockNumber);
		}
		else
			Debug.LogFormat("Tried to remove string block {0} but not present in dictionary", blockNumber);
	}
}
