using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraManager : MonoBehaviour {

	public int MinX;
	public int MaxX;
	public int MinY;
	public int MaxY;
	public int MinZ;
	public int MaxZ;

	public float MoveSpeed;
	public float ZoomSpeed;
	public float ConvEditSpeed;

	private float moveX;
	private float moveY;
	private float mouseZ;
	private bool onUI;

	private Vector3 target;

	private UIManager uiManager;

	public bool IsDragging { get; private set; }

	private void Start()
	{
		uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
	}

	private void Update()
	{
		if (uiManager.IsWindowActive())
		{
			//Debug.Log("Window active");
			return;
		}
		float dt = Time.deltaTime;
		if (uiManager.ConversationEditorGO != null)     //User is using conversation editor
		{
			//Debug.Log("ConvEdit active");
			ConversationEditor convEdit = uiManager.ConversationEditorGO.GetComponent<ConversationEditor>();
			GameObject selGO = EventSystem.current.currentSelectedGameObject;
			if (selGO && selGO.name == "InputField")
				return;				
			convEdit.MoveContent(new Vector3(Input.GetAxis("Horizontal") * ConvEditSpeed * dt, Input.GetAxis("Vertical") * ConvEditSpeed * dt));
			return;
		}
		GetKeyboardInput(dt);
		GetDepthInput(dt);
		onUI = EventSystem.current.IsPointerOverGameObject();
		
		MoveCamera();
		if(mouseZ != 0 && !onUI)
			ZoomCamera();
	}

	private void GetDepthInput(float dt)
	{
		mouseZ = Input.GetAxis("Depth") * ZoomSpeed * dt;
	}

	private void GetKeyboardInput(float dt)
	{
		if (!onUI)
		{
			moveX = Input.GetAxis("Horizontal") * MoveSpeed * dt;
			moveY = Input.GetAxis("Vertical") * MoveSpeed * dt;
		}
		else
		{
			moveX *= 0.9f;
			if (Mathf.Abs(moveX) < 0.001f)
				moveX = 0;
			moveY *= 0.9f;
			if (Mathf.Abs(moveY) < 0.001f)
				moveY = 0;
		}
	}

	private void MoveCamera()
	{
		//if (Input.GetMouseButton(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
		//{
		//	if (moveX != 0 || moveY != 0)
		//		IsDragging = true;
			float nx = Camera.main.transform.position.x + moveX;
			float ny = Camera.main.transform.position.y + moveY;
			
			nx = Mathf.Clamp(nx, MinX, MaxX);
			ny = Mathf.Clamp(ny, MinY, MaxY);

			Camera.main.transform.position = new Vector3(nx, ny, Camera.main.transform.position.z);
		//}
		//else if(Input.GetMouseButtonUp(0))
		//{
		//	IsDragging = false;
		//}
	}

	private void ZoomCamera()
	{
		float nz = Camera.main.transform.position.z + mouseZ;
		nz = Mathf.Clamp(nz, MinZ, MaxZ);
		Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, nz);
	}

	public Vector2 GetMousePosition()
	{
		Vector3 rawPos = Input.mousePosition;
		rawPos.z += 20;
		Vector3 mousePos = Camera.main.ScreenToWorldPoint(rawPos);
		return new Vector2(mousePos.x, mousePos.y);
	}
}
