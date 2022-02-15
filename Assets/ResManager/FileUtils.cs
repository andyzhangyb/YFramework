using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class FileUtils : Singleton<FileUtils>
{
    private List<string> searchPaths = new List<string>();
    private Dictionary<string, string> fullPathCache = new Dictionary<string, string>();

    public List<string> SearchPaths
    {
        get
        {
            return searchPaths;
        }
    }

    public FileUtils()
    {
    }

    public string UpdateDirectory()
    {
        string updateDir = PlayerPrefs.GetString("UpdateDir", "WorkingA");
        updateDir = Path.Combine(Application.persistentDataPath, updateDir);
        if (!Directory.Exists(updateDir))
        {
            Directory.CreateDirectory(updateDir);
            PlayerPrefs.SetString("UpdateDir", "WorkingA");
        }
        return updateDir + "/";
    }

    public void AddSearchPath(string pathStr)
    {
        searchPaths.Add(pathStr);
    }

    public void ResetSearchPath()
    {
        searchPaths.Clear();
    }

    public void PurgeCachedEntries()
    {
        fullPathCache.Clear();
    }

    /// <summary>
    /// Cant guarantee file is exist.
    /// </summary>
    /// <param name="fileName">file name</param>
    /// <returns>file path</returns>
    public string FullPathForFilename(string fileName)
    {
        string fullPath = string.Empty;
        if (fullPathCache.TryGetValue(fileName, out fullPath))
        {
            return fullPath;
        }
        for (int i = searchPaths.Count - 1; i >= 0; i--)
        {
            string tempPath = searchPaths[i] + fileName;
            if (File.Exists(tempPath))
            {
                fullPath = tempPath;
                break;
            }
        }
        if (string.IsNullOrEmpty(fullPath))
        {
            fullPath = Application.streamingAssetsPath + "/" + fileName;
        }
        fullPathCache[fileName] = fullPath;
        return fullPath;
    }

    public void Copy(string srcPath, string targetPath)
    {
        try
        {
            if (!Directory.Exists(srcPath))
            {
                Directory.CreateDirectory(srcPath);
            }
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }
            targetPath += Path.DirectorySeparatorChar;
            var allFiles = Directory.GetFileSystemEntries(srcPath);
            foreach (var item in allFiles)
            {
                if (File.Exists(item))
                {
                    File.Copy(item, targetPath + Path.GetFileName(item), true);
                }
                else
                {
                    Copy(item, targetPath);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }


}
