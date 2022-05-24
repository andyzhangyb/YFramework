using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class AsyncLoadObjectParam : BasePoolObject
{
    public Action<string, UnityEngine.GameObject, object> LoadedResFinishCallback = null;
    public uint CRC;
    public string ResPath;
    public object TransbackData = null;
    public Transform ParentTransform;
    public AsyncLoadObjectParam()
    {
        Reset();
    }

    public override void Reset()
    {
        LoadedResFinishCallback = null;
        CRC = 0;
        ResPath = "";
        TransbackData = null;
        ParentTransform = null;
    }
}

public class ObjectManager : Singleton<ObjectManager>
{
    protected Transform recycleTransform;
    // All dont use GameObject.
    protected Dictionary<uint, List<GameObjectItem>> gameObjectPoolDic = new Dictionary<uint, List<GameObjectItem>>();
    // All in use GameObject.
    protected Dictionary<int, GameObjectItem> gameObjectDic = new Dictionary<int, GameObjectItem>();
    // loading asset async.
    protected Dictionary<ulong, AsyncLoadObjectParam> loadingAssetAsyncDic = new Dictionary<ulong, AsyncLoadObjectParam>();

    // GameObjectItem poll.
    protected ClassObjectPool<GameObjectItem> gameObjectItemPool;
    // AsyncLoadObjectParam poll.
    protected ClassObjectPool<AsyncLoadObjectParam> asyncLoadObjectParamPool;

    public ObjectManager()
    {
        gameObjectItemPool = GetOrCreateClassPool<GameObjectItem>(500);
        asyncLoadObjectParamPool = GetOrCreateClassPool<AsyncLoadObjectParam>(100);
    }

    ~ObjectManager()
    {
        ClearAllCache();
    }

    public void Init(Transform recycleTransform)
    {
        this.recycleTransform = recycleTransform;
    }

    public bool IsInAsyncLoad(ulong asyncLoadId)
    {
        return loadingAssetAsyncDic.ContainsKey(asyncLoadId);
    }

    public bool ManageByObjectManager(GameObject gameObject)
    {
        return gameObjectDic.ContainsKey(gameObject.GetInstanceID());
    }

    public void ClearAllCache()
    {
        uint[] keys = new uint[gameObjectPoolDic.Keys.Count];
        gameObjectPoolDic.Keys.CopyTo(keys, 0);
        for (int i = 0; i < keys.Length; i++)
        {
            ClearCache(keys[i]);
        }
    }

    public void ClearCache(string path)
    {
        ClearCache(CRC32.GetCRC3232(path));
    }

    public void ClearCache(uint crc)
    {
        if (!gameObjectPoolDic.TryGetValue(crc, out List<GameObjectItem> gameObjectItems))
        {
            return;
        }
        while (gameObjectItems.Count > 0)
        {
            var gameObjectItem = gameObjectItems[0];
            gameObjectItems.RemoveAt(0);

            GameObject.Destroy(gameObjectItem.GameObj);
            //ReleaseGameObject(gameObjectItem.GameObj, 0, false);
            ResourceManager.Instance.ReleaseResource(gameObjectItem.ResItem);
            gameObjectItemPool.Recycle(gameObjectItem);
        }
        gameObjectPoolDic.Remove(crc);
    }

    public void PreloadGameObject(string path, int count = 1, bool isCache = true)
    {
        List<UnityEngine.GameObject> gameObjects = new List<GameObject>();
        while (count > 0)
        {
            gameObjects.Add(InstantiateObject(path, null, isCache));
            --count;
        }
        while (gameObjects.Count > 0)
        {
            ReleaseGameObject(gameObjects[0]);
            gameObjects.RemoveAt(0);
        }
    }

    /// <summary>
    /// Sync load GameObject.
    /// </summary>
    /// <param name="path">Prefab file path.</param>
    /// <param name="parentTransform">Game Object will add to.</param>
    /// <param name="cache">Cache assetbundle and this asset.</param>
    /// <returns>Instantiate gameobject.</returns>
    public GameObject InstantiateObject(string path, Transform parentTransform = null, bool cache = true)
    {
        uint crc = CRC32.GetCRC3232(path);
        GameObjectItem gameObjectItem = GetUnusedGameObjectItem(crc);
        if (gameObjectItem == null)
        {
            gameObjectItem = gameObjectItemPool.Spawn(true);
            gameObjectItem.CRC = crc;
            gameObjectItem.ResItem = ResourceManager.Instance.LoadResourceItem<UnityEngine.GameObject>(path, cache);
            if (parentTransform != null)
            {
                gameObjectItem.GameObj = GameObject.Instantiate(gameObjectItem.ResItem.GetGameObject<UnityEngine.GameObject>(), parentTransform);
            }
            else
            {
                gameObjectItem.GameObj = GameObject.Instantiate(gameObjectItem.ResItem.GetGameObject<UnityEngine.GameObject>());
            }
            gameObjectItem.GUID = gameObjectItem.GameObj.GetInstanceID();
        }
        else
        {
            gameObjectItem.GameObj.GetComponent<Transform>().SetParent(parentTransform);
        }
        gameObjectDic[gameObjectItem.GameObj.GetInstanceID()] = gameObjectItem;
        return gameObjectItem.GameObj;
    }

    public ulong InstantiateObjectAsync(string path, Action<string, UnityEngine.GameObject, object> loadedCallback, Transform parentTransform = null, bool isCache = true, AsyncLoadPriority priority = AsyncLoadPriority.Normal, object tansformData = null)
    {
        uint crc = CRC32.GetCRC3232(path);
        GameObjectItem gameObjectItem = GetUnusedGameObjectItem(crc);
        // If have exist dont used item.
        if (gameObjectItem != null)
        {
            if (parentTransform != null)
            {
                gameObjectItem.GameObj.transform.SetParent(parentTransform);
            }
            gameObjectDic[gameObjectItem.GameObj.GetInstanceID()] = gameObjectItem;
            loadedCallback(path, gameObjectItem.GameObj, tansformData);
            return ResourceManager.ASYNC_SYNC_ID;
        }
        // If have loaded this resource.
        ResourceItem resourceItem = ResourceManager.Instance.GetCacheResouceItem(crc);
        if (resourceItem != null)
        {
            gameObjectItem = gameObjectItemPool.Spawn(true);
            gameObjectItem.CRC = crc;
            gameObjectItem.ResItem = resourceItem;
            gameObjectItem.GameObj = GameObject.Instantiate(gameObjectItem.ResItem.GetGameObject<UnityEngine.GameObject>());
            gameObjectItem.GUID = gameObjectItem.GameObj.GetInstanceID();
            if (parentTransform != null)
            {
                gameObjectItem.GameObj.transform.SetParent(parentTransform);
            }
            gameObjectDic[gameObjectItem.GameObj.GetInstanceID()] = gameObjectItem;
            loadedCallback(path, gameObjectItem.GameObj, tansformData);
            return ResourceManager.ASYNC_SYNC_ID;
        }

        AsyncLoadObjectParam asyncLoadObjectParam = asyncLoadObjectParamPool.Spawn(true);
        asyncLoadObjectParam.CRC = crc;
        asyncLoadObjectParam.ResPath = path;
        asyncLoadObjectParam.LoadedResFinishCallback = loadedCallback;
        asyncLoadObjectParam.TransbackData = tansformData;
        asyncLoadObjectParam.ParentTransform = parentTransform;

        var asyncLoadId = ResourceManager.Instance.LoadResourceItemAsync(path, LoadResItemcallback, isCache, priority, tansformData, false, crc);
        if (asyncLoadId != ResourceManager.ASYNC_SYNC_ID)
        {
            loadingAssetAsyncDic[asyncLoadId] = asyncLoadObjectParam;
            return asyncLoadId;
        }
        return ResourceManager.ASYNC_SYNC_ID;
    }

    private void LoadResItemcallback(string path, ulong asyncLoadId, ResourceItem resource, object data)
    {
        uint crc = resource.ResBaseInfo.CRC;
        if (!loadingAssetAsyncDic.ContainsKey(asyncLoadId) && asyncLoadId != ResourceManager.ASYNC_SYNC_ID)
        {
            resource.Release();
            return;
        }
        var asyncLoadObjectParam = loadingAssetAsyncDic[asyncLoadId];
        // If exist UnusedGameObject, will not step to here. 
        GameObjectItem gameObjectItem = GetUnusedGameObjectItem(crc);
        if (gameObjectItem == null)
        {
            gameObjectItem = gameObjectItemPool.Spawn(true);
            gameObjectItem.CRC = crc;
            gameObjectItem.ResItem = resource;
            gameObjectItem.GameObj = GameObject.Instantiate(gameObjectItem.ResItem.GetGameObject<UnityEngine.GameObject>());
            gameObjectItem.GUID = gameObjectItem.GameObj.GetInstanceID();
        }
        if (asyncLoadObjectParam.ParentTransform != null)
        {
            gameObjectItem.GameObj.transform.SetParent(asyncLoadObjectParam.ParentTransform);
        }
        gameObjectDic[gameObjectItem.GameObj.GetInstanceID()] = gameObjectItem;

        asyncLoadObjectParam.LoadedResFinishCallback(path, gameObjectItem.GameObj, asyncLoadObjectParam.TransbackData);

        loadingAssetAsyncDic.Remove(asyncLoadId);
        asyncLoadObjectParamPool.Recycle(asyncLoadObjectParam);
    }

    public void CancelLoadAssetAsync(ulong asyncLoadId)
    {
        ResourceManager.Instance.CancelLoadAssetAsync(asyncLoadId);
        if (loadingAssetAsyncDic.ContainsKey(asyncLoadId))
        {
            var asyncLoadObjectParam = loadingAssetAsyncDic[asyncLoadId];
            loadingAssetAsyncDic.Remove(asyncLoadId);
            asyncLoadObjectParamPool.Recycle(asyncLoadObjectParam);
        }
    }

    public void ReleaseGameObject(GameObject gameObject, int maxCacheCount = -1, bool recycleToParent = true)
    {
        if (gameObject == null)
            return;
        var objectInstanceID = gameObject.GetInstanceID();
        if (!gameObjectDic.TryGetValue(objectInstanceID, out GameObjectItem gameObjectItem))
        {
            Debug.Log("This GameObject dont create by ObjectManager.");
            return;
        }
        var crc = gameObjectItem.CRC;
        gameObjectDic.Remove(objectInstanceID);

        if (!gameObjectPoolDic.ContainsKey(crc))
        {
            gameObjectPoolDic[crc] = new List<GameObjectItem>();
        }

        if (maxCacheCount == 0 || (maxCacheCount > 0 && gameObjectPoolDic[crc].Count >= maxCacheCount))
        {
            GameObject.Destroy(gameObjectItem.GameObj);
            ResourceManager.Instance.ReleaseResource(gameObjectItem.ResItem);
            gameObjectItem.Reset();
            gameObjectItemPool.Recycle(gameObjectItem);
            return;
        }
#if UNITY_EDITOR
        gameObject.name += "(Recycle)";
#endif
        if (recycleToParent)
        {
            gameObjectItem.GameObj.transform.SetParent(recycleTransform);
        }
        else
        {
            gameObjectItem.GameObj.SetActive(false);
        }
        gameObjectPoolDic[crc].Add(gameObjectItem);
    }

    /// <summary>
    /// Get Cached GameObject.
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    private GameObjectItem GetUnusedGameObjectItem(uint crc)
    {
        if (gameObjectPoolDic.TryGetValue(crc, out List<GameObjectItem> gameObjectItems))
        {
            if (gameObjectItems.Count > 0)
            {
                GameObjectItem gameObjectItem = gameObjectItems[0];
                gameObjectItems.RemoveAt(0);

#if UNITY_EDITOR
                if (gameObjectItem.GameObj.name.EndsWith("(Recycle)"))
                {
                    gameObjectItem.GameObj.name = gameObjectItem.GameObj.name.Replace("(Recycle)", "");
                }
#endif
                if (gameObjectItem.OfflineData != null)
                {
                    gameObjectItem.OfflineData.ResetData();
                }
                return gameObjectItem;
            }
        }
        return null;
    }

    #region
    protected Dictionary<Type, object> objectPoolDic = new Dictionary<Type, object>();

    public ClassObjectPool<T> GetOrCreateClassPool<T>(int maxCount) where T : BasePoolObject, new()
    {
        object pool = null;
        var type = typeof(T);
        if (objectPoolDic.TryGetValue(type, out pool) && pool != null)
        {
            return pool as ClassObjectPool<T>;
        }
        pool = new ClassObjectPool<T>(maxCount);
        objectPoolDic[type] = pool;
        return pool as ClassObjectPool<T>;
    }
    #endregion

    public bool ExistAsyncLoad()
    {
        return loadingAssetAsyncDic.Count > 0;
    }

    public void PurgeAll()
    {
        int[] keys = new int[gameObjectDic.Keys.Count];
        gameObjectDic.Keys.CopyTo(keys, 0);
        for (int i = 0; i < keys.Length; i++)
        {
            ReleaseGameObject(gameObjectDic[keys[i]].GameObj, 0, false);
        }
        ClearAllCache();
        gameObjectPoolDic.Clear();
        gameObjectDic.Clear();
        loadingAssetAsyncDic.Clear();
    }

}

public class GameObjectItem : BasePoolObject
{
    public uint CRC;
    public ResourceItem ResItem;
    private GameObject gameObj;
    public GameObject GameObj
    {
        get { return gameObj; }
        set
        {
            OfflineData = null;
            gameObj = value;
            if (gameObj != null)
            {
                OfflineData = gameObj.GetComponent<OfflineData>();
            }
        }
    }
    public long GUID;
    public OfflineData OfflineData;

    public override void Reset()
    {
        CRC = 0;
        ResItem = null;
        GameObj = null;
        GUID = 0;
    }
}
