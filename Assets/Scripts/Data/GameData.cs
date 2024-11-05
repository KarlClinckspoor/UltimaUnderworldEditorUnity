using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData {

	public Dictionary<int, string> Quests;

	public GameData(string questFile)
	{
		//If file exists, read from file...

		//else, set default values
		Quests = GetDefaultQuests();
	}

	public string GetQuestName(int id)
	{
		if (id == 32)
			return "Knight of the Crux";
		if (id > 32)
			return "Invalid quest id - max is 32";
		if (Quests.ContainsKey(id))
			return Quests[id];
		else
			return "Quest id " + id + " no description.";
	}

	public Dictionary<int, string> GetDefaultQuests()
	{
		Dictionary<int, string> quests = new Dictionary<int, string>();
		quests[0] = "Murgo released from lizardmen";
		quests[1] = "Talked to Hagbard";
		quests[2] = "Met Dr. Owl";
		quests[3] = "Permission to speak to king Ketchaval";
		quests[4] = "Kill monster at mines";
		quests[5] = "Find talismans and throw into lava (?)";
		quests[6] = "Friend of lizardmen";
		quests[7] = "Murgo (?)";
		quests[8] = "Book from Bronus for Morlock";
		quests[9] = "Find Gurstang";
		quests[10] = "Where to find Zak, for Delanrey";
		quests[11] = "Rodrick killed";
		return quests;
	}
}
