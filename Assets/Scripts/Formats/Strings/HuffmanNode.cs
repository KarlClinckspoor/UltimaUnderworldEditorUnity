// Moved from Data/StringData.cs (Unity-free, Formats assembly). The code is unchanged.
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



	/// <summary>Leaf node by character, from the tree STRINGS.PAK was read with. UW2's tree
	/// has other characters (and another leaf order) than UW1's, which the table below lists.</summary>
	static System.Collections.Generic.Dictionary<char, int> leaves;

	public static void SetTree(HuffmanNode[] nodes)
	{
		leaves = new System.Collections.Generic.Dictionary<char, int>();
		if (nodes == null) return;
		for (int i = 0; i < nodes.Length; i++)
			if (nodes[i] != null && (nodes[i].Left == 255 || nodes[i].Right == 255) && !leaves.ContainsKey((char)nodes[i].Char))
				leaves[(char)nodes[i].Char] = i;
	}

	public static int GetNumber(char c)
	{
		if (leaves != null && leaves.Count > 0)
		{
			int n;
			return leaves.TryGetValue(c, out n) ? n : -1;
		}
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
