using System;

namespace UltimateEditor.Games
{
	/// <summary>
	/// Where the Formats assembly's messages go. It cannot reference UnityEngine, so the editor
	/// points these at Debug.Log, Debug.LogWarning and Debug.LogError at start-up (DataReader.Init);
	/// other programs (tests, the mod manager) can point them anywhere. Unset, messages are dropped.
	/// </summary>
	public static class FormatsLog
	{
		public static Action<string> InfoSink, WarningSink, ErrorSink;

		public static void Info(string m) { var s = InfoSink; if (s != null) s(m); }
		public static void Warning(string m) { var s = WarningSink; if (s != null) s(m); }
		public static void Error(string m) { var s = ErrorSink; if (s != null) s(m); }
	}
}
