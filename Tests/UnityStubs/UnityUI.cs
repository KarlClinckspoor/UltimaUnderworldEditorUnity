// Compile-only stand-ins for UnityEngine.UI, EventSystems and the third-party UI packages the
// editor uses (unity-ui-extensions, FlexibleColorPicker, Newtonsoft.Json).
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.EventSystems
{
	public class UIBehaviour : MonoBehaviour
	{
		protected virtual void Awake() { }
		protected virtual void OnEnable() { }
		protected virtual void OnDisable() { }
		protected virtual void Start() { }
		protected virtual void OnDestroy() { }
		protected virtual void OnRectTransformDimensionsChange() { }
		public virtual bool IsActive() { return true; }
	}
	public class BaseEventData { public BaseEventData(EventSystem e) { } public GameObject selectedObject { get; set; } public void Use() { } public bool used { get { return false; } } }
	public class PointerEventData : BaseEventData
	{
		public enum InputButton { Left, Right, Middle }
		public PointerEventData(EventSystem e) : base(e) { }
		public Vector2 position { get; set; }
		public Vector2 delta { get; set; }
		public Vector2 pressPosition { get; set; }
		public Vector2 scrollDelta { get; set; }
		public InputButton button { get; set; }
		public int clickCount { get; set; }
		public float clickTime { get; set; }
		public bool dragging { get; set; }
		public bool useDragThreshold { get; set; }
		public GameObject pointerEnter { get; set; }
		public GameObject pointerPress { get; set; }
		public GameObject pointerDrag { get; set; }
		public GameObject pointerCurrentRaycastGameObject { get; set; }
		public GameObject rawPointerPress { get; set; }
		public RaycastResult pointerCurrentRaycast { get; set; }
		public RaycastResult pointerPressRaycast { get; set; }
		public Camera pressEventCamera { get; set; }
		public Camera enterEventCamera { get; set; }
		public List<GameObject> hovered = new List<GameObject>();
		public int pointerId { get; set; }
		public bool eligibleForClick { get; set; }
		public bool IsPointerMoving() { return false; }
	}
	public struct RaycastResult { public GameObject gameObject; public float distance; public Vector3 worldPosition; public Vector2 screenPosition; public bool isValid { get { return gameObject != null; } } public int depth; public int sortingOrder; public BaseRaycaster module; }
	public class BaseRaycaster : UIBehaviour { }
	public class GraphicRaycaster : BaseRaycaster { public void Raycast(PointerEventData e, List<RaycastResult> r) { } }
	public class PhysicsRaycaster : BaseRaycaster { }
	public class EventSystem : UIBehaviour
	{
		public static EventSystem current { get; set; }
		public GameObject currentSelectedGameObject { get { return null; } }
		public bool alreadySelecting { get { return false; } }
		public bool sendNavigationEvents { get; set; }
		public void SetSelectedGameObject(GameObject g) { }
		public void SetSelectedGameObject(GameObject g, BaseEventData e) { }
		public bool IsPointerOverGameObject() { return false; }
		public bool IsPointerOverGameObject(int id) { return false; }
		public void RaycastAll(PointerEventData e, List<RaycastResult> r) { }
		public int pixelDragThreshold { get; set; }
	}
	public class StandaloneInputModule : UIBehaviour { }
	public interface IEventSystemHandler { }
	public interface IPointerEnterHandler : IEventSystemHandler { void OnPointerEnter(PointerEventData e); }
	public interface IPointerExitHandler : IEventSystemHandler { void OnPointerExit(PointerEventData e); }
	public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData e); }
	public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData e); }
	public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData e); }
	public interface IInitializePotentialDragHandler : IEventSystemHandler { void OnInitializePotentialDrag(PointerEventData e); }
	public interface IBeginDragHandler : IEventSystemHandler { void OnBeginDrag(PointerEventData e); }
	public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData e); }
	public interface IEndDragHandler : IEventSystemHandler { void OnEndDrag(PointerEventData e); }
	public interface IDropHandler : IEventSystemHandler { void OnDrop(PointerEventData e); }
	public interface IScrollHandler : IEventSystemHandler { void OnScroll(PointerEventData e); }
	public interface ISelectHandler : IEventSystemHandler { void OnSelect(BaseEventData e); }
	public interface IDeselectHandler : IEventSystemHandler { void OnDeselect(BaseEventData e); }
	public interface ISubmitHandler : IEventSystemHandler { void OnSubmit(BaseEventData e); }
	public interface ICancelHandler : IEventSystemHandler { void OnCancel(BaseEventData e); }
	public interface IUpdateSelectedHandler : IEventSystemHandler { void OnUpdateSelected(BaseEventData e); }
	public interface IMoveHandler : IEventSystemHandler { void OnMove(AxisEventData e); }
	public class AxisEventData : BaseEventData { public AxisEventData(EventSystem e) : base(e) { } public MoveDirection moveDir; }
	public enum MoveDirection { Left, Up, Right, Down, None }
	public enum EventTriggerType { PointerEnter, PointerExit, PointerDown, PointerUp, PointerClick, Drag, Drop, Scroll, UpdateSelected, Select, Deselect, Move, InitializePotentialDrag, BeginDrag, EndDrag, Submit, Cancel }
	public class EventTrigger : MonoBehaviour
	{
		public class TriggerEvent : UnityEvent<BaseEventData> { }
		public class Entry { public EventTriggerType eventID; public TriggerEvent callback = new TriggerEvent(); }
		public List<Entry> triggers = new List<Entry>();
	}
	public static class ExecuteEvents
	{
		public delegate void EventFunction<T>(T handler, BaseEventData e);
		public static EventFunction<IPointerClickHandler> pointerClickHandler;
		public static EventFunction<IPointerDownHandler> pointerDownHandler;
		public static EventFunction<IPointerUpHandler> pointerUpHandler;
		public static EventFunction<IBeginDragHandler> beginDragHandler;
		public static EventFunction<IDragHandler> dragHandler;
		public static EventFunction<IEndDragHandler> endDragHandler;
		public static EventFunction<IScrollHandler> scrollHandler;
		public static EventFunction<ISubmitHandler> submitHandler;
		public static bool Execute<T>(GameObject t, BaseEventData e, EventFunction<T> f) where T : IEventSystemHandler { return false; }
		public static GameObject ExecuteHierarchy<T>(GameObject t, BaseEventData e, EventFunction<T> f) where T : IEventSystemHandler { return null; }
		public static GameObject GetEventHandler<T>(GameObject t) where T : IEventSystemHandler { return null; }
	}
}

namespace UnityEngine.UI
{
	using UnityEngine.EventSystems;
	public abstract class Graphic : UIBehaviour
	{
		public virtual Color color { get; set; } = Color.white;
		public virtual Material material { get; set; }
		public virtual Material materialForRendering { get { return material; } }
		public virtual Texture mainTexture { get { return null; } }
		public bool raycastTarget { get; set; } = true;
		public RectTransform rectTransform { get { return (RectTransform)transform; } }
		public Canvas canvas { get { return null; } }
		public CanvasRenderer canvasRenderer { get { return new CanvasRenderer(); } }
		public virtual void SetVerticesDirty() { }
		public virtual void SetMaterialDirty() { }
		public virtual void SetAllDirty() { }
		public virtual void SetLayoutDirty() { }
		public virtual void SetNativeSize() { }
		public virtual void CrossFadeColor(Color c, float d, bool i, bool a) { }
		public virtual void CrossFadeAlpha(float a, float d, bool i) { }
		protected virtual void OnPopulateMesh(VertexHelper vh) { }
		public virtual void Rebuild(CanvasUpdate u) { }
	}
	public enum CanvasUpdate { Prelayout, Layout, PostLayout, PreRender, LatePreRender, MaxUpdateValue }
	public class VertexHelper : IDisposable
	{
		public int currentVertCount { get { return 0; } }
		public void Clear() { }
		public void AddVert(Vector3 p, Color32 c, Vector2 uv) { }
		public void AddVert(UIVertex v) { }
		public void AddTriangle(int a, int b, int c) { }
		public void AddUIVertexQuad(UIVertex[] q) { }
		public void Dispose() { }
	}
	public struct UIVertex { public Vector3 position; public Color32 color; public Vector4 uv0; public static UIVertex simpleVert { get { return new UIVertex(); } } }
	public abstract class MaskableGraphic : Graphic { public bool maskable { get; set; } }
	public class Text : MaskableGraphic
	{
		public string text { get; set; } = "";
		public Font font { get; set; }
		public int fontSize { get; set; } = 14;
		public FontStyle fontStyle { get; set; }
		public TextAnchor alignment { get; set; }
		public bool alignByGeometry { get; set; }
		public bool supportRichText { get; set; }
		public bool resizeTextForBestFit { get; set; }
		public int resizeTextMinSize { get; set; }
		public int resizeTextMaxSize { get; set; }
		public HorizontalWrapMode horizontalOverflow { get; set; }
		public VerticalWrapMode verticalOverflow { get; set; }
		public float lineSpacing { get; set; }
		public float preferredWidth { get { return (text ?? "").Length * fontSize * 0.5f; } }
		public float preferredHeight { get { return fontSize; } }
		public TextGenerator cachedTextGenerator { get { return new TextGenerator(); } }
		public TextGenerator cachedTextGeneratorForLayout { get { return new TextGenerator(); } }
		public TextGenerationSettings GetGenerationSettings(Vector2 extents) { return new TextGenerationSettings(); }
		public float pixelsPerUnit { get { return 1; } }
	}
	public class Image : MaskableGraphic
	{
		public enum Type { Simple, Sliced, Tiled, Filled }
		public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
		public Sprite sprite { get; set; }
		public Sprite overrideSprite { get; set; }
		public Type type { get; set; }
		public bool preserveAspect { get; set; }
		public bool fillCenter { get; set; }
		public float fillAmount { get; set; }
		public FillMethod fillMethod { get; set; }
		public int fillOrigin { get; set; }
		public float pixelsPerUnitMultiplier { get; set; }
		public bool useSpriteMesh { get; set; }
		public float alphaHitTestMinimumThreshold { get; set; }
	}
	public class RawImage : MaskableGraphic { public Texture texture { get; set; } public Rect uvRect { get; set; } }
	public struct ColorBlock
	{
		public Color normalColor { get; set; }
		public Color highlightedColor { get; set; }
		public Color pressedColor { get; set; }
		public Color selectedColor { get; set; }
		public Color disabledColor { get; set; }
		public float colorMultiplier { get; set; }
		public float fadeDuration { get; set; }
		public static ColorBlock defaultColorBlock { get { var c = new ColorBlock(); c.normalColor = Color.white; c.colorMultiplier = 1; return c; } }
	}
	public struct Navigation { public enum Mode { None = 0, Horizontal = 1, Vertical = 2, Automatic = 3, Explicit = 4 } public Mode mode { get; set; } public static Navigation defaultNavigation { get { return new Navigation(); } } public Selectable selectOnUp { get; set; } public Selectable selectOnDown { get; set; } }
	public class SpriteState { }
	public class Selectable : UIBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
	{
		public enum Transition { None, ColorTint, SpriteSwap, Animation }
		public bool interactable { get; set; } = true;
		public Graphic targetGraphic { get; set; }
		public Image image { get; set; }
		public ColorBlock colors { get; set; } = ColorBlock.defaultColorBlock;
		public Navigation navigation { get; set; }
		public Transition transition { get; set; }
		public virtual bool IsInteractable() { return interactable; }
		public virtual void Select() { }
		public virtual void OnPointerEnter(PointerEventData e) { }
		public virtual void OnPointerExit(PointerEventData e) { }
		public virtual void OnPointerDown(PointerEventData e) { }
		public virtual void OnPointerUp(PointerEventData e) { }
		public virtual void OnSelect(BaseEventData e) { }
		public virtual void OnDeselect(BaseEventData e) { }
		protected virtual void DoStateTransition(SelectionState s, bool instant) { }
		protected enum SelectionState { Normal, Highlighted, Pressed, Selected, Disabled }
		public static List<Selectable> allSelectablesArray { get { return new List<Selectable>(); } }
	}
	public class Button : Selectable, IPointerClickHandler, ISubmitHandler
	{
		public class ButtonClickedEvent : UnityEvent { }
		public ButtonClickedEvent onClick { get; set; } = new ButtonClickedEvent();
		public virtual void OnPointerClick(PointerEventData e) { onClick.Invoke(); }
		public virtual void OnSubmit(BaseEventData e) { onClick.Invoke(); }
	}
	public class ToggleGroup : UIBehaviour { public bool allowSwitchOff { get; set; } public void RegisterToggle(Toggle t) { } public void SetAllTogglesOff() { } public bool AnyTogglesOn() { return false; } public void NotifyToggleOn(Toggle t) { } }
	public class Toggle : Selectable, IPointerClickHandler, ISubmitHandler
	{
		public enum ToggleTransition { None, Fade }
		public class ToggleEvent : UnityEvent<bool> { }
		bool on;
		public bool isOn { get { return on; } set { if (on != value) { on = value; onValueChanged.Invoke(value); } } }
		public void SetIsOnWithoutNotify(bool v) { on = v; }
		public ToggleEvent onValueChanged { get; set; } = new ToggleEvent();
		public Graphic graphic { get; set; }
		public ToggleGroup group { get; set; }
		public ToggleTransition toggleTransition { get; set; }
		public virtual void OnPointerClick(PointerEventData e) { isOn = !isOn; }
		public virtual void OnSubmit(BaseEventData e) { isOn = !isOn; }
	}
	public class Slider : Selectable, IDragHandler, IInitializePotentialDragHandler
	{
		public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
		public class SliderEvent : UnityEvent<float> { }
		float v;
		public float value { get { return v; } set { v = value; onValueChanged.Invoke(value); } }
		public void SetValueWithoutNotify(float x) { v = x; }
		public float minValue { get; set; }
		public float maxValue { get; set; } = 1;
		public bool wholeNumbers { get; set; }
		public float normalizedValue { get; set; }
		public Direction direction { get; set; }
		public RectTransform fillRect { get; set; }
		public RectTransform handleRect { get; set; }
		public SliderEvent onValueChanged { get; set; } = new SliderEvent();
		public virtual void OnDrag(PointerEventData e) { }
		public virtual void OnInitializePotentialDrag(PointerEventData e) { }
	}
	public class Scrollbar : Selectable
	{
		public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
		public class ScrollEvent : UnityEvent<float> { }
		public float value { get; set; }
		public float size { get; set; }
		public int numberOfSteps { get; set; }
		public Direction direction { get; set; }
		public RectTransform handleRect { get; set; }
		public ScrollEvent onValueChanged { get; set; } = new ScrollEvent();
		public void SetValueWithoutNotify(float v) { value = v; }
	}
	public class ScrollRect : UIBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
	{
		public enum MovementType { Unrestricted, Elastic, Clamped }
		public enum ScrollbarVisibility { Permanent, AutoHide, AutoHideAndExpandViewport }
		public class ScrollRectEvent : UnityEvent<Vector2> { }
		public RectTransform content { get; set; }
		public RectTransform viewport { get; set; }
		public bool horizontal { get; set; }
		public bool vertical { get; set; }
		public MovementType movementType { get; set; }
		public float elasticity { get; set; }
		public bool inertia { get; set; }
		public float decelerationRate { get; set; }
		public float scrollSensitivity { get; set; }
		public Scrollbar horizontalScrollbar { get; set; }
		public Scrollbar verticalScrollbar { get; set; }
		public ScrollbarVisibility horizontalScrollbarVisibility { get; set; }
		public ScrollbarVisibility verticalScrollbarVisibility { get; set; }
		public float horizontalScrollbarSpacing { get; set; }
		public float verticalScrollbarSpacing { get; set; }
		public float verticalNormalizedPosition { get; set; }
		public float horizontalNormalizedPosition { get; set; }
		public Vector2 normalizedPosition { get; set; }
		public Vector2 velocity { get; set; }
		public ScrollRectEvent onValueChanged { get; set; } = new ScrollRectEvent();
		public void StopMovement() { }
		public virtual void OnScroll(PointerEventData e) { }
		public virtual void OnBeginDrag(PointerEventData e) { }
		public virtual void OnDrag(PointerEventData e) { }
		public virtual void OnEndDrag(PointerEventData e) { }
		public virtual void OnInitializePotentialDrag(PointerEventData e) { }
		public virtual void Rebuild(CanvasUpdate u) { }
	}
	public class InputField : Selectable, IUpdateSelectedHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, ISubmitHandler
	{
		public enum ContentType { Standard, Autocorrected, IntegerNumber, DecimalNumber, Alphanumeric, Name, EmailAddress, Password, Pin, Custom }
		public enum LineType { SingleLine, MultiLineSubmit, MultiLineNewline }
		public enum InputType { Standard, AutoCorrect, Password }
		public enum CharacterValidation { None, Integer, Decimal, Alphanumeric, Name, EmailAddress }
		public class SubmitEvent : UnityEvent<string> { }
		public class OnChangeEvent : UnityEvent<string> { }
		public class EndEditEvent : UnityEvent<string> { }
		public delegate char OnValidateInput(string text, int charIndex, char addedChar);
		string t = "";
		public string text { get { return t; } set { t = value ?? ""; onValueChanged.Invoke(t); } }
		public void SetTextWithoutNotify(string v) { t = v ?? ""; }
		public Text textComponent { get; set; }
		public Graphic placeholder { get; set; }
		public ContentType contentType { get; set; }
		public LineType lineType { get; set; }
		public InputType inputType { get; set; }
		public CharacterValidation characterValidation { get; set; }
		public int characterLimit { get; set; }
		public bool readOnly { get; set; }
		public bool isFocused { get { return false; } }
		public bool multiLine { get { return lineType != LineType.SingleLine; } }
		public int caretPosition { get; set; }
		public int selectionAnchorPosition { get; set; }
		public int selectionFocusPosition { get; set; }
		public int stringPosition { get; set; }
		public float caretBlinkRate { get; set; }
		public int caretWidth { get; set; }
		public Color caretColor { get; set; }
		public bool customCaretColor { get; set; }
		public Color selectionColor { get; set; }
		public bool shouldHideMobileInput { get; set; }
		public OnValidateInput onValidateInput { get; set; }
		public OnChangeEvent onValueChanged { get; set; } = new OnChangeEvent();
		public SubmitEvent onEndEdit { get; set; } = new SubmitEvent();
		public SubmitEvent onSubmit { get; set; } = new SubmitEvent();
		public void ActivateInputField() { }
		public void DeactivateInputField() { }
		public void ForceLabelUpdate() { }
		public void MoveTextEnd(bool shift) { }
		public void MoveTextStart(bool shift) { }
		public virtual void OnUpdateSelected(BaseEventData e) { }
		public virtual void OnBeginDrag(PointerEventData e) { }
		public virtual void OnDrag(PointerEventData e) { }
		public virtual void OnEndDrag(PointerEventData e) { }
		public virtual void OnPointerClick(PointerEventData e) { }
		public virtual void OnSubmit(BaseEventData e) { }
	}
	public class Dropdown : Selectable, IPointerClickHandler, ISubmitHandler, ICancelHandler
	{
		public class OptionData { public string text { get; set; } public Sprite image { get; set; } public OptionData() { } public OptionData(string t) { text = t; } public OptionData(Sprite i) { image = i; } public OptionData(string t, Sprite i) { text = t; image = i; } }
		public class OptionDataList { public List<OptionData> options { get; set; } = new List<OptionData>(); }
		public class DropdownEvent : UnityEvent<int> { }
		int v;
		public List<OptionData> options { get; set; } = new List<OptionData>();
		public int value { get { return v; } set { v = value; onValueChanged.Invoke(value); } }
		public void SetValueWithoutNotify(int x) { v = x; }
		public Text captionText { get; set; }
		public Text itemText { get; set; }
		public Image captionImage { get; set; }
		public Image itemImage { get; set; }
		public RectTransform template { get; set; }
		public DropdownEvent onValueChanged { get; set; } = new DropdownEvent();
		public float alphaFadeSpeed { get; set; }
		public void AddOptions(List<string> o) { foreach (var s in o) options.Add(new OptionData(s)); }
		public void AddOptions(List<OptionData> o) { options.AddRange(o); }
		public void AddOptions(List<Sprite> o) { foreach (var s in o) options.Add(new OptionData(s)); }
		public void ClearOptions() { options.Clear(); }
		public void RefreshShownValue() { }
		public void Show() { }
		public void Hide() { }
		public virtual void OnPointerClick(PointerEventData e) { }
		public virtual void OnSubmit(BaseEventData e) { }
		public virtual void OnCancel(BaseEventData e) { }
		protected virtual GameObject CreateBlocker(Canvas c) { return null; }
		protected virtual GameObject CreateDropdownList(GameObject t) { return null; }
	}
	public interface ILayoutElement { float minWidth { get; } float preferredWidth { get; } float flexibleWidth { get; } float minHeight { get; } float preferredHeight { get; } float flexibleHeight { get; } int layoutPriority { get; } void CalculateLayoutInputHorizontal(); void CalculateLayoutInputVertical(); }
	public interface ILayoutController { void SetLayoutHorizontal(); void SetLayoutVertical(); }
	public interface ILayoutGroup : ILayoutController { }
	public interface ILayoutSelfController : ILayoutController { }
	public interface ILayoutIgnorer { bool ignoreLayout { get; } }
	public class LayoutElement : UIBehaviour, ILayoutElement, ILayoutIgnorer
	{
		public bool ignoreLayout { get; set; }
		public float minWidth { get; set; } = -1;
		public float minHeight { get; set; } = -1;
		public float preferredWidth { get; set; } = -1;
		public float preferredHeight { get; set; } = -1;
		public float flexibleWidth { get; set; } = -1;
		public float flexibleHeight { get; set; } = -1;
		public int layoutPriority { get; set; } = 1;
		public void CalculateLayoutInputHorizontal() { }
		public void CalculateLayoutInputVertical() { }
	}
	public class RectOffset { public int left, right, top, bottom; public RectOffset() { } public RectOffset(int l, int r, int t, int b) { left = l; right = r; top = t; bottom = b; } public int horizontal { get { return left + right; } } public int vertical { get { return top + bottom; } } }
	public abstract class LayoutGroup : UIBehaviour, ILayoutElement, ILayoutGroup
	{
		public RectOffset padding { get; set; } = new RectOffset();
		public TextAnchor childAlignment { get; set; }
		public float minWidth { get { return 0; } }
		public float preferredWidth { get { return 0; } }
		public float flexibleWidth { get { return 0; } }
		public float minHeight { get { return 0; } }
		public float preferredHeight { get { return 0; } }
		public float flexibleHeight { get { return 0; } }
		public int layoutPriority { get { return 0; } }
		public virtual void CalculateLayoutInputHorizontal() { }
		public virtual void CalculateLayoutInputVertical() { }
		public virtual void SetLayoutHorizontal() { }
		public virtual void SetLayoutVertical() { }
	}
	public abstract class HorizontalOrVerticalLayoutGroup : LayoutGroup
	{
		public float spacing { get; set; }
		public bool childForceExpandWidth { get; set; } = true;
		public bool childForceExpandHeight { get; set; } = true;
		public bool childControlWidth { get; set; }
		public bool childControlHeight { get; set; }
		public bool childScaleWidth { get; set; }
		public bool childScaleHeight { get; set; }
		public bool reverseArrangement { get; set; }
	}
	public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
	public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
	public class GridLayoutGroup : LayoutGroup
	{
		public enum Corner { UpperLeft, UpperRight, LowerLeft, LowerRight }
		public enum Axis { Horizontal, Vertical }
		public enum Constraint { Flexible, FixedColumnCount, FixedRowCount }
		public Vector2 cellSize { get; set; }
		public Vector2 spacing { get; set; }
		public Corner startCorner { get; set; }
		public Axis startAxis { get; set; }
		public Constraint constraint { get; set; }
		public int constraintCount { get; set; }
	}
	public class ContentSizeFitter : UIBehaviour, ILayoutSelfController
	{
		public enum FitMode { Unconstrained, MinSize, PreferredSize }
		public FitMode horizontalFit { get; set; }
		public FitMode verticalFit { get; set; }
		public void SetLayoutHorizontal() { }
		public void SetLayoutVertical() { }
	}
	public class AspectRatioFitter : UIBehaviour { public enum AspectMode { None, WidthControlsHeight, HeightControlsWidth, FitInParent, EnvelopeParent } public AspectMode aspectMode { get; set; } public float aspectRatio { get; set; } }
	public class CanvasScaler : UIBehaviour
	{
		public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
		public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
		public ScaleMode uiScaleMode { get; set; }
		public float scaleFactor { get; set; } = 1;
		public Vector2 referenceResolution { get; set; }
		public ScreenMatchMode screenMatchMode { get; set; }
		public float matchWidthOrHeight { get; set; }
		public float referencePixelsPerUnit { get; set; }
	}
	public static class LayoutRebuilder { public static void ForceRebuildLayoutImmediate(RectTransform r) { } public static void MarkLayoutForRebuild(RectTransform r) { } }
	public static class LayoutUtility { public static float GetPreferredWidth(RectTransform r) { return 0; } public static float GetPreferredHeight(RectTransform r) { return 0; } public static float GetMinWidth(RectTransform r) { return 0; } public static float GetMinHeight(RectTransform r) { return 0; } }
	public class Mask : UIBehaviour { public bool showMaskGraphic { get; set; } }
	public class RectMask2D : UIBehaviour { public Vector4 padding { get; set; } public Vector2Int softness { get; set; } }
	public abstract class BaseMeshEffect : UIBehaviour { public abstract void ModifyMesh(VertexHelper vh); }
	public class Shadow : BaseMeshEffect { public Color effectColor { get; set; } public Vector2 effectDistance { get; set; } public bool useGraphicAlpha { get; set; } public override void ModifyMesh(VertexHelper vh) { } }
	public class Outline : Shadow { }
	public class GraphicRaycaster : UnityEngine.EventSystems.GraphicRaycaster { }
	public static class DefaultControls
	{
		public struct Resources { public Sprite standard, background, inputField, knob, checkmark, dropdown, mask; }
		public static GameObject CreateButton(Resources r) { return new GameObject("Button"); }
		public static GameObject CreateText(Resources r) { return new GameObject("Text"); }
		public static GameObject CreateImage(Resources r) { return new GameObject("Image"); }
		public static GameObject CreatePanel(Resources r) { return new GameObject("Panel"); }
		public static GameObject CreateInputField(Resources r) { return new GameObject("InputField"); }
		public static GameObject CreateToggle(Resources r) { return new GameObject("Toggle"); }
		public static GameObject CreateDropdown(Resources r) { return new GameObject("Dropdown"); }
		public static GameObject CreateSlider(Resources r) { return new GameObject("Slider"); }
		public static GameObject CreateScrollbar(Resources r) { return new GameObject("Scrollbar"); }
		public static GameObject CreateScrollView(Resources r) { return new GameObject("Scroll View"); }
	}
}

namespace UnityEngine.UI.Extensions
{
	public class UIPrimitiveBase : MaskableGraphic { }
	public class UILineRenderer : UIPrimitiveBase
	{
		public Vector2[] Points { get; set; }
		public float lineThickness { get; set; }
		public float LineThickness { get; set; }
		public bool RelativeSize { get; set; }
		public bool LineList { get; set; }
		public bool LineCaps { get; set; }
	}
}

public class FlexibleColorPicker : UnityEngine.MonoBehaviour
{
	public UnityEngine.UI.Button OKButton;
	public UnityEngine.Color color;
	public UnityEngine.Color GetColor() { return color; }
	public void SetColor(UnityEngine.Color c) { color = c; }
	public UnityEngine.Color startingColor;
}

namespace Newtonsoft.Json
{
	public enum Formatting { None, Indented }
	public abstract class JsonReader : IDisposable { public void Close() { } public void Dispose() { } }
	public abstract class JsonWriter : IDisposable { public Formatting Formatting { get; set; } public void Close() { } public void Dispose() { } }
	public class JsonTextReader : JsonReader { public JsonTextReader(System.IO.TextReader r) { } }
	public class JsonTextWriter : JsonWriter { public JsonTextWriter(System.IO.TextWriter w) { } public int Indentation { get; set; } public char IndentChar { get; set; } }
	public class JsonSerializer
	{
		public Formatting Formatting { get; set; }
		public object Deserialize(JsonReader r) { return null; }
		public T Deserialize<T>(JsonReader r) { return default(T); }
		public void Serialize(JsonWriter w, object o) { }
		public void Serialize(System.IO.TextWriter w, object o) { }
	}
	public static class JsonConvert
	{
		public static string SerializeObject(object o) { return "{}"; }
		public static string SerializeObject(object o, Formatting f) { return "{}"; }
		public static T DeserializeObject<T>(string s) { return default(T); }
		public static object DeserializeObject(string s) { return null; }
	}
}
