using System.Collections;
using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

public class ConverterStatus
{
	public string Message;
	public ConverterStatus(string str)
	{
		Message = str;
	}
}
public enum WordType
{
	Null,
	Word,
	Number,
	Operator,
	Separator,
	Control,
	Punctuation,
	Surrogate,
}
public static class ConversationConverter {

	private static Dictionary<string, int> vars = new Dictionary<string, int>();
	private static Dictionary<string, int> localVars = new Dictionary<string, int>();		//cleared after each block
	private static Dictionary<string, int> operationVars = new Dictionary<string, int>();   //cleared after each line
	private static Dictionary<string, string> arrayPointers = new Dictionary<string, string>();
	private static int operationCount;
	private static int localCount;
	private static int operationVarsOffset;
	private static int localVarsOffset;
	//private static Dictionary<string, int> globalVars = new Dictionary<string, int>();
	//private static List<string> vars = new List<string>();
	private static int currentLine;
	private static ConversationNode currentNode;
	private static List<ConversationNode> nodeList;
	private static Dictionary<ConversationNode, int> nodeAdresses = new Dictionary<ConversationNode, int>();
	private static Dictionary<ConversationNode, List<int>> jumps = new Dictionary<ConversationNode, List<int>>();
	private static string[] barterStrings;
	//private static Dictionary<string, int> genderDict = new Dictionary<string, int>();
	
	private static Conversation conv;
	private static StringBlock sb;
	private static ConversationNode startingNode;
	private static string[] keywords =
	{
		"array",
		"global",
		"local"
	};

	private static string maleHandle = "";
	private static string femaleHandle = "";

	private static ConverterFunction[] functions =
	{
		new ConverterFunction("Jump", "void", "string"),
		new ConverterFunction("End", "void"),

		new ConverterFunction("Say", "void", "string"),
		new ConverterFunction("Description", "void", "string"),

		new ConverterFunction("Responses", "void"),
		new ConverterFunction("ResponsesIf", "void"),

		new ConverterFunction("Ask", "int"),
		new ConverterFunction("Compare", "int", "string", "int"),
		new ConverterFunction("Contains", "int", "string", "int"),
		new ConverterFunction("Random", "int", "int"),
		new ConverterFunction("ValueOf", "int", "int"),

		new ConverterFunction("GetAttitude", "int"),
		new ConverterFunction("GetTalked", "int"),
		new ConverterFunction("GetHomeX", "int"),
		new ConverterFunction("GetHomeY", "int"),
		new ConverterFunction("GetProperty", "int", "string"),
		new ConverterFunction("SetProperty", "void", "string", "int"),

		new ConverterFunction("GetQuest", "int", "int"),
		new ConverterFunction("SetQuest", "void", "int", "int"),
		new ConverterFunction("GetSex", "int", "string", "string"),

		new ConverterFunction("ArrayContains", "int", "array", "int"),
		new ConverterFunction("GetInventorySlots", "int", "array", "array"),
		new ConverterFunction("FindItemInBarter", "int", "int"),
		new ConverterFunction("FindItem", "int", "int", "int"),
		new ConverterFunction("CountItem", "int", "int"),

		new ConverterFunction("GiveMany", "int", "array", "int"),
		new ConverterFunction("GiveItem", "int", "int", "int"),
		new ConverterFunction("TakeItemByPos", "int", "int"),
		new ConverterFunction("TakeItemByID", "int", "int"),
		new ConverterFunction("GetNPCItemPos", "int", "int"),

		new ConverterFunction("CreateItem", "int", "int"),
		new ConverterFunction("RemoveItem", "int", "int"),
		new ConverterFunction("PlaceItem", "void", "int", "int", "int"),
		new ConverterFunction("HandleDoor", "int", "int", "int", "int"),

		new ConverterFunction("GetItemQuality", "int", "int"),
		new ConverterFunction("GetItemOwner", "int", "int"),
		new ConverterFunction("GetItemDirection", "int", "int"),
		new ConverterFunction("GetItemSpecial", "int", "int"),
		new ConverterFunction("GetItemFlags", "int", "int"),

		new ConverterFunction("SetItemDirection", "void", "int", "int"),
		new ConverterFunction("SetItemOwner", "void", "int", "int"),
		new ConverterFunction("SetItemSpecial", "void", "int", "int"),
		new ConverterFunction("SetItemFlags", "void", "int", "int"),
		new ConverterFunction("SetItemQuality", "int", "int", "int"),
		new ConverterFunction("IdentifyItem", "void", "int"),

		new ConverterFunction("GetSkill", "int", "int"),
		new ConverterFunction("AddSkill", "int", "int"),

		new ConverterFunction("GetTrapVariable", "int", "int"),
		new ConverterFunction("SetTrapVariable", "int", "int", "int"),

		new ConverterFunction("SetRaceAttitude", "void", "int", "int", "int"),
		new ConverterFunction("SetNPCAttitude", "void", "int", "int"),
		new ConverterFunction("SetAttitude", "void", "int"),

		new ConverterFunction("RemoveNPC", "void"),

		new ConverterFunction("Barter", "void"),

		new ConverterFunction("GetItemArg3", "int", "int"),
		new ConverterFunction("GetItemArg2", "int", "int"),
		new ConverterFunction("SetItemArg3", "int", "int", "int"),
		new ConverterFunction("SetItemArg2", "int", "int", "int"),
	};

	public static void Reset()
	{
		vars = new Dictionary<string, int>();
		localVars = new Dictionary<string, int>();
		operationVars = new Dictionary<string, int>();
		nodeList = new List<ConversationNode>();
		nodeAdresses = new Dictionary<ConversationNode, int>();
		jumps = new Dictionary<ConversationNode, List<int>>();
		//genderDict = new Dictionary<string, int>();
		operationCount = 0;
		localCount = 0;
		operationVarsOffset = 0;
		localVarsOffset = 0;
		currentLine = 0;
		currentNode = null;
		conv = null;
		sb = null;
		maleHandle = "";
		femaleHandle = "";
	}
	private static bool isFunction(string word)
	{
		foreach (var func in functions)
		{
			if (word.ToLower() == func.Name.ToLower())
				return true;
		}
		return false;
	}
	private static ConverterFunction getFunction(string word)
	{
		foreach (var func in functions)
		{
			if (word.ToLower() == func.Name.ToLower())
				return func;
		}
		return null;
	}
	private static bool isVariable(string word)
	{
		if (vars.ContainsKey(word))
			return true;
		if (localVars.ContainsKey(word))
			return true;
		if (operationVars.ContainsKey(word))
			return true;
		return false;
	}
	private static bool isArray(string word)
	{
		if (arrayPointers.ContainsKey(word))
			return true;
		return false;
	}
	private static bool isKeyword(string word)
	{
		foreach (var keyword in keywords)
		{
			if (keyword == word)
				return true;
		}
		return false;
	}
	private static int getVariableAdress(string word)
	{
		if (vars.ContainsKey(word))
			return vars[word];
		else if (localVars.ContainsKey(word))
			return localVars[word];
		else if (operationVars.ContainsKey(word))
			return operationVars[word];
		return -1;
	}
	private static string getArrayVar(string arr, int i)
	{
		return arr + "_arr_" + i;
	}
	private static int getArrayAdress(string arr)
	{
		string varName = getArrayVar(arr, 0);
		if (vars.ContainsKey(varName))
			return vars[varName];
		else if (localVars.ContainsKey(varName))
			return localVars[varName];
		return -1;
	}
	private static bool checkArraySize(string name, int expectedSize)
	{
		string varName = "";
		for (int i = 0; i < expectedSize; i++)
		{
			varName = getArrayVar(name, i);
			if (vars.ContainsKey(varName))
				continue;
			if (localVars.ContainsKey(varName))
				continue;
			return false;
		}
		return true;
	}
	private static int getArraySize(string name)
	{
		string varName = "";
		int i = 0;
		while(true)
		{
			varName = getArrayVar(name, i);
			if (vars.ContainsKey(varName) || localVars.ContainsKey(varName))
			{
				i++;
				continue;
			}
			break;
		}
		return i;
	}
	private static string createOperationVariable()
	{
		if(operationVars.Count >= 100)
			throw new ConverterException("Operation limit reached (max 100 operations per line)", currentNode);
		string name = "opvar_" + operationCount;
		while (vars.ContainsKey(name) || operationVars.ContainsKey(name))
			name = "opvar_" + ++operationCount;
		
		operationVars[name] = operationCount + operationVarsOffset;
		operationCount++;
		return name;
	}
	private static void createLocalVariable(string name)
	{
		if(localVars.Count >= 100)
			throw new ConverterException("Local variable limit reached (max 100 variables per block)", currentNode);
		if (vars.ContainsKey(name))
			throw new ConverterException(string.Format("Variable of this name {0} already exists", name), currentNode);
		localVars[name] = localCount + localVarsOffset;
		localCount++;
	}
	private static void createGlobalVariable(string name)
	{
		if(vars.Count >= 100)
			throw new ConverterException("Global variable limit reached (max 100 variables)", currentNode);
		if(vars.ContainsKey(name))
			throw new ConverterException(string.Format("Variable of this name {0} already exists", name), currentNode);
		vars[name] = getNextVarAdress();
	}

	private static string getNewOperationVariable()
	{		
		string name = "opvar_" + operationCount;
		operationCount++;
		return name;
	}
	private static void clearLocals()
	{
		localVars.Clear();
		localCount = 0;
	}
	private static void clearOperations()
	{
		operationVars.Clear();
		operationCount = 0;
	}
	#region Operations
	private static void op(OperationType type) => conv.Code.Add((int)type);
	private static void start() => op(OperationType.START);

	private static void push_int(int i)
	{
		op(OperationType.PUSHI);
		if (i < 0)
		{
			i = Mathf.Abs(i);
			conv.Code.Add(i);
			op(OperationType.OPNEG);
		}
		else
			conv.Code.Add(i);
	}
	private static void new_base()
	{
		op(OperationType.PUSHBP);
		op(OperationType.SPTOBP);
	}
	private static void end_base()
	{
		op(OperationType.BPTOSP);
		op(OperationType.POPBP);
	}
	private static void add_space(int i)
	{
		push_int(i);
		op(OperationType.ADDSP);
	}
	private static void fetch(string varName)
	{
		foreach (var var in vars)
		{
			if (var.Key == varName)
			{
				fetch(var.Value);
				return;
			}
		}
		foreach (var var in localVars)
		{
			if(var.Key == varName)
			{
				fetch(var.Value);
				return;
			}
		}
		foreach (var opvar in operationVars)
		{
			if (opvar.Key == varName)
			{
				fetch(opvar.Value);
				return;
			}
		}
		throw new ConverterException(string.Format("Unknown variable name {0} in line {1}", varName, currentLine), currentNode);
	}
	private static void fetch(int i)
	{
		push_int(i);
		op(OperationType.FETCHM);
	}
	private static void add() => op(OperationType.OPADD);
	private static void sub() => op(OperationType.OPSUB);
	private static void mul() => op(OperationType.OPMUL);
	private static void div() => op(OperationType.OPDIV);
	private static void mod() => op(OperationType.OPMOD);
	private static void or() => op(OperationType.OPOR);
	private static void and() => op(OperationType.OPAND);
	private static void neg() => op(OperationType.OPNEG);
	private static void not() => op(OperationType.OPNOT);
	private static void equal() => op(OperationType.TSTEQ);
	private static void nonEqual() => op(OperationType.TSTNE);
	private static void greater() => op(OperationType.TSTGT);
	private static void greaterOrEqual() => op(OperationType.TSTGE);
	private static void less() => op(OperationType.TSTLT);
	private static void lessOrEqual() => op(OperationType.TSTLE);

	private static int branchIfFalse()
	{
		op(OperationType.BEQ);
		int pos = conv.Code.Count;
		conv.Code.Add(-1);
		return pos;
	}
	private static int branchIfTrue()
	{
		op(OperationType.BNE);
		int pos = conv.Code.Count;
		conv.Code.Add(-1);
		return pos;
	}
	private static void branchEnd(int branch, int target)
	{
		if (target < 0)
			target += 65535;
		conv.Code[branch] = target;
	}
	private static void jump(int target)
	{
		op(OperationType.JMP);
		conv.Code.Add(target);
	}
	private static void say_op(int i)
	{
		push_int(i);
		op(OperationType.SAY_OP);
	}
	private static void store(string var)
	{
		int adress = 0;
		if (vars.ContainsKey(var))
			adress = vars[var];
		else if (localVars.ContainsKey(var))
			adress = localVars[var];
		else if (operationVars.ContainsKey(var))
			adress = operationVars[var];
		else
			throw new ConverterException(string.Format("Unrecognized variable name {0} at line {1}", var, currentLine), currentNode);
		push_int(adress);
		op(OperationType.SWAP);
		op(OperationType.STO);
	}
	private static void swap_store()
	{
		op(OperationType.SWAP);
		op(OperationType.STO);
	}
	private static void store(int adress, int value)
	{
		push_int(adress);
		push_int(value);
		op(OperationType.STO);
	}
	private static void calli(int id)
	{
		op(OperationType.CALLI);
		conv.Code.Add(id);
	}
	private static void pop() => op(OperationType.POP);
	private static void pop(int c)
	{
		for (int i = 0; i < c; i++)
		{
			pop();
		}
	}

	private static void endConversation()
	{
		conv.Code.Add((int)OperationType.EXIT_OP);
	}
	#endregion
	public static void Convert(Conversation _conv, ConversationNode startNode, List<ConversationNode> nodes, List<string> savedVars, string npcname)
	{
		conv = _conv;
		sb = MapCreator.StringData.AddNewBlock(conv.Slot);
		sb.AddString("");
		MapCreator.StringData.SetNPCName(conv.Slot, npcname);
		conv.StringBlock = sb.BlockNumber;
		conv.FirstMemorySlot = 32;
		if (conv.MemorySlots != savedVars.Count + conv.FirstMemorySlot)
		{
			Debug.LogErrorFormat("conv.PrivateGlobalCount {0} != savedVars.Count {1}", conv.MemorySlots, savedVars.Count);
			return;
		}
		currentNode = startNode;
		startingNode = startNode;
		nodeList = nodes;
		barterStrings = conv.BarterStrings;
		if (savedVars.Count > 100)
			throw new ConverterException(string.Format("Saved variables limit (100) exceeded"), currentNode);
		//if (globalVars.Count > 100)
		//	throw new ConverterException(string.Format("Global variables limit (100) exceeded"));

		conv.Code = new List<int>();
		
		localVarsOffset = conv.MemorySlots + 100;
		operationVarsOffset = localVarsOffset + 100;
		for (int i = 0; i < savedVars.Count; i++)
		{
			if (string.IsNullOrEmpty(savedVars[i]))
				throw new ConverterException(string.Format("Invalid global variable name (empty) at index {0}", i), currentNode);
			string var = savedVars[i];
			if (vars.ContainsKey(var))
				throw new ConverterException(string.Format("Variable of the same name already exists : {0}", var), currentNode);
			vars[var] = getNextVarAdress();
		}
		Debug.LogFormat("Memory layout : conv.MemorySlots : {0}, savedVars.Count : {1}, localVarsOffset : {3}, operationVarsOffset : {2}", conv.MemorySlots, savedVars.Count, operationVarsOffset, localVarsOffset);
		//Predefined vars
		vars["response"] = getNextVarAdress();
		start();
		add_space(300);
		readNode(startNode);
		ConversationNode unread = getNextUnreadNode(nodeList);
		while(unread)
		{
			Debug.LogFormat("Found next unconnected node : {0}", unread);
			readNode(unread);
			unread = getNextUnreadNode(nodeList);
		}
		foreach (var jump in jumps)
		{
			foreach (var adress in jump.Value)
			{
				conv.Code[adress] = nodeAdresses[jump.Key];
			}
		}
		end();

		conv.Unk00 = 2088;
		conv.CodeSize = conv.Code.Count;
		conv.Functions = MapCreator.ConversationData.DefaultFunctions;
		conv.ImportedFunctionsAndVariables = 82;
		conv.SetConverted();
	}
	private static int getNextVarAdress()
	{
		return conv.FirstMemorySlot + vars.Count;
	}
	private static ConversationNode getNextUnreadNode(List<ConversationNode> nodeList)
	{
		foreach (var node in nodeList)
		{
			if (!nodeAdresses.ContainsKey(node))
				return node;
		}
		return null;
	}
	private static ConversationNode getNode(string name)
	{
		return nodeList.Find((node) => node.NodeName == name);
	}
	private static void readNode(ConversationNode node)
	{
		currentNode = node;
		nodeAdresses[currentNode] = conv.Code.Count;
		Debug.LogFormat("Reading next node : {0}, local count : {1}, opvar count : {2}, adress : {3}", node, localCount, operationCount, nodeAdresses[currentNode]);
		string str = node.NodeContent.text;
		clearLocals();
		readBlock(ref str);
		for (int i = 0; i < node.ConditionsGO.transform.childCount; i++)
		{
			GameObject conditionGO = node.ConditionsGO.transform.GetChild(i).gameObject;
			ConversationCondition condition = conditionGO.GetComponent<ConversationCondition>();
			ConversationNode target = condition.GetTarget();
			Debug.LogFormat("conditionGO : {0}, condition : {1}, target : {2}", conditionGO, condition, target);
			if (!target)
				continue;
			string con = "";
			if (condition.Content.gameObject.activeSelf)
				con = condition.Content.text;
			if (!string.IsNullOrEmpty(con))
			{
				Word ifWord = getNext(ref con);
				ConverterAction cAct = getAction(ifWord);
				if (cAct != ConverterAction.If)
					throw new ConverterException(string.Format("Invalid statement at condition {0}", i + 1), currentNode);
				List<Word> conditionL = getLine(ref con);
				Debug.LogFormat("condition...condition : {0}", debugWords(conditionL));
				resolveFunctions(conditionL);
				Word result = resolveParenthesis(conditionL);
				fetch(result.S);
				if(nodeAdresses.ContainsKey(target))
				{
					int start = branchIfTrue();
					branchEnd(start, nodeAdresses[target] - start + 1);
				}
				else
				{
					int start = branchIfFalse();
					readNode(target);
					branchEnd(start, conv.Code.Count - start);
				}
			}
			else
			{
				if (nodeAdresses.ContainsKey(target))
				{
					jump(nodeAdresses[target]);
				}
				else
					readNode(target);
			}
		}
		endConversation();
	}

	private static void readBlock(ref string str, bool endIf = false)
	{
		int safe = 500;
		while (str.Length > 0)
		{
			Word word = getNext(ref str);
			Debug.Log(word.S + " / " + word.T);

			ConverterAction cact = getAction(word);
			if (cact == ConverterAction.Invalid)
				throw new ConverterException("Invalid statement '" + word.S + "' at line " + currentLine, currentNode);
			if (cact == ConverterAction.EndIf)
			{
				if (!endIf)
					throw new ConverterException("Start of 'if' statement not found, endif invalid at line " + currentLine, currentNode);
				else
					break;
			}
			if (cact == ConverterAction.Null)
			{
				Debug.LogErrorFormat("Invalid statement '{0}'", word.S);
				return;
			}
			prepareAction(cact, word.S, ref str);

			Debug.Log("------------------NEXT LINE------------------");
			currentLine++;

			if (str.Length == 0)
				currentLine = 0;

			safe--;
			if (safe == 0)
			{
				Debug.LogError("Failed");
				return;
			}
		}
	}

	private static Word getNext(ref string str, WordType expected = WordType.Null, bool returnExpected = false)
	{
		string cur = "";
		bool firstChar = false;
		WordType wordType = WordType.Null;
		for (int i = 0; i < str.Length; i++)
		{
			
			char c = str[i];
			if(!firstChar)
				wordType = getWordType(c);
			firstChar = true;
			WordType curType = getWordType(c);
			//Debug.LogFormat("c : {0}, cur : {1}, wordtype : {2}, curtype : {3}, expected : {4}", c, cur, wordType, curType, expected);
			if (expected != WordType.Null && curType != expected)
			{
				if (i > 0)
				{
					str = str.Substring(i);
					if (returnExpected)
						return new Word(cur, wordType);
					else
						return null;
				}
				else
					return null;
			}
			if (wordType == WordType.Null)
			{
				Debug.LogErrorFormat("Null word type : '{0}'", c);
				return null;
			}
			
			if (cur == "-")
			{
				if(curType == WordType.Number)
					wordType = WordType.Number;
				else
				{
					str = str.Substring(i);
					return new Word(cur, wordType);
				}
			}
			else if(cur == "!")
			{
				if(c == '=')
				{
					Word punct = new Word("!=", wordType);
					str = str.Substring(i + 1);
					//Debug.LogFormat("cur : {0}, c : {1}, wordtype : {2}, curtype : {3}", cur, c, wordType, curType);
					return punct;
				}
				else
				{
					str = str.Substring(i);
					return new Word(cur, wordType);
				}
			}
			if (wordType == WordType.Word)
			{
				if (curType != WordType.Word && curType != WordType.Number)
				{
					str = str.Substring(i);
					//Debug.LogFormat("cur : {0}, c : {1}, wordtype : {2}, curtype : {3}", cur, c, wordType, curType);
					return new Word(cur, wordType);
				}
			}
			else if(wordType == WordType.Punctuation)	//All punctuations (exept - ) will be separated
			{
				if (c == '-' && i == 0)
				{
					cur += str[i];
					continue;
				}
				else if(c == '!' && i == 0)
				{
					cur += str[i];
					continue;
				}
				string s = "" + str[i];
				Word punct = new Word(s, wordType);
				str = str.Substring(i+1);
				//Debug.LogFormat("cur : {0}, c : {1}, wordtype : {2}, curtype : {3}", cur, c, wordType, curType);
				return punct;
			}
			else if (wordType != curType)
			{
				str = str.Substring(i);
				//Debug.LogFormat("cur : {0}, c : {1}, wordtype : {2}, curtype : {3}", cur, c, wordType, curType);
				return new Word(cur, wordType);
			}
			cur += str[i];
			//Debug.LogFormat("cur : {0}, c : {1}, wordtype : {2}, curtype : {3}", cur, c, wordType, curType);
		}
		str = "";
		return new Word(cur, wordType);
	}

	private static ConverterAction getAction(Word pair)
	{
		string word = pair.S;
		WordType type = pair.T;
		if (type == WordType.Control)
			return ConverterAction.NextLine;
		if (type == WordType.Punctuation)
			return ConverterAction.Invalid;
		if (type == WordType.Operator)
			return ConverterAction.Invalid;
		if (type == WordType.Surrogate)
			return ConverterAction.Invalid;
		if (type == WordType.Separator)
			return ConverterAction.Separator;
		if (type == WordType.Number)
			return ConverterAction.Invalid;
		if (type == WordType.Null)
			return ConverterAction.Invalid;
		if (type == WordType.Word)
		{
			if (isVariable(word))
				return ConverterAction.Variable;
			if (isArray(word))
				return ConverterAction.Variable;
			if (isFunction(word))
				return ConverterAction.Function;
			if (word.ToLower() == "if")
				return ConverterAction.If;
			if (word.ToLower() == "endif")
				return ConverterAction.EndIf;
			if (word.ToLower() == "local")
				return ConverterAction.Local;
			if (word.ToLower() == "global")
				return ConverterAction.Global;
			//if (word.ToLower() == "response" || word.ToLower() == "responseif")
			//	return ConverterAction.Response;
			throw new ConverterException("Unrecognized word '"+ word +"'at line " + currentLine, currentNode);
			//return ConverterAction.Invalid;
		}
		return ConverterAction.Null;
	}
	private static void createArray(ConverterAction type, ref string str)
	{
		string arrName = "";
		int size = 0;
		clearSeparators(ref str);
		Word next = getNext(ref str);
		if (next.T == WordType.Word)
		{
			if (isVariable(next.S))
				throw new ConverterException(string.Format("Variable name {0} already used, line {1}", next.S, currentLine), currentNode);
			if (isFunction(next.S))
				throw new ConverterException(string.Format("Variable name invalid, function name {0} is already using it, line {1}", next.S, currentLine), currentNode);
			if (isKeyword(next.S))
				throw new ConverterException(string.Format("Variable name invalid, {0} is a kewyord, line {1}", next.S, currentLine), currentNode);
			arrName = next.S;
		}
		if (type == ConverterAction.Global && currentNode != startingNode)
			throw new ConverterException(string.Format("Invalid array {0} at line {1}, global arrays can only be declared in starting nodes.", arrName, currentLine), currentNode);
		clearSeparators(ref str);
		next = getNext(ref str);
		if (next.S != "[")
			throw new ConverterException(string.Format("Invalid array declaration, should start with '[', at line {0}", currentLine), currentNode);
		clearSeparators(ref str);
		next = getNext(ref str);
		if (next.T != WordType.Number)
			throw new ConverterException(string.Format("Invalid array declaration, should have integer value between square braces '[ ]', at line {0}", currentLine), currentNode);
		bool success = int.TryParse(next.S, out size);
		if (!success)
			throw new ConverterException(string.Format("Invalid array declaration, should have integer value between square braces '[ ]', at line {0}", currentLine), currentNode);
		clearSeparators(ref str);
		next = getNext(ref str);
		if (next.S != "]")
			throw new ConverterException(string.Format("Invalid array declaration, should end with ']', at line {0}", currentLine), currentNode);
		clearSeparators(ref str);
		List<Word> line = getLine(ref str);
		if (line.Count > 1)
			throw new ConverterException(string.Format("Invalid operation after array declaration, you can't create array and assign values at the same line."), currentNode);
		createArray(arrName, size, type);
	}
	private static void createArray(string arrName, int size, ConverterAction type)
	{
		for (int i = 0; i < size; i++)
		{
			string varName = getArrayVar(arrName, i);
			if (isVariable(varName))
				throw new ConverterException(string.Format("Invalid array name {0}, variable name already used, line {1}", arrName, currentLine), currentNode);
			if (type == ConverterAction.Global)
				createGlobalVariable(varName);
			else if (type == ConverterAction.Local)
				createLocalVariable(varName);
		}
		string ptrName = arrName + "_ptr_0";
		if (isVariable(ptrName))
			throw new ConverterException(string.Format("Invalid array name {0}, variable name already used, line {1}", arrName, currentLine), currentNode);
		if (type == ConverterAction.Global)
			createGlobalVariable(ptrName);
		else if (type == ConverterAction.Local)
			createLocalVariable(ptrName);
		push_int(getVariableAdress(getArrayVar(arrName, 0)));
		store(ptrName);
		arrayPointers[arrName] = ptrName;
	}
	private static void prepareAction(ConverterAction cAct, string word, ref string str)
	{
		Debug.LogFormat("prepare action : {0}, {1}", cAct, word);
		if(cAct == ConverterAction.Invalid)
		{
			Debug.LogWarningFormat("Invalid action : {0}", word);
			return;
		}
		if(cAct == ConverterAction.Separator)
		{
			//nothing?
			return;
		}
		if(cAct == ConverterAction.NextLine)
		{

			return;
		}
		if(cAct == ConverterAction.Variable)
		{
			List<Word> assignment = getLine(ref str);
			doAssignment(word, assignment);
			clearOperations();
		}
		if(cAct == ConverterAction.Local || cAct == ConverterAction.Global)
		{
			while(str.Length > 0)
			{
				Word next = getNext(ref str);
				if (next.T == WordType.Word)
				{
					if (isVariable(next.S))
						throw new ConverterException(string.Format("Variable name {0} already used, line {1}", next.S, currentLine), currentNode);
					if (isFunction(next.S))
						throw new ConverterException(string.Format("Variable name invalid, function name {0} is already using it, line {1}", next.S, currentLine), currentNode);
					if (next.S.ToLower() == "array")
					{
						createArray(cAct, ref str);
						return;
					}
					if(cAct == ConverterAction.Local)
						createLocalVariable(next.S);
					else if(cAct == ConverterAction.Global)
					{
						if (currentNode == startingNode)
							createGlobalVariable(next.S);
						else
							throw new ConverterException(string.Format("Invalid variable {0} at line {1}, global variables can only be declared in starting nodes.", next.S, currentLine), currentNode);
					}
					List<Word> assignment = getLine(ref str);
					doAssignment(next.S, assignment);
					clearOperations();
					break;
				}
				else if (next.T == WordType.Separator)
					continue;
				else
					throw new ConverterException(string.Format("Invalid variable name {0} at line {1}", next.S, currentLine), currentNode);
			}
		}
		else if(cAct == ConverterAction.Function)
		{
			if(word.ToLower() == "responses")
			{
				setResponses(ref str);
				return;
			}
			if(word.ToLower() == "responsesif")
			{
				setResponsesIf(ref str);
				return;
			}
			ConverterFunction func = getFunction(word);
			if (func == null)
				throw new ConverterException(string.Format("Unrecognized function name '{0}' in line {1}", word, currentLine), currentNode);

			if(func.Arguments.Count == 0)
			{
				while(str.Length > 0)
				{
					Word first = getNext(ref str);
					Word sec = getNext(ref str);
					if(first.S != "(" || sec.S != ")")
						throw new ConverterException(string.Format("Function {0} at line {1} is not a variable, requires '()' to call.", func.Name, currentLine), currentNode);
					doFunction(word);
					return;
				}
				
			}
			Word start = getNext(ref str);
			if (start.S != "(")
				throw new ConverterException(string.Format("Invalid function argument '{0}' at line {1}", start.S, currentLine), currentNode);
			int i = 0;
			List<Word> arguments = new List<Word>();
			while (str.Length > 0)
			{
				Word next = getNext(ref str);
				//Debug.LogFormat("next : {0}", next);
				if (next.T == WordType.Separator)
					continue;
				if(i % 2 == 0)
				{
					if (next.T == WordType.Control)
						break;
					if (i / 2 >= func.Arguments.Count)
						throw new ConverterException(string.Format("Invalid argument count at line '{0}'", currentLine), currentNode);
					string expectedType = func.Arguments[i / 2];
					if (expectedType == "string")
					{
						if (next.S == "\"")
							arguments.Add(new Word(getString(ref str), WordType.Word));
						else if (isVariable(next.S))
							arguments.Add(new Word(next.S, WordType.Number));
						else
							throw new ConverterException(string.Format("Invalid function argument '{0}' at line {1}", next.S, currentLine), currentNode);
					}
					else if (expectedType == "int")
					{
						if (isArray(next.S))
							arguments.Add(new Word(getArrayIndex(next.S, ref str), WordType.Word));
						else if(next.T == WordType.Number || next.T == WordType.Word)	//Not safe?
							arguments.Add(next);
						else
							throw new ConverterException(string.Format("Invalid function argument '{0}' at line {1}", next.S, currentLine), currentNode);
					}
					else if(expectedType == "array")
					{
						if (isArray(next.S))
							arguments.Add(next);
						else
							throw new ConverterException(string.Format("Invalid function argument '{0}' at line {1}", next.S, currentLine), currentNode);
					}
					else
						throw new ConverterException(string.Format("Invalid function argument '{0}' at line {1}", next.S, currentLine), currentNode);
				}
				else
				{
					if (next.S == ")")
					{
						if ((i + 1) / 2 == func.Arguments.Count)
						{
							i++;
							continue;
						}
						else
							throw new ConverterException(string.Format("Invalid function argument '{0}' at line {1}", next.S, currentLine), currentNode);
					}
					else if (next.S != ",")
						throw new ConverterException(string.Format("Invalid function argument '{0}' at line {1}", next.S, currentLine), currentNode);
				}
				i++;
			}
			doFunction(word, arguments);
		}
		else if(cAct == ConverterAction.If)
		{
			Debug.Log("----------------IF start---------------");
			List<Word> condition = getLine(ref str);
			resolveFunctions(condition);
			Word result = resolveParenthesis(condition);
			fetch(result.S);
			//if condition[0] == true, do IF
			int start = branchIfFalse();
			readBlock(ref str, true);
			branchEnd(start, conv.Code.Count - start);
			clearOperations();
			Debug.Log("--------------ENDIF---------------------");
		}
	}
	private static void setResponses(ref string str)
	{
		List<Response> responses = getResponses(ref str);
		if (responses == null || responses.Count == 0)
			throw new ConverterException(string.Format("Failed to get responses at line {0}", currentLine), currentNode);
		int arrayStart = 0;
		string opvar = "";
		for (int i = 0; i < responses.Count; i++)
		{
			int index = sb.AddString(replaceString(responses[i].Text));
			push_int(index);
			opvar = createOperationVariable();
			if (i == 0)
				arrayStart = operationVars[opvar];
			store(opvar);
		}
		push_int(0);	//after last response, needs to be 0
		opvar = createOperationVariable();
		store(opvar);
		push_int(arrayStart);
		push_int(0);    //number of add. arguments
		op(OperationType.CALLI);
		conv.Code.Add(0);   //babl_menu
		pop();				//NEEDS TESTING
		pop();
		op(OperationType.PUSH_REG);
		store("response");
	}
	private static void setResponsesIf(ref string str)
	{
		List<Response> responses = getResponses(ref str);
		if (responses == null || responses.Count == 0)
			throw new ConverterException(string.Format("Failed to get responses at line {0}", currentLine), currentNode);
		int stringArray = 0;
		int flagArray = 0;
		int stringOffset = 0;
		string opvar = "";
		for (int i = 0; i < responses.Count; i++)
		{
			int index = sb.AddString(replaceString(responses[i].Text));
			push_int(index);
			opvar = createOperationVariable();
			if (i == 0)
			{
				stringArray = operationVars[opvar];
				stringOffset = index;
			}
			store(opvar);
		}
		push_int(0);    //after last response, needs to be 0
		opvar = createOperationVariable();
		store(opvar);
		for (int i = 0; i < responses.Count; i++)
		{
			if (!string.IsNullOrEmpty(responses[i].Variable))
			{
				fetch(responses[i].Variable);
				push_int(0);
				op(OperationType.TSTGT);
			}
			else
				push_int(1);
			opvar = createOperationVariable();
			if (i == 0)
				flagArray = operationVars[opvar];
			store(opvar);
		}
		push_int(0);    //after last response, needs to be 0
		opvar = createOperationVariable();
		store(opvar);
		push_int(flagArray);
		push_int(stringArray);
		push_int(0);
		op(OperationType.CALLI);
		conv.Code.Add(1);       //babl_fmenu
		pop();
		pop();
		pop();
		op(OperationType.PUSH_REG);
		push_int(stringOffset);
		op(OperationType.OPSUB);
		push_int(1);
		op(OperationType.OPADD);
		store("response");
	}
	private static List<Response> getResponses(ref string str)
	{
		Word start = getNext(ref str);
		if (start.S != "(")
			throw new ConverterException(string.Format("Invalid response '{0}' at line {1}", start.S, currentLine), currentNode);

		List<Response> responses = new List<Response>();
		while(str.Length > 0)
		{
			Response response = getResponse(ref str);
			Debug.LogFormat("Got response : {0}", response);
			responses.Add(response);
			clearSeparators(ref str);
			Word sepOrEnd = getNext(ref str);
			
			if (sepOrEnd.S == ",")
				continue;
			else if (sepOrEnd.S == ")")
				break;
			else throw new ConverterException(string.Format("Invalid character in response declaration {0} in line {1}", sepOrEnd.S, currentLine), currentNode);
		}
		return responses;
	}
	private static Response getResponse(ref string str)
	{
		string text = "";
		string variable = "";
		clearSeparators(ref str);
		Word stringStart = getNext(ref str);
		if (stringStart.S != "\"")
			throw new ConverterException(string.Format("Invalid response, should start with '\"', line {0}", currentLine), currentNode);
		text = getString(ref str);
		clearSeparators(ref str);
		Word next = getNext(ref str, WordType.Word, true);
		Debug.LogFormat("next in responses : {0}", next);
		if (next == null)
			return new Response(text);
		else if (next.S == "if")
		{
			clearSeparators(ref str);
			Word var = getNext(ref str);
			if (var.T != WordType.Word)
				throw new ConverterException(string.Format("Invalid variable name {0} in response, in line {1}", var.S, currentLine), currentNode);
			if (!isVariable(var.S))
				throw new ConverterException(string.Format("Unknown variable name {0} in response, in line {1}", var.S, currentLine), currentNode);
			variable = var.S;
			clearSeparators(ref str);
			return new Response(text, variable);
		}
		else
			throw new ConverterException(string.Format("Invalid character in response {0}, should end with ']' or add condition with 'if' and add a variable name after, line {1}", next.S, currentLine), currentNode);
	}
	private static void clearSeparators(ref string str)
	{
		while (str.Length > 0)
		{
			Word next = getNext(ref str, WordType.Separator);
			if (next != null)
				continue;
			else
				break;
		}
	}
	
	private static string getString(ref string str)
	{
		//Pamietaj o cudzyslowiu (\")
		string val = "";
		while(str.Length > 0)
		{
			Word next = getNext(ref str);
			if (next.S == "\"")
				return val;
			val += next.S;
		}
		throw new ConverterException(string.Format("Failed to get string {0} at line {1}", val, currentLine), currentNode);
	}
	private static List<Word> getLine(ref string str)
	{
		List<Word> assignment = new List<Word>();
		while (str.Length > 0)
		{
			Word next = getNext(ref str);
			//Debug.LogFormat("get line next : {0}", next);
			if(next.S == "\"")
				assignment.Add(new Word(getString(ref str), WordType.Word));
			else if (next.T == WordType.Operator || next.T == WordType.Number || next.T == WordType.Punctuation)
				assignment.Add(next);
			else if (next.T == WordType.Word)
			{
				if (isArray(next.S))
					assignment.Add(new Word(getArrayIndex(next.S, ref str), WordType.Word));
				else if (isVariable(next.S) || isFunction(next.S))
					assignment.Add(next);
				else
					throw new ConverterException("Unrecognized variable '" + next.S + "' at line " + currentLine, currentNode);
			}
			else if (next.T == WordType.Control)
				break;
			else if (next.T == WordType.Surrogate)
			{
				throw new ConverterException(string.Format("Invalid character '{0}' at line {1}", next, currentLine), currentNode);
			}
			else if (next.T == WordType.Null)
			{
				Debug.LogErrorFormat("Invalid word at {0}", next.S);
				break;
			}

		}
		return assignment;
	}
	private static Word doFunction(string word, List<Word> arguments = null)
	{
		Debug.LogFormat("doFunction {0}", word);
		if(arguments != null)
		{
			string args = "";
			foreach (var arg in arguments)
			{
				args += arg + " ";
			}
			Debug.LogFormat("arguments : {0}", args);
		}
		switch (word.ToLower())
		{
			case "jump":				jump(arguments);			return null;
			case "say":					say(arguments);				return null;
			case "end":					end();						return null;
			case "getattitude":			return getAttitude();
			case "gettalked":			return getTalked();
			case "description":			description(arguments);		return null;
			case "ask":					return ask();
			case "compare":				return compare(arguments);
			case "random":				return random(arguments);
			case "contains":			return contains(arguments);
			case "getquest":			return getQuest(arguments);
			case "setquest":			setQuest(arguments);		return null;
			case "getsex":				return sex(arguments);
			case "getinventoryslots":	return show_inv(arguments);
			case "givemany":			return give_to_npc(arguments);
			case "arraycontains":		return array_contains(arguments);
			case "finditeminbarter":	return find_barter(arguments);
			case "valueof":				return valueOf(arguments);
			case "countitem":			return count_inv(arguments);
			case "finditem":			return find_inv(arguments);

			case "takeitembypos":		return take_id_from_npc(arguments);
			case "takeitembyid":		return take_from_npc(arguments);

			case "getnpcitempos":		return take_from_npc_inv(arguments);
			case "giveitem":			return give_ptr_npc(arguments);
			case "getitemdirection":	return getItemDirection(arguments);
			case "getitemowner":		return getItemOwner(arguments);
			case "getitemflags":		return getItemFlags(arguments);
			case "getitemspecial":		return getItemSpecial(arguments);
			case "getitemquality":		return check_inv_quality(arguments);
			case "setitemdirection":	setItemDirection(arguments);return null;
			case "setitemowner":		setItemOwner(arguments);	return null;
			case "setitemflags":		setItemFlags(arguments);	return null;
			case "setitemspecial":		setItemSpecial(arguments);	return null;
			case "setitemquality":		return set_inv_quality(arguments);
			case "createitem":			return do_inv_create(arguments);
			case "getskill":			return getSkill(arguments);
			case "addskill":			return addSkill(arguments);
			case "gettrapvariable":		return getTrap(arguments);
			case "settrapvariable":		setTrap(arguments);				return null;
			case "setraceattitude":		set_race_attitude(arguments);	return null;
			case "setnpcattitude":		set_other_attitude(arguments);	return null;
			case "setattitude":			set_attitude(arguments);		return null;
			case "removenpc":			remove_npc();					return null;
			case "placeitem":			place_object(arguments);		return null;
			case "gethomex":			return getHomeX();
			case "gethomey":			return getHomeY();
			case "handledoor":			return gronk_door(arguments);
			case "removeitem":			return do_inv_delete(arguments);
			case "identifyitem":		identify_item(arguments);		return null;
			case "barter":				barter();						return null;
			case "getproperty":			return get_property(arguments);
			case "setproperty":			set_property(arguments);		return null;


			case "getitemarg3":			return getArg3(arguments);
			case "getitemarg2":			return getArg2(arguments);
			case "setitemarg3":			setItemArg3(arguments);		return null;
			case "setitemarg2":			setItemArg2(arguments);		return null;
			//case "getinventoryid":		
			//case "searchinventory":
			default:
				throw new ConverterException(string.Format("Unrecognized function name {0} in line {1}", word, currentLine), currentNode);
		}
	}
	/// <summary>
	/// Used in assignment
	/// </summary>
	private static string getArrayIndex(string var, List<Word> assignment)
	{
		Debug.LogFormat("get array index, var : {0}, assignment : {1}", var, debugWords(assignment));
		if (assignment[0].S != "[")
			throw new ConverterException(string.Format("Invalid array operation {0} at line {1}", assignment[0], currentLine), currentNode);
		else if (assignment[2].S !="]")
			throw new ConverterException(string.Format("Invalid array operation {0} at line {1}", assignment[0], currentLine), currentNode);
		int index = 0;
		string varName = "";
		if (assignment[1].T == WordType.Number)
		{
			bool success = int.TryParse(assignment[1].S, out index);
			if (!success)
				throw new ConverterException(string.Format("Invalid array index {0} at line {1}", assignment[0], currentLine), currentNode);
			varName = getArrayVar(var, index);
			if (!isVariable(varName))
				Debug.LogErrorFormat("Failed to get array variable from name {0} and index {1}", var, index);
		}
		else if (assignment[1].T == WordType.Word)
		{
			fetch(assignment[1].S);
			push_int(getArrayAdress(var));
			op(OperationType.OPADD);	//Pushing exact array adress on stack
		}
		else
			throw new ConverterException(string.Format("Invalid array index {0} at line {1}", assignment[0], currentLine), currentNode);

		assignment.RemoveAt(0);
		assignment.RemoveAt(0);
		assignment.RemoveAt(0);
		return varName;
	}
	/// <summary>
	/// Used in getLine
	/// </summary>
	private static string getArrayIndex(string var, ref string str)
	{
		clearSeparators(ref str);
		if (str.Length > 0 && str[0] != '[')
			return var;
		else if (str.Length == 0)
			return var;
		Word next = getNext(ref str);
		int index = 0;
		string varName = "";
		if(next.S != "[")
			throw new ConverterException(string.Format("Invalid array operation {0} at line {1}", next.S, currentLine), currentNode);
		clearSeparators(ref str);
		next = getNext(ref str);
		if (next.T == WordType.Number)
		{
			bool succ = int.TryParse(next.S, out index);
			if (!succ)
				throw new ConverterException(string.Format("Invalid array index {0} at line {1}", next.S, currentLine), currentNode);
			varName = getArrayVar(var, index);
			if (!isVariable(varName))
				Debug.LogErrorFormat("Failed to get array variable from name {0} and index {1}", var, index);
		}
		else if(next.T == WordType.Word)
		{
			fetch(next.S);
			push_int(getArrayAdress(var));
			op(OperationType.OPADD);
			op(OperationType.FETCHM);
			varName = createOperationVariable();
			store(varName);
		}
		else
			throw new ConverterException(string.Format("Invalid array index {0} at line {1}", next.S, currentLine), currentNode);

		clearSeparators(ref str);
		next = getNext(ref str);
		if (next.S != "]")
			throw new ConverterException(string.Format("Invalid array operation {0} at line {1}", next.S, currentLine), currentNode);
		clearSeparators(ref str);
		return varName;
	}
	private static Word doAssignment(string var, List<Word> assignment)
	{
		Debug.LogFormat("assigment : {0}", debugWords(assignment, true));
		if (assignment.Count <= 1)	//If did not assign a number (eg. local myLoc)
		{
			push_int(0);
			store(var);
			return new Word("0", WordType.Number);
		}
		bool array = isArray(var);
		if (array)
			var = getArrayIndex(var, assignment);
		if (assignment[0].S != "=")
			throw new ConverterException("Invalid operator at line " + currentLine, currentNode);
		assignment.RemoveAt(0);
		Debug.LogFormat("assignment at start : {0}", debugWords(assignment));
		resolveFunctions(assignment);
		Word result = resolveParenthesis(assignment);
		if (result == null)
			throw new ConverterException(string.Format("Invalid assignment for variable {0} at line {1}", var, currentLine), currentNode);
		if (result.T == WordType.Word)
			fetch(result.S);
		else if (result.T == WordType.Number)
			push_int(int.Parse(result.S));
		if (array)
		{
			if (string.IsNullOrEmpty(var))
				op(OperationType.STO);
			else
				store(var);
		}
		else
			store(var);
		Debug.LogFormat("Assign {0} to var {1}", assignment[0], var);
		Debug.Log("Clearing operation variables");
		clearOperations();
		return result;
	}
	private static void resolveFunctions(List<Word> assignment)
	{
		foreach (var fun in functions)
		{
			if(fun.ReturnType == "int")
			{
				int index = assignment.FindLastIndex((x) => x.S.ToLower() == fun.Name.ToLower());
				while(index != -1)
				{
					if (assignment.Count <= index)
						throw new ConverterException(string.Format("Function {0} at line {1} is not a variable, requires '()' to call.", assignment[index].S, currentLine), currentNode);
					if (assignment[index + 1].S != "(")
						throw new ConverterException(string.Format("Function {0} at line {1} is not a variable, requires '()' to call.", assignment[index].S, currentLine), currentNode);
					int cur = index + 1;
					List<Word> args = new List<Word>();
					int parts = fun.Arguments.Count + fun.Arguments.Count - 1;
					for (int i = 0; i < parts; i++)
					{
						cur = index + 2 + i;
						if (assignment.Count <= cur)
							throw new ConverterException(string.Format("Invalid arguments for function {0} in line {1}", fun.Name, currentLine), currentNode);
						if((i % 2) == 0)	
						{
							Word arg = assignment[cur];
							if (arg.T != WordType.Number && arg.T != WordType.Word)
								throw new ConverterException(string.Format("Invalid argument type for function {0} at line {1}", fun.Name, currentLine), currentNode);
							args.Add(arg);
						}
						else
						{
							Word coma = assignment[cur];
							if (coma.S != ",")
								throw new ConverterException(string.Format("Invalid function argument for function {0} at line {1}", fun.Name, currentLine), currentNode);
						}
					}
					cur++;

					if(assignment.Count <= cur)
						throw new ConverterException(string.Format("Function {0} at line {1} is not a variable, requires '()' to call.", assignment[index].S, currentLine), currentNode);
					if (assignment[cur].S != ")")
						throw new ConverterException(string.Format("Function {0} at line {1} is not a variable, requires '()' to call.", assignment[index].S, currentLine), currentNode);
					Debug.LogFormat("Function arguments : {0}", debugWords(args));
					Word ret = doFunction(fun.Name, args);
					for (int i = index; i < cur; i++)
					{
						assignment.RemoveAt(index);
					}
					assignment[index] = ret;
					Debug.LogFormat("Assignment after removing function : {0}", debugWords(assignment));
					index = assignment.FindLastIndex((x) => x.S == fun.Name);
				}
			}
			else
			{
				int index = assignment.FindLastIndex((x) => x.S == fun.Name);
				if (index != -1)
					throw new ConverterException(string.Format("Invalid function call {0} at line {1}", fun.Name, currentLine), currentNode);
			}
		}
	}
	private static Word resolveParenthesis(List<Word> assignment)
	{
		int first = assignment.FindLastIndex((x) => x.S == "(");
		Debug.LogFormat("Found par at {0}", first);
		Word result = null;
		while(first != -1)
		{

			int second = -1;
			for (int i = first + 1; i < assignment.Count; i++)
			{
				if(assignment[i].S == ")")
				{
					second = i;
					break;
				}
			}
			Debug.LogFormat("Found end par at {0}", second);
			if (second != -1)
			{
				List<Word> newAssignment = new List<Word>();
				for (int i = first + 1; i < second; i++)
				{
					newAssignment.Add(assignment[i]);
				}
				Debug.LogFormat("new calculation : {0}", debugWords(newAssignment));
				result = calculate(newAssignment);
				//action(newAssignment);
				Debug.LogFormat("calculated : {0}, count {1}", newAssignment[0], newAssignment.Count);
				assignment[first] = newAssignment[0];
				for (int i = first + 1; i < second + 1; i++)
				{
					assignment.RemoveAt(first + 1);
				}
				Debug.LogFormat("after resolving parenthesis : {0}", debugWords(newAssignment));
			}
			else
				throw new ConverterException(string.Format("Invalid operation '{0}' at {1}", assignment[first], currentLine), currentNode);
			first = assignment.FindLastIndex((x) => x.S == "(");
		}
		//THIS NEEDS TESTING!!!
		if(result == null)
			result = calculate(assignment);
		Debug.LogFormat("ending resolve par., result : {0}", result);
		return result;
	}
	private static Word calculate(List<Word> assignment)
	{
		Word result = null;
		while (true)
		{
			int index = assignment.FindIndex((x) => x.S == "!");
			if (index != -1)
				result = negation(assignment, index);
			else
				break;
		}
		while (true)
		{
			int index = assignment.FindIndex((x) => x.S == "*" || x.S == "/" || x.S == "%" || x.S == "&");
			if (index != -1)
				result = prepareOperation(assignment, index);
			else
				break;
		}
		while (true)
		{
			int index = assignment.FindIndex((x) => x.S == "+" || x.S == "-" || x.S == "|");
			if (index != -1)
				result = prepareOperation(assignment, index);
			else
				break;
		}
		while(true)
		{
			int index = assignment.FindIndex((x) => x.S == "==" || x.S == "!=" || x.S == ">" || x.S == "<" || x.S == ">=" || x.S == "<=");
			if (index != -1)
				result = prepareOperation(assignment, index);
			else
				break;
		}
		if (result == null)
			result = assignment[0];
		Debug.LogFormat("ending calculate, result : {0}", result);
		return result;
	}
	private static Word prepareOperation(List<Word> assignment, int index)
	{
		if (index + 1 >= assignment.Count)
			throw new ConverterException("Invalid operation at line " + currentLine, currentNode);
		Word first = assignment[index - 1];
		Word second = assignment[index + 1];
		if ((first.T != WordType.Number && first.T != WordType.Word) || (second.T != WordType.Number && second.T != WordType.Word))
			throw new ConverterException("Invalid operation '"+ first + assignment[index].S + second +"'at line " + currentLine, currentNode);
		Word result = doOperation(first, assignment[index].S, second);
		assignment[index - 1] = result;
		assignment.RemoveAt(index);
		assignment.RemoveAt(index);
		Debug.LogFormat("ending prepare operation, result : {0}", result);
		return result;
	}
	private static Word negation(List<Word> assignment, int index)
	{
		if (index + 1 >= assignment.Count)
			throw new ConverterException("Invalid operation at line " + currentLine, currentNode);
		Word negated = assignment[index + 1];
		if(negated.T != WordType.Number && negated.T != WordType.Word)
			throw new ConverterException("Invalid operation '" + assignment[index].S + negated.S + "'at line " + currentLine, currentNode);
		int valA = 0;
		if(negated.T  == WordType.Word)
		{
			if (isVariable(negated.S))
				fetch(negated.S);
			else
				throw new ConverterException(string.Format("Unknown variable {0} at line {1}", negated.S, currentLine), currentNode);
		}
		else if(negated.T == WordType.Number)
		{
			bool success = int.TryParse(negated.S, out valA);
			if(!success)
				throw new ConverterException(string.Format("Invalid variable name {0} at line {1}", negated.S, currentLine), currentNode);
			push_int(valA);
		}
		
		op(OperationType.OPNEG);
		string newop = createOperationVariable();
		store(newop);
		Word result = new Word(newop, WordType.Word);
		Debug.LogFormat("Ending negation, result : {0}", newop);
		assignment[index] = result;
		assignment.RemoveAt(index + 1);
		return result;
	}
	private static Word doOperation(Word first, string op, Word second)
	{
		int valA = 0;
		int valB = 0;
		if(first.T == WordType.Word)
		{
			Debug.LogFormat("Retrieving value of variable / function {0}", first.S);
			//Convert to number or leave as string
			if (isVariable(first.S))
				fetch(first.S);
			else if (isArray(first.S))
				throw new ConverterException(string.Format("Invalid array operation {0} at line {1}", first.S, currentLine), currentNode);
			else
				throw new ConverterException(string.Format("Unknown variable {0} at line {1}", first.S, currentLine), currentNode);
		}
		else if(first.T == WordType.Number)
		{
			bool success = int.TryParse(first.S, out valA);
			if (!success)
				throw new ConverterException(string.Format("Invalid variable name {0} at line {1}", first.S, currentLine), currentNode);
			push_int(valA);
		}
		if(second.T == WordType.Word)
		{
			Debug.LogFormat("Retrieving value of variable / function {0}", second.S);
			//j.w.
			if(isVariable(second.S))
				fetch(second.S);
			else if(isArray(second.S))
				throw new ConverterException(string.Format("Invalid array operation {0} at line {1}", second.S, currentLine), currentNode);
			else
				throw new ConverterException(string.Format("Unknown variable {0} at line {1}", second.S, currentLine), currentNode);
		}
		else if (second.T == WordType.Number)
		{
			bool success = int.TryParse(second.S, out valB);
			if (!success)
				throw new ConverterException(string.Format("Invalid variable name {0} at line {1}", second.S, currentLine), currentNode);
			push_int(valB);
		}

		//if (first.T != second.T)
		//	throw new ConverterException(string.Format("Not matching variable types {0} and {1} in line {2}", first, second, currentLine));
		string newop = "";
		if (op == "+")
		{
			Debug.LogFormat("Operation : Addition");
			add();
		}
		else if (op == "-")
		{
			Debug.LogFormat("Operation : Substraction");
			sub();
		}
		else if (op == "*")
		{
			Debug.LogFormat("Operation : Multiplication");
			mul();
		}
		else if (op == "/")
		{
			Debug.LogFormat("Operation : Division");
			div();
		}
		else if (op == "%")
		{
			Debug.LogFormat("Operation : Modulo");
			mod();
		}
		else if(op == "==")
		{
			Debug.LogFormat("Operation : Equation");
			equal();
		}
		else if(op == "!=")
		{
			Debug.LogFormat("Operation : Not-equality");
			nonEqual();
		}
		else if(op == ">")
		{
			Debug.LogFormat("Operation : greater than");
			greater();
		}
		else if(op == "<")
		{
			Debug.LogFormat("Operation : less than");
			less();
		}
		else if(op == ">=")
		{
			Debug.LogFormat("Operation : greater or equal than");
			greaterOrEqual();
		}
		else if(op == "<=")
		{
			Debug.LogFormat("Operation : less or equal than");
			less();
		}
		else if(op == "|")
		{
			Debug.LogFormat("Operation : or");
			or();
		}
		else if(op == "&")
		{
			Debug.LogFormat("Operation : and");
			and();
		}
		else
			throw new ConverterException("Invalid operation '" + op + "' at line " + currentLine, currentNode);

		newop = createOperationVariable();
		store(newop);
		Debug.LogFormat("Ending operation, result : {0}", newop);
		return new Word(newop, WordType.Word);
	}


	private static WordType getWordType(char c)
	{
		if (char.IsDigit(c))
			return WordType.Number;
		else if (char.IsLetter(c))
			return WordType.Word;
		else if (char.IsSymbol(c))
			return WordType.Operator;
		else if (char.IsControl(c))
			return WordType.Control;
		else if (char.IsSeparator(c))
			return WordType.Separator;
		else if (char.IsPunctuation(c))
			return WordType.Punctuation;
		else if (char.IsSurrogate(c))
			return WordType.Surrogate;
		
		return WordType.Null;
	}

	#region Functions
	#region Helpers
	private static void test_args(List<Word> args, int count, string funcName, string argsNames)
	{
		if (args == null || args.Count != count)
			throw new ConverterException(string.Format("Invalid arguments for function '{0}', expected {1}", funcName, argsNames), currentNode);
	}
	private static Word return_variable(int i)
	{
		fetch(i);
		string opvar = createOperationVariable();
		store(opvar);
		return new Word(opvar, WordType.Word);
	}
	private static void push_int(Word argument, int arg, string funcName)
	{
		if (isVariable(argument.S))
			fetch(argument.S);
		else if (argument.T == WordType.Number)
			push_int(int.Parse(argument.S));
		else
			throw new ConverterException(string.Format("Invalid argument {0} for function '{1}', expected int", arg, funcName), currentNode);
	}
	private static void push_arr(Word array, int arg, string funcName, int size = 0)
	{
		if (array.T != WordType.Word)
			throw new ConverterException(string.Format("Invalid argument no {0} for function '{1}', expected array", arg, funcName), currentNode);
		if (!isArray(array.S))
			throw new ConverterException(string.Format("Invalid argument no {0} for function '{1}', expected array", arg, funcName), currentNode);
		if (size > 0 && !checkArraySize(array.S, size))
			throw new ConverterException(string.Format("Invalid array '{0}' size for function '{1}', must be {2} size", array.S, funcName, size), currentNode);
		int arr = getArrayAdress(array.S);
		push_int(arr);
	}
	private static Word return_register()
	{
		op(OperationType.PUSH_REG);
		string opvar = createOperationVariable();
		store(opvar);
		return new Word(opvar, WordType.Word);
	}
	private static void push_adr(Word argument, int arg, string funcName)
	{
		if (isVariable(argument.S))
		{
			fetch(argument.S);
			string opvar = createOperationVariable();
			store(opvar);
			push_int(operationVars[opvar]);
		}
		else if (argument.T == WordType.Number)
			push_adr(int.Parse(argument.S));
		else
			throw new ConverterException(string.Format("Invalid argument {0} for function '{1}', expected int", arg, funcName), currentNode);
	}
	private static void push_adr(int i)
	{
		if(i < 0)
		{
			i = Mathf.Abs(i);
			push_int(i);
			op(OperationType.OPNEG);
		}
		else
			push_int(i);
		string opvar = createOperationVariable();
		store(opvar);
		push_int(operationVars[opvar]);
	}
	#endregion
	private static void jump(List<Word> args)
	{
		test_args(args, 1, "Jump", "string");
		ConversationNode toJumpTo = getNode(args[0].S);
		if (!toJumpTo)
			throw new ConverterException(string.Format("Unknown node to jump to {0}, in line {1}", args[0].S, currentLine), currentNode);

		op(OperationType.JMP);
		int adress = conv.Code.Count;
		conv.Code.Add(-1);
		if(jumps.ContainsKey(toJumpTo))
		{
			jumps[toJumpTo].Add(adress);
		}
		else
		{
			List<int> ints = new List<int>();
			ints.Add(adress);
			jumps[toJumpTo] = ints;
		}
	}
	private static void say(List<Word> args)
	{
		test_args(args, 1, "Say", "string");
		int index = 0;
		if (args[0].T == WordType.Word)
		{
			index = sb.AddString(replaceString(args[0].S));
			say_op(index);
		}
		else if (args[0].T == WordType.Number)
		{
			index = getVariableAdress(args[0].S);
			fetch(index);
			op(OperationType.SAY_OP);
		}
	}
	private static void description(List<Word> args)
	{
		test_args(args, 1, "Description", "string");
		int index = sb.AddString(replaceString(args[0].S));
		push_adr(index);
		push_int(1);
		calli(2);
		pop();
		pop();
	}
	private static Word ask()
	{
		calli(3);
		return return_register();
	}
	private static Word compare(List<Word> args)
	{
		test_args(args, 2, "Compare", "string and int");
		int index = sb.AddString(args[0].S);
		push_adr(index);
		push_int(getVariableAdress(args[1].S));
		push_int(2);
		calli(4);
		pop();
		pop();
		pop();
		return return_register();
	}

	private static Word random(List<Word> args)
	{
		test_args(args, 1, "Random", "int");
		push_int(args[0], 1, "Random");
		push_int(1);
		calli(5);
		pop();
		pop();
		return return_register();
	}
	private static Word contains(List<Word>args)
	{
		test_args(args, 2, "Contains", "string and int");
		int index = sb.AddString(args[0].S);
		push_adr(index);
		push_int(getVariableAdress(args[1].S));
		push_int(2);
		calli(7);
		pop();
		pop();
		pop();
		return return_register();
	}
	private static Word getQuest(List<Word> args)
	{
		test_args(args, 1, "GetQuest", "int");
		push_int(args[0], 1, "GetQuest");
		push_int(1);
		calli(15);
		pop();
		pop();
		return return_register();
	}
	private static void setQuest(List<Word> args)
	{
		test_args(args, 2, "SetQuest", "int and int");
		push_int(args[0], 1, "SetQuest");
		push_int(args[1], 2, "SetQuest");
		push_int(2);
		calli(16);
		pop();
		pop();
		pop();
	}
	private static Word sex(List<Word> args)
	{
		if (args == null || args.Count < 2 || args[0].T != WordType.Word || args[1].T != WordType.Word)
			throw new ConverterException(string.Format("Invalid arguments for function 'GetSex', expected string and string"), currentNode);
		if (maleHandle == "")
		{
			maleHandle = "male_handle";
			int m = 0;
			while (vars.ContainsKey(maleHandle))
			{
				maleHandle = "male_handle" + m;
				m++;
			}
			vars[maleHandle] = getNextVarAdress();
			int male = sb.AddString(args[0].S);
			push_int(male);
			store(maleHandle);
		}
		if (femaleHandle == "")
		{
			femaleHandle = "female_handle";
			int f = 0;
			while (vars.ContainsKey(femaleHandle))
			{
				femaleHandle = "female_handle" + f;
				f++;
			}
			vars[femaleHandle] = getNextVarAdress();
			int female = sb.AddString(args[1].S);
			push_int(female);
			store(femaleHandle);
		}
		push_int(getVariableAdress(maleHandle));
		push_int(getVariableAdress(femaleHandle));
		push_int(2);
		calli(17);
		pop();
		pop();
		pop();
		return return_register();
	}
	/// <summary>
	/// [0] - array of item ids, [1] - array of item pos
	/// </summary>
	private static Word show_inv(List<Word> args)
	{
		test_args(args, 2, "GetInventorySlots", "array and array");
		push_arr(args[0], 1, "GetInventorySlots", 4);
		push_arr(args[1], 2, "GetInventorySlots", 4);
		push_int(2);
		calli(18);
		pop();
		pop();
		pop();
		return return_register();
	}
	/// <summary>
	/// [0] - array of item pos, [1] - number of items in array
	/// </summary>
	private static Word give_to_npc(List<Word> args)
	{
		test_args(args, 2, "GiveToNPC", "array and int");
		push_adr(args[1], 2, "GiveToNPC");
		push_arr(args[0], 1, "GiveToNPC");
		push_int(2);
		calli(19);
		pop();
		pop();
		return return_register();
	}
	private static Word count_inv(List<Word> args)
	{
		test_args(args, 1, "CountItem", "int");
		push_adr(args[0], 1, "CountItem");
		push_int(1);
		calli(30);
		pop();
		pop();
		return return_register();
	}
	private static Word array_contains(List<Word>args)
	{
		if (args == null || args.Count < 1)
			throw new ConverterException(string.Format("Invalid arguments for function 'ArrayContains', expected array"), currentNode);
		int size = getArraySize(args[0].S);
		if (size == 0)
			throw new ConverterException(string.Format("Invalid array '{0}' at function 'ArrayContains', at line {1}", args[0].S, currentLine), currentNode);
		string result = createOperationVariable();
		push_int(0);
		store(result);
		string arr = args[0].S;
		int[] branches = new int[size];
		for (int i = 0; i < size; i++)
		{
			string varName = getArrayVar(arr, i);
			fetch(varName);
			push_int(args[1], 2, "ArrayContains");
			op(OperationType.TSTEQ);
			branches[i] = branchIfFalse();
			push_int(i + 1);
			store(result);
			branchEnd(branches[i], conv.Code.Count - branches[i]);
		}
		return new Word(result, WordType.Word);
	}
	private static Word find_barter(List<Word> args)
	{
		test_args(args, 1, "FindItemInBarter", "int");
		push_adr(args[0], 1, "FindItemInBarter");
		push_int(1);
		calli(49);
		pop();
		pop();
		return return_register();
	}
	private static Word valueOf(List<Word>args)
	{
		test_args(args, 1, "ValueOf", "int");
		push_adr(args[0], 1, "ValueOf");
		push_int(1);
		calli(12);
		pop();
		pop();
		return return_register();
	}
	/// <summary>
	/// [0] : item ID, [1] : search type (0 : npc, 1 : player)
	/// </summary>
	private static Word find_inv(List<Word>args)
	{
		test_args(args, 2, "FindItem", "int and int");
		push_adr(args[0], 1, "FindItem");
		push_adr(args[1], 2, "FindItem");
		push_int(2);
		calli(48);
		pop();
		pop();
		pop();
		return return_register();
	}
	/// <summary>
	/// [0] : item master list position, returns 1 : success, 2 : player have no space
	/// </summary>
	private static Word take_id_from_npc(List<Word>args)
	{
		test_args(args, 1, "TakeItemByPos", "int");
		push_adr(args[0], 1, "TakeItemByPos");
		push_int(1);
		calli(22);
		pop();
		pop();
		return return_register();
	}
	/// <summary>
	/// [0] : item index in NPC inventory? returns master list pos
	/// </summary>
	private static Word take_from_npc_inv(List<Word>args)
	{
		test_args(args, 1, "GetNPCItemPos", "int");
		push_adr(args[0], 1, "GetNPCItemPos");
		push_int(1);
		calli(40);
		pop();
		pop();
		return return_register();
	}
	/// <summary>
	/// [0] - item position in master list, [1] - item quantity or -1 to ignore
	/// </summary>
	private static Word give_ptr_npc(List<Word>args)
	{
		test_args(args, 2, "GiveNPCQuantity", "int and int");
		push_adr(args[0], 1, "GiveNPCQuantity");
		push_adr(args[1], 2, "GiveNPCQuantity");
		push_int(2);
		calli(20);
		pop();
		pop();
		return return_register();
	}
	/// <summary>
	/// [0] - item ID or if > 1000 all items of category ([0] - 1000) * 16
	/// </summary>
	private static Word take_from_npc(List<Word>args)
	{
		test_args(args, 1, "TakeByItemID", "int");
		push_adr(args[0], 1, "TakeByItemID");
		push_int(1);
		calli(21);
		pop();
		pop();
		return return_register();
	}
	private static void setItemDirection(List<Word> args)
	{
		test_args(args, 2, "SetItemDirection", "int and int");

		push_adr(args[0], 1, "SetItemDirection");
		push_adr(1);
		push_adr(args[1], 2, "SetItemDirection");
		push_adr(-1);
		push_adr(-1);		
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
	}
	private static void identify_item(List<Word> args)
	{
		test_args(args, 1, "IdentifyItem", "int");

		push_adr(args[0], 1, "SetItemDirection");
		push_adr(1);
		push_adr(7);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
	}
	private static void setItemOwner(List<Word> args)
	{
		test_args(args, 2, "SetItemOwner", "int and int");

		push_adr(args[0], 1, "SetItemOwner");
		push_adr(1);
		push_adr(-1);
		push_adr(args[1], 2, "SetItemOwner");
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
	}
	private static void setItemFlags(List<Word> args)
	{
		test_args(args, 2, "SetItemFlags", "int and int");

		push_adr(args[0], 1, "SetItemFlags");
		push_adr(1);
		push_adr(-1);
		push_adr(-1);
		push_adr(args[1], 2, "SetItemFlags");
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
	}
	private static void setItemSpecial(List<Word> args)
	{
		test_args(args, 2, "SetItemSpecial", "int and int");

		push_adr(args[0], 1, "SetItemSpecial");
		push_adr(1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(args[1], 2, "SetItemSpecial");
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
	}
	private static void setItemArg3(List<Word> args)
	{
		test_args(args, 2, "Unknown", "int and int");

		push_adr(args[0], 1, "Unknown");
		push_adr(1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(args[1], 2, "Unknown");
		push_adr(-1);
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
	}
	private static void setItemArg2(List<Word> args)
	{
		test_args(args, 2, "Unknown", "int and int");

		push_adr(args[0], 1, "Unknown");
		push_adr(1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(args[1], 2, "Unknown");
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
	}
	private static void setItemQuality2(List<Word> args)
	{
		test_args(args, 2, "SetItemSpecial", "int and int");

		push_adr(args[0], 1, "SetItemSpecial");
		push_adr(1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(args[1], 2, "SetItemSpecial");
		push_int(9);
		calli(47);
		pop(10);
	}
	private static Word getItemDirection(List<Word> args)
	{
		test_args(args, 1, "GetItemDirection", "int");

		string qual = createOperationVariable();
		push_int(0);
		store(qual);

		push_adr(args[0], 1, "GetItemDirection");
		push_adr(0);
		push_int(getVariableAdress(qual));
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
		return new Word(qual, WordType.Word);
	}
	private static Word getItemOwner(List<Word> args)
	{
		test_args(args, 1, "GetItemOwner", "int");

		string owner = createOperationVariable();
		push_int(0);
		store(owner);

		push_adr(args[0], 1, "GetItemOwner");
		push_adr(0);
		push_adr(-1);
		push_int(getVariableAdress(owner));
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
		return new Word(owner, WordType.Word);
	}
	private static Word getItemFlags(List<Word> args)
	{
		test_args(args, 1, "GetItemFlags", "int");

		string arg5 = createOperationVariable();
		push_int(0);
		store(arg5);

		push_adr(args[0], 1, "GetItemFlags");
		push_adr(0);
		push_adr(-1);
		push_adr(-1);
		push_int(getVariableAdress(arg5));
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
		return new Word(arg5, WordType.Word);
	}
	private static Word getItemSpecial(List<Word> args)
	{
		test_args(args, 1, "GetItemSpecial", "int");
		string special = createOperationVariable();
		push_int(0);
		store(special);

		push_adr(args[0], 1, "GetItemSpecial");
		push_adr(0);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(getVariableAdress(special));
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
		return new Word(special, WordType.Word);
	}
	/// <summary>
	/// Flags part
	/// </summary>
	private static Word getArg3(List<Word> args)
	{
		test_args(args, 1, "Unknown", "int");
		string arg3 = createOperationVariable();
		push_int(0);
		store(arg3);

		push_adr(args[0], 1, "Unknown");
		push_adr(0);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(getVariableAdress(arg3));
		push_adr(-1);
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
		return new Word(arg3, WordType.Word);
	}
	/// <summary>
	/// Flags part
	/// </summary>
	private static Word getArg2(List<Word> args)
	{
		test_args(args, 1, "Unknown", "int");
		string arg2 = createOperationVariable();
		push_int(0);
		store(arg2);

		push_adr(args[0], 1, "Unknown");
		push_adr(0);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(getVariableAdress(arg2));
		push_adr(-1);
		push_int(9);
		calli(47);
		pop(10);
		return new Word(arg2, WordType.Word);
	}
	private static Word getItemQuality2(List<Word> args)
	{
		test_args(args, 1, "GetItemQuality", "int");
		string arg1 = createOperationVariable();
		push_int(0);
		store(arg1);

		push_adr(args[0], 1, "GetItemQuality");
		push_adr(0);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_adr(-1);
		push_int(getVariableAdress(arg1));
		push_int(9);
		calli(47);
		pop(10);
		return new Word(arg1, WordType.Word);
	}
	private static Word do_inv_create(List<Word>args)
	{
		test_args(args, 1, "CreateItem", "int");
		push_adr(args[0], 1, "CreateItem");
		push_int(1);
		calli(26);
		pop();
		pop();
		return return_register();
	}
	private static Word getSkill(List<Word>args)
	{
		test_args(args, 1, "GetSkill", "int");
		push_adr(args[0], 1, "GetSkill");
		push_adr(10001);
		push_int(2);
		calli(44);
		pop();
		pop();
		pop();
		return return_register();
	}
	private static Word addSkill(List<Word>args)
	{
		test_args(args, 1, "AddSkill", "int");
		//FIXME : add check for skill
		push_adr(args[0], 1, "AddSkill");
		push_adr(10000);
		push_int(2);
		calli(44);
		pop();
		pop();
		pop();
		return return_register();
	}
	private static Word getTrap(List<Word>args)
	{
		test_args(args, 1, "GetTrapVariable", "int");
		push_adr(args[0], 1, "GetTrapVariable");
		push_adr(10001);
		push_int(2);
		calli(45);
		pop();
		pop();
		pop();
		return return_register();
	}
	/// <summary>
	/// [0] - trap index, [1] - value to set
	/// </summary>
	private static void setTrap(List<Word>args)
	{
		test_args(args, 2, "SetTrapVariable", "int and int");
		push_adr(args[0], 1, "SetTrapVariable");
		push_adr(args[1], 2, "SetTrapVariable");
		push_int(2);
		calli(45);
		pop();
		pop();
		pop();
	}
	private static void set_race_attitude(List<Word>args)
	{
		test_args(args, 3, "SetRaceAttitude", " 3 x int");
		push_adr(args[0], 1, "SetRaceAttitude");
		push_adr(args[1], 2, "SetRaceAttitude");
		push_adr(args[2], 3, "SetRaceAttitude");
		push_int(3);
		calli(38);
		pop();
		pop();
		pop();
		pop();
	}
	private static void set_other_attitude(List<Word>args)
	{
		test_args(args, 2, "SetNPCAttitude", "int and int");
		push_adr(args[0], 1, "SetNPCAttitude");
		push_adr(args[1], 2, "SetNPCAttitude");
		push_int(2);
		calli(43);
		pop();
		pop();
		pop();
	}
	private static void set_attitude(List<Word>args)
	{
		test_args(args, 1, "SetAttitude", "int");
		push_int(22);
		push_int(args[0], 1, "SetAttitude");
		op(OperationType.STO);
	}
	private static void place_object(List<Word>args)
	{
		test_args(args, 3, "PlaceItem", "3 x int");
		push_adr(args[0], 1, "PlaceItem");
		push_adr(args[1], 2, "PlaceItem");
		push_adr(args[2], 3, "PlaceItem");
		push_int(3);
		calli(39);
		pop();
		pop();
		pop();
		pop();
	}
	private static Word gronk_door(List<Word>args)
	{
		test_args(args, 3, "HandleDoor", "3 x int");
		push_adr(args[0], 1, "HandleDoor");
		push_adr(args[1], 2, "HandleDoor");
		push_adr(args[2], 3, "HandleDoor");
		push_int(3);
		calli(37);
		pop();
		pop();
		pop();
		pop();
		return return_register();
	}
	private static Word set_inv_quality(List<Word>args)
	{
		test_args(args, 2, "SetItemQuality", "int and int");
		push_adr(args[0], 1, "SetItemQuality");
		push_adr(args[1], 2, "SetItemQuality");
		push_int(2);
		calli(29);
		pop();
		pop();
		pop();
		return return_register();
	}
	private static Word check_inv_quality(List<Word>args)
	{
		test_args(args, 1, "GetItemQuality", "int");
		push_adr(args[0], 1, "GetItemQuality");
		push_int(1);
		calli(28);
		pop();
		pop();
		return return_register();
	}
	private static Word do_inv_delete(List<Word>args)
	{
		test_args(args, 1, "RemoveItem", "int");
		push_adr(args[0], 1, "RemoveItem");
		push_int(1);
		calli(27);
		pop();
		pop();
		return return_register();
	}
	private static void barter()
	{
		set_likes_dislikes();
		int index = addBarterStrings();
		calli(31);  //setup_to_barter
		int start = conv.Code.Count;
		barter_main_menu(index);
		
		//Offer
		op(OperationType.PUSH_REG);
		push_int(1);
		op(OperationType.TSTEQ);
		int answ1 = branchIfFalse();
		barter_offer(index);
		int tradeDone = branchIfTrue();
		jump(start);
		branchEnd(answ1, conv.Code.Count - answ1);

		//Demand
		op(OperationType.PUSH_REG);
		push_int(2);
		op(OperationType.TSTEQ);
		int answ2 = branchIfFalse();
		barter_demand_menu(index);
		op(OperationType.PUSH_REG);
		push_int(1);
		op(OperationType.TSTEQ);
		int bra = branchIfTrue();
		//If reconsidered
		jump(start);
		branchEnd(bra, conv.Code.Count - bra);
		//If taking by force
		push_int(22);
		push_int(1);
		op(OperationType.STO);
		barter_demand(index);
		op(OperationType.PUSH_REG);
		push_int(1);
		op(OperationType.TSTEQ);
		int persuaded = branchIfTrue();
		//If did not persuade
		push_int(22);
		push_int(0);
		op(OperationType.STO);
		end();	//End and fight!
		branchEnd(answ2, conv.Code.Count - answ2);
		
		//Appraise
		op(OperationType.PUSH_REG);
		push_int(3);
		op(OperationType.TSTEQ);
		int answ3 = branchIfFalse();
		calli(33);
		jump(start);
		branchEnd(answ3, conv.Code.Count - answ3);

		calli(34);		
		branchEnd(tradeDone, conv.Code.Count - tradeDone);
		branchEnd(persuaded, conv.Code.Count - persuaded);
	}
	private static void set_likes_dislikes()
	{
		if (conv.Likes.Count == 0 && conv.Dislikes.Count == 0)
			return;
		int likesStart = 0;
		for (int i = 0; i < conv.Likes.Count; i++)
		{
			push_int(conv.Likes[i]);
			string likesOpvar = createOperationVariable();
			store(likesOpvar);
			if (i == 0)
				likesStart = operationVars[likesOpvar];
		}

		push_int(1);
		op(OperationType.OPNEG);
		string likesEnd = createOperationVariable();
		store(likesEnd);
		if (conv.Likes.Count == 0)
			likesStart = operationVars[likesEnd];

		int dislikesStart = 0;
		for (int i = 0; i < conv.Dislikes.Count; i++)
		{
			push_int(conv.Dislikes[i]);
			string dislikesOpvar = createOperationVariable();
			store(dislikesOpvar);
			if (i == 0)
				dislikesStart = operationVars[dislikesOpvar];
		}
		push_int(1);
		op(OperationType.OPNEG);
		string dislikesEnd = createOperationVariable();
		store(dislikesEnd);
		if (conv.Dislikes.Count == 0)
			dislikesStart = operationVars[dislikesEnd];

		push_int(dislikesStart);
		push_int(likesStart);
		push_int(2);
		calli(36);
		pop();
		pop();
		pop();
	}
	private static void barter_main_menu(int index)
	{
		string resp1 = createOperationVariable();
		string resp2 = createOperationVariable();
		string resp3 = createOperationVariable();
		string resp4 = createOperationVariable();
		int arrayStart = operationVars[resp1];
		push_int(index);
		store(resp1);
		push_int(index + 1);
		store(resp2);
		push_int(index + 2);
		store(resp3);
		push_int(index + 3);
		store(resp4);
		push_int(0);    //after last response, needs to be 0
		string opvar = createOperationVariable();
		store(opvar);
		push_int(arrayStart);
		push_int(0);    //number of add. arguments
		op(OperationType.CALLI);
		conv.Code.Add(0);   //babl_menu
		pop();              //NEEDS TESTING
		pop();
		
	}
	private static void barter_offer(int index)
	{
		push_adr(-1);
		push_adr(-1);
		push_adr(index + 5);
		push_adr(index + 6);
		push_adr(index + 7);
		push_adr(index + 8);
		push_adr(index + 9);
		push_int(7);
		calli(24);
		pop(8);
		op(OperationType.PUSH_REG);
	}
	private static void barter_demand_menu(int index)
	{
		push_int(index + 10);
		op(OperationType.SAY_OP);

		string resp1 = createOperationVariable();
		string resp2 = createOperationVariable();
		int arrayStart = operationVars[resp1];
		push_int(index + 11);
		store(resp1);
		push_int(index + 12);
		store(resp2);

		push_int(0);    //after last response, needs to be 0
		string opvar = createOperationVariable();
		store(opvar);
		push_int(arrayStart);
		push_int(0);    //number of add. arguments
		op(OperationType.CALLI);
		conv.Code.Add(0);   //babl_menu
		pop();              //NEEDS TESTING
		pop();
	}
	private static void barter_demand(int index)
	{
		push_adr(index + 13);
		push_adr(index + 14);
		push_adr(index + 15);
		push_int(3);
		calli(25);
		pop();
		pop();
		pop();
	}
	private static void remove_npc()
	{
		push_int(0);
		calli(42);
		pop();
	}
	private static void end()
	{
		endConversation();
	}
	private static Word get_property(List<Word>args)
	{
		test_args(args, 1, "GetProperty", "string");
		int var = get_var(args[0].S);
		if (var == -1)
			throw new ConverterException(string.Format("Unknown variable {0} at function 'GetProperty' at line {1}", args[0].S, currentLine), currentNode);
		fetch(var);
		string result = createOperationVariable();
		store(result);
		return new Word(result, WordType.Word);
	}
	private static void set_property(List<Word>args)
	{
		test_args(args, 2, "SetProperty", "string and int");
		int var = get_var(args[0].S);
		if (var == -1)
			throw new ConverterException(string.Format("Unknown variable {0} at function 'SetProperty' at line {1}", args[0].S, currentLine), currentNode);
		push_int(var);
		push_int(args[1], 2, "SetProperty");
		op(OperationType.STO);
	}

	private static Word getAttitude() => return_variable(22);
	private static Word getTalked() => return_variable(24);
	private static Word getHomeX() => return_variable(13);
	private static Word getHomeY() => return_variable(14);

	#endregion
	private static int addBarterStrings()
	{
		int first = 0;
		for (int i = 0; i < 16; i++)
		{
			int index = sb.AddString(barterStrings[i]);
			if (first == 0)
				first = index;
		}
		return first;
	}
	private static string replaceString(string str)
	{
		int atIndex = str.IndexOf('$');
		while(atIndex != -1)
		{
			char type = str[atIndex + 1];
			char colon = str[atIndex + 2];
			if (type != 'I' && type != 'S' && type != 'G')
				throw new ConverterException(string.Format("Invalid text substitution type, valid types : I (integer), S (string), G (global variable)"), currentNode);
			if (colon != ':')
				throw new ConverterException(string.Format("Invalid character at text substitution, expected colon (:)"), currentNode);
			string cur = "";
			int cutCount = 3;
			for (int i = atIndex + 3; i < str.Length; i++)
			{
				char c = str[i];
				if (char.IsSeparator(c) || char.IsControl(c) || char.IsPunctuation(c) || char.IsSymbol(c))
					break;
				cur += str[i];
				cutCount++;
			}
			str = str.Remove(atIndex, cutCount);
			Debug.LogFormat("Substitution get : ${0}{1}{2}", type, colon, cur);
			string sub = "";
			if (type == 'G')
				sub = getGlobal(cur);
			else if(type == 'S')
			{

				if (!isVariable(cur))
					throw new ConverterException(string.Format("Invalid variable {0} at text substitution at line {1}", cur, currentLine), currentNode);
				sub += "@SS";
				sub += (getVariableAdress(cur) - conv.MemorySlots);
			}
			else if(type == 'I')
			{
				if(isArray(cur))
				{
					int arrCount = 0;
					string arrIndex = "";
					for (int i = atIndex; i < str.Length; i++)
					{
						Debug.LogFormat("getting array in sub, i : {0}, atindex : {1}, cur : {2}, c : {3}, arrIndex : {4}", i, atIndex, cur, str[i], arrIndex);
						arrCount++;
						if (i == atIndex)
						{
							if (str[i] != '[')
								throw new ConverterException(string.Format("Invalid array {0} at text substitution at lint {1}", cur, currentLine), currentNode);
							else
								continue;
						}
						if (i > atIndex && char.IsNumber(str[i]))
						{
							arrIndex += str[i];
						}
						else if (i > atIndex && str[i] == ']')
							break;
						else
							throw new ConverterException(string.Format("Invalid array {0} at text substitution at lint {1}", cur, currentLine), currentNode);
					}
					int index = 0;
					bool succ = int.TryParse(arrIndex, out index);
					if(!succ)
						throw new ConverterException(string.Format("Invalid array index {0} at text substitution at lint {1}", arrIndex, currentLine), currentNode);
					cur = cur + "_arr_" + index;
					str = str.Remove(atIndex, arrCount);
				}
				if (!isVariable(cur))
					throw new ConverterException(string.Format("Invalid variable {0} at text substitution at line {1}", cur, currentLine), currentNode);
				sub += "@SI";
				sub += (getVariableAdress(cur) - conv.MemorySlots);
			}
			Debug.LogFormat("Substitution set : {0}, cur : {1}", sub, cur);
			str = str.Insert(atIndex, sub);
			Debug.LogFormat("string after sub : {0}", str);
			atIndex = str.IndexOf('$');
		}
		return str;
	}
	private static int get_var(string name)
	{
		switch (name)
		{
			case "game_mins": return 31;
			case "game_days": return 30;
			case "game_time": return 29;
			case "riddlecounter": return 28;
			case "dungeon_level": return 27;
			case "npc_name": return 26;
			case "npc_level": return 25;
			case "npc_talkedto": return 24;
			case "npc_gtarg": return 23;
			case "npc_attitude": return 22;
			case "npc_goal": return 21;
			case "npc_power": return 20;
			case "npc_arms": return 19;
			case "npc_hp": return 18;
			case "npc_health": return 17;
			case "npc_hunger": return 16;
			case "npc_whoami": return 15;
			case "npc_yhome": return 14;
			case "npc_xhome": return 13;
			case "play_sex": return 12;
			case "play_drawn": return 11;
			case "play_poison": return 10;
			case "play_name": return 9;
			case "new_player_exp": return 8;
			case "play_level": return 7;
			case "play_mana": return 6;
			case "play_hp": return 5;
			case "play_power": return 4;
			case "play_arms": return 3;
			case "play_health": return 2;
			case "play_hunger": return 1;
			default:
				return -1;
		}
	}
	private static string getGlobal(string name)
	{
		if (name == "playerhunger")
			return "@GI1";
		else if (name == "playerhealth")
			return "@GI2";
		else if (name == "playerarms")
			return "@GI3";
		else if (name == "playerpower")
			return "@GI4";
		else if (name == "playerhp")
			return "@GI5";
		else if (name == "playermana")
			return "@GI6";
		else if (name == "playerlevel")
			return "@GI7";
		else if (name == "playerexp")
			return "@GI8";
		else if (name == "playername")
			return "@GS9";
		else if (name == "playerpoison")
			return "@GI10";
		else if (name == "playerdrawn")
			return "@GI11";
		else if (name == "playersex")
			return "@GI12";
		else if (name == "npcxhome")
			return "@GI13";
		else if (name == "npcyhome")
			return "@GI14";
		else if (name == "npcid")
			return "@GI15";
		else if (name == "npchunger")
			return "@GI16";
		else if (name == "npchealth")
			return "@GI17";
		else if (name == "npchp")
			return "@GI18";
		else if (name == "npcarms")
			return "@GI19";
		else if (name == "npcpower")
			return "@GI20";
		else if (name == "npcgoal")
			return "@GI21";
		else if (name == "npcattitude")
			return "@GI22";
		else if (name == "npcgtarg")
			return "@GI23";
		else if (name == "npctalked")
			return "@GI24";
		else if (name == "npclevel")
			return "@GI25";
		else if (name == "npcname")
			return "@GS26";
		else if (name == "dungeonlevel")
			return "@GI27";
		else if (name == "riddlecounter")
			return "@GI28";
		else if (name == "gametime")
			return "@GI29";
		else if (name == "gamedays")
			return "@GI30";
		else if (name == "gamemins")
			return "@GI31";

		return "Invalid_global";
	}
	private static string debugWords(List<Word> assignment, bool info = false)
	{
		string log = "{ ";
		foreach (var item in assignment)
		{
			log += item.S + " ";
			if (info)
				log += "(" + item.T + ") ";
		}
		log += " }";
		return log;
	}

}

public enum ConverterAction
{
	Null,
	NextLine,
	Function,
	Variable,
	End,
	Separator,
	Invalid,
	If,
	EndIf,
	Local,
	Global,
	Response
}


public class Word
{
	public string S;
	public WordType T;
	public Word(string s, WordType t)
	{
		S = s;
		T = t;
	}
	public override string ToString()
	{
		return S + " (" + T.ToString() + ")";
	}
}
public class Response
{
	public string Text;
	//public int Target;
	public string Variable;

	public Response(string text)
	{
		Text = text;
		//Target = target;
	}
	public Response(string text, string var) : this(text)
	{
		Variable = var;
	}
	public override string ToString()
	{
		//return "Text : " + Text + ", Target : " + Target + ", Variable : " + Variable;
		return "Text : " + Text + ", Variable : " + Variable;
	}
}

public class ConverterFunction
{
	public string ReturnType;
	public string Name;
	public List<string> Arguments;

	public ConverterFunction(string name, string ret, params string[] args)
	{
		Name = name;
		ReturnType = ret;
		Arguments = new List<string>();
		foreach (var arg in args)
			Arguments.Add(arg);
	}
}

public class ConverterException : Exception
{
	public ConversationNode Node;

	public ConverterException(string message, ConversationNode node) : base(message)
	{
		Node = node;
	}

	public static implicit operator bool(ConverterException obj)
	{
		if (obj == null)
			return false;
		return true;
	}
}
