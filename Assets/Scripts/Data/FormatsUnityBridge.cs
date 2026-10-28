using UnityEngine;
using UltimateEditor.Games;

/// <summary>
/// The Formats assembly (Assets/Scripts/Formats) builds without UnityEngine, so its messages go
/// through FormatsLog. This sends them to the Unity console, as the code did before it moved,
/// before the first scene loads.
/// </summary>
public static class FormatsUnityBridge
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void Install()
	{
		FormatsLog.InfoSink = m => Debug.Log(m);
		FormatsLog.WarningSink = m => Debug.LogWarning(m);
		FormatsLog.ErrorSink = m => Debug.LogError(m);
	}
}
