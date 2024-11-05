using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaticObjectPanel : MonoBehaviour {

	public Text Name;
	public Image Icon;
	public Text Position;
	public Text Flags;
	public Text Adress;
	public Text Direction;
	public Text Quality;

	public Text Enchantable;
	public Text DoorOpen;
	public Text Invisible;
	public Text IsQuantity;

	public Text Owner_Special;
	public Text Quantity_Link;

	public Text Prev;
	public Text Next;

	public Text Attitude;
	public Text Home;
	public Text Goal;
	public Text GTarg;
	public Text GLevel;
	public Text HP;
	public Text WhoAmI;
	public Text MobListIndex;
	public Text AdditionalInfo;

	public GameObject NextObjectChainPanel;
	public GameObject MobilePanel;
	public GameObject ContainerPanel;

	public void SetStaticObject(StaticObject so)
	{
		Name.text = so.GetFullName();
		Name.text += " (" + so.ObjectID + ") [" + DataReader.ToHex(so.ObjectID) + "] {" + DataReader.ToHex(so.GetFileIndex()) + "}";
		Icon.sprite = MapCreator.GetObjectSpriteFromID(so);
		Position.text = "[x:" + so.XPos + " y:" + so.YPos + " z:" + so.ZPos + "]" + "[X:" + so.MapPosition.x + "/Y:" + so.MapPosition.y + "]";
		Flags.text = "Flags : " + so.Flags;
		Adress.text = "Adr : " + so.CurrentAdress;
		Direction.text = "Direction : " + so.Direction;
		Quality.text = "Quality : " + so.Quality;

		Enchantable.text = "Enchanted : " + so.IsEnchanted;
		DoorOpen.text = "Door open : " + so.IsDoorOpen;
		Invisible.text = "Invisible : " + so.IsInvisible;
		IsQuantity.text = "Quantity : " + so.IsQuantity;

		Owner_Special.text = "Owner/special : " + so.Owner;
		Quantity_Link.text = "Quantity/link : " + so.Special;

		Prev.text = "Prev : " + so.PrevAdress.ToString();
		Next.text = "Next : " + so.NextAdress.ToString();

		StaticObject next = so.GetNextObject();
		int safe = 100;
		while(next)
		{
			CreateItemSprite(next, NextObjectChainPanel.transform);
			next = next.GetNextObject();

			safe--;
			if(safe == 0)
			{
				Debug.LogError("FUCK");
				return;
			}
		}

		if(!so.IsQuantity || so is MobileObject)
		{
			SetContainerItems(so);
		}
		else
		{
			ContainerPanel.SetActive(false);
		}

		if(so is MobileObject)
		{
			MobilePanel.SetActive(true);
			MobileObject mo = (MobileObject)so;
			Attitude.text = "Attitude : " + mo.Attitude;
			Home.text = "Home : [X : " + mo.XHome + ", Y : " + mo.YHome + "]";
			Goal.text = "Goal : " + mo.Goal;
			GTarg.text = "GTarg : " + DataReader.ToHex(mo.GTarg);
			GLevel.text = "GLevel : " + mo.Level;
			HP.text = "HP : " + mo.HP;
			WhoAmI.text = "Who am I : " + mo.Whoami;
			MobListIndex.text = "Mob list : " + mo.MobListIndex;
			AdditionalInfo.text = "" + mo.B13 + "/" + mo.B14 + "/" + mo.B15;
		}
		else
		{
			MobilePanel.SetActive(false);
			Attitude.text = "";
			Home.text = "";
			Goal.text = "";
			GTarg.text = "";
			GLevel.text = "";
			HP.text = "";
			WhoAmI.text = "";
			MobListIndex.text = "";
			AdditionalInfo.text = "";
		}
	}

	public void SetContainerItems(StaticObject so)
	{
		int safe = 100;
		ContainerPanel.SetActive(true);
		StaticObject contained = so.GetContainedObject();
		while (contained)
		{
			CreateItemSprite(contained, ContainerPanel.transform);
			contained = contained.GetNextObject();

			safe--;
			if (safe == 0)
			{
				Debug.LogError("FUCK");
				return;
			}
		}
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

	public void CreateItemSprite(StaticObject staticObject, Transform target)
	{
		GameObject imgObj = new GameObject();
		imgObj.name = staticObject.Name;
		imgObj.transform.SetParent(target);
		Image img = imgObj.AddComponent<Image>();
		img.sprite = MapCreator.GetObjectSpriteFromID(staticObject);
	}
}
