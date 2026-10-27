using System;
using System.Runtime.InteropServices;

namespace UltimateEditor.Uw1.UI
{
	/// <summary>
	/// The Windows "Open" dialog (comdlg32 GetOpenFileNameW), so the editor can use the
	/// system's file browser with its favourites, recent folders and typed paths. Elsewhere
	/// (or if the call fails) GameOpener falls back to the editor's own file explorer.
	/// </summary>
	public static class NativeFileDialog
	{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		public static bool Available { get { return true; } }

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		class OpenFileName
		{
			public int lStructSize = Marshal.SizeOf(typeof(OpenFileName));
			public IntPtr hwndOwner = IntPtr.Zero;
			public IntPtr hInstance = IntPtr.Zero;
			public string lpstrFilter = null;
			public string lpstrCustomFilter = null;
			public int nMaxCustFilter = 0;
			public int nFilterIndex = 0;
			public IntPtr lpstrFile = IntPtr.Zero;
			public int nMaxFile = 0;
			public string lpstrFileTitle = null;
			public int nMaxFileTitle = 0;
			public string lpstrInitialDir = null;
			public string lpstrTitle = null;
			public int Flags = 0;
			public short nFileOffset = 0;
			public short nFileExtension = 0;
			public string lpstrDefExt = null;
			public IntPtr lCustData = IntPtr.Zero;
			public IntPtr lpfnHook = IntPtr.Zero;
			public string lpTemplateName = null;
			public IntPtr pvReserved = IntPtr.Zero;
			public int dwReserved = 0;
			public int FlagsEx = 0;
		}

		[DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern bool GetOpenFileNameW([In, Out] OpenFileName ofn);

		[DllImport("user32.dll")]
		static extern IntPtr GetActiveWindow();

		const int OFN_HIDEREADONLY = 0x4, OFN_NOCHANGEDIR = 0x8, OFN_PATHMUSTEXIST = 0x800,
			OFN_FILEMUSTEXIST = 0x1000, OFN_EXPLORER = 0x80000;

		/// <summary>
		/// Shows the dialog. filter is "Description|pattern;pattern|Description|pattern...".
		/// Returns the chosen file, or "" when the dialog was cancelled.
		/// </summary>
		public static string OpenFile(string title, string filter, string initialFolder)
		{
			const int chars = 4096;
			var ofn = new OpenFileName();
			ofn.hwndOwner = GetActiveWindow();
			// the API wants "description\0pattern\0...\0\0"
			ofn.lpstrFilter = filter.Replace('|', '\0') + "\0";
			ofn.nFilterIndex = 1;
			ofn.lpstrFile = Marshal.AllocHGlobal(chars * 2);
			ofn.nMaxFile = chars;
			ofn.lpstrInitialDir = string.IsNullOrEmpty(initialFolder) ? null : initialFolder.Replace('/', '\\');
			ofn.lpstrTitle = title;
			// NOCHANGEDIR: the dialog must not move the process's current folder
			ofn.Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR | OFN_HIDEREADONLY;
			try
			{
				Marshal.WriteInt16(ofn.lpstrFile, 0);
				return GetOpenFileNameW(ofn) ? Marshal.PtrToStringUni(ofn.lpstrFile) : "";
			}
			finally
			{
				Marshal.FreeHGlobal(ofn.lpstrFile);
			}
		}
#else
		public static bool Available { get { return false; } }

		public static string OpenFile(string title, string filter, string initialFolder)
		{
			throw new PlatformNotSupportedException("The native file dialog is only available on Windows.");
		}
#endif
	}
}
