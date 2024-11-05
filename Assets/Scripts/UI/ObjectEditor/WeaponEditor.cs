using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class WeaponEditor : MonoBehaviour {

	public InputField Slash;
	public InputField Bash;
	public InputField Stab;

	public InputField Unk1;
	public InputField Unk2;
	public InputField Unk3;

	public InputField Skill;
	public InputField Durability;

	public Image WeaponSprite;
	public Text WeaponName;

	public void SetWeapon(WeaponData wd)
	{
		Slash.text = wd.Slash.ToString();
		Bash.text = wd.Bash.ToString();
		Stab.text = wd.Stab.ToString();

		Unk1.text = wd.Unk1.ToString();
		Unk2.text = wd.Unk2.ToString();
		Unk3.text = wd.Unk3.ToString();

		Skill.text = wd.Skill.ToString();
		Durability.text = wd.Durability.ToString();

		WeaponSprite.sprite = MapCreator.GetObjectSpriteFromID(wd.ObjectID);
		WeaponName.text = StaticObject.GetName(wd.ObjectID);
	}
}
