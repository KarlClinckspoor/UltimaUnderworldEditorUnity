using UnityEngine.EventSystems;

/// <summary>
/// Whether the pointer or keyboard belongs to the interface rather than the map. The editor's
/// own panels are uGUI (EventSystem); the Dear ImGui shell reports what ImGui wants each
/// frame (Extensions/ImGui/ImGuiShell.cs).
/// </summary>
public static class UiInput
{
	public static bool ImGuiWantsMouse;
	public static bool ImGuiWantsKeyboard;

	public static bool PointerOverUI
	{
		get
		{
			if (ImGuiWantsMouse) return true;
			var es = EventSystem.current;
			return es != null && es.IsPointerOverGameObject();
		}
	}
}
