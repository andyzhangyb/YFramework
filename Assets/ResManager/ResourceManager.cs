using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public enum AsyncLoadPriority
{
    Hight = 0,
    Normal,
    Slow,
    Count
}

public class AsyncLoadResParam : BasePoolObject
{
    public List<AsyncLoadedCallback> AsyncLoadedCallbacks = new List<AsyncLoadedCallback>();
    public uint CRC;
    public string ResPath;
    public bool IsCache = false;
    public bool IsSprite = false;
    public AsyncLoadPriority Priority = AsyncLoadPriority.Normal;

    public override void Reset()
    {
        AsyncLoadedCallbacks.Clear();
        CRC = 0;
        ResPath = "";
        IsCache = false;
        IsSprite = false;
        Priority = AsyncLoadPriority.Normal;
    }
}

public class AsyncLoadedCallback : BasePoolObject
{
    public Action<string, ulong, UnityEngine.Object, object> LoadedResFinishCallback = null;
    public Action<string, ulong, ResourceItem, object> LoadedItemFinishCallback = null;
    public ulong AsyncLoadId = 0;
    public object TransbackData = null;

    public override void Reset()
    {
        LoadedResFinishCallback = null;
        LoadedItemFinishCallback = null;
        AsyncLoadId = 0;
        TransbackData = null;
    }
}

public class ResourceManager : Singleton<ResourceManager>
{
    public bool LoadFormAssetBundleForEditor = false;

    private const int MAXCACHECOUNT = 500;
    protected List<ResourceItem> cacheAsset = new List<ResourceItem>();
    protected List<ResourceItem> keepInMemoryAsset = new List<ResourceItem>();

    protected ClassObjectPool<AsyncLoadResParam> AsyncLoadResParamPool = ObjectManager.Instance.GetOrCreateClassPool<AsyncLoadResParam>(50);
    protected ClassObjectPool<AsyncLoadedCallback> AsyncLoadedCallbackPool = ObjectManager.Instance.GetOrCreateClassPool<AsyncLoadedCallback>(100);

    protected List<AsyncLoadResParam>[] asyncLoadingAssetList = new List<AsyncLoadResParam>[(int)AsyncLoadPriority.Count];
    protected Dictionary<uint, AsyncLoadResParam> asyncLoadingAssetDic = new Dictionary<uint, AsyncLoadResParam>();

    protected MonoBehaviour monoBehaviour = null;
    private const long MAXLOADRESTIME = 200000;

    private AsyncLoadResParam waiteAsyncLoadResItem = null;

    public const ulong ASYNC_SYNC_ID = 0;
    public const ulong INVALIDASYNCID = ulong.MaxValue;
    private ulong asyncLoadId = ASYNC_SYNC_ID + 1;

    public ulong GetAsyncLoadId()
    {
        if (asyncLoadId == INVALIDASYNCID)
        {
            asyncLoadId = ASYNC_SYNC_ID + 1;
        }
        return asyncLoadId++;
    }

    public void SetMonoBehaviour(MonoBehaviour monoBehaviour)
    {
        for (int i = 0; i < (int)AsyncLoadPriority.Count; i++)
        {
            asyncLoadingAssetList[i] = new List<AsyncLoadResParam>();
        }
        this.monoBehaviour = monoBehaviour;
        monoBehaviour.StartCoroutine(LoadAsyncCoroutine());
        AssetBundleManager.Instance.StartCoroutine(monoBehaviour);
    }

    /// <summary>
    /// PreLoad resource, this part will keep in memory and dont release.
    /// </summary>
    /// <param name="path">res path.</param>
    public void PreLoadResource(string path)
    {
        var resItem = LoadResourceItem<UnityEngine.Object>(path, false);
        keepInMemoryAsset.Add(resItem);
    }

    /// <summary>
    /// Load resource sync.
    /// </summary>
    /// <typeparam name="T">resource type.</typeparam>
    /// <param name="path">resource path.</param>
    /// <param name="cache">cache or not this resource.</param>
    /// <returns>Resource.</returns>
    public T LoadResource<T>(string path, bool cache = true) where T : UnityEngine.Object
    {
        return LoadResourceItem<T>(path, cache).GetGameObject<T>();
    }

    public ResourceItem LoadResourceItem<T>(string path, bool cache = true) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        uint crc = CRC32.GetCRC3232(path);
        ResourceItem item = GetCacheResouceItem(crc);
        if (item != null) return item;

#if UNITY_EDITOR
        if (!LoadFormAssetBundleForEditor)
        {
            // Editor env and dont load from assetbundle, direct load from disk.
            item = AssetBundleManager.Instance.LoadResForEditor<T>(path);
            item.Retain();
        }
#endif
        if (item == null)
        {
            item = AssetBundleManager.Instance.LoadResAndAssetBundle(crc);
            item.Retain();
        }
        item.GetGameObject<T>();
        if (cache)
        {
            WashOut();

            item.Retain();
            cacheAsset.Add(item);
        }
        return item;
    }

    public ulong LoadResourceAsync(string path, Action<string, ulong, UnityEngine.Object, object> loadedCallback, bool isCache = false, AsyncLoadPriority priority = AsyncLoadPriority.Normal, object tansformData = null, bool isSprite = false, uint crc = 0)
    {
        if (crc == 0)
        {
            crc = CRC32.GetCRC3232(path);
        }
        ResourceItem item = GetCacheResouceItem(crc);
        if (item != null)
        {
            if (loadedCallback != null)
            {
                loadedCallback(path, ASYNC_SYNC_ID, item.GetGameObject<UnityEngine.Object>(), tansformData);
            }
            return ASYNC_SYNC_ID;
        }
        AsyncLoadResParam asyncLoadResParam = null;
        if (!asyncLoadingAssetDic.TryGetValue(crc, out asyncLoadResParam))
        {
            asyncLoadResParam = AsyncLoadResParamPool.Spawn(true);
            asyncLoadResParam.CRC = crc;
            asyncLoadResParam.ResPath = path;
            asyncLoadResParam.IsCache = isCache;
            asyncLoadResParam.IsSprite = isSprite;
            asyncLoadResParam.Priority = priority;
            asyncLoadingAssetList[(int)priority].Add(asyncLoadResParam);
            asyncLoadingAssetDic[crc] = asyncLoadResParam;
        }
        var asyncLoadedCallback = AsyncLoadedCallbackPool.Spawn(true);
        asyncLoadedCallback.LoadedResFinishCallback = loadedCallback;
        asyncLoadedCallback.AsyncLoadId = GetAsyncLoadId();
        asyncLoadedCallback.TransbackData = tansformData;
        asyncLoadResParam.AsyncLoadedCallbacks.Add(asyncLoadedCallback);
        return asyncLoadedCallback.AsyncLoadId;
    }

    public ulong LoadResourceItemAsync(string path, Action<string, ulong, ResourceItem, object> loadedCallback, bool isCache = false, AsyncLoadPriority priority = AsyncLoadPriority.Normal, object tansformData = null, bool isSprite = false, uint crc = 0)
    {
        if (crc == 0)
        {
            crc = CRC32.GetCRC3232(path);
        }
        ResourceItem item = GetCacheResouceItem(crc);
        if (item != null)
        {
            if (loadedCallback != null)
            {
                loadedCallback(path, ASYNC_SYNC_ID, item, tansformData);
            }
            return ASYNC_SYNC_ID;
        }
        AsyncLoadResParam asyncLoadResParam = null;
        if (!asyncLoadingAssetDic.TryGetValue(crc, out asyncLoadResParam))
        {
            asyncLoadResParam = AsyncLoadResParamPool.Spawn(true);
            asyncLoadResParam.CRC = crc;
            asyncLoadResParam.ResPath = path;
            asyncLoadResParam.IsCache = isCache;
            asyncLoadResParam.IsSprite = isSprite;
            asyncLoadResParam.Priority = priority;
            asyncLoadingAssetList[(int)priority].Add(asyncLoadResParam);
            asyncLoadingAssetDic[crc] = asyncLoadResParam;
        }
        var asyncLoadedCallback = AsyncLoadedCallbackPool.Spawn(true);
        asyncLoadedCallback.LoadedItemFinishCallback = loadedCallback;
        asyncLoadedCallback.AsyncLoadId = GetAsyncLoadId();
        asyncLoadedCallback.TransbackData = tansformData;
        asyncLoadResParam.AsyncLoadedCallbacks.Add(asyncLoadedCallback);
        return asyncLoadedCallback.AsyncLoadId;
    }

    public bool CancelLoadAssetAsync(ulong asyncLoadId)
    {
        var result = false;
        for (int priorityListIndex = 0; priorityListIndex < asyncLoadingAssetList.Length; priorityListIndex++)
        {
            for (int i = 0; i < asyncLoadingAssetList[priorityListIndex].Count; i++)
            {
                for (int j = 0; j < asyncLoadingAssetList[priorityListIndex][j].AsyncLoadedCallbacks.Count; j++)
                {
                    if (asyncLoadingAssetList[priorityListIndex][j].AsyncLoadedCallbacks[j].AsyncLoadId == asyncLoadId)
                    {
                        if (waiteAsyncLoadResItem != asyncLoadingAssetList[priorityListIndex][j])
                        {
                            asyncLoadingAssetList[priorityListIndex][j].AsyncLoadedCallbacks.RemoveAt(j);
                            result = true;
                        }

                        if (asyncLoadingAssetList[priorityListIndex][j].AsyncLoadedCallbacks.Count == 0)
                        {
                            AsyncLoadResParamPool.Recycle(asyncLoadingAssetList[priorityListIndex][j]);
                            asyncLoadingAssetList[priorityListIndex].RemoveAt(j);
                        }
                        goto ENDCANCEL;
                    }
                }
            }
        }
    ENDCANCEL:
        return result;
    }

    private IEnumerator LoadAsyncCoroutine()
    {
        int priorityListIndex = 0;
        long lastYiledTime = System.DateTime.Now.Ticks;
        bool haveCallYield = false;
        while (true)
        {
            haveCallYield = false;
            if (waiteAsyncLoadResItem != null)
            {
                ResourceItem item = AssetBundleManager.Instance.GetResourceItem(waiteAsyncLoadResItem.CRC);
                if (item == null)
                {
                    yield return null;
                    continue;
                }
                UnityEngine.Object gameObject = item.GameObject;
                if (gameObject == null)
                {
                    AssetBundleRequest assetBundleRequest = null;
                    item.GetGameObjectAsync(waiteAsyncLoadResItem.IsSprite, ref gameObject, ref assetBundleRequest);
                    yield return assetBundleRequest;
                    lastYiledTime = System.DateTime.Now.Ticks;
                    if (assetBundleRequest.isDone)
                    {
                        if (waiteAsyncLoadResItem.IsSprite)
                        {
                            gameObject = item.GetGameObject<Sprite>();
                        }
                        else
                        {
                            gameObject = item.GetGameObject<UnityEngine.Object>();
                        }
                    }
                }

                if (waiteAsyncLoadResItem.IsCache)
                {
                    WashOut();

                    item.Retain();
                    cacheAsset.Add(item);
                }

                AsyncLoadedCallback asyncLoadedCallback = null;
                for (int i = 0; i < waiteAsyncLoadResItem.AsyncLoadedCallbacks.Count; i++)
                {
                    item.Retain();
                    asyncLoadedCallback = waiteAsyncLoadResItem.AsyncLoadedCallbacks[i];
                    if (asyncLoadedCallback.LoadedResFinishCallback != null)
                    {
                        asyncLoadedCallback.LoadedResFinishCallback(waiteAsyncLoadResItem.ResPath, asyncLoadedCallback.AsyncLoadId, gameObject, asyncLoadedCallback.TransbackData);
                    }
                    if (asyncLoadedCallback.LoadedItemFinishCallback != null)
                    {
                        asyncLoadedCallback.LoadedItemFinishCallback(waiteAsyncLoadResItem.ResPath, asyncLoadedCallback.AsyncLoadId, item, asyncLoadedCallback.TransbackData);
                    }
                    AsyncLoadedCallbackPool.Recycle(asyncLoadedCallback);
                }

                for (priorityListIndex = 0; priorityListIndex < asyncLoadingAssetList.Length; priorityListIndex++)
                {
                    var removed = false;
                    for (int i = 0; i < asyncLoadingAssetList[priorityListIndex].Count; i++)
                    {
                        if (asyncLoadingAssetList[priorityListIndex][i] == waiteAsyncLoadResItem)
                        {
                            asyncLoadingAssetList[priorityListIndex].RemoveAt(i);
                            removed = true;
                            break;
                        }
                    }
                    if (removed)
                    {
                        break;
                    }
                }
                asyncLoadingAssetDic.Remove(waiteAsyncLoadResItem.CRC);

                AsyncLoadResParamPool.Recycle(waiteAsyncLoadResItem);
                waiteAsyncLoadResItem = null;

                if (System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
                {
                    yield return null;
                    lastYiledTime = System.DateTime.Now.Ticks;
                    haveCallYield = true;
                }
            }

            for (priorityListIndex = 0; priorityListIndex < asyncLoadingAssetList.Length; priorityListIndex++)
            {
                if (asyncLoadingAssetList[(int)AsyncLoadPriority.Hight].Count > 0)
                {
                    priorityListIndex = (int)AsyncLoadPriority.Hight;
                }
                if (asyncLoadingAssetList[(int)AsyncLoadPriority.Normal].Count > 0)
                {
                    priorityListIndex = (int)AsyncLoadPriority.Normal;
                }
                if (asyncLoadingAssetList[priorityListIndex].Count == 0)
                    continue;
                AsyncLoadResParam asyncLoadResParam = asyncLoadingAssetList[priorityListIndex][0];

                ResourceItem item = null;
                UnityEngine.Object gameObject = null;
#if UNITY_EDITOR
                if (!LoadFormAssetBundleForEditor)
                {
                    if (asyncLoadResParam.IsSprite)
                    {
                        item = AssetBundleManager.Instance.LoadResForEditor<Sprite>(asyncLoadResParam.ResPath);
                        gameObject = item.GetGameObject<Sprite>();
                    }
                    else
                    {
                        item = AssetBundleManager.Instance.LoadResForEditor<UnityEngine.Object>(asyncLoadResParam.ResPath);
                        gameObject = item.GetGameObject<UnityEngine.Object>();
                    }
                    waiteAsyncLoadResItem = asyncLoadResParam;
                    break;
                }
#endif
                AssetBundleManager.Instance.LoadResAndAssetBundleAsync(asyncLoadResParam.CRC, OnLoadedResourceItemSuccess);
                waiteAsyncLoadResItem = asyncLoadResParam;
                break;
            }

            if (!haveCallYield)
            {
                yield return null;
                lastYiledTime = System.DateTime.Now.Ticks;
            }
        }
    }

    public void OnLoadedResourceItemSuccess(ResourceItem resourceItem)
    {

    }

    /// <summary>
    /// Release source of this Game Object.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool ReleaseResource(UnityEngine.Object obj)
    {
        if (obj == null)
        {
            return false;
        }
        ResourceItem item = AssetBundleManager.Instance.GetResourceItem(obj);
        if (item != null)
        {
            item.Release();
        }
        return true;
    }

    /// <summary>
    /// Release source of this path.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool ReleaseResource(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }
        uint crc = CRC32.GetCRC3232(path);
        ResourceItem item = AssetBundleManager.Instance.GetResourceItem(crc);
        if (item != null)
        {
            item.Release();
        }
        return true;
    }

    public bool ReleaseResource(ResourceItem item)
    {
        if (item == null)
        {
            return false;
        }
        item.Release();
        return true;
    }

    private void WashOut()
    {
        while (cacheAsset.Count >= MAXCACHECOUNT)
        {
            for (int i = 0; i < MAXCACHECOUNT / 2; i++)
            {
                ResourceItem item = cacheAsset[0];
                item.Release();
                cacheAsset.RemoveAt(0);
            }
        }
    }

    public ResourceItem GetCacheResouceItem(uint crc, int addRefCount = 1)
    {
        ResourceItem item = AssetBundleManager.Instance.GetResourceItem(crc);
        if (item != null)
        {
            while (addRefCount-- > 0)
            {
                item.Retain();
            }
            item.LastUsedTime = Time.realtimeSinceStartup;
        }
        return item;
    }

    public void RemoveUnusedResource()
    {
        AssetBundleManager.Instance.RemoveUnusedResource();
#if UNITY_EDITOR
        Resources.UnloadUnusedAssets();
#endif
    }

    public void ClearCache()
    {
        for (int i = 0; i < cacheAsset.Count; i++)
        {
            cacheAsset[i].Release();
        }
        cacheAsset.Clear();
    }

    public bool ExistAsyncLoad()
    {
        for (int i = 0; i < asyncLoadingAssetList.Length; i++)
        {
            if (asyncLoadingAssetList[i].Count > 0)
            {
                return true;
            }
        }
        return false;
    }

    public void PurgeAll()
    {
        ClearCache();
        for (int i = 0; i < keepInMemoryAsset.Count; i++)
        {
            keepInMemoryAsset[i].Release();
        }
        keepInMemoryAsset.Clear();
        asyncLoadingAssetDic.Clear();
    }
}
