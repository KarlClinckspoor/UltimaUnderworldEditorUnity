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
		if (path == null)
			currentPath = Application.dataPath;
		else
			currentPath = path;
		SetContent(currentPath, currentPath);
		SetDrives();
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
	private void CreateDirectoryInfo(string text)
	{
		GameObject dirInfo = Instantiate(DirectoryInfoPrefab, Content.transform);
		Text dirText = dirInfo.GetComponentInChildren<Text>();
		dirText.text = text;
		Button button = dirInfo.GetComponentInChildren<Button>();
		button.onClick.AddListener(() => GoToDirectory(button));
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
			CreateDirectoryInfo(dir.Name);
		}
	}
	private void SetFiles(string path)
	{
		FileInfo[] files = GetFileList(path);
		foreach (var file in files)
		{
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
		int index = path.LastIndexOf('/');
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
		LoadData(ChosenFile.text);
		Destroy(gameObject);
	}
	private void LoadData(string file)
	{
		if(file != "LEV.ARK")
		{
			uiManager.SpawnPopupMessage("Invalid file. Choose LEV.ARK from Ultima Underworld 'Data' directory.");
			return;
		}
		string path = currentPath + "/" + file;
		DataReader.FilePath = path;
		DataWriter.SavePathToFile(path);		
		//UIManager.SetMenu(MainPanel);
	}
}
