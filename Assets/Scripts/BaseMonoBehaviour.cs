using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BaseMonoBehaviour : MonoBehaviour
{
    private List<GameObject> cacheAutoReleaseObj = new List<GameObject>();

    protected Dictionary<string, Object> loadedRes = new Dictionary<string, Object>();

    protected T LoadRes<T>(string path) where T : Object
    {
        if (loadedRes.ContainsKey(path))
        {
            return loadedRes[path] as T;
        }
        T t = ResourceManager.Instance.LoadResource<T>(path);
        if (t != null)
        {
            loadedRes[path] = t;
        }
        return t;
    }

    protected virtual void Awake()
    {
        ResetByTheme();
    }

    public virtual void OnDestroy()
    {
        foreach (var item in loadedRes)
        {
            ResourceManager.Instance.ReleaseResource(item.Value);
        }
        MessageManager.Instance.Unregister(gameObject);
#if UNITY_EDITOR
        if (ResourceManager.Instance.LoadFormAssetBundleForEditor)
        {
#else
        if (true) {
#endif
            for (int i = 0; i < cacheAutoReleaseObj.Count; i++)
            {
                ObjectManager.Instance.ReleaseGameObject(cacheAutoReleaseObj[i], 0);
            }
            cacheAutoReleaseObj.Clear();
        }
    }

    public void SetAutoReleasePrefab()
    {
        cacheAutoReleaseObj.Add(gameObject);
    }

    protected virtual void ResetByTheme()
    {
    }

    public T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T t = go.GetComponent<T>();
        if (t == null)
        {
            t = go.AddComponent<T>();
        }
        return t;
    }

    public void OnClick(GameObject go, System.Action<PointerEventData> cb)
    {
        GetOrAddComponent<TouchEventListener>(go).OnPointerClickCallback = cb;
    }

    public void OnClickDown(GameObject go, System.Action<PointerEventData> cb)
    {
        TouchEventListener tel = GetOrAddComponent<TouchEventListener>(go);
        tel.OnClickDownCallback = cb;
    }

    public void OnDrag(GameObject go, System.Action<PointerEventData> cb)
    {
        TouchEventListener tel = GetOrAddComponent<TouchEventListener>(go);
        tel.OnDragCallback = cb;
    }

    public void OnClickUp(GameObject go, System.Action<PointerEventData> cb)
    {
        TouchEventListener tel = GetOrAddComponent<TouchEventListener>(go);
        tel.OnClickUpCallback = cb;
    }

    public void OnClickExit(GameObject go, System.Action<PointerEventData> cb)
    {
        TouchEventListener tel = GetOrAddComponent<TouchEventListener>(go);
        tel.OnClickExitCallback = cb;
    }

}
