using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: selection (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
	#region Selection

	public bool SelectObject(StaticObject so, bool centerCamera)
	{
		DeselectObjectAndSetProperties(so);
		if (centerCamera)
			SetCameraToObject(so);
		if (MapCreator.ObjectToGO.ContainsKey(so))
		{
			GameObject go = MapCreator.ObjectToGO[so];
			OnSelectObject(go);
			GameObject col = go.GetComponentInChildren<Collider>().gameObject;
			SetSpriteOutline(col, true, Color.green);
			return true;
		}
		else
			return false;
	}

	public void SetTileProperties(GameObject obj, bool activate)
	{
		//Debug.Log("Set tile properties");
		if (activate)
		{
			TilePropertiesObject.SetActive(true);
			TileProperties tp = TilePropertiesObject.GetComponent<TileProperties>();
			MapTileScript mts = obj.GetComponent<MapTileScript>();
			if (tp && mts)
			{
				tp.SetTile(mts.MapTile);
			}
		}
		else
		{
			TilePropertiesObject.SetActive(false);
		}
	}

	public void SetObjectProperties(GameObject obj, bool activate)
	{
		if (activate)
		{
			StaticObjectScript sos = obj.GetComponent<StaticObjectScript>();
			if (sos)
				SetObjectProperties(sos.StaticObject);
		}
		else
			ObjectProperties.SetActive(false);
	}
	public void DeselectObjectAndSetProperties(StaticObject so)
	{
		OnSelectNonGO();	//InputManager.SelectedObject = null
		SetObjectProperties(so);
	}
	public void SetObjectProperties(StaticObject so)
	{
		ObjectProperties.SetActive(true);
		PropertiesPanel pp = ObjectProperties.GetComponent<PropertiesPanel>();
		pp.SetObject(so);
		pp.SetBasicPanel(so);
		
	}

	public void SetTilePanel(GameObject obj, bool activate, GameObject sel)
	{
		if (TilePanelObject.activeSelf != activate)
			TilePanelObject.SetActive(activate);
		if (activate)
		{
			TilePanel tp = TilePanelObject.GetComponent<TilePanel>();
			MapTileScript mts = obj.transform.parent.GetComponent<MapTileScript>();
			if (tp && mts)
			{
				tp.SetTile(mts.MapTile, CurrentLevel);
				SetTileOutline(obj, true);
			}
		}
		else
		{
			if (obj.transform.parent && obj.transform.parent.gameObject == sel)
				SetTileOutline(obj, true, Color.green);
			else
				SetTileOutline(obj, false);
		}
	}

	public void SetTileOutline(GameObject obj, bool activate, Color? col = null)
	{
		MapTileScript mts = obj.transform.parent.GetComponent<MapTileScript>();
		if (mts)
		{
			MeshRenderer floor = mts.FloorObject.GetComponent<MeshRenderer>();
			MeshRenderer wall = null;
			if (mts.WallObject)
				wall = mts.WallObject.GetComponent<MeshRenderer>();
			if (activate)
			{
				if (floor)
				{
					floor.material.SetFloat("_OutlineSize", 4.0f);
					if (col != null)
						floor.material.SetColor("_OutlineColor", (Color)col);
					else
						floor.material.SetColor("_OutlineColor", Color.white);
				}
				if (wall)
				{
					wall.material.SetFloat("_OutlineSize", 4.0f);
					if (col != null)
						wall.material.SetColor("_OutlineColor", (Color)col);
					else
						wall.material.SetColor("_OutlineColor", Color.white);
				}
			}
			else
			{
				if (floor)
				{
					floor.material.SetFloat("_OutlineSize", 0);
				}
				if (wall)
				{
					wall.material.SetFloat("_OutlineSize", 0);
				}
			}
		}
	}
	public void SetStaticObjectPanel(GameObject obj, bool activate, GameObject sel)
	{
		StaticObjectPanelObject.SetActive(activate);
		StaticObjectPanel sop = StaticObjectPanelObject.GetComponent<StaticObjectPanel>();
		StaticObjectScript sos = obj.transform.parent.GetComponent<StaticObjectScript>();
		if (activate)
		{
			if (sop && sos)
			{
				sop.SetStaticObject(sos.StaticObject);
			}
			SetSpriteOutline(obj, true);
		}
		else
		{
			sop.ClearObjectList(sop.NextObjectChainPanel.transform);
			sop.ClearObjectList(sop.ContainerPanel.transform, true);
			//Debug.LogFormat("Moving out of object {0}, parent {1}", obj, obj.transform.parent);
			if (obj.transform.parent && obj.transform.parent.gameObject == sel)
			{
				SetSpriteOutline(obj, true, Color.green);
			}
			else
				SetSpriteOutline(obj, false);
		}
	}

	public void SetSpriteOutline(GameObject obj, bool activate, Color? col = null)
	{
		SpriteOutline outline = obj.transform.parent.GetComponent<SpriteOutline>();
		if (outline)
		{
			if (activate)
			{
				outline.OutlineObject.SetActive(true);

				if (col != null)
				{
					Material currentMat = outline.OutlineObject.GetComponent<SpriteRenderer>().material;
					currentMat.SetColor("_Color", (Color)col);
				}
				else
				{
					Material currentMat = outline.OutlineObject.GetComponent<SpriteRenderer>().material;
					currentMat.SetColor("_Color", Color.white);
				}
			}
			else
				outline.OutlineObject.SetActive(false);
		}
	}

	#endregion
}
