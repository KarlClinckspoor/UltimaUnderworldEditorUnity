// Moved from Data/StringData.cs (Unity-free, Formats assembly). Changes: Debug.LogFormat goes
// through FormatsLog, and the NPC name in GetDescription comes from the NpcName hook.
using System.Collections.Generic;
using UltimateEditor.Games;

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
		FormatsLog.Info(string.Format("Adding string {0} to block {1}, count : {2}", str, BlockNumber, Strings.Count));
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
		FormatsLog.Info(string.Format("Adding string {0} to block {1}, count : {2}", str, BlockNumber, Strings.Count));
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
	/// <summary>The NPC name of a conversation slot, for GetDescription. The editor sets it
	/// (StringData's constructor) to MapCreator.StringData.GetNPCName, the call it used to make here.</summary>
	public static System.Func<int, string> NpcName;

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
			return "Conversation (" + (NpcName != null ? NpcName(i - 3584) : "slot " + (i - 3584)) + ")";
		if (i >= 3072)
			return "Cutscenes";

		return "Unknown";
	}

}
