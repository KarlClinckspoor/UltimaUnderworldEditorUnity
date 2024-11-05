using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ResizableWindow : MovableWindow, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	public bool IsMovable;
	public bool HorizontalOn;
	public bool VerticalOn;

	public float MinSizeX;
	public float MinSizeY;

	protected Vector2 resizeOffset;
	protected GameObject hoveredOver;

	protected bool isResizing;

	protected ResizingType currentResizing;

	protected override void Start()
	{
		base.Start();
	}

	public override void OnBeginDrag(PointerEventData eventData)
	{
		if (currentResizing == ResizingType.None)
			if(IsMovable)
				base.OnBeginDrag(eventData);
	}

	public override void OnDrag(PointerEventData eventData)
	{
		if (currentResizing == ResizingType.None)
		{
			if (IsMovable)
				base.OnDrag(eventData);
		}
		else if(isResizing)
			ResizeWindow(eventData, currentResizing);
	}

	public override void OnEndDrag(PointerEventData eventData)
	{
		resizeOffset = Vector2.zero;
		isResizing = false;
		base.OnEndDrag(eventData);
	}


	public override void OnInitializePotentialDrag(PointerEventData eventData)
	{
		if (currentResizing == ResizingType.None)
		{
			if (IsMovable)
				base.OnInitializePotentialDrag(eventData);
		}
		else
			ResizeWindowInit(eventData);
	}

	//Should use OnMouseOver() from Monobehaviour
	public void OnPointerEnter(PointerEventData eventData)
	{
		if (eventData.pointerCurrentRaycast.gameObject != null)
		{
			GameObject current = eventData.pointerCurrentRaycast.gameObject;
			MovableWindow mw = current.GetComponent<MovableWindow>();
			while (!mw && current)
			{
				current = current.transform.parent.gameObject;
				mw = current.GetComponent<MovableWindow>();
			}
			if (mw)
			{
				hoveredOver = current;
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!isResizing)
		{
			currentResizing = ResizingType.None;
			uiManager.SetCursorMode(CursorType.Normal);
			hoveredOver = null;
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (isResizing)
		{
			isResizing = false;
			uiManager.SetCursorMode(CursorType.Normal);
		}
	}
	public override void Close()
	{
		uiManager.SetCursorMode(CursorType.Normal);
		base.Close();
	}

	protected override void Update()
	{
		//Debug.LogFormat("anchored : {0}, sizedelta : {1}, offsetmax : {2}, offsetmin : {3}", rt.anchoredPosition, rt.sizeDelta, rt.offsetMax, rt.offsetMin);
		//Debug.LogFormat("{0} {1} {2} {3}", rt.anchoredPosition, rt.offsetMax, rt.offsetMin, rt.sizeDelta);

		if (hoveredOver && !isDragging && !isResizing)
		{
			float top = transform.position.y + rt.sizeDelta.y / 2;
			float bottom = transform.position.y - rt.sizeDelta.y / 2;
			float right = transform.position.x + rt.sizeDelta.x / 2;
			float left = transform.position.x - rt.sizeDelta.x / 2;
			if (Input.mousePosition.y < top && Input.mousePosition.y > top - borderLength && VerticalOn)
			{
				currentResizing = ResizingType.Top;
				uiManager.SetCursorMode(CursorType.ArrowY);
			}
			else if (Input.mousePosition.y > bottom && Input.mousePosition.y < bottom + borderLength && VerticalOn)
			{
				currentResizing = ResizingType.Bottom;
				uiManager.SetCursorMode(CursorType.ArrowY);
			}
			else if (Input.mousePosition.x < right && Input.mousePosition.x > right - borderLength && HorizontalOn)
			{
				currentResizing = ResizingType.Right;
				uiManager.SetCursorMode(CursorType.ArrowX);
			}
			else if (Input.mousePosition.x > left && Input.mousePosition.x < left + borderLength && HorizontalOn)
			{
				currentResizing = ResizingType.Left;
				uiManager.SetCursorMode(CursorType.ArrowX);
			}
			else
			{
				currentResizing = ResizingType.None;
				uiManager.SetCursorMode(CursorType.Normal);
			}
		}
	}


	private void ResizeWindowInit(PointerEventData eventData)
	{
		resizeOffset = eventData.position;
		isResizing = true;
	}



	private void ResizeWindow(PointerEventData eventData, ResizingType type)
	{
		if (type == ResizingType.Top)
		{			
			Vector2 newOffsetMax = new Vector2(rt.offsetMax.x, rt.offsetMax.y + (eventData.position.y - resizeOffset.y));
			if(newOffsetMax.y - rt.offsetMin.y > MinSizeY)
				rt.offsetMax = newOffsetMax;
		}
		else if (type == ResizingType.Bottom)
		{
			Vector2 newOffsetMin = new Vector2(rt.offsetMin.x, rt.offsetMin.y + (eventData.position.y - resizeOffset.y));
			if(rt.offsetMax.y - newOffsetMin.y > MinSizeY)
				rt.offsetMin = newOffsetMin;
		}
		else if (type == ResizingType.Right)
		{
			Vector2 newOffsetMax = new Vector2(rt.offsetMax.x + (eventData.position.x - resizeOffset.x), rt.offsetMax.y);
			if(newOffsetMax.x - rt.offsetMin.x > MinSizeX)
				rt.offsetMax = newOffsetMax;
		}
		else if (type == ResizingType.Left)
		{
			Vector2 newOffsetMin = new Vector2(rt.offsetMin.x + (eventData.position.x - resizeOffset.x), rt.offsetMin.y);
			if(rt.offsetMax.x - newOffsetMin.x > MinSizeX)
				rt.offsetMin = newOffsetMin;
		}
		resizeOffset = eventData.position;

		//Debug.LogFormat("an : {0}, size : {1}", rt.anchoredPosition, rt.sizeDelta);
		Debug.LogFormat("anchored : {0}, sizedelta : {1}, offsetmax : {2}, offsetmin : {3}", rt.anchoredPosition, rt.sizeDelta, rt.offsetMax, rt.offsetMin);
	}

}
