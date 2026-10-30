using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Strings > Key editor: the names of keys 1 to 63, with buttons that find the keys and the
	/// locks of each number on every level.
	///
	/// Both games (underworld-exhumed, uw1 and uw2 src/obj/USEITEMS.C, UseKey and UseKeyOn;
	/// OBJUSE.C, checkLock): items 0x100 to 0x10E other than the lockpick (0x101) are keys, and a
	/// key's owner field is the number of the lock it fits. The owner field has 6 bits, so there
	/// are at most 63 keys. A lock is item 0x10F in a door's or container's contents; it opens
	/// with the key whose number is its link field (the low 9 bits; 0 means no key opens it).
	/// A key's name is string 0x64 + its number of string block 5 (LOOK.C, KeyLook).
	/// </summary>
	public static class KeyEditorWindow
	{
		public const int MaxKey = 63;
		const int ItemLockpick = 0x101, ItemLock = 0x10F;

		public static bool IsKeyItem(int item) { return item >= 0x100 && item < ItemLock && item != ItemLockpick; }
		public static bool IsLockItem(int item) { return item == ItemLock; }
		/// <summary>The key a lock's link field asks for; 0 when no key opens it.</summary>
		public static int LockKey(int link) { return link & 0x1FF; }

		sealed class Found
		{
			public StaticObject Object;
			public int Level, Index;
		}

		sealed class Cursor
		{
			public StaticObject Last;
		}

		public static GameObject Show(UIManager ui)
		{
			var w = UiKit.Window("Key editor", 820, 620);
			UiKit.Paragraph(w.Toolbar,
				"UW1 and UW2 have at most 63 keys: a key's number is its owner field, which has 6 bits. " +
				"A lock (in a door or a container) opens with the key whose number is its link field. " +
				"The name is what the game says when a key is looked at. " +
				"Find goes to the first key or lock with that number on any level, < and > step through them.", 11);
			var block = MapCreator.StringData.GetKeyBlock();
			var keys = new Dictionary<int, Cursor>();
			var locks = new Dictionary<int, Cursor>();
			var keyCounts = new Text[MaxKey + 1];
			var lockCounts = new Text[MaxKey + 1];

			for (int k = 1; k <= MaxKey; k++)
			{
				int id = k, index = 100 + k;
				var row = UiKit.Row(w.Content, 20, 4);
				UiKit.Label(row, "Key " + id, 11, FontStyle.Normal, UiKit.TextColor, 46);
				string name = index < block.Strings.Count ? block.Strings[index] : "";
				var input = UiKit.Input(row, name, s => { if (index < block.Strings.Count) block.Strings[index] = s; }, -1, false);
				input.interactable = index < block.Strings.Count;
				keyCounts[id] = UiKit.Label(row, "", 10, FontStyle.Normal, UiKit.DimTextColor, 44);
				keyCounts[id].alignment = TextAnchor.MiddleRight;
				Tip(UiKit.Button(row, "Find key", () => Step(ui, w, id, false, keys, 0), 56), "The first key " + id + " (any level)");
				Tip(UiKit.Button(row, "<", () => Step(ui, w, id, false, keys, -1), 20), "The previous key " + id);
				Tip(UiKit.Button(row, ">", () => Step(ui, w, id, false, keys, 1), 20), "The next key " + id);
				lockCounts[id] = UiKit.Label(row, "", 10, FontStyle.Normal, UiKit.DimTextColor, 50);
				lockCounts[id].alignment = TextAnchor.MiddleRight;
				Tip(UiKit.Button(row, "Find lock", () => Step(ui, w, id, true, locks, 0), 60), "The first lock that key " + id + " opens (any level)");
				Tip(UiKit.Button(row, "<", () => Step(ui, w, id, true, locks, -1), 20), "The previous lock for key " + id);
				Tip(UiKit.Button(row, ">", () => Step(ui, w, id, true, locks, 1), 20), "The next lock for key " + id);
			}

			Action counts = () =>
			{
				var nk = new int[MaxKey + 1]; var nl = new int[MaxKey + 1];
				foreach (var f in Collect(0, false)) if (f.Object.Owner >= 1 && f.Object.Owner <= MaxKey) nk[f.Object.Owner]++;
				foreach (var f in Collect(0, true)) { int k = LockKey(f.Object.Special); if (k >= 1 && k <= MaxKey) nl[k]++; }
				for (int k = 1; k <= MaxKey; k++)
				{
					keyCounts[k].text = nk[k] == 1 ? "1 key" : nk[k] + " keys";
					lockCounts[k].text = nl[k] == 1 ? "1 lock" : nl[k] + " locks";
				}
			};
			counts();
			UiKit.Button(w.Toolbar, "Count again", () => { counts(); w.Status.text = "Counted on every level."; }, -1, 20);
			w.Status.text = "Counts are of every level.";
			return w.Root;
		}

		static void Tip(Button b, string text) { UiKit.Tooltip(b.gameObject, text); }

		/// <summary>Every key (or lock) for key number id on every level, by level and index;
		/// id 0: every key (or lock) whatever its number.</summary>
		static List<Found> Collect(int id, bool lockSearch)
		{
			var list = new List<Found>();
			if (MapCreator.LevelData == null) return list;
			for (int l = 0; l < MapCreator.LevelData.Count; l++)
			{
				var objects = MapCreator.LevelData[l].Objects;
				if (objects == null) continue;
				for (int i = 0; i < objects.Length; i++)
				{
					var so = objects[i];
					if (!so) continue;
					bool match = lockSearch
						? IsLockItem(so.ObjectID) && (id == 0 || LockKey(so.Special) == id)
						: IsKeyItem(so.ObjectID) && (id == 0 || so.Owner == id);
					if (match) list.Add(new Found { Object = so, Level = l + 1, Index = i });
				}
			}
			return list;
		}

		/// <summary>dir 0: the first; 1 or -1: the next or previous after the last one shown,
		/// wrapping around.</summary>
		static void Step(UIManager ui, Window w, int id, bool lockSearch, Dictionary<int, Cursor> cursors, int dir)
		{
			if (MapCreator.LevelData == null) return;
			var list = Collect(id, lockSearch);
			string what = lockSearch ? "lock for key " + id : "key " + id;
			if (list.Count == 0)
			{
				w.Status.text = "No " + what + " on any level.";
				return;
			}
			Cursor c;
			if (!cursors.TryGetValue(id, out c)) cursors[id] = c = new Cursor();
			int at = c.Last ? list.FindIndex(f => f.Object == c.Last) : -1;
			int pick;
			if (dir == 0 || at < 0)
				pick = dir < 0 ? list.Count - 1 : 0;
			else
				pick = ((at + dir) % list.Count + list.Count) % list.Count;
			var found = list[pick];
			c.Last = found.Object;

			if (ui.CurrentLevel != found.Level)
				ui.GoToLevel(found.Level);
			ItemFinderWindow.GoTo(ui, found.Object);
			w.Root.transform.SetAsLastSibling();
			w.Status.text = string.Format("{0}: {1} of {2}, {3}, object #{4}, {5}.", Capital(what), pick + 1, list.Count,
				LevArk.Game.LevelName(found.Level), found.Index, Where(found.Object));
		}

		static string Where(StaticObject so)
		{
			var container = so.GetContainer();
			if (container)
			{
				var top = so.GetFirstContainer() ?? container;
				string s = "in " + container.GetFullName();
				if (top != container) s += ", inside " + top.GetFullName();
				return s + (top.Tile != null ? string.Format(" at tile {0},{1}", top.Tile.Position.x, top.Tile.Position.y) : "");
			}
			return so.Tile != null ? string.Format("tile {0},{1}", so.Tile.Position.x, so.Tile.Position.y) : "off the map";
		}

		static string Capital(string s) { return s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1); }
	}
}
