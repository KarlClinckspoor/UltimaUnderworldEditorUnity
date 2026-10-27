using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Turns the Dear ImGui interface on when its package is installed: once the UImGui assembly
/// (package com.psydack.uimgui) is loaded, the scripting define UIMGUI_SHELL is added for the
/// Windows build target, which compiles Assets/Scripts/Extensions/ImGui. Without the package
/// the define stays off and the editor keeps its classic interface.
///
/// To remove the package later, first remove UIMGUI_SHELL in Project Settings > Player >
/// Scripting Define Symbols; with the define on and the package gone the scripts do not compile.
/// </summary>
[InitializeOnLoad]
static class ImGuiShellDefine
{
	const string Define = "UIMGUI_SHELL";

	static ImGuiShellDefine()
	{
		bool installed = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "UImGui");
		if (!installed) return;
		var group = BuildTargetGroup.Standalone;
		var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
			.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
		if (defines.Contains(Define)) return;
		defines.Add(Define);
		PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
		Debug.Log("Dear ImGui package found: added the scripting define " + Define + " (the new interface).");
	}
}
