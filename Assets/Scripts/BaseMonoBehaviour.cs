using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseMonoBehaviour : MonoBehaviour
{
    protected List<Object> loadedRes = new List<Object>();

    protected T LoadRes<T>(string path) where T : Object
    {
        T t = ResourceManager.Instance.LoadResource<T>(path);
        loadedRes.Add(t);
        return t;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < loadedRes.Count; i++)
        {
            ResourceManager.Instance.ReleaseResource(loadedRes[i]);
        }
    }

}
