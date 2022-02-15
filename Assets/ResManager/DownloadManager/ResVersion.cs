using System;
using System.Collections.Generic;
using System.Xml.Serialization;

[Serializable]
public class ResVersion
{
    [XmlAttribute("ResVersionCode")]
    public ulong ResVersionCode { get; set; } = 0;
    [XmlElement("FileList")]
    public List<ResFileInfo> FileList { get; set; } = new List<ResFileInfo>();
}

[Serializable]
public class ResFileInfo
{
    [XmlAttribute("Name")]
    public string Name { get; set; }
    [XmlAttribute("FilePath")]
    public string FilePath { get; set; }
    [XmlAttribute("Md5")]
    public string Md5 { get; set; }
    [XmlAttribute("Size")]
    public float Size { get; set; }
}
