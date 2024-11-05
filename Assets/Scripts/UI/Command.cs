using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Command {

	public string Description { get; protected set; }

	public virtual void Do() { }
	public virtual void Undo() { }
}

public class MoveObjectCommand : Command
{
	private StaticObject staticObject;
	private MapTile oldTile;
	private MapTile newTile;
	private Vector2Int oldOffsets;
	private Vector2Int newOffsets;

	public MoveObjectCommand(StaticObject so, MapTile ot, MapTile nt, Vector2Int oo, Vector2Int no)
	{
		staticObject = so;
		oldTile = ot;
		newTile = nt;
		oldOffsets = oo;
		newOffsets = no;

		Description = string.Format("Move {0} from tile {1} to {2}", so.Name, ot.Position, nt.Position);
	}

	public override void Do()
	{
		if (!staticObject)
			return;
		MapCreator.SetNewPosition(staticObject, oldTile, newTile, newOffsets);
	}
	public override void Undo()
	{
		if (!staticObject)
			return;
		MapCreator.SetNewPosition(staticObject, newTile, oldTile, oldOffsets);
	}
}

public class AddToContainerCommand : Command
{
	private StaticObject staticObject;
	private StaticObject container;

	private MapTile oldTile;
	private Vector2Int oldOffsets;

	public AddToContainerCommand(StaticObject so, StaticObject con, MapTile t, Vector2Int oo)
	{
		staticObject = so;
		container = con;
		oldTile = t;
		oldOffsets = oo;

		Description = string.Format("Add {0} to {1} inventory", so.Name, con.Name);
	}

	public override void Do()
	{
		bool added = container.AddToContainer(staticObject);
		if (!added)
			return;
		oldTile.RemoveObjectFromTile(staticObject);		
		GameObject toDestroy = MapCreator.ObjectToGO[staticObject];
		MapCreator.ObjectToGO.Remove(staticObject);
		Object.Destroy(toDestroy);
	}

	public override void Undo()
	{
		oldTile.AddObjectToTile(staticObject);
		container.RemoveFromContainer(staticObject);
		GameObject go = MapCreator.SpawnGO(staticObject, MapCreator.MapTileToGO[oldTile]);
	}
}

public class RemoveFromContainerCommand : Command
{
	private StaticObject container;
	private StaticObject removed;

	private MapTile newTile;
	
	public RemoveFromContainerCommand(StaticObject so, StaticObject con, MapTile tile)
	{
		container = con;
		removed = so;
		newTile = tile;

		Description = string.Format("Remove {0} from inventory {1}", removed.Name, container.Name);
	}

	public override void Do()
	{
		container.RemoveFromContainer(removed);
		newTile.AddObjectToTile(removed, container);		
		GameObject go = MapCreator.SpawnGO(removed, MapCreator.MapTileToGO[newTile]);
	}

	public override void Undo()
	{
		newTile.RemoveObjectFromTile(removed);
		container.AddToContainer(removed);
		Object.Destroy(MapCreator.ObjectToGO[removed]);
	}
}
public class RemoveObjectCommand : Command
{
	private StaticObject toRemove;

	public RemoveObjectCommand(StaticObject so)
	{
		toRemove = so;

		Description = string.Format("Remove {0}", so.Name);
	}

	public override void Do()
	{
		MapCreator.RemoveObject(toRemove, true);
	}

	public override void Undo()
	{
		MapCreator.AddObject(toRemove.Tile, toRemove.CurrentLevel, toRemove);
	}
}

public class AddObjectCommand : Command
{
	private StaticObject toAdd;
	private int level;
	private MapTile tile;

	public AddObjectCommand(StaticObject so, int l, MapTile t)
	{
		toAdd = so;
		level = l;
		tile = t;

		Description = string.Format("Add {0} at {1} in level {2}", toAdd, tile.Position, level);
	}

	public override void Do()
	{
		MapCreator.AddObject(tile, level, toAdd);
	}

	public override void Undo()
	{
		MapCreator.RemoveObject(toAdd, true);
	}
}