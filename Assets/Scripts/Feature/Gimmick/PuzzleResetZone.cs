using UnityEngine;

// 플레이어가 영역 안에서 죽으면 연결된 기믹들을 시작 상태로 되돌리는 컴포넌트
// 사용법: 퍼즐이 있는 타일 아래에 빈 오브젝트를 만들어 붙이고 BoxCollider2D 크기를 퍼즐 범위에 맞춘다.
//         GameObject_ResetTargets에 되돌릴 기믹(또는 기믹들을 묶은 부모 오브젝트)을 넣는다.
// - 되돌리는 방법: 대상 오브젝트를 껐다가 다시 켠다. 기믹들은 켜질 때(OnEnable) 시작 상태로 초기화된다.
//   (이동 광원 → 레일 시작 위치, 빛 영역 → 시작 켜짐/꺼짐, 누름 발판 → 눌림 해제)
// - Transform_RestoreTargets에 넣은 오브젝트(밀 수 있는 상자, 그림자 수레 등)는 처음 위치로 되돌린다.
// - 부활 위치는 SpawnSpot이 정한다. 이 컴포넌트는 기믹 상태만 되돌린다.
[RequireComponent(typeof(BoxCollider2D))]
public class PuzzleResetZone : MonoBehaviour
{
    [Header("Reset Target")]
    [SerializeField] private GameObject[] GameObject_ResetTargets;     // 껐다 켜서 시작 상태로 되돌릴 기믹
    [SerializeField] private Transform[] Transform_RestoreTargets;     // 처음 위치로 되돌릴 오브젝트 (상자, 수레 등)

    [Header("Timing")]
    [SerializeField, Min(0f)] private float _resetDelay = 0.4f;     // 사망 후 되돌리기까지 기다리는 시간 (화면이 어두워진 뒤)

    [Header("에디터 표시")]
    [SerializeField] private Color _gizmoColor = Color.red;

    private BoxCollider2D _box;
    private Player _player;
    private bool _isWaitingReset;
    private float _resetTimer;
    private Vector3[] _restorePositions;
    private Quaternion[] _restoreRotations;

    private void Awake()
    {
        _box = GetComponent<BoxCollider2D>();
        SaveRestoreStates();
    }

    // 되돌릴 오브젝트들의 처음 위치와 회전을 기억한다
    private void SaveRestoreStates()
    {
        int count = Transform_RestoreTargets == null ? 0 : Transform_RestoreTargets.Length;
        _restorePositions = new Vector3[count];
        _restoreRotations = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            if (Transform_RestoreTargets[i] == null)
                continue;

            _restorePositions[i] = Transform_RestoreTargets[i].localPosition;
            _restoreRotations[i] = Transform_RestoreTargets[i].localRotation;
        }
    }

    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(32f, 32f);
    }

    private void OnDestroy()
    {
        UnsubscribePlayer();
    }

    // 영역에 처음 들어온 플레이어를 기억해 두고 사망 알림을 받는다
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (GimmickUtil.TryGetFromPlayerCollider(collision, out Player player) == false)
            return;

        if (_player == player)
            return;

        UnsubscribePlayer();
        _player = player;
        _player.Died += OnPlayerDied;
    }

    private void Update()
    {
        if (_isWaitingReset == false)
            return;

        _resetTimer -= Time.deltaTime;
        if (_resetTimer > 0f)
            return;

        _isWaitingReset = false;
        ResetTargets();
    }

    // 죽은 위치가 이 영역 안일 때만 되돌린다
    private void OnPlayerDied()
    {
        if (_player == null)
            return;

        if (_box.OverlapPoint(_player.transform.position) == false)
            return;

        _isWaitingReset = true;
        _resetTimer = _resetDelay;
    }

    /// <summary>
    /// 연결된 기믹들을 껐다 켜서 시작 상태로 되돌리고, 위치 복구 대상은 처음 위치로 옮긴다.
    /// </summary>
    public void ResetTargets()
    {
        RestoreTransforms();

        if (GameObject_ResetTargets == null)
            return;

        for (int i = 0; i < GameObject_ResetTargets.Length; i++)
        {
            GameObject target = GameObject_ResetTargets[i];
            if (target == null || target.activeSelf == false)
                continue;

            target.SetActive(false);
            target.SetActive(true);
        }
    }

    private void RestoreTransforms()
    {
        if (Transform_RestoreTargets == null || _restorePositions == null)
            return;

        for (int i = 0; i < Transform_RestoreTargets.Length && i < _restorePositions.Length; i++)
        {
            Transform target = Transform_RestoreTargets[i];
            if (target == null)
                continue;

            target.localPosition = _restorePositions[i];
            target.localRotation = _restoreRotations[i];

            // 물리로 움직이는 오브젝트는 남은 속도도 지운다
            Rigidbody2D targetRigidbody = target.GetComponent<Rigidbody2D>();
            if (targetRigidbody != null)
            {
                targetRigidbody.linearVelocity = Vector2.zero;
                targetRigidbody.angularVelocity = 0f;
                targetRigidbody.position = target.position;
            }
        }
    }

    private void UnsubscribePlayer()
    {
        if (_player == null)
            return;

        _player.Died -= OnPlayerDied;
        _player = null;
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            return;

        Gizmos.color = _gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
