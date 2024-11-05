using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum NodeType
{
	Node,
	Start,
	Barter
}
[System.Serializable]
public class ConversationNodeStructure
{
	public NodeType Type;
	public SerializableVector3 Position;
	public float InputXSize;
	public List<ConversationConditionStructure> Conditions;
	public List<ConversationConnectionStructure> InputConnections;
	public string NodeName;

	public string Content;
}

public class ConversationNode : MonoBehaviour {

	public NodeType Type;
	public InputField NodeContent;
	public GameObject ConditionsGO;

	public GameObject InputGO;
	public List<GameObject> InputConnections;
	public string NodeName;
	public InputField NodeNameField;
	public Image MainPanelImage;
	public Image HandleImage;
	
	private RectTransform rt;
	private Vector2 dragOffset;
	private UIManager uiManager;
	private ConversationEditor conversationEditor;

	private bool isInit;

	private void Start()
	{
		if(!isInit)
			Init();
	}

	public void Init()
	{
		rt = GetComponent<RectTransform>();
		uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
		GameObject convEdGO = uiManager.ConversationEditorGO;
		conversationEditor = convEdGO.GetComponent<ConversationEditor>();
		StartCoroutine(LateStart());
		isInit = true;
	}

	private IEnumerator LateStart()
	{
		yield return new WaitForSeconds(0.1f);
		//if (Type != NodeType.Start)
		//{
			RectTransform inputCaretRt = NodeContent.transform.Find("InputField Input Caret").GetComponent<RectTransform>();
			inputCaretRt.anchorMax = Vector2.one;
			inputCaretRt.anchorMin = Vector2.zero;
			inputCaretRt.sizeDelta = Vector2.zero;
			inputCaretRt.anchoredPosition = Vector2.zero;
		//}
	}

	public GameObject AddCondition()
	{
		if (!ConditionsGO.transform.parent.gameObject.activeSelf)
			ConditionsGO.transform.parent.gameObject.SetActive(true);
		return Instantiate(conversationEditor.ConditionPrefab, ConditionsGO.transform);
	}

	public GameObject AddCondition(string str)
	{
		if(!ConditionsGO.transform.parent.gameObject.activeSelf)
			ConditionsGO.transform.parent.gameObject.SetActive(true);
		GameObject condGO = Instantiate(conversationEditor.ConditionPrefab, ConditionsGO.transform);
		ConversationCondition cond = condGO.GetComponent<ConversationCondition>();
		cond.Content.text = str;
		return condGO;
	}

	public void SetNodeName(InputField input)
	{
		string oldName = input.text;
		string newName = conversationEditor.ChangeNodeName(oldName, this);
		NodeName = newName;
		NodeNameField.text = newName;
	}

	public void PointerUp(BaseEventData baseEvent)
	{
		PointerEventData pointerEvent = (PointerEventData)baseEvent;
		if (pointerEvent != null)
		{
			if (pointerEvent.button == PointerEventData.InputButton.Right)
			{
				GameObject menu = conversationEditor.SpawnContextMenu(baseEvent);
				if (Type != NodeType.Start)
					conversationEditor.CreateButton("Set as conversation start", () => conversationEditor.SetStartNode(this), conversationEditor.GetContextMenuContent(), menu, 0);
				conversationEditor.CreateButton("Add condition", () => AddCondition(), conversationEditor.GetContextMenuContent(), menu, 1);
				conversationEditor.CreateButton("Remove node", () => Remove(), conversationEditor.GetContextMenuContent(), menu, 2);

			}
			conversationEditor.ClearCurrentConnection();
		}
	}
	public void Remove()
	{
		foreach (var con in InputConnections)
		{
			Destroy(con);
		}
		for (int i = 0; i < ConditionsGO.transform.childCount; i++)
		{
			GameObject condition = ConditionsGO.transform.GetChild(i).gameObject;
			ConversationCondition cond = condition.GetComponent<ConversationCondition>();
			Destroy(cond.OutputConnection);
		}
		conversationEditor.RemoveNode(this);
		Destroy(gameObject);
	}

	public void InputPointerUp(BaseEventData baseEvent)
	{
		PointerEventData pointerEvent = (PointerEventData)baseEvent;
		if (pointerEvent != null)
		{
			if (pointerEvent.button == PointerEventData.InputButton.Left)
			{
				conversationEditor.CreateNewConnection(InputGO);
			}
		}
	}

	public void StartDrag(BaseEventData eventData)
	{
		PointerEventData pointerEvent = (PointerEventData)eventData;
		if(pointerEvent != null)
		{
			Vector2 pos = new Vector2(transform.localPosition.x, transform.localPosition.y);
			dragOffset = pos - pointerEvent.position;
		}
	}

	public virtual void Drag(BaseEventData eventData)
	{
		PointerEventData pointerEvent = (PointerEventData)eventData;
		if (pointerEvent != null)
			transform.localPosition = pointerEvent.position + dragOffset;
	}

	public virtual void EndDrag(BaseEventData eventData)
	{
		PointerEventData pointerEvent = (PointerEventData)eventData;
		if (pointerEvent != null)
			dragOffset = new Vector2(0, 0);
	}

	public override string ToString()
	{
		return NodeName + ", type " + Type;
	}
}
