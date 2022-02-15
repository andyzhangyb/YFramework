using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Login : BaseMonoBehaviour, IResDownloadHandler
{
    private bool needUpdate = true;

    public Canvas UICanvas;
    public RectTransform LoginButton;
    public UpdateAndLoad UIUpdateAndLoad;

    private ResDownloadManager resDownloadManager;

    private bool startedDownload = false;
    // Start is called before the first frame update
    void Start()
    {
        LoginButton.gameObject.SetActive(false);
        resDownloadManager = new ResDownloadManager(this, this);
#if UNITY_EDITOR
        if (!ResourceManager.Instance.LoadFormAssetBundleForEditor)
        {
            IsLastVersionContinue();
        }
        else
        {
            resDownloadManager.CheckNeedUpdate("http://192.168.33.131/");
        }
        return;
#else
        if (needUpdate && ResourceManager.Instance.LoadFormAssetBundleForEditor)
        {
            resDownloadManager.CheckNeedUpdate("http://192.168.33.131/");
        }
        else
        {
            IsLastVersionContinue();
        }
#endif
    }

    private void Update()
    {
        if (startedDownload)
        {

        }
    }
    public void OnClickLoginButton()
    {
        AssetBundleManager.Instance.LoadSceneAssetBundle("Assets/Scenes/GameScene.unity");
        SceneManager.LoadScene("GameScene");

        AssetBundleManager.Instance.UnloadSceneAssetBundle("Assets/Scenes/LoginScene.unity");
    }

    public void IsLastVersionContinue()
    {
        LoginButton.gameObject.SetActive(true);
        UIUpdateAndLoad.SetTxtInfo("点击开始按钮进入游戏。");
    }

    void IResDownloadHandler.StartCheckVersion()
    {
        UIUpdateAndLoad.SetTxtInfo("开始检查版本号...");
    }

    void IResDownloadHandler.StartDownload()
    {
        startedDownload = true;
    }

    void IResDownloadHandler.DownloadError()
    {
        //ConfirmWindow.PopDialog(UICanvas.GetComponent<RectTransform>(), string.Format("下载失败，是否重试？"), new Action<object>(obj =>
        //{
        //    SceneManager.LoadScene("LoginScene");
        //}), new Action<object>(obj =>
        //{
        //    Application.Quit();
        //}));
        UIBase uIBase = UIManager.Instance.OpenUI("ConfirmWindow", UICanvas.GetComponent<RectTransform>()).UIScript;
        ILRuntimeManager.Instance.AppDomain.Invoke("HotFixLibrary.UIScript.ConfirmWindow", "Show", uIBase, new object[] {
            "下载失败，是否重试？",
            new Action(()=>{
                SceneManager.LoadScene("LoginScene");
            }),
            new Action(()=>{
                Application.Quit();
            })
        });
    }

    void IResDownloadHandler.DontNeedUpdateFinish()
    {
        IsLastVersionContinue();
    }

    void IResDownloadHandler.ConfirmDownload(float downloadSize, int fileCount)
    {

        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork)
        {
            ConfirmWindow.PopDialog(UICanvas.GetComponent<RectTransform>(), string.Format("当前用的手机网络，是否下载？大小：{0}MB", downloadSize / 1024), new Action<object>(obj =>
            {
                resDownloadManager.ConfirmToDownlod();
            }), new Action<object>(obj =>
            {
                Application.Quit();
            }));
        }
        else
        {
            resDownloadManager.ConfirmToDownlod();
        }
    }

    void IResDownloadHandler.UpdateComplete()
    {
        AssetBundleManager.Instance.LoadSceneAssetBundle("Assets/Scenes/LoginScene.unity");
        SceneManager.LoadScene("LoginScene");
    }
}
