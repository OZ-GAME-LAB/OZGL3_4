using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 누름 발판 기믹입니다. 플레이어나 밀 수 있는 오브젝트(PushableObject)가 올라가 있는 동안 신호를 보냅니다.
///
/// 구조
/// Gimmick_PressurePlate        ← PressurePlate, BoxCollider2D(Is Trigger, 발판 위 감지 범위)
/// └─ Visual                    ← 발판 그림 (눌리면 살짝 내려감)
///
/// - 올라가면 신호 켜짐, 모두 내려가면 신호 꺼짐입니다.
/// - _isLatched를 켜면 한 번 눌린 뒤에는 내려가도 계속 켜져 있습니다.
/// - 시작할 때는 실제로 눌려 있는지에 따라 연결된 기믹 상태를 맞춥니다. (Is Start On 값은 사용하지 않음)
/// </summary>
public class PressurePlate : SignalSender
{
    private const string PlayerTag = "Player";

    [Header("Reference")]
    [SerializeField] private Transform Transform_Visual;

    [Header("Press")]
    [SerializeField] private bool _isPressedByPlayer = true;
    [SerializeField] private bool _isGrabbable = true;
    [SerializeField] private bool _isLatched = false;
    [SerializeField] private float _pressDepth = 0.1f;

    private readonly List<Collider2D> _pressingColliders = new List<Collider2D>();
    private Vector3 _visualDefaultPosition;
    private bool _isLatchedOn;

    public bool IsPressed
    {
        get { return _pressingColliders.Count > 0 || _isLatchedOn; }
    }

    private void Reset()
    {
        if (transform.childCount > 0)
        {
            Transform_Visual = transform.GetChild(0);
        }
    }

    private void Awake()
    {
        if (Transform_Visual != null)
        {
            _visualDefaultPosition = Transform_Visual.localPosition;
        }
    }

    protected override void OnEnable()
    {
        // 풀에서 다시 꺼낼 때를 위해 상태를 초기화합니다.
        _pressingColliders.Clear();
        _isLatchedOn = false;
        RefreshVisual();

        base.OnEnable();
    }

    protected override bool GetStartState()
    {
        return IsPressed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (CanPress(other) == false || _pressingColliders.Contains(other))
            return;

        _pressingColliders.Add(other);
        RefreshState();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_pressingColliders.Remove(other))
        {
            RefreshState();
        }
    }

    private bool CanPress(Collider2D other)
    {
        if (_isPressedByPlayer && other.CompareTag(PlayerTag))
            return true;

        if (_isGrabbable && other.GetComponentInParent<Grabbable>() != null)
            return true;

        return false;
    }

    private void RefreshState()
    {
        // 사라진 오브젝트는 목록에서 정리합니다.
        for (int i = _pressingColliders.Count - 1; i >= 0; i--)
        {
            if (_pressingColliders[i] == null || _pressingColliders[i].enabled == false)
            {
                _pressingColliders.RemoveAt(i);
            }
        }

        if (_isLatched && _pressingColliders.Count > 0)
        {
            _isLatchedOn = true;
        }

        RefreshVisual();

        if (IsPressed != IsOn)
        {
            SendSignal(IsPressed);
        }
    }

    private void RefreshVisual()
    {
        if (Transform_Visual == null)
            return;

        float depth = IsPressed ? _pressDepth : 0f;
        Transform_Visual.localPosition = _visualDefaultPosition + Vector3.down * depth;
    }
}
