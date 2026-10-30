using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

/// <summary>UIManager: save (split from UIManager.cs by region, review package 10).</summary>
public partial class UIManager
{
	#region Save

	public void SaveData()
	{
		if(!MapCreator.IsInitialized)
		{
			SpawnPopupMessage("Data is not loaded.");
			return;
		}
		if (saving)
		{
			SpawnPopupMessage("Already saving.");
			return;
		}
		StartCoroutine(Save());
	}

	public void SaveToJSON()
	{
		if (!MapCreator.IsInitialized)
		{
			SpawnPopupMessage("Data is not loaded.");
			return;
		}
		List<SavedLevel> toSave = MapCreator.LevelData.Select(ld => new SavedLevel(ld)).ToList();
		DataWriter.WriteToJson(toSave, Path.Combine(Application.dataPath, "uwmaps.json"));
	}

	public void LoadFromJSON()
	{
		if (!MapCreator.IsInitialized)
		{
			SpawnPopupMessage("Map is not created.");
			return;
		}
		List<SavedLevel> sls = DataReader.ReadFromJson(Path.Combine(Application.dataPath, "uwmaps.json")) as List<SavedLevel>;
		for (int i = 0; i < sls.Count; i++)
			MapCreator.LevelData[i].LoadLevel(sls[i]);
	}

	bool lastSaveOk;

	/// <summary>Saves, then runs after if everything was written (File > Open: "Save, then open").</summary>
	public void SaveThen(Action after)
	{
		if (!MapCreator.IsInitialized)
		{
			after();
			return;
		}
		StartCoroutine(saveThen(after));
	}

	IEnumerator saveThen(Action after)
	{
		while (saving)
			yield return null;
		yield return StartCoroutine(Save());
		if (!lastSaveOk)
			yield break;
		if (GrPictures.Problems.Count == 0)
		{
			after();
			yield break;
		}
		// opening reloads the scene at once, so ask here rather than leave a popup nobody sees
		if (popup) Destroy(popup);
		var w = UltimateEditor.Uw1.UI.UiKit.Window("Saved, with problems", 480, 240);
		UltimateEditor.Uw1.UI.UiKit.Paragraph(w.Content, "Everything was saved except these pictures:\n" + string.Join("\n", GrPictures.Problems.ToArray()), 11, Color.white);
		var row = UltimateEditor.Uw1.UI.UiKit.Row(w.Content, 24);
		UltimateEditor.Uw1.UI.UiKit.Button(row, "Open anyway", () => { w.Close(); after(); }, 120, 22);
		UltimateEditor.Uw1.UI.UiKit.Button(row, "Stay here", w.Close, 100, 22);
	}

	bool saving;
	/// <summary>True while Save runs (it takes several frames).</summary>
	public bool IsSaving { get { return saving; } }
	/// <summary>The frame a save started on, so a click during a save is not taken as saved
	/// (Extensions/UI/Autosave.cs).</summary>
	public int SaveStartFrame { get; private set; }
	/// <summary>Raised when a save wrote everything: File > Save, "Save, then open" or an autosave.</summary>
	public event Action Saved;

	/// <summary>An autosave (Extensions/UI/Autosave.cs): the same Save as File > Save, without its
	/// progress messages; a failure is still shown. done gets whether everything was written.
	/// Returns false, doing nothing, while the data is not loaded or a load or save runs.</summary>
	public bool AutoSave(Action<bool> done)
	{
		if (!MapCreator.IsInitialized || loading || saving)
			return false;
		StartCoroutine(autoSave(done));
		return true;
	}

	/// <summary>The folder the last autosave wrote its copy to (Extensions/UI/Autosave.cs).</summary>
	public string LastAutosaveFolder { get; private set; }

	IEnumerator autoSave(Action<bool> done)
	{
		// The autosave goes to a copy of the game ("Editor autosaves"), never the game's own
		// files: those change only when the user saves (Data/GameWrite.cs).
		string snapshot = null;
		try { snapshot = GameWrite.NewSnapshot(); }
		catch (Exception e)
		{
			SpawnPopupMessage("Autosave failed: could not copy the game\n" + e.Message);
			if (done != null) done(false);
			yield break;
		}
		yield return StartCoroutine(Save(true, snapshot));
		if (lastSaveOk) LastAutosaveFolder = snapshot;
		if (done != null)
			done(lastSaveOk);
	}

	/// <summary>Writes every file. quiet (an autosave) leaves out the progress messages and the
	/// closing popup; failures are shown either way.</summary>
	private IEnumerator Save(bool quiet = false, string snapshot = null)
	{
		if (saving)
			yield break;
		saving = true;
		SaveStartFrame = Time.frameCount;
		lastSaveOk = false;
		try
		{
			if (!quiet) SpawnPopupMessage("Saving level data", null, false, true);
			yield return null;
			try
			{
				GameWrite.RunIn(snapshot, () => DataWriter.SaveLevelData(MapCreator.LevelData));
			}
			catch(Exception e)
			{
				SpawnPopupMessage("Failed to save level data\n" + e.Message);
				yield break;
			}
			if (!quiet) SpawnPopupMessage("Saving texture data", null, false, true);
			yield return null;
			try
			{
				GameWrite.RunIn(snapshot, () => DataWriter.SaveTextureData(MapCreator.TextureData));
			}
			catch(Exception e)
			{
				SpawnPopupMessage("Failed to save texture data\n"+e.Message);
				yield break;
			}
			if (!quiet) SpawnPopupMessage("Saving strings", null, false, true);
			yield return null;
			try
			{
				GameWrite.RunIn(snapshot, () => DataWriter.SaveStringData(MapCreator.StringData));
			}
			catch(Exception e)
			{
				SpawnPopupMessage("Failed to save string data\n" + e.Message);
				yield break;
			}
			if (!quiet) SpawnPopupMessage("Saving conversations", null, false, true);
			yield return null;
			try
			{
				GameWrite.RunIn(snapshot, () => DataWriter.SaveConversationData(MapCreator.ConversationData));
			}
			catch(Exception e)
			{
				SpawnPopupMessage("Failed to save conversation data\n" + e.Message);
				yield break;
			}
			if (!quiet) SpawnPopupMessage("Saving object data", null, false, true);
			yield return null;
			try
			{
				GameWrite.RunIn(snapshot, () => DataWriter.SaveObjectDatas(MapCreator.ObjectData));
			}
			catch(Exception e)
			{
				SpawnPopupMessage("Failed to save object data\n" + e.Message);
				yield break;
			}
			if (!quiet) Destroy(popup);
			lastSaveOk = true;
			// an autosave copy leaves the game's files as they were: they are not saved
			if (snapshot == null && Saved != null) Saved();
			if (!quiet && GrPictures.Problems.Count > 0)
				SpawnPopupMessage("Saved. Some pictures were not written:\n" + string.Join("\n", GrPictures.Problems.ToArray()), null, true);
		}
		finally
		{
			saving = false;
		}
	}

	#endregion
}
