#if UIMGUI_SHELL
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace UltimateEditor.Uw1.ImGuiUI
{
	/// <summary>
	/// Creates the UImGui component (package com.psydack.uimgui v4.1.0) from code, so the scene
	/// needs no change. The component expects its camera, shaders, style and cursors to be set in
	/// the Inspector before it is enabled; those fields and the asset types are internal to the
	/// package, so they are set here by reflection, on a disabled object, before enabling it.
	/// The font is Windows' Segoe UI when it exists (through the package's font atlas
	/// configuration, also internal), else Dear ImGui's built-in font.
	/// </summary>
	static class ImGuiHost
	{
		const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

		public static UImGui.UImGui Create(Camera camera, float fontSize, Action<UImGui.UImGui> layout)
		{
			var go = new GameObject("Dear ImGui");
			go.SetActive(false);
			var host = go.AddComponent<UImGui.UImGui>();
			Set(host, "_camera", camera);
			Set(host, "_shaders", Resources.Load<ScriptableObject>("DefaultShader"));
			Set(host, "_style", Resources.Load<ScriptableObject>("DefaultStyle"));
			Set(host, "_cursorShapes", Resources.Load<ScriptableObject>("DefaultCursorShape"));
			EnsureFontCallback(host);
			SetFont(host, fontSize);
			host.Layout += layout;
			go.SetActive(true);
			return host;
		}

		/// <summary>Points the font configuration at Segoe UI of the given size (applied on the next enable or Reload).</summary>
		public static void SetFont(UImGui.UImGui host, float size)
		{
			ScriptableObject config = null;
			try { config = FontAtlasConfig(size); }
			catch (Exception e) { Debug.LogWarning("Dear ImGui: using the built-in font (" + e.Message + ")"); }
			Set(host, "_fontAtlasConfiguration", config);
		}

		/// <summary>The package reads the font callback when no font configuration is set; make
		/// sure that field is not null even if the serializer left it so.</summary>
		static void EnsureFontCallback(UImGui.UImGui host)
		{
			var f = typeof(UImGui.UImGui).GetField("_fontCustomInitializer", Fields);
			if (f == null || f.GetValue(host) != null) return;
			try { f.SetValue(host, Activator.CreateInstance(f.FieldType)); }
			catch (Exception e) { Debug.LogWarning("Dear ImGui: " + e.Message); }
		}

		/// <summary>A FontAtlasConfigAsset with one font: Segoe UI at size pixels, Dear ImGui's
		/// default glyph settings (FontConfig.SetDefaults) and the Latin glyph range. Null when
		/// the font file is not there.</summary>
		static ScriptableObject FontAtlasConfig(float size)
		{
			string fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
			string path = string.IsNullOrEmpty(fonts) ? null : Path.Combine(fonts, "segoeui.ttf");
			if (path == null || !File.Exists(path)) return null;

			var asm = typeof(UImGui.UImGui).Assembly;
			Type assetType = Need(asm, "UImGui.Assets.FontAtlasConfigAsset");
			Type defType = Need(asm, "UImGui.FontDefinition");
			Type cfgType = Need(asm, "UImGui.FontConfig");
			Type rangesType = Need(asm, "UImGui.ScriptGlyphRanges");

			object cfg = Activator.CreateInstance(cfgType);
			cfgType.GetMethod("SetDefaults").Invoke(cfg, null);     // mutates the boxed struct
			cfgType.GetField("SizeInPixels").SetValue(cfg, size);
			cfgType.GetField("GlyphRanges").SetValue(cfg, Enum.Parse(rangesType, "Default"));

			object def = Activator.CreateInstance(defType);
			// the package combines this with StreamingAssets; a rooted path is kept as it is
			defType.GetField("Path").SetValue(def, path);
			defType.GetField("Config").SetValue(def, cfg);

			Array fontsArray = Array.CreateInstance(defType, 1);
			fontsArray.SetValue(def, 0);
			var asset = ScriptableObject.CreateInstance(assetType);
			assetType.GetField("Fonts").SetValue(asset, fontsArray);
			return asset;
		}

		static Type Need(Assembly asm, string name)
		{
			var t = asm.GetType(name);
			if (t == null) throw new MissingMemberException(name + " not found in the UImGui package");
			return t;
		}

		static void Set(object target, string field, object value)
		{
			var f = target.GetType().GetField(field, Fields);
			if (f == null) throw new MissingFieldException("UImGui." + field + " (the editor expects com.psydack.uimgui v4.1.0)");
			f.SetValue(target, value);
		}
	}
}
#endif
