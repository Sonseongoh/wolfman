using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 배치모드 WebGL 빌드 진입점. 배포 절차는 Docs/Build.md 참고.
/// 압축·해시 설정은 여기서 건드리지 않는다 — ProjectSettings 에 있는 값이 곧 ADR 0003 의 결정이다.
/// </summary>
public static class WebBuilder
{
    public static void Build()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        Debug.Log($"[WebBuilder] scenes ({scenes.Length}): {string.Join(", ", scenes)}");
        Debug.Log($"[WebBuilder] compression={PlayerSettings.WebGL.compressionFormat} " +
                  $"fallback={PlayerSettings.WebGL.decompressionFallback} " +
                  $"hashes={PlayerSettings.WebGL.nameFilesAsHashes}");

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Build/Web",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });

        BuildSummary s = report.summary;
        Debug.Log($"[WebBuilder] result={s.result} errors={s.totalErrors} warnings={s.totalWarnings} " +
                  $"size={s.totalSize} out={s.outputPath}");

        EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }
}
