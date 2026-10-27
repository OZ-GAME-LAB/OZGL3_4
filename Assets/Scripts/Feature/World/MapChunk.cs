using UnityEngine;

public class MapChunk : MonoBehaviour
{
    [SerializeField] private Transform Transform_Object;


    public Room Room { get; private set; }
    public Region Region { get; private set; }


    private bool _isStarted = false;


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

        _isStarted = true;
    }

    private void OnEnable()
    {
        if (!_isStarted) return;

        if (MapManager.Instance == null)
        {
            Debug.LogWarning($"[MapChunk] (OnEnable) {name} 재등록 실패: MapManager 없음");

            return;
        }

        MapManager.Instance.RegisterChunk(this);
    }

    private void OnDisable()
    {
        if (!_isStarted) return;

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
