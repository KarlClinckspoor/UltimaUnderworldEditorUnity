// Compile-only stand-ins for the Unity API the editor uses, so the editor's scripts build and
// the data code runs outside Unity. Texture2D keeps its pixels; Color <-> Color32 conversion
// rounds the way Unity does. Everything else is inert.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
	public class Object
	{
		public string name;
		public HideFlags hideFlags;
		public static void Destroy(Object o) { }
		public static void Destroy(Object o, float t) { }
		public static void DestroyImmediate(Object o) { }
		public static void DontDestroyOnLoad(Object o) { }
		public static T Instantiate<T>(T original) where T : Object { return original; }
		public static T Instantiate<T>(T original, Transform parent) where T : Object { return original; }
		public static T Instantiate<T>(T original, Transform parent, bool worldPositionStays) where T : Object { return original; }
		public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation) where T : Object { return original; }
		public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object { return original; }
		public static Object Instantiate(Object original) { return original; }
		public static T FindObjectOfType<T>() where T : Object { return null; }
		public static T[] FindObjectsOfType<T>() where T : Object { return new T[0]; }
		public static Object FindObjectOfType(Type t) { return null; }
		public int GetInstanceID() { return 0; }
		public static implicit operator bool(Object o) { return !ReferenceEquals(o, null); }
		public override string ToString() { return name; }
	}
	[Flags] public enum HideFlags { None = 0, HideAndDontSave = 61, DontSave = 52 }
	public class Component : Object
	{
		public GameObject gameObject { get { return _go ?? (_go = new GameObject()); } }
		internal GameObject _go;
		public Transform transform { get { return gameObject.transform; } }
		public string tag { get; set; }
		public T GetComponent<T>() { return gameObject.GetComponent<T>(); }
		public Component GetComponent(Type t) { return null; }
		public Component GetComponent(string t) { return null; }
		public bool TryGetComponent<T>(out T c) { c = default(T); return false; }
		public T GetComponentInChildren<T>() { return default(T); }
		public T GetComponentInChildren<T>(bool inactive) { return default(T); }
		public T[] GetComponentsInChildren<T>() { return new T[0]; }
		public T[] GetComponentsInChildren<T>(bool inactive) { return new T[0]; }
		public void GetComponentsInChildren<T>(bool inactive, List<T> result) { }
		public T GetComponentInParent<T>() { return default(T); }
		public T[] GetComponents<T>() { return new T[0]; }
		public T[] GetComponentsInParent<T>() { return new T[0]; }
		public bool CompareTag(string t) { return false; }
		public void SendMessage(string m) { }
		public void BroadcastMessage(string m) { }
	}
	public class Behaviour : Component { public bool enabled { get; set; } public bool isActiveAndEnabled { get { return true; } } }
	public class MonoBehaviour : Behaviour
	{
		public Coroutine StartCoroutine(IEnumerator r) { return new Coroutine(); }
		public Coroutine StartCoroutine(string r) { return new Coroutine(); }
		public void StopCoroutine(Coroutine c) { }
		public void StopCoroutine(IEnumerator c) { }
		public void StopAllCoroutines() { }
		public void Invoke(string m, float t) { }
		public void InvokeRepeating(string m, float t, float r) { }
		public void CancelInvoke() { }
		public static void print(object o) { }
		public bool useGUILayout;
	}
	public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject, new() { return new T(); } }
	public sealed class Coroutine : YieldInstruction { }
	public class YieldInstruction { }
	public sealed class WaitForSeconds : YieldInstruction { public WaitForSeconds(float s) { } }
	public sealed class WaitForEndOfFrame : YieldInstruction { }
	public sealed class WaitForFixedUpdate : YieldInstruction { }
	public sealed class WaitForSecondsRealtime : CustomYieldInstruction { public WaitForSecondsRealtime(float s) { } public override bool keepWaiting { get { return false; } } }
	public sealed class WaitUntil : CustomYieldInstruction { public WaitUntil(Func<bool> f) { } public override bool keepWaiting { get { return false; } } }
	public sealed class WaitWhile : CustomYieldInstruction { public WaitWhile(Func<bool> f) { } public override bool keepWaiting { get { return false; } } }
	public abstract class CustomYieldInstruction : IEnumerator { public abstract bool keepWaiting { get; } public object Current { get { return null; } } public bool MoveNext() { return keepWaiting; } public void Reset() { } }

	public class GameObject : Object
	{
		readonly List<object> comps = new List<object>();
		Transform tr;
		public GameObject() { }
		public GameObject(string name) { this.name = name; }
		public GameObject(string name, params Type[] components) { this.name = name; foreach (var t in components) AddComponent(t); }
		public Transform transform { get { if (tr == null) { tr = new RectTransform(); tr._go = this; } return tr; } }
		public bool activeSelf { get; private set; } = true;
		public bool activeInHierarchy { get { return activeSelf; } }
		public string tag { get; set; }
		public int layer { get; set; }
		public GameObject gameObject { get { return this; } }
		public bool isStatic;
		public void SetActive(bool a) { activeSelf = a; }
		public T AddComponent<T>() where T : Component, new() { var c = new T(); c._go = this; comps.Add(c); return c; }
		public Component AddComponent(Type t) { var c = (Component)Activator.CreateInstance(t); c._go = this; comps.Add(c); return c; }
		public T GetComponent<T>() { foreach (var c in comps) if (c is T) return (T)c; if (tr is T) return (T)(object)tr; return default(T); }
		public Component GetComponent(Type t) { foreach (var c in comps) if (t.IsInstanceOfType(c)) return (Component)c; return null; }
		public bool TryGetComponent<T>(out T c) { c = GetComponent<T>(); return c != null; }
		public T GetComponentInChildren<T>() { return GetComponent<T>(); }
		public T GetComponentInChildren<T>(bool inactive) { return GetComponent<T>(); }
		public T[] GetComponentsInChildren<T>() { return new T[0]; }
		public T[] GetComponentsInChildren<T>(bool inactive) { return new T[0]; }
		public T GetComponentInParent<T>() { return default(T); }
		public T[] GetComponents<T>() { return new T[0]; }
		public static GameObject Find(string n) { return null; }
		public static GameObject FindWithTag(string n) { return null; }
		public static GameObject FindGameObjectWithTag(string n) { return null; }
		public static GameObject[] FindGameObjectsWithTag(string n) { return new GameObject[0]; }
		public static GameObject CreatePrimitive(PrimitiveType t) { return new GameObject(); }
		public bool CompareTag(string t) { return false; }
		public void SendMessage(string m) { }
	}
	public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
	public class Transform : Component, IEnumerable
	{
		readonly List<Transform> children = new List<Transform>();
		Transform parentT;
		public Vector3 position { get; set; }
		public Vector3 localPosition { get; set; }
		public Vector3 localScale { get; set; } = Vector3.one;
		public Vector3 lossyScale { get { return localScale; } }
		public Vector3 eulerAngles { get; set; }
		public Vector3 localEulerAngles { get; set; }
		public Quaternion rotation { get; set; }
		public Quaternion localRotation { get; set; }
		public Vector3 forward { get; set; }
		public Vector3 up { get; set; }
		public Vector3 right { get; set; }
		public Transform parent { get { return parentT; } set { SetParent(value); } }
		public Transform root { get { var t = this; while (t.parentT != null) t = t.parentT; return t; } }
		public int childCount { get { return children.Count; } }
		public Transform GetChild(int i) { return children[i]; }
		public void SetParent(Transform p) { SetParent(p, true); }
		public void SetParent(Transform p, bool worldStays) { if (parentT != null) parentT.children.Remove(this); parentT = p; if (p != null) p.children.Add(this); }
		public void SetAsLastSibling() { }
		public void SetAsFirstSibling() { }
		public void SetSiblingIndex(int i) { }
		public int GetSiblingIndex() { return parentT == null ? 0 : parentT.children.IndexOf(this); }
		public Transform Find(string n) { foreach (var c in children) if (c.name == n || c.gameObject.name == n) return c; return null; }
		public Transform FindChild(string n) { return Find(n); }
		public bool IsChildOf(Transform t) { return false; }
		public void DetachChildren() { children.Clear(); }
		public void Translate(Vector3 v) { }
		public void Translate(float x, float y, float z) { }
		public void Translate(Vector3 v, Space s) { }
		public void Translate(float x, float y, float z, Space s) { }
		public void Rotate(Vector3 v) { }
		public void Rotate(float x, float y, float z) { }
		public void Rotate(Vector3 axis, float angle) { }
		public void RotateAround(Vector3 p, Vector3 axis, float angle) { }
		public void LookAt(Transform t) { }
		public void LookAt(Vector3 t) { }
		public Vector3 TransformPoint(Vector3 v) { return v; }
		public Vector3 InverseTransformPoint(Vector3 v) { return v; }
		public Vector3 TransformDirection(Vector3 v) { return v; }
		public Vector3 InverseTransformDirection(Vector3 v) { return v; }
		public IEnumerator GetEnumerator() { return children.ToArray().GetEnumerator(); }
	}
	public class RectTransform : Transform
	{
		public Vector2 anchoredPosition { get; set; }
		public Vector3 anchoredPosition3D { get; set; }
		public Vector2 sizeDelta { get; set; }
		public Vector2 anchorMin { get; set; }
		public Vector2 anchorMax { get; set; }
		public Vector2 pivot { get; set; }
		public Vector2 offsetMin { get; set; }
		public Vector2 offsetMax { get; set; }
		public Rect rect { get { return new Rect(0, 0, sizeDelta.x, sizeDelta.y); } }
		public void SetSizeWithCurrentAnchors(Axis axis, float size) { }
		public void SetInsetAndSizeFromParentEdge(Edge e, float inset, float size) { }
		public void GetWorldCorners(Vector3[] c) { }
		public void GetLocalCorners(Vector3[] c) { }
		public enum Axis { Horizontal, Vertical }
		public enum Edge { Left, Right, Top, Bottom }
		public void ForceUpdateRectTransforms() { }
	}
	public static class RectTransformUtility
	{
		public static bool ScreenPointToLocalPointInRectangle(RectTransform r, Vector2 p, Camera c, out Vector2 local) { local = p; return true; }
		public static bool RectangleContainsScreenPoint(RectTransform r, Vector2 p) { return false; }
		public static bool RectangleContainsScreenPoint(RectTransform r, Vector2 p, Camera c) { return false; }
		public static bool ScreenPointToWorldPointInRectangle(RectTransform r, Vector2 p, Camera c, out Vector3 w) { w = p; return true; }
		public static Vector2 WorldToScreenPoint(Camera c, Vector3 p) { return p; }
	}

	public struct Vector2
	{
		public float x, y;
		public Vector2(float x, float y) { this.x = x; this.y = y; }
		public static Vector2 zero { get { return new Vector2(0, 0); } }
		public static Vector2 one { get { return new Vector2(1, 1); } }
		public static Vector2 up { get { return new Vector2(0, 1); } }
		public static Vector2 down { get { return new Vector2(0, -1); } }
		public static Vector2 left { get { return new Vector2(-1, 0); } }
		public static Vector2 right { get { return new Vector2(1, 0); } }
		public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
		public float sqrMagnitude { get { return x * x + y * y; } }
		public Vector2 normalized { get { float m = magnitude; return m > 0 ? new Vector2(x / m, y / m) : zero; } }
		public float this[int i] { get { return i == 0 ? x : y; } set { if (i == 0) x = value; else y = value; } }
		public static float Distance(Vector2 a, Vector2 b) { return (a - b).magnitude; }
		public static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
		public static float Angle(Vector2 a, Vector2 b) { return 0; }
		public static float SignedAngle(Vector2 a, Vector2 b) { return 0; }
		public static Vector2 Lerp(Vector2 a, Vector2 b, float t) { return a + (b - a) * t; }
		public static Vector2 Min(Vector2 a, Vector2 b) { return new Vector2(Math.Min(a.x, b.x), Math.Min(a.y, b.y)); }
		public static Vector2 Max(Vector2 a, Vector2 b) { return new Vector2(Math.Max(a.x, b.x), Math.Max(a.y, b.y)); }
		public static Vector2 Scale(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
		public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
		public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
		public static Vector2 operator -(Vector2 a) { return new Vector2(-a.x, -a.y); }
		public static Vector2 operator *(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }
		public static Vector2 operator *(float d, Vector2 a) { return new Vector2(a.x * d, a.y * d); }
		public static Vector2 operator *(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
		public static Vector2 operator /(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }
		public static Vector2 operator /(Vector2 a, Vector2 b) { return new Vector2(a.x / b.x, a.y / b.y); }
		public static bool operator ==(Vector2 a, Vector2 b) { return a.x == b.x && a.y == b.y; }
		public static bool operator !=(Vector2 a, Vector2 b) { return !(a == b); }
		public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0); }
		public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
		public override bool Equals(object o) { return o is Vector2 && this == (Vector2)o; }
		public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() << 2; }
		public void Set(float x, float y) { this.x = x; this.y = y; }
		public override string ToString() { return "(" + x + ", " + y + ")"; }
	}
	public struct Vector3
	{
		public float x, y, z;
		public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
		public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
		public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
		public static Vector3 one { get { return new Vector3(1, 1, 1); } }
		public static Vector3 up { get { return new Vector3(0, 1, 0); } }
		public static Vector3 down { get { return new Vector3(0, -1, 0); } }
		public static Vector3 left { get { return new Vector3(-1, 0, 0); } }
		public static Vector3 right { get { return new Vector3(1, 0, 0); } }
		public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
		public static Vector3 back { get { return new Vector3(0, 0, -1); } }
		public float magnitude { get { return (float)Math.Sqrt(x * x + y * y + z * z); } }
		public float sqrMagnitude { get { return x * x + y * y + z * z; } }
		public Vector3 normalized { get { float m = magnitude; return m > 0 ? this / m : zero; } }
		public float this[int i] { get { return i == 0 ? x : i == 1 ? y : z; } set { if (i == 0) x = value; else if (i == 1) y = value; else z = value; } }
		public static float Distance(Vector3 a, Vector3 b) { return (a - b).magnitude; }
		public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
		public static Vector3 Cross(Vector3 a, Vector3 b) { return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x); }
		public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { return a + (b - a) * t; }
		public static Vector3 MoveTowards(Vector3 a, Vector3 b, float d) { return b; }
		public static Vector3 Normalize(Vector3 v) { return v.normalized; }
		public static Vector3 Scale(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
		public static Vector3 Min(Vector3 a, Vector3 b) { return new Vector3(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z)); }
		public static Vector3 Max(Vector3 a, Vector3 b) { return new Vector3(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z)); }
		public static float Angle(Vector3 a, Vector3 b) { return 0; }
		public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
		public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
		public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
		public static Vector3 operator *(Vector3 a, float d) { return new Vector3(a.x * d, a.y * d, a.z * d); }
		public static Vector3 operator *(float d, Vector3 a) { return a * d; }
		public static Vector3 operator /(Vector3 a, float d) { return new Vector3(a.x / d, a.y / d, a.z / d); }
		public static bool operator ==(Vector3 a, Vector3 b) { return a.x == b.x && a.y == b.y && a.z == b.z; }
		public static bool operator !=(Vector3 a, Vector3 b) { return !(a == b); }
		public override bool Equals(object o) { return o is Vector3 && this == (Vector3)o; }
		public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2; }
		public void Set(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
		public override string ToString() { return "(" + x + ", " + y + ", " + z + ")"; }
	}
	public struct Vector4 { public float x, y, z, w; public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; } public static Vector4 zero { get { return new Vector4(); } } }
	public struct Vector2Int : IEquatable<Vector2Int>
	{
		public int x { get; set; }
		public int y { get; set; }
		public Vector2Int(int x, int y) { this.x = x; this.y = y; }
		public static Vector2Int zero { get { return new Vector2Int(0, 0); } }
		public static Vector2Int one { get { return new Vector2Int(1, 1); } }
		public static Vector2Int up { get { return new Vector2Int(0, 1); } }
		public static Vector2Int down { get { return new Vector2Int(0, -1); } }
		public static Vector2Int left { get { return new Vector2Int(-1, 0); } }
		public static Vector2Int right { get { return new Vector2Int(1, 0); } }
		public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
		public int sqrMagnitude { get { return x * x + y * y; } }
		public int this[int i] { get { return i == 0 ? x : y; } set { if (i == 0) x = value; else y = value; } }
		public static float Distance(Vector2Int a, Vector2Int b) { return (a - b).magnitude; }
		public static Vector2Int operator +(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x + b.x, a.y + b.y); }
		public static Vector2Int operator -(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x - b.x, a.y - b.y); }
		public static Vector2Int operator -(Vector2Int a) { return new Vector2Int(-a.x, -a.y); }
		public static Vector2Int operator *(Vector2Int a, int d) { return new Vector2Int(a.x * d, a.y * d); }
		public static Vector2Int operator *(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x * b.x, a.y * b.y); }
		public static Vector2Int operator /(Vector2Int a, int d) { return new Vector2Int(a.x / d, a.y / d); }
		public static bool operator ==(Vector2Int a, Vector2Int b) { return a.x == b.x && a.y == b.y; }
		public static bool operator !=(Vector2Int a, Vector2Int b) { return !(a == b); }
		public static implicit operator Vector2(Vector2Int v) { return new Vector2(v.x, v.y); }
		public static explicit operator Vector3Int(Vector2Int v) { return new Vector3Int(v.x, v.y, 0); }
		public static Vector2Int RoundToInt(Vector2 v) { return new Vector2Int((int)Math.Round(v.x), (int)Math.Round(v.y)); }
		public static Vector2Int FloorToInt(Vector2 v) { return new Vector2Int((int)Math.Floor(v.x), (int)Math.Floor(v.y)); }
		public static Vector2Int CeilToInt(Vector2 v) { return new Vector2Int((int)Math.Ceiling(v.x), (int)Math.Ceiling(v.y)); }
		public static Vector2Int Min(Vector2Int a, Vector2Int b) { return new Vector2Int(Math.Min(a.x, b.x), Math.Min(a.y, b.y)); }
		public static Vector2Int Max(Vector2Int a, Vector2Int b) { return new Vector2Int(Math.Max(a.x, b.x), Math.Max(a.y, b.y)); }
		public void Set(int x, int y) { this.x = x; this.y = y; }
		public bool Equals(Vector2Int o) { return this == o; }
		public override bool Equals(object o) { return o is Vector2Int && this == (Vector2Int)o; }
		public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() << 2; }
		public override string ToString() { return "(" + x + ", " + y + ")"; }
	}
	public struct Vector3Int
	{
		public int x { get; set; }
		public int y { get; set; }
		public int z { get; set; }
		public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
		public static Vector3Int zero { get { return new Vector3Int(); } }
		public static bool operator ==(Vector3Int a, Vector3Int b) { return a.x == b.x && a.y == b.y && a.z == b.z; }
		public static bool operator !=(Vector3Int a, Vector3Int b) { return !(a == b); }
		public static implicit operator Vector3(Vector3Int v) { return new Vector3(v.x, v.y, v.z); }
		public override bool Equals(object o) { return o is Vector3Int && this == (Vector3Int)o; }
		public override int GetHashCode() { return x ^ y << 2 ^ z >> 2; }
	}
	public struct Quaternion
	{
		public float x, y, z, w;
		public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
		public static Quaternion identity { get { return new Quaternion(0, 0, 0, 1); } }
		public Vector3 eulerAngles { get { return Vector3.zero; } set { } }
		public static Quaternion Euler(float x, float y, float z) { return identity; }
		public static Quaternion Euler(Vector3 v) { return identity; }
		public static Quaternion AngleAxis(float a, Vector3 axis) { return identity; }
		public static Quaternion LookRotation(Vector3 f) { return identity; }
		public static Quaternion LookRotation(Vector3 f, Vector3 up) { return identity; }
		public static Quaternion Lerp(Quaternion a, Quaternion b, float t) { return b; }
		public static Quaternion Slerp(Quaternion a, Quaternion b, float t) { return b; }
		public static Quaternion operator *(Quaternion a, Quaternion b) { return a; }
		public static Vector3 operator *(Quaternion a, Vector3 b) { return b; }
	}
	public struct Rect
	{
		public float x { get; set; }
		public float y { get; set; }
		public float width { get; set; }
		public float height { get; set; }
		public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
		public Rect(Vector2 p, Vector2 s) { x = p.x; y = p.y; width = s.x; height = s.y; }
		public Vector2 size { get { return new Vector2(width, height); } set { width = value.x; height = value.y; } }
		public Vector2 position { get { return new Vector2(x, y); } set { x = value.x; y = value.y; } }
		public Vector2 center { get { return new Vector2(x + width / 2, y + height / 2); } }
		public Vector2 min { get { return new Vector2(x, y); } }
		public Vector2 max { get { return new Vector2(x + width, y + height); } }
		public float xMin { get { return x; } set { x = value; } }
		public float yMin { get { return y; } set { y = value; } }
		public float xMax { get { return x + width; } set { width = value - x; } }
		public float yMax { get { return y + height; } set { height = value - y; } }
		public bool Contains(Vector2 p) { return p.x >= x && p.x < x + width && p.y >= y && p.y < y + height; }
		public bool Contains(Vector3 p) { return Contains((Vector2)p); }
		public bool Overlaps(Rect r) { return true; }
		public static Rect zero { get { return new Rect(); } }
	}
	public struct RectInt { public int x, y, width, height; public RectInt(int x, int y, int w, int h) { this.x = x; this.y = y; width = w; height = h; } }
	public struct Bounds { public Vector3 center, size, min, max, extents; public Bounds(Vector3 c, Vector3 s) { center = c; size = s; min = c; max = c; extents = s; } public void Encapsulate(Vector3 p) { } public bool Contains(Vector3 p) { return false; } }

	public struct Color
	{
		public float r, g, b, a;
		public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
		public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; }
		public static Color white { get { return new Color(1, 1, 1, 1); } }
		public static Color black { get { return new Color(0, 0, 0, 1); } }
		public static Color clear { get { return new Color(0, 0, 0, 0); } }
		public static Color red { get { return new Color(1, 0, 0, 1); } }
		public static Color green { get { return new Color(0, 1, 0, 1); } }
		public static Color blue { get { return new Color(0, 0, 1, 1); } }
		public static Color yellow { get { return new Color(1, 0.9215686f, 0.01568628f, 1); } }
		public static Color cyan { get { return new Color(0, 1, 1, 1); } }
		public static Color magenta { get { return new Color(1, 0, 1, 1); } }
		public static Color gray { get { return new Color(0.5f, 0.5f, 0.5f, 1); } }
		public static Color grey { get { return gray; } }
		public float grayscale { get { return 0.299f * r + 0.587f * g + 0.114f * b; } }
		public float maxColorComponent { get { return Math.Max(r, Math.Max(g, b)); } }
		public float this[int i] { get { return i == 0 ? r : i == 1 ? g : i == 2 ? b : a; } set { if (i == 0) r = value; else if (i == 1) g = value; else if (i == 2) b = value; else a = value; } }
		public static Color Lerp(Color x, Color y, float t) { t = Mathf.Clamp01(t); return new Color(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t); }
		public static Color HSVToRGB(float h, float s, float v) { return white; }
		public static void RGBToHSV(Color c, out float h, out float s, out float v) { h = s = v = 0; }
		public static Color operator *(Color a, float d) { return new Color(a.r * d, a.g * d, a.b * d, a.a * d); }
		public static Color operator *(Color a, Color b) { return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a); }
		public static Color operator +(Color a, Color b) { return new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a); }
		public static Color operator -(Color a, Color b) { return new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a); }
		public static Color operator /(Color a, float d) { return new Color(a.r / d, a.g / d, a.b / d, a.a / d); }
		public static bool operator ==(Color a, Color b) { return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a; }
		public static bool operator !=(Color a, Color b) { return !(a == b); }
		public override bool Equals(object o) { return o is Color && this == (Color)o; }
		public override int GetHashCode() { return r.GetHashCode() ^ g.GetHashCode() << 2 ^ b.GetHashCode() >> 2 ^ a.GetHashCode() >> 1; }
		public static implicit operator Color(Color32 c) { return new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f); }
		public static implicit operator Vector4(Color c) { return new Vector4(c.r, c.g, c.b, c.a); }
		public override string ToString() { return string.Format("RGBA({0:F3}, {1:F3}, {2:F3}, {3:F3})", r, g, b, a); }
	}
	public struct Color32
	{
		public byte r, g, b, a;
		public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
		// Unity: (byte)(Mathf.Round(Mathf.Clamp01(c) * 255f))
		public static implicit operator Color32(Color c) { return new Color32(B(c.r), B(c.g), B(c.b), B(c.a)); }
		static byte B(float f) { return (byte)Mathf.Round(Mathf.Clamp01(f) * 255f); }
		public static Color32 Lerp(Color32 x, Color32 y, float t) { return Color.Lerp(x, y, t); }
		public override string ToString() { return string.Format("RGBA({0}, {1}, {2}, {3})", r, g, b, a); }
	}
	public static class ColorUtility
	{
		public static bool TryParseHtmlString(string s, out Color c) { c = Color.white; return false; }
		public static string ToHtmlStringRGB(Color c) { Color32 k = c; return k.r.ToString("X2") + k.g.ToString("X2") + k.b.ToString("X2"); }
		public static string ToHtmlStringRGBA(Color c) { Color32 k = c; return k.r.ToString("X2") + k.g.ToString("X2") + k.b.ToString("X2") + k.a.ToString("X2"); }
	}

	public static class Mathf
	{
		public const float PI = (float)Math.PI;
		public const float Infinity = float.PositiveInfinity;
		public const float NegativeInfinity = float.NegativeInfinity;
		public const float Deg2Rad = PI / 180f;
		public const float Rad2Deg = 180f / PI;
		public const float Epsilon = float.Epsilon;
		public static float Abs(float f) { return Math.Abs(f); }
		public static int Abs(int f) { return Math.Abs(f); }
		public static float Min(float a, float b) { return a < b ? a : b; }
		public static int Min(int a, int b) { return a < b ? a : b; }
		public static float Min(params float[] v) { float m = v[0]; foreach (var x in v) if (x < m) m = x; return m; }
		public static int Min(params int[] v) { int m = v[0]; foreach (var x in v) if (x < m) m = x; return m; }
		public static float Max(float a, float b) { return a > b ? a : b; }
		public static int Max(int a, int b) { return a > b ? a : b; }
		public static float Max(params float[] v) { float m = v[0]; foreach (var x in v) if (x > m) m = x; return m; }
		public static int Max(params int[] v) { int m = v[0]; foreach (var x in v) if (x > m) m = x; return m; }
		public static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
		public static int Clamp(int v, int a, int b) { return v < a ? a : v > b ? b : v; }
		public static float Clamp01(float v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
		public static float Round(float f) { return (float)Math.Round(f); }
		public static int RoundToInt(float f) { return (int)Math.Round(f); }
		public static float Floor(float f) { return (float)Math.Floor(f); }
		public static int FloorToInt(float f) { return (int)Math.Floor(f); }
		public static float Ceil(float f) { return (float)Math.Ceiling(f); }
		public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }
		public static float Sqrt(float f) { return (float)Math.Sqrt(f); }
		public static float Pow(float a, float b) { return (float)Math.Pow(a, b); }
		public static float Log(float f) { return (float)Math.Log(f); }
		public static float Log(float f, float b) { return (float)Math.Log(f, b); }
		public static float Log10(float f) { return (float)Math.Log10(f); }
		public static float Exp(float f) { return (float)Math.Exp(f); }
		public static float Sin(float f) { return (float)Math.Sin(f); }
		public static float Cos(float f) { return (float)Math.Cos(f); }
		public static float Tan(float f) { return (float)Math.Tan(f); }
		public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
		public static float Atan(float f) { return (float)Math.Atan(f); }
		public static float Asin(float f) { return (float)Math.Asin(f); }
		public static float Acos(float f) { return (float)Math.Acos(f); }
		public static float Sign(float f) { return f >= 0 ? 1 : -1; }
		public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
		public static float LerpUnclamped(float a, float b, float t) { return a + (b - a) * t; }
		public static float InverseLerp(float a, float b, float v) { return a != b ? Clamp01((v - a) / (b - a)) : 0; }
		public static float MoveTowards(float a, float b, float d) { return Math.Abs(b - a) <= d ? b : a + Math.Sign(b - a) * d; }
		public static float Repeat(float t, float l) { return Clamp(t - Floor(t / l) * l, 0, l); }
		public static float PingPong(float t, float l) { t = Repeat(t, l * 2); return l - Abs(t - l); }
		public static float SmoothStep(float a, float b, float t) { return Lerp(a, b, t); }
		public static bool Approximately(float a, float b) { return Math.Abs(a - b) < 1e-6f; }
		public static int NextPowerOfTwo(int v) { int p = 1; while (p < v) p <<= 1; return p; }
		public static bool IsPowerOfTwo(int v) { return v > 0 && (v & (v - 1)) == 0; }
	}
	public static class Random
	{
		static System.Random r = new System.Random(1);
		public static float value { get { return (float)r.NextDouble(); } }
		public static int Range(int a, int b) { return r.Next(a, b); }
		public static float Range(float a, float b) { return a + (float)r.NextDouble() * (b - a); }
		public static void InitState(int s) { r = new System.Random(s); }
		public static Vector2 insideUnitCircle { get { return Vector2.zero; } }
		public static int seed { set { r = new System.Random(value); } }
	}
	public static class Debug
	{
		public static Action<string> Sink;
		public static void Log(object m) { if (Sink != null) Sink("" + m); }
		public static void Log(object m, Object c) { Log(m); }
		public static void LogWarning(object m) { Log(m); }
		public static void LogWarning(object m, Object c) { Log(m); }
		public static void LogError(object m) { Log(m); }
		public static void LogError(object m, Object c) { Log(m); }
		public static void LogException(Exception e) { Log(e); }
		public static void LogFormat(string f, params object[] a) { Log(string.Format(f, a)); }
		public static void LogWarningFormat(string f, params object[] a) { Log(string.Format(f, a)); }
		public static void LogErrorFormat(string f, params object[] a) { Log(string.Format(f, a)); }
		public static void Assert(bool c) { }
		public static void Assert(bool c, object m) { }
		public static void DrawLine(Vector3 a, Vector3 b) { }
		public static void DrawLine(Vector3 a, Vector3 b, Color c) { }
		public static void DrawRay(Vector3 a, Vector3 b) { }
		public static void Break() { }
		public static bool isDebugBuild { get { return true; } }
	}
	public static class QualitySettings { public static int vSyncCount; }
	public static class Application
	{
		public static string dataPath = ".";
		public static string streamingAssetsPath = "StreamingAssets";
		public static string persistentDataPath = ".";
		public static string temporaryCachePath = ".";
		public static string version = "test";
		public static string productName = "test";
		public static string companyName = "test";
		public static string unityVersion = "2020.3.32f1";
		public static bool isEditor { get { return false; } }
		public static bool isPlaying { get { return true; } }
		public static bool isFocused { get { return true; } }
		public static RuntimePlatform platform { get { return RuntimePlatform.WindowsPlayer; } }
		public static int targetFrameRate;
		public static bool runInBackground;
		public static void Quit() { }
		public static void Quit(int c) { }
		public static void OpenURL(string u) { }
		public static event Action<string, string, LogType> logMessageReceived { add { } remove { } }
		public static event Action quitting { add { } remove { } }
		public delegate void LogCallback(string condition, string stackTrace, LogType type);
	}
	public enum RuntimePlatform { WindowsPlayer, WindowsEditor, OSXPlayer, OSXEditor, LinuxPlayer, LinuxEditor, WebGLPlayer }
	public enum LogType { Error, Assert, Warning, Log, Exception }
	public static class Time
	{
		public static float deltaTime { get { return 0.016f; } }
		public static float unscaledDeltaTime { get { return 0.016f; } }
		public static float time { get { return 0; } }
		public static float realtimeSinceStartup { get { return 0; } }
		public static float unscaledTime { get { return 0; } }
		public static int frameCount { get { return 0; } }
		public static float timeScale { get; set; }
		public static float fixedDeltaTime { get; set; }
	}
	public static class Screen
	{
		public static int width { get { return 1920; } }
		public static int height { get { return 1080; } }
		public static float dpi { get { return 96; } }
		public static bool fullScreen { get; set; }
		public static FullScreenMode fullScreenMode { get; set; }
		public static Resolution currentResolution { get { return new Resolution(); } }
		public static Resolution[] resolutions { get { return new Resolution[0]; } }
		public static void SetResolution(int w, int h, bool f) { }
		public static void SetResolution(int w, int h, FullScreenMode f) { }
		public static Rect safeArea { get { return new Rect(0, 0, width, height); } }
	}
	public struct Resolution { public int width, height, refreshRate; }
	public enum FullScreenMode { ExclusiveFullScreen, FullScreenWindow, MaximizedWindow, Windowed }
	public static class Cursor { public static bool visible { get; set; } public static CursorLockMode lockState { get; set; } public static void SetCursor(Texture2D t, Vector2 h, CursorMode m) { } }
	public enum CursorLockMode { None, Locked, Confined }
	public enum CursorMode { Auto, ForceSoftware }
	public static class Input
	{
		public static Vector3 mousePosition { get { return Vector3.zero; } }
		public static Vector2 mouseScrollDelta { get { return Vector2.zero; } }
		public static bool anyKey { get { return false; } }
		public static bool anyKeyDown { get { return false; } }
		public static string inputString { get { return ""; } }
		public static bool GetKey(KeyCode k) { return false; }
		public static bool GetKeyDown(KeyCode k) { return false; }
		public static bool GetKeyUp(KeyCode k) { return false; }
		public static bool GetKey(string k) { return false; }
		public static bool GetKeyDown(string k) { return false; }
		public static bool GetMouseButton(int b) { return false; }
		public static bool GetMouseButtonDown(int b) { return false; }
		public static bool GetMouseButtonUp(int b) { return false; }
		public static float GetAxis(string a) { return 0; }
		public static float GetAxisRaw(string a) { return 0; }
		public static bool GetButton(string a) { return false; }
		public static bool GetButtonDown(string a) { return false; }
		public static bool mousePresent { get { return true; } }
		public static int touchCount { get { return 0; } }
	}
	public enum KeyCode
	{
		None, Backspace, Delete, Tab, Clear, Return, Pause, Escape, Space, Keypad0, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9, KeypadPeriod, KeypadDivide, KeypadMultiply, KeypadMinus, KeypadPlus, KeypadEnter, KeypadEquals, UpArrow, DownArrow, RightArrow, LeftArrow, Insert, Home, End, PageUp, PageDown,
		F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9, Exclaim, DoubleQuote, Hash, Dollar, Ampersand, Quote, LeftParen, RightParen, Asterisk, Plus, Comma, Minus, Period, Slash, Colon, Semicolon, Less, Equals, Greater, Question, At, LeftBracket, Backslash, RightBracket, Caret, Underscore, BackQuote,
		A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z, Numlock, CapsLock, ScrollLock, RightShift, LeftShift, RightControl, LeftControl, RightAlt, LeftAlt, LeftCommand, LeftApple, LeftWindows, RightCommand, RightApple, RightWindows, AltGr, Help, Print, SysReq, Break, Menu,
		Mouse0, Mouse1, Mouse2, Mouse3, Mouse4, Mouse5, Mouse6
	}
	public static class PlayerPrefs
	{
		static readonly Dictionary<string, object> d = new Dictionary<string, object>();
		public static int GetInt(string k, int def = 0) { object v; return d.TryGetValue(k, out v) ? (int)v : def; }
		public static float GetFloat(string k, float def = 0) { object v; return d.TryGetValue(k, out v) ? (float)v : def; }
		public static string GetString(string k, string def = "") { object v; return d.TryGetValue(k, out v) ? (string)v : def; }
		public static void SetInt(string k, int v) { d[k] = v; }
		public static void SetFloat(string k, float v) { d[k] = v; }
		public static void SetString(string k, string v) { d[k] = v; }
		public static bool HasKey(string k) { return d.ContainsKey(k); }
		public static void DeleteKey(string k) { d.Remove(k); }
		public static void Save() { }
	}
	public static class GUIUtility { public static string systemCopyBuffer { get; set; } public static int hotControl { get; set; } public static int keyboardControl { get; set; } }
	public static class Resources
	{
		public static T Load<T>(string p) where T : Object { return null; }
		public static Object Load(string p) { return null; }
		public static Object Load(string p, Type t) { return null; }
		public static T[] LoadAll<T>(string p) where T : Object { return new T[0]; }
		public static Object[] LoadAll(string p) { return new Object[0]; }
		public static void UnloadUnusedAssets() { }
		public static T GetBuiltinResource<T>(string p) where T : Object { return null; }
		public static Object GetBuiltinResource(Type t, string p) { return null; }
	}
	public enum TextureFormat { Alpha8 = 1, ARGB4444, RGB24, RGBA32, ARGB32, RGB565 = 7, R16 = 9, DXT1, DXT5 = 12, RGBA4444, BGRA32, RHalf, RGHalf, RGBAHalf, RFloat, RGFloat, RGBAFloat, R8 = 63 }
	public enum FilterMode { Point, Bilinear, Trilinear }
	public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }
	public class Texture : Object
	{
		public virtual int width { get; set; }
		public virtual int height { get; set; }
		public FilterMode filterMode { get; set; }
		public TextureWrapMode wrapMode { get; set; }
		public int anisoLevel { get; set; }
		public float mipMapBias { get; set; }
	}
	/// <summary>Keeps its pixels (as Color32, rounded the way Unity stores RGBA32).</summary>
	public class Texture2D : Texture
	{
		Color32[] px;
		public TextureFormat format { get; private set; }
		public bool alphaIsTransparency { get; set; }
		public bool isReadable { get { return true; } }
		public int mipmapCount { get { return 1; } }
		public Texture2D(int w, int h) : this(w, h, TextureFormat.RGBA32, true) { }
		public Texture2D(int w, int h, TextureFormat f, bool mip) { width = w; height = h; format = f; px = new Color32[w * h]; }
		public Texture2D(int w, int h, TextureFormat f, bool mip, bool linear) : this(w, h, f, mip) { }
		public Texture2D(int w, int h, TextureFormat f, int mipCount, bool linear) : this(w, h, f, mipCount > 1) { }
		public void SetPixel(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < width && y < height) px[y * width + x] = c; }
		public void SetPixel(int x, int y, Color c, int mip) { SetPixel(x, y, c); }
		public Color GetPixel(int x, int y) { x = Math.Max(0, Math.Min(width - 1, x)); y = Math.Max(0, Math.Min(height - 1, y)); return px[y * width + x]; }
		public Color GetPixel(int x, int y, int mip) { return GetPixel(x, y); }
		public Color GetPixelBilinear(float u, float v) { return GetPixel((int)(u * width), (int)(v * height)); }
		public void SetPixels(Color[] c) { for (int i = 0; i < px.Length && i < c.Length; i++) px[i] = c[i]; }
		public void SetPixels(Color[] c, int mip) { SetPixels(c); }
		public void SetPixels(int x, int y, int w, int h, Color[] c) { for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) SetPixel(x + i, y + j, c[j * w + i]); }
		public void SetPixels32(Color32[] c) { for (int i = 0; i < px.Length && i < c.Length; i++) px[i] = c[i]; }
		public void SetPixels32(Color32[] c, int mip) { SetPixels32(c); }
		public void SetPixels32(int x, int y, int w, int h, Color32[] c) { for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) if (x + i < width && y + j < height) px[(y + j) * width + x + i] = c[j * w + i]; }
		public Color[] GetPixels() { var r = new Color[px.Length]; for (int i = 0; i < px.Length; i++) r[i] = px[i]; return r; }
		public Color[] GetPixels(int mip) { return GetPixels(); }
		public Color[] GetPixels(int x, int y, int w, int h) { var r = new Color[w * h]; for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) r[j * w + i] = GetPixel(x + i, y + j); return r; }
		public Color32[] GetPixels32() { return (Color32[])px.Clone(); }
		public Color32[] GetPixels32(int mip) { return GetPixels32(); }
		public void Apply() { }
		public void Apply(bool updateMipmaps) { }
		public void Apply(bool updateMipmaps, bool makeNoLongerReadable) { }
		public bool Resize(int w, int h) { width = w; height = h; px = new Color32[w * h]; return true; }
		public bool Reinitialize(int w, int h) { return Resize(w, h); }
		public bool LoadImage(byte[] data) { return false; }
		public bool LoadImage(byte[] data, bool markNonReadable) { return false; }
		public void LoadRawTextureData(byte[] d) { }
		public byte[] GetRawTextureData() { return new byte[0]; }
		public byte[] EncodeToPNG() { return new byte[0]; }
		public byte[] EncodeToJPG() { return new byte[0]; }
		public void ReadPixels(Rect r, int x, int y) { }
		public static Texture2D whiteTexture { get { var t = new Texture2D(4, 4); return t; } }
		public static Texture2D blackTexture { get { return new Texture2D(4, 4); } }
	}
	public static class ImageConversion
	{
		public static byte[] EncodeToPNG(this Texture2D t) { return new byte[0]; }
		public static bool LoadImage(this Texture2D t, byte[] d) { return false; }
	}
	public class RenderTexture : Texture { public RenderTexture(int w, int h, int d) { width = w; height = h; } public static RenderTexture active { get; set; } public bool Create() { return true; } public void Release() { } }
	public class Sprite : Object
	{
		public Texture2D texture { get; private set; }
		public Rect rect { get; private set; }
		public Vector2 pivot { get; private set; }
		public float pixelsPerUnit { get; private set; }
		public Bounds bounds { get { return new Bounds(); } }
		public Vector4 border { get; private set; }
		public static Sprite Create(Texture2D t, Rect r, Vector2 p) { return new Sprite { texture = t, rect = r, pivot = p, pixelsPerUnit = 100 }; }
		public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu) { return new Sprite { texture = t, rect = r, pivot = p, pixelsPerUnit = ppu }; }
		public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint extrude) { return Create(t, r, p, ppu); }
		public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint extrude, SpriteMeshType m) { return Create(t, r, p, ppu); }
		public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu, uint extrude, SpriteMeshType m, Vector4 border) { return Create(t, r, p, ppu); }
	}
	public enum SpriteMeshType { FullRect, Tight }
	public class Shader : Object { public static Shader Find(string n) { return new Shader(); } public static int PropertyToID(string n) { return 0; } }
	public class Material : Object
	{
		public Material(Shader s) { }
		public Material(Material m) { }
		public Color color { get; set; }
		public Texture mainTexture { get; set; }
		public Shader shader { get; set; }
		public Vector2 mainTextureScale { get; set; }
		public Vector2 mainTextureOffset { get; set; }
		public int renderQueue { get; set; }
		public void SetColor(string n, Color c) { }
		public void SetColor(int n, Color c) { }
		public void SetFloat(string n, float f) { }
		public void SetFloat(int n, float f) { }
		public void SetInt(string n, int f) { }
		public void SetTexture(string n, Texture t) { }
		public void SetTexture(int n, Texture t) { }
		public void SetVector(string n, Vector4 v) { }
		public Color GetColor(string n) { return Color.white; }
		public float GetFloat(string n) { return 0; }
		public Texture GetTexture(string n) { return null; }
		public bool HasProperty(string n) { return false; }
		public void EnableKeyword(string k) { }
		public void DisableKeyword(string k) { }
	}
	public class Mesh : Object
	{
		public Vector3[] vertices { get; set; }
		public int[] triangles { get; set; }
		public Vector2[] uv { get; set; }
		public Vector3[] normals { get; set; }
		public Color[] colors { get; set; }
		public Color32[] colors32 { get; set; }
		public int subMeshCount { get; set; }
		public Bounds bounds { get; set; }
		public UnityEngine.Rendering.IndexFormat indexFormat { get; set; }
		public void SetTriangles(int[] t, int sub) { }
		public void SetVertices(List<Vector3> v) { }
		public void SetTriangles(List<int> t, int sub) { }
		public void SetUVs(int ch, List<Vector2> uv) { }
		public void SetColors(List<Color> c) { }
		public void RecalculateNormals() { }
		public void RecalculateBounds() { }
		public void Clear() { }
		public void Optimize() { }
		public void CombineMeshes(CombineInstance[] c) { }
		public void CombineMeshes(CombineInstance[] c, bool merge) { }
		public void CombineMeshes(CombineInstance[] c, bool merge, bool useMatrices) { }
		public void MarkDynamic() { }
	}
	public class Renderer : Component
	{
		public Material material { get; set; }
		public Material sharedMaterial { get; set; }
		public Material[] materials { get; set; }
		public Material[] sharedMaterials { get; set; }
		public bool enabled { get; set; }
		public int sortingOrder { get; set; }
		public string sortingLayerName { get; set; }
		public int sortingLayerID { get; set; }
		public Bounds bounds { get { return new Bounds(); } }
	}
	public class MeshRenderer : Renderer { public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode { get; set; } public bool receiveShadows { get; set; } public UnityEngine.Rendering.ReflectionProbeUsage reflectionProbeUsage { get; set; } public UnityEngine.Rendering.LightProbeUsage lightProbeUsage { get; set; } }
	public class MeshCollider : Collider { public Mesh sharedMesh { get; set; } public bool convex { get; set; } }
	public struct CombineInstance { public Mesh mesh; public Matrix4x4 transform; public int subMeshIndex; }
	public struct Matrix4x4 { public static Matrix4x4 identity { get { return new Matrix4x4(); } } public static Matrix4x4 TRS(Vector3 p, Quaternion q, Vector3 s) { return identity; } public Vector3 MultiplyPoint(Vector3 v) { return v; } public Vector3 MultiplyPoint3x4(Vector3 v) { return v; } }
	public enum Space { World, Self }
	public class MeshFilter : Component { public Mesh mesh { get; set; } public Mesh sharedMesh { get; set; } }
	public class SpriteRenderer : Renderer
	{
		public Sprite sprite { get; set; }
		public Color color { get; set; }
		public bool flipX { get; set; }
		public bool flipY { get; set; }
		public Vector2 size { get; set; }
		public SpriteDrawMode drawMode { get; set; }
	}
	public enum SpriteDrawMode { Simple, Sliced, Tiled }
	public class LineRenderer : Renderer
	{
		public int positionCount { get; set; }
		public float startWidth { get; set; }
		public float endWidth { get; set; }
		public float widthMultiplier { get; set; }
		public Color startColor { get; set; }
		public Color endColor { get; set; }
		public bool useWorldSpace { get; set; }
		public bool loop { get; set; }
		public int numCapVertices { get; set; }
		public int numCornerVertices { get; set; }
		public void SetPosition(int i, Vector3 p) { }
		public void SetPositions(Vector3[] p) { }
		public Vector3 GetPosition(int i) { return Vector3.zero; }
		public int GetPositions(Vector3[] p) { return 0; }
		public void SetWidth(float a, float b) { }
		public void SetColors(Color a, Color b) { }
		public void SetVertexCount(int c) { }
	}
	public class Collider : Component { public bool enabled { get; set; } public Bounds bounds { get { return new Bounds(); } } }
	public class BoxCollider : Collider { public Vector3 size { get; set; } public Vector3 center { get; set; } }
	public class Collider2D : Behaviour { public Bounds bounds { get { return new Bounds(); } } public bool isTrigger { get; set; } public bool OverlapPoint(Vector2 p) { return false; } }
	public class BoxCollider2D : Collider2D { public Vector2 size { get; set; } public Vector2 offset { get; set; } }
	public class Rigidbody2D : Component { }
	public struct RaycastHit { public Collider collider; public Vector3 point; public Transform transform; public float distance; }
	public struct RaycastHit2D { public Collider2D collider; public Vector2 point; public Transform transform; public static implicit operator bool(RaycastHit2D h) { return h.collider != null; } }
	public struct Ray { public Vector3 origin, direction; public Ray(Vector3 o, Vector3 d) { origin = o; direction = d; } public Vector3 GetPoint(float d) { return origin; } }
	public struct Plane { public Plane(Vector3 n, Vector3 p) { } public bool Raycast(Ray r, out float d) { d = 0; return false; } }
	public static class Physics { public static bool Raycast(Ray r, out RaycastHit h) { h = new RaycastHit(); return false; } public static bool Raycast(Ray r, out RaycastHit h, float d) { h = new RaycastHit(); return false; } public static RaycastHit[] RaycastAll(Ray r) { return new RaycastHit[0]; } public static bool Raycast(Ray r, out RaycastHit h, float d, int mask) { h = new RaycastHit(); return false; } public static bool Raycast(Vector3 o, Vector3 d, out RaycastHit h, float dist) { h = new RaycastHit(); return false; } }
	public static class Physics2D { public static RaycastHit2D Raycast(Vector2 o, Vector2 d) { return new RaycastHit2D(); } public static RaycastHit2D Raycast(Vector2 o, Vector2 d, float dist) { return new RaycastHit2D(); } public static Collider2D OverlapPoint(Vector2 p) { return null; } public static RaycastHit2D[] RaycastAll(Vector2 o, Vector2 d) { return new RaycastHit2D[0]; } public static Collider2D[] OverlapPointAll(Vector2 p) { return new Collider2D[0]; } public static RaycastHit2D GetRayIntersection(Ray r) { return new RaycastHit2D(); } }
	public class Camera : Behaviour
	{
		public static Camera main { get { return null; } }
		public static Camera current { get { return null; } }
		public float orthographicSize { get; set; }
		public bool orthographic { get; set; }
		public float fieldOfView { get; set; }
		public float aspect { get; set; }
		public float nearClipPlane { get; set; }
		public float farClipPlane { get; set; }
		public Color backgroundColor { get; set; }
		public CameraClearFlags clearFlags { get; set; }
		public int cullingMask { get; set; }
		public float depth { get; set; }
		public Rect rect { get; set; }
		public Rect pixelRect { get; set; }
		public int pixelWidth { get { return Screen.width; } }
		public int pixelHeight { get { return Screen.height; } }
		public RenderTexture targetTexture { get; set; }
		public Vector3 ScreenToWorldPoint(Vector3 p) { return p; }
		public Vector3 WorldToScreenPoint(Vector3 p) { return p; }
		public Vector3 ScreenToViewportPoint(Vector3 p) { return p; }
		public Vector3 ViewportToWorldPoint(Vector3 p) { return p; }
		public Vector3 WorldToViewportPoint(Vector3 p) { return p; }
		public Vector3 ViewportToScreenPoint(Vector3 p) { return p; }
		public Ray ScreenPointToRay(Vector3 p) { return new Ray(p, Vector3.forward); }
		public void Render() { }
	}
	public enum CameraClearFlags { Skybox = 1, Color = 2, SolidColor = 2, Depth = 3, Nothing = 4 }
	public class Canvas : Behaviour
	{
		public float scaleFactor { get; set; }
		public RenderMode renderMode { get; set; }
		public int sortingOrder { get; set; }
		public bool overrideSorting { get; set; }
		public Canvas rootCanvas { get { return this; } }
		public Camera worldCamera { get; set; }
		public bool pixelPerfect { get; set; }
		public static void ForceUpdateCanvases() { }
	}
	public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
	public class CanvasGroup : Behaviour { public float alpha { get; set; } public bool interactable { get; set; } public bool blocksRaycasts { get; set; } public bool ignoreParentGroups { get; set; } }
	public class CanvasRenderer : Component { public void SetColor(Color c) { } public void SetAlpha(float a) { } public bool cull { get; set; } }
	public class Font : Object
	{
		public Font() { }
		public Font(string n) { }
		public int fontSize { get { return 14; } }
		public bool dynamic { get { return true; } }
		public Material material { get; set; }
		public static Font CreateDynamicFontFromOSFont(string n, int s) { return new Font(); }
		public static Font CreateDynamicFontFromOSFont(string[] n, int s) { return new Font(); }
		public static string[] GetOSInstalledFontNames() { return new string[0]; }
		public void RequestCharactersInTexture(string s) { }
		public void RequestCharactersInTexture(string s, int size, FontStyle st) { }
		public bool GetCharacterInfo(char c, out CharacterInfo i) { i = new CharacterInfo(); return false; }
		public bool GetCharacterInfo(char c, out CharacterInfo i, int size) { i = new CharacterInfo(); return false; }
		public bool GetCharacterInfo(char c, out CharacterInfo i, int size, FontStyle st) { i = new CharacterInfo(); return false; }
	}
	public struct CharacterInfo { public int advance; public int glyphWidth; }
	public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
	public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
	public enum HorizontalWrapMode { Wrap, Overflow }
	public enum VerticalWrapMode { Truncate, Overflow }
	public class TextGenerator { public TextGenerator() { } public float GetPreferredWidth(string s, TextGenerationSettings t) { return s.Length * 7; } public float GetPreferredHeight(string s, TextGenerationSettings t) { return 14; } }
	public struct TextGenerationSettings { public Font font; public int fontSize; public FontStyle fontStyle; public float scaleFactor; public Vector2 generationExtents; public bool richText; public float lineSpacing; public TextAnchor textAnchor; public HorizontalWrapMode horizontalOverflow; public VerticalWrapMode verticalOverflow; public bool resizeTextForBestFit; public Color color; public Vector2 pivot; public bool generateOutOfBounds; public bool updateBounds; public bool alignByGeometry; }
	public class AudioSource : Behaviour { public AudioClip clip; public void Play() { } public void Stop() { } public float volume; }
	public class AudioClip : Object { }
	public class Animator : Behaviour { }
	public class Light : Behaviour { }
	public class Event { public static Event current { get { return null; } } public KeyCode keyCode; public EventType type; public Vector2 mousePosition; public char character; public bool shift, control, alt, command; public void Use() { } }
	public enum EventType { MouseDown, MouseUp, MouseMove, MouseDrag, KeyDown, KeyUp, ScrollWheel, Repaint, Layout }
	public static class GUI { public static Color color { get; set; } public static bool Button(Rect r, string s) { return false; } public static void Label(Rect r, string s) { } public static void Box(Rect r, string s) { } public static int depth { get; set; } public static void DrawTexture(Rect r, Texture t) { } }
	public static class GUILayout { public static bool Button(string s) { return false; } public static void Label(string s) { } }
	public class GUIStyle { }
	public static class SystemInfo { public static string deviceUniqueIdentifier { get { return "x"; } } public static string operatingSystem { get { return "test"; } } public static int maxTextureSize { get { return 16384; } } public static int systemMemorySize { get { return 8192; } } public static string graphicsDeviceName { get { return "stub"; } } }
	public static class LayerMask { public static int GetMask(params string[] n) { return 0; } public static int NameToLayer(string n) { return 0; } }
	public static class JsonUtility { public static string ToJson(object o) { return "{}"; } public static string ToJson(object o, bool pretty) { return "{}"; } public static T FromJson<T>(string s) { return default(T); } public static void FromJsonOverwrite(string s, object o) { } }
	public static class Gizmos { public static Color color { get; set; } public static void DrawLine(Vector3 a, Vector3 b) { } public static void DrawWireCube(Vector3 c, Vector3 s) { } }

	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)] public class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)] public class TooltipAttribute : Attribute { public TooltipAttribute(string h) { } }
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)] public class SpaceAttribute : Attribute { public SpaceAttribute() { } public SpaceAttribute(float h) { } }
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)] public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)] public class MinAttribute : Attribute { public MinAttribute(float a) { } }
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)] public class TextAreaAttribute : Attribute { public TextAreaAttribute() { } public TextAreaAttribute(int a, int b) { } }
	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)] public class MultilineAttribute : Attribute { public MultilineAttribute() { } public MultilineAttribute(int a) { } }
	[AttributeUsage(AttributeTargets.All)] public class SerializeField : Attribute { }
	[AttributeUsage(AttributeTargets.All)] public class SerializeReference : Attribute { }
	[AttributeUsage(AttributeTargets.All)] public class HideInInspector : Attribute { }
	[AttributeUsage(AttributeTargets.All)] public class ContextMenu : Attribute { public ContextMenu(string n) { } }
	[AttributeUsage(AttributeTargets.All)] public class ExecuteInEditMode : Attribute { }
	[AttributeUsage(AttributeTargets.All)] public class ExecuteAlways : Attribute { }
	[AttributeUsage(AttributeTargets.All)] public class DisallowMultipleComponent : Attribute { }
	[AttributeUsage(AttributeTargets.All)] public class AddComponentMenu : Attribute { public AddComponentMenu(string n) { } public AddComponentMenu(string n, int o) { } }
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public class RequireComponent : Attribute { public RequireComponent(Type a) { } public RequireComponent(Type a, Type b) { } public RequireComponent(Type a, Type b, Type c) { } }
	[AttributeUsage(AttributeTargets.Method)] public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute() { } public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
	public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }
	[AttributeUsage(AttributeTargets.All)] public class CreateAssetMenuAttribute : Attribute { public string fileName, menuName; public int order; }
	[AttributeUsage(AttributeTargets.Class)] public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int o) { } }
	[AttributeUsage(AttributeTargets.Class)] public class SelectionBaseAttribute : Attribute { }
	[AttributeUsage(AttributeTargets.Field)] public class FormerlySerializedAsAttribute : Attribute { public FormerlySerializedAsAttribute(string n) { } }
}
namespace UnityEngine.Rendering { public enum IndexFormat { UInt16, UInt32 } public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly } public enum ReflectionProbeUsage { Off, BlendProbes, BlendProbesAndSkybox, Simple } public enum LightProbeUsage { Off, BlendProbes, UseProxyVolume, CustomProvided } }
namespace UnityEngine.Serialization { [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)] public class FormerlySerializedAsAttribute : System.Attribute { public FormerlySerializedAsAttribute(string n) { } } }
namespace UnityEngine.SceneManagement
{
	public struct Scene { public string name; public int buildIndex; public bool isLoaded; public UnityEngine.GameObject[] GetRootGameObjects() { return new UnityEngine.GameObject[0]; } }
	public enum LoadSceneMode { Single, Additive }
	public static class SceneManager
	{
		public static void LoadScene(int i) { }
		public static void LoadScene(string n) { }
		public static void LoadScene(int i, LoadSceneMode m) { }
		public static void LoadScene(string n, LoadSceneMode m) { }
		public static Scene GetActiveScene() { return new Scene(); }
		public static int sceneCount { get { return 1; } }
		public static event System.Action<Scene, LoadSceneMode> sceneLoaded { add { } remove { } }
	}
}
namespace UnityEngine.Events
{
	public delegate void UnityAction();
	public delegate void UnityAction<T0>(T0 a);
	public delegate void UnityAction<T0, T1>(T0 a, T1 b);
	public delegate void UnityAction<T0, T1, T2>(T0 a, T1 b, T2 c);
	public abstract class UnityEventBase { public void RemoveAllListeners() { } public int GetPersistentEventCount() { return 0; } public void SetPersistentListenerState(int i, UnityEventCallState s) { } }
	public enum UnityEventCallState { Off, EditorAndRuntime, RuntimeOnly }
	public class UnityEvent : UnityEventBase
	{
		readonly System.Collections.Generic.List<UnityAction> l = new System.Collections.Generic.List<UnityAction>();
		public void AddListener(UnityAction a) { l.Add(a); }
		public void RemoveListener(UnityAction a) { l.Remove(a); }
		public void Invoke() { foreach (var a in l.ToArray()) a(); }
	}
	public class UnityEvent<T0> : UnityEventBase
	{
		readonly System.Collections.Generic.List<UnityAction<T0>> l = new System.Collections.Generic.List<UnityAction<T0>>();
		public void AddListener(UnityAction<T0> a) { l.Add(a); }
		public void RemoveListener(UnityAction<T0> a) { l.Remove(a); }
		public void Invoke(T0 v) { foreach (var a in l.ToArray()) a(v); }
	}
	public class UnityEvent<T0, T1> : UnityEventBase
	{
		readonly System.Collections.Generic.List<UnityAction<T0, T1>> l = new System.Collections.Generic.List<UnityAction<T0, T1>>();
		public void AddListener(UnityAction<T0, T1> a) { l.Add(a); }
		public void RemoveListener(UnityAction<T0, T1> a) { l.Remove(a); }
		public void Invoke(T0 v, T1 w) { foreach (var a in l.ToArray()) a(v, w); }
	}
}
