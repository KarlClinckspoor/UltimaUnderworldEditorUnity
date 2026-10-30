using UnityEngine;

/// <summary>
/// The edits of a level's object lists, in one place (review package 10): taking an object out
/// of a container, putting one in, deleting and moving. Each keeps the lists whole (links,
/// tile chains, the active critter list, the scene's game objects); the callers only choose
/// what to do and wrap it in an undoable step (UIManager.Undoable). Before, taking an object out
/// of a container was written three times (one of them undoable) and each copy differed.
/// </summary>
public static class ObjectList
{
	/// <summary>Takes the object out of its container (or door, critter, trap) and puts it on the
	/// container's tile, where it is shown (<paramref name="shown"/>, null without a scene). False
	/// when it was in no container.</summary>
	public static bool TakeOut(StaticObject so, out GameObject shown)
	{
		shown = null;
		if (!so) return false;
		StaticObject container = so.GetContainer();
		if (!container || container.Tile == null) return false;
		MapTile tile = container.Tile;
		if (!container.RemoveFromContainer(so)) return false;
		tile.AddObjectToTile(so, container);
		GameObject parent;
		if (MapCreator.MapTileToGO != null && MapCreator.MapTileToGO.TryGetValue(tile, out parent))
			shown = MapCreator.SpawnGO(so, parent);
		return true;
	}

	public static bool TakeOut(StaticObject so)
	{
		GameObject shown;
		return TakeOut(so, out shown);
	}

	/// <summary>Puts the object in the container (its game object goes, as it no longer lies on
	/// a tile). False when the container cannot hold it.</summary>
	public static bool PutIn(StaticObject so, StaticObject container)
	{
		if (!so || !container || so == container || !container.CanBeInserted(so)) return false;
		// never into something it holds itself (a bag into the pouch inside it)
		var seen = new System.Collections.Generic.HashSet<StaticObject>();
		for (var up = container.GetContainer(); up && seen.Add(up); up = up.GetContainer())
			if (up == so) return false;
		return MapCreator.AddToContainer(so, container);
	}

	/// <summary>Deletes the object and everything in it, clearing links to it.</summary>
	public static bool Delete(StaticObject so)
	{
		return so && MapCreator.RemoveObject(so, true);
	}

	/// <summary>Moves the object to a tile and position (traps and triggers keep their height).</summary>
	public static bool Move(StaticObject so, MapTile from, MapTile to, Vector2Int offsets)
	{
		if (!so || to == null) return false;
		MapCreator.SetNewPosition(so, from, to, offsets);
		return true;
	}
}
