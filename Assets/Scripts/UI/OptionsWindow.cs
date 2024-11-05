using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionsWindow : MovableWindow {

	public GameObject OptionPrefab;
	public Text Title;
	public GameObject MainPanel;

	public InputField AddOption(string description, InputField.ContentType contentType, string defValue, Action<string> act, Color? col = null)
	{
		GameObject optionGO = Instantiate(OptionPrefab, MainPanel.transform);
		Text text = optionGO.GetComponentInChildren<Text>();
		text.text = description;
		InputField input = optionGO.GetComponentInChildren<InputField>();
		input.text = defValue;
		input.contentType = contentType;
		input.onEndEdit.AddListener((str) => act(str));
		if(col != null)
			optionGO.GetComponent<Image>().color = (Color)col;
		return input;
	}
	//Add dropdown for some options
	public void AddDefaultOptions(int[] options)
	{
		if (options == null)
			return;
		AddOption("Player hunger (?)",		InputField.ContentType.IntegerNumber, options[0].ToString(), (str) => options[0] = int.Parse(str));
		AddOption("Player health",			InputField.ContentType.IntegerNumber, options[1].ToString(), (str) => options[1] = int.Parse(str));
		AddOption("Player arms (?)",		InputField.ContentType.IntegerNumber, options[2].ToString(), (str) => options[2] = int.Parse(str));
		AddOption("Player power (?)",		InputField.ContentType.IntegerNumber, options[3].ToString(), (str) => options[3] = int.Parse(str));
		AddOption("Player HP",				InputField.ContentType.IntegerNumber, options[4].ToString(), (str) => options[4] = int.Parse(str));
		AddOption("Player mana",			InputField.ContentType.IntegerNumber, options[5].ToString(), (str) => options[5] = int.Parse(str));
		AddOption("Player level",			InputField.ContentType.IntegerNumber, options[6].ToString(), (str) => options[6] = int.Parse(str));
		AddOption("New player exp (?)",		InputField.ContentType.IntegerNumber, options[7].ToString(), (str) => options[7] = int.Parse(str));
		AddOption("Player name (pointer)",	InputField.ContentType.IntegerNumber, options[8].ToString(), (str) => options[8] = int.Parse(str));
		AddOption("Player poison",			InputField.ContentType.IntegerNumber, options[9].ToString(), (str) => options[9] = int.Parse(str));
		AddOption("Player drawn (?)",		InputField.ContentType.IntegerNumber, options[10].ToString(), (str) => options[10] = int.Parse(str));
		AddOption("Player sex",				InputField.ContentType.IntegerNumber, options[11].ToString(), (str) => options[11] = int.Parse(str));
		AddOption("NPC home X",				InputField.ContentType.IntegerNumber, options[12].ToString(), (str) => options[12] = int.Parse(str));
		AddOption("NPC home Y",				InputField.ContentType.IntegerNumber, options[13].ToString(), (str) => options[13] = int.Parse(str));
		AddOption("NPC hunger (?)",			InputField.ContentType.IntegerNumber, options[15].ToString(), (str) => options[15] = int.Parse(str));
		AddOption("NPC health",				InputField.ContentType.IntegerNumber, options[16].ToString(), (str) => options[16] = int.Parse(str));
		AddOption("NPC HP",					InputField.ContentType.IntegerNumber, options[17].ToString(), (str) => options[17] = int.Parse(str));
		AddOption("NPC arms (?)",			InputField.ContentType.IntegerNumber, options[18].ToString(), (str) => options[18] = int.Parse(str));
		AddOption("NPC power (?)",			InputField.ContentType.IntegerNumber, options[19].ToString(), (str) => options[19] = int.Parse(str));
		AddOption("NPC goal",				InputField.ContentType.IntegerNumber, options[20].ToString(), (str) => options[20] = int.Parse(str));
		AddOption("NPC attitude",			InputField.ContentType.IntegerNumber, options[21].ToString(), (str) => options[21] = int.Parse(str), Color.green);
		AddOption("NPC target",				InputField.ContentType.IntegerNumber, options[22].ToString(), (str) => options[22] = int.Parse(str));
		AddOption("NPC talked to(*)",		InputField.ContentType.IntegerNumber, options[23].ToString(), (str) => options[23] = int.Parse(str));
		AddOption("NPC level",				InputField.ContentType.IntegerNumber, options[24].ToString(), (str) => options[24] = int.Parse(str));
		AddOption("Dungeon level",			InputField.ContentType.IntegerNumber, options[26].ToString(), (str) => options[26] = int.Parse(str));
		AddOption("Riddle counter (?)",		InputField.ContentType.IntegerNumber, options[27].ToString(), (str) => options[27] = int.Parse(str));
		AddOption("Game time",				InputField.ContentType.IntegerNumber, options[28].ToString(), (str) => options[28] = int.Parse(str));
		AddOption("Game days",				InputField.ContentType.IntegerNumber, options[29].ToString(), (str) => options[29] = int.Parse(str));
		AddOption("Game mins",				InputField.ContentType.IntegerNumber, options[30].ToString(), (str) => options[30] = int.Parse(str));
	}
}
