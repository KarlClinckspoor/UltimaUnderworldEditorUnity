using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Tools > Release > Build Windows x64: builds the editor into
/// Builds/UltimateEditor_&lt;Edition&gt;_&lt;version&gt;/ next to Assets, using the scenes in the
/// build settings and the version in Player Settings (bundleVersion).
/// </summary>
public static class ReleaseBuild
{
	/// <summary>Which game this branch's release is for. Used only in the output folder name.</summary>
	const string Edition = "UW1";

	public const string ExeName = "UltimateUnderworldEditor.exe";

	[MenuItem("Tools/Release/Build Windows x64")]
	public static void BuildWindows()
	{
		string version = PlayerSettings.bundleVersion;
		string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
		string dir = Path.Combine(root, "UltimateEditor_" + Edition + "_" + version);
		if (Directory.Exists(dir))
			Directory.Delete(dir, true);

		string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
		var options = new BuildPlayerOptions
		{
			scenes = scenes,
			locationPathName = Path.Combine(dir, ExeName),
			target = BuildTarget.StandaloneWindows64,
			targetGroup = BuildTargetGroup.Standalone,
			options = BuildOptions.None,
		};

		BuildReport report = BuildPipeline.BuildPlayer(options);
		BuildSummary summary = report.summary;
		if (summary.result == BuildResult.Succeeded)
		{
			Debug.Log("Release build " + Edition + " " + version + " written to " + dir
				+ " (" + (summary.totalSize / (1024 * 1024)) + " MB, " + summary.totalTime + ")");
			EditorUtility.RevealInFinder(Path.Combine(dir, ExeName));
		}
		else
		{
			Debug.LogError("Release build " + Edition + " " + version + " failed: " + summary.result
				+ ", " + summary.totalErrors + " error(s). See the Console.");
		}
	}
}
