using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class AsyncLoadAssetBundleParam : BasePoolObject
{
    public List<Action<ResourceItem>> AsyncLoadedCallbacks = new List<Action<ResourceItem>>();
    public List<uint> AssetBundleNamesCRC = new List<uint>();
    public List<AssetBundleItem> AlreadyLoadedBundleItem = new List<AssetBundleItem>();
    public uint AssetCRC;
    public AsyncLoadAssetBundleParam()
    {

    }

    public override void Reset()
    {
        AsyncLoadedCallbacks.Clear();
        AssetBundleNamesCRC.Clear();
        AlreadyLoadedBundleItem.Clear();
    }
}

public class AssetBundleManager : Singleton<AssetBundleManager>
{
    protected string assetBundlePath = Application.streamingAssetsPath + "/";
    protected string assetInfoBundleName = "assetbundleinfo";

    protected Dictionary<uint, AssetBaseInfo> assetBaseInfos = new Dictionary<uint, AssetBaseInfo>();

    protected Dictionary<uint, ResourceItem> resourceItemsDic = new Dictionary<uint, ResourceItem>();
    protected Dictionary<uint, AssetBundleItem> assetBundleItemDic = new Dictionary<uint, AssetBundleItem>();

    protected Dictionary<uint, AssetBundleItem> LoadingAssetBundleItemDic = new Dictionary<uint, AssetBundleItem>();
    protected Dictionary<uint, AssetBundleCreateRequest> LoadingAssetBundleRequestDic = new Dictionary<uint, AssetBundleCreateRequest>();
    protected Dictionary<uint, AsyncLoadAssetBundleParam> AsyncLoadAssetBundleParamDic = new Dictionary<uint, AsyncLoadAssetBundleParam>();

    protected ClassObjectPool<ResourceItem> resourceItemPool = ObjectManager.Instance.GetOrCreateClassPool<ResourceItem>(500);
    protected ClassObjectPool<AssetBundleItem> assetBundlePool = ObjectManager.Instance.GetOrCreateClassPool<AssetBundleItem>(500);
    protected ClassObjectPool<AsyncLoadAssetBundleParam> asyncLoadAssetBundleParamPool = ObjectManager.Instance.GetOrCreateClassPool<AsyncLoadAssetBundleParam>(50);

    /// <summary>
    /// Get basic assets info.
    /// </summary>
    /// <returns>Successed or not.</returns>
    public bool BuildAssetInfo()
    {
#if UNITY_EDITOR
        if (!ResourceManager.Instance.LoadFormAssetBundleForEditor)
            return false;
#endif
        assetBaseInfos.Clear();

        //AssetBundleItem bundleItem = LoadAssetBundle(assetBundlePath + assetInfoBundleName);
        AssetBundleItem bundleItem = LoadAssetBundle(assetInfoBundleName);
        bundleItem.Retain();
        var assetBundleInfoText = bundleItem.AssetBundleObj.LoadAsset<TextAsset>(assetInfoBundleName);
        if (assetBundleInfoText == null)
        {
            Debug.LogError(string.Format("Load {0} failed.", assetInfoBundleName));
            return false;
        }
        MemoryStream memoryStream = new MemoryStream(assetBundleInfoText.bytes);
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        AssetInfoConfig assetInfoConfig = binaryFormatter.Deserialize(memoryStream) as AssetInfoConfig;
        memoryStream.Close();
        foreach (var item in assetInfoConfig.AssetInfoList)
        {
            assetBaseInfos[item.CRC] = item;
        }
        return true;
    }

    public void StartCoroutine(MonoBehaviour monoBehaviour)
    {
        monoBehaviour.StartCoroutine(LoadAsyncCoroutine());
    }

    public void RemoveUnusedResource()
    {
        List<ResourceItem> cacheResList = new List<ResourceItem>();
        cacheResList.AddRange(resourceItemsDic.Values);
        foreach (var resItem in cacheResList)
        {
            resItem.Retain();
            resItem.Release();
        }
    }

    /// <summary>
    /// Get ResourceItem only from cache.
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    public ResourceItem GetResourceItem(uint crc)
    {
        resourceItemsDic.TryGetValue(crc, out ResourceItem item);
        return item;
    }

    /// <summary>
    /// Get ResourceItem only from cache.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public ResourceItem GetResourceItem(UnityEngine.Object obj)
    {
        foreach (var item in resourceItemsDic.Values)
        {
            if (item.ObjectGuid == obj.GetInstanceID())
            {
                return item;
            }
        }
        return null;
    }

    public void LoadSceneAssetBundle(string sceneName)
    {
#if UNITY_EDITOR
        if (!ResourceManager.Instance.LoadFormAssetBundleForEditor)
        {
            return;
        }
#endif
        if (!assetBaseInfos.TryGetValue(CRC32.GetCRC3232(sceneName), out AssetBaseInfo baseInfo))
        {
            Debug.LogError(string.Format("Cant not find res which crc is: {0}", sceneName));
            return;
        }
        foreach (var dependencyBundleName in baseInfo.Dependencies)
        {
            LoadAssetBundle(dependencyBundleName).Retain();
        }
        LoadAssetBundle(baseInfo.BundleName).Retain();
    }

    public void UnloadSceneAssetBundle(string sceneName)
    {
#if UNITY_EDITOR
        if (!ResourceManager.Instance.LoadFormAssetBundleForEditor)
        {
            return;
        }
#endif
        if (!assetBaseInfos.TryGetValue(CRC32.GetCRC3232(sceneName), out AssetBaseInfo baseInfo))
        {
            Debug.LogError(string.Format("Cant not find res which crc is: {0}", sceneName));
            return;
        }
        UnLoadAssetBundle(baseInfo.BundleName);
        foreach (var dependencyBundleName in baseInfo.Dependencies)
        {
            UnLoadAssetBundle(dependencyBundleName);
        }
    }

    /// <summary>
    /// Load asset, get ResourceItem object. After call this, load assetbundle at same time. After this, ResourceItem RefCount is 0, but AssetBundleItem will increase.
    /// </summary>
    /// <param name="crc">Path of asset.</param>
    /// <returns>ResourceItem of asset.</returns>
    public ResourceItem LoadResAndAssetBundle(uint crc)
    {
        ResourceItem item = null;
        if (resourceItemsDic.TryGetValue(crc, out item))
        {
            return item;
        }
        if (!assetBaseInfos.TryGetValue(crc, out AssetBaseInfo baseInfo))
        {
            Debug.LogError(string.Format("Cant not find res which crc is: {0}", crc));
            return null;
        }
        item = resourceItemPool.Spawn(true);
        item.SetAssetBaseInfo(baseInfo);
        foreach (var dependencyBundleName in baseInfo.Dependencies)
        {
            LoadAssetBundle(dependencyBundleName).Retain();
        }
        item.ABItem = LoadAssetBundle(baseInfo.BundleName);
        item.ABItem.Retain();
        resourceItemsDic[item.ResBaseInfo.CRC] = item;

        return item;
    }

    public void LoadResAndAssetBundleAsync(uint crc, Action<ResourceItem> callback)
    {
        if (resourceItemsDic.TryGetValue(crc, out ResourceItem item))
        {
            callback(item);
            return;
        }

        if (!assetBaseInfos.TryGetValue(crc, out AssetBaseInfo baseInfo))
        {
            Debug.LogError(string.Format("Cant not find res which crc is: {0}", crc));
            return;
        }
        if (AsyncLoadAssetBundleParamDic.ContainsKey(crc))
        {
            AsyncLoadAssetBundleParamDic[crc].AsyncLoadedCallbacks.Add(callback);
            return;
        }

        List<string> bunldeNames = new List<string>();
        foreach (var dependencyBundleName in baseInfo.Dependencies)
            bunldeNames.Add(dependencyBundleName);
        bunldeNames.Add(baseInfo.BundleName);

        AsyncLoadAssetBundleParam asyncLoadAssetBundleParam = asyncLoadAssetBundleParamPool.Spawn(true);
        asyncLoadAssetBundleParam.AssetCRC = crc;
        for (int i = 0; i < bunldeNames.Count; i++)
        {
            uint bundleCrc = CRC32.GetCRC3232(bunldeNames[i]);
            if (!assetBundleItemDic.ContainsKey(bundleCrc))
            {
                LoadAssetBundleAsync(bunldeNames[i]);
                asyncLoadAssetBundleParam.AssetBundleNamesCRC.Add(bundleCrc);
                asyncLoadAssetBundleParam.AsyncLoadedCallbacks.Add(callback);
            }
            else
            {
                // Cache have loaded assetbundle.
                assetBundleItemDic[bundleCrc].Retain();
                asyncLoadAssetBundleParam.AlreadyLoadedBundleItem.Add(assetBundleItemDic[bundleCrc]);
            }
        }

        AsyncLoadAssetBundleParamDic[crc] = asyncLoadAssetBundleParam;
    }

    ///// <summary>
    ///// 
    ///// </summary>
    ///// <param name="crc"></param>
    ///// <returns></returns>
    //public bool LoadResAndAssetBundleAsync(uint crc)
    //{
    //}

#if UNITY_EDITOR
    public ResourceItem LoadResForEditor<T>(string path) where T : UnityEngine.Object
    {
        var crc = CRC32.GetCRC3232(path);
        var item = new ResourceItem();
        // For release.
        item.SetAssetBaseInfo(new AssetBaseInfo()
        {
            CRC = crc,
            Path = path
        });
        item.SetGameObject(LoadAssetByEditor<T>(path));
        resourceItemsDic[crc] = item;
        return item;
    }

    protected T LoadAssetByEditor<T>(string path) where T : UnityEngine.Object
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
    }

    protected void UnloadAssetByEditor(UnityEngine.Object obj)
    {
        Resources.UnloadAsset(obj);
        System.GC.Collect();
    }
#endif

    /// <summary>
    /// WARNING: only called by ResourceItem in which Release method!!! when ResourceItem RefCount is 0, ResourceItem will call this method in which Release method.
    /// </summary>
    /// <param name="item">ResourceItem which RefCount is 0</param>
    public void ReleaseAsset(ResourceItem item)
    {
#if UNITY_EDITOR
        if (!ResourceManager.Instance.LoadFormAssetBundleForEditor)
        {
            if (!item.ResBaseInfo.Path.EndsWith(".prefab"))
                // Unload resource that dont load from assetbundle.
                UnloadAssetByEditor(item.GetGameObject<UnityEngine.Object>());
            resourceItemsDic.Remove(item.ResBaseInfo.CRC);
            resourceItemPool.Recycle(item);
            return;
        }
#endif
        if (item == null || item.ResBaseInfo == null)
        {
            return;
        }
        UnLoadAssetBundle(item.ResBaseInfo.BundleName);
        foreach (var dependencyBundleName in item.ResBaseInfo.Dependencies)
        {
            UnLoadAssetBundle(dependencyBundleName);
        }

        resourceItemsDic.Remove(item.ResBaseInfo.CRC);
        resourceItemPool.Recycle(item);
    }

    /// <summary>
    /// Get or load AssetBundleItem.
    /// </summary>
    /// <param name="bundleName">AssetBundle name.</param>
    /// <returns>AssetBundleItem of assetbundle.</returns>
    private AssetBundleItem LoadAssetBundle(string bundleName)
    {
        uint crc = CRC32.GetCRC3232(bundleName);
        AssetBundleItem item;
        var startTime = Time.realtimeSinceStartup;
        RESTARTCHECK:
        if (!assetBundleItemDic.TryGetValue(crc, out item) || item == null)
        {
            // ERROR!!!
            //if (LoadingAssetBundleRequestDic.ContainsKey(crc))
            //{
            //    Thread.Sleep(1000 / 60);
            //    goto RESTARTCHECK;
            //}
            //else
            //{
            item = assetBundlePool.Spawn(true);
            //item.LoadAssetBundle(assetBundlePath + bundleName);
            item.LoadAssetBundle(FileUtils.Instance.FullPathForFilename(bundleName));
            assetBundleItemDic[crc] = item;
            //}
        }
        return item;
    }

    private void LoadAssetBundleAsync(string bundleName)
    {
        uint crc = CRC32.GetCRC3232(bundleName);
        AssetBundleItem item = assetBundlePool.Spawn(true);

        LoadingAssetBundleItemDic[crc] = item;
        //LoadingAssetBundleRequestDic[crc] = item.LoadAssetBundleAsync(assetBundlePath + bundleName);
        LoadingAssetBundleRequestDic[crc] = item.LoadAssetBundleAsync(FileUtils.Instance.FullPathForFilename(bundleName));
    }

    /// <summary>
    /// Decrease RefCount of AssetBundleItem.
    /// </summary>
    /// <param name="bundleName">AssetBundle Name.</param>
    private void UnLoadAssetBundle(string bundleName)
    {
        uint crc = CRC32.GetCRC3232(bundleName);
        if (assetBundleItemDic.TryGetValue(crc, out AssetBundleItem item))
        {
            item.Release();
            if (item.RefCount <= 0)
            {
                assetBundlePool.Recycle(item);
                assetBundleItemDic.Remove(crc);
            }
        }
    }

    private IEnumerator LoadAsyncCoroutine()
    {
        var removeKeys = new List<uint>();
        while (true)
        {
            removeKeys.Clear();
            // if assetbundle loaded success, add to assetBundleItemDic.
            foreach (var key in LoadingAssetBundleRequestDic.Keys)
            {
                if (LoadingAssetBundleRequestDic[key].isDone)
                {
                    removeKeys.Add(key);
                    LoadingAssetBundleItemDic[key].AssetBundleObj = LoadingAssetBundleRequestDic[key].assetBundle;

                    assetBundleItemDic[key] = LoadingAssetBundleItemDic[key];
                    // Retain in here. Prevent in sync process, load assetbundle and release in case assetbundle be released.
                    assetBundleItemDic[key].Retain();
                }
            }
            // if assetbundle loaded success, remove from check list.
            for (int i = 0; i < removeKeys.Count; i++)
            {
                LoadingAssetBundleItemDic.Remove(removeKeys[i]);
                LoadingAssetBundleRequestDic.Remove(removeKeys[i]);
            }

            removeKeys.Clear();
            foreach (var keyPair in AsyncLoadAssetBundleParamDic)
            {
                // check resrouce all dependency assetbundle load success or not.
                for (int i = 0; i < keyPair.Value.AssetBundleNamesCRC.Count; i++)
                {
                    // still exist assetbundle have not loaded.
                    if (!assetBundleItemDic.ContainsKey(keyPair.Value.AssetBundleNamesCRC[i]))
                    {
                        goto NEXTASYNCLOADPARAM;
                    }
                }

                removeKeys.Add(keyPair.Key);
                // load success in sync.
                if (resourceItemsDic.ContainsKey(keyPair.Key))
                {
                    for (int i = 0; i < keyPair.Value.AsyncLoadedCallbacks.Count; i++)
                    {
                        keyPair.Value.AsyncLoadedCallbacks[i](resourceItemsDic[keyPair.Key]);
                    }
                    // if load success in sync. Release cache assetbundle in method 'LoadResAndAssetBundleAsync'.
                    for (int i = 0; i < keyPair.Value.AlreadyLoadedBundleItem.Count; i++)
                    {
                        keyPair.Value.AlreadyLoadedBundleItem[i].Release();
                    }
                    continue;
                }

                var baseInfo = assetBaseInfos[keyPair.Value.AssetCRC];
                ResourceItem item = resourceItemPool.Spawn(true);
                item.SetAssetBaseInfo(baseInfo);
                item.ABItem = assetBundleItemDic[CRC32.GetCRC3232(baseInfo.BundleName)];
                resourceItemsDic[item.ResBaseInfo.CRC] = item;

                for (int i = 0; i < keyPair.Value.AsyncLoadedCallbacks.Count; i++)
                {
                    keyPair.Value.AsyncLoadedCallbacks[i](item);
                }

                NEXTASYNCLOADPARAM:;
            }
            for (int i = 0; i < removeKeys.Count; i++)
            {
                asyncLoadAssetBundleParamPool.Recycle(AsyncLoadAssetBundleParamDic[removeKeys[i]]);
                AsyncLoadAssetBundleParamDic.Remove(removeKeys[i]);
            }

            yield return null;
        }
    }

    public bool ExistAsyncLoad()
    {
        return LoadingAssetBundleItemDic.Count > 0 || LoadingAssetBundleRequestDic.Count > 0 || AsyncLoadAssetBundleParamDic.Count > 0;
    }

    public void PurgeAll()
    {
        uint[] keys = new uint[resourceItemsDic.Keys.Count];
        resourceItemsDic.Keys.CopyTo(keys, 0);
        for (int i = 0; i < keys.Length; i++)
        {
            var resourceItem = resourceItemsDic[keys[i]];
            while (resourceItem.RefCount > 0)
            {
                resourceItem.Release();
            }
        }
        keys = new uint[assetBundleItemDic.Keys.Count];
        assetBundleItemDic.Keys.CopyTo(keys, 0);
        for (int i = 0; i < keys.Length; i++)
        {
            var assetBundleItem = assetBundleItemDic[keys[i]];
            while (assetBundleItem.RefCount > 0)
            {
                assetBundleItem.Release();
            }
        }
        assetBundleItemDic.Clear();
    }

}

public class AssetBundleItem : BasePoolObject
{
    public AssetBundle AssetBundleObj = null;
    public int RefCount = 0;

    public override void Reset()
    {
        AssetBundleObj = null;
        RefCount = 0;
    }

    public void LoadAssetBundle(string bundlePath)
    {
        Reset();

        AssetBundle assetBundle = AssetBundle.LoadFromFile(bundlePath);
        if (assetBundle == null)
        {
            Debug.LogError("Load asset bundle failed: " + bundlePath);
        }
        AssetBundleObj = assetBundle;
    }

    public AssetBundleCreateRequest LoadAssetBundleAsync(string bundlePath)
    {
        Reset();
        AssetBundleCreateRequest assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(bundlePath);
        if (assetBundleCreateRequest == null)
        {
            Debug.LogError("Load assetbundle async failed: " + bundlePath);
        }
        return assetBundleCreateRequest;
    }

    public void Retain()
    {
        ++RefCount;
    }

    public void Release()
    {
        --RefCount;
        if (RefCount <= 0)
        {
            if (AssetBundleObj != null)
            {
                AssetBundleObj.Unload(true);
                AssetBundleObj = null;
            }
            Reset();
        }
    }

}

/// <summary>
/// Indicate asset in project.
/// </summary>
public class ResourceItem : BasePoolObject
{
    public AssetBaseInfo ResBaseInfo = null;
    public AssetBundleItem ABItem = null;

    /***********************************************************************/

    private UnityEngine.Object gameObject = null;
    public UnityEngine.Object GameObject { get { return gameObject; } }
    public int ObjectGuid = 0;
    public float LastUsedTime = 0;
    public int RefCount = 0;

    public override void Reset()
    {
        gameObject = null;
        ObjectGuid = 0;
        LastUsedTime = 0;
        RefCount = 0;
    }

    public void SetAssetBaseInfo(AssetBaseInfo assetBaseInfo)
    {
        this.ResBaseInfo = assetBaseInfo;
    }

    public T GetGameObject<T>() where T : UnityEngine.Object
    {
        if (gameObject != null)
            return gameObject as T;
        if (ABItem == null)
            return null;
        gameObject = ABItem.AssetBundleObj.LoadAsset<T>(ResBaseInfo.AssetName);
        ObjectGuid = gameObject.GetInstanceID();
        return gameObject as T;
    }

    /// <summary>
    /// Get GameObject async
    /// </summary>
    /// <param name="isSprite">GameObject is sprite.</param>
    /// <param name="gameObject">If exist GameObject, gameObject is Result.</param>
    /// <param name="assetBundleRequest">If not exist GameObject, assetBundleRequest is not null.</param>
    public void GetGameObjectAsync(bool isSprite, ref UnityEngine.Object gameObject, ref AssetBundleRequest assetBundleRequest)
    {
        if (gameObject != null)
        {
            gameObject = this.gameObject;
            return;
        }
        if (isSprite)
        {
            assetBundleRequest = ABItem.AssetBundleObj.LoadAssetAsync<Sprite>(ResBaseInfo.AssetName);
        }
        else
        {
            assetBundleRequest = ABItem.AssetBundleObj.LoadAssetAsync(ResBaseInfo.AssetName);
        }
    }

    public void SetGameObject(UnityEngine.Object obj)
    {
        gameObject = obj;
        ObjectGuid = gameObject.GetInstanceID();
    }

    public void Retain()
    {
        ++RefCount;
    }

    public void Release()
    {
        --RefCount;
        if (RefCount <= 0)
        {
            AssetBundleManager.Instance.ReleaseAsset(this);
            gameObject = null;
            ABItem = null;
        }
    }

}