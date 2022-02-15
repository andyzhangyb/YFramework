using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AssetBundleConfig", menuName = "AssetBundleConfig", order = 0)]
public class AssetBundleConfig: ScriptableObject
{
    // find all prefabs in this directory
    public List<string> AllPrefabPath = new List<string>();
    // one director gernate one bundle.
    public List<FileDirName> AllFileDirAssetBundle = new List<FileDirName>();

    [System.Serializable]
    public struct FileDirName
    {
        public string AssetBundleName;
        public string Path;
    }
}
