using System.Collections.Generic;
using UnityEngine;

public class Region : MonoBehaviour
{
    [Header("지역 정보")]
    [SerializeField] private string _regionId;

    [Header("인접 지역")]
    [SerializeField] private List<string> _neighborRegionIds = new List<string>();


    public string RegionId => _regionId;
    public IReadOnlyList<string> NeighborRegionIds => _neighborRegionIds;


    private void Start()
    {
        if (string.IsNullOrEmpty(_regionId))
        {
            Debug.LogWarning($"[Region] (Start) {name}: RegionId가 비어 있음");
        }
    }


}
