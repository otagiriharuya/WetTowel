using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Windows (StandaloneWindows64) ビルド用エディタツール。
/// 呼び出し: Unity -batchmode -executeMethod BuildWindows.Build
/// 出力先は環境変数 WT_BUILD_DIR、未指定時は ~/workspace/goals/unity-steam/WetTowel-build
/// </summary>
public static class BuildWindows
{
    [MenuItem("WetTowel/Build Windows")]
    public static void Build()
    {
        string outDir = System.Environment.GetEnvironmentVariable("WT_BUILD_DIR");
        if (string.IsNullOrEmpty(outDir))
            outDir = "/home/hatch/workspace/goals/unity-steam/WetTowel-build";
        Directory.CreateDirectory(outDir);

        // Steam向けはWindows版のみ。モバイル版シーンは含めない。
        // 画面は16:9（PlayerSettingsの既定解像度1920x1080）。
        string[] scenes = new[]
        {
            "Assets/Scenes/BootScene.unity",
            "Assets/Scenes/Windows/Loading_Windows.unity",
            "Assets/Scenes/Windows/Title_Windows.unity",
            "Assets/Scenes/Windows/Select_Windows.unity",
            "Assets/Scenes/Windows/Game_Windows.unity",
        };
        Debug.Log($"[Build] 対象シーン {scenes.Length} 件: {string.Join(", ", scenes)}");

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outDir, "WetTowel.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[Build] result={report.summary.result} errors={report.summary.totalErrors} " +
                  $"warnings={report.summary.totalWarnings} size={report.summary.totalSize} bytes " +
                  $"time={report.summary.totalTime}");

        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("[Build] Windowsビルドに失敗しました。");
        Debug.Log("[Build] Windowsビルドに成功しました。");
    }
}
