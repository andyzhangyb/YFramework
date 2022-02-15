using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;

[System.Serializable]
public class AssetInfoConfig
{
    [XmlElement("AssetInfoList")]
    public List<AssetBaseInfo> AssetInfoList { get; set; }
    public AssetInfoConfig()
    {
        AssetInfoList = new List<AssetBaseInfo>();
    }
}

[System.Serializable]
public class AssetBaseInfo
{
    [XmlAttribute("Path")]
    public string Path { get; set; } = "";
    [XmlAttribute("CRC")]
    public uint CRC { get; set; } = 0;
    [XmlAttribute("BundleName")]
    public string BundleName { get; set; } = "";
    [XmlAttribute("AssetName")]
    public string AssetName { get; set; } = "";
    [XmlElement("Dependencies")]
    public List<string> Dependencies { get; set; }

    public AssetBaseInfo()
    {
        Dependencies = new List<string>();
    }
}