using Cysharp.Threading.Tasks;
using UnityEngine;

public class ShadowCrowSpot : MonoBehaviour
{
    [Header("까마귀")]
    [SerializeField] private string _crowAddress = "ShadowCrow";    

    [Header("대사")]
    [SerializeField] private string _dialogId;                      
    [SerializeField] private bool _isAutoTalk = true;               

    [Header("에디터 표시")]
    [SerializeField] private Color _gizmoColor = new Color(0.4f, 0.2f, 0.8f, 0.8f);

    private ShadowCrow _crow;
    private bool _isAutoTalked;
    private int _spawnVersion;

    private void Start()
    {
        if (GetComponentInParent<MapChunk>() == null)
        {
            Debug.LogWarning($"[ShadowCrowSpot] {name}: MapChunk 아래에 있지 않아 청크 활성화를 따르지 않습니다.", this);
        }
    }

    private void OnEnable()
    {
        _spawnVersion++;
        SpawnCrowAsync(_spawnVersion).Forget();
    }

    private void OnDisable()
    {
        _spawnVersion++;
        ReturnCrow();
    }

    private async UniTaskVoid SpawnCrowAsync(int spawnVersion)
    {
        if (GameObjectManager.Instance == null)
        {
            Debug.LogError($"[ShadowCrowSpot] {name}: GameObjectManager가 없어 까마귀를 생성할 수 없습니다.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(_dialogId))
        {
            Debug.LogError($"[ShadowCrowSpot] {name}: 대사 ID(_dialogId)를 입력하세요.", this);
            return;
        }

        GameObject crowObject = await GameObjectManager.Instance.CreateObjectAsync(_crowAddress, transform.position);
        if (crowObject == null)
        {
            Debug.LogError($"[ShadowCrowSpot] {name}: 까마귀 생성 실패. Addressables 주소를 확인하세요: {_crowAddress}", this);
            return;
        }

        if (this == null || spawnVersion != _spawnVersion)
        {
            GameObjectManager.Instance.RequestDestroyObject(crowObject);
            return;
        }

        ShadowCrow crow = crowObject.GetComponent<ShadowCrow>();
        if (crow == null)
        {
            Debug.LogError($"[ShadowCrowSpot] {name}: 생성한 오브젝트에 ShadowCrow가 없습니다: {_crowAddress}", this);
            GameObjectManager.Instance.RequestDestroyObject(crowObject);
            return;
        }

        _crow = crow;
        _crow.SetSpawnData(_dialogId, _isAutoTalk && _isAutoTalked == false);
        _crow.OnStartAutoTalk -= OnStartCrowAutoTalk;
        _crow.OnStartAutoTalk += OnStartCrowAutoTalk;
    }

    private void ReturnCrow()
    {
        if (_crow == null)
        {
            return;
        }

        _crow.OnStartAutoTalk -= OnStartCrowAutoTalk;

        if (GameObjectManager.Instance != null)
        {
            GameObjectManager.Instance.RequestDestroyObject(_crow.gameObject);
        }
        _crow = null;
    }

    private void OnStartCrowAutoTalk()
    {
        _isAutoTalked = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 3.7f);
    }
}
