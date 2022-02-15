using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OfflineData : MonoBehaviour
{
    public Rigidbody Rigidbody;
    public Collider Collider;
    public Transform[] AllTransforms;
    public int[] AllTransformChildrenCount;
    public bool[] AllGameObjectActive;
    public Vector3[] Positions;
    public Vector3[] Scales;
    public Quaternion[] Rotations;

    public virtual void ResetData()
    {
        for (int i = 0; i < AllTransforms.Length; i++)
        {
            Transform transform = AllTransforms[i];
            if (transform == null) continue;
            transform.localPosition = Positions[i];
            transform.localScale = Scales[i];
            transform.localRotation = Rotations[i];
            if (transform.gameObject.activeSelf != AllGameObjectActive[i])
            {
                transform.gameObject.SetActive(AllGameObjectActive[i]);
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
    }

    public virtual void BindData()
    {
        Rigidbody = gameObject.GetComponentInChildren<Rigidbody>(true);
        Collider = gameObject.GetComponentInChildren<Collider>(true);
        AllTransforms = gameObject.GetComponentsInChildren<Transform>(true);
        int gameObjectCount = AllTransforms.Length;
        AllTransformChildrenCount = new int[gameObjectCount];
        AllGameObjectActive = new bool[gameObjectCount];
        Positions = new Vector3[gameObjectCount];
        Scales = new Vector3[gameObjectCount];
        Rotations = new Quaternion[gameObjectCount];
        for (int i = 0; i < gameObjectCount; i++)
        {
            Transform temp = AllTransforms[i] as Transform;
            AllTransformChildrenCount[i] = temp.childCount;
            AllGameObjectActive[i] = temp.gameObject.activeSelf;
            Positions[i] = temp.localPosition;
            Scales[i] = temp.localScale;
            Rotations[i] = temp.localRotation;
        }
    }

}
