using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    private Dictionary<string, System.Type> uiNameToScript = new Dictionary<string, System.Type>();
    private Dictionary<string, string> prefabFilePath = new Dictionary<string, string>();
    private List<UIBase> currentOpenUI = new List<UIBase>();

    public void RegisterUI<T>(string name, string filePath) where T : UIBase
    {
        uiNameToScript[name] = typeof(T);
        prefabFilePath[name] = filePath;
    }

    public UIBaseMonoBehaviour OpenUI(string name, Transform parentTransform)
    {
        if (!uiNameToScript.TryGetValue(name, out System.Type type))
        {
            return null;
        }
        if (!prefabFilePath.TryGetValue(name, out string filePath))
        {
            return null;
        }
        var result = ObjectManager.Instance.InstantiateObject(filePath, parentTransform, true);
        var mono = result.AddComponent<UIBaseMonoBehaviour>();
        mono.UIScript = ILRuntimeManager.Instance.AppDomain.Instantiate<UIBase>("HotFixLibrary.UIScript." + name);
        return mono;
    }

    public void CloseUI(UIBase uIBase)
    {
        for (int i = 0; i < currentOpenUI.Count; i++)
        {
            if (currentOpenUI[i] == uIBase)
            {
                currentOpenUI.RemoveAt(i);
                break;
            }
        }
    }

    public void ClearCache()
    {
        uiNameToScript.Clear();
        prefabFilePath.Clear();
        currentOpenUI.Clear();
    }

}
