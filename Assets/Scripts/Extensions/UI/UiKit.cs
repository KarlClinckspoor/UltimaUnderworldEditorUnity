using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Builds simple windows and controls in code, so the new tools need no prefab or scene
	/// change. Windows are children of the UIManager's canvas, can be dragged anywhere
	/// (MovableWindow) and closed with their X button.
	/// </summary>
	public static class UiKit
	{
		public static readonly Color WindowColor = new Color(0.16f, 0.16f, 0.18f, 0.97f);
		public static readonly Color BarColor = new Color(0.10f, 0.10f, 0.12f, 1f);
		public static readonly Color FieldColor = new Color(0.93f, 0.93f, 0.93f, 1f);
		public static readonly Color ButtonColor = new Color(0.32f, 0.34f, 0.40f, 1f);
		public static readonly Color AccentColor = new Color(0.36f, 0.52f, 0.34f, 1f);
		public static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f, 1f);
		public static readonly Color DimTextColor = new Color(0.70f, 0.72f, 0.76f, 1f);
		public static readonly Color WarnColor = new Color(0.95f, 0.75f, 0.35f, 1f);

		static Font font;
		public static Font Font
		{
			get
			{
				if (!font) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
				return font;
			}
		}

		public static UIManager FindUIManager()
		{
			var go = GameObject.FindGameObjectWithTag("UIManager");
			return go ? go.GetComponent<UIManager>() : null;
		}

		public static RectTransform Rect(string name, Transform parent)
		{
			var go = new GameObject(name, typeof(RectTransform));
			var rt = go.GetComponent<RectTransform>();
			rt.SetParent(parent, false);
			return rt;
		}

		public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
		{
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = new Vector2(left, bottom);
			rt.offsetMax = new Vector2(-right, -top);
		}

		public static LayoutElement Size(GameObject go, float width = -1, float height = -1, float flexibleWidth = -1)
		{
			var le = go.GetComponent<LayoutElement>();
			if (!le) le = go.AddComponent<LayoutElement>();
			if (width >= 0) { le.preferredWidth = width; le.minWidth = width; }
			if (height >= 0) { le.preferredHeight = height; le.minHeight = height; }
			if (flexibleWidth >= 0) le.flexibleWidth = flexibleWidth;
			return le;
		}

		public static Text Label(Transform parent, string text, int size = 12, FontStyle style = FontStyle.Normal, Color? color = null, float width = -1, float height = 18)
		{
			var rt = Rect("Label", parent);
			var t = rt.gameObject.AddComponent<Text>();
			t.font = Font;
			t.fontSize = size;
			t.fontStyle = style;
			t.color = color ?? TextColor;
			t.text = text;
			t.alignment = TextAnchor.MiddleLeft;
			t.horizontalOverflow = HorizontalWrapMode.Wrap;
			t.verticalOverflow = VerticalWrapMode.Overflow;
			t.raycastTarget = false;
			Size(rt.gameObject, width, height, width < 0 ? 1 : -1);
			return t;
		}

		/// <summary>A label whose height follows its text (for paragraphs).</summary>
		public static Text Paragraph(Transform parent, string text, int size = 11, Color? color = null)
		{
			var t = Label(parent, text, size, FontStyle.Normal, color ?? DimTextColor, -1, -1);
			t.alignment = TextAnchor.UpperLeft;
			var fitter = t.gameObject.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			return t;
		}

		public static Button Button(Transform parent, string label, Action onClick, float width = 90, float height = 20, Color? color = null)
		{
			var rt = Rect("Button " + label, parent);
			var img = rt.gameObject.AddComponent<Image>();
			img.color = color ?? ButtonColor;
			var b = rt.gameObject.AddComponent<Button>();
			b.targetGraphic = img;
			var t = Label(rt, label, 11, FontStyle.Normal, TextColor);
			t.alignment = TextAnchor.MiddleCenter;
			Stretch(t.rectTransform, 2, 2, 0, 0);
			Size(rt.gameObject, width, height);
			if (onClick != null) b.onClick.AddListener(() => onClick());
			return b;
		}

		public static void SetButtonText(Button b, string text)
		{
			var t = b.GetComponentInChildren<Text>();
			if (t) t.text = text;
		}

		public static InputField Input(Transform parent, string value, Action<string> onEndEdit, float width = 60, bool numeric = true, float height = 18)
		{
			var rt = Rect("Input", parent);
			var img = rt.gameObject.AddComponent<Image>();
			img.color = FieldColor;
			var input = rt.gameObject.AddComponent<InputField>();
			input.targetGraphic = img;

			var text = Label(rt, "", 11, FontStyle.Normal, Color.black);
			text.supportRichText = false;
			Stretch(text.rectTransform, 4, 4, 1, 1);
			var ph = Label(rt, numeric ? "0" : "", 11, FontStyle.Italic, new Color(0.4f, 0.4f, 0.4f));
			Stretch(ph.rectTransform, 4, 4, 1, 1);

			input.textComponent = text;
			input.placeholder = ph;
			input.contentType = numeric ? InputField.ContentType.Standard : InputField.ContentType.Standard;
			input.text = value ?? "";
			if (onEndEdit != null) input.onEndEdit.AddListener(s => onEndEdit(s));
			Size(rt.gameObject, width, height, width < 0 ? 1 : -1);
			return input;
		}

		/// <summary>A dropdown, from the editor's own dropdown prefab when it is available.</summary>
		public static Dropdown Dropdown(Transform parent, IList<string> options, int value, Action<int> onChange, float width = 160, float height = 20)
		{
			var ui = FindUIManager();
			GameObject go;
			if (ui && ui.DropdownPrefab)
			{
				go = UnityEngine.Object.Instantiate(ui.DropdownPrefab, parent);
				go.SetActive(true);
			}
			else
				throw new InvalidOperationException("UIManager.DropdownPrefab is not set");
			var dd = go.GetComponent<Dropdown>();
			dd.onValueChanged.RemoveAllListeners();
			dd.options = new List<Dropdown.OptionData>();
			foreach (var o in options) dd.options.Add(new Dropdown.OptionData(o));
			dd.value = Mathf.Clamp(value, 0, Math.Max(0, options.Count - 1));
			dd.RefreshShownValue();
			if (onChange != null) dd.onValueChanged.AddListener(i => onChange(i));
			Size(go, width, height);
			return dd;
		}

		/// <summary>A button that cycles through labels; onChange gets the new index.</summary>
		public static Button Cycle(Transform parent, string[] labels, int start, Action<int> onChange, float width = 120)
		{
			int current = start;
			Button b = null;
			b = Button(parent, labels[current], () =>
			{
				current = (current + 1) % labels.Length;
				SetButtonText(b, labels[current]);
				if (onChange != null) onChange(current);
			}, width);
			return b;
		}

		public static RectTransform Row(Transform parent, float height = 20, float spacing = 6)
		{
			var rt = Rect("Row", parent);
			var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
			h.spacing = spacing;
			h.childControlWidth = true;
			h.childControlHeight = true;
			h.childForceExpandWidth = false;
			h.childForceExpandHeight = false;
			h.childAlignment = TextAnchor.MiddleLeft;
			Size(rt.gameObject, -1, height);
			return rt;
		}

		public static RectTransform Column(Transform parent, float spacing = 3)
		{
			var rt = Rect("Column", parent);
			var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
			v.spacing = spacing;
			v.childControlWidth = true;
			v.childControlHeight = true;
			v.childForceExpandWidth = true;
			v.childForceExpandHeight = false;
			var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			return rt;
		}

		public static void Clear(Transform t)
		{
			for (int i = t.childCount - 1; i >= 0; i--)
				UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
		}

		public static void Tooltip(GameObject go, string text)
		{
			if (string.IsNullOrEmpty(text) || !go) return;
			var tip = go.GetComponent<HoverTooltip>();
			if (!tip) tip = go.AddComponent<HoverTooltip>();
			tip.Text = text;
		}

		public static Window Window(string title, float width, float height)
		{
			var ui = FindUIManager();
			if (!ui) throw new InvalidOperationException("No UIManager in the scene");
			return new Window(ui, title, width, height);
		}
	}

	/// <summary>A code-built, draggable window with a title bar, a toolbar and a scrolled body.</summary>
	public sealed class Window
	{
		public readonly GameObject Root;
		public readonly Text Title;
		public readonly RectTransform Toolbar;
		public readonly RectTransform Content;
		public readonly Text Status;
		public event Action Closed;

		public Window(UIManager ui, string title, float width, float height)
		{
			var root = UiKit.Rect("Window " + title, ui.transform);
			Root = root.gameObject;
			root.sizeDelta = new Vector2(width, height);
			root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
			root.anchoredPosition = Vector2.zero;
			var bg = Root.AddComponent<Image>();
			bg.color = UiKit.WindowColor;
			Root.AddComponent<MovableWindow>();

			var bar = UiKit.Rect("TitleBar", root);
			bar.anchorMin = new Vector2(0, 1); bar.anchorMax = new Vector2(1, 1);
			bar.pivot = new Vector2(0.5f, 1); bar.sizeDelta = new Vector2(0, 24); bar.anchoredPosition = Vector2.zero;
			bar.gameObject.AddComponent<Image>().color = UiKit.BarColor;
			Title = UiKit.Label(bar, title, 13, FontStyle.Bold);
			UiKit.Stretch(Title.rectTransform, 8, 30, 0, 0);
			var close = UiKit.Button(bar, "X", Close, 22, 20, new Color(0.55f, 0.2f, 0.2f));
			var crt = close.GetComponent<RectTransform>();
			crt.anchorMin = crt.anchorMax = new Vector2(1, 0.5f); crt.pivot = new Vector2(1, 0.5f);
			crt.sizeDelta = new Vector2(22, 20); crt.anchoredPosition = new Vector2(-2, 0);

			Toolbar = UiKit.Rect("Toolbar", root);
			Toolbar.anchorMin = new Vector2(0, 1); Toolbar.anchorMax = new Vector2(1, 1);
			Toolbar.pivot = new Vector2(0.5f, 1); Toolbar.anchoredPosition = new Vector2(0, -26);
			Toolbar.sizeDelta = new Vector2(-12, 0);
			var tv = Toolbar.gameObject.AddComponent<VerticalLayoutGroup>();
			tv.spacing = 3; tv.childControlWidth = true; tv.childControlHeight = true; tv.childForceExpandHeight = false; tv.childForceExpandWidth = true;
			var tf = Toolbar.gameObject.AddComponent<ContentSizeFitter>();
			tf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			Status = UiKit.Label(root, "", 11, FontStyle.Normal, UiKit.DimTextColor, -1, 16);
			var srt = Status.rectTransform;
			srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 0); srt.pivot = new Vector2(0.5f, 0);
			srt.sizeDelta = new Vector2(-12, 16); srt.anchoredPosition = new Vector2(0, 3);

			var scroll = UiKit.Rect("Scroll", root);
			UiKit.Stretch(scroll, 6, 6, 30, 22);
			var sr = scroll.gameObject.AddComponent<ScrollRect>();
			sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 25; sr.movementType = ScrollRect.MovementType.Clamped;
			scroll.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);
			var viewport = UiKit.Rect("Viewport", scroll);
			UiKit.Stretch(viewport);
			viewport.gameObject.AddComponent<RectMask2D>();
			Content = UiKit.Rect("Content", viewport);
			Content.anchorMin = new Vector2(0, 1); Content.anchorMax = new Vector2(1, 1); Content.pivot = new Vector2(0.5f, 1);
			Content.sizeDelta = new Vector2(-8, 0); Content.anchoredPosition = Vector2.zero;
			var v = Content.gameObject.AddComponent<VerticalLayoutGroup>();
			v.spacing = 3; v.padding = new RectOffset(4, 4, 4, 4);
			v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
			var fit = Content.gameObject.AddComponent<ContentSizeFitter>();
			fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			sr.viewport = viewport; sr.content = Content;
			root.SetAsLastSibling();

			// The scrolled body starts below the toolbar once its height is known.
			Root.AddComponent<WindowLayout>().Init(Toolbar, scroll);
		}

		public void Close()
		{
			if (Root) UnityEngine.Object.Destroy(Root);
			if (Closed != null) Closed();
		}
	}

	/// <summary>Keeps the scrolled body under the toolbar as the toolbar grows.</summary>
	public sealed class WindowLayout : MonoBehaviour
	{
		RectTransform toolbar, body;
		public void Init(RectTransform toolbar, RectTransform body) { this.toolbar = toolbar; this.body = body; }
		void LateUpdate()
		{
			if (!toolbar || !body) return;
			float top = 30 + toolbar.rect.height + (toolbar.childCount > 0 ? 4 : 0);
			body.offsetMax = new Vector2(-6, -top);
		}
	}

	/// <summary>Shows a tooltip through UIManager.SpawnTooltip while the pointer is over the object.</summary>
	public sealed class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public string Text;
		UIManager ui;
		public void OnPointerEnter(PointerEventData e)
		{
			if (!ui) ui = UiKit.FindUIManager();
			if (ui && !string.IsNullOrEmpty(Text)) ui.SpawnTooltip(Text);
		}
		public void OnPointerExit(PointerEventData e)
		{
			if (ui) ui.DestroyTooltip();
		}
		void OnDisable()
		{
			if (ui) ui.DestroyTooltip();
		}
	}
}
