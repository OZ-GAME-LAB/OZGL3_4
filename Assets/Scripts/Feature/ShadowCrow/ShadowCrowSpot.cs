using Cysharp.Threading.Tasks;
using UnityEngine;

public class ShadowCrowSpot : MonoBehaviour, ISaveableGimmick
{
    [Header("까마귀")]
    [SerializeField] private string _crowAddress = "ShadowCrow";    

    [Header("대사")]
    [SerializeField] private string _dialogId;                      
    [SerializeField] private bool _isAutoTalk = true;               

    [Header("등장 연출")]
    [SerializeField] private string _appearAnimationName = "CrowAppear";   // 까마귀 Animator 상태 이름. 비석은 CrowSummon
    [SerializeField] private Animator Animator_Feather;                    // 비석 깃털. 없으면 생략
    [SerializeField] private Transform Transform_SpawnPoint;               // 까마귀 발 위치(비석 위). 없으면 이 오브젝트 위치

    [Header("에디터 표시")]
    [SerializeField] private Color _gizmoColor = new Color(0.4f, 0.2f, 0.8f, 0.8f);

    private const float _crowHeight = 1f;   // 까마귀 키(유닛). Gizmo 표시용
    private readonly int _featherSummonHash = Animator.StringToHash("FeatherSummon");

    private ShadowCrow _crow;
    private bool _isAutoTalked;
    private int _spawnVersion;

    private void Awake()
    {
        if (_isAutoTalk)
        {
            GimmickSaveRegistry.Register(this, this);
        }
    }

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

    private void OnDestroy()
    {
        GimmickSaveRegistry.Unregister(this);
    }

    void ISaveableGimmick.CaptureState(GimmickSaveState state)
    {
        state.IsUsed = _isAutoTalked;
    }

    void ISaveableGimmick.RestoreState(GimmickSaveState state)
    {
        _isAutoTalked = state.IsUsed;
        if (_crow == null)
        {
            return;
        }

        _crow.SetSpawnData(_dialogId, _isAutoTalk && !_isAutoTalked);
        if (_isAutoTalked && _crow.CurrentStateType == ShadowCrowState.Appear)
        {
            _crow.EndTalkDirecting();
        }
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

        GameObject crowObject = await GameObjectManager.Instance.CreateObjectAsync(_crowAddress, GetSpawnPosition());
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
        // 풀에서 다른 지점의 까마귀를 재사용해도 이 지점의 등장 애니메이션으로 덮어쓴다.
        _crow.SetAppearAnimation(_appearAnimationName);
        _crow.OnStartAutoTalk -= OnStartCrowAutoTalk;
        _crow.OnStartAutoTalk += OnStartCrowAutoTalk;
        _crow.OnStartAppear -= OnStartCrowAppear;
        _crow.OnStartAppear += OnStartCrowAppear;
    }

    private void ReturnCrow()
    {
        if (_crow == null)
        {
            return;
        }

        _crow.OnStartAutoTalk -= OnStartCrowAutoTalk;
        _crow.OnStartAppear -= OnStartCrowAppear;

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

    // 까마귀가 등장을 시작한 순간 깃털이 부서지는 연출을 함께 시작한다.
    // CrowSummon 클립 앞부분이 비어 있어서 까마귀는 깃털 Summon 5번 프레임쯤 보이기 시작한다.
    private void OnStartCrowAppear()
    {
        if (Animator_Feather == null)
        {
            return;
        }

        Animator_Feather.Play(_featherSummonHash, 0, 0f);
    }

    private Vector3 GetSpawnPosition()
    {
        if (Transform_SpawnPoint == null)
        {
            return transform.position;
        }

        return Transform_SpawnPoint.position;
    }

    private void OnDrawGizmos()
    {
        Vector3 spawnPosition = GetSpawnPosition();
        Gizmos.color = _gizmoColor;
        Gizmos.DrawWireSphere(spawnPosition, 0.5f);
        Gizmos.DrawLine(spawnPosition, spawnPosition + Vector3.up * _crowHeight);
    }
}
