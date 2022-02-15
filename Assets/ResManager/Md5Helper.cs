using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

public class Md5Helper
{
    public static string CalcuFileMd5(string filePath)
    {
        string md5Str = "";
        try
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                var md5 = MD5.Create();
                md5Str = FormatMd5(md5.ComputeHash(fileStream));
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
        return md5Str;
    }

    public static string FormatMd5(Byte[] data)
    {
        return System.BitConverter.ToString(data).Replace("-", "").ToLower();
    }
}
