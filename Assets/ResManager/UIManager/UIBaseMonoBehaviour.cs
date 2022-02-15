using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBaseMonoBehaviour : MonoBehaviour
{
    private UIBase uIBase = null;
    public UIBase UIScript
    {
        get { return uIBase; }
        set
        {
            uIBase = value;
            uIBase.SetGameObject(gameObject);
            uIBase.Awake();
        }
    }

    void Start()
    {
        if (uIBase != null)
        {
            uIBase.Start();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (uIBase != null)
        {
            uIBase.Update();
        }
    }

    private void OnDestroy()
    {
        if (uIBase != null)
        {
            UIManager.Instance.CloseUI(uIBase);
        }
    }

}
