using System;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>Marks a row of an object or item list with what it stands for.</summary>
	public sealed class ObjectListEntry : MonoBehaviour
	{
		public StaticObject Object;   // a placed object, or null for an item type
		public int Item = -1;
	}

	/// <summary>
	/// Category and flag filter for the editor's list windows (the level's object list and the
	/// item pickers), combined with their name search. Rows without an ObjectListEntry are
	/// left alone.
	/// </summary>
	public sealed class ObjectListFilter : MonoBehaviour
	{
		public ItemFilter Filter = new ItemFilter();
		public string Search = "";
		Transform content;

		static readonly string[] Tri = { "any", "yes", "no" };

		/// <summary>Adds the filter controls to a list window. Placed objects get the flag filters too.</summary>
		public static ObjectListFilter Attach(ListMenu list, bool placedObjects)
		{
			var f = list.gameObject.AddComponent<ObjectListFilter>();
			f.content = list.Content.transform;
			Transform host = list.OptionsPanel ? list.OptionsPanel.transform : null;
			if (host == null) return f;
			list.OptionsPanel.SetActive(true);
			var labels = new string[Uw1ItemCategories.Choices.Length + 1];
			labels[0] = "Category: all";
			for (int i = 0; i < Uw1ItemCategories.Choices.Length; i++) labels[i + 1] = Uw1ItemCategories.Choices[i].Value;
			UiKit.Cycle(host, labels, 0, i =>
			{
				f.Filter.Categories = i == 0 ? ItemCategory.None : Uw1ItemCategories.Choices[i - 1].Key;
				f.Apply();
			}, 200);
			if (placedObjects)
			{
				Flag(host, "Enchanted", v => f.Filter.Enchanted = v, f);
				Flag(host, "Invisible", v => f.Filter.Invisible = v, f);
				Flag(host, "In container", v => f.Filter.InContainer = v, f);
				Flag(host, "Owned", v => f.Filter.Owned = v, f);
			}
			return f;
		}

		static void Flag(Transform host, string label, Action<Want> set, ObjectListFilter f)
		{
			var labels = new string[3];
			for (int i = 0; i < 3; i++) labels[i] = label + ": " + Tri[i];
			UiKit.Cycle(host, labels, 0, i => { set((Want)i); f.Apply(); }, 130);
		}

		public void SetSearch(string s)
		{
			Search = (s ?? "").ToLowerInvariant();
			Apply();
		}

		public void Apply()
		{
			if (!content) return;
			for (int i = 0; i < content.childCount; i++)
			{
				var row = content.GetChild(i).gameObject;
				bool nameOk = string.IsNullOrEmpty(Search) || row.name.ToLowerInvariant().Contains(Search);
				var e = row.GetComponent<ObjectListEntry>();
				bool filterOk = true;
				if (e != null)
				{
					if (e.Object != null)
						filterOk = Filter.Matches(ItemFinderWindow.View(e.Object, e.Object.CurrentLevel, e.Object.CurrentAdress));
					else if (e.Item >= 0)
						filterOk = Filter.Categories == ItemCategory.None || (Uw1ItemCategories.Of(e.Item) & Filter.Categories) != 0;
				}
				row.SetActive(nameOk && filterOk);
			}
		}
	}
}
