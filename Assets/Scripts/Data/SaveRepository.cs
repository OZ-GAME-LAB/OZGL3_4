using System;
using System.IO;
using UnityEngine;

public class SaveRepository
{
    public static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public bool Save(SaveData data)
    {
        try
        {
            string tempPath = SavePath + ".tmp";
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

            if (File.Exists(SavePath))
            {
                File.Replace(tempPath, SavePath, null);
            }
            else
            {
                File.Move(tempPath, SavePath);
            }
            return true;
        }
        catch(Exception e)
        {
            Debug.LogError("[SaveRepository] 저장 실패 : " + e.Message);
            return false;
        }
    }

    public bool TryLoad(out SaveData data)
    {
        data = null;
        if (!File.Exists(SavePath))
        {
            return false;
        }
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            return data != null;
        }
        catch (Exception e)
        {
            Debug.LogError("[SaveRepository] 불러오기 실패: " + e.Message);
            return false;
        }
    }

    public void Delete()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }
    }
}