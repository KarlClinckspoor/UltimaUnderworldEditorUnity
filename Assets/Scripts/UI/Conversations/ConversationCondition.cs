using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum ConditionType
{
	None,
	If,
	ElseIf,
	Else
}
[System.Serializable]
public class ConversationConditionStructure
{
	public ConversationConnectionStructure OutputConnection;
	public string Content;
	public bool Active;
}

public class ConversationCondition : MonoBehaviour {

	public InputField Content;
	public GameObject Output;
	public GameObject OutputConnection;

	//public ConditionType ConditionType;
	//public ConversationNode TargetNode;

	private UIManager uiManager;
	private bool isInit;

	private void Start()
	{
		if (!isInit)
			Init();
	}

	public void Init(bool deactivate = false)
	{
		uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
		StartCoroutine(LateStart(deactivate));
		isInit = true;
	}

	public ConversationNode GetTarget()
	{
		if (OutputConnection)
		{
			ConversationConnection connection = OutputConnection.GetComponent<ConversationConnection>();
			ConversationNode next = connection.GetInputNode();
			return next;
		}
		return null;
	}

	public void ConditionPopup(BaseEventData baseEvent)
	{
		PointerEventData pointerEvent = (PointerEventData)baseEvent;
		if (pointerEvent != null)
		{
			if (pointerEvent.button == PointerEventData.InputButton.Right)
			{
				GameObject convEdGO = uiManager.ConversationEditorGO;
				ConversationEditor convEd = convEdGO.GetComponent<ConversationEditor>();
				GameObject menu = convEd.SpawnContextMenu(baseEvent);
				if (!Content.gameObject.activeSelf)
					convEd.CreateButton("Add if statement", () => EnableCondition(), convEd.GetContextMenuContent(), menu, 0);
				if (Content.gameObject.activeSelf)
					convEd.CreateButton("Remove if statement", () => DisableCondition(), convEd.GetContextMenuContent(), menu, 1);
				if (OutputConnection)
					convEd.CreateButton("Remove connection", () => Destroy(OutputConnection), convEd.GetContextMenuContent(), menu, 2);
				convEd.CreateButton("Remove condition", () => Remove(), convEd.GetContextMenuContent(), menu, 3);
			}
			else if(pointerEvent.button == PointerEventData.InputButton.Left)
			{
				GameObject convEdGO = uiManager.ConversationEditorGO;
				ConversationEditor convEd = convEdGO.GetComponent<ConversationEditor>();
				if (OutputConnection)
					Destroy(OutputConnection);
				convEd.CreateNewConnection(Output);
			}
		}
	}

	public void Remove()
	{
		Destroy(OutputConnection);
		if (transform.parent.childCount == 1)
			transform.parent.parent.gameObject.SetActive(false);
		Destroy(gameObject);
	}

	public void EnableCondition()
	{
		Content.gameObject.SetActive(true);
	}
	public void DisableCondition()
	{
		Content.text = "";
		Content.gameObject.SetActive(false);
	}

	private IEnumerator LateStart(bool deactivate = false)
	{
		yield return new WaitForSeconds(0.1f);
		RectTransform inputCaretRt = Content.transform.Find("InputField Input Caret").GetComponent<RectTransform>();
		inputCaretRt.anchorMax = Vector2.one;
		inputCaretRt.anchorMin = Vector2.zero;
		inputCaretRt.sizeDelta = Vector2.zero;
		inputCaretRt.anchoredPosition = Vector2.zero;
		if (deactivate)
			Content.gameObject.SetActive(false);
	}
}
