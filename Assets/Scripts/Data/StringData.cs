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

public class HuffmanNode  {
	public int Char;
	public int Current;
	public int Parent;
	public int Left;
	public int Right;

	public HuffmanNode(int _char, int cur, int parent, int left, int right)
	{
		Char = _char;
		Current = cur;
		Parent = parent;
		Left = left;
		Right = right;
	}



	public static int GetNumber(char c)
	{
		switch (c)
		{
			case '\a': return 0;
			case '\n': return 1;
			case '\b': return 2;
			case ' ': return 3;
			case '!': return 4;
			case '"': return 5;
			case '#': return 6;
			case '&': return 7;
			case '\'': return 8;
			case '(': return 9;
			case ')': return 10;
			case '*': return 11;
			case '+': return 12;
			case ',': return 13;
			case '-': return 14;
			case '.': return 15;
			case '/': return 16;
			case '0': return 17;
			case '1': return 18;
			case '2': return 19;
			case '3': return 20;
			case '4': return 21;
			case '5': return 22;
			case '6': return 23;
			case '7': return 24;
			case '8': return 25;
			case '9': return 26;
			case ':': return 27;
			case ';': return 28;
			case '<': return 29;
			case '=': return 30;
			case '>': return 31;
			case '?': return 32;
			case '@': return 33;
			case 'A': return 34;
			case 'B': return 35;
			case 'C': return 36;
			case 'D': return 37;
			case 'E': return 38;
			case 'F': return 39;
			case 'G': return 40;
			case 'H': return 41;
			case 'I': return 42;
			case 'J': return 43;
			case 'K': return 44;
			case 'L': return 45;
			case 'M': return 46;
			case 'N': return 47;
			case 'O': return 48;
			case 'P': return 49;
			case 'Q': return 50;
			case 'R': return 51;
			case 'S': return 52;
			case 'T': return 53;
			case 'U': return 54;
			case 'V': return 55;
			case 'W': return 56;
			case 'X': return 57;
			case 'Y': return 58;
			case 'Z': return 59;
			case '\\': return 60;
			case '^': return 61;
			case '_': return 62;
			case '`': return 63;
			case 'a': return 64;
			case 'b': return 65;
			case 'c': return 66;
			case 'd': return 67;
			case 'e': return 68;
			case 'f': return 69;
			case 'g': return 70;
			case 'h': return 71;
			case 'i': return 72;
			case 'j': return 73;
			case 'k': return 74;
			case 'l': return 75;
			case 'm': return 76;
			case 'n': return 77;
			case 'o': return 78;
			case 'p': return 79;
			case 'q': return 80;
			case 'r': return 81;
			case 's': return 82;
			case 't': return 83;
			case 'u': return 84;
			case 'v': return 85;
			case 'w': return 86;
			case 'x': return 87;
			case 'y': return 88;
			case 'z': return 89;
			case '|': return 90;
			//case '|': return 91;
			//case '|': return 92;
			//case '|': return 93;
			default:
				return -1;
		}
	}

	public override string ToString()
	{
		return string.Format("Node char {0}, current {1}, parent {2}, left {3}, right {4}, char {5}", Char, Current, Parent, Left, Right, (char)Char);
	}
}

public class StringBlock
{
	public int BlockNumber;
	public long Offset;
	public int StringCount;

	public List<int> Offsets;
	public List<string> Strings;

	public override string ToString()
	{
		return string.Format("String block, number {0}, offset {1}, str.count {2}", BlockNumber, Offset, StringCount);
	}

	public string GetDescription()
	{
		return GetDescription(BlockNumber);
	}


	public static int GetConversationBlock(int conversationSlot)
	{
		return 3584 + conversationSlot;
	}

	public int AddString(string str)
	{
		Debug.LogFormat("Adding string {0} to block {1}, count : {2}", str, BlockNumber, Strings.Count);
		ValidateString(str);
		Strings.Add(str);
		StringCount++;
		return Strings.Count - 1;
	}

	public static string[] GetDefaultBarterStrings()
	{
		string[] bs = new string[16];
		bs[0] = "I make thee this offer.";
		bs[1] = "I demand that thou givest me these items.";
		bs[2] = "Excuse me, I must take to time to consider this deal.";
		bs[3] = "I do not wish to barter any further.";
		bs[4] = "Farewell.";
		bs[5] = "I accept thy offer.";
		bs[6] = "No, I do not like this deal.";
		bs[7] = "Thou canst not be serious.";
		bs[8] = "I am weary of this haggling.";
		bs[9] = "Is this some kind of a joke?";
		bs[10] = "Art thou going to take my belingings by force?";
		bs[11] = "Yes, I must.";
		bs[12] = "No, thou dost misunderstand me.";
		bs[13] = "I know not what items you mean.";
		bs[14] = "If thou dost insist, thou canst have them.";
		bs[15] = "No! Thou shalt not take them!";
		return bs;
	}

	public int AddTempString(string str)
	{
		Debug.LogFormat("Adding string {0} to block {1}, count : {2}", str, BlockNumber, Strings.Count);
		ValidateString(str);
		Strings.Add(str);
		return Strings.Count - 1;
	}

	private void ValidateString(string str)
	{
		foreach (var letter in str)
		{
			int number = HuffmanNode.GetNumber(letter);
			if (number == -1)
				throw new System.Exception(string.Format("Invalid character ({0}) in string : {1}", letter, str));
		}
	}

	public void ResetStrings()
	{
		for (int i = Strings.Count; i > StringCount; i--)
		{
			Strings.RemoveAt(i - 1);
		}
	}
	public static string GetDescription(int i)
	{
		switch (i)
		{
			case 1:
				return "General game and UI strings";
			case 2:
				return "Attributes, skills, mantras";
			case 3:
				return "Scrolls and books";
			case 4:
				return "Item names";
			case 5:
				return "Adverbs, key descriptions";
			case 6:
				return "Spell effects";
			case 7:
				return "Appraising, NPC names";
			case 8:
				return "Wall & grave writings, 3D object names";
			case 9:
				return "Text traps";
			case 10:
				return "Wall, floor descriptions";
			case 24:
				return "Debug strings";
			default:
				break;
		}

		if (i >= 3585)
			return "Conversation (" + MapCreator.StringData.GetNPCName(i - 3584) + ")";
		if (i >= 3072)
			return "Cutscenes";

		return "Unknown";
	}

}
