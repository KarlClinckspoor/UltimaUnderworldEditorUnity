using System;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// The randomizer tools are a work in progress, so they are hidden unless turned on in the
	/// Tools menu ("Show the randomizer tools"). Hidden: Sector mode in the mode list (its
	/// panel holds the sector buttons, so they go with it) and Tools > Zones and Zone graph.
	/// The choice is kept in PlayerPrefs; nothing in the data changes either way.
	/// </summary>
	public static class RandomizerToggle
	{
		const string PrefKey = "UltimateEditor.ShowRandomizer";
		const string SectorWord = "Sector";
		static Dropdown.OptionData sectorOption;

		public static bool Shown
		{
			get { return PlayerPrefs.GetInt(PrefKey, 0) != 0; }
			set
			{
				PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
				PlayerPrefs.Save();
				Apply(UiKit.FindUIManager());
			}
		}

		/// <summary>Adds or removes Sector mode in the scene's mode list. Sector mode is the
		/// list's last entry (UIManager.SetEditorMode takes the entry number + 1 as the mode),
		/// so it is put back at the end.</summary>
		public static void Apply(UIManager ui)
		{
			if (!ui || !ui.ModePanel) return;
			var t = ui.ModePanel.transform.Find("ModeDropdown");
			var dd = t ? t.GetComponent<Dropdown>() : null;
			if (!dd) return;
			int idx = dd.options.FindIndex(o => o.text != null && o.text.IndexOf(SectorWord, StringComparison.OrdinalIgnoreCase) >= 0);
			if (Shown)
			{
				if (idx < 0 && sectorOption != null)
				{
					dd.options.Add(sectorOption);
					dd.RefreshShownValue();
				}
			}
			else if (idx >= 0)
			{
				sectorOption = dd.options[idx];
				if (dd.value == idx)
					dd.value = 0;   // its listener, UIManager.SetEditorMode, switches to Object mode
				dd.options.RemoveAt(idx);
				dd.RefreshShownValue();
			}
		}
	}
}
