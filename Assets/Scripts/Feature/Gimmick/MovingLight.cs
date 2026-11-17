using UnityEngine;

/// <summary>
/// 이동 광원 기믹입니다. 램프(Carriage)가 레일을 따라 움직이고, 그 아래에 붙은 빛 영역이 함께 이동합니다.
/// 플레이어가 잡는 범위 안에서 상호작용 키를 누르면 램프를 잡고, 다시 누르거나 범위를 벗어나면 놓습니다.
///
/// 구조
/// Gimmick_MovingLight          ← MovingLight, 스케일 (1, 1, 1)
/// └─ Carriage                  ← 레일 위를 움직이는 부분
///     ├─ Visual                ← 램프 그림
///     ├─ GrabArea              ← MovingLightHandle, InteractionPrompt, BoxCollider2D(Is Trigger, 잡는 범위), 레이어 Default
///     │   └─ Prompt            ← 말주머니 (InteractionPrompt의 GameObject_Prompt에 연결)
///     └─ Gimmick_LightZone     ← 빛 영역 프리팹 (함께 이동)
///
/// - 플레이어의 PlayerInteractor는 트리거 콜라이더와 같은 오브젝트에서 IInteractable을 찾으므로,
///   잡는 범위(GrabArea)에는 이 컴포넌트로 전달해 주는 MovingLightHandle을 붙입니다.
/// - 레일은 오브젝트를 선택하면 씬 뷰에 노란 선으로 표시됩니다.
/// - 잡는 범위(GrabArea)는 빛이 비추지 않는 쪽에 두세요. (잡는 순간 빛에 닿지 않도록)
/// - 신호를 받으면 켜짐 → 레일 끝(End), 꺼짐 → 레일 시작(Start)으로 스스로 이동합니다.
/// - Move Delay를 주면 신호를 받은 뒤 그 시간만큼 기다렸다가 움직이기 시작합니다. (플레이어가 잡고 끌 때는 적용되지 않음)
/// </summary>
public class MovingLight : MonoBehaviour, ISignalReceiver, IToggleReceiver
{
    [Header("Reference")]
    [SerializeField] private Transform Transform_Carriage;
    [SerializeField] private Collider2D Collider2D_GrabArea;

    [Header("Rail (부모 기준 좌표)")]
    [SerializeField] private Vector2 _railStart = new Vector2(-3f, 0f);
    [SerializeField] private Vector2 _railEnd = new Vector2(3f, 0f);
    [SerializeField, Range(0f, 1f)] private float _startRatio = 0f;
    [SerializeField] private float _moveSpeed = 3f;

    [Header("Signal")]
    [Tooltip("신호를 받은 뒤 움직이기 시작할 때까지 기다리는 시간(초)입니다. 0이면 바로 움직입니다.")]
    [SerializeField, Min(0f)] private float _moveDelay = 0f;

    [Header("Grab")]
    [SerializeField] private bool _isGrabbable = true;

    private Transform _grabbingPlayer;
    private float _grabOffsetRatio;
    private float _currentRatio;
    private float _targetRatio;
    private float _delayTimer;

    /// <summary>
    /// 신호를 받고 움직이기 전 유예 시간을 기다리는 중인지 여부입니다.
    /// </summary>
    public bool IsWaitingToMove
    {
        get { return _delayTimer > 0f; }
    }

    /// <summary>
    /// 레일 위 현재 위치입니다. 0 = 시작, 1 = 끝
    /// </summary>
    public float CurrentRatio
    {
        get { return _currentRatio; }
    }

    public bool IsGrabbed
    {
        get { return _grabbingPlayer != null; }
    }

    private void Reset()
    {
        if (transform.childCount > 0)
        {
            Transform_Carriage = transform.GetChild(0);
        }

        MovingLightHandle handle = GetComponentInChildren<MovingLightHandle>();
        if (handle != null)
        {
            Collider2D_GrabArea = handle.GetComponent<Collider2D>();
        }
    }

    private void OnEnable()
    {
        // 풀에서 다시 꺼낼 때도 시작 위치로 돌아갑니다.
        _grabbingPlayer = null;
        _currentRatio = _startRatio;
        _targetRatio = _startRatio;
        _delayTimer = 0f;
        ApplyCarriagePosition();
    }

    private void Update()
    {
        if (Transform_Carriage == null)
            return;

        if (IsGrabbed)
        {
            // 잡은 채로 잡는 범위를 벗어나면 놓습니다.
            if (IsPlayerInGrabArea(_grabbingPlayer) == false)
            {
                ReleaseGrab();
            }
            else
            {
                // 잡은 순간의 간격을 유지하면서 플레이어를 따라갑니다.
                _targetRatio = Mathf.Clamp01(GetRatioOnRail(_grabbingPlayer.position) + _grabOffsetRatio);
            }
        }

        // 신호를 받은 직후에는 유예 시간이 끝날 때까지 움직이지 않습니다.
        if (_delayTimer > 0f)
        {
            _delayTimer -= Time.deltaTime;
            return;
        }

        MoveCarriage();
    }

    /// <summary>
    /// 상호작용 키를 눌렀을 때 호출됩니다. 잡고 있지 않으면 잡고, 잡고 있으면 놓습니다.
    /// </summary>
    public void ToggleGrab(Player player)
    {
        if (IsGrabbed)
        {
            ReleaseGrab();
            return;
        }

        if (_isGrabbable == false || player == null)
            return;

        // 잡으면 기다리던 신호 이동은 취소하고 플레이어를 바로 따라갑니다.
        _delayTimer = 0f;
        _targetRatio = _currentRatio;

        _grabbingPlayer = player.transform;
        _grabOffsetRatio = _currentRatio - GetRatioOnRail(_grabbingPlayer.position);
    }

    /// <summary>
    /// 신호 켜짐이면 레일 끝으로, 꺼짐이면 레일 시작으로 이동합니다.
    /// </summary>
    public void ReceiveSignal(bool isOn)
    {
        ReleaseGrab();
        SetSignalTarget(isOn ? 1f : 0f);
    }

    /// <summary>
    /// 토글 스위치가 눌릴 때마다 호출됩니다. 목표가 레일 시작이면 끝으로, 끝이면 시작으로 바꿉니다.
    /// </summary>
    public void ToggleSignal()
    {
        ReleaseGrab();
        SetSignalTarget(_targetRatio < 0.5f ? 1f : 0f);
    }

    /// <summary>
    /// 레일 위 지정한 위치(0 = 시작, 1 = 끝)로 이동합니다.
    /// </summary>
    public void MoveToRatio(float ratio)
    {
        ReleaseGrab();
        SetSignalTarget(Mathf.Clamp01(ratio));
    }

    /// <summary>
    /// 신호로 정해진 목표 위치를 적용합니다. 실제로 움직여야 할 때만 유예 시간(_moveDelay)을 시작합니다.
    /// </summary>
    private void SetSignalTarget(float targetRatio)
    {
        bool isTargetChanged = Mathf.Approximately(_targetRatio, targetRatio) == false;
        _targetRatio = targetRatio;

        if (isTargetChanged == false)
            return;

        // 이미 목표 위치에 있으면 기다릴 필요가 없습니다.
        if (Mathf.Approximately(_currentRatio, _targetRatio))
        {
            _delayTimer = 0f;
            return;
        }

        _delayTimer = _moveDelay;
    }

    private void ReleaseGrab()
    {
        if (IsGrabbed == false)
            return;

        _grabbingPlayer = null;

        // 놓은 자리에서 멈춥니다.
        _targetRatio = _currentRatio;
    }

    private bool IsPlayerInGrabArea(Transform player)
    {
        if (player == null)
            return false;

        // 잡는 범위 콜라이더가 없으면 범위 검사를 하지 않습니다.
        if (Collider2D_GrabArea == null)
            return true;

        return Collider2D_GrabArea.OverlapPoint(player.position);
    }

    private void MoveCarriage()
    {
        float railLength = Vector2.Distance(_railStart, _railEnd);
        if (railLength <= 0f)
            return;

        float ratioSpeed = _moveSpeed / railLength;
        _currentRatio = Mathf.MoveTowards(_currentRatio, _targetRatio, ratioSpeed * Time.deltaTime);
        ApplyCarriagePosition();
    }

    private void ApplyCarriagePosition()
    {
        if (Transform_Carriage == null)
            return;

        Vector2 railPosition = Vector2.Lerp(_railStart, _railEnd, _currentRatio);
        Transform_Carriage.localPosition = new Vector3(railPosition.x, railPosition.y, Transform_Carriage.localPosition.z);
    }

    /// <summary>
    /// 월드 위치를 레일 위 비율로 바꿉니다. (범위를 벗어나면 0보다 작거나 1보다 클 수 있음)
    /// </summary>
    private float GetRatioOnRail(Vector3 worldPosition)
    {
        Vector2 localPosition = transform.InverseTransformPoint(worldPosition);
        Vector2 railDirection = _railEnd - _railStart;
        float railLengthSquared = railDirection.sqrMagnitude;
        if (railLengthSquared <= 0f)
            return 0f;

        return Vector2.Dot(localPosition - _railStart, railDirection) / railLengthSquared;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 railStartWorld = transform.TransformPoint(_railStart);
        Vector3 railEndWorld = transform.TransformPoint(_railEnd);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(railStartWorld, railEndWorld);
        Gizmos.DrawWireSphere(railStartWorld, 0.15f);
        Gizmos.DrawWireSphere(railEndWorld, 0.15f);
    }
}
