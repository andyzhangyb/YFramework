using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class OfflineDataEditor
{
    [MenuItem("Assets/Generate OfflineData")]
    public static void CreateOfflineDataForAsset()
    {
        var gameObjects = Selection.gameObjects;
        for (int i = 0; i < gameObjects.Length; i++)
        {
            GenerateOfflineData<OfflineData>(gameObjects[i]);
            EditorUtility.DisplayProgressBar("Add OfflineData", gameObjects[i].name, (float)i / (float)gameObjects.Length);
        }
        EditorUtility.ClearProgressBar();
    }

    [MenuItem("Assets/Generate UI OfflineData")]
    public static void CreateUIOfflineDataForAsset()
    {
        var gameObjects = Selection.gameObjects;
        for (int i = 0; i < gameObjects.Length; i++)
        {
            GenerateOfflineData<UIOfflineData>(gameObjects[i]);
            //gameObjects[i].layer = LayerMask.NameToLayer("");
            EditorUtility.DisplayProgressBar("Add UI OfflineData", gameObjects[i].name, (float)i / (float)gameObjects.Length);
        }
        EditorUtility.ClearProgressBar();
    }

    [MenuItem("Assets/Generate Effect OfflineData")]
    public static void CreateEffectOfflineDataForAsset()
    {
        var gameObjects = Selection.gameObjects;
        for (int i = 0; i < gameObjects.Length; i++)
        {
            GenerateOfflineData<EffectOfflineData>(gameObjects[i]);
            EditorUtility.DisplayProgressBar("Add UI OfflineData", gameObjects[i].name, (float)i / (float)gameObjects.Length);
        }
        EditorUtility.ClearProgressBar();
    }

    private static void GenerateOfflineData<T>(GameObject gameObject) where T : OfflineData, new()
    { 
        T offlineData = gameObject.GetComponent<T>();
        if (offlineData == null)
            offlineData = gameObject.AddComponent<T>();
        offlineData.BindData();
        EditorUtility.SetDirty(gameObject);
        Resources.UnloadUnusedAssets();
        AssetDatabase.Refresh();
    }
}
