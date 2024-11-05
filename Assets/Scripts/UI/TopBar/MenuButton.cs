using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuButton : MonoBehaviour {

	private static UIManager uiManager;
	public event System.Action OnClick;

	private void Start()
	{
		uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIManager>();
		OnClick += () =>
		{
			transform.parent.gameObject.SetActive(false);
		};
	}

	public void CreateObjectListMenu()
	{
		uiManager.CreateObjectListMenu();
		OnClick?.Invoke();
	}

	public void CreateFileExplorer()
	{
		uiManager.CreateFileExplorer();
		OnClick?.Invoke();
	}

	public void ReadData()
	{
		uiManager.LoadData();
		OnClick?.Invoke();
	}

	public void SaveData()
	{
		//Validate Doors
		//DataWriter.SaveData(MapCreator.LevelData, MapCreator.TextureData, MapCreator.StringData, uiManager);
		uiManager.SaveData();
		OnClick?.Invoke();
	}

	public void SaveToJSON()
	{
		uiManager.SaveToJSON();
		OnClick?.Invoke();
	}

	public void LoadFromJSON()
	{
		uiManager.LoadFromJSON();
		OnClick?.Invoke();
	}

	public void AddStaticObject()
	{
		uiManager.AddObject();
		OnClick?.Invoke();
	}

	public void CreateFloorTextureExplorer()
	{
		uiManager.CreateTextureExplorer(TextureExplorerType.Level, TextureType.Floor, "Level floors", "Select texture to swap", "Available floors", "Select new texture");
		OnClick?.Invoke();
	}

	public void CreateWallTextureExplorer()
	{
		uiManager.CreateTextureExplorer(TextureExplorerType.Level, TextureType.Wall, "Level walls", "Select texture to swap", "Available walls", "Select new texture");
		OnClick?.Invoke();
	}

	public void ShowLevelDoors()
	{
		uiManager.CreateTextureExplorer(TextureExplorerType.Level, TextureType.Door, "Level doors", "Select texture to swap", "Available doors", "Select new texture");
		OnClick?.Invoke();
	}

	public void ShowPallette()
	{
		uiManager.ShowPalette();
		OnClick?.Invoke();
	}

	public void ShowLoadedFloorTextures()
	{
		uiManager.CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.Floor, "Available floors", "Select texture to swap or add a new texture", "Loaded floors", "Select new texture");
		OnClick?.Invoke();
	}

	public void ShowLoadedWallTextures()
	{
		uiManager.CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.Wall, "Available walls", "Select texture to swap or add a new texture", "Loaded walls", "Select new texture");
		OnClick?.Invoke();
	}

	public void ShowLoadedDoorTextures()
	{
		uiManager.CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.Door, "Available doors", "Select texture to swap or add a new texture", "Loaded doors", "Select new texture");
		OnClick?.Invoke();
	}

	public void ShowLeverExplorer()
	{
		uiManager.CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.Lever, "Available levers", "Select texture to swap or add a new texture", "Loaded levers", "Select new texture");
		OnClick?.Invoke();
	}

	public void ShowOtherTexturesExplorer()
	{
		uiManager.CreateTextureExplorer(TextureExplorerType.Loaded, TextureType.Other, "Available other textures", "Select texture to swap or add a new texture", "Loaded other textures", "Select new texture");
		OnClick?.Invoke();
	}

	public void ExportTexturesToPNG()
	{
		if(!uiManager.MapCreator.IsInitialized)
		{
			uiManager.SpawnPopupMessage("You can export only after loading game data.");
			return;
		}
		//try
		//{
			DataWriter.ExportDefaultTextures();
		//}
		//catch(System.Exception e)
		//{
		//	uiManager.SpawnPopupMessage("Failed to export textures\n" + e.Message);
		//	return;
		//}
		uiManager.SpawnPopupMessage("Exported textures to folder Export.");
		OnClick?.Invoke();
	}

	public void CreateStringBlockList()
	{
		uiManager.CreateStringBlockList();
		OnClick?.Invoke();
	}

	public void ExitApplication()
	{
		Application.Quit();
	}
}
