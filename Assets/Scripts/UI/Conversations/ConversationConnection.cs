using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

[System.Serializable]
public class ConversationConnectionStructure
{
	public ConversationConditionStructure Output;
	public ConversationNodeStructure Input;
}

public class ConversationConnection : MonoBehaviour {

	public GameObject InputGO;
	public GameObject OutputGO;
	public UILineRenderer Line;
	
	void Update ()
	{
		transform.position = Vector3.zero;
		Vector2[] points = new Vector2[2];
		if (OutputGO)
			points[0] = OutputGO.transform.position;
		else
			points[0] = Input.mousePosition;
		if (InputGO)
			points[1] = InputGO.transform.position;
		else
			points[1] = Input.mousePosition;
		Line.Points = points;
	}

	public bool IsNodeParent(Transform nodeT)
	{
		if(InputGO)
		{
			Transform parent = InputGO.transform.parent.parent;
			if (nodeT == parent)
				return true;
		}
		if(OutputGO)
		{
			Transform parent = OutputGO.transform.parent.parent.parent.parent;
			if (nodeT == parent)
				return true;
		}
		return false;
	}

	public GameObject GetInputGO()
	{
		return InputGO.transform.parent.parent.gameObject;
	}
	public GameObject GetOutputGO()
	{
		return OutputGO.transform.parent.parent.parent.parent.gameObject;
	}
	public ConversationNode GetInputNode()
	{
		return GetInputGO().GetComponent<ConversationNode>();
	}
	public ConversationNode GetOutputNode()
	{
		return GetOutputGO().GetComponent<ConversationNode>();
	}

	public static GameObject GetNode(GameObject slot)
	{
		if (slot.name == "I")
			return slot.transform.parent.parent.gameObject;
		else if (slot.name == "O")
			return slot.transform.parent.parent.parent.parent.gameObject;
		return null;
	}
}
