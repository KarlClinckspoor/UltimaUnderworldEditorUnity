using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// Autosave: every so many minutes the editor runs its own Save (UIManager.AutoSave), the
	/// same as File > Save without the progress messages. The interval is set in Tools >
	/// Autosave: 10 minutes by default, 0 turns it off. It is kept in PlayerPrefs, so it holds
	/// for every game and survives a restart.
	///
	/// Like File > Save, it writes the game's files in place (DATA, or the save that is open);
	/// the originals keep their one-time .BKP copies. When it saves:
	/// - the interval has passed since the last save, File > Save or automatic (opening a game
	///   counts as one: the files are then as on disk);
	/// - something was clicked or typed since that save, so an idle editor writes nothing;
	/// - it is a quiet moment: no mouse button held, no text field being typed in, no key or
	///   click for the last two seconds, and no load or save running. Until then it waits.
	/// A failed autosave shows Save's own message and is tried again one interval later.
	///
	/// It installs itself on the UIManager after the scene loads, as ExtensionsMenu does, and
	/// again after File > Open reloads the scene.
	/// </summary>
	public sealed class Autosave : MonoBehaviour
	{
		public const int DefaultMinutes = 10, MaxMinutes = 1440;
		const string PrefKey = "UltimateEditor.AutosaveMinutes";
		const float IdleSeconds = 2f, ToastSeconds = 4f;

		/// <summary>Minutes between autosaves, 0 for off.</summary>
		public static int Minutes
		{
			get { return Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, DefaultMinutes), 0, MaxMinutes); }
			set { PlayerPrefs.SetInt(PrefKey, Mathf.Clamp(value, 0, MaxMinutes)); PlayerPrefs.Save(); }
		}

		/// <summary>The autosave of the current scene (null before it starts).</summary>
		public static Autosave Instance { get; private set; }

		UIManager ui;
		bool wasLoaded;
		float lastSave;             // realtime of the last save, or of opening the game
		float lastInput = -1000f;   // realtime of the last key, click or scroll
		int lastEditFrame = -1;     // frame of the last key or click: what counts as an edit
		bool editSinceSave;
		DateTime? lastAutosave;
		GameObject toast;
		Text toastText;
		float toastUntil;

		static bool hooked;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Install()
		{
			if (!hooked)
			{
				hooked = true;
				UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => Install();
			}
			var ui = UiKit.FindUIManager();
			if (!ui || ui.GetComponent<Autosave>()) return;
			ui.gameObject.AddComponent<Autosave>();
		}

		void Start()
		{
			ui = GetComponent<UIManager>();
			ui.Saved += OnSaved;
			Instance = this;
		}

		void OnDestroy()
		{
			if (ui) ui.Saved -= OnSaved;
			if (Instance == this) Instance = null;
		}

		void OnSaved()
		{
			lastSave = Time.realtimeSinceStartup;
			// a click during the save (it takes several frames) may have changed something
			// that was already written
			editSinceSave = lastEditFrame > ui.SaveStartFrame;
		}

		bool Loaded { get { return ui && ui.MapCreator != null && ui.MapCreator.IsInitialized; } }

		void Update()
		{
			float now = Time.realtimeSinceStartup;
			if (toast && toast.activeSelf && now > toastUntil) toast.SetActive(false);

			if (!Loaded)
			{
				wasLoaded = false;
				return;
			}
			if (!wasLoaded)
			{
				wasLoaded = true;
				lastSave = now;
				editSinceSave = false;
			}

			// Input.anyKeyDown includes the mouse buttons
			if (Input.anyKeyDown)
			{
				lastInput = now;
				lastEditFrame = Time.frameCount;
				editSinceSave = true;
			}
			if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2) || Input.mouseScrollDelta != Vector2.zero)
				lastInput = now;

			int minutes = Minutes;
			if (minutes <= 0 || !editSinceSave || ui.IsSaving || ui.IsLoading) return;
			if (now - lastSave < minutes * 60f) return;
			if (!QuietMoment(now)) return;
			if (ui.AutoSave(Done))
				lastSave = now;     // a failure is tried again one interval later
		}

		bool QuietMoment(float now)
		{
			if (now - lastInput < IdleSeconds) return false;
			if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)) return false;
			var es = EventSystem.current;
			var selected = es ? es.currentSelectedGameObject : null;
			if (selected)
			{
				var field = selected.GetComponent<InputField>();
				if (field && field.isFocused) return false;
			}
			return true;
		}

		void Done(bool ok)
		{
			if (!ok) return;    // Save has shown what failed
			lastAutosave = DateTime.Now;
			string text = "Autosaved at " + lastAutosave.Value.ToString("HH:mm");
			int problems = Uw2Graphics.Problems.Count;
			if (problems > 0)
				text += "; " + problems + (problems == 1 ? " picture was" : " pictures were") + " not written (File > Save lists them)";
			ShowToast(text);
		}

		/// <summary>A short note in the bottom right corner that clicks pass through.</summary>
		void ShowToast(string text)
		{
			if (!toast)
			{
				var rt = UiKit.Rect("AutosaveToast", ui.transform);
				rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1, 0);
				rt.anchoredPosition = new Vector2(-8, 8);
				var bg = rt.gameObject.AddComponent<Image>();
				bg.color = new Color(UiKit.BarColor.r, UiKit.BarColor.g, UiKit.BarColor.b, 0.9f);
				bg.raycastTarget = false;
				var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
				h.padding = new RectOffset(8, 8, 4, 4);
				h.childControlWidth = true; h.childControlHeight = true;
				var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
				fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
				fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
				toastText = UiKit.Label(rt, "", 11, FontStyle.Normal, UiKit.TextColor);
				toastText.horizontalOverflow = HorizontalWrapMode.Overflow;
				toast = rt.gameObject;
			}
			toastText.text = text;
			toast.SetActive(true);
			toast.transform.SetAsLastSibling();
			toastUntil = Time.realtimeSinceStartup + ToastSeconds;
		}

		/// <summary>What the autosave is waiting for, for the Tools > Autosave window.</summary>
		public static string Describe()
		{
			int minutes = Minutes;
			var a = Instance;
			string last = a != null && a.lastAutosave.HasValue ? " Last autosave at " + a.lastAutosave.Value.ToString("HH:mm") + "." : "";
			if (minutes <= 0) return "Autosave is off." + last;
			if (a == null || !a.Loaded) return "Autosave starts once a game is open.";
			if (a.ui.IsSaving) return "Saving...";
			if (!a.editSinceSave) return "Nothing edited since the last save." + last;
			float left = minutes * 60f - (Time.realtimeSinceStartup - a.lastSave);
			if (left <= 0) return "Due: waiting for a pause in typing or dragging." + last;
			string when = left < 60 ? (int)Math.Ceiling(left) + " s" : "about " + (int)Math.Ceiling(left / 60f) + " min";
			return "Next autosave in " + when + "." + last;
		}
	}

	/// <summary>Tools > Autosave: the interval, and what the autosave is doing.</summary>
	public static class AutosaveWindow
	{
		public static void Show()
		{
			var w = UiKit.Window("Autosave", 480, 200);
			UiKit.Paragraph(w.Toolbar,
				"Saves your work every so many minutes, as File > Save does: the game's files are written in place, " +
				"and the originals keep their one-time .BKP copies. It saves only after an edit since the last save, " +
				"and waits until you stop typing or dragging. 0 minutes turns it off. The setting holds for every game.", 10);
			var row = UiKit.Row(w.Content, 20);
			UiKit.Label(row, "Every (minutes)", 11, FontStyle.Bold, null, 110);
			InputField input = null;
			Action<int> set = m =>
			{
				Autosave.Minutes = m;
				input.text = Autosave.Minutes.ToString();
				w.Status.text = m == 0 ? "Autosave turned off." : "Autosave every " + Autosave.Minutes + " min.";
			};
			input = UiKit.Input(row, Autosave.Minutes.ToString(), s =>
			{
				int m;
				if (int.TryParse(s, out m) && m >= 0 && m <= Autosave.MaxMinutes) set(m);
				else
				{
					input.text = Autosave.Minutes.ToString();
					w.Status.text = "Enter 0 to " + Autosave.MaxMinutes + " minutes.";
				}
			}, 50);
			UiKit.Button(row, "Default (" + Autosave.DefaultMinutes + ")", () => set(Autosave.DefaultMinutes), 90, 20);
			UiKit.Button(row, "Off", () => set(0), 50, 20);
			var state = UiKit.Label(w.Content, Autosave.Describe(), 11, FontStyle.Normal, UiKit.DimTextColor);
			w.Root.AddComponent<AutosaveStatus>().Label = state;
		}
	}

	/// <summary>Keeps the window's line about the autosave up to date.</summary>
	public sealed class AutosaveStatus : MonoBehaviour
	{
		public Text Label;
		float next;

		void Update()
		{
			if (!Label || Time.realtimeSinceStartup < next) return;
			next = Time.realtimeSinceStartup + 0.5f;
			Label.text = Autosave.Describe();
		}
	}
}
