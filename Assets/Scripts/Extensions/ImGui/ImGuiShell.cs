#if UIMGUI_SHELL
using System;
using System.Collections.Generic;
using System.IO;
using ImGuiNET;
using UltimateEditor.Games;
using UltimateEditor.Uw1.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using V2 = UnityEngine.Vector2;
using V4 = UnityEngine.Vector4;

namespace UltimateEditor.Uw1.ImGuiUI
{
	/// <summary>
	/// A Dear ImGui interface around the map (first slice, UW1 data only): a menu bar, and
	/// dockable windows for the level and mode, the selection (tile or object), the level's
	/// objects and what is under the mouse. The editor's uGUI panels it replaces are hidden
	/// (CanvasGroup), not removed: they still run, so the rest of the editor keeps working, and
	/// dialogs not ported yet (texture pickers, create menus, conversations, Tools windows)
	/// open the old way on top. F11 or View > Classic interface switches back; UW2 data always
	/// uses the classic interface for now.
	///
	/// Needs the package com.psydack.uimgui v4.1.0 and the scripting define UIMGUI_SHELL.
	/// </summary>
	public sealed partial class ImGuiShell : MonoBehaviour
	{
		const string PrefClassic = "imgui.classic", PrefFontSize = "imgui.fontsize13", PrefSegoe = "imgui.segoe", PrefTrial = "imgui.fonttrial";
		static readonly float[] FontSizes = { 13f, 16f, 20f, 26f };

		static bool hooked;

		UIManager ui;
		UImGui.UImGui host;
		bool classic;
		float fontSize;
		bool segoe;
		float pendingFontSize = -1;
		bool styleDirty = true;
		GameKind? styledFor;
		bool? classicApplied;
		bool classicObjectEditorApplied;
		string lastError;

		bool showLevelWindow = true, showSelectionWindow = true, showObjectsWindow = true, showHoverWindow = true, showDemo;
		bool classicObjectEditor;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Install()
		{
			// File > Open reloads the scene to open another game: install again then
			if (!hooked)
			{
				hooked = true;
				UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => Install();
			}
			var uim = UiKit.FindUIManager();
			if (!uim || uim.GetComponent<ImGuiShell>()) return;
			uim.gameObject.AddComponent<ImGuiShell>();
		}

		void Start()
		{
			ui = GetComponent<UIManager>();
			classic = PlayerPrefs.GetInt(PrefClassic, 0) == 1;
			fontSize = PlayerPrefs.GetFloat(PrefFontSize, 13f);
			// building a TrueType font atlas crashed the Unity editor natively on one machine; a flag
			// is saved before trying it, and still being set at the next start means it crashed
			if (PlayerPrefs.GetInt(PrefTrial, 0) == 1)
			{
				PlayerPrefs.SetInt(PrefTrial, 0);
				PlayerPrefs.SetInt(PrefSegoe, 0);
				PlayerPrefs.Save();
				Debug.LogWarning("Dear ImGui: the last start with the Segoe UI font did not finish; using the built-in font from now on (View > Text size > Segoe UI to retry).");
			}
			// the Segoe UI atlas crashed Unity natively again when chosen: it stays off (and a saved
			// choice from the earlier build is dropped) until the cause is found
			segoe = false;
			PlayerPrefs.DeleteKey(PrefSegoe);
			try
			{
				if (segoe) { PlayerPrefs.SetInt(PrefTrial, 1); PlayerPrefs.Save(); }
				host = ImGuiHost.Create(Camera.main, fontSize, segoe, OnLayout);
				if (segoe) { PlayerPrefs.SetInt(PrefTrial, 0); PlayerPrefs.Save(); }
			}
			catch (Exception e)
			{
				// the classic interface stays as it is
				Debug.LogError("The Dear ImGui interface could not start; using the classic one. " + e);
				host = null;
			}
			ApplyClassicVisibility();
		}

		void OnDestroy()
		{
			UiInput.ImGuiWantsMouse = false;
			UiInput.ImGuiWantsKeyboard = false;
		}

		bool Loaded { get { return ui && ui.MapCreator && ui.MapCreator.IsInitialized; } }
		bool Uw2Loaded { get { return LevArk.Path != null && LevArk.Game.Kind == GameKind.UW2; } }

		/// <summary>The new interface is shown: it started, the user did not pick the classic one, and the data is not UW2's.</summary>
		bool ShellOn { get { return host != null && !classic && !Uw2Loaded; } }

		void SetClassic(bool value)
		{
			classic = value;
			PlayerPrefs.SetInt(PrefClassic, value ? 1 : 0);
			ApplyClassicVisibility();
		}

		void Update()
		{
			// a new font size rebuilds the font atlas, outside the ImGui frame
			if (pendingFontSize > 0 && host)
			{
				fontSize = pendingFontSize;
				pendingFontSize = -1;
				PlayerPrefs.SetFloat(PrefFontSize, fontSize);
				if (segoe)
				{
					PlayerPrefs.SetInt(PrefTrial, 1); PlayerPrefs.Save();
					ImGuiHost.SetFont(host, fontSize);
					host.Reload();
					PlayerPrefs.SetInt(PrefTrial, 0); PlayerPrefs.Save();
				}
				styleDirty = true;
			}
			ApplyClassicVisibility();

			if (host == null || Uw2Loaded || UiInput.ImGuiWantsKeyboard)
				return;
			if (Input.GetKeyDown(KeyCode.F11))
				SetClassic(!classic);
			if (!ShellOn)
				return;
			bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
			if (ctrl && Input.GetKeyDown(KeyCode.O)) ui.CreateFileExplorer();
			if (ctrl && Input.GetKeyDown(KeyCode.S) && Loaded) ui.SaveData();
			if (ctrl && Input.GetKeyDown(KeyCode.Z) && Loaded) ui.Undo();
		}

		// ---- the classic panels ----

		/// <summary>Hides the uGUI panels the shell replaces (or shows them again), through a
		/// CanvasGroup so that they keep running for the code that fills them.</summary>
		void ApplyClassicVisibility()
		{
			if (!ui) return;
			bool showClassic = !ShellOn;
			if (classicApplied == showClassic && classicObjectEditorApplied == classicObjectEditor) return;
			classicApplied = showClassic;
			classicObjectEditorApplied = classicObjectEditor;
			hiddenPanels.Clear();
			foreach (var g in new[] { ui.TopBarGO, ui.TopBarGO && ui.TopBarGO.transform.parent ? ui.TopBarGO.transform.parent.gameObject : null,
				ui.ModePanel, ui.InfoPanel, ui.TilePanelObject, ui.StaticObjectPanelObject, ui.TilePropertiesObject })
				if (g) hiddenPanels.Add(g.transform);
			Show(ui.TopBarGO, showClassic);
			Show(ui.ModePanel, showClassic);
			Show(ui.InfoPanel, showClassic);
			Show(ui.TilePanelObject, showClassic);
			Show(ui.StaticObjectPanelObject, showClassic);
			Show(ui.TilePropertiesObject, showClassic);
			Show(ui.ObjectProperties, showClassic || classicObjectEditor);
		}

		static void Show(GameObject go, bool visible)
		{
			if (!go) return;
			var g = go.GetComponent<CanvasGroup>();
			if (!g) g = go.AddComponent<CanvasGroup>();
			g.alpha = visible ? 1f : 0f;
			g.interactable = visible;
			g.blocksRaycasts = visible;
		}

		// ---- the frame ----

		void OnLayout(UImGui.UImGui h)
		{
			var io = ImGui.GetIO();
			// a uGUI dialog over the shell gets the mouse, not the ImGui window under it
			BlockedBy = ShellOn ? UguiUnderPointer() : null;
			bool overUgui = ShellOn ? BlockedBy != null : EventSystem.current && EventSystem.current.IsPointerOverGameObject();
			if (overUgui) io.ConfigFlags |= ImGuiConfigFlags.NoMouse;
			else io.ConfigFlags &= ~ImGuiConfigFlags.NoMouse;
			// arrow keys move the map; ImGui's keyboard navigation would take them too
			io.ConfigFlags &= ~ImGuiConfigFlags.NavEnableKeyboard;
			// the built-in font is 13 px: the text size menu scales it (the Segoe atlas is rebuilt instead)
			io.FontGlobalScale = segoe ? 1f : fontSize / 13f;
			try
			{
				ApplyStyle();
				if (ShellOn) DrawShell();
				else if (!Uw2Loaded) DrawClassicHint();
			}
			catch (Exception e)
			{
				Report(e);
			}
			finally
			{
				UiInput.ImGuiWantsMouse = io.WantCaptureMouse;
				UiInput.ImGuiWantsKeyboard = io.WantTextInput;
			}
		}

		/// <summary>Name of the classic-interface object under the pointer while it keeps the mouse from ImGui (shown in the "Under the mouse" window).</summary>
		public static string BlockedBy;

		/// <summary>The classic panels the shell hides (set by ApplyClassicVisibility): whatever they still catch does not count.</summary>
		static readonly System.Collections.Generic.List<Transform> hiddenPanels = new System.Collections.Generic.List<Transform>();

		static bool UnderHiddenPanel(Transform t)
		{
			foreach (var h in hiddenPanels)
				if (h && (t == h || t.IsChildOf(h))) return true;
			return false;
		}

		static string UguiUnderPointer()
		{
			try
			{
				var data = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
				var hits = new System.Collections.Generic.List<RaycastResult>();
				EventSystem.current.RaycastAll(data, hits);
				foreach (var hit in hits)
				{
					var t = hit.gameObject.transform;
					if (UnderHiddenPanel(t)) continue;
					string path = t.name;
					for (int i = 0; i < 3 && t.parent; i++) { t = t.parent; path = t.name + "/" + path; }
					return path;
				}
				return null;
			}
			catch (Exception) { return "(unknown)"; }
		}

		void Report(Exception e)
		{
			string msg = e.GetType().Name + ": " + e.Message;
			if (msg == lastError) return;
			lastError = msg;
			Debug.LogException(e);
		}

		void DrawShell()
		{
			DrawMenuBar();
			var vp = ImGui.GetMainViewport();
			// windows dock to the screen's edges; the map shows through the middle
			ImGui.DockSpaceOverViewport(vp, ImGuiDockNodeFlags.PassthruCentralNode);
			V2 at = vp.WorkPos, size = vp.WorkSize;
			float side = 330;

			ImGui.SetNextWindowPos(new V2(at.x + 8, at.y + 8), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new V2(side, 340), ImGuiCond.FirstUseEver);
			Window("Level and mode", ref showLevelWindow, DrawLevelWindow);

			ImGui.SetNextWindowPos(new V2(at.x + 8, at.y + 356), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new V2(side, Math.Max(200, size.y - 364)), ImGuiCond.FirstUseEver);
			Window("Objects on this level", ref showObjectsWindow, DrawObjectsWindow);

			ImGui.SetNextWindowPos(new V2(at.x + size.x - side - 8, at.y + 8), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new V2(side, Math.Max(300, size.y * 0.62f)), ImGuiCond.FirstUseEver);
			Window("Selection", ref showSelectionWindow, DrawSelectionWindow);

			ImGui.SetNextWindowPos(new V2(at.x + size.x - side - 8, at.y + Math.Max(300, size.y * 0.62f) + 16), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new V2(side, Math.Max(150, size.y * 0.38f - 24)), ImGuiCond.FirstUseEver);
			Window("Under the mouse", ref showHoverWindow, DrawHoverWindow);

			if (showDemo) ImGui.ShowDemoWindow(ref showDemo);
		}

		/// <summary>With the classic interface on: a small button to come back.</summary>
		void DrawClassicHint()
		{
			var io = ImGui.GetIO();
			ImGui.SetNextWindowPos(new V2(io.DisplaySize.x * 0.5f, io.DisplaySize.y - 6), ImGuiCond.Always, new V2(0.5f, 1f));
			ImGui.SetNextWindowBgAlpha(0.55f);
			var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings
				| ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoDocking;
			bool visible = ImGui.Begin("##classic", flags);
			try
			{
				if (visible && ImGui.SmallButton("New interface (F11)"))
					SetClassic(false);
			}
			finally { ImGui.End(); }
		}

		// ---- menus ----

		void DrawMenuBar()
		{
			if (!ImGui.BeginMainMenuBar()) return;
			try
			{
				Menu("File", () =>
				{
					if (ImGui.MenuItem("Open game...", "Ctrl+O")) ui.CreateFileExplorer();
					if (ImGui.MenuItem("Load the last opened game", "", false, !Loaded && !ui.IsLoading && File.Exists(DataReader.FilePath ?? ""))) ui.LoadData();
					if (ImGui.MenuItem("Save", "Ctrl+S", false, Loaded)) ui.SaveData();
					ImGui.Separator();
					if (ImGui.MenuItem("Exit")) Application.Quit();
				});
				Menu("Edit", () =>
				{
					if (ImGui.MenuItem("Undo", "Ctrl+Z", false, Loaded)) ui.Undo();
				});
				Menu("Levels", () =>
				{
					if (ImGui.MenuItem("Clear levels...", "", false, Loaded)) ui.CreateClearLevelsMenu();
					if (ImGui.MenuItem("Key editor...", "", false, Loaded)) ui.CreateKeyEditor();
					if (ImGui.MenuItem("Mantra editor...", "", false, Loaded)) ui.CreateMantraEditor();
				});
				Menu("Objects", () =>
				{
					if (ImGui.MenuItem("Add object...", "", false, Loaded)) ui.AddObject();
					if (ImGui.MenuItem("Object type properties...", "", false, Loaded)) ui.CreateCommonPropertiesEditor();
					if (ImGui.MenuItem("Wall terrain...", "", false, Loaded)) ui.CreateWallTerrainEditor();
					if (ImGui.MenuItem("Floor terrain...", "", false, Loaded)) ui.CreateFloorTerrainEditor();
					ImGui.Separator();
					if (ImGui.MenuItem("Classic object list...", "", false, Loaded)) ui.CreateObjectListMenu();
				});
				Menu("Graphics", DrawGraphicsMenu);
				Menu("Strings", () =>
				{
					if (ImGui.MenuItem("String list...", "", false, Loaded)) ui.CreateStringBlockList();
				});
				Menu("Conversations", () =>
				{
					if (ImGui.MenuItem("Play a conversation...", "", false, Loaded)) ui.CreatePlayConversationList();
					if (ImGui.MenuItem("Add a conversation...", "", false, Loaded)) ui.CreateAddConversationList();
					if (ImGui.MenuItem("Edit a conversation...", "", false, Loaded)) ui.CreateEditConversationList();
					if (ImGui.MenuItem("Delete a conversation...", "", false, Loaded)) ui.CreateDeleteConversationList();
				});
				Menu("Tools", () =>
				{
					foreach (var e in ExtensionsMenu.Entries())
						if (ImGui.MenuItem(e.Label, "", false, e.NeedsLevels ? Loaded : !string.IsNullOrEmpty(ExtensionsMenu.DataFolder)))
							ExtensionsMenu.Run(ui, e);
				});
				Menu("View", DrawViewMenu);
				Menu("Help", () =>
				{
					if (ImGui.MenuItem("Documentation")) ui.RunDocumentation();
					ImGui.Separator();
					ImGui.TextDisabled("Map: arrow keys move, the zoom axis zooms.");
					ImGui.TextDisabled("Windows dock to the edges and to each other.");
				});

				// the game and level, on the right of the bar
				string status = Loaded
					? LevArk.Game.Name + (GameSession.IsSave ? ", " + GameSession.SaveName : "") + ", " + LevArk.Game.LevelName(ui.CurrentLevel)
					: ui.IsLoading ? "Loading..." : "No game loaded";
				float w = ImGui.CalcTextSize(status).x;
				ImGui.SameLine(Math.Max(ImGui.GetCursorPosX() + 20, ImGui.GetWindowWidth() - w - 16));
				ImGui.TextDisabled(Escape(status));
			}
			finally { ImGui.EndMainMenuBar(); }
		}

		void DrawGraphicsMenu()
		{
			bool l = Loaded;
			if (ImGui.MenuItem("This level's floors...", "", false, l)) ui.CreateTextureExplorer(TextureExplorerType.Level, TextureType.Floor, "Level floors", "Select texture to swap", "Available floors", "Select new texture");
			if (ImGui.MenuItem("This level's walls...", "", false, l)) ui.CreateTextureExplorer(TextureExplorerType.Level, TextureType.Wall, "Level walls", "Select texture to swap", "Available walls", "Select new texture");
			if (ImGui.MenuItem("This level's doors...", "", false, l)) ui.CreateTextureExplorer(TextureExplorerType.Level, TextureType.Door, "Level doors", "Select texture to swap", "Available doors", "Select new texture");
			ImGui.Separator();
			if (ImGui.MenuItem("Floors...", "", false, l)) ui.CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.Floor, "Available floors", "Select texture to swap or add a new texture", "Loaded floors", "Select new texture");
			if (ImGui.MenuItem("Walls...", "", false, l)) ui.CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.Wall, "Available walls", "Select texture to swap or add a new texture", "Loaded walls", "Select new texture");
			if (ImGui.MenuItem("Doors...", "", false, l)) ui.CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.Door, "Available doors", "Select texture to swap or add a new texture", "Loaded doors", "Select new texture");
			if (ImGui.MenuItem("Switches and levers...", "", false, l)) ui.CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.Lever, "Available levers", "Select texture to swap or add a new texture", "Loaded levers", "Select new texture");
			if (ImGui.MenuItem("Other (wall decals)...", "", false, l)) ui.CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.Other, "Available other textures", "Select texture to swap or add a new texture", "Loaded other textures", "Select new texture");
			if (ImGui.MenuItem("Generic portraits...", "", false, l)) ui.CreateGenericHeadExplorer();
			if (ImGui.MenuItem("NPC portraits...", "", false, l)) ui.CreateNPCHeadExplorer();
			if (ImGui.MenuItem("Object graphics...", "", false, l)) ui.CreateObjectGraphicExplorer();
			ImGui.Separator();
			if (ImGui.MenuItem("Export to PNG", "", false, l))
			{
				DataWriter.ExportDefaultTextures();
				ui.SpawnPopupMessage("Exported textures to folder Export.");
			}
		}

		void DrawViewMenu()
		{
			ImGui.MenuItem("Level and mode", "", ref showLevelWindow);
			ImGui.MenuItem("Selection", "", ref showSelectionWindow);
			ImGui.MenuItem("Objects on this level", "", ref showObjectsWindow);
			ImGui.MenuItem("Under the mouse", "", ref showHoverWindow);
			ImGui.Separator();
			Menu("Text size", () =>
			{
				foreach (var s in FontSizes)
					if (ImGui.MenuItem(s.ToString("0") + " px", "", Math.Abs(s - fontSize) < 0.1f))
						pendingFontSize = s;
			});
			if (ImGui.MenuItem("Classic interface", "F11", false)) SetClassic(true);
			ImGui.Separator();
			ImGui.MenuItem("Dear ImGui demo", "", ref showDemo);
		}

		// ---- style ----

		/// <summary>Dear ImGui's dark style, a little rounder, with brown accents for UW1 (as the
		/// editor's own buttons, UiKit.ButtonColor) and blue ones otherwise.</summary>
		void ApplyStyle()
		{
			var game = UiKit.ThemeGame;
			if (!styleDirty && styledFor == game) return;
			styleDirty = false;
			styledFor = game;
			var style = ImGui.GetStyle();
			ImGui.StyleColorsDark(style);
			style.WindowRounding = 4;
			style.FrameRounding = 3;
			style.GrabRounding = 3;
			style.ScrollbarRounding = 6;
			style.TabRounding = 3;
			style.WindowPadding = new V2(8, 8);
			style.FramePadding = new V2(6, 4);
			style.ItemSpacing = new V2(8, 5);
			if (game != GameKind.UW1) return;
			var accent = new V4(0.44f, 0.32f, 0.21f, 1f);
			var hover = new V4(0.56f, 0.42f, 0.28f, 1f);
			var active = new V4(0.64f, 0.48f, 0.30f, 1f);
			var c = style.Colors;
			c[(int)ImGuiCol.Button] = accent;
			c[(int)ImGuiCol.ButtonHovered] = hover;
			c[(int)ImGuiCol.ButtonActive] = active;
			c[(int)ImGuiCol.Header] = new V4(0.44f, 0.32f, 0.21f, 0.7f);
			c[(int)ImGuiCol.HeaderHovered] = hover;
			c[(int)ImGuiCol.HeaderActive] = active;
			c[(int)ImGuiCol.FrameBg] = new V4(0.30f, 0.24f, 0.18f, 0.6f);
			c[(int)ImGuiCol.FrameBgHovered] = new V4(0.44f, 0.34f, 0.24f, 0.6f);
			c[(int)ImGuiCol.FrameBgActive] = new V4(0.52f, 0.40f, 0.28f, 0.7f);
			c[(int)ImGuiCol.TitleBgActive] = new V4(0.34f, 0.25f, 0.17f, 1f);
			c[(int)ImGuiCol.CheckMark] = new V4(0.93f, 0.78f, 0.52f, 1f);
			c[(int)ImGuiCol.SliderGrab] = hover;
			c[(int)ImGuiCol.SliderGrabActive] = new V4(0.93f, 0.78f, 0.52f, 1f);
			c[(int)ImGuiCol.Tab] = new V4(0.34f, 0.25f, 0.17f, 0.9f);
			c[(int)ImGuiCol.TabHovered] = hover;
			c[(int)ImGuiCol.TabActive] = accent;
			c[(int)ImGuiCol.TabUnfocusedActive] = new V4(0.36f, 0.27f, 0.19f, 1f);
			c[(int)ImGuiCol.SeparatorHovered] = hover;
			c[(int)ImGuiCol.SeparatorActive] = active;
			c[(int)ImGuiCol.ResizeGrip] = new V4(0.44f, 0.32f, 0.21f, 0.4f);
			c[(int)ImGuiCol.ResizeGripHovered] = hover;
			c[(int)ImGuiCol.ResizeGripActive] = active;
			c[(int)ImGuiCol.DockingPreview] = new V4(0.64f, 0.48f, 0.30f, 0.6f);
			c[(int)ImGuiCol.TextSelectedBg] = new V4(0.64f, 0.48f, 0.30f, 0.4f);
		}

		// ---- helpers (every Begin is matched by its End, even when the body throws) ----

		void Window(string title, ref bool open, Action body)
		{
			if (!open) return;
			bool visible = ImGui.Begin(title, ref open);
			try
			{
				if (visible) body();
			}
			catch (Exception e)
			{
				Report(e);
			}
			finally { ImGui.End(); }
		}

		static void Menu(string label, Action body)
		{
			if (!ImGui.BeginMenu(label)) return;
			try { body(); }
			finally { ImGui.EndMenu(); }
		}

		static void Child(string id, V2 size, Action body)
		{
			ImGui.BeginChild(id, size, false);
			try { body(); }
			finally { ImGui.EndChild(); }
		}

		static void WithId(string id, Action body)
		{
			ImGui.PushID(id);
			try { body(); }
			finally { ImGui.PopID(); }
		}

		/// <summary>Text that may hold '%' (game strings): ImGui's Text formats like printf.</summary>
		static void T(string text) { ImGui.TextUnformatted(text ?? ""); }

		static string Escape(string text) { return (text ?? "").Replace("%", "%%"); }

		static void Wrapped(string text) { ImGui.TextWrapped(Escape(text)); }

		static void Dim(string text) { ImGui.TextDisabled(Escape(text)); }

		static void Help(string text)
		{
			if (!ImGui.IsItemHovered()) return;
			ImGui.BeginTooltip();
			try
			{
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 24);
				T(text);
				ImGui.PopTextWrapPos();
			}
			finally { ImGui.EndTooltip(); }
		}
	}
}
#endif
