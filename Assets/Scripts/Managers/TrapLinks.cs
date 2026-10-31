using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Who links to whom among a level's triggers and traps, and the squares they act on, for
/// the properties panel, the target crosshairs and Tools > Triggers and traps.
/// A trigger (item 0x1A0-0x1BF) fires the trap its link names, and the whole chain runs at
/// the trigger's target square (quality, owner), not where the objects lie (both games'
/// event/TRIGGER.C UseTrigger). Each trap passes on along its own link.
/// </summary>
public static class TrapLinks
{
	public struct Target
	{
		public int Level, X, Y;
		public string What;
	}

	/// <summary>The objects of so's level whose link names so: the triggers that fire a trap,
	/// the traps before it in a chain, levers and buttons holding a trigger, doors holding a
	/// trap. Containers and critters (whose link is their inventory) are left out.</summary>
	public static List<StaticObject> LinkedFrom(StaticObject so)
	{
		var list = new List<StaticObject>();
		if (!so || so.CurrentLevel < 1 || so.CurrentLevel > MapCreator.LevelData.Count) return list;
		foreach (var o in MapCreator.LevelData[so.CurrentLevel - 1].Objects)
		{
			if (o == null || o == so || !HasLink(o) || o.Special != so.CurrentAdress) continue;
			if (o.IsContainer() || o is MobileObject) continue;
			list.Add(o);
		}
		return list;
	}

	/// <summary>Whether so's link field is a link. Traps and triggers follow it whatever
	/// their is_quant bit says (the shipped triggers have it set; uw1/src/event/TRIGGER.C
	/// UseTrigger and UseTrap read ol.link directly); other objects only without it.</summary>
	public static bool HasLink(StaticObject so)
	{
		return MapCreator.IsTrapOrTrigger(so) || !so.IsQuantity;
	}

	/// <summary>What so links to (its trap, the next trap of a chain), or null.</summary>
	public static StaticObject LinksTo(StaticObject so)
	{
		if (!so || !HasLink(so) || so.Special <= 0) return null;
		var objs = MapCreator.LevelData[so.CurrentLevel - 1].Objects;
		return so.Special < objs.Length ? objs[so.Special] : null;
	}

	/// <summary>The triggers whose chain reaches so, following links backwards.</summary>
	public static List<StaticObject> TriggersReaching(StaticObject so)
	{
		var found = new List<StaticObject>();
		var seen = new HashSet<StaticObject> { so };
		var todo = new Queue<StaticObject>();
		todo.Enqueue(so);
		while (todo.Count > 0 && seen.Count < 1024)
			foreach (var o in LinkedFrom(todo.Dequeue()))
			{
				if (!seen.Add(o)) continue;
				if (o.IsTrigger()) found.Add(o);
				else if (o.IsTrap()) todo.Enqueue(o);
			}
		return found;
	}

	/// <summary>The triggers of so's level whose chain runs on so's square.</summary>
	public static List<StaticObject> TriggersAimedAt(StaticObject so)
	{
		var list = new List<StaticObject>();
		if (!so || so.Tile == null) return list;
		foreach (var o in MapCreator.LevelData[so.CurrentLevel - 1].Objects)
			if (o != null && o != so && o.IsTrigger() && o.Quality == so.Tile.Position.x && o.Owner == so.Tile.Position.y)
				list.Add(o);
		return list;
	}

	/// <summary>The squares so acts on: a trigger's target square; a teleport's destination;
	/// a delete object trap's square; and for any other trap, the target squares of the
	/// triggers whose chain reaches it (where it runs).</summary>
	public static List<Target> Targets(StaticObject so)
	{
		var list = new List<Target>();
		if (!so) return list;
		int level = so.CurrentLevel;
		if (so.IsTrigger())
			list.Add(new Target { Level = level, X = so.Quality, Y = so.Owner, What = "its chain runs here" });
		else if (so.ObjectID == 0x181)
		{
			int to = so.ZPos == 0 ? level : so.ZPos;
			if (so.Quality > 0 && so.Quality < 63 && so.Owner > 0 && so.Owner < 63)
				list.Add(new Target { Level = to, X = so.Quality, Y = so.Owner, What = "teleports the player here" + (to != level ? " (" + LevArk.Game.LevelName(to) + ")" : "") });
		}
		else if (so.ObjectID == 0x18B)
			list.Add(new Target { Level = level, X = so.Quality, Y = so.Owner, What = "deletes its linked object from here" });
		if (so.IsTrap())
			foreach (var t in TriggersReaching(so))
				list.Add(new Target { Level = level, X = t.Quality, Y = t.Owner, What = "runs here (from " + t.Name + " [" + t.CurrentAdress + "])" });
		// drop duplicates and squares off the map
		var unique = new List<Target>();
		foreach (var t in list)
		{
			if (t.X < 0 || t.X > 63 || t.Y < 0 || t.Y > 63) continue;
			if (unique.Exists(u => u.Level == t.Level && u.X == t.X && u.Y == t.Y)) continue;
			unique.Add(t);
		}
		return unique;
	}

	/// <summary>The chain from a trigger: the trigger, then each linked trap in turn.</summary>
	public static List<StaticObject> Chain(StaticObject trigger)
	{
		var list = new List<StaticObject> { trigger };
		var seen = new HashSet<StaticObject> { trigger };
		var next = LinksTo(trigger);
		while (next && seen.Add(next) && list.Count < 256)
		{
			list.Add(next);
			if (!next.IsTrap()) break;
			next = LinksTo(next);
		}
		return list;
	}
}

/// <summary>Red crosshairs on the squares the selected object acts on (TrapLinks.Targets),
/// on the level shown. They follow the properties panel's selection.</summary>
public class TargetMarkers : MonoBehaviour
{
	static TargetMarkers instance;
	UIManager ui;
	StaticObject shown;
	int shownLevel;
	List<TrapLinks.Target> targets = new List<TrapLinks.Target>();

	public static void Show(UIManager ui, StaticObject so)
	{
		if (!instance)
		{
			instance = new GameObject("Target markers").AddComponent<TargetMarkers>();
			instance.ui = ui;
		}
		instance.shown = so;
		instance.targets = TrapLinks.Targets(so);
		instance.shownLevel = -1;
	}

	void Update()
	{
		if (!ui) return;
		var panel = ui.ObjectProperties;
		StaticObject sel = panel && panel.activeSelf ? panel.GetComponent<PropertiesPanel>().SelectedObject : null;
		if (sel != shown)
		{
			shown = sel;
			targets = sel ? TrapLinks.Targets(sel) : new List<TrapLinks.Target>();
			shownLevel = -1;
		}
		if (shownLevel == ui.CurrentLevel) return;
		shownLevel = ui.CurrentLevel;
		for (int i = transform.childCount - 1; i >= 0; i--)
			Destroy(transform.GetChild(i).gameObject);
		if (!MapCreator.IsLoaded) return;
		float ts = MapCreator.TileSize * 2.0f;
		foreach (var t in targets)
		{
			if (t.Level != shownLevel || t.Level < 1 || t.Level > MapCreator.LevelData.Count) continue;
			MapTile tile = MapCreator.LevelData[t.Level - 1].MapTiles[t.X, t.Y];
			float z = MapCreator.GetTileHeight(tile) - 0.01f;
			Vector3 c = new Vector3((t.X + 0.5f) * ts, (t.Y + 0.5f) * ts, z);
			float r = ts * 0.45f;
			Line(c + new Vector3(-r, -r, 0), c + new Vector3(r, r, 0));
			Line(c + new Vector3(-r, r, 0), c + new Vector3(r, -r, 0));
			Line(c + new Vector3(-r, 0, 0), c + new Vector3(r, 0, 0));
			Line(c + new Vector3(0, -r, 0), c + new Vector3(0, r, 0));
		}
	}

	void Line(Vector3 a, Vector3 b)
	{
		var go = new GameObject("crosshair");
		go.transform.SetParent(transform, false);
		var lr = go.AddComponent<LineRenderer>();
		if (MapCreator.SpriteMat) lr.material = MapCreator.SpriteMat;
		lr.sortingLayerName = "Items";
		lr.sortingOrder = 5;
		lr.startWidth = lr.endWidth = 0.025f;
		lr.startColor = lr.endColor = new Color(1f, 0.15f, 0.1f, 0.95f);
		lr.positionCount = 2;
		lr.SetPositions(new[] { a, b });
	}
}
