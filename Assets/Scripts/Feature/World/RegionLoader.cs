using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class RegionLoader
{
    private Dictionary<string, GameObject> _loadedRegions = new Dictionary<string, GameObject>();
    private HashSet<string> _loadingRegions = new HashSet<string>();
    private Transform _root;
    private HashSet<string> _pendingUnloads = new HashSet<string>();


    public RegionLoader(Transform root)
    {
        _root = root;

    }

    public bool IsRegionLoaded(string regionId)
    {
        return _loadedRegions.ContainsKey(regionId);
    }

    public async UniTask LoadRegionAsync(string regionId)
    {
        if (string.IsNullOrEmpty(regionId))
        {
            Debug.LogWarning("[RegionLoader] (LoadRegionAsync) regionId가 비어 있음");

            return;
        }


        _pendingUnloads.Remove(regionId);


        if (IsRegionLoaded(regionId))
        {
            SetRegionVisible(regionId, true);

            return;
        }

        if (_loadingRegions.Contains(regionId)) return;


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

        _loadingRegions.Remove(regionId);


        if (_pendingUnloads.Remove(regionId))
        {
            Object.Destroy(instance);

            ReleaseAsset(regionId);

            Debug.Log($"[RegionLoader] 예약된 내리기 처리: {regionId}");

            return;
        }


        _loadedRegions.Add(regionId, instance);

        Debug.Log($"[RegionLoader] 불러오기 완료: {regionId}");
    }

    public void UnloadRegion(string regionId)
    {
        if (string.IsNullOrEmpty(regionId))
        {
            Debug.LogWarning("[RegionLoader] (UnloadRegion): regionId가 비어있습니다.");

            return;
        }

        if (_loadingRegions.Contains(regionId))
        {
            _pendingUnloads.Add(regionId);

            Debug.Log($"[RegionLoader] (UnloadRegion) 불러오는 중이라 내리기 예약: {regionId}");

            return;
        }

        if (!_loadedRegions.TryGetValue(regionId, out GameObject instance)) return;

        _loadedRegions.Remove(regionId);

        if (instance != null)
        {
            Object.Destroy(instance);
        }

        ReleaseAsset(regionId);

        Debug.Log($"[RegionLoader] 내리기 완료: {regionId}");
    }

    public void SetRegionVisible(string regionId, bool isVisible)
    {
        if (!_loadedRegions.TryGetValue((regionId), out GameObject instance) || instance == null)
        {
            Debug.LogWarning($"[RegionLoader] (SetRegionVisible) 불러오지 않은 지역: {regionId}");

            return;
        }

        if (instance.activeSelf == isVisible) return;

        instance.SetActive(isVisible);

        Debug.Log($"[RegionLoader] (SetRegionVisible) {regionId}: {(isVisible ? "보이기" : "숨기기")}");
    }

    public bool IsRegionVisible(string regionId)
    {
        if (!_loadedRegions.TryGetValue(regionId, out GameObject instance) || instance == null)
        {
            return false;
        }

        return instance.activeSelf;
    }


    private void ReleaseAsset(string regionId)
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogWarning($"[RegionLoader] (ReleaseAsset) ResourceManager가 없어 에셋 해제 실패: {regionId}");
            return;
        }

        ResourceManager.Instance.Release(regionId);
    }

    public bool IsRegionLoading(string regionId) //EnterRegionAsync의 타이밍 조절용
    {
        return _loadingRegions.Contains(regionId);
    }

    public bool HasLoadingRegion()
    {
        return _loadingRegions.Count > 0;
    }

    public Region GetRegion(string regionId)
    {
        if (!_loadedRegions.TryGetValue(regionId, out GameObject instance))
        {
            return null;
        }

        if (instance == null)
        {
            return null;
        }

        return instance.GetComponent<Region>();
    }

    public void CollectActiveRegionIds(List<string> result) // 올라와있거나, 올라올 지역의 목록을 얻고, 내릴 지역을 고르기 위한 용도
    {
        result.Clear();
        foreach (string regionId in _loadedRegions.Keys)
        {
            result.Add(regionId);
        }

        foreach (string regionId in _loadingRegions)
        {
            result.Add(regionId);
        }
    }
}
