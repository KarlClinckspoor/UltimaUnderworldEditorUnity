#if UIMGUI_SHELL
using System;
using System.Collections.Generic;
using System.IO;
using ImGuiNET;
using UltimateEditor.Uw1.UI;
using UnityEngine;
using V2 = UnityEngine.Vector2;
using V4 = UnityEngine.Vector4;

namespace UltimateEditor.Uw1.ImGuiUI
{
	public sealed partial class ImGuiShell
	{
		static readonly string[] TileTypeNames = Enum.GetNames(typeof(TileType));
		static readonly string[] ModeNames = { "Object", "Tile", "Texture", "Sector" };
		static readonly string[] Headings = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
		static readonly string[] Attitudes = { "hostile", "upset", "mellow", "friendly" };

		string objectFilter = "";
		int objectListLevel = -1;
		float objectListTime;
		readonly List<KeyValuePair<StaticObject, string>> objectList = new List<KeyValuePair<StaticObject, string>>();

		// ---- Level and mode ----

		void DrawLevelWindow()
		{
			if (!Loaded)
			{
				T(ui.IsLoading ? "Loading..." : "No game loaded.");
				if (!ui.IsLoading)
				{
					if (ImGui.Button("Open game...")) ui.CreateFileExplorer();
					string last = DataReader.FilePath;
					if (!string.IsNullOrEmpty(last) && File.Exists(last))
					{
						ImGui.SameLine();
						if (ImGui.Button("Load the last one")) ui.LoadData();
						Dim(last);
					}
				}
				return;
			}

			var game = LevArk.Game;
			T(game.Name);
			if (GameSession.IsSave) { ImGui.SameLine(); Dim("saved game " + GameSession.SaveName); }

			int count = ui.MapCreator.GetLevelCount();
			int level = ui.CurrentLevel;
			if (ImGui.ArrowButton("##prev", ImGuiDir.Left) && level > 1) ui.GoToLevel(level - 1);
			ImGui.SameLine();
			ImGui.SetNextItemWidth(Math.Max(80, ImGui.GetContentRegionAvail().x - ImGui.GetFrameHeight() - 8));
			if (ImGui.SliderInt("##level", ref level, 1, count, "Level %d")) ui.GoToLevel(level);
			ImGui.SameLine();
			if (ImGui.ArrowButton("##next", ImGuiDir.Right) && ui.CurrentLevel < count) ui.GoToLevel(ui.CurrentLevel + 1);

			ImGui.Separator();
			int mode = (int)UIManager.CurrentMode;
			for (int i = 0; i < ModeNames.Length; i++)
			{
				if (i > 0) ImGui.SameLine();
				if (ImGui.RadioButton(ModeNames[i], mode == i + 1) && mode != i + 1)
					ui.SetEditorMode((EditorMode)(i + 1));
			}
			ImGui.Spacing();

			switch (UIManager.CurrentMode)
			{
				case EditorMode.Object:
					Wrapped("Click an object to select it, drag it to move it. Right-click an object or a tile for its menu.");
					break;
				case EditorMode.Tile:
					DrawTileTemplate(level);
					break;
				case EditorMode.Texture:
					DrawTextureTemplate(level);
					break;
				case EditorMode.Sector:
					DrawSectorTools();
					break;
			}
		}

		void DrawTileTemplate(int level)
		{
			Wrapped("Left click selects a tile. The right button paints tiles with the template below; the mouse wheel raises or lowers a tile.");
			int type = (int)ui.TemplateType;
			if (ImGui.Combo("Type", ref type, TileTypeNames, TileTypeNames.Length)) ui.TemplateType = (TileType)type;
			int height = ui.TemplateHeight;
			if (ImGui.SliderInt("Height", ref height, 0, 15)) ui.TemplateHeight = height;
			TexturePick("Floor", MapCreator.GetFloorTextureFromIndex(ui.TemplateFloor, level), ui.CreateTemplateFloorExplorer);
			TexturePick("Wall", MapCreator.GetWallTextureFromIndex(ui.TemplateWall, level), ui.CreateTemplateWallExplorer);
		}

		void DrawTextureTemplate(int level)
		{
			Wrapped("Either mouse button paints the ticked textures onto tiles.");
			var floorToggle = ui.TextureTemplateFloorToggle;
			var wallToggle = ui.TextureTemplateWallToggle;
			if (floorToggle)
			{
				bool on = floorToggle.isOn;
				if (ImGui.Checkbox("Paint the floor", ref on)) floorToggle.isOn = on;
			}
			TexturePick("Floor", MapCreator.GetFloorTextureFromIndex(ui.TemplateFloor, level), ui.CreateTemplateFloorExplorer);
			if (wallToggle)
			{
				bool on = wallToggle.isOn;
				if (ImGui.Checkbox("Paint the wall", ref on)) wallToggle.isOn = on;
			}
			TexturePick("Wall", MapCreator.GetWallTextureFromIndex(ui.TemplateWall, level), ui.CreateTemplateWallExplorer);
		}

		void DrawSectorTools()
		{
			if (ZoneTools.PaintEnabled && ZoneTools.Zones != null)
			{
				Wrapped("Painting zones: the Zones window (Tools menu) is open. Left button paints its selected zone, right button unassigns.");
				return;
			}
			Wrapped("The left button paints tiles with the chosen sector.");
			var dd = ui.SectorDropdown;
			if (dd && dd.options.Count > 0)
			{
				var names = new string[dd.options.Count];
				for (int i = 0; i < names.Length; i++) names[i] = dd.options[i].text ?? "";
				int v = Mathf.Clamp(dd.value, 0, names.Length - 1);
				if (ImGui.Combo("Sector", ref v, names, names.Length)) dd.value = v;
			}
			else
				Dim("No sectors on this level yet.");
			if (ImGui.Button("Add a sector...")) ui.AddSector();
			ImGui.SameLine();
			if (ImGui.Button("Edit the sector...")) ui.EditSector();
		}

		void TexturePick(string label, Texture2D tex, Action pick)
		{
			WithId(label, () =>
			{
				Image(tex, 40);
				ImGui.SameLine();
				ImGui.BeginGroup();
				T(label);
				if (ImGui.SmallButton("Change...")) pick();
				ImGui.EndGroup();
			});
		}

		// ---- Selection ----

		StaticObject SelectedObject()
		{
			if (!ui.ObjectProperties || !ui.ObjectProperties.activeSelf) return null;
			var pp = ui.ObjectProperties.GetComponent<PropertiesPanel>();
			return pp ? pp.SelectedObject : null;
		}

		MapTileScript SelectedTile()
		{
			if (!ui.TilePropertiesObject || !ui.TilePropertiesObject.activeSelf || !ui.InputManager) return null;
			var go = ui.InputManager.SelectedObject;
			return go ? go.GetComponent<MapTileScript>() : null;
		}

		void DrawSelectionWindow()
		{
			if (!Loaded) { Dim("Nothing loaded."); return; }
			var so = SelectedObject();
			if (so) { DrawObject(so); return; }
			var mts = SelectedTile();
			if (mts && mts.MapTile) { DrawTile(mts); return; }
			Wrapped("Nothing selected. Click a tile in Tile mode or an object in Object mode, or pick one in the object list.");
		}

		void DrawObject(StaticObject so)
		{
			ObjectImage(so, 48);
			ImGui.SameLine();
			ImGui.BeginGroup();
			T(so.GetFullName());
			Dim(string.Format("item 0x{0:X3}, object {1}, level {2}", so.ObjectID, so.CurrentAdress, so.CurrentLevel));
			Dim(string.Format("tile ({0}, {1})", so.MapPosition.x, so.MapPosition.y));
			ImGui.EndGroup();

			if (ImGui.Button("Show on the map")) ui.SetCameraToObject(so);
			ImGui.SameLine();
			if (ImGui.Button("Raw fields...")) FieldInspectorWindow.Show(so);
			bool classicEditor = classicObjectEditor;
			if (ImGui.Checkbox("Classic object editor", ref classicEditor))
			{
				classicObjectEditor = classicEditor;
				if (classicEditor) ui.SetObjectProperties(so);   // refresh it with the current values
				ApplyClassicVisibility();
			}
			Help("The old properties panel, with each object type's own fields (traps, doors, books...). It shows on the right while ticked.");

			if (ImGui.CollapsingHeader("Position", ImGuiTreeNodeFlags.DefaultOpen))
			{
				IntField("X in the tile", so.XPos, 0, 7, v => { so.XPos = v; MapCreator.SetGOPosition(so); });
				IntField("Y in the tile", so.YPos, 0, 7, v => { so.YPos = v; MapCreator.SetGOPosition(so); });
				IntField("Height (Z)", so.ZPos, 0, 127, v => { so.ZPos = v; MapCreator.SetGOHeight(so); });
				int heading = Mathf.Clamp(so.Direction, 0, 7);
				if (ImGui.Combo("Heading", ref heading, Headings, Headings.Length)) { so.Direction = heading; MapCreator.SetGODirection(so); }
			}
			if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
			{
				IntField("Quality", so.Quality, 0, 63, v => so.Quality = v);
				IntField("Owner", so.Owner, 0, 63, v => so.Owner = v);
				IntField("Flags", so.Flags, 0, 7, v => so.Flags = v);
				if (so.IsQuantity)
				{
					IntField("Quantity", so.Special, 0, 1023, v => so.Special = v);
					Help("Below 512 it is a count; from 512 up it is a special value (512 + n) that some items read.");
				}
				else
				{
					T("Link: " + so.Special);
					Help("The first object inside this one (its contents, or a trap's or trigger's target). Edit it with Raw fields.");
				}
				bool b = so.IsEnchanted;
				if (ImGui.Checkbox("Enchanted", ref b)) so.IsEnchanted = b;
				ImGui.SameLine();
				b = so.IsInvisible;
				if (ImGui.Checkbox("Invisible", ref b)) so.IsInvisible = b;
				b = so.IsDoorOpen;
				if (ImGui.Checkbox("Door open", ref b)) so.IsDoorOpen = b;
				ImGui.SameLine();
				Dim(so.IsQuantity ? "has a quantity" : "has a link");
			}
			var mo = so as MobileObject;
			if (mo && ImGui.CollapsingHeader("Creature", ImGuiTreeNodeFlags.DefaultOpen))
			{
				IntField("Hit points", mo.HP, 0, 255, v => mo.HP = v);
				int att = Mathf.Clamp(mo.Attitude, 0, 3);
				if (ImGui.Combo("Attitude", ref att, Attitudes, Attitudes.Length)) mo.Attitude = att;
				IntField("Goal", mo.Goal, 0, 15, v => mo.Goal = v);
				IntField("Home X", mo.XHome, 0, 63, v => mo.XHome = v);
				IntField("Home Y", mo.YHome, 0, 63, v => mo.YHome = v);
				IntField("NPC (whoami)", mo.Whoami, 0, 255, v => mo.Whoami = v);
			}
			var inside = so.GetContainedObjects();
			if (inside != null && inside.Count > 0 && ImGui.CollapsingHeader("Contents (" + inside.Count + ")", ImGuiTreeNodeFlags.DefaultOpen))
				ObjectLinks("in", inside);
			var next = new List<StaticObject>();
			for (var n = so.GetNextObject(); n && next.Count < 100; n = n.GetNextObject()) next.Add(n);
			if (next.Count > 0 && ImGui.CollapsingHeader("Next on the tile (" + next.Count + ")"))
				ObjectLinks("next", next);
		}

		void DrawTile(MapTileScript mts)
		{
			MapTile tile = mts.MapTile;
			int level = ui.CurrentLevel;
			T(string.Format("Tile ({0}, {1}), level {2}", tile.Position.x, tile.Position.y, tile.Level));

			int type = (int)tile.TileType;
			if (ImGui.Combo("Type", ref type, TileTypeNames, TileTypeNames.Length))
			{
				tile.TileType = (TileType)type;
				TileChanged(mts);
			}
			int height = tile.FloorHeight;
			if (ImGui.SliderInt("Height", ref height, 0, 15))
			{
				tile.FloorHeight = height;
				TileChanged(mts);
				// doors stand on the floor (UIManager.ChangeTileProperty)
				var doors = tile.GetTileObjects(o => o.IsDoor());
				if (doors != null)
					foreach (var door in doors)
					{
						door.ZPos = tile.FloorHeight * 8;
						MapCreator.SetGOHeight(door);
						MapCreator.SetDoorGOPosition(door);
					}
			}
			bool b = tile.IsDoor;
			if (ImGui.Checkbox("Door bit", ref b)) tile.IsDoor = b;
			Help("Not \"a door is here\": the game keeps an open door on this tile open.");
			ImGui.SameLine();
			b = tile.IsAntimagic;
			if (ImGui.Checkbox("Anti-magic", ref b)) tile.IsAntimagic = b;

			var ld = MapCreator.LevelData[level - 1];
			TexturePick("Floor " + tile.FloorTexture + " (texture " + Safe(ld.FloorTextures, tile.FloorTexture) + ")",
				MapCreator.GetFloorTextureFromIndex(tile.FloorTexture, level), ui.CreateTileFloorExplorer);
			TexturePick("Wall " + tile.WallTexture + " (texture " + Safe(ld.WallTextures, tile.WallTexture) + ")",
				MapCreator.GetWallTextureFromIndex(tile.WallTexture, level), ui.CreateTileWallExplorer);

			var objects = tile.GetTileObjects();
			if (objects != null && objects.Count > 0 && ImGui.CollapsingHeader("Objects on the tile (" + objects.Count + ")", ImGuiTreeNodeFlags.DefaultOpen))
				ObjectLinks("tile", objects);
		}

		static string Safe(int[] a, int i) { return a != null && i >= 0 && i < a.Length ? a[i].ToString() : "?"; }

		void TileChanged(MapTileScript mts)
		{
			ui.MapCreator.UpdateTileAndNeighbours(ui.CurrentLevel, mts, false);
			ZoneTools.TileEdited(mts.MapTile);
		}

		void ObjectLinks(string id, List<StaticObject> list)
		{
			WithId(id, () =>
			{
				for (int i = 0; i < list.Count; i++)
				{
					var o = list[i];
					if (!o) continue;
					if (ImGui.Selectable(o.GetFullName() + "##" + i))
						ItemFinderWindow.GoTo(ui, o);
				}
			});
		}

		static void IntField(string label, int value, int min, int max, Action<int> set)
		{
			int v = value;
			if (ImGui.InputInt(label, ref v))
			{
				v = Mathf.Clamp(v, min, max);
				if (v != value) set(v);
			}
		}

		// ---- Objects on this level ----

		void DrawObjectsWindow()
		{
			if (!Loaded) { Dim("Nothing loaded."); return; }
			int level = ui.CurrentLevel;
			var ld = MapCreator.LevelData[level - 1];
			// the labels are rebuilt twice a second, not every frame
			if (objectListLevel != level || Time.unscaledTime - objectListTime > 0.5f)
			{
				objectListLevel = level;
				objectListTime = Time.unscaledTime;
				objectList.Clear();
				for (int i = 0; i < ld.Objects.Length; i++)
				{
					var o = ld.Objects[i];
					if (!o) continue;
					objectList.Add(new KeyValuePair<StaticObject, string>(o, i + ": " + o.GetFullName() + "  [" + DataReader.ToHex(o.ObjectID) + "]"));
				}
			}
			ImGui.SetNextItemWidth(-1);
			ImGui.InputTextWithHint("##filter", "Filter by name, number or item id", ref objectFilter, 64);
			Dim(objectList.Count + " objects, " + ld.ActiveMobs + " active creatures");
			var selected = SelectedObject();
			string filter = (objectFilter ?? "").Trim();
			Child("list", new V2(0, 0), () =>
			{
				for (int i = 0; i < objectList.Count; i++)
				{
					var e = objectList[i];
					if (filter.Length > 0 && e.Value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
					if (ImGui.Selectable(e.Value + "##o" + i, e.Key == selected))
						ItemFinderWindow.GoTo(ui, e.Key);
				}
			});
		}

		// ---- Under the mouse ----

		void DrawHoverWindow()
		{
			if (BlockedBy != null) Dim("Classic interface under the pointer: " + BlockedBy);
			if (!Loaded) { Dim("Nothing loaded."); return; }
			var so = ui.StaticObjectPanelObject && ui.StaticObjectPanelObject.activeSelf ? StaticObjectPanel.LastObject : null;
			if (so)
			{
				ObjectImage(so, 32);
				ImGui.SameLine();
				ImGui.BeginGroup();
				T(so.GetFullName());
				Dim(string.Format("item 0x{0:X3}, object {1}", so.ObjectID, so.CurrentAdress));
				Dim(string.Format("quality {0}, owner {1}, {2} {3}", so.Quality, so.Owner, so.IsQuantity ? "quantity" : "link", so.Special));
				ImGui.EndGroup();
				return;
			}
			var tile = ui.TilePanelObject && ui.TilePanelObject.activeSelf ? TilePanel.LastTile : null;
			if (tile)
			{
				int level = TilePanel.LastTileLevel;
				T(string.Format("Tile ({0}, {1}): {2}, height {3}", tile.Position.x, tile.Position.y, tile.TileType, tile.FloorHeight));
				if (tile.IsDoor || tile.IsAntimagic) Dim((tile.IsDoor ? "door bit " : "") + (tile.IsAntimagic ? "anti-magic" : ""));
				Image(MapCreator.GetFloorTextureFromIndex(tile.FloorTexture, level), 32);
				ImGui.SameLine();
				Image(MapCreator.GetWallTextureFromIndex(tile.WallTexture, level), 32);
				var objects = tile.GetTileObjects();
				if (objects != null && objects.Count > 0)
				{
					Dim(objects.Count + " object(s):");
					foreach (var o in objects)
						if (o) T("  " + o.GetFullName());
				}
				return;
			}
			Dim("Point at a tile or an object on the map.");
		}

		// ---- pictures ----

		static void Image(Texture2D tex, float size)
		{
			if (!tex) { ImGui.Dummy(new V2(size, size)); return; }
			ImGui.Image(UImGui.UImGuiUtility.GetTextureId(tex), new V2(size, size));
		}

		/// <summary>An object's sprite: a rectangle of the item sheet. The package's shader flips
		/// V, so ImGui's top-left UV is 1 minus Unity's top.</summary>
		static void ObjectImage(StaticObject so, float size)
		{
			Sprite s = MapCreator.GetObjectSpriteFromID(so);
			if (!s || !s.texture) { ImGui.Dummy(new V2(size, size)); return; }
			var t = s.texture;
			Rect r = s.textureRect;
			var uv0 = new V2(r.xMin / t.width, 1f - r.yMax / t.height);
			var uv1 = new V2(r.xMax / t.width, 1f - r.yMin / t.height);
			float scale = size / Mathf.Max(r.width, r.height);
			ImGui.Image(UImGui.UImGuiUtility.GetTextureId(t), new V2(r.width * scale, r.height * scale), uv0, uv1);
		}
	}
}
#endif
