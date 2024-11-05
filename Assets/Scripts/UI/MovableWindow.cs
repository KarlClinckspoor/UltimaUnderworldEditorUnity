using UnityEngine;
using UnityEngine.EventSystems;

public enum ResizingType
{
	None,
	Top,
	Bottom,
	Right,
	Left
}

public class MovableWindow : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
	protected UIManager uiManager;
	protected Vector2 dragOffset;

	protected RectTransform rt;

	protected bool isDragging;


	protected static float borderLength = 8.0f;

	protected virtual void Start()
	{
		uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
		rt = GetComponent<RectTransform>();
	}

	protected virtual void Update()
	{

	}

	public virtual void Close()
	{
		Destroy(gameObject);
	}

	private void MoveWindowInit(PointerEventData eventData)
	{
		Vector2 pos = new Vector2(transform.position.x, transform.position.y);
		dragOffset = pos - eventData.position;
	}

	public virtual void OnBeginDrag(PointerEventData eventData)
	{
		transform.SetAsLastSibling();
		isDragging = true;
	}

	public virtual void OnDrag(PointerEventData eventData)
	{
		transform.position = eventData.position + dragOffset;
	}

	public virtual void OnEndDrag(PointerEventData eventData)
	{
		dragOffset = Vector2.zero;
		isDragging = false;
	}

	public virtual void OnInitializePotentialDrag(PointerEventData eventData)
	{
		MoveWindowInit(eventData);
	}

}
