using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmWindow : MonoBehaviour
{
    public Text TxtTitle;
    public Button BtnCancel;
    public Button BtnSure;

    private Action<object> sureCallback;
    private Action<object> cancelCallback;

    public static void PopDialog(RectTransform parentTransform, string strTitle, Action<object> sureCallback, Action<object> cancelCallback)
    {
        var result = ObjectManager.Instance.InstantiateObject("Assets/Prefabs/ConfirmWindow.prefab", parentTransform, true).GetComponent<ConfirmWindow>();
        result.Show(strTitle, sureCallback, cancelCallback);
    }

    public void Show(string strTitle, Action<object> sureCallback, Action<object> cancelCallback)
    {
        this.sureCallback = sureCallback;
        this.cancelCallback = cancelCallback;

        gameObject.SetActive(true);
    }

    public void OnClickSure()
    {
        sureCallback(this);
        gameObject.SetActive(false);
    }

    public void OnClickCancel()
    {
        cancelCallback(this);
        gameObject.SetActive(false);
    }

}
