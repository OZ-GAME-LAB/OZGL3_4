using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 이동 광원 기믹입니다. 램프(Carriage)가 레일을 따라 움직이고, 그 아래에 붙은 빛 영역이 함께 이동합니다.
/// 플레이어가 잡는 범위 안에서 E 키를 누르고 있으면 램프를 잡고 밀거나 당길 수 있습니다.
///
/// 구조
/// Gimmick_MovingLight          ← MovingLight, 스케일 (1, 1, 1)
/// └─ Carriage                  ← 레일 위를 움직이는 부분
///     ├─ Visual                ← 램프 그림
///     ├─ Prompt                ← 'E' 말주머니 (선택)
///     └─ Gimmick_LightZone     ← 빛 영역 프리팹 (함께 이동)
///
/// - 레일과 잡는 범위는 오브젝트를 선택하면 씬 뷰에 표시됩니다. (노랑: 레일, 초록: 잡는 범위)
/// - 플레이어가 잡는 위치가 빛 안에 들어가지 않도록 _grabAreaOffset으로 잡는 범위를 빛 반대쪽에 두세요.
/// - 신호를 받으면 켜짐 → 레일 끝(End), 꺼짐 → 레일 시작(Start)으로 스스로 이동합니다.
/// - 현재는 임시로 E 키를 직접 감지합니다. 플레이어 상호작용 시스템이 완성되면 교체합니다.
/// </summary>
public class MovingLight : MonoBehaviour, ISignalReceiver
{
    private const string PlayerTag = "Player";

    [Header("Reference")]
    [SerializeField] private Transform Transform_Carriage;
    [SerializeField] private GameObject GameObject_Prompt;

    [Header("Rail (부모 기준 좌표)")]
    [SerializeField] private Vector2 _railStart = new Vector2(-3f, 0f);
    [SerializeField] private Vector2 _railEnd = new Vector2(3f, 0f);
    [SerializeField, Range(0f, 1f)] private float _startRatio = 0f;
    [SerializeField] private float _moveSpeed = 3f;

    [Header("Grab")]
    [SerializeField] private bool _isGrabbable = true;
    [SerializeField] private Key _grabKey = Key.E;
    [SerializeField] private Vector2 _grabAreaSize = new Vector2(1.5f, 2f);
    [SerializeField] private Vector2 _grabAreaOffset = Vector2.zero;

    private readonly List<Collider2D> _overlapResults = new List<Collider2D>();
    private ContactFilter2D _contactFilter;
    private Transform _grabbingPlayer;
    private float _grabOffsetRatio;
    private float _currentRatio;
    private float _targetRatio;

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
    }

    private void Awake()
    {
        _contactFilter = new ContactFilter2D();
        _contactFilter.NoFilter();
    }

    private void OnEnable()
    {
        // 풀에서 다시 꺼낼 때도 시작 위치로 돌아갑니다.
        _grabbingPlayer = null;
        _currentRatio = _startRatio;
        _targetRatio = _startRatio;
        ApplyCarriagePosition();
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (Transform_Carriage == null)
            return;

        Transform playerInArea = FindPlayerInGrabArea();
        UpdateGrab(playerInArea);
        SetPromptVisible(_isGrabbable && playerInArea != null && IsGrabbed == false);
        MoveCarriage();
    }

    /// <summary>
    /// 신호 켜짐이면 레일 끝으로, 꺼짐이면 레일 시작으로 이동합니다.
    /// </summary>
    public void ReceiveSignal(bool isOn)
    {
        ReleaseGrab();
        _targetRatio = isOn ? 1f : 0f;
    }

    /// <summary>
    /// 레일 위 지정한 위치(0 = 시작, 1 = 끝)로 이동합니다.
    /// </summary>
    public void MoveToRatio(float ratio)
    {
        ReleaseGrab();
        _targetRatio = Mathf.Clamp01(ratio);
    }

    private void UpdateGrab(Transform playerInArea)
    {
        if (_isGrabbable == false || Keyboard.current == null)
        {
            ReleaseGrab();
            return;
        }

        bool isKeyHeld = Keyboard.current[_grabKey].isPressed;

        if (IsGrabbed)
        {
            // 키를 떼거나 범위를 벗어나면 놓습니다.
            if (isKeyHeld == false || playerInArea == null)
            {
                ReleaseGrab();
                return;
            }

            // 잡은 순간의 간격을 유지하면서 플레이어를 따라갑니다.
            _targetRatio = Mathf.Clamp01(GetRatioOnRail(_grabbingPlayer.position) + _grabOffsetRatio);
            return;
        }

        if (playerInArea != null && Keyboard.current[_grabKey].wasPressedThisFrame)
        {
            _grabbingPlayer = playerInArea;
            _grabOffsetRatio = _currentRatio - GetRatioOnRail(playerInArea.position);
        }
    }

    private void ReleaseGrab()
    {
        if (IsGrabbed == false)
            return;

        _grabbingPlayer = null;

        // 놓은 자리에서 멈춥니다.
        _targetRatio = _currentRatio;
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

    private Transform FindPlayerInGrabArea()
    {
        Vector2 areaCenter = (Vector2)Transform_Carriage.position + _grabAreaOffset;
        int count = Physics2D.OverlapBox(areaCenter, _grabAreaSize, 0f, _contactFilter, _overlapResults);

        for (int i = 0; i < count; i++)
        {
            if (_overlapResults[i].CompareTag(PlayerTag))
            {
                return _overlapResults[i].transform;
            }
        }

        return null;
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (GameObject_Prompt != null && GameObject_Prompt.activeSelf != isVisible)
        {
            GameObject_Prompt.SetActive(isVisible);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 railStartWorld = transform.TransformPoint(_railStart);
        Vector3 railEndWorld = transform.TransformPoint(_railEnd);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(railStartWorld, railEndWorld);
        Gizmos.DrawWireSphere(railStartWorld, 0.15f);
        Gizmos.DrawWireSphere(railEndWorld, 0.15f);

        if (Transform_Carriage == null)
            return;

        Gizmos.color = Color.green;
        Vector3 areaCenter = Transform_Carriage.position + (Vector3)_grabAreaOffset;
        Gizmos.DrawWireCube(areaCenter, _grabAreaSize);
    }
}
