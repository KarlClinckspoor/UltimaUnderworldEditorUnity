using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FileExplorer : ResizableWindow {

	public GameObject Content;
	public InputField CurrentDirInfo; 
	public InputField ChosenFile;
	public Dropdown DriveChoice;
	
	//public GameObject MainPanel;

	public GameObject FileInfoPrefab;			//Needs Text
	public GameObject DirectoryInfoPrefab;      //Needs Text and Button
	public GameObject PopupMessagePrefab;

	private string currentPath;

	public void InitFileReader(string path = null)
	{
		uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
		if (path == null || !Directory.Exists(path))
			currentPath = Application.dataPath;
		else
			currentPath = path;
		SetContent(currentPath, currentPath);
		SetDrives();
		// a folder typed or pasted into the path box opens; a file there is chosen
		CurrentDirInfo.onEndEdit.AddListener(GoToTypedPath);
	}

	void GoToTypedPath(string typed)
	{
		if (string.IsNullOrEmpty(typed)) return;
		typed = typed.Trim().Trim('"').Replace('\\', '/');
		if (typed.Length > 3) typed = typed.TrimEnd('/');
		// onEndEdit also fires when the box just loses focus: leave the list alone then, or
		// rebuilding it would swallow the click on an entry
		if (typed == currentPath) return;
		if (Directory.Exists(typed))
		{
			string oldpath = currentPath;
			currentPath = typed;
			SetContent(currentPath, oldpath);
		}
		else if (File.Exists(typed))
		{
			Destroy(gameObject);
			UltimateEditor.Uw1.UI.GameOpener.Picked(uiManager, typed);
		}
		else
			ShowCurrentDir();
	}

	/// <summary>The files the editor can open: the games' programs and LEV.ARK.</summary>
	static bool Openable(string name)
	{
		name = name.ToUpperInvariant();
		return name.EndsWith(".EXE") || name == "LEV.ARK";
	}

	/// <summary>A label for a folder that holds a game (DATA with LEV.ARK) or is a game's DATA folder.</summary>
	static string GameLabel(DirectoryInfo dir)
	{
		try
		{
			string data = UltimateEditor.Games.GameFiles.Resolve(dir.FullName + "/DATA");
			string folder = File.Exists(UltimateEditor.Games.GameFiles.Resolve(data + "/LEV.ARK")) ? data
				: File.Exists(UltimateEditor.Games.GameFiles.Resolve(dir.FullName + "/LEV.ARK")) ? dir.FullName : null;
			if (folder == null) return null;
			return UltimateEditor.Games.GameProfile.For(UltimateEditor.Games.GameProfile.Detect(folder)).Name;
		}
		catch (System.Exception) { return null; }
	}
	private void SetContent(string path, string oldpath)
	{
		try
		{
			Clear();
			if (!IsRootDirectory(path))
				CreateGoUpDirButton();
			ShowCurrentDir();
			SetDirectories(path);
			SetFiles(path);

		}
		catch(System.UnauthorizedAccessException)
		{
			uiManager.SpawnPopupMessage("Illegal directory");
			currentPath = oldpath;
		}
	}
	private FileInfo[] GetFileList(string path)
	{
		return new DirectoryInfo(path).GetFiles();
	}
	private DirectoryInfo[] GetDirectoryList(string path)
	{
		return new DirectoryInfo(path).GetDirectories();
	}
	private void SetDrives()
	{
		string[] drives = Directory.GetLogicalDrives();
		List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
		foreach (var drive in drives)
		{
			Dropdown.OptionData option = new Dropdown.OptionData(drive);
			options.Add(option);
		}
		DriveChoice.AddOptions(options);
	}
	private void Clear()
	{
		for (int i = 0; i < Content.transform.childCount; i++)
		{
			Destroy(Content.transform.GetChild(i).gameObject);
		}
		ChosenFile.text = "";
	}
	private void CreateDirectoryInfo(string name, string label = null)
	{
		GameObject dirInfo = Instantiate(DirectoryInfoPrefab, Content.transform);
		Text dirText = dirInfo.GetComponentInChildren<Text>();
		dirText.text = label == null ? name : name + "   [" + label + "]";
		Button button = dirInfo.GetComponentInChildren<Button>();
		button.onClick.AddListener(() => GoToDirectory(name));
	}
	private void CreateFileInfo(string text)
	{
		GameObject fileInfo = Instantiate(FileInfoPrefab, Content.transform);
		Text fileText = fileInfo.GetComponentInChildren<Text>();
		fileText.text = text;
		Button button = fileInfo.GetComponentInChildren<Button>();
		button.onClick.AddListener(() => ChooseFile(text));
		
	}
	private void CreateGoUpDirButton()
	{
		GameObject upDir = Instantiate(DirectoryInfoPrefab, Content.transform);
		Text upDirText = upDir.GetComponentInChildren<Text>();
		upDirText.text = "..";
		Button button = upDir.GetComponentInChildren<Button>();
		button.onClick.AddListener(GoUpDirectory);
	}
	private void SetDirectories(string path)
	{
		DirectoryInfo[] dirs = GetDirectoryList(path);
		foreach (var dir in dirs)
		{
			if ((dir.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
				continue;
			CreateDirectoryInfo(dir.Name, GameLabel(dir));
		}
	}
	private void SetFiles(string path)
	{
		FileInfo[] files = GetFileList(path);
		foreach (var file in files)
		{
			if (Openable(file.Name))
				CreateFileInfo(file.Name);
		}
	}
	private bool IsRootDirectory(string path)
	{
		if(path.Length <= 3)
			return true;
		return false;
	}
	public static string GetUpperPath(string path)
	{
		if (string.IsNullOrEmpty(path))
			return null;
		// Windows paths may use either separator (the native file dialog returns backslashes)
		int index = System.Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
		if (index == -1)
			return null;

		string end = path.Substring(0, index);
		if (index == 2)
			end += "/";
		return end;
	}
	public void GoUpDirectory()
	{
		string oldpath = currentPath;
		string upperDir = GetUpperPath(currentPath);
		if (string.IsNullOrEmpty(upperDir))
			return;

		currentPath = upperDir;
		SetContent(currentPath, oldpath);
	}
	public void GoToDirectory(Button button)
	{
		GoToDirectory(button.GetComponentInChildren<Text>().text);
	}
	private void GoToDirectory(string dirName)
	{
		string oldpath = currentPath;
		if (!(currentPath[currentPath.Length - 1] == '/'))
			currentPath += "/";
		currentPath += dirName;
		SetContent(currentPath, oldpath);
	}
	public void GoToDrive(Dropdown dropdown)
	{
		GoToDrive(dropdown.options[dropdown.value].text);
	}
	private void GoToDrive(string drive)
	{
		string oldpath = currentPath;
		drive = drive.Substring(0, drive.Length - 1);
		drive += "/";
		currentPath = drive;
		SetContent(currentPath, oldpath);
	}
	public void ChooseFile(string file)
	{
		ChosenFile.text = file;
	}
	private void ShowCurrentDir()
	{
		CurrentDirInfo.text = currentPath;
	}
	//private void SpawnPopupMessage(string message)
	//{
	//	popup = Instantiate(PopupMessagePrefab, transform.parent);
	//	popup.GetComponentInChildren<Text>().text = message;
	//	popup.GetComponent<Button>().onClick.AddListener(() => Destroy(popup));
	//}
	public void LoadData()
	{
		string file = ChosenFile.text;
		if (string.IsNullOrEmpty(file))
		{
			uiManager.SpawnPopupMessage("Choose the game's UW.EXE or UW2.EXE (or a LEV.ARK).");
			return;
		}
		string path = currentPath.TrimEnd('/') + "/" + file;
		Destroy(gameObject);
		// a program: the game's DATA and saves to pick from; a LEV.ARK opens directly (GameOpener)
		UltimateEditor.Uw1.UI.GameOpener.Picked(uiManager, path);
	}
}
