using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemButton : MonoBehaviour {

	public string Tooltip;
	public Image MainSprite;
	public Image AdditionalSprite;
	private Coroutine tooltipRoutine;
	private UIManager uiManager;

	private void Start()
	{
		uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
	}

	public void PointerEnter(BaseEventData baseEvent)
	{
		tooltipRoutine = StartCoroutine(showTooltip());
	}

	public void PointerExit(BaseEventData baseEvent)
	{
		StopCoroutine(tooltipRoutine);
	}

	private IEnumerator showTooltip()
	{
		yield return new WaitForSeconds(0.5f);
		if (!string.IsNullOrEmpty(Tooltip))
			uiManager.SpawnTooltip(Tooltip);
	}
}
