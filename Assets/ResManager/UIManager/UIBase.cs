using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBase
{
    protected GameObject gameObject { get; set; }
    public virtual void SetGameObject(GameObject gameObject)
    {
        this.gameObject = gameObject;
    }
    public virtual void Awake()
    {

    }

    public virtual void Start()
    {

    }

    public virtual void Update()
    {

    }
}
