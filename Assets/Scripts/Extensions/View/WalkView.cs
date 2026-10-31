using System;
using System.Collections.Generic;
using System.IO;
using UltimateEditor.Uw1;
using UltimateEditor.Uw1.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.View
{
	/// <summary>
	/// Walking round the shown level at eye height, to check heights and placement: the level
	/// built as the game builds it (WalkGeometry), objects drawn the game's way (WalkObjects):
	/// sprites facing the viewer, levers, switches, writings and wall pictures flat on their
	/// walls, doors, furniture and the like as the built-in 3D models of UW.EXE / UW2.EXE.
	/// The map's camera and mouse are off meanwhile; Esc goes back. A snapshot: R rebuilds it.
	/// </summary>
	public sealed class WalkView : MonoBehaviour
	{
		public static WalkView Active { get; private set; }

		/// <summary>Where the walk world is built, far from the map's own geometry.</summary>
		static readonly Vector3 Origin = new Vector3(10000f, 0f, 10000f);
		const float Radius = 0.15f, MaxRise = 0.3f, SpriteTilesPerPixel = 1f / 64f, CritterHeight = 0.6f;

		UIManager ui;
		Camera mapCamera, cam;
		Behaviour inputManager, cameraManager;
		GameObject world, hudGO;
		Text hud;
		int level;
		bool uw2;
		MapTile[,] tiles;
		Vector3 pos;            // the eye, in tiles
		float yaw, pitch, floorY, eye = 0.6f, hudTimer;
		bool fly, ceilingOn = true, dark = true;
		readonly List<Transform> billboards = new List<Transform>();
		readonly List<Mesh> meshes = new List<Mesh>();
		readonly Dictionary<Texture, Material> materials = new Dictionary<Texture, Material>();
		Material baseMaterial;
		Uw1ExeModels models;
		Color[] palette;
		int[] render;
		string note;

		/// <summary>Starts walking on the shown level: at the selected tile when there is one on
		/// it, else at the middle of the map view, else at the player's start.</summary>
		public static void Enter()
		{
			var ui = UiKit.FindUIManager();
			if (!ui || !MapCreator.IsLoaded) throw new InvalidOperationException("Open a game first.");
			if (Active) return;
			var go = new GameObject("Walk view");
			var w = go.AddComponent<WalkView>();
			try { w.Begin(ui); }
			catch { Destroy(go); throw; }
		}

		void Begin(UIManager ui)
		{
			this.ui = ui;
			level = ui.CurrentLevel;
			uw2 = LevArk.Game.Kind == UltimateEditor.Games.GameKind.UW2;
			var ld = MapCreator.LevelData[level - 1];
			tiles = ld.MapTiles;
			LoadGameData();

			Vector2Int start = PickStart();
			MapTile st = tiles[start.x, start.y];
			pos = new Vector3(start.x + 0.5f, 0f, start.y + 0.5f);
			floorY = WalkGeometry.FloorAt(st, 0.5f, 0.5f);
			pos.y = floorY + eye;

			// the map stops listening and drawing
			mapCamera = Camera.main;
			inputManager = ui.InputManager;
			cameraManager = FindObjectOfType<CameraManager>();
			if (inputManager) inputManager.enabled = false;
			if (cameraManager) cameraManager.enabled = false;
			if (mapCamera) mapCamera.enabled = false;

			var camGO = new GameObject("Walk camera");
			camGO.transform.SetParent(transform, false);
			cam = camGO.AddComponent<Camera>();
			cam.fieldOfView = 70f;
			cam.nearClipPlane = 0.02f;
			cam.farClipPlane = 80f;
			cam.clearFlags = CameraClearFlags.SolidColor;
			cam.backgroundColor = Color.black;
			cam.depth = mapCamera ? mapCamera.depth + 1 : 0;

			ApplyDarkness();

			Build();
			MakeHud();
			Active = this;
		}

		void LoadGameData()
		{
			baseMaterial = Resources.Load<Material>("WalkUnlit");
			if (!baseMaterial)
				baseMaterial = new Material(Shader.Find("Sprites/Default"));
			var td = MapCreator.TextureData;
			if (td != null && td.Palettes != null && td.Palettes.Count > 0)
				palette = td.Palettes[0];
			string comobj = ExtensionsMenu.FindDataFile("COMOBJ.DAT");
			render = comobj != null ? WalkObjects.ReadRenderTypes(File.ReadAllBytes(comobj)) : null;
			if (render == null) note = "COMOBJ.DAT not found: every object is drawn as a sprite.";
			try
			{
				string exe = UltimateEditor.Games.StartPosition.ExePath(ExtensionsMenu.DataFolder, LevArk.Game.Kind);
				if (exe != null) models = Uw1ExeModels.Load(File.ReadAllBytes(exe));
			}
			catch (Exception e) { models = null; note = "The built-in models could not be read (" + e.Message + "): those objects are drawn as sprites."; }
			if (models == null && note == null) note = (uw2 ? "UW2.EXE" : "UW.EXE") + " not found next to DATA: models are drawn as sprites.";
		}

		Vector2Int PickStart()
		{
			var sel = TileProperties.LastSelected;
			if (sel != null && TileProperties.LastSelectedLevel == level && sel.TileType != TileType.Solid)
				return sel.Position;
			if (Camera.main)
			{
				float ts = MapCreator.TileSize * 2f;
				var c = Camera.main.transform.position;
				var near = NearestOpen(Mathf.FloorToInt(c.x / ts), Mathf.FloorToInt(c.y / ts));
				if (near.x >= 0) return near;
			}
			if (MapCreator.StartLevel == level)
				return MapCreator.StartSquare;
			var any = NearestOpen(32, 32);
			if (any.x < 0) throw new InvalidOperationException("This level has no open square to stand on.");
			return any;
		}

		Vector2Int NearestOpen(int cx, int cy)
		{
			for (int r = 0; r < 64; r++)
				for (int dx = -r; dx <= r; dx++)
					for (int dy = -r; dy <= r; dy++)
					{
						if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
						int x = cx + dx, y = cy + dy;
						if (x < 0 || y < 0 || x > 63 || y > 63) continue;
						var t = tiles[x, y];
						if (t != null && t.TileType == TileType.Open) return new Vector2Int(x, y);
					}
			return new Vector2Int(-1, -1);
		}

		#region Building

		void Build()
		{
			if (world) Destroy(world);
			billboards.Clear();
			foreach (var old in meshes) if (old) Destroy(old);
			meshes.Clear();
			world = new GameObject("Walk world");
			world.transform.SetParent(transform, false);
			world.transform.position = Origin;

			foreach (var kv in WalkGeometry.Build(tiles, ceilingOn && HasCeiling()))
			{
				Texture2D tex = SurfaceTexture(kv.Key);
				AddMesh(kv.Key, kv.Value, tex, true);
			}
			BuildObjects();
		}

		/// <summary>UW2's Ethereal Void (world 8, but for level 68) has no ceiling (uw2 GRIDDB.C).</summary>
		bool HasCeiling()
		{
			return !(uw2 && (level - 1) / 8 == 8 && level != 68);
		}

		Texture2D SurfaceTexture(string key)
		{
			try
			{
				if (key == "ceiling")
					// UW1 draws the ceiling with the level's floor texture 9 (GRIDDB.C grdb_elem);
					// UW2's is not known here: plain grey
					return uw2 ? null : Repeat(MapCreator.GetFloorTextureFromIndex(9, level));
				int colon = key.IndexOf(':');
				int n = int.Parse(key.Substring(colon + 1));
				if (key.StartsWith("floor")) return Repeat(MapCreator.GetFloorTextureFromIndex(n, level));
				if (key.StartsWith("wall")) return Repeat(MapCreator.GetWallTextureFromIndex(n, level));
			}
			catch (Exception) { }
			return null;
		}

		static Texture2D Repeat(Texture2D t)
		{
			if (t) { t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Point; }
			return t;
		}

		Material MaterialFor(Texture tex)
		{
			if (!tex) tex = Texture2D.whiteTexture;
			Material m;
			if (!materials.TryGetValue(tex, out m))
			{
				m = new Material(baseMaterial);
				m.mainTexture = tex;
				materials[tex] = m;
			}
			return m;
		}

		GameObject AddMesh(string name, MeshPart p, Texture tex, bool staticWorld, Transform parent = null)
		{
			if (p == null || p.V.Count == 0) return null;
			var go = new GameObject(name);
			go.transform.SetParent(parent ? parent : world.transform, false);
			var mesh = new Mesh();
			meshes.Add(mesh);
			if (p.V.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			mesh.SetVertices(p.V);
			mesh.SetUVs(0, p.UV);
			mesh.SetColors(p.C);
			mesh.SetTriangles(p.T, 0);
			mesh.RecalculateBounds();
			go.AddComponent<MeshFilter>().sharedMesh = mesh;
			var mr = go.AddComponent<MeshRenderer>();
			mr.sharedMaterial = MaterialFor(tex);
			mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mr.receiveShadows = false;
			return go;
		}

		void BuildObjects()
		{
			var objectsGO = new GameObject("Objects");
			objectsGO.transform.SetParent(world.transform, false);
			// one mesh for each texture the models use, one for their plain faces
			var plain = new MeshPart();
			var byTexture = new Dictionary<Texture2D, MeshPart>();
			Func<Texture2D, MeshPart> texPart = t =>
			{
				if (!t) return null;
				MeshPart mp;
				if (!byTexture.TryGetValue(t, out mp)) byTexture[t] = mp = new MeshPart();
				return mp;
			};
			for (int x = 0; x < 64; x++)
				for (int y = 0; y < 64; y++)
				{
					var list = tiles[x, y] != null ? tiles[x, y].GetTileObjects() : null;
					if (list == null) continue;
					foreach (var so in list)
					{
						if (!so || so.IsInvisible) continue;
						try { AddObject(so, x, y, objectsGO.transform, plain, texPart); }
						catch (Exception e) { Debug.LogWarning("Walk view: " + so.Name + " [" + so.CurrentAdress + "]: " + e.Message); }
					}
				}
			AddMesh("Model faces", plain, null, true, objectsGO.transform);
			foreach (var kv in byTexture)
				AddMesh("Model faces " + kv.Key.name, kv.Value, kv.Key, true, objectsGO.transform);
		}

		/// <summary>An object's place in tiles: fine x and y are the middles of eighths of the tile (a wall
		/// lever at fine 0 or 7 then sits 1/16 from the wall, where its model's face is); z is in
		/// units of an eighth of a floor step (the floor of height h is at z = 8h).</summary>
		static Vector3 Place(StaticObject so, int x, int y)
		{
			return new Vector3(x + (so.XPos + 0.5f) / 8f, so.ZPos / 8f * WalkGeometry.Step, y + (so.YPos + 0.5f) / 8f);
		}

		void AddObject(StaticObject so, int x, int y, Transform parent, MeshPart plain, Func<Texture2D, MeshPart> texPart)
		{
			int model;
			var kind = WalkObjects.Classify(so.ObjectID, render, uw2, out model);
			if (kind == WalkObjects.Kind.None) return;
			Vector3 at = Place(so, x, y);
			float heading = so.Direction * 45f;
			Uw1ExeModels.Model m = models != null && model >= 0 && model < models.Models.Count ? models.Models[model] : null;
			bool usable = m != null && m.Faces.Count > 0;
			switch (kind)
			{
				case WalkObjects.Kind.Door:
					// the frame (model 1) in the square's wall texture, up to the ceiling; its
					// opening runs from x -0.1875 to 0.3125, where the leaf (0.5 wide, from its
					// hinge at x 0) goes
					if (models != null && models.Models.Count > 1)
						WalkObjects.AddModel(models.Models[1], at, heading, palette, plain, texPart(Repeat01(SafeWall(tiles[x, y].WallTexture))), Color.white, WalkGeometry.Ceiling - at.y);
					var leaf = usable ? m : (models != null && models.Models.Count > 0xE ? models.Models[0xE] : null);
					if (leaf != null && leaf.Faces.Count > 0)
					{
						bool open = so.ObjectID >= 0x148;
						bool portcullis = (so.ObjectID & 7) == 6;
						Vector3 hinge = at + Quaternion.Euler(0, heading, 0) * new Vector3(-0.1875f, 0, 0);
						float leafHeading = heading;
						if (open && portcullis)
							hinge.y += 0.75f;
						else if (open)
							leafHeading -= 90f;   // swung open about its hinge
						WalkObjects.AddModel(leaf, hinge, leafHeading, palette, plain, texPart(Repeat01(so.GetDoorTexture())), Color.white);
					}
					return;
				case WalkObjects.Kind.TmapLever:
					if (usable)
					{
						var t = MapCreator.TextureData.Levers;
						int i = so.ObjectID & 0xF;
						WalkObjects.AddModel(m, at, heading, palette, plain, texPart(i < t.Count ? Repeat01(t.Textures[i]) : null), Color.white);
						return;
					}
					break;
				case WalkObjects.Kind.TmapWall:
					if (usable)
					{
						WalkObjects.AddModel(m, at, heading, palette, plain, texPart(Repeat01(SafeWall(so.Owner))), Color.white);
						return;
					}
					break;
				case WalkObjects.Kind.Model:
					if (usable)
					{
						bool floorTex;
						int tex = WalkObjects.ModelTexture(model, so.Flags, uw2, out floorTex);
						Texture2D t = null;
						if (tex >= 0)
							t = floorTex ? SafeFloor(tex) : TmObj(tex);
						// the pillar, like the door frame, reaches the ceiling in the game
						WalkObjects.AddModel(m, at, heading, palette, plain, texPart(Repeat01(t)), Color.white, model == 0x0A ? WalkGeometry.Ceiling - at.y : -1f);
						return;
					}
					break;
			}
			AddSprite(so, at, parent, kind == WalkObjects.Kind.Critter);
		}

		Texture2D SafeWall(int index) { try { return MapCreator.GetWallTextureFromIndex(index, level); } catch (Exception) { return null; } }
		Texture2D SafeFloor(int index) { try { return MapCreator.GetFloorTextureFromIndex(index, level); } catch (Exception) { return null; } }

		/// <summary>A texture across a model face fits it once: clamped, sharp.</summary>
		static Texture2D Repeat01(Texture2D t)
		{
			if (t) t.filterMode = FilterMode.Point;
			return t;
		}

		/// <summary>The renderer's object textures: 0-0x0F TMFLAT (levers, switches), from 0x10
		/// TMOBJ (writings, the bridge...), from 0x30 the doors (gfx/LOADGR.C first_tmobj).</summary>
		static Texture2D TmObj(int t)
		{
			var td = MapCreator.TextureData;
			if (td == null) return null;
			if (t < 0x10) return t < td.Levers.Count ? td.Levers.Textures[t] : null;
			if (t < 0x30) return t - 0x10 < td.Other.Count ? td.Other.Textures[t - 0x10] : null;
			return t - 0x30 < td.Doors.Count ? td.Doors.Textures[t - 0x30] : null;
		}

		void AddSprite(StaticObject so, Vector3 at, Transform parent, bool critter)
		{
			Sprite s = MapCreator.GetObjectSpriteFromID(so);
			if (!s || !s.texture) return;
			Texture2D tex = s.texture;
			tex.filterMode = FilterMode.Point;
			float pw = s.rect.width, ph = s.rect.height;
			float scale = critter ? CritterHeight / Math.Max(1f, ph) : SpriteTilesPerPixel;
			float w = pw * scale, h = ph * scale;
			var p = new MeshPart();
			float u0 = s.rect.xMin / tex.width, u1 = s.rect.xMax / tex.width, v0 = s.rect.yMin / tex.height, v1 = s.rect.yMax / tex.height;
			p.Quad(new Vector3(-w / 2, 0, 0), new Vector3(w / 2, 0, 0), new Vector3(w / 2, h, 0), new Vector3(-w / 2, h, 0),
				new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1), Color.white);
			var go = AddMesh(so.Name, p, tex, false, parent);
			if (!go) return;
			go.transform.localPosition = at;
			billboards.Add(go.transform);
		}

		#endregion

		#region Moving

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.Escape)) { Exit(); return; }
			if (Typing()) return;
			if (Input.GetKeyDown(KeyCode.F)) { fly = !fly; if (!fly) Settle(); }
			if (Input.GetKeyDown(KeyCode.G)) { ceilingOn = !ceilingOn; Build(); }
			if (Input.GetKeyDown(KeyCode.L)) { dark = !dark; ApplyDarkness(); }
			if (Input.GetKeyDown(KeyCode.R)) Build();

			if (Input.GetMouseButton(1))
			{
				yaw += Input.GetAxis("Mouse X") * 3f;
				pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 3f, -85f, 85f);
			}
			float turn = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
			yaw += turn * 90f * Time.deltaTime;

			float speed = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 6f : 2f) * Time.deltaTime;
			float fwd = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
			float side = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
			float up = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
			float rad = yaw * Mathf.Deg2Rad;
			Vector3 f = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)), r = new Vector3(Mathf.Cos(rad), 0, -Mathf.Sin(rad));
			Vector3 move = (f * fwd + r * side) * speed;
			if (fly)
			{
				pos += move;
				pos.y += up * speed;
			}
			else
			{
				eye = Mathf.Clamp(eye + up * speed * 0.5f, 0.1f, 2f);
				TryMove(move.x, 0);
				TryMove(0, move.z);
				pos.y = Mathf.Min(floorY + eye, WalkGeometry.Ceiling - 0.05f);
			}
			cam.transform.position = Origin + pos;
			cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0);

			hudTimer -= Time.deltaTime;
			if (hudTimer <= 0) { hudTimer = 0.2f; UpdateHud(); }
		}

		void LateUpdate()
		{
			if (!cam) return;
			// sprites face the viewer, standing upright, as in the game
			var rot = Quaternion.Euler(0, yaw, 0);
			foreach (var b in billboards)
				if (b) b.rotation = rot;
		}

		void TryMove(float dx, float dz)
		{
			if (dx == 0 && dz == 0) return;
			float f;
			if (WalkGeometry.CanStand(tiles, pos.x + dx, pos.z + dz, Radius, floorY, MaxRise, out f))
			{
				pos.x += dx; pos.z += dz;
				floorY = f;
			}
		}

		/// <summary>Back on the floor after flying, if the eye is over an open square.</summary>
		void Settle()
		{
			int tx = Mathf.FloorToInt(pos.x), tz = Mathf.FloorToInt(pos.z);
			if (tx < 0 || tz < 0 || tx > 63 || tz > 63) return;
			var t = tiles[tx, tz];
			if (t != null && WalkGeometry.PointOpen(t.TileType, pos.x - tx, pos.z - tz))
				floorY = WalkGeometry.FloorAt(t, pos.x - tx, pos.z - tz);
			else
				fly = true;   // in rock: stays flying
		}

		static bool Typing()
		{
			var es = UnityEngine.EventSystems.EventSystem.current;
			var sel = es ? es.currentSelectedGameObject : null;
			return sel && sel.GetComponent<InputField>();
		}

		/// <summary>The darkness: surfaces fade to black from 2 to 12 tiles away (the shader's
		/// globals; a rough stand-in for the game's distance shading, SHADES.DAT).</summary>
		void ApplyDarkness()
		{
			Shader.SetGlobalFloat("_WalkFadeStart", 2f);
			Shader.SetGlobalFloat("_WalkFadeEnd", 12f);
			Shader.SetGlobalFloat("_WalkDark", dark ? 1f : 0f);
		}

		#endregion

		#region HUD and leaving

		void MakeHud()
		{
			var rt = UiKit.Rect("Walk view HUD", ui.transform);
			hudGO = rt.gameObject;
			rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
			rt.pivot = new Vector2(0.5f, 1f);
			rt.anchoredPosition = new Vector2(0, -4);
			rt.sizeDelta = new Vector2(760, 76);
			hudGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
			var col = UiKit.Column(rt, 2);
			UiKit.Stretch(col, 8, 8, 4, 4);
			hud = UiKit.Paragraph(col, "", 11, UiKit.TextColor);
			var row = UiKit.Row(col, 20);
			UiKit.Button(row, "Back to the map (Esc)", Exit, 160, 20, UiKit.AccentColor);
			UiKit.Button(row, "Rebuild (R)", Build, 90, 20);
			hudGO.transform.SetAsLastSibling();
			UpdateHud();
		}

		void UpdateHud()
		{
			if (!hud) return;
			int tx = Mathf.FloorToInt(pos.x), tz = Mathf.FloorToInt(pos.z);
			string where = tx >= 0 && tz >= 0 && tx < 64 && tz < 64 ? string.Format("square ({0}, {1}), floor height {2}", tx, tz, tiles[tx, tz].FloorHeight) : "outside the map";
			hud.text = string.Format("{0}: {1}{2}. W/S/A/D or arrows move (Shift faster), right mouse looks, Q/E {3}, F {4}, G ceiling {5}, L darkness {6}.{7}",
				LevArk.Game.LevelName(level), where, fly ? ", flying" : "", fly ? "down/up" : "eye height", fly ? "walks" : "flies",
				ceilingOn ? "on" : "off", dark ? "on" : "off", note != null ? "\n" + note : "");
		}

		public void Exit()
		{
			if (Active != this) return;
			Active = null;
			if (mapCamera)
			{
				mapCamera.enabled = true;
				// the map looks at where the walk ended
				float ts = MapCreator.TileSize * 2f;
				mapCamera.transform.position = new Vector3(pos.x * ts, pos.z * ts, mapCamera.transform.position.z);
			}
			if (inputManager) inputManager.enabled = true;
			if (cameraManager) cameraManager.enabled = true;
			if (hudGO) Destroy(hudGO);
			foreach (var m in materials.Values) if (m) Destroy(m);
			foreach (var mesh in meshes) if (mesh) Destroy(mesh);
			Destroy(gameObject);
		}

		void OnDestroy()
		{
			if (Active == this) Active = null;
		}

		#endregion
	}
}
