using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchEventListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerExitHandler
{
    public Action<PointerEventData> OnClickDownCallback;
    public Action<PointerEventData> OnStartDragCallback;
    public Action<PointerEventData> OnDragCallback;
    public Action<PointerEventData> OnEndDragCallback;
    public Action<PointerEventData> OnClickUpCallback;
    public Action<PointerEventData> OnPointerClickCallback;
    public Action<PointerEventData> OnClickExitCallback;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (OnClickDownCallback != null)
        {
            OnClickDownCallback(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (OnDragCallback != null)
        {
            OnDragCallback(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (OnClickUpCallback != null)
        {
            OnClickUpCallback(eventData);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (OnPointerClickCallback != null)
        {
            OnPointerClickCallback(eventData);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (OnClickExitCallback != null)
        {
            OnClickExitCallback(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (OnEndDragCallback != null)
        {
            OnEndDragCallback(eventData);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (OnStartDragCallback != null)
        {
            OnStartDragCallback(eventData);
        }
    }
}
