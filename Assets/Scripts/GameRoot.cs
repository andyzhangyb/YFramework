using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameRoot : MonoBehaviour
{
    private static GameRoot instance;

    public static GameRoot Instance
    {
        get
        {
            return instance;
        }
    }
    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);

        FileUtils.Instance.AddSearchPath(FileUtils.Instance.UpdateDirectory());

        ResourceManager.Instance.SetMonoBehaviour(this);
        AssetBundleManager.Instance.BuildAssetInfo();

        ILRuntimeManager.Instance.Init();
        ILRuntimeManager.Instance.AppDomain.Invoke("HotFixLibrary.HotFixHelper", "OnGameStart", null, null);

        GoToLoginScene();

    }

    public void GoToLoginScene()
    {
        AssetBundleManager.Instance.LoadSceneAssetBundle("Assets/Scenes/LoginScene.unity");
        SceneManager.LoadScene("LoginScene");
    }

}
