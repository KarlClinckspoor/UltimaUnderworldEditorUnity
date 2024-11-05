using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTileScript : MonoBehaviour {

	public MapTile MapTile;

	public MapTile NTile;
	public MapTile STile;
	public MapTile ETile;
	public MapTile WTile;

	public GameObject FloorObject;
	public GameObject WallObject;

	public void SetOverlay(bool overlay)
	{
		if(FloorObject)		FloorObject.GetComponent<MeshRenderer>().material.SetFloat("ToggleOverlay", overlay ? 1.0f : 0);
		if(WallObject)		WallObject.GetComponent<MeshRenderer>().material.SetFloat("ToggleOverlay", overlay ? 1.0f : 0);
	}
	public void SetOverlayColor(Color col)
	{
		if (FloorObject) FloorObject.GetComponent<MeshRenderer>().material.SetColor("_ColorOverlay", col);
		if (WallObject) WallObject.GetComponent<MeshRenderer>().material.SetColor("_ColorOverlay", col);
	}
}
