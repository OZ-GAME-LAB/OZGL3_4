using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    [Serializable]
    private class SerializationWrapper<T>
    {
        public List<T> items;
    }

    private readonly Dictionary<Type, object> _dataList = new Dictionary<Type, object>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private bool TryLoadJsonData<T>(out Dictionary<string, T> result) where T : GameDataBase
    {
        result = new Dictionary<string, T>();
        string dataName = typeof(T).Name;

        string resourcePath = $"JsonOutput/{dataName}";
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);

        if (textAsset == null && dataName.EndsWith("Data", StringComparison.OrdinalIgnoreCase))
        {
            resourcePath = $"JsonOutput/{dataName.Substring(0, dataName.Length - 4)}";
            textAsset = Resources.Load<TextAsset>(resourcePath);
        }

        if (textAsset == null)
        {
            Debug.LogError($"[GameDataManager] 리소스 없음: Resources/JsonOutput/{dataName}");
            return false;
        }

        try
        {
            var wrapper = JsonUtility.FromJson<SerializationWrapper<T>>("{\"items\":" + textAsset.text + "}");

            if (wrapper?.items == null)
            {
                Debug.LogError($"[GameDataManager]: [{dataName}] 파싱 결과가 비어있습니다.");
                return false;
            }

            foreach (T item in wrapper.items)
            {
                if (item == null) continue;

                string key = item.Id;
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogError($"[GameDataManager]: [{dataName}] Id가 비어있는 항목을 건너뜁니다.");
                    continue;
                }
                if (result.ContainsKey(key))
                {
                    Debug.LogError($"[GameDataManger]: [{dataName}] Id 중복 : {key}");
                    continue;
                }
                result.Add(key, item);
            }
            Debug.Log($"[GameDataManager]: {dataName} 데이터를 {result.Count}개 로드했습니다.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameDataManager]: [{dataName} JSON 로드 오류]");
            Debug.LogException(ex);
            return false;
        }
        finally
        {
            Resources.UnloadAsset(textAsset);
        }
    }

    private readonly Dictionary<Type, int> _failCount = new Dictionary<Type, int>();
    private const int MaxRetry = 3;

    private Dictionary<string, T> GetOrLoadDataTable<T>() where T : GameDataBase
    {
        Type dataType = typeof(T);

        if (_dataList.TryGetValue(dataType, out object dictionaryObject))
        {
            if (dictionaryObject is Dictionary<string, T> cached)
            {
                return cached;
            }
            Debug.LogError($"[GameDataManager]: {dataType.Name} 캐시 타입 불일치");
            _dataList.Remove(dataType);
        }

        _failCount.TryGetValue(dataType, out int fails);
        if (fails >= MaxRetry)
        {
            return new Dictionary<string, T>();
        }

        if (TryLoadJsonData(out Dictionary<string, T> loaded))
        {
            _dataList[dataType] = loaded;
            _failCount.Remove(dataType);
        }
        else
        {
            _failCount[dataType] = fails + 1;
        }
        return loaded;

    }

    public T GetData<T>(string id) where T : GameDataBase
    {
        Dictionary<string, T> dict = GetOrLoadDataTable<T>();

        return dict.TryGetValue(id, out T data) ? data : null;
    }

    public List<string> GetAllDataId<T>() where T : GameDataBase
    {
        Dictionary<string, T> dict = GetOrLoadDataTable<T>();
        return dict.Keys.ToList();
    }

    public List<T> GetAllData<T>() where T : GameDataBase
    {
        Dictionary<string, T> dict = GetOrLoadDataTable<T>();
        return dict.Values.ToList();
    }
}
