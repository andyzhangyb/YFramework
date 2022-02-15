using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class ResEditor : MonoBehaviour
{
    [MenuItem("Tools/Build Android Res")]
    public static void BuildAndroidRes()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("Switch platform failed.");
        }
        else
        {
            StartBuild();
        }
    }

    [MenuItem("Tools/Build iOS Res")]
    public static void BuildiOSRes()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
        {
            Debug.LogError("Switch platform failed.");
        }
        else
        {
            StartBuild();
        }
    }

    [MenuItem("Tools/Build Windows Res")]
    public static void BuildWindowsRes()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64 && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
        {
            Debug.LogError("Switch platform failed.");
        }
        else
        {
            StartBuild();
        }
    }

    public static void StartBuild()
    {
        BundleEditor.StartBuildAssetBundle();
        GernerateVersionFile(BundleEditor.BundlePath);
        //BundleEditor.CopyAssetBundleToStreamingAssetsPath();
    }

    public static void GernerateVersionFile(string bundleFilesPath)
    {
        DirectoryInfo fileDirInfo = new DirectoryInfo(bundleFilesPath);
        string buildPath = Path.GetFullPath(bundleFilesPath + "../");

        DirectoryInfo allResDirInfo = new DirectoryInfo(buildPath + "../");
        DirectoryInfo[] allBundlesDirInfos = allResDirInfo.GetDirectories();

        string currentBuildName = new DirectoryInfo(buildPath).Name;

        long lastDateTime = 0;
        DirectoryInfo lastBuildDir = null;
        foreach (var dir in allBundlesDirInfos)
        {
            if (!long.TryParse(dir.Name, out long longDateTime))
            {
                continue;
            }
            if (longDateTime > lastDateTime && dir.Name != currentBuildName)
            {
                lastDateTime = longDateTime;
                lastBuildDir = dir;
            }
        }
        ResVersion lastResVersion = null;
        if (lastBuildDir != null)
        {
            var path = Path.Combine(lastBuildDir.FullName, "ResVersion.bytes");
            if (File.Exists(path))
            {
                lastResVersion = BinarySerializeHelper.BinaryDeserilizeFromDisk<ResVersion>(path);
            }
        }
        ResVersion currentResVersion = new ResVersion();
        if (lastResVersion != null)
        {
            currentResVersion.ResVersionCode = lastResVersion.ResVersionCode + 1;
        }
        else
        {
            currentResVersion.ResVersionCode = 0;
        }

        FileInfo[] files = fileDirInfo.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (!files[i].Name.EndsWith(".meta") && !files[i].Name.EndsWith(".manifest"))
            {
                ResFileInfo resFileInfo = new ResFileInfo();
                resFileInfo.FilePath = files[i].FullName.Replace(fileDirInfo.FullName, "");
                resFileInfo.Name = files[i].Name;
                resFileInfo.Md5 = Md5Helper.CalcuFileMd5(files[i].FullName);
                resFileInfo.Size = files[i].Length / 1024.0f;
                currentResVersion.FileList.Add(resFileInfo);
            }
        }
        string packageResVersionPath = Path.GetFullPath(bundleFilesPath + "ResVersion.bytes");
        BinarySerializeHelper.SerilizeToBinary(packageResVersionPath, currentResVersion);

        ++currentResVersion.ResVersionCode;
        string serverResVersionPath = Path.GetFullPath(bundleFilesPath + "../ResVersion.bytes");
        BinarySerializeHelper.SerilizeToBinary(serverResVersionPath, currentResVersion);
        serverResVersionPath = Path.GetFullPath(bundleFilesPath + "../ResVersion.xml");
        BinarySerializeHelper.SerializeToXml(serverResVersionPath, currentResVersion);

        if (lastResVersion == null)
            return;
        // Write changed res to disk.
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("Pre build path: {0}", lastBuildDir.FullName);
        for (int i = 0; i < currentResVersion.FileList.Count; i++)
        {
            bool notChanged = false;
            for (int j = 0; j < lastResVersion.FileList.Count; j++)
            {
                if (currentResVersion.FileList[i].Name == lastResVersion.FileList[j].Name)
                {
                    notChanged = currentResVersion.FileList[i].Md5 == lastResVersion.FileList[j].Md5;
                    break;
                }
            }
            if (!notChanged)
            {
                sb.AppendFormat("Name: {0}, Md5: {1}\r\n", currentResVersion.FileList[i].Name, currentResVersion.FileList[i].Md5);
            }
        }
        string changedRes = Path.GetFullPath(bundleFilesPath + "../ResVersion.txt");
        File.WriteAllText(changedRes, sb.ToString());

    }

}
