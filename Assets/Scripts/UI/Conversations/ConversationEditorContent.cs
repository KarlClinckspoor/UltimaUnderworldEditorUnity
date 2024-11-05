using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ConversationEditorContent : MonoBehaviour
{
	public RectTransform RT_A;
	public RectTransform RT_B;
	public RectTransform RT_C;
	public RectTransform RT_D;
	public RectTransform RT;

	private RectTransform parentRt;
	private float width;
	private float height;

	private void Start()
	{
		parentRt = transform.parent.gameObject.GetComponent<RectTransform>();
		width = parentRt.rect.width;
		height = parentRt.rect.height;
	}
	private void Update()
	{
		//Debug.LogFormat("RT : {0}", RT.localPosition);

		float newWidth = parentRt.rect.width;
		float newHeight = parentRt.rect.height;
		if (newWidth != width)
			SetWidth(newWidth);
		if (newHeight != height)
			SetHeight(newHeight);

		float x = RT.anchoredPosition.x;
		float y = RT.anchoredPosition.y;

		if (x < -width)
			x -= width;
		if (y < -height)
			y -= height;
		int stepX = (int)((x + width) / -width);
		int stepY = (int)((y + height) / -height);

		RT_A.localPosition = new Vector2(0 + (stepX * width), 0 + (stepY * height));
		RT_B.localPosition = new Vector2(width + (stepX * width), 0 + (stepY * height));
		RT_C.localPosition = new Vector2(0 + (stepX * width), height + (stepY * height));
		RT_D.localPosition = new Vector2(width + (stepX * width), height + (stepY * height));
	}

	private void SetWidth(float newWidth)
	{
		width = newWidth;
		UpdateBackground();
	}
	private void SetHeight(float newHeight)
	{
		height = newHeight;
		UpdateBackground();
	}

	private void UpdateBackground()
	{
		RT_A.sizeDelta = new Vector2(width, height);
		RT_B.sizeDelta = new Vector2(width, height);
		RT_C.sizeDelta = new Vector2(width, height);
		RT_D.sizeDelta = new Vector2(width, height);
	}
}
