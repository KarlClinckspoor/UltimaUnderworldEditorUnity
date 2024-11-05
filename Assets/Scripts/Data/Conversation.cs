using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConversationData
{
	public Conversation[] Conversations;
	public Dictionary<int, int> PrivateVariables;
	public ConversationFunction[] DefaultFunctions;

	public ConversationData(Conversation[] convs, Dictionary<int, int> privVars)
	{
		Conversations = convs;
		PrivateVariables = privVars;
	}

	public string DebugMemory()
	{
		string str = "Conversations memory debug\n";
		for (int i = 0; i < Conversations.Length; i++)
		{
			Conversation conv = Conversations[i];
			if(conv)
			{
				str += conv.DebugConversationMemory();
				str += string.Format(", priv. variables : {0}\n", PrivateVariables[i]);
			}
		}
		return str;
	}
}

[Serializable]
public class ConversationStructure
{
	public int Slot;
	public List<ConversationNodeStructure> Nodes;
	public List<ConversationConnectionStructure> Connections;

	public int PrivateGlobalCount;
	//public Dictionary<int, string> PrivateGlobals;
	public List<string> GlobalVariables;
	public List<string> LocalVariables;
	public string NPCName;

	public string[] BarterStrings;
	public List<int> Likes;
	public List<int> Dislikes;

	public override string ToString()
	{
		return "Conversation structure, slot " + Slot + ", node count " + Nodes.Count + ", priv. globs : " + GlobalVariables.Count + ", name : " + NPCName;
	}
}

public enum ConversationState
{
	Uneditable,
	Unconverted,
	Converted,
}

public class Conversation  {

	public int Slot;

	public int Unk00;
	public int Unk02;
	public int CodeSize;
	public int Unk06;
	public int Unk08;
	public int StringBlock;
	public int MemorySlots;              //MemorySlots ( 32 + additional)?
	public int ImportedFunctionsAndVariables;    //Almost always 82

	public int FirstMemorySlot;
	public int ImportedVariables;                
	public ConversationFunction[] Functions;    //List?
	public List<int> Code;


	//public int[] ConvGlobals;
	public int[] SavedVars;
	public int[] UnnamedVars;

	public string[] BarterStrings;
	public List<int> Likes;
	public List<int> Dislikes;

	public ConversationState State { get; private set; }
	//public ConversationStructure Structure;

	public Conversation(ConversationState state)
	{
		State = state;
	}
	public void SetConverted()
	{
		State = ConversationState.Converted;
	}

	//public void SetPrivateGlobals(int count)
	//{
	//	ConvGlobals = new int[count];
	//}
	public void SetSavedVariables(int count)
	{
		SavedVars = new int[count];
	}

	public StringBlock GetStringBlock()
	{
		int blockIndex = MapCreator.StringData.BlockDictionary[StringBlock];
		StringBlock sb = MapCreator.StringData.Blocks[blockIndex];
		return sb;
	}

	public string GetString(int index)
	{
		StringBlock sb = GetStringBlock();
		if(index < 0 || index >= sb.Strings.Count)
		{
			Debug.LogWarningFormat("Trying to get string at invalid index at conv. {0}, string block {1}, index {2}", Slot, sb.BlockNumber, index);
			return "invalid string, index : " + index;
		}
		return sb.Strings[index];
	}

	public static implicit operator bool(Conversation c)
	{
		if (c == null)
			return false;
		return true;
	}

	public string DumpConversation()
	{
		string dump = string.Format("Conversation {0}: \n", Slot);
		dump += string.Format("Unk00 : {0}, Unk02: {1}, CodeSize : {2}. Unk06 : {3}, Unk08 : {4}, StringBlock : {5}, MemorySlots : {6}, ImportedGlobals : {7}\n", Unk00, Unk02, CodeSize, Unk06, Unk08, StringBlock, MemorySlots, ImportedFunctionsAndVariables);
		dump += "Functions : \n";
		for (int j = 0; j < Functions.Length; j++)
		{
			ConversationFunction f = Functions[j];
			dump += string.Format("Length : {0:00},\tName : {1},\tID : {2},\tUnk : {3},\tType : {4},\tReturn : {5}\n", f.NameLength, f.Name, f.Id_Adress, f.Unk04, f.GetFuncType(), f.GetReturnType());
		}
		dump += "Code : \n";
		dump += ConversationOperation.DumpCode(Code);
		return dump;
	}

	public string DebugConversationMemory()
	{
		string dbg = string.Format("Conversation {0}, ", Slot);
		dbg += string.Format("MemorySlots : {0}, ImportedGlobals : {1}, ", MemorySlots, ImportedFunctionsAndVariables);
		for (int i = 0; i < Code.Count; i++)
		{
			if((OperationType)Code[i] == OperationType.PUSHI)
			{
				i++;
				if(Code[i] > 0)
				{
					dbg += string.Format("Memory PUSHI : {0}", Code[i]);
					break;
				}
			}
		}
		return dbg;
	}
}



public class ConversationManager
{
	public Conversation Conv;

	public ConversationStack Stack;
	public int CallLevel;
	public int Current;
	public int Base;
	public int ResultRegister;

	public ConversationWindow ConvWindow;
	public ConversationDebugWindow DebugWindow;


	private bool debugOn;
	private bool waitingResponse;
	private bool finished;
	private bool single;

	public ConversationManager(Conversation conv, GameObject convWindow, GameObject debugWindow, int[] savedVars = null, int[] gameVars = null, bool debugOn = false, bool single = false)
	{
		Conv = conv;
		CallLevel = 1;
		Current = 0;
		Base = 0;
		ResultRegister = 0;
		//int stackOffset = conv.ImportedGlobals + conv.PrivateGlobalCount;
		Stack = new ConversationStack(65535);
		int first = Conv.FirstMemorySlot - 31;
		if (first < 0)
			Debug.LogErrorFormat("First memory slot < 0");
		string gameVarsStr = "";
		for (int i = 0; i < gameVars.Length; i++)
			gameVarsStr += "[" + (i + first) + "]: " + gameVars[i].ToString() + "\n";
		if (gameVars != null)
		{
			Stack.Set(first, gameVars);
			Debug.LogFormat("Setting game vars from {0}, vars : \n{1}", first, gameVarsStr);
		}
		else
		{
			Debug.LogWarning("Did not pass game variables to conversation manager, creating new");
			gameVars = GetImportedGlobals();
			gameVars[first + 14] = Conv.Slot;
		}
		string savedVarsStr = "";

		if (savedVars != null)
		{
			//Stack.Set(conv.ImportedVariables, savedVars);
			Stack.Set(Conv.FirstMemorySlot, savedVars);
			for (int i = 0; i < savedVars.Length; i++)
				savedVarsStr += "[" + (i + first) + "]: " + savedVars[i].ToString() + "\n";
			Debug.LogFormat("Saved vars preset, setting saved vars from {0}, saved vars : \n{1}", Conv.FirstMemorySlot, savedVarsStr);
		}
		else
		{
			Debug.LogFormat("Creating empty saved vars array for conversation");
			savedVars = new int[Conv.MemorySlots - Conv.FirstMemorySlot];
			Conv.SavedVars = savedVars;
		}

		if(Conv.UnnamedVars == null)
			Conv.UnnamedVars = new int[first];
		string unnamedVarsStr = "";
		for (int i = 0; i < Conv.UnnamedVars.Length; i++)
			unnamedVarsStr += "[" + i + "]: " + Conv.UnnamedVars[i].ToString() + "\n";
		Debug.LogFormat("Unnamed vars : \n{0}", unnamedVarsStr);
		Stack.Set(0, Conv.UnnamedVars);

		Debug.LogFormat("Conversation memory layout : Conv.MemorySlots : {0}, Conv.FirstMemorySlot : {1}, Conv.SavedVars.Length : {2}, Conv.ImportedVariables : {3}, first : {4}, Conv.UnnamedVars : {5}", Conv.MemorySlots, Conv.FirstMemorySlot, Conv.SavedVars.Length, Conv.ImportedVariables, first, Conv.UnnamedVars.Length);

		Base = conv.MemorySlots;
		Stack.SetPointer(conv.MemorySlots);

		string stackStr = "";
		for (int i = 0; i < conv.MemorySlots + 10; i++)
			stackStr += Stack[i].ToString() + "\n";
		Debug.LogFormat("Stack at start to conv.MemorySlots + 10 : \n {0}", stackStr);

		ConvWindow = convWindow.GetComponent<ConversationWindow>();
		if(debugWindow)
			DebugWindow = debugWindow.GetComponent<ConversationDebugWindow>();

		this.debugOn = debugOn;
		this.single = single;

		//for (int i = 0; i < 31; i++)
		//{
		//	Debug.LogFormat("Imp glob {0} is {1}", i, Stack[i]);
		//}
	}

	public static int[] GetImportedGlobals()
	{
		int[] impGlobs = new int[32];
		impGlobs[0] = 0;
		impGlobs[1] = 30;
		impGlobs[2] = 0;
		impGlobs[3] = 0;
		impGlobs[4] = 30;
		impGlobs[5] = 30;
		impGlobs[6] = 3;
		impGlobs[7] = 0;
		impGlobs[8] = 0;  
		impGlobs[9] = 0;
		impGlobs[10] = 0;
		impGlobs[11] = 0;
		impGlobs[12] = 32;
		impGlobs[13] = 32;
		//impGlobs[14] = conv.Slot;
		impGlobs[15] = 0;
		impGlobs[16] = 30;
		impGlobs[17] = 30;
		impGlobs[18] = 0;
		impGlobs[19] = 0;
		impGlobs[20] = 8;   //Goal
		impGlobs[21] = 3;   //Attitude
		impGlobs[22] = 0;   //GTarg
		impGlobs[23] = 0;   //Talked to - not used?
		impGlobs[24] = 0;   //Level
		impGlobs[25] = 0;   //NPC name
		impGlobs[26] = 1;   //Dungeon level
		impGlobs[27] = 0;   //Riddle counter
		impGlobs[28] = 1;   //Game time
		impGlobs[29] = 1;   //Game days
		impGlobs[30] = 1;	//Game mins
		return impGlobs;
	}


	private int GetImportedGlobal(int index)
	{
		return Stack[index];
	}
	private int GetPrivateGlobal(int index)
	{
		return Stack[Conv.ImportedVariables + index];
	}

	public void ProcessConversation()
	{
		int safe = 1000;
		//Debug.Log("Proc conv");
		while (!waitingResponse && !finished)
		{
			ProcessCode();
			//Debug.LogFormat("loop, waiting : {0}", waitingResponse);
			safe--;
			if(safe == 0)
			{
				Say("Error in conversation, infinite loop.", Color.red, FontStyle.Bold);
				finished = true;
			}
		}
	}

	public void PauseConversation()
	{
		waitingResponse = true;
		if (debugOn)
		{
			string stackDebug = "Stack : \n";
			stackDebug += Stack.DumpStackFrom(0, Stack.Pointer);
			DebugConversation("", stackDebug, true);
		}
	}

	public void ResumeConversation()
	{
		waitingResponse = false;
		if(!single)
			ProcessConversation();
	}

	private void OffsetCurrent(int offset)
	{
		Current += offset;
		if (Current >= 65536)
			Current -= 65536;
	}

	public void ProcessCode()
	{
		if (!Conv || waitingResponse || finished)
			return;

		OperationType op = (OperationType)Conv.Code[Current];
		string convDebug = "---------------\n";

		switch (op)
		{
			case OperationType.NOP:
				break;

			case OperationType.OPADD:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					Stack.Push(arg1 + arg2);
					convDebug += string.Format("OPADD, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
				}
				break;
			case OperationType.OPMUL:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					Stack.Push(arg1 * arg2);
					convDebug += string.Format("OPMUL, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
				}
				break;
			case OperationType.OPSUB:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					Stack.Push(arg2 - arg1);
					convDebug += string.Format("OPSUB, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
				}
				break;
			case OperationType.OPDIV:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					Stack.Push(arg2 / arg1);
					convDebug += string.Format("OPDIV, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
				}
				break;
			case OperationType.OPMOD:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					Stack.Push(arg2 % arg1);
					convDebug += string.Format("OPMOD, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
				}
				break;
			case OperationType.OPOR:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					Stack.Push(arg2 | arg1);
					convDebug += string.Format("OPOR, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
				}
				break;
			case OperationType.OPAND:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					Stack.Push(arg2 & arg1);
					convDebug += string.Format("OPAND, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
				}
				break;
			case OperationType.OPNOT:
				{
					int arg1 = Stack.Pop();
					convDebug += string.Format("OPNOT, arg1 : {0}\n", arg1);
					if (arg1 == 0)
						Stack.Push(1);
					else
						Stack.Push(0);
					break;

				}
			case OperationType.TSTGT:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					convDebug += string.Format("TSTGT, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
					if (arg2 > arg1)
						Stack.Push(1);
					else
						Stack.Push(0);
				}
				break;
			case OperationType.TSTGE:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					convDebug += string.Format("TSTGE, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
					if (arg2 >= arg1)
						Stack.Push(1);
					else
						Stack.Push(0);
				}
				break;
			case OperationType.TSTLT:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					convDebug += string.Format("TSTLT, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
					if (arg2 < arg1)
						Stack.Push(1);
					else
						Stack.Push(0);
				}
				break;
			case OperationType.TSTLE:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					convDebug += string.Format("TSTLE, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
					if (arg2 <= arg1)
						Stack.Push(1);
					else
						Stack.Push(0);
				}
				break;
			case OperationType.TSTEQ:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					convDebug += string.Format("TSTEQ, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
					if (arg2 == arg1)
						Stack.Push(1);
					else
						Stack.Push(0);
				}
				break;
			case OperationType.TSTNE:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					convDebug += string.Format("TSTNE, arg1 : {0}, arg2 : {1}\n", arg1, arg2);
					if (arg2 != arg1)
						Stack.Push(1);
					else
						Stack.Push(0);
				}
				break;
			case OperationType.JMP:
				convDebug += string.Format("JMP, jumping from Current {0} to {1}\n", Current, Conv.Code[Current + 1] - 1);
				Current = Conv.Code[Current + 1] - 1;				
				break;
			case OperationType.BEQ:
				{
					int arg1 = Stack.Pop();
					convDebug += string.Format("BEQ, branching if arg1 {0} == 0 from Current {1} to {2}, else Current++\n", arg1, Current, (Current + Conv.Code[Current + 1]));
					if (arg1 == 0)
						//Current += Conv.Code[Current + 1];
						OffsetCurrent(Conv.Code[Current + 1]);
					else
						Current++;
				}
				break;
			case OperationType.BNE:
				{
					int arg1 = Stack.Pop();
					convDebug += string.Format("BNE, branching if arg1 {0} != 0 from Current {1} to {2}, else Current++\n", arg1, Current, (Current + Conv.Code[Current + 1]));
					if (arg1 != 0)
						//Current += Conv.Code[Current + 1];
						OffsetCurrent(Conv.Code[Current + 1]);
					else
						Current++;
				}
				break;
			case OperationType.BRA:
				convDebug += string.Format("BRA, branching from Current {0} to {1}\n", Current, (Current + Conv.Code[Current + 1]));
				//Current += Conv.Code[Current + 1];
				OffsetCurrent(Conv.Code[Current + 1]);
				break;

			case OperationType.CALL:
				Stack.Push(Current + 1);    //Save the current function
				convDebug += string.Format("CALL, pushing current + 1 to stack {0}, setting curent to {1}, CallLevel {2}\n", (Current + 1), Conv.Code[Current + 1] - 1, CallLevel + 1);
				Current = Conv.Code[Current + 1] - 1;
				CallLevel++;

				break;
			case OperationType.CALLI:
				{
					int arg1 = Conv.Code[++Current];
					for (int i = 0; i <= Conv.Functions.Length; i++)
					{
						if ((Conv.Functions[i].Id_Adress == arg1) && (Conv.Functions[i].Type == ConversationFunction.Function))
						{
							ConversationFunction convFunc = Conv.Functions[i];
							Debug.LogFormat("Calling function, current : {0}, function : {1}\n", Current, convFunc.Name);
							DoFunction(convFunc);
							//yield return StartCoroutine(run_imported_function(conv[convtodisplay].functions[i]));
							break;
						}
					}
				}
				break;
			case OperationType.RET:
				{
					if (--CallLevel < 0)
						FinishConversation();
					else
					{
						int arg1 = Stack.Pop();
						convDebug += string.Format("RET, finishing subroutine, CallLevel : {0}, arg1 : {1}, Current : {2} (Current = arg1)\n", CallLevel, arg1, Current);
						Current = arg1;
					}
				}
				break;
			case OperationType.PUSHI:
				convDebug += string.Format("PUSHI, pushing : {0}\n", Conv.Code[Current + 1]);
				Stack.Push(Conv.Code[++Current]);
				break;
			case OperationType.PUSHI_EFF:
				//convDebug += string.Format("PUSHI_EFF, arg : {0}, Base : {1}, pushed : {2}\n", Conv.Code[Current + 1], Base, Base + Conv.Code[Current + 1]);
				int offset = Conv.Code[Current + 1];
				convDebug += string.Format("PUSHI_EFF, offset : {0}, Base : {1}, sum : {2}\n", offset, Base, Base + offset);
				if (offset >= 0)
					Stack.Push(Base + offset);
				else
				{
					offset--; //to skip over base ptr;
					Stack.Push(Base + offset);
				}
				Current++;
				//Stack.Push(Base + Conv.Code[++Current]);
				break;
			case OperationType.POP:
				convDebug += "POP\n";
				Stack.Pop();
				break;
			case OperationType.SWAP:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					convDebug += string.Format("SWAP, swapping top two stack, arg1 (top) : {0}, arg2 (second) : {1}\n", arg1, arg2);
					Stack.Push(arg1);
					Stack.Push(arg2);
				}
				break;
			case OperationType.PUSHBP:
				Stack.Push(Base);
				convDebug += string.Format("PUSHBP, pushing Base {0} to stack\n", Base);
				break;
			case OperationType.POPBP:
				{
					int arg1 = Stack.Pop();
					convDebug += string.Format("POPBP, popping arg1 : {0}, setting Base to that\n", arg1);
					Base = arg1;
				}
				break;

			case OperationType.SPTOBP:
				convDebug += string.Format("SPTOBP, stack ptr to base ptr, setting Base {0} to Stack.GetPointer() {1}\n", Base, Stack.GetPointer());
				Base = Stack.GetPointer();
				break;

			case OperationType.BPTOSP:
				convDebug += string.Format("BPTOSP, setting stack ptr to base ptr, stack ptr {0}, Base {1}\n", Stack.GetPointer(), Base);
				Stack.SetPointer(Base);
				break;
			case OperationType.ADDSP:
				{
					int arg1 = Stack.Pop();
					convDebug += string.Format("ADDSP, clearing memory from stack top to arg1 : {0}\n", arg1);
					for (int i = 0; i <= arg1; i++)
						Stack.Push(0);
				}
				break;
			case OperationType.FETCHM:
				{
					int arg1 = Stack.Pop();
					if (arg1 > 65537)
					{
						Debug.LogWarningFormat("Fetch argument is greater than 65537");
						arg1 -= 65537;
					}
					//convDebug += string.Format("FETCHM, popping adress : {0}, getting value : {1}\n", arg1, Stack.At(arg1));
					convDebug += string.Format("FETCHM, popping adress : {0}, getting value : {1}\n", arg1, Stack[arg1]);
					//Stack.Push(Stack.At(arg1));
					Stack.Push(Stack[arg1]);
				}
				break;
			case OperationType.STO:
				{
					int val = Stack.Pop();// Stack.At(Stack.StackPointer - 1);
					int adr = Stack.Pop();// Stack.At(Stack.StackPointer - 2);
					convDebug += string.Format("STO, popping s[0] : {0} - value, popping s[1] : {1} - adress, storing {0} at {1}\n", val, adr);
					Stack.Set(adr, val);
					//if (adr == Stack.StackPointer)
					//	Stack.StackPointer++;
				}
				break;
			case OperationType.OFFSET:
				{
					int arg1 = Stack.Pop();
					int arg2 = Stack.Pop();
					convDebug += string.Format("OFFSET, popped two, arg1 (top) : {0}, arg2 (second) : {1}, pushing (sum - 1) : {2}\n", arg1, arg2, arg1 + arg2 - 1);
					//arg1 += arg2 - 1;
					Stack.Push(arg1 + arg2 - 1);
				}
				break;
			case OperationType.START:
				break;
			case OperationType.SAVE_REG:
				{
					int arg1 = Stack.Pop();
					convDebug += string.Format("SAVE_REG, setting result register {0} to stack pop {1}\n", ResultRegister, arg1);
					ResultRegister = arg1;
				}
				break;
			case OperationType.PUSH_REG:
				convDebug += string.Format("PUSH_REG, pushing ResultRegister to stack {0}\n", ResultRegister);
				Stack.Push(ResultRegister);
				break;
			case OperationType.EXIT_OP:
				convDebug += "EXIT_OP\n";
				FinishConversation();
				break;
			case OperationType.SAY_OP:
				{
					int arg1 = Stack.Pop();
					convDebug += string.Format("SAY_OP, popping arg1 {0}\n", arg1);
					string text = Conv.GetString(arg1);
					Say(text);
				}
				break;

			case OperationType.OPNEG:
				{
					int arg1 = Stack.Pop();
					convDebug += string.Format("OPNEG, popping arg1 {0}, pushing negative of that {1}\n", arg1, -arg1);
					Stack.Push(-arg1);
				}
				break;
			default:
				Debug.LogErrorFormat("Invalid operation {0} at {1}\n", op, Current);
				return;
		}

		++Current;
		if (Current > Conv.Code.Count)
			FinishConversation();

		convDebug += string.Format("After, Current : {0},\nop : {1}, Base : {2},\nCallLevel : {3},\nResultRegister : {4},\nStackPointer : {5}\n", Current, op, Base, CallLevel, ResultRegister, Stack.Pointer);
		string stackDebug = "";
		if (single)
		{
			stackDebug = "Stack : \n";
			stackDebug += Stack.DumpStackFrom(0, Stack.Pointer);
		}
		if (debugOn)
			DebugConversation(convDebug, stackDebug);
	}

	private void DebugConversation(string convDebug, string stackDebug, bool refresh = false)
	{
		if (DebugWindow)
		{
			if (DebugWindow.DebugPanel.text.Length > 16000)
				DebugWindow.DebugPanel.text = DebugWindow.DebugPanel.text.Substring(2000);
			DebugWindow.DebugPanel.text += convDebug;
			if (refresh || single)
			{
				if (!string.IsNullOrEmpty(stackDebug))
					DebugWindow.StackPanel.text = stackDebug;

				Canvas.ForceUpdateCanvases();
				RectTransform rt = DebugWindow.DebugPanel.transform.parent.GetComponent<RectTransform>();
				rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.sizeDelta.y);
			}
		}
	}

	public void FinishConversation()
	{
		finished = true;
		Debug.LogFormat("Finishing conv, conv.MemorySlots : {0}, Conv.SavedVars.Length : {1}, Conv.ImportedVariables : {2}, conv.FirstMemorySlot : {3}, conv.UnnamedVars.Length", Conv.MemorySlots, Conv.SavedVars.Length, Conv.ImportedVariables, Conv.FirstMemorySlot, Conv.UnnamedVars.Length);
		for (int i = 0; i < Conv.MemorySlots - Conv.FirstMemorySlot; i++)
		{
			//Conv.PrivateGlobals[i] = Stack.At(Conv.ImportedVariables + i);

			//Conv.ConvGlobals[i] = Stack[Conv.ImportedVariables + i];
			Conv.SavedVars[i] = Stack[Conv.FirstMemorySlot + i];
		}
		for (int i = 0; i < Conv.UnnamedVars.Length; i++)
		{
			Conv.UnnamedVars[i] = Stack[i];
		}
		Conv.GetStringBlock().ResetStrings();
		Say("Finished conversation", Color.red, FontStyle.Bold);
	}

	public void Say(string text, Color? col = null, FontStyle fs = FontStyle.Italic)
	{
		int safe = 100;
		while (text.IndexOf('@') != -1)
		{
			text = ReplaceText(text, text.IndexOf('@'));
			safe--;
			if(safe==0)
			{
				Debug.LogErrorFormat("Infinite loop while replacing text : {0}", text);
				break;
			}
		}
		GameObject newText = UnityEngine.Object.Instantiate(ConvWindow.TextPrefab, ConvWindow.TextContent.transform);
		newText.name = "Text_" + ConvWindow.TextContent.transform.childCount;
		Text textComp = newText.GetComponent<Text>();
		textComp.text = text;
		if (col != null)
			textComp.color = (Color)col;
		textComp.fontStyle = fs;

		Canvas.ForceUpdateCanvases();
		
		RectTransform rt = ConvWindow.TextContent.GetComponent<RectTransform>();
		RectTransform textRt = newText.GetComponent<RectTransform>();
		//Debug.LogFormat("RT anchored: {0}, RT size: {1}", rt.anchoredPosition, rt.sizeDelta);
		rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.y + textRt.sizeDelta.y);
		rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, rt.sizeDelta.y + textRt.sizeDelta.y);
	}
	private Tuple<string, int>[] GetResponses(int stringAdress)
	{
		Tuple<string, int>[] responses = new Tuple<string, int>[GetResponseCount(stringAdress)];
		for (int i = 0; i < responses.Length; i++)
		{
			int j = stringAdress + i;
			if (Stack[j] > 0)
				responses[i] = new Tuple<string, int>(Conv.GetString(Stack[j]), Stack[j]);
			else
				responses[i] = null;
		}
		return responses;
	}

	private int GetResponseCount(int stringAdress)
	{
		int count = 0;
		for (int i = stringAdress; i < Stack.Length; i++)
		{
			if (Stack[i] > 0)
				count++;
			else
				break;
		}
		Debug.LogFormat("Response count : {0}", count);
		return count;
	}
	private void SetResponses(string[] responses)
	{
		Tuple<string, int>[] fresponses = new Tuple<string, int>[responses.Length];
		for (int i = 0; i < responses.Length; i++)
			fresponses[i] = new Tuple<string, int>(responses[i], -1);
		SetResponses(fresponses);
	}

	private void SetResponses(Tuple<string, int>[] responses, bool[] flags = null)
	{
		for (int i = 0; i < responses.Length; i++)
		{
			if (responses[i] == null)
				continue;
			if (flags != null && !flags[i])
				continue;
			string response = responses[i].Item1;
			while (response.IndexOf('@') != -1)
				response = ReplaceText(response, response.IndexOf('@'));
			GameObject newResponse = UnityEngine.Object.Instantiate(ConvWindow.ResponsePrefab, ConvWindow.ResponsePanel.transform);
			newResponse.GetComponent<Text>().text = (i + 1) + ": " + response;
			Button button = newResponse.GetComponent<Button>();
			int newIndex = i;
			int storedIndex = responses[i].Item2;
			Action act = () =>
			{
				ResultRegister = flags == null ? newIndex + 1 : storedIndex;
				if (debugOn)
					DebugConversation("Setting result to " + ResultRegister + "\n", null);
				Say(response, new Color(0.2f, 0.2f, 0.2f), FontStyle.Normal);
				ClearResponses();
				ResumeConversation();
			};
			button.onClick.AddListener(() => act());
		}
		PauseConversation();
	}

	private void ClearResponses()
	{
		for (int i = 0; i < ConvWindow.ResponsePanel.transform.childCount; i++)
			UnityEngine.Object.Destroy(ConvWindow.ResponsePanel.transform.GetChild(i).gameObject);
	}

	public string ReplaceText(string text, int index)
	{
		char repType = text[index + 1];
		char varType = text[index + 2];
		string num = "";
		int end = index;
		string old = "@" + repType.ToString() + varType.ToString();
		for (int i = index + 3; i < text.Length; i++)
		{
			char c = text[i];
			if (c == '-' || char.IsDigit(c))
			{
				num += c;
				old += c;
				end++;
			}
			else
				break;
		}
		int ptr = 0;
		bool success = int.TryParse(num, out ptr);
		string replacement = "";
		if(!success)
		{
			Debug.LogErrorFormat("Failed to parse string {0} to int in text {1}", num, text);
		}
		if(repType == 'G')
			replacement = GetImportedGlobal(ptr).ToString();
		else if(repType == 'S')
		{
			if (varType == 'I')
				replacement = Stack[Base + ptr].ToString();
			else if(varType == 'S')
				replacement = Conv.GetString(Stack[Base + ptr]);
		}
		else if(repType == 'P')
		{
			if(varType == 'I')
				replacement = Stack[Stack[Base + ptr]].ToString();
			else if(varType == 'S')
				replacement = Conv.GetString(Stack[Stack[Base + ptr]]);
		}
		else
		{
			Debug.LogErrorFormat("Invalid replacement signature {0} from text {1}", old, text);
			replacement = old;
		}
		if(replacement == "")
		{
			Debug.LogWarningFormat("Replacement failed - empty string, old : {0}, repType : {1}, varType : {2}, Base : {3}, ptr : {4}", old, repType, varType, Base, ptr);
			replacement = old;
		}
		Debug.LogFormat("Searching text for old : {0}", text.IndexOf(old));
		text = text.Replace(old, replacement);
		
		Debug.LogFormat("String has replacement type {0}, of variable {1}, pointer : {2}\nold : {5}, replacement : {3}, replaced text : {4}", repType, varType, num, replacement, text, old);
		return text;
	}

	private void DoFunction(ConversationFunction func)
	{
		switch (func.Name)
		{
			case "babl_menu":			Babl_Menu();		break;
			case "babl_fmenu":			Babl_FMenu();		break;
			case "print":				Print();			break;
			case "babl_ask":			Babl_Ask();			break;
			case "compare":				Compare();			break;
			case "random":				Random();			break;
			case "contains":			Contains();			break;
			case "length":				Length();			break;
			case "get_quest":			Get_Quest();		break;
			case "set_quest":			Set_Quest();		break;
			case "sex":					Sex();				break;
			case "show_inv":			Show_Inv();			break;
			case "give_to_npc":			Give_To_Npc();		break;
			case "give_ptr_npc":		Give_Ptr_Npc();		break;
			case "take_from_npc":		Take_From_Npc();	break;
			case "take_id_from_npc":	Take_Id_From_Npc();	break;
			case "identify_inv":		Identify_Inv();		break;
			case "do_offer":			Do_Offer();			break;
			case "do_demand":			Do_Demand();		break;
			case "do_inv_create":		Do_Inv_Create();	break;
			case "do_inv_delete":		Do_Inv_Delete();	break;
			case "check_inv_quality":	Check_Inv_Quality();break;
			case "set_inv_quality":		Set_Inv_Quality();	break;
			case "count_inv":			Count_Inv();		break;
			case "setup_to_barter":		Setup_To_Barter();	break;
			case "end_barter":			End_Barter();		break;
			case "do_judgement":		Do_Judgement();		break;
			case "do_decline":			Do_Decline();		break;
			case "set_likes_dislikes":	Set_Likes_Dislikes();break;
			case "gronk_door":			Gronk_Door();		break;
			case "set_race_attitude":	Set_Race_Attitude();break;
			case "place_object":		Place_Object();		break;
			case "take_from_npc_inv":	Take_From_Npc_Inv();break;
			case "add_to_npc_inv":		Add_To_Npc_Inv();	break;
			case "remove_talker":		Remove_Talker();	break;
			case "set_attitude":		Set_Attitude();		break;
			case "x_skills":			X_Skills();			break;
			case "x_traps":				X_Traps();			break;
			case "x_obj_pos":			X_Obj_Pos();		break;
			case "x_obj_stuff":			X_Obj_Stuff();		break;
			case "find_inv":			Find_Inv();			break;
			case "find_barter":			Find_Barter();		break;
			case "find_barter_total":	Find_Barter_Total();break;

			case "pause":
			case "val":					Val();				break;
			case "say":
			case "respond":
			case "copy":
			case "find":
			case "append":
			case "plural": Debug.LogWarningFormat("Conv. {0} is calling {1} at {2}", Conv, func.Name, Current); break;

			default:
				Debug.LogErrorFormat("Not implemented function : {0}", func.Name);
				finished = true;
				return;
		}
	}

	#region Imported functions

	private void Babl_Menu()
	{
		int arrayPointer = Stack[Stack.Pointer - 2];
		Debug.LogFormat("Babl menu, argStart: {0}, stack ptr : {1}", arrayPointer, Stack.Pointer);
		SetResponses(GetResponses(arrayPointer));
	}
	private void Babl_FMenu()
	{
		int strArrayPointer = Stack[Stack.Pointer - 2];
		int flagArrayPointer = Stack[Stack.Pointer - 3];
		Debug.LogFormat("Babl fmenu, strStart : {0}, flagStart : {1}, stack ptr : {2}", strArrayPointer, flagArrayPointer, Stack.Pointer);
		Tuple<string, int>[] responses = GetResponses(strArrayPointer);
		bool[] flags = new bool[responses.Length];
		for (int i = 0; i < responses.Length; i++)
		{
			//if (Stack.Values[flagAdr + i] == 1)
			if(Stack[flagArrayPointer + i] == 1)
				flags[i] = true;
		}
		//foreach (var response in responses)
		//	Debug.Log(response);
		//foreach (var flag in flags)
		//	Debug.Log(flag);
		SetResponses(responses, flags);
	}
	private void Sex()
	{
		int femPtr = Stack[Stack.Pointer - 2];
		int malPtr = Stack[Stack.Pointer - 3];
		ConvWindow.Popup = CreateYesOrNoPopup("Are you male?", () => ResultRegister = Stack[malPtr], () => ResultRegister = Stack[femPtr]);
		PauseConversation();
	}
	private void Print()
	{
		//Debug.LogFormat("print, stack pointer - 2 : {0}, Stack[Stack.Pointer - 2] : {1}, ...pointer : {2}", Stack.Pointer - 2, Stack[Stack.Pointer - 2], Stack[Stack[Stack.Pointer - 2]]);
		string toPrint = Conv.GetString(Stack[Stack[Stack.Pointer - 2]]);
		Say(toPrint, Color.blue);
	}
	private void Babl_Ask()
	{
		ConvWindow.Popup = CreateInputPopup("Ask something?", (str) => ResultRegister = Conv.GetStringBlock().AddTempString(str), InputField.ContentType.Alphanumeric);
		PauseConversation();
	}
	private void Compare()
	{
		int ptr1 = Stack[Stack[Stack.Pointer - 2]];
		int ptr2 = Stack[Stack[Stack.Pointer - 3]];
		string str1 = Conv.GetString(ptr1).ToLower();
		string str2 = Conv.GetString(ptr2).ToLower();
		Debug.LogFormat("compare, ptr1 : {0}, ptr2 : {1}, str1 : {2}, str2 : {3}", ptr1, ptr2, str1, str2);
		if (str1 == str2)
			ResultRegister = 1;
		else
			ResultRegister = 0;
	}
	private void Random()
	{
		int max = Stack[Stack[Stack.Pointer - 2]];
		System.Random rnd = new System.Random();
		ResultRegister = rnd.Next(max + 1);
	}
	private void Contains()
	{
		int ptr1 = Stack[Stack[Stack.Pointer - 2]];
		int ptr2 = Stack[Stack[Stack.Pointer - 3]];
		string str1 = Conv.GetString(ptr1).ToLower();
		string str2 = Conv.GetString(ptr2).ToLower();
		Debug.LogFormat("contains, ptr1 : {0}, ptr2 : {1}, str1 : {2}, str2 : {3}", ptr1, ptr2, str1, str2);
		if (str1.Contains(str2))
			ResultRegister = 1;
		else
			ResultRegister = 0;
	}
	private void Length()
	{
		int strIndex = Stack[Stack.Pointer - 2];
		string str = Conv.GetString(strIndex);
		ResultRegister = str.Length;
	}
	private void Val()
	{
		int ptr = Stack[Stack[Stack.Pointer - 2]];
		string str = Conv.GetString(ptr).ToLower();
		int val = 0;
		bool success = int.TryParse(str, out val);
		if (success)
			ResultRegister = val;
		else
			ResultRegister = 0;
	}
	private void Get_Quest()
	{
		int questId = Stack[Stack[Stack.Pointer - 2]];
		string quest = MapCreator.GameData.GetQuestName(questId);
		ConvWindow.Popup = CreateInputPopup(string.Format("Get quest\nQuest ID : {0}, description : \n'{1}'\nReturn quest status as 0 (not yet done)\nor 1 (done)",questId, quest), (str) => ResultRegister = int.Parse(str), InputField.ContentType.IntegerNumber);
		PauseConversation();
	}
	private void Set_Quest()
	{
		int val = Stack[Stack[Stack.Pointer - 2]];
		int questId = Stack[Stack[Stack.Pointer - 3]];
		
		string quest = MapCreator.GameData.GetQuestName(questId);
		ConvWindow.Popup = CreateOKPopup(string.Format("Set quest\nQuest ID : {0}, description :\n'{1}'\nSetting value to {2}", questId, quest, val));
		PauseConversation();
	}
	private void Show_Inv()
	{
		int arr1 = Stack[Stack.Pointer - 2];	//Master list positions?
		int arr2 = Stack[Stack.Pointer - 3];	//Object id's
		Action act = () =>
		{
			ConversationPopup convPopup = ConvWindow.Popup.GetComponent<ConversationPopup>();
			ResultRegister = 0;
			for (int i = 0; i < 4; i++)
			{
				Dropdown itemDropdown = convPopup.GridPanel.transform.GetChild(i).gameObject.GetComponent<Dropdown>();
				int objId = itemDropdown.value - 1;
				if (objId > -1 && objId < 320)
				{
					Stack.Set(arr2 + ResultRegister, objId);
					Stack.Set(arr1 + ResultRegister, 750 + ResultRegister);	//Dummy master list pos
					ResultRegister++;
				}
			}
		};
		ConvWindow.Popup = CreateInventoryDropdownPopup("Choose objects to give", act);
		PauseConversation();
	}
	private void Give_To_Npc()
	{
		int arr1 = Stack[Stack.Pointer - 2];    //Master list positions?
		int arr2 = Stack[Stack.Pointer - 3];    //Object quantities
		ConvWindow.Popup = CreateYesOrNoPopup("Give to NPC\nGive items to NPC?", () => ResultRegister = 1, () => ResultRegister = 0);
		PauseConversation();
	}
	private void Give_Ptr_Npc()
	{
		int quant = Stack[Stack[Stack.Pointer - 2]];
		int pos = Stack[Stack[Stack.Pointer - 3]];
		ConvWindow.Popup = CreateOKPopup(string.Format("Give ptr NPC\nQuantity? : {0}\nPosition : {1}\nCopied item to NPC", quant, pos));
		PauseConversation();
	}
	private void Add_To_Npc_Inv()
	{
		ConvWindow.Popup = CreateOKPopup("Add to NPC inv.\nunknown function");
		PauseConversation();
	}
	private void Take_From_Npc()
	{
		int arg = Stack[Stack.Pointer - 2];
		string name = (arg < 320) ? StaticObject.GetName(arg) : "ID from " + (arg - 1000) + " to " + (arg - 1000 + 16);
		ConvWindow.Popup = CreateYesOrNoPopup(string.Format("Take from NPC\nItem name or accepted item ID's:\n{0}\nAccept items from NPC?", name), () => ResultRegister = 1, () => ResultRegister = 2);
		PauseConversation();
	}
	private void Take_Id_From_Npc()
	{
		int pos = Stack[Stack.Pointer - 2];
		ConvWindow.Popup = CreateYesOrNoPopup(string.Format("Take Id from NPC\nObject position on master list : {0}\nAccept items from NPC?", pos), () => ResultRegister = 1, () => ResultRegister = 2);
		PauseConversation();
	}
	private void Take_From_Npc_Inv()
	{
		int arg = Stack[Stack.Pointer - 2];
		ConvWindow.Popup = CreateInputPopup(string.Format("Take from NPC inv.\nargument passed : {0}\nreturn object list position", arg), (str) => ResultRegister = int.Parse(str), InputField.ContentType.IntegerNumber);
		PauseConversation();
	}
	private void Find_Inv()
	{
		int invType = Stack[Stack[Stack.Pointer - 2]];
		int itemType = Stack[Stack[Stack.Pointer - 3]];
		string invTypeStr = (invType == 0) ? "NPC" : "player";
		string itemTypeStr = StaticObject.GetName(itemType);
		ConvWindow.Popup = CreateInputPopup(string.Format("Find inv.\nInventory type : {0}\nSearched object ID : {1}\nReturn position in master list\nor 0 if not found\n(for player, 1 if found?).", invTypeStr, itemTypeStr), (str) => ResultRegister = int.Parse(str), InputField.ContentType.IntegerNumber);
		PauseConversation();
	}
	private void Identify_Inv()
	{
		int arg1 = Stack[Stack.Pointer - 2];
		int arg2 = Stack[Stack.Pointer - 3];
		int arg3 = Stack[Stack.Pointer - 4];
		int arg4 = Stack[Stack.Pointer - 5];
		ConvWindow.Popup = CreateInputPopup(string.Format("Identify inventory\nArguments : {0}\n{1}(string pointer), {2}\n{3}(trade slot index)\nReturn object value", arg1, arg2, arg3, arg4), (str) => ResultRegister = int.Parse(str), InputField.ContentType.IntegerNumber);
		PauseConversation();
	}
	private void Do_Inv_Create()
	{
		int id = Stack[Stack.Pointer - 2];
		string name = StaticObject.GetName(id);
		ConvWindow.Popup = CreateInputPopup(string.Format("Do inv. create\nCreated {0} in NPC\ninventory, input object list position:", name), (str) => ResultRegister = int.Parse(str), InputField.ContentType.IntegerNumber);
		PauseConversation();
	}
	private void Do_Inv_Delete()
	{
		int id = Stack[Stack.Pointer - 2];
		string name = StaticObject.GetName(id);
		ConvWindow.Popup = CreateOKPopup(string.Format("Do inv. delete\nDeleted {0} in NPC\ninventory", name));
		PauseConversation();
	}
	private void Check_Inv_Quality()
	{
		ConvWindow.Popup = CreateInputPopup("Check inv. quality\nInput inventory quality\n(0 - 40)", (str) => ResultRegister = int.Parse(str), InputField.ContentType.IntegerNumber);
		PauseConversation();
	}
	private void Set_Inv_Quality()
	{
		int qual = Stack[Stack.Pointer - 2];
		int pos = Stack[Stack.Pointer - 3];
		ConvWindow.Popup = CreateOKPopup(string.Format("Set inv. quality\nChanged quality of item at pos {0}\n to {1}", qual, pos));
		PauseConversation();
	}
	private void Count_Inv()
	{
		int arg = Stack[Stack[Stack.Pointer - 2]];
		ConvWindow.Popup = CreateInputPopup(string.Format("Count inv.\nReturn number of items\nArgument - object position : {0}", arg), (str) => ResultRegister = int.Parse(str), InputField.ContentType.IntegerNumber);
		PauseConversation();
	}
	private void Gronk_Door()
	{
		int open = Stack[Stack[Stack.Pointer - 2]];
		int doory = Stack[Stack[Stack.Pointer - 3]];
		int doorx = Stack[Stack[Stack.Pointer - 4]];
		bool isOpen = (open == 0);
		ConvWindow.Popup = CreateOKPopup(string.Format("Handle door\ndoor at [x {0},y {1}]\nopen : {2}", doorx, doory, isOpen));
		ResultRegister = 1;
		PauseConversation();
	}
	private void Set_Race_Attitude()
	{
		int arg1 = Stack[Stack.Pointer - 2];
		int arg2 = Stack[Stack.Pointer - 3];
		int arg3 = Stack[Stack.Pointer - 4];
		ConvWindow.Popup = CreateOKPopup(string.Format("Set race attitude\nArguments : {0}(unknown - area?)\n{1}(attitude)\n{2}(race)", arg1, arg2, arg3));
		PauseConversation();
	}
	private void Place_Object()
	{
		int itemx = Stack[Stack.Pointer - 2];
		int itemy = Stack[Stack.Pointer - 3];
		int slot = Stack[Stack.Pointer - 4];
		ConvWindow.Popup = CreateOKPopup(string.Format("Place object\ncoords [x {0},y {1}]\nnpc item pos {2}", itemx, itemy, slot));
		PauseConversation();
	}
	private void Remove_Talker()
	{
		ConvWindow.Popup = CreateOKPopup("Remove talker");
		PauseConversation();
	}
	private void Set_Attitude()
	{
		int arg1 = Stack[Stack.Pointer - 2];
		int arg2 = Stack[Stack.Pointer - 3];
		ConvWindow.Popup = CreateOKPopup(string.Format("Set attitude\nArguments : {0}(attitude)\n{1}(target ID)", arg1, arg2));
		PauseConversation();
	}
	private void X_Skills()
	{
		int type = Stack[Stack[Stack.Pointer - 2]];
		int skill = Stack[Stack[Stack.Pointer - 3]];
		string typeStr = "";
		if (type == 10000)
			typeStr = "add skill";
		else if (type == 10001)
			typeStr = "get skill";
		else
			typeStr = "invalid";
		string skillStr = MapCreator.StringData.GetSkillName(skill);
		ConvWindow.Popup = CreateInputPopup(string.Format("X skills\nType : {0}, skill : {1}\nEnter skill value\n(if add skill - after adding)", typeStr, skillStr), (str) => ResultRegister = int.Parse(str), InputField.ContentType.IntegerNumber);
		PauseConversation();
	}
	private void X_Traps()
	{
		int type = Stack[Stack[Stack.Pointer - 2]];
		int var = Stack[Stack[Stack.Pointer - 3]];
		string typeStr = "";
		if (type == 10001)
			typeStr = "get variable";
		else
			typeStr = "invalid / unknown";
		ConvWindow.Popup = CreateInputPopup(string.Format("X traps\nType : {0}, variable : {1}\nEnter variable value", typeStr, var), (str) => ResultRegister = int.Parse(str), InputField.ContentType.IntegerNumber);
		PauseConversation();
	}
	private void X_Obj_Pos()
	{
		ConvWindow.Popup = CreateOKPopup("X object pos.\nunknown function");
		PauseConversation();
	}
	private void X_Obj_Stuff()
	{
		int qual = Stack[Stack.Pointer - 2];
		int arg2 = Stack[Stack.Pointer - 3];
		int arg3 = Stack[Stack.Pointer - 4];
		int link = Stack[Stack.Pointer - 5];
		int flags = Stack[Stack.Pointer - 6];
		int owner = Stack[Stack.Pointer - 7];
		int dir = Stack[Stack.Pointer - 8];
		int type = Stack[Stack.Pointer - 9];
		int pos = Stack[Stack[Stack.Pointer - 10]];
		string typeStr = "";
		if (Stack[type] == 0)
			typeStr = "get item property";
		else if (Stack[type] == 1)
			typeStr = "set item property";
		else
			typeStr = "invalid";

		string changed = "";

		if(Stack[dir] != -1)
			changed += "direction ";
		if(Stack[owner] != -1)
			changed += "owner ";
		if(Stack[link] != -1)
			changed += "special ";
		if (Stack[flags] != -1)
			changed += "flags ";
		if (Stack[qual] != -1)
			changed += "quality ";
		Action<string> change = (str) =>
		{
			if(Stack[dir] != -1)
			{
				int q = int.Parse(str);
				Stack.Set(dir, q);
			}
			if(Stack[owner] != -1)
			{
				int o = int.Parse(str);
				Stack.Set(owner, o);
			}
			if(Stack[link] != -1)
			{
				int s = int.Parse(str);
				Stack.Set(link, s);
			}
			if (Stack[flags] != -1)
			{
				int f = int.Parse(str);
				Stack.Set(flags, f);
			}
			if (Stack[qual] != -1)
			{
				int q = int.Parse(str);
				Stack.Set(qual, q);
			}
		};

		ConvWindow.Popup = CreateInputPopup(string.Format("X obj stuff\nType : {0}\nProperties : {1}", typeStr, changed), change, InputField.ContentType.IntegerNumber);
		PauseConversation();
	}

	#region Barter functions


	private void Setup_To_Barter()
	{
		ConvWindow.Popup = CreateOKPopup("Setting up to barter");
		PauseConversation();
	}
	private void End_Barter()
	{
		ConvWindow.Popup = CreateOKPopup("Ending barter");
		PauseConversation();
	}
	private void Do_Judgement()
	{
		ConvWindow.Popup = CreateOKPopup("Appraising items\n(normally this prints results in window).");
		PauseConversation();
	}
	private void Do_Decline()
	{
		ConvWindow.Popup = CreateOKPopup("Do Decline\nEnding barter");
		PauseConversation();
	}
	private void Set_Likes_Dislikes()
	{
		int likes = Stack[Stack.Pointer - 2];
		int dislikes = Stack[Stack.Pointer - 3];
		ConvWindow.Popup = CreateOKPopup(string.Format("Set likes / dislikes\nlikes array at {0}\ndislikes array at {1}", likes, dislikes));
		PauseConversation();
	}
	private void Do_Offer()
	{
		string str1 = Conv.GetString(Stack[Stack[Stack.Pointer - 2]]);
		string str2 = Conv.GetString(Stack[Stack[Stack.Pointer - 3]]);
		string str3 = Conv.GetString(Stack[Stack[Stack.Pointer - 4]]);
		string str4 = Conv.GetString(Stack[Stack[Stack.Pointer - 5]]);
		string str5 = Conv.GetString(Stack[Stack[Stack.Pointer - 6]]);
		ConvWindow.Popup = CreateYesOrNoPopup(string.Format("Do offer\nBarter strings :\n{0}\n{1}\n{2}\n{3}\n{4}\nNPC accepted offer?", str1, str2, str3, str4, str5), () => ResultRegister = 1, () => ResultRegister = 0);
		PauseConversation();
	}
	private void Do_Demand()
	{
		string str1 = Conv.GetString(Stack[Stack[Stack.Pointer - 2]]);
		string str2 = Conv.GetString(Stack[Stack[Stack.Pointer - 3]]);
		ConvWindow.Popup = CreateYesOrNoPopup(string.Format("Do demand\nDemand strings:\n{0}\n{1}\nPlayer persuaded NPC?", str1, str2), () => ResultRegister = 1, () => ResultRegister = 0);
		PauseConversation();
	}
	private void Find_Barter()
	{
		int itemType = Stack[Stack[Stack.Pointer - 2]];
		string itemTypeStr = StaticObject.GetName(itemType);
		ConvWindow.Popup = CreateInputPopup(string.Format("Find barter\nSearching item {0} in barter area.\nReturn position in master list\nor 0 if not found.", itemTypeStr), (str) => ResultRegister = int.Parse(str), InputField.ContentType.IntegerNumber);
		PauseConversation();
	}
	private void Find_Barter_Total()
	{
		int arg1 = Stack[Stack.Pointer - 2];
		int arg2 = Stack[Stack.Pointer - 3];
		int arg3 = Stack[Stack.Pointer - 4];
		int arg4 = Stack[Stack[Stack.Pointer - 5]];
		//int arg5 = Stack[Stack.Pointer - 6];
		Action<string> act = (str) =>
		{
			int val = int.Parse(str);
			Stack.Set(arg1, 0);
			Stack.Set(arg2, 0);
			if(val > 0)
			{

			}
			Debug.LogWarning("Function not implemented");
		};
		ConvWindow.Popup = CreateInputPopup(string.Format("Find barter total\nFunction has 5 arguments\n{0}(count), {1}(slot), {2}(slot count), {3}(object id)\nreturn 1 when found?", arg1, arg2, arg3, arg4), act, InputField.ContentType.IntegerNumber);
		PauseConversation();
	}
	#endregion

	#endregion



	private ConversationPopup CreatePopup(string question)
	{
		GameObject convPopupGO = UnityEngine.Object.Instantiate(ConvWindow.ConversationPopupPrefab, ConvWindow.transform.parent);
		ConversationPopup convPopup = convPopupGO.GetComponent<ConversationPopup>();
		convPopup.CloseButton.SetActive(false);
		convPopup.Question.text = question;
		return convPopup;
	}

	private GameObject CreateYesOrNoPopup(string question, Action trueAct, Action falseAct)
	{
		ConversationPopup convPopup = CreatePopup(question);

		convPopup.Yes.gameObject.SetActive(true);
		convPopup.No.gameObject.SetActive(true);
		convPopup.Yes.onClick.AddListener(() => trueAct());
		convPopup.No.onClick.AddListener(() => falseAct());
		convPopup.Yes.onClick.AddListener(() => { ResumeConversation(); UnityEngine.Object.Destroy(convPopup.gameObject); });
		convPopup.No.onClick.AddListener(() => { ResumeConversation(); UnityEngine.Object.Destroy(convPopup.gameObject); });
		return convPopup.gameObject;
	}

	private GameObject CreateInputPopup(string question, Action<string> onEnter, InputField.ContentType inputType)
	{
		ConversationPopup convPopup = CreatePopup(question);

		convPopup.Answer.gameObject.SetActive(true);
		convPopup.Answer.contentType = inputType;
		convPopup.Answer.onEndEdit.AddListener((x) => onEnter(x));
		convPopup.Answer.onEndEdit.AddListener((x) => { ResumeConversation(); UnityEngine.Object.Destroy(convPopup.gameObject); });
		return convPopup.gameObject;
	}

	private GameObject CreateOKPopup(string message)
	{
		ConversationPopup convPopup = CreatePopup(message);

		convPopup.Yes.gameObject.SetActive(true);
		convPopup.Yes.GetComponentInChildren<Text>().text = "OK";
		convPopup.Yes.onClick.AddListener(() => { ResumeConversation(); UnityEngine.Object.Destroy(convPopup.gameObject); });
		return convPopup.gameObject;
	}

	private GameObject CreateInventoryDropdownPopup(string message, Action onOK)
	{
		ConversationPopup convPopup = CreatePopup(message);

		convPopup.Yes.gameObject.SetActive(true);
		convPopup.Yes.GetComponentInChildren<Text>().text = "OK";
		convPopup.Yes.onClick.AddListener(() => onOK());
		convPopup.Yes.onClick.AddListener(() => { ResumeConversation(); UnityEngine.Object.Destroy(convPopup.gameObject); });
		convPopup.GridPanel.SetActive(true);
		for (int i = 0; i < 4; i++)
		{
			GameObject dropdown = UnityEngine.Object.Instantiate(UIManager.ItemDropdownPrefab, convPopup.GridPanel.transform);
			dropdown.SetActive(true);
		}
		return convPopup.gameObject;
	}

	private GameObject CreateInventoryInputPrefab(string message, Action onOK)
	{
		ConversationPopup convPopup = CreatePopup(message);
		convPopup.Yes.gameObject.SetActive(true);
		convPopup.Yes.GetComponentInChildren<Text>().text = "OK";
		convPopup.Yes.onClick.AddListener(() => onOK());
		convPopup.Yes.onClick.AddListener(() => { ResumeConversation(); UnityEngine.Object.Destroy(convPopup.gameObject); });
		convPopup.GridPanel.SetActive(true);
		for (int i = 0; i < 4; i++)
		{
			GameObject inputGO = UnityEngine.Object.Instantiate(convPopup.InputFieldPrefab, convPopup.GridPanel.transform);
			InputField input = inputGO.GetComponent<InputField>();
			input.contentType = InputField.ContentType.IntegerNumber;
			input.text = "0";
		}
		return convPopup.gameObject;
	}
}

[System.Serializable]
public class ConversationFunction
{
	public const int Void = 0;
	public const int Int = 297;
	public const int String = 299;
	public const int Variable = 271;
	public const int Function = 273;

	public int NameLength;
	public string Name;
	public int Id_Adress;
	public int Unk04;
	public int Type;
	public int Return;

	public static string GetFuncName(int id)
	{
		switch (id)
		{
			case 0: return "babl_menu";
			case 1: return "babl_fmenu";
			case 2: return "print";
			case 3: return "babl_ask";
			case 4: return "compare";
			case 5: return "random";
			case 6: return "plural";
			case 7: return "contains";
			case 8: return "append";
			case 9: return "copy";
			case 10: return "find";
			case 11: return "length";
			case 12: return "val";
			case 13: return "say";
			case 14: return "respond";
			case 15: return "get_quest";
			case 16: return "set_quest";
			case 17: return "sex";
			case 18: return "show_inv";
			case 19: return "give_to_npc";
			case 20: return "give_ptr_npc";
			case 21: return "take_from_npc";
			case 22: return "take_id_from_npc";
			case 23: return "identify_inv";
			case 24: return "do_offer";
			case 25: return "do_demand";
			case 26: return "do_inv_create";
			case 27: return "do_inv_delete";
			case 28: return "check_inv_quality";
			case 29: return "set_inv_quality";
			case 30: return "count_inv";
			case 31: return "setup_to_barter";
			case 32: return "end_barter";
			case 33: return "do_judgement";
			case 34: return "do_decline";
			case 35: return "pause";
			case 36: return "set_likes_dislikes";
			case 37: return "gronk_door";
			case 38: return "set_race_attitude";
			case 39: return "place_object";
			case 40: return "take_from_npc_inv";
			case 41: return "add_to_npc_inv";
			case 42: return "remove_talker";
			case 43: return "set_attitude";
			case 44: return "x_skills";
			case 45: return "x_traps";
			case 46: return "x_obj_pos";
			case 47: return "x_obj_stuff";
			case 48: return "find_inv";
			case 49: return "find_barter";
			case 50: return "find_barter_total";

			default:
				return "invalid";
		}
	}

	public string GetFuncType()
	{
		if (Type == 271)
			return "variable";
		else if (Type == 273)
			return "function";

		return "INVALID";
	}

	public string GetReturnType()
	{
		if (Return == 0)
			return "void";
		else if (Return == 297)
			return "int";
		else if (Return == 299)
			return "string";

		return "INVALID";
	}
}

public class ConversationOperation
{
	public static int GetArgsLength(OperationType type)
	{
		switch (type)
		{
			case OperationType.NOP:	return 0;
			case OperationType.OPADD: return 0;
			case OperationType.OPMUL: return 0;
			case OperationType.OPSUB: return 0;
			case OperationType.OPDIV: return 0;
			case OperationType.OPMOD: return 0;
			case OperationType.OPOR: return 0;
			case OperationType.OPAND: return 0;
			case OperationType.OPNOT: return 0;
			case OperationType.TSTGT: return 0;
			case OperationType.TSTGE: return 0;
			case OperationType.TSTLT: return 0;
			case OperationType.TSTLE: return 0;
			case OperationType.TSTEQ: return 0;
			case OperationType.TSTNE: return 0;
			case OperationType.JMP: return 1;
			case OperationType.BEQ: return 1;
			case OperationType.BNE: return 1;
			case OperationType.BRA: return 1;
			case OperationType.CALL: return 1;
			case OperationType.CALLI: return 1;
			case OperationType.RET: return 0;
			case OperationType.PUSHI: return 1;
			case OperationType.PUSHI_EFF: return 1;
			case OperationType.POP: return 0;
			case OperationType.SWAP: return 0;
			case OperationType.PUSHBP: return 0;
			case OperationType.POPBP: return 0;
			case OperationType.SPTOBP: return 0;
			case OperationType.BPTOSP: return 0;
			case OperationType.ADDSP: return 0;
			case OperationType.FETCHM: return 0;
			case OperationType.STO: return 0;
			case OperationType.OFFSET: return 0;
			case OperationType.START: return 0;
			case OperationType.SAVE_REG: return 0;
			case OperationType.PUSH_REG: return 0;
			case OperationType.STRCMP: return -1;
			case OperationType.EXIT_OP: return 0;
			case OperationType.SAY_OP: return 0;
			case OperationType.RESPOND_OP: return -1;
			case OperationType.OPNEG: return 0;
			default: return -1;
		}
	}
	public static int GetArgsLength(int type)
	{
		return GetArgsLength((OperationType)type);
	}

	public static string DumpCode(List<int> code)
	{
		string str = "";
		for (int i = 0; i < code.Count; i++)
		{
			OperationType type = (OperationType)code[i];
			str += type;
			if(type == OperationType.CALLI)
			{
				i++;
				str += ", func : " + ConversationFunction.GetFuncName(code[i]) + "(" + code[i] + ")";
			}
			else if(GetArgsLength(type) > 0)
			{
				i++;
				str += ", arg : " + code[i];
			}
			str += "\n";
		}
		return str;
	}
}

public class ConversationStack
{

	private int[] Values;
	public int Pointer;
	public int Length { get { return Values.Length; } }

	public ConversationStack(int size)
	{
		Values = new int[size];
		Pointer = 0;
	}

	public int Pop()
	{
		Pointer--;
		int pop = Values[Pointer];	//--StackPointer
		Values[Pointer] = 0;
		return pop;
	}

	public void Push(int val)
	{
		Values[Pointer++] = val;
	}

	public void Set(int adr, int val)
	{
		Values[adr] = val;
	}

	public void Set(int adr, int[] vals)
	{
		for (int i = 0; i < vals.Length; i++)
		{
			Values[adr + i] = vals[i];
		}
	}

	public int GetPointer()
	{
		return Pointer;
	}

	public void SetPointer(int ptr)
	{
		Pointer = ptr;
	}

	//Fix me!
	public int this[int index]
	{
		get {
			if (index >= Values.Length)
				index = (index - 65535);
			return Values[index];
		}
		set {
			if (index >= Values.Length)
				index = (index - 65535);
			Values[index] = value;
		}
	}

	public string DumpStackFrom(int from, int to)
	{
		string str = "";
		for (int i = from; i < to + 1; i++)
		{
			str += string.Format("\tstack at {0} = {1}\n", i, Values[i]);
		}
		return str;
	}
}

public enum OperationType
{
	NOP = 0,
	OPADD = 1,
	OPMUL = 2,
	OPSUB = 3,
	OPDIV = 4,
	OPMOD = 5,
	OPOR = 6,
	OPAND = 7,
	OPNOT = 8,
	TSTGT = 9,
	TSTGE = 10,
	TSTLT = 11,
	TSTLE = 12,
	TSTEQ = 13,
	TSTNE = 14,
	JMP = 15,
	BEQ = 16,
	BNE = 17,
	BRA = 18,
	CALL = 19,
	CALLI = 20,
	RET = 21,
	PUSHI = 22,
	PUSHI_EFF = 23,
	POP = 24,
	SWAP = 25,
	PUSHBP = 26,
	POPBP = 27,
	SPTOBP = 28,
	BPTOSP = 29,
	ADDSP = 30,
	FETCHM = 31,
	STO = 32,
	OFFSET = 33,
	START = 34,
	SAVE_REG = 35,
	PUSH_REG = 36,
	STRCMP = 37,
	EXIT_OP = 38,
	SAY_OP = 39,
	RESPOND_OP = 40,
	OPNEG = 41,
}
