using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class RegionLoader
{
    private Dictionary<string, GameObject> _loadedRegions = new Dictionary<string, GameObject>();
    private HashSet<string> _loadingRegions = new HashSet<string>();
    private Transform _root;

    public RegionLoader(Transform root)
    {
        _root = root;

    }

    public bool IsRegionLoaded(string regionId)
    {
        _loadedRegions.ContainsKey(regionId);

        return _loadedRegions.ContainsKey(regionId);
    }

    public async UniTask LoadRegionAsync(string regionId)
    {
        if (string.IsNullOrEmpty(regionId))
        {
            Debug.LogWarning("[RegionLoader] (LoadRegionAsync) regionId가 비어 있음");

            return;
        }

        if (IsRegionLoaded(regionId) || _loadingRegions.Contains(regionId)) return;

        _loadingRegions.Add(regionId);

        if (ResourceManager.Instance == null)
        {
            Debug.LogError("[RegionLoader] ResourceManager.Instance == null");

            _loadingRegions.Remove(regionId);

            return;
        }

        GameObject prefab = await ResourceManager.Instance.LoadAsset<GameObject>(regionId);

        if (prefab == null)
        {
            Debug.LogError($"[RegionLoader] (LoadRegionAsync) 프리팹 로드 실패: {regionId}");

            _loadingRegions.Remove(regionId);

            return;
        }

        AsyncInstantiateOperation<GameObject> operation = Object.InstantiateAsync(prefab, _root);

        await operation;

        GameObject instance = operation.Result[0];

        _loadedRegions.Add(regionId, instance);

        _loadingRegions.Remove(regionId);

        Debug.Log($"[RegionLoader] 불러오기 완료: {regionId}");
    }
}
