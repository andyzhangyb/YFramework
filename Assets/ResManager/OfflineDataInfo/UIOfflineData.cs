using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIOfflineData : OfflineData
{
    public Vector2[] AnchorMax;
    public Vector2[] AnchorMin;
    public Vector2[] Pivot;
    public Vector2[] SizeDelta;
    public Vector3[] AnchoredPosition;
    public ParticleSystem[] Particle;

    public override void ResetData()
    {
        int gameObjectCount = AllTransforms.Length;
        RectTransform rectTransform = null;
        for (int i = 0; i < gameObjectCount; i++)
        {
            if (AllTransforms[i] == null) continue;
            rectTransform = AllTransforms[i] as RectTransform;
            rectTransform.localPosition = Positions[i];
            rectTransform.localScale = Scales[i];
            rectTransform.localRotation = Rotations[i];

            rectTransform.anchorMax = AnchorMax[i];
            rectTransform.anchorMin = AnchorMin[i];
            rectTransform.pivot = Pivot[i];
            rectTransform.sizeDelta = SizeDelta[i];
            rectTransform.anchoredPosition = AnchoredPosition[i];

            if (rectTransform.gameObject.activeSelf != AllGameObjectActive[i])
            {
                rectTransform.gameObject.SetActive(AllGameObjectActive[i]);
            }
            if (transform.childCount <= AllTransformChildrenCount[i]) continue;
            for (int j = transform.childCount - 1; j >= AllTransformChildrenCount[i]; j--)
            {
                GameObject gameObject = transform.GetChild(j).gameObject;
                if (!ObjectManager.Instance.ManageByObjectManager(gameObject))
                {
                    GameObject.Destroy(gameObject);
                }
            }
        }
        for (int i = 0; i < Particle.Length; i++)
        {
            Particle[i].Clear(true);
            Particle[i].Play();
        }
    }

    public override void BindData()
    {
        var allTransforms = gameObject.GetComponentsInChildren<Transform>(true);
        int gameObjectCount = allTransforms.Length;
        for (int i = 0; i < gameObjectCount; i++)
        {
            if (!(allTransforms[i] is RectTransform))
            {
                allTransforms[i].gameObject.AddComponent<RectTransform>();
            }
        }
        AllTransforms = gameObject.GetComponentsInChildren<RectTransform>(true);
        Particle = gameObject.GetComponentsInChildren<ParticleSystem>(true);
        gameObjectCount = AllTransforms.Length;
        AllTransformChildrenCount = new int[gameObjectCount];
        AllGameObjectActive = new bool[gameObjectCount];
        Positions = new Vector3[gameObjectCount];
        Scales = new Vector3[gameObjectCount];
        Rotations = new Quaternion[gameObjectCount];

        AnchorMax = new Vector2[gameObjectCount];
        AnchorMin = new Vector2[gameObjectCount];
        Pivot = new Vector2[gameObjectCount];
        SizeDelta = new Vector2[gameObjectCount];
        AnchoredPosition = new Vector3[gameObjectCount];

        for (int i = 0; i < gameObjectCount; i++)
        {
            RectTransform rectTransform = AllTransforms[i] as RectTransform;
            AllTransformChildrenCount[i] = rectTransform.childCount;
            AllGameObjectActive[i] = rectTransform.gameObject.activeSelf;
            Positions[i] = rectTransform.localPosition;
            Scales[i] = rectTransform.localScale;
            Rotations[i] = rectTransform.localRotation;

            AnchorMax[i] = rectTransform.anchorMax;
            AnchorMin[i] = rectTransform.anchorMin;
            Pivot[i] = rectTransform.pivot;
            SizeDelta[i] = rectTransform.sizeDelta;
            AnchoredPosition[i] = rectTransform.anchoredPosition;
        }
    }

}
