using UnityEngine;
using UnityEngine.UI;

public class ConversationWindow : MovableWindow {

	public GameObject TextPanel;
	public GameObject TextContent;
	public GameObject ResponsePanel;

	public GameObject DebugWindow;
	public GameObject Popup;
	public Button NextOperation;

	public GameObject TextPrefab;
	public GameObject ResponsePrefab;
	public GameObject ConversationPopupPrefab;

	public ConversationManager ConvManager;


	public void ProcessConversation()
	{
		ConvManager.ProcessCode();
	}

	public override void Close()
	{
		if (DebugWindow)
			Destroy(DebugWindow);
		if (Popup)
			Destroy(Popup);
		Destroy(gameObject);
	}
}
