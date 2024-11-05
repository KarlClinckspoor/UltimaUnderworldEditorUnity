using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using UnityEngine.EventSystems;

public class ConversationEditor : ResizableWindow {

	public GameObject BasicNodePrefab;
	public GameObject NodePrefab;
	public GameObject ConditionPrefab;
	public GameObject ConversationEditorContextMenuPrefab;
	public GameObject FindNodePrefab;
	public GameObject ButtonPrefab;
	public Toggle Catching;
	public InputField NameField;
	public InputField PrivateCountField;

	public GameObject Content;

	private GameObject contextMenu;
	public GameObject StartNode { get; protected set; }

	public Conversation Conv { get; protected set; }
	//public Dictionary<int, string> PrivateGlobals;
	public List<string> GlobalVariables;
	public List<string> LocalVariables;
	private string npcName;
	//private int globalVariableCount;

	private GameObject currentLine;

	public Color NodeColor;
	public Color StartColor;
	public Color ErrorColor;

	private ConversationNode startNode;
	private ConversationNode errorNode;
	private List<ConversationNode> nodeList;
	private Dictionary<ConversationNode, GameObject> nodeDict;

	private float moveContentFactor = 40.0f;

	public void Init(Conversation conv, UIManager ui)
	{
		uiManager = ui;
		//PrivateGlobals = new Dictionary<int, string>();
		GlobalVariables = new List<string>();
		LocalVariables = new List<string>();
		nodeList = new List<ConversationNode>();
		nodeDict = new Dictionary<ConversationNode, GameObject>();
		Conv = conv;
		Load(Conv.Slot);
		Action<string> changePrivates = (str) =>
		{
			if (string.IsNullOrEmpty(str))
				return;
			//globalVariableCount = int.Parse(str);
			ChangePrivates(int.Parse(str));
		};
		PrivateCountField.onEndEdit.AddListener((str) => changePrivates(str));
		Action<string> changeName = (str) =>
		{
			if (string.IsNullOrEmpty(str))
				return;
			npcName = str;
		};
		NameField.onEndEdit.AddListener((str) => changeName(str));
	}

	private void ChangePrivates(int newCount)
	{
		int min = Mathf.Min(newCount, GlobalVariables.Count);
		string[] newList = new string[newCount];
		Debug.LogFormat("new list, count : {0}", newList.Length);
		for (int i = 0; i < min; i++)
			newList[i] = GlobalVariables[i];
		GlobalVariables = newList.ToList();
		Conv.MemorySlots = newCount + 32;
		Debug.LogFormat("private globs count {0}", GlobalVariables.Count);
	}

	public string GetConversationFile()
	{
		if (!Conv)
			return null;
		return GetConversationFile(Conv.Slot);
	}
	public static string GetConversationFile(int slot)
	{
		return Application.dataPath + "/Conversations/conversation_" + slot + ".dat";
	}

	public void Save()
	{
		ConversationStructure str = CreateStructure();
		if (File.Exists(GetConversationFile()))
			File.Delete(GetConversationFile());
		FileStream fs = new FileStream(GetConversationFile(), FileMode.Create);
		BinaryFormatter bf = new BinaryFormatter();
		bf.Serialize(fs, str);
		fs.Close();
	}

	public void Load(int index)
	{
		if (File.Exists(GetConversationFile()))
		{
			//try
			//{
			Stream stream = File.Open(GetConversationFile(), FileMode.Open);
			BinaryFormatter bf = new BinaryFormatter();
			ConversationStructure str = (ConversationStructure)bf.Deserialize(stream);
			GlobalVariables = str.GlobalVariables;
			LocalVariables = str.LocalVariables;
			npcName = str.NPCName;
			//globalVariableCount = str.PrivateGlobalCount;

			PrivateCountField.text = (str.PrivateGlobalCount - 32).ToString();
			NameField.text = str.NPCName;

			Conv.MemorySlots = str.PrivateGlobalCount;
			Conv.BarterStrings = str.BarterStrings;
			Conv.Likes = str.Likes;
			Conv.Dislikes = str.Dislikes;
			//MapCreator.StringData.SetNPCName(Conv.Slot, str.NPCName);

			Dictionary<ConversationNodeStructure, GameObject> nodeDict = new Dictionary<ConversationNodeStructure, GameObject>();
			Dictionary<ConversationConditionStructure, GameObject> condDict = new Dictionary<ConversationConditionStructure, GameObject>();
			foreach (var nodeStr in str.Nodes)
			{
				GameObject nodeGO = AddNode(nodeStr.Position, nodeStr.NodeName);
				nodeDict[nodeStr] = nodeGO;
				ConversationNode node = nodeGO.GetComponent<ConversationNode>();
				node.Init();
				node.NodeContent.text = nodeStr.Content;
				RectTransform inputRt = node.NodeContent.gameObject.GetComponent<RectTransform>();
				inputRt.sizeDelta = new Vector2(nodeStr.InputXSize, inputRt.sizeDelta.y);
				node.Type = nodeStr.Type;
				if (node.Type == NodeType.Start)
					SetStartNode(node);
				foreach (var condStr in nodeStr.Conditions)
				{
					if (!node.ConditionsGO.transform.parent.gameObject.activeSelf)
						node.ConditionsGO.transform.parent.gameObject.SetActive(true);
					GameObject condGO = node.AddCondition(condStr.Content);
					ConversationCondition cond = condGO.GetComponent<ConversationCondition>();
					cond.Init(!condStr.Active);
					cond.Content.text = condStr.Content;

					condDict[condStr] = condGO;
				}
			}
			foreach (var connStr in str.Connections)
			{
				GameObject connGO = AddConnection(condDict[connStr.Output], nodeDict[connStr.Input]);

			}
			Debug.Log(str);
			stream.Close();
			//}
			//catch(Exception e)
			//{
			//	uiManager.SpawnPopupMessage("Failed to load conversation\nfile corruption?\n" + e.Message);
			//	return;
			//}
		}

	}

	public void Convert()
	{
		if (startNode)
		{
			if (Conv.State != ConversationState.Converted)
				convert();
			else
			{
				SpawnContextMenu(new Vector3(Screen.width / 2, Screen.height / 2));
				GameObject titleGO = contextMenu.transform.Find("TopPanel/Text").gameObject;
				titleGO.SetActive(true);
				titleGO.GetComponent<Text>().text = "Overwrite?";
				CreateButton("Yes", convert, GetContextMenuContent(), contextMenu, 0);
				CreateButton("No", () => { return; }, GetContextMenuContent(), contextMenu, 1);
			}
		}
		else
			uiManager.SpawnPopupMessage("No starting node!");
		ConversationConverter.Reset();
	}
	private void convert()
	{
		if (errorNode)
		{
			SetNodeColor(errorNode, GetNodeColor(errorNode.Type));
			errorNode = null;
		}
		//Always catching now!
		try
		{
			ConversationConverter.Convert(Conv, startNode, nodeList, GlobalVariables, npcName);
			DataWriter.SaveStringToTxt(Conv.DumpConversation(), "conv_" + Conv.Slot + ".txt");
		}
		catch (Exception e)
		{
			uiManager.SpawnPopupMessage("Error\n" + e.Message);
			ConverterException ce = (ConverterException)e;
			if(ce && ce.Node)
			{
				errorNode = ce.Node;
				centerOnNode(errorNode);
				SetNodeColor(ce.Node, ErrorColor);
			}
		}
	}

	public void FindStart()
	{
		if (!startNode)
			uiManager.SpawnPopupMessage("No starting node defined");
		centerOnNode(startNode);
	}
	public void FindNode()
	{
		Action<string> findAct = (str) =>
		{		
			foreach (var node in nodeList)
			{
				if(node.NodeName == str)
				{
					centerOnNode(node);
					return;
				}
			}
			uiManager.SpawnPopupMessage(string.Format("No node with name \"{0}\" found", str));
		};
		GameObject findGO = Instantiate(FindNodePrefab, uiManager.transform);
		InputField find = findGO.GetComponentInChildren<InputField>();
		find.onEndEdit.AddListener((str) => findAct(str));
		find.onEndEdit.AddListener((str) => Destroy(findGO));
	}

	private void centerOnNode(ConversationNode node)
	{
		RectTransform crt = Content.GetComponent<RectTransform>();
		RectTransform nrt = node.GetComponent<RectTransform>();
		crt.localPosition = -nrt.localPosition;
	}
	public void MoveContent(Vector3 vec)
	{
		RectTransform crt = Content.GetComponent<RectTransform>();
		crt.localPosition -= (vec * moveContentFactor);
	}

	public void GlobalVariableList()
	{
		int count = 0;
		if (!string.IsNullOrEmpty(PrivateCountField.text))
			count = int.Parse(PrivateCountField.text);
		//Debug.LogFormat("var list, count : {0}, priv.globs count : {1}", count, GlobalVariables.Count);
		string title = "List of conversation variables";
		string descr = "These variables are stored after conversation ends.";
		uiManager.CreateVariableList(count, GlobalVariables, title, descr);
	}
	public void LocalVariableList()
	{
		string title = "List of local variables";
		string descr = "These variables are NOT stored after conversation ends.";
		uiManager.CreateVariableList(LocalVariables.Count, LocalVariables, title, descr, true);
	}

	private ConversationStructure CreateStructure()
	{
		ConversationStructure str = new ConversationStructure();
		str.Nodes = new List<ConversationNodeStructure>();
		str.Connections = new List<ConversationConnectionStructure>();
		str.GlobalVariables = GlobalVariables;
		str.LocalVariables = LocalVariables;
		str.PrivateGlobalCount = int.Parse(PrivateCountField.text) + 32;
		str.NPCName = npcName;
		str.BarterStrings = Conv.BarterStrings;
		str.Likes = Conv.Likes;
		str.Dislikes = Conv.Dislikes;

		Conv.MemorySlots = str.PrivateGlobalCount;  //?????

		//MapCreator.StringData.SetNPCName(Conv.Slot, str.NPCName);
		Dictionary<GameObject, ConversationNodeStructure> nodeDict = new Dictionary<GameObject, ConversationNodeStructure>();
		Dictionary<GameObject, ConversationConditionStructure> condDict = new Dictionary<GameObject, ConversationConditionStructure>();
		for (int i = 4; i < Content.transform.childCount; i++)
		{
			GameObject convGO = Content.transform.GetChild(i).gameObject;

			if (convGO.name == "Node")
			{
				GameObject nodeGO = convGO;
				RectTransform rt = nodeGO.GetComponent<RectTransform>();
				ConversationNodeStructure cns = new ConversationNodeStructure();
				cns.Conditions = new List<ConversationConditionStructure>();
				cns.Position = rt.position;
				//cns.SizeDelta = rt.sizeDelta;
				nodeDict[nodeGO] = cns;
				ConversationNode node = nodeGO.GetComponent<ConversationNode>();
				cns.Content = node.NodeContent.text;
				RectTransform inputRt = node.NodeContent.gameObject.GetComponent<RectTransform>();
				cns.InputXSize = inputRt.sizeDelta.x;
				cns.Type = node.Type;
				cns.NodeName = node.NodeName;
				for (int j = 0; j < node.ConditionsGO.transform.childCount; j++)
				{
					GameObject conditionGO = node.ConditionsGO.transform.GetChild(j).gameObject;
					ConversationCondition condition = conditionGO.GetComponent<ConversationCondition>();
					ConversationConditionStructure ccs = new ConversationConditionStructure();
					ccs.Content = condition.Content.text;
					ccs.Active = condition.Content.gameObject.activeSelf;
					condDict[conditionGO] = ccs;
					cns.Conditions.Add(ccs);
				}
				str.Nodes.Add(cns);
			}
		}
		for (int i = 4; i < Content.transform.childCount; i++)
		{
			GameObject convGO = Content.transform.GetChild(i).gameObject;
			if (convGO.name == "Connection")
			{
				GameObject connGO = convGO;
				ConversationConnection conn = connGO.GetComponent<ConversationConnection>();
				GameObject inputNode = conn.InputGO.transform.parent.parent.gameObject;
				GameObject outputCond = conn.OutputGO.transform.parent.gameObject;
				ConversationConnectionStructure ccs = new ConversationConnectionStructure();
				ccs.Input = nodeDict[inputNode];
				ccs.Output = condDict[outputCond];
				str.Connections.Add(ccs);
			}
		}
		return str;
	}

	public void CreateNewConnection(GameObject target)
	{
		if (currentLine)
		{
			ConversationConnection currentCc = currentLine.GetComponent<ConversationConnection>();
			if (currentCc.InputGO == null && target.name == "O")
				Destroy(currentLine);
			else if (currentCc.OutputGO == null && target.name == "I")
				Destroy(currentLine);
			else if (currentCc.IsNodeParent(ConversationConnection.GetNode(target).transform))
				Destroy(currentLine);
			else if (currentCc.InputGO == null && target.name == "I")
			{
				currentCc.InputGO = target;
				AddConnection(currentCc);
				currentLine = null;
			}
			else if (currentCc.OutputGO == null && target.name == "O")
			{
				currentCc.OutputGO = target;
				AddConnection(currentCc);
				currentLine = null;
			}
		}
		else
		{
			currentLine = CreateNewConnection();
			ConversationConnection cc = currentLine.GetComponent<ConversationConnection>();
			if (target.name == "O")
				cc.OutputGO = target;
			else if (target.name == "I")
				cc.InputGO = target;
		}
	}


	private string GenerateNewNodeName(ConversationNode node)
	{
		string oldName = "Node " + nodeList.Count;
		string newName = ChangeNodeName(oldName, node);
		return newName;
	}

	public string ChangeNodeName(string oldName, ConversationNode thisNode)
	{
		string newName = oldName;
		int i = 0;
		while (CheckForDuplicateName(newName, thisNode))
		{
			i++;
			newName = oldName + "(" + i + ")";
		}
		return newName;
	}

	private bool CheckForDuplicateName(string newName, ConversationNode thisNode)
	{
		foreach (var node in nodeList)
		{
			if (node == thisNode)
				continue;
			if (node.NodeName == newName)
			{
				return true;
			}
		}
		return false;
	}

	public GameObject CreateNewConnection()
	{
		GameObject connGO = new GameObject("Connection");
		connGO.transform.SetParent(Content.transform);
		UILineRenderer lr = connGO.AddComponent<UILineRenderer>();
		float r = UnityEngine.Random.Range(0.5f, 1.0f);
		float g = UnityEngine.Random.Range(0.5f, 1.0f);
		float b = UnityEngine.Random.Range(0.5f, 1.0f);

		lr.color = new Color(r, g, b);
		lr.lineThickness = 5;
		lr.SetVerticesDirty();
		ConversationConnection cc = connGO.AddComponent<ConversationConnection>();
		cc.Line = lr;
		connGO.AddComponent<Shadow>();
		connGO.transform.SetSiblingIndex(4);
		return connGO;
	}

	public GameObject AddConnection(GameObject outputCond, GameObject inputNode)
	{
		GameObject connGO = CreateNewConnection();
		ConversationConnection conn = connGO.GetComponent<ConversationConnection>();
		ConversationCondition cond = outputCond.GetComponent<ConversationCondition>();
		ConversationNode node = inputNode.GetComponent<ConversationNode>();
		conn.OutputGO = cond.Output;
		conn.InputGO = node.InputGO;
		cond.OutputConnection = connGO;
		node.InputConnections.Add(connGO);
		return connGO;
	}

	public void AddConnection(ConversationConnection cc)
	{
		Transform inputNodeT = cc.InputGO.transform.parent.parent;
		ConversationNode inputNode = inputNodeT.GetComponent<ConversationNode>();
		inputNode.InputConnections.Add(cc.gameObject);

		Transform conditionT = cc.OutputGO.transform.parent;
		ConversationCondition condition = conditionT.GetComponent<ConversationCondition>();
		if (condition.OutputConnection)
			Destroy(condition.OutputConnection);
		condition.OutputConnection = cc.gameObject;
	}

	public void ClearCurrentConnection()
	{
		if (currentLine)
			Destroy(currentLine);
	}

	public GameObject AddNode(Vector3 pos, string name = "")
	{
		GameObject nodeGO = Instantiate(NodePrefab, Content.transform);
		RectTransform nodeRT = nodeGO.GetComponent<RectTransform>();
		nodeRT.position = pos;
		//nodeRT.sizeDelta = size;

		nodeGO.GetComponentInChildren<Image>().color = NodeColor;
		ConversationNode node = nodeGO.GetComponent<ConversationNode>();
		node.InputConnections = new List<GameObject>();
		nodeList.Add(node);
		nodeDict[node] = nodeGO;
		if (string.IsNullOrEmpty(name))
			SetNodeName(node, GenerateNewNodeName(node));
		else
			SetNodeName(node, name);
		SetNodeColor(node, NodeColor);
		return nodeGO;
	}

	private void SetNodeName(ConversationNode node, string newName)
	{
		node.NodeName = newName;
		node.NodeNameField.text = newName;
		nodeDict[node].name = "Node";
	}

	public void AddNode(BaseEventData eventData)
	{
		PointerEventData pointerEvent = (PointerEventData)eventData;
		if (pointerEvent != null && pointerEvent.button == PointerEventData.InputButton.Right)
		{
			GameObject nodeGO = Instantiate(NodePrefab, Content.transform);
			nodeGO.transform.position = new Vector3(pointerEvent.position.x, pointerEvent.position.y);
			nodeGO.GetComponentInChildren<Image>().color = NodeColor;
			ConversationNode node = nodeGO.GetComponent<ConversationNode>();
			node.InputConnections = new List<GameObject>();
			nodeList.Add(node);
			nodeDict[node] = nodeGO;
			SetNodeName(node, GenerateNewNodeName(node));
			SetNodeColor(node, NodeColor);
		}
	}

	public void SetStartNode(ConversationNode node)
	{
		if (StartNode)
		{
			StartNode.GetComponentInChildren<Text>().text = "Node";
			ConversationNode oldStart = StartNode.GetComponent<ConversationNode>();
			oldStart.Type = NodeType.Node;
			GameObject input = StartNode.transform.Find("Input").gameObject;
			input.SetActive(true);
			SetNodeColor(oldStart, NodeColor);
		}
		StartNode = node.gameObject;
		StartNode.GetComponentInChildren<Text>().text = "Start";
		ConversationNode newNode = StartNode.GetComponent<ConversationNode>();
		newNode.Type = NodeType.Start;
		foreach (var inputConn in newNode.InputConnections)
			Destroy(inputConn);
		StartNode.transform.Find("Input").gameObject.SetActive(false);
		SetNodeColor(newNode, StartColor);
		startNode = newNode;
	}
	public void SetNodeColor(ConversationNode node, Color baseColor)
	{
		Color mainPanelColor = baseColor + new Color(0.1f, 0.1f, 0.1f);
		Color handleColor = baseColor + new Color(0.3f, 0.3f, 0.3f);
		Color nodeColor = baseColor - new Color(0, 0, 0, 0.5f);
		node.gameObject.GetComponent<Image>().color = nodeColor;
		
		node.MainPanelImage.color = mainPanelColor;
		node.HandleImage.color = handleColor;
	}
	private Color GetNodeColor(NodeType type)
	{
		switch (type)
		{
			case NodeType.Node:
				return NodeColor;
			case NodeType.Start:
				return StartColor;
			case NodeType.Barter:
				return NodeColor;
			default:
				return NodeColor;
		}
	}

	private GameObject CreateCondition(ConditionType cType, Transform parent)
	{
		GameObject condGO = Instantiate(ConditionPrefab, parent);
		ConversationCondition cond = condGO.GetComponent<ConversationCondition>();

		return condGO;
	}

	public GameObject SpawnContextMenu(BaseEventData eventData)
	{
		PointerEventData pointerEvent = (PointerEventData)eventData;
		if (pointerEvent != null && pointerEvent.button == PointerEventData.InputButton.Right)
			SpawnContextMenu(pointerEvent.position);
		return contextMenu;
	}
	public GameObject SpawnContextMenu(Vector3 pos)
	{
		if (contextMenu)
			Destroy(contextMenu);
		contextMenu = Instantiate(ConversationEditorContextMenuPrefab, transform.parent);
		contextMenu.name = "ContextMenu";
		contextMenu.transform.position = new Vector3(pos.x, pos.y);
		contextMenu.transform.Find("TopPanel/Close").gameObject.GetComponent<Button>().onClick.AddListener(() => Destroy(contextMenu));
		return contextMenu;
	}

	public Transform GetContextMenuContent()
	{
		if (contextMenu)
			return contextMenu.transform.Find("TopPanel/Main");
		else
			return null;
	}

	public void PointerUp(BaseEventData eventData)
	{
		PointerEventData pointerEvent = (PointerEventData)eventData;
		if (pointerEvent != null)
		{
			if (pointerEvent.button == PointerEventData.InputButton.Right)
			{
				if (contextMenu)
					Destroy(contextMenu);
				SpawnContextMenu(eventData);
				CreateButton("Add node", () => AddNode(eventData), GetContextMenuContent(), contextMenu, 0);
			}
			else if (pointerEvent.button == PointerEventData.InputButton.Left)
			{
				ClearCurrentConnection();
			}
		}
	}

	public void RemoveNode(ConversationNode toRemove)
	{
		nodeList.Remove(toRemove);
		nodeDict.Remove(toRemove);
	}

	public GameObject CreateButton(string message, Action onClick, Transform parent, GameObject menu, int id)
	{
		GameObject butGO = Instantiate(ButtonPrefab, parent);
		butGO.name = "Button_" + id;
		butGO.GetComponentInChildren<Text>().text = message;
		Button but = butGO.GetComponent<Button>();
		but.onClick.AddListener(() => onClick());
		but.onClick.AddListener(() => Destroy(menu));
		return butGO;
	}

	public void CreateBarterStringsList()
	{
		uiManager.CreateBarterStringsList(Conv);
	}

	public void CreateLikedList()
	{
		uiManager.CreateConversationItemList(Conv.Likes, "Liked items", "Items / categories preferred in trade.");
	}
	public void CreateDislikedList()
	{
		uiManager.CreateConversationItemList(Conv.Dislikes, "Disliked items", "Items / categories less effective in trade.");
	}
	public void DebugMessage(string message)
	{
		Debug.Log(message);
	}
}
