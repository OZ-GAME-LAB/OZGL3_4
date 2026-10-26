using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 문 기믹입니다. 신호를 받으면 문(Body)이 정해진 방향으로 미끄러지며 열리고 닫힙니다.
///
/// 배치 방법
/// Gimmick_Door              ← Door 컴포넌트, 스케일 (1, 1, 1)
/// └─ Body                   ← SpriteRenderer, BoxCollider2D(Is Trigger 해제), Rigidbody2D(Kinematic)
///
/// - Body를 놓은 위치가 "닫힌 위치"입니다. 열린 위치는 닫힌 위치 + _openOffset 입니다.
///   (오브젝트를 선택하면 씬 뷰에 열린 위치가 하늘색 상자로 표시됩니다.)
/// - 스위치에 연결할 때는 스위치의 시작 상태와 문의 시작 상태(_isStartOpen)를 맞춰 주세요.
///   예) 스위치 Is Start On 켜짐 + Is Open When Signal On 켜짐 → 문 Is Start Open 켜짐
/// </summary>
public class Door : MonoBehaviour, ISignalReceiver
{
    [Header("Reference")]
    [SerializeField] private Transform Transform_Body;
    [SerializeField] private Rigidbody2D Rigidbody2D_Body;

    [Header("Movement")]
    [SerializeField] private Vector2 _openOffset = new Vector2(0f, 2f);
    [SerializeField] private float _moveDuration = 0.5f;

    [Header("State")]
    [SerializeField] private bool _isStartOpen = false;
    [SerializeField] private bool _isOpenWhenSignalOn = true;

    private Vector3 _closedLocalPosition;
    private CancellationTokenSource _moveCancellationSource;

    public bool IsOpen { get; private set; }

    private void Reset()
    {
        if (transform.childCount > 0)
        {
            Transform_Body = transform.GetChild(0);
        }

        Rigidbody2D_Body = GetComponentInChildren<Rigidbody2D>();
    }

    private void Awake()
    {
        if (Transform_Body != null)
        {
            _closedLocalPosition = Transform_Body.localPosition;
        }
    }

    private void OnEnable()
    {
        // 풀에서 다시 꺼낼 때도 시작 상태로 즉시 돌아갑니다.
        StopMove();
        IsOpen = _isStartOpen;
        SnapBody(GetTargetLocalPosition(IsOpen));
    }

    private void OnDisable()
    {
        StopMove();
    }

    /// <summary>
    /// 신호를 받아 문을 열거나 닫습니다.
    /// _isOpenWhenSignalOn이 꺼져 있으면 반대로 동작합니다. (신호 켜짐 → 닫힘)
    /// </summary>
    public void ReceiveSignal(bool isOn)
    {
        bool isOpen = isOn == _isOpenWhenSignalOn;
        ChangeState(isOpen);
    }

    /// <summary>
    /// 문을 엽니다.
    /// </summary>
    public void Open()
    {
        ChangeState(true);
    }

    /// <summary>
    /// 문을 닫습니다.
    /// </summary>
    public void Close()
    {
        ChangeState(false);
    }

    /// <summary>
    /// 열려 있으면 닫고, 닫혀 있으면 엽니다.
    /// </summary>
    public void ToggleDoor()
    {
        ChangeState(!IsOpen);
    }

    private void ChangeState(bool isOpen)
    {
        if (Transform_Body == null)
            return;

        // 이미 같은 상태면 움직이지 않습니다. (시작 시 스위치가 같은 상태를 보내도 문이 움직이지 않음)
        if (IsOpen == isOpen)
            return;

        IsOpen = isOpen;

        StopMove();
        _moveCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        MoveAsync(GetTargetLocalPosition(isOpen), _moveCancellationSource.Token).Forget();
    }

    private void StopMove()
    {
        if (_moveCancellationSource == null)
            return;

        _moveCancellationSource.Cancel();
        _moveCancellationSource.Dispose();
        _moveCancellationSource = null;
    }

    private async UniTaskVoid MoveAsync(Vector3 targetLocalPosition, CancellationToken cancellationToken)
    {
        // 움직이는 도중 방향이 바뀌어도 자연스럽도록 현재 위치에서 출발하고, 남은 거리만큼만 시간을 씁니다.
        Vector3 startLocalPosition = Transform_Body.localPosition;
        float fullDistance = _openOffset.magnitude;
        float remainingDistance = Vector3.Distance(startLocalPosition, targetLocalPosition);
        float duration = fullDistance > 0f ? _moveDuration * (remainingDistance / fullDistance) : 0f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.fixedDeltaTime;
            MoveBody(Vector3.Lerp(startLocalPosition, targetLocalPosition, elapsedTime / duration));

            bool isCanceled = await UniTask.Yield(PlayerLoopTiming.FixedUpdate, cancellationToken).SuppressCancellationThrow();
            if (isCanceled)
                return;
        }

        MoveBody(targetLocalPosition);
    }

    private Vector3 GetTargetLocalPosition(bool isOpen)
    {
        if (isOpen)
        {
            return _closedLocalPosition + (Vector3)_openOffset;
        }

        return _closedLocalPosition;
    }

    /// <summary>
    /// 물리 이동으로 Body를 옮깁니다. (움직이는 동안 사용)
    /// </summary>
    private void MoveBody(Vector3 localPosition)
    {
        if (Rigidbody2D_Body != null)
        {
            Rigidbody2D_Body.MovePosition(transform.TransformPoint(localPosition));
            return;
        }

        Transform_Body.localPosition = localPosition;
    }

    /// <summary>
    /// Body를 즉시 옮깁니다. (시작 상태를 맞출 때 사용)
    /// </summary>
    private void SnapBody(Vector3 localPosition)
    {
        if (Transform_Body == null)
            return;

        Transform_Body.localPosition = localPosition;

        if (Rigidbody2D_Body != null)
        {
            Rigidbody2D_Body.position = Transform_Body.position;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Transform_Body == null)
            return;

        // 플레이 전에는 지금 Body 위치가 닫힌 위치입니다.
        Vector3 closedLocalPosition = Application.isPlaying ? _closedLocalPosition : Transform_Body.localPosition;
        Vector3 closedWorldPosition = transform.TransformPoint(closedLocalPosition);
        Vector3 openWorldPosition = transform.TransformPoint(closedLocalPosition + (Vector3)_openOffset);

        Vector3 bodySize = Vector3.one * 0.5f;
        BoxCollider2D bodyCollider = Transform_Body.GetComponent<BoxCollider2D>();
        if (bodyCollider != null)
        {
            bodySize = Vector3.Scale(bodyCollider.size, Transform_Body.lossyScale);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(openWorldPosition, bodySize);
        Gizmos.DrawLine(closedWorldPosition, openWorldPosition);
    }

    private void OnValidate()
    {
        if (Transform_Body == null)
            return;

        BoxCollider2D bodyCollider = Transform_Body.GetComponent<BoxCollider2D>();
        if (bodyCollider != null && bodyCollider.isTrigger)
        {
            Debug.LogWarning($"[{name}] 문(Body)의 BoxCollider2D는 길을 막아야 하므로 Is Trigger를 해제해 주세요.", this);
        }

        if (Rigidbody2D_Body != null && Rigidbody2D_Body.bodyType != RigidbodyType2D.Kinematic)
        {
            Debug.LogWarning($"[{name}] 문(Body)의 Rigidbody2D Body Type을 Kinematic으로 설정해 주세요.", this);
        }
    }
}
