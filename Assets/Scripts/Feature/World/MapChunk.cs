using UnityEngine;

public class MapChunk : MonoBehaviour
{
    [SerializeField] private Transform Transform_Object;


    public Room Room { get; private set; }
    public Region Region { get; private set; }


    private void Start()
    {
        Room = GetComponentInParent<Room>();
        Region = GetComponentInParent<Region>();

        if (MapManager.Instance == null)
        {
            Debug.LogError($"[MapChunk] (Start) {name} 등록 실패: MapManager 없음");

            return;
        }

        if (Region == null)
        {
            Debug.LogWarning($"[MapChunk] (Start) {name}: 소속 Region 없음");
        }

        MapManager.Instance.RegisterChunk(this);
    }

    private void OnDestroy()
    {
        if (MapManager.Instance == null) return;

        MapManager.Instance.UnregisterChunk(this);
    }



    public void SetObjectActive(bool isActive)
    {
        if (Transform_Object == null)
        {
            Debug.LogError($"[MapChunk] (SetObjectActive) {name} Transform_Object == null, return.");

            return;
        }

        Transform_Object.gameObject.SetActive(isActive);
    }
}
