using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Turns the Dear ImGui interface on when its package is installed: once the UImGui assembly
/// (package com.psydack.uimgui) is loaded, the scripting define UIMGUI_SHELL (and the UIMGUI_REMOVE_* defines) are added for the
/// Windows build target, which compiles Assets/Scripts/Extensions/ImGui. Without the package
/// the define stays off and the editor keeps its classic interface.
///
/// To remove the package later, first remove UIMGUI_SHELL (and the UIMGUI_REMOVE_* defines) in Project Settings > Player >
/// Scripting Define Symbols; with the define on and the package gone the scripts do not compile.
/// </summary>
[InitializeOnLoad]
static class ImGuiShellDefine
{
	// UIMGUI_SHELL compiles the new interface. The UIMGUI_REMOVE_* defines take ImPlot, imnodes
	// and ImGuizmo out of the package: each of their native libraries carries its own copy of
	// Dear ImGui, and calling them (the package does every frame) crashed the editor natively.
	static readonly string[] Defines = { "UIMGUI_SHELL", "UIMGUI_REMOVE_IMPLOT", "UIMGUI_REMOVE_IMNODES", "UIMGUI_REMOVE_IMGUIZMO" };

	static ImGuiShellDefine()
	{
		bool installed = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "UImGui");
		if (!installed) return;
		var group = BuildTargetGroup.Standalone;
		var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
			.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
		var missing = Defines.Where(d => !defines.Contains(d)).ToList();
		if (missing.Count == 0) return;
		defines.AddRange(missing);
		PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
		Debug.Log("Dear ImGui package found: added the scripting defines " + string.Join(", ", missing) + ".");
	}
}
