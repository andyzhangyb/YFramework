using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class AppBuilder
{
    private static string BuildOutputDir = Application.dataPath + "/../BuildOutput/";

    [MenuItem("Build/Export Android Project")]
    public static void ExportAndroidProject()
    {

    }

    [MenuItem("Build/Build Android")]
    public static void BuildAndroid()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("Switch platform failed.");
        }
        else
        {
            var fileName = BuildOutputDir + "Android/" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".apk";
            Build(fileName);
        }
    }

    [MenuItem("Build/Build iOS")]
    public static void BuildiOS()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
        {
            Debug.LogError("Switch platform failed.");
        }
        else
        {
            var fileName = BuildOutputDir + "iOS/" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            Build(fileName);
        }
    }

    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64 && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
        {
            Debug.LogError("Switch platform failed.");
        }
        else
        {
            var fileName = BuildOutputDir + "Windows/" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".exe";
            Build(fileName);
        }
    }

    private static void Build(string fileName)
    {
        BundleEditor.StartBuildAssetBundle();
        BundleEditor.CopyAssetBundleToStreamingAssetsPath();

        BuildPipeline.BuildPlayer(GetEnabledEditorScenes(), fileName, EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
        ClearDir(Application.streamingAssetsPath);
    }

    private static string[] GetEnabledEditorScenes()
    {
        List<string> enabledEditorScenes = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                enabledEditorScenes.Add(scene.path);
            }
        }
        return enabledEditorScenes.ToArray();
    }

    private static void ClearDir(string path)
    {
        try
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            foreach (var info in directoryInfo.GetFileSystemInfos())
            {
                if (info is FileInfo)
                {
                    File.Delete(info.FullName);
                }
                else
                {
                    new DirectoryInfo(info.FullName).Delete(true);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            throw;
        }
    }

}
