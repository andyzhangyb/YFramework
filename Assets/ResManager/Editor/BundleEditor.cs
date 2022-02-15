using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System;

public class BundleEditor
{
    private static string bundleRootPath = Application.dataPath + "/../AssetBundle/";
    private static string bundleInfoPath = "Assets/GameData/AssetBundleInfo/" + "assetbundleinfo.bytes";
    public static string BundlePath = "";

    public static string ABConfigPath = "Assets/ResManager/Editor/AssetBundleConfig.asset";
    // dictionary to A AssetBundle.
    public static Dictionary<string, string> AllFileDir = new Dictionary<string, string>();
    // prefab to A AssetBundle. this contain all dependencies of this prefab.
    public static Dictionary<string, List<string>> AllPrefabDir = new Dictionary<string, List<string>>();
    // cache, prevent have added to list for set bundle name and which asset be added again.
    public static List<string> HaveAddedToAssetBundle = new List<string>();
    // cache, only add asset which config in ABConfigPath to our config file.
    public static List<string> validPath = new List<string>();

    [MenuItem("Tools/Build Current Platform Asset Bundle And Copy")]
    public static void MenuBuildAssetBundle()
    {
        ResEditor.StartBuild();
        CopyAssetBundleToStreamingAssetsPath();
    }

    public static void StartBuildAssetBundle()
    {
        if (!Directory.Exists(bundleRootPath))
        {
            Directory.CreateDirectory(bundleRootPath);
        }
        BundlePath = bundleRootPath + EditorUserBuildSettings.activeBuildTarget.ToString() + "/" + DateTime.Now.ToString("yyyyMMddHHmmss")+ "/BundleFiles/";

        AllFileDir.Clear();
        AllPrefabDir.Clear();
        HaveAddedToAssetBundle.Clear();
        validPath.Clear();

        var abConfig = AssetDatabase.LoadAssetAtPath<AssetBundleConfig>(ABConfigPath);
        // Add directory.
        foreach (var item in abConfig.AllFileDirAssetBundle)
        {
            if (AllFileDir.ContainsKey(item.AssetBundleName))
            {
                Debug.LogError("AB name is duplicate.");
                return;
            }
            else
            {
                AllFileDir.Add(item.AssetBundleName, item.Path);
                HaveAddedToAssetBundle.Add(item.Path);
                validPath.Add(item.Path);
            }
        }

        var listPrefabsGUIDs = AssetDatabase.FindAssets("t:Prefab", abConfig.AllPrefabPath.ToArray());
        for (int i = 0; i < listPrefabsGUIDs.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(listPrefabsGUIDs[i]);
            validPath.Add(path);
            EditorUtility.DisplayProgressBar("Search prefabs", path, (float)i / (float)listPrefabsGUIDs.Length);

            // Add Prefab denpencies.
            var allDependencies = AssetDatabase.GetDependencies(path);
            List<string> currentPrefabDependencies = new List<string>();
            for (int j = 0; j < allDependencies.Length; j++)
            {
                if (HaveAddedToBundle(allDependencies[j]) || allDependencies[j].EndsWith(".cs"))
                    continue;
                currentPrefabDependencies.Add(allDependencies[j]);
                HaveAddedToAssetBundle.Add(allDependencies[j]);
            }
            var prefabName = Path.GetFileNameWithoutExtension(path);
            if (AllPrefabDir.ContainsKey(prefabName))
            {
                Debug.LogError(string.Format("Containe prefab: {0}", prefabName));
                return;
            }
            else
            {
                AllPrefabDir.Add(prefabName, currentPrefabDependencies);
            }
        }

        foreach (var item in AllFileDir)
        {
            SetAssetBundleName(item.Key, item.Value);
        }
        foreach (var item in AllPrefabDir)
        {
            SetAssetBundleName(item.Key, item.Value);
        }
        EditorUtility.ClearProgressBar();
        //AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();

        BuildAssetBundle();

        var existAssetBundleNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < existAssetBundleNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(existAssetBundleNames[i], true);
            EditorUtility.DisplayProgressBar(string.Format("Remove AssetBundle Name"), existAssetBundleNames[i], (float)i / (float)listPrefabsGUIDs.Length);
        }
        EditorUtility.ClearProgressBar();

        AssetDatabase.Refresh();

    }

    public static void CopyAssetBundleToStreamingAssetsPath()
    {
        Copy(BundlePath, Application.streamingAssetsPath);
    }

    static void BuildAssetBundle()
    {
        string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        Dictionary<string, string> pathToBundleName = new Dictionary<string, string>();
        for (int i = 0; i < allBundleNames.Length; i++)
        {
            var assetPath = AssetDatabase.GetAssetPathsFromAssetBundle(allBundleNames[i]);
            for (int j = 0; j < assetPath.Length; j++)
            {
                if (allBundleNames[i].StartsWith(".cs"))
                    continue;
                pathToBundleName.Add(assetPath[j], allBundleNames[i]);
            }
        }

        // If incremental build, need to delete unuse bundle.
        if (Directory.Exists(BundlePath))
        {
            Directory.Delete(BundlePath, true);
        }
        Directory.CreateDirectory(BundlePath);

        WriteAssetInfoToFile(pathToBundleName);

        BuildPipeline.BuildAssetBundles(BundlePath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
    }

    static void WriteAssetInfoToFile(Dictionary<string, string> assetPathDictionary)
    {
        AssetInfoConfig assetInfoConfig = new AssetInfoConfig();
        foreach (var item in assetPathDictionary)
        {
            var path = item.Key;
            var bundleName = item.Value;

            if (!CheckPathIsValid(path))
                continue;

            AssetBaseInfo assetBaseInfo = new AssetBaseInfo();
            assetBaseInfo.Path = path;
            assetBaseInfo.CRC = CRC32.GetCRC3232(path);
            assetBaseInfo.BundleName = bundleName;
            assetBaseInfo.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);
            var dependencyPaths = AssetDatabase.GetDependencies(path);
            foreach (var dependencyPath in dependencyPaths)
            {
                if (dependencyPath == path || dependencyPath.EndsWith(".cs"))
                    continue;

                string dependencyBundleName = "";
                if (!assetPathDictionary.TryGetValue(dependencyPath, out dependencyBundleName))
                    continue;
                if (dependencyBundleName == bundleName)
                    continue;
                if (assetBaseInfo.Dependencies.Contains(dependencyBundleName))
                    continue;
                assetBaseInfo.Dependencies.Add(dependencyBundleName);
            }

            assetInfoConfig.AssetInfoList.Add(assetBaseInfo);
        }

        // Write our config to xml.
        {
            string xmlPath = Application.dataPath + "/AssetInfoConfig.xml";
            if (File.Exists(xmlPath))
                File.Delete(xmlPath);
            FileStream fileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
            XmlSerializer xmlSerializer = new XmlSerializer(assetInfoConfig.GetType());
            xmlSerializer.Serialize(streamWriter, assetInfoConfig);
            streamWriter.Close();
            fileStream.Close();
        }
        // Write our config to binary.
        {
            foreach (var config in assetInfoConfig.AssetInfoList)
            {
                config.Path = "";
            }
            FileStream fileStream = new FileStream(bundleInfoPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.SetLength(0);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, assetInfoConfig);
            fileStream.Close();

            AssetDatabase.Refresh();
            SetAssetBundleName("assetbundleinfo", bundleInfoPath);
        }

    }

    static void SetAssetBundleName(string name, List<string> paths)
    {
        foreach (var path in paths)
        {
            SetAssetBundleName(name, path);
        }
    }

    static void SetAssetBundleName(string name, string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if (assetImporter == null)
        {
            Debug.LogError(string.Format("Error when set assetbundle name at path: {0}", path));
            return;
        }
        assetImporter.assetBundleName = name;
    }

    static bool HaveAddedToBundle(string path)
    {
        for (int i = 0; i < HaveAddedToAssetBundle.Count; i++)
        {
            if (path == HaveAddedToAssetBundle[i] || (path.Contains(HaveAddedToAssetBundle[i]) && path.Replace(HaveAddedToAssetBundle[i], "")[0] == '/'))
                return true;
        }
        return false;
    }

    static bool CheckPathIsValid(string path)
    {
        foreach (var item in validPath)
        {
            if (path.Contains(item))
            {
                return true;
            }
        }
        return false;
    }

    private static void Copy(string srcPath, string targetPath)
    {
        try
        {
            if (!Directory.Exists(srcPath))
            {
                Directory.CreateDirectory(srcPath);
            }
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
            targetPath += Path.DirectorySeparatorChar;
            var allFiles = Directory.GetFileSystemEntries(srcPath);
            foreach (var item in allFiles)
            {
                if (File.Exists(item))
                {
                    File.Copy(item, targetPath + Path.GetFileName(item), true);
                }
                else
                {
                    Copy(item, targetPath);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }

}
