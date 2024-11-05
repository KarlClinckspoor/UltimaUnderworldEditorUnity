using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TilePanel : MonoBehaviour {

	public Text Position;
	public Text Type;
	public Text Height;
	public Text Antimagic;
	public Text Door;
	public Text Adress;
	public Text FileAdr;

	public Text FloorText;
	public Image Floor;
	public Text WallText;
	public Image Wall;

	public GameObject ObjectList;

	public void SetTile(MapTile tile, int level)
	{
		Position.text = "[X : " + tile.Position.x + ", Y : " + tile.Position.y + "]";
		Type.text = "Type : " + tile.TileType;
		Height.text = "Height : " + tile.FloorHeight;
		Antimagic.text = "Antimagic : " + tile.IsAntimagic;
		Door.text = "Door : " + tile.IsDoor;
		Adress.text = "Object : " + tile.ObjectAdress;
		FileAdr.text = "File : " + DataReader.ToHex(tile.FileAdress);

		FloorText.text = "Floor: " + tile.FloorTexture.ToString() + "/" + MapCreator.LevelData[level - 1].FloorTextures[tile.FloorTexture].ToString();
		Floor.sprite = CreateTextureSprite(MapCreator.GetFloorTextureFromIndex(tile.FloorTexture, level));
		WallText.text = "Wall: " + tile.WallTexture.ToString() + "/" + MapCreator.LevelData[level - 1].WallTextures[tile.WallTexture].ToString();
		Wall.sprite = CreateTextureSprite(MapCreator.GetWallTextureFromIndex(tile.WallTexture, level));

		ClearObjectList(ObjectList.transform);
		List<StaticObject> objects = tile.GetTileObjects();
		if (objects != null)
			foreach (var obj in objects)
				CreateItemSprite(obj, ObjectList.transform);
	}

	private Sprite CreateTextureSprite(Texture2D tex)
	{
		Rect rect = new Rect(0, 0, tex.width, tex.height);
		Vector2 pivot = new Vector2(0.5f, 0.5f);
		Sprite spr = Sprite.Create(tex, rect, pivot);
		return spr;
	}

	public void ClearObjectList(Transform container, bool ignoreFirst = false)
	{
		for (int i = 0; i < container.childCount; i++)
		{
			if (ignoreFirst && i == 0)
				continue;
			Destroy(container.GetChild(i).gameObject);
		}
	}

	private void CreateItemSprite(StaticObject staticObject, Transform target)
	{
		GameObject imgObj = new GameObject();
		imgObj.name = staticObject.Name;
		imgObj.transform.SetParent(target);
		Image img = imgObj.AddComponent<Image>();
		img.sprite = MapCreator.GetObjectSpriteFromID(staticObject);
	}
}
