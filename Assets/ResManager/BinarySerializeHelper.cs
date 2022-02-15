using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;

public class BinarySerializeHelper
{
    public static void SerializeToXml(string path, System.Object obj)
    {
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                {
                    XmlSerializer xs = new XmlSerializer(obj.GetType());
                    xs.Serialize(sw, obj);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("Cant serialize object to xml. {0} {1}", path, e.ToString()));
        }
    }


    public static void SerilizeToBinary(string path, System.Object obj)
    {
        try
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, obj);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("Serilize to binary failed. {0} {1}", path, e.ToString()));
        }
    }

    public static T BinaryDeserilizeFromDisk<T>(string path) where T : class
    {
        T t = null;
        try
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open))
            {
                BinaryFormatter bf = new BinaryFormatter();
                t = (T)bf.Deserialize(fileStream);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("Deserilize failed. {0} {1}", path, e.ToString()));
        }
        return t;
    }

    public static T BinaryDeserilize<T>(byte[] bytes) where T : class
    {
        T t = null;
        try
        {
            using (MemoryStream memoryStream = new MemoryStream(bytes))
            {
                BinaryFormatter bf = new BinaryFormatter();
                t = (T)bf.Deserialize(memoryStream);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("Deserilize failed. {0}", e.ToString()));
        }
        return t;
    }

    public static T BinaryDeserilize<T>(string path) where T : class
    {
        TextAsset textAsset = ResourceManager.Instance.LoadResource<TextAsset>(path);
        if (textAsset == null)
        {
            Debug.LogError(string.Format("Load TextAsset failed. {0}", path));
            return null;
        }

        T t = null;
        try
        {
            using (MemoryStream stream = new MemoryStream(textAsset.bytes))
            {
                BinaryFormatter bf = new BinaryFormatter();
                t = (T)bf.Deserialize(stream);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("Load TextAsset failed. {0} {1}", path, e.ToString()));
        }
        ResourceManager.Instance.ReleaseResource(textAsset);
        return t;
    }

}
