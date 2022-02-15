using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class MainScene : MonoBehaviour
{
    public AudioSource Audio;
    public Transform RecycleTransform;
    public Transform ParentTransform;
    private AudioClip audioClip;
    private GameObject rock;
    private GameObject veg;
    private ulong loadId;

    private void Awake()
    {
        //ILRuntimeManager.Instance.Init();
        ResourceManager.Instance.SetMonoBehaviour(this);
        AssetBundleManager.Instance.BuildAssetInfo();
        ObjectManager.Instance.Init(RecycleTransform);
    }

    private void OnApplicationQuit()
    {
        ResourceManager.Instance.RemoveUnusedResource();
    }

    void Start()
    {
        {
            //ObjectManager.Instance.PreloadGameObject("Assets/Prefabs/Rock.prefab", 5, true);
        }
        {
            //ResourceManager.Instance.LoadResourceAsync("Assets/Sounds/audio.mp3", new System.Action<string, Object, object>((string path, Object obj, object data) =>
            //{
            //    audioClip = obj as AudioClip;
            //    Audio.clip = audioClip;
            //    Audio.Play();
            //}), true, AsyncLoadPriority.Hight);
        }
        {
            //audioClip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/Sounds/audio.mp3");
            //Audio.clip = audioClip;
            //Audio.Play();
        }
        {
            //ResourceManager.Instance.PreLoadResource("Assets/Sounds/audio.mp3");
        }
        {
            //rock = ObjectManager.Instance.InstantiateObject("Assets/Prefabs/Rock.prefab", ParentTransform);
        }
        {
            loadId = ObjectManager.Instance.InstantiateObjectAsync("Assets/Prefabs/VegetationLarge01.prefab", new System.Action<string, GameObject, object>((string path, GameObject obj, object data) =>
             {
                 veg = obj;
             }), ParentTransform);
            //Invoke("CancelLoad", (float)1.0 / (float)60.0);
        }
    }

    public void CancelLoad()
    {
        ObjectManager.Instance.CancelLoadAssetAsync(loadId);
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ObjectManager.Instance.InstantiateObjectAsync("Assets/Prefabs/Rock.prefab", new System.Action<string, GameObject, object>((string path, GameObject obj, object data) =>
            {
                veg = obj;
            }), ParentTransform);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            //audioClip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/Sounds/audio.mp3");
            //Audio.clip = audioClip;
            //Audio.Play();
            veg = ObjectManager.Instance.InstantiateObject("Assets/Prefabs/Rock.prefab", ParentTransform);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            //Audio.Stop();
            //Audio.clip = null;
            //ResourceManager.Instance.ReleaseResource(audioClip);
            if (veg == null) return;
            ObjectManager.Instance.ReleaseGameObject(veg);
            veg = null;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (veg == null) return;
            ObjectManager.Instance.ReleaseGameObject(veg, 0);
            Destroy(veg);
            veg = null;
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            ObjectManager.Instance.ClearAllCache();
            ResourceManager.Instance.ClearCache();
        }
    }


    void LoadTest()
    {
        //AssetBundle assetInfoBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/assetbundleinfo");
        //var assetBundleInfoText = assetInfoBundle.LoadAsset<TextAsset>("assetbundleinfo");
        //MemoryStream memoryStream = new MemoryStream(assetBundleInfoText.bytes);
        //BinaryFormatter binaryFormatter = new BinaryFormatter();
        //AssetInfoConfig assetInfoConfig = binaryFormatter.Deserialize(memoryStream) as AssetInfoConfig;
        //memoryStream.Close();

        //string path = "Assets/Prefabs/Rock.prefab";
        //uint crc = CRC32.GetCRC3232(path);
        //AssetBaseInfo assetBaseInfo = null;
        //for (int i = 0; i < assetInfoConfig.AssetInfoList.Count; i++)
        //{
        //    if (crc == assetInfoConfig.AssetInfoList[i].CRC)
        //    {
        //        assetBaseInfo = assetInfoConfig.AssetInfoList[i];
        //        break;
        //    }
        //}
        //foreach (var item in assetBaseInfo.Dependencies)
        //{
        //    AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + item);
        //}
        //AssetBundle assetBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + assetBaseInfo.BundleName);
        //var rock = assetBundle.LoadAsset<GameObject>(assetBaseInfo.AssetName);
        //GameObject gameObject = Instantiate(rock);
    }
}
