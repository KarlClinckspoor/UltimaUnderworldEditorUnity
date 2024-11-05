using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConversationPopup : MovableWindow {

	public RectTransform Window;
	public Text Title;
	public Text Question;
	public Button Yes;
	public Button No;
	public InputField Answer;
	public GameObject CloseButton;
	public GameObject GridPanel;
	public GameObject InputFieldPrefab;

}
