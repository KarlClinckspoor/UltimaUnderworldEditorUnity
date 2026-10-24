using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Adds a "Tools" entry to the editor's top bar, built at run time so the scene is not
	/// changed. It installs itself after the scene loads (RuntimeInitializeOnLoadMethod).
	/// </summary>
	public sealed class ExtensionsMenu : MonoBehaviour
	{
		UIManager ui;
		GameObject popup;
		public static StaticObject Selected { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Install()
		{
			var ui = UiKit.FindUIManager();
			if (!ui || ui.GetComponent<ExtensionsMenu>()) return;
			ui.gameObject.AddComponent<ExtensionsMenu>();
		}

		void Start()
		{
			ui = GetComponent<UIManager>();
			ui.OnSelectObject += go =>
			{
				if (!go) { Selected = null; return; }
				var sos = go.GetComponentInParent<StaticObjectScript>();
				Selected = sos ? sos.StaticObject : null;
			};
			CreateTopBarButton();
		}

		void CreateTopBarButton()
		{
			Button button = null;
			var bar = ui.TopBarGO ? ui.TopBarGO.transform : null;
			Transform template = bar ? (bar.Find("NPCs") ?? bar.Find("Strings")) : null;
			if (template)
			{
				var clone = Instantiate(template.gameObject, bar);
				clone.name = "Tools";
				// Keep only the label: the clone's own menu belongs to the original entry.
				for (int i = clone.transform.childCount - 1; i >= 0; i--)
				{
					var child = clone.transform.GetChild(i);
					if (!child.GetComponent<Text>()) Destroy(child.gameObject);
				}
				var label = clone.GetComponentInChildren<Text>();
				if (label) label.text = "Tools";
				button = clone.GetComponent<Button>();
				if (button)
				{
					button.onClick = new Button.ButtonClickedEvent();
					button.interactable = true;
				}
			}
			if (!button)
			{
				// Fallback: a small button in the top right corner.
				button = UiKit.Button(ui.transform, "Tools", null, 60, 22);
				var rt = button.GetComponent<RectTransform>();
				rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
				rt.pivot = new Vector2(1, 1);
				rt.anchoredPosition = new Vector2(-8, -4);
			}
			button.onClick.AddListener(TogglePopup);
		}

		void TogglePopup()
		{
			if (popup) { Destroy(popup); return; }
			var root = UiKit.Rect("ToolsMenu", ui.transform);
			popup = root.gameObject;
			root.anchorMin = root.anchorMax = new Vector2(0, 1);
			root.pivot = new Vector2(0, 1);
			root.anchoredPosition = new Vector2(Input.mousePosition.x / Mathf.Max(1f, CanvasScale()) - 10, -24);
			root.gameObject.AddComponent<Image>().color = UiKit.BarColor;
			var v = root.gameObject.AddComponent<VerticalLayoutGroup>();
			v.padding = new RectOffset(4, 4, 4, 4); v.spacing = 2;
			v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
			var fit = root.gameObject.AddComponent<ContentSizeFitter>();
			fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			Entry(root, "Object fields (selected object)...", () => FieldInspectorWindow.Show(Selected), true);
			Entry(root, "Find items in all levels...", () => ItemFinderWindow.Show(), true);
			Entry(root, "Class skills and attributes (SKILLS.DAT)...", () => ClassSkillsWindow.Show(), false);
			Entry(root, "Item combinations (CMB.DAT)...", () => CombinationsWindow.Show(), false);
			Entry(root, "Built-in 3D models (UW.EXE)...", () => ModelViewerWindow.Show(), false);
			root.SetAsLastSibling();
		}

		float CanvasScale()
		{
			var c = ui.GetComponentInParent<Canvas>();
			return c ? c.scaleFactor : 1f;
		}

		void Entry(Transform parent, string label, Action act, bool needsLevels)
		{
			var b = UiKit.Button(parent, label, () =>
			{
				Destroy(popup);
				if (needsLevels && (ui.MapCreator == null || !ui.MapCreator.IsInitialized))
				{
					ui.SpawnPopupMessage("Load the game data first (File > Read data).");
					return;
				}
				if (!needsLevels && string.IsNullOrEmpty(DataFolder))
				{
					ui.SpawnPopupMessage("Choose the game's LEV.ARK first (File menu), so the editor knows the DATA folder.");
					return;
				}
				try { act(); }
				catch (Exception e) { ui.SpawnPopupMessage("Error: " + e.Message); Debug.LogException(e); }
			}, 300, 22);
			var t = b.GetComponentInChildren<Text>();
			if (t) t.alignment = TextAnchor.MiddleLeft;
		}

		/// <summary>The game's DATA folder, from the LEV.ARK the editor was pointed at.</summary>
		public static string DataFolder
		{
			get
			{
				if (string.IsNullOrEmpty(DataReader.FilePath)) return null;
				var dir = FileExplorer.GetUpperPath(DataReader.FilePath.Replace('\\', '/'));
				return string.IsNullOrEmpty(dir) || !Directory.Exists(dir) ? null : dir;
			}
		}

		/// <summary>Writes a DATA file, keeping a one-time .BKP copy of the original as the editor does.</summary>
		public static void WriteDataFile(string fileName, byte[] data)
		{
			string path = Path.Combine(DataFolder, fileName);
			string backup = path + ".BKP";
			if (File.Exists(path) && !File.Exists(backup))
				File.Copy(path, backup);
			File.WriteAllBytes(path, data);
		}

		public static string FindDataFile(string fileName)
		{
			var dir = DataFolder;
			if (dir == null) return null;
			foreach (var f in Directory.GetFiles(dir))
				if (string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase))
					return f;
			return null;
		}
	}
}
