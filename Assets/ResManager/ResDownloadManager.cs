using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public interface IResDownloadHandler
{
    void StartCheckVersion();
    void StartDownload();
    void DownloadError();
    void DontNeedUpdateFinish();
    void ConfirmDownload(float downloadSize, int fileCount);
    void UpdateComplete();
}

public class ResDownloadManager
{
    private int maxDownloadFailed = 4;
    private int sameTimeDownloadCount = 4;
    private int currentDownloadIndex = 0;
    private List<ResDownloadRequest> webRequests = new List<ResDownloadRequest>();
    private Dictionary<string, int> downloadFilesTryTimes = new Dictionary<string, int>();

    private IResDownloadHandler resDownloadHandler;
    private MonoBehaviour monoBehaviour = null;

    private ResVersion currentResVersion = null;
    private ResVersion newResVersion = null;

    private string newWorkingDir = string.Empty;
    private string newResDownloadPath = string.Empty;

    private List<ResFileInfo> downloadList = new List<ResFileInfo>();

    private float needDownloadSize = 0;
    private float alreadyDownloadedSize = 0;
    public float AlreadyDownloadedSize
    {
        get
        {
            return alreadyDownloadedSize;
        }
    }
    public float DownloadingSize
    {
        get
        {
            float downloadingSize = 0;
            for (int i = 0; i < webRequests.Count; i++)
            {
                if (!webRequests[i].IsDone())
                {
                    downloadingSize += webRequests[i].DownloadProgress * webRequests[i].DownloadResFileInfo.Size;
                }
            }
            return downloadingSize;
        }
    }

    private string updateUrl = string.Empty;

    public ResDownloadManager(MonoBehaviour monoBehaviour, IResDownloadHandler resDownloadHandler)
    {
        this.monoBehaviour = monoBehaviour;
        this.resDownloadHandler = resDownloadHandler;
    }
    public void CheckNeedUpdate(string updateUrl)
    {
        this.updateUrl = updateUrl;
        monoBehaviour.StartCoroutine(ReadCurrentConfig());
    }

    private IEnumerator ReadCurrentConfig()
    {
        resDownloadHandler.StartCheckVersion();
        string fullPath = FileUtils.Instance.FullPathForFilename("ResVersion.bytes");
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(fullPath);
        unityWebRequest.timeout = 30;
        yield return unityWebRequest.SendWebRequest();
        currentResVersion = BinarySerializeHelper.BinaryDeserilize<ResVersion>(unityWebRequest.downloadHandler.data);
        if (currentResVersion == null)
        {
            currentResVersion = new ResVersion();
        }
        monoBehaviour.StartCoroutine(StartCheckUpdate());
    }

    private IEnumerator StartCheckUpdate()
    {
        var serverResInfo = updateUrl + "ResVersion.bytes";
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(serverResInfo);
        unityWebRequest.timeout = 5;
        yield return unityWebRequest.SendWebRequest();
        if (!unityWebRequest.isDone || unityWebRequest.responseCode != 200)
        {
            resDownloadHandler.DownloadError();
        }
        else
        {
            newResVersion = BinarySerializeHelper.BinaryDeserilize<ResVersion>(unityWebRequest.downloadHandler.data);
            if (newResVersion == null)
            {
                resDownloadHandler.DownloadError();
            }
            else
            {
                StartCalcuDownInfo();
            }
        }
    }

    private void StartCalcuDownInfo()
    {
        if (currentResVersion.ResVersionCode == newResVersion.ResVersionCode)
        {
            resDownloadHandler.DontNeedUpdateFinish();
            return;
        }

        string currentUpdateDir = FileUtils.Instance.UpdateDirectory();
        if (PlayerPrefs.GetString("UpdateDir", "WorkingA") == "WorkingA")
        {
            newWorkingDir = "WorkingB";
            newResDownloadPath = Path.Combine(Application.persistentDataPath, "WorkingB") + "/";
        }
        else
        {
            newWorkingDir = "WorkingA";
            newResDownloadPath = Path.Combine(Application.persistentDataPath, "WorkingA") + "/";
        }

        downloadList.Clear();
        for (int i = 0; i < newResVersion.FileList.Count; i++)
        {
            bool changed = true;
            for (int j = 0; j < currentResVersion.FileList.Count; j++)
            {
                if (newResVersion.FileList[i].FilePath == currentResVersion.FileList[j].FilePath && newResVersion.FileList[i].Md5 == currentResVersion.FileList[j].Md5)
                {
                    changed = false;
                    break;
                }
            }
            if (changed)
            {
                downloadList.Add(newResVersion.FileList[i]);
            }
        }
        if (downloadList.Count == 0)
        {
            BinarySerializeHelper.SerilizeToBinary(Path.Combine(currentUpdateDir, "ResVersion.bytes"), newResVersion);
            resDownloadHandler.DontNeedUpdateFinish();
            return;
        }

        needDownloadSize = 0;
        alreadyDownloadedSize = 0;

        string downloadingIsWorkPath = newResDownloadPath + "UpdateIsWorking.tag";
        bool isDownloading = File.Exists(downloadingIsWorkPath);
        if (!isDownloading)
        {
            FileUtils.Instance.Copy(currentUpdateDir, newResDownloadPath);
            File.WriteAllText(downloadingIsWorkPath, "1");
        }
        else
        {
            for (int i = downloadList.Count - 1; i >= 0; i--)
            {
                string tempPath = newResDownloadPath + downloadList[i].FilePath;
                if (File.Exists(tempPath) && Md5Helper.CalcuFileMd5(tempPath) == downloadList[i].Md5)
                {
                    downloadList.RemoveAt(i);
                }
            }
        }
        if (downloadList.Count == 0)
        {
            DownloadFilesCheckFinished();
            return;
        }

        for (int i = 0; i < downloadList.Count; i++)
        {
            needDownloadSize += downloadList[i].Size;
        }
        resDownloadHandler.ConfirmDownload(needDownloadSize, downloadList.Count);
    }

    public void ConfirmToDownlod()
    {
        resDownloadHandler.StartCheckVersion();
        while (webRequests.Count < sameTimeDownloadCount)
        {
            webRequests.Add(new ResDownloadRequest(monoBehaviour, OnOneResDownloadFinished));
        }
        for (int i = 0; i < webRequests.Count; i++)
        {
            StartDownloadNextRes(webRequests[i]);
        }
    }

    private void OnOneResDownloadFinished(long httpCode, byte[] data, ResDownloadRequest resDownloadRequest)
    {
        if (httpCode == 200)
        {
            var saveFilePath = newResDownloadPath + resDownloadRequest.DownloadResFileInfo.FilePath;
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }
            File.WriteAllBytes(saveFilePath, data);
            if (Md5Helper.CalcuFileMd5(saveFilePath) == resDownloadRequest.DownloadResFileInfo.Md5)
            {
                alreadyDownloadedSize += resDownloadRequest.DownloadResFileInfo.Size;
                StartDownloadNextRes(resDownloadRequest);
                return;
            }
            goto TRYAGAINDOWNLOAD;
        }

    TRYAGAINDOWNLOAD:
        if (!downloadFilesTryTimes.ContainsKey(resDownloadRequest.DownloadResFileInfo.FilePath) || downloadFilesTryTimes[resDownloadRequest.DownloadResFileInfo.FilePath] < maxDownloadFailed)
        {
            resDownloadRequest.ReRequest();
        }
        else
        {
            StopDownloadBecauseError();
        }
    }

    private void StartDownloadNextRes(ResDownloadRequest resDownloadRequest)
    {
        if (currentDownloadIndex >= downloadList.Count)
        {
            CheckAllFinished();
            return;
        }
        resDownloadRequest.RequestUrl(updateUrl, downloadList[currentDownloadIndex++]);
    }

    private void CheckAllFinished()
    {
        bool allDone = true;
        for (int i = 0; i < webRequests.Count; i++)
        {
            if (!webRequests[i].IsDone())
            {
                allDone = false;
                break;
            }
        }
        if (allDone)
        {
            DownloadFilesCheckFinished();
        }
    }

    private void StopDownloadBecauseError()
    {
        for (int i = 0; i < webRequests.Count; i++)
        {
            webRequests[i].Abort();
        }
        resDownloadHandler.DownloadError();
    }

    private void DownloadFilesCheckFinished()
    {
        var resVersionPath = Path.Combine(newResDownloadPath, "ResVersion.bytes");
        BinarySerializeHelper.SerilizeToBinary(resVersionPath, newResVersion);

        monoBehaviour.StartCoroutine(CheckHaveAsyncComplete());
    }

    private IEnumerator CheckHaveAsyncComplete()
    {
        if (ObjectManager.Instance.ExistAsyncLoad() || ResourceManager.Instance.ExistAsyncLoad() || AssetBundleManager.Instance.ExistAsyncLoad())
        {
            yield return null;
        }
        ObjectManager.Instance.PurgeAll();
        ResourceManager.Instance.PurgeAll();
        AssetBundleManager.Instance.PurgeAll();

        PlayerPrefs.SetString("UpdateDir", newWorkingDir);
        FileUtils.Instance.PurgeCachedEntries();
        FileUtils.Instance.ResetSearchPath();
        FileUtils.Instance.AddSearchPath(newResDownloadPath);

        AssetBundleManager.Instance.BuildAssetInfo();

        ILRuntimeManager.Instance.LoadHotFixAssembly();
        ILRuntimeManager.Instance.AppDomain.Invoke("HotFixLibrary.HotFixHelper", "OnGameStart", null, null);

        resDownloadHandler.UpdateComplete();

        yield return null;
    }

}

public class ResDownloadRequest
{
    private UnityWebRequest webRequest = new UnityWebRequest();
    private Action<long, byte[], ResDownloadRequest> finishCallback = null;
    private MonoBehaviour monoBehaviour = null;
    private string urlStr;
    private ResFileInfo resFileInfo;
    private bool inProgress;

    public float DownloadProgress
    {
        get
        {
            return webRequest.downloadProgress;
        }
    }

    public ResFileInfo DownloadResFileInfo
    {
        get
        {
            return resFileInfo;
        }
    }

    public ResDownloadRequest(MonoBehaviour monoBehaviour, Action<long, byte[], ResDownloadRequest> finishCallback)
    {
        webRequest.method = "GET";
        this.monoBehaviour = monoBehaviour;
        this.finishCallback = finishCallback;
    }

    public void RequestUrl(string urlStr, ResFileInfo resFileInfo)
    {
        this.urlStr = urlStr;
        this.resFileInfo = resFileInfo;
        monoBehaviour.StartCoroutine(StartRequest());
    }

    public void ReRequest()
    {
        webRequest.Abort();
        monoBehaviour.StartCoroutine(StartRequest());
    }

    private IEnumerator StartRequest()
    {
        webRequest.url = urlStr + resFileInfo.FilePath;
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        inProgress = true;
        yield return webRequest.SendWebRequest();
        inProgress = false;
        finishCallback(webRequest.responseCode, webRequest.downloadHandler.data, this);
    }

    public void Abort()
    {
        webRequest.Abort();
    }

    public bool IsDone()
    {
        return !inProgress;
    }

}