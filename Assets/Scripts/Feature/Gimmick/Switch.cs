using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 스위치 기믹입니다. 플레이어가 상호작용 키를 누르면 연결된 기믹에 신호를 보냅니다.
///
/// 구조
/// Gimmick_Switch               ← Switch, InteractionPrompt, BoxCollider2D(Is Trigger, 상호작용 범위)
/// ├─ Visual                    ← 버튼 그림 (SpriteRenderer_Switch에 연결)
/// └─ Prompt                    ← 말주머니 (InteractionPrompt의 GameObject_Prompt에 연결)
///
/// - List_Receiver에 빛 영역, 문 등 신호를 받을 기믹을 넣습니다.
/// - 플레이어가 트리거 범위 안에서 상호작용 키를 누르면 PlayerInteractor가 Interact(player)를 호출합니다.
///   (IInteractable은 트리거 콜라이더와 같은 오브젝트에 있어야 합니다)
///
/// 외형 표현
/// - Sprite_On, Sprite_Off를 둘 다 넣으면 상태에 따라 그림이 바뀌고, 비워 두면 색으로 표시합니다.
/// - 누를 때마다 Visual이 살짝 내려갔다 올라오는 눌림 연출이 재생됩니다.
/// </summary>
public class Switch : SignalSender, IInteractable
{
    public enum SwitchMode
    {
        Toggle, // 누를 때마다 켜짐/꺼짐 전환
        Once,   // 한 번만 작동하고 고정
    }

    [Header("Reference")]
    [SerializeField] private SpriteRenderer SpriteRenderer_Switch;

    [Header("Behaviour")]
    [SerializeField] private SwitchMode _switchMode = SwitchMode.Toggle;

    [Header("State View")]
    [SerializeField] private Sprite Sprite_On;
    [SerializeField] private Sprite Sprite_Off;
    [SerializeField] private Color _onColor = Color.yellow;
    [SerializeField] private Color _offColor = Color.gray;

    [Header("Press Animation")]
    [SerializeField] private float _pressDepth = 0.1f;
    [SerializeField] private float _pressDuration = 0.06f;

    private bool _isUsed;
    private Vector3 _visualDefaultPosition;
    private CancellationTokenSource _pressCancellationSource;

    private void Reset()
    {
        SpriteRenderer_Switch = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (SpriteRenderer_Switch != null)
        {
            _visualDefaultPosition = SpriteRenderer_Switch.transform.localPosition;
        }
    }

    protected override void OnEnable()
    {
        // 부모의 OnEnable(연결된 기믹 시작 상태 맞추기)을 먼저 실행합니다.
        base.OnEnable();

        // 풀에서 다시 꺼낼 때를 위해 상태를 초기화합니다.
        _isUsed = false;
    }

    private void OnDisable()
    {
        StopPressAnimation();
    }

    public Vector2 Position
    {
        get { return transform.position; }
    }

    /// <summary>
    /// 플레이어가 상호작용 키를 눌렀을 때 PlayerInteractor가 호출합니다.
    /// </summary>
    public void Interact(Player player)
    {
        Interact();
    }

    /// <summary>
    /// 스위치를 작동시킵니다. 다른 스크립트에서 직접 호출해도 됩니다.
    /// Toggle 모드는 누를 때마다 상태가 바뀌고, Once 모드는 한 번만 바뀝니다.
    /// </summary>
    public void Interact()
    {
        if (_switchMode == SwitchMode.Once)
        {
            if (_isUsed)
                return;

            _isUsed = true;
        }

        PlayPressAnimation();
        SendSignal(!IsOn);
    }

    protected override void HandleSignalSent(bool isOn)
    {
        if (SpriteRenderer_Switch == null)
            return;

        // 켜짐/꺼짐 그림이 둘 다 있으면 그림을 바꾸고, 없으면 색으로 표시합니다.
        if (Sprite_On != null && Sprite_Off != null)
        {
            SpriteRenderer_Switch.sprite = isOn ? Sprite_On : Sprite_Off;
        }
        else
        {
            SpriteRenderer_Switch.color = isOn ? _onColor : _offColor;
        }
    }

    private void PlayPressAnimation()
    {
        if (SpriteRenderer_Switch == null)
            return;

        // 연타했을 때 이전 연출을 멈추고 처음부터 다시 재생합니다.
        StopPressAnimation();

        _pressCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        PressAsync(_pressCancellationSource.Token).Forget();
    }

    private void StopPressAnimation()
    {
        if (_pressCancellationSource != null)
        {
            _pressCancellationSource.Cancel();
            _pressCancellationSource.Dispose();
            _pressCancellationSource = null;
        }

        if (SpriteRenderer_Switch != null)
        {
            SpriteRenderer_Switch.transform.localPosition = _visualDefaultPosition;
        }
    }

    private async UniTaskVoid PressAsync(CancellationToken cancellationToken)
    {
        Transform visualTransform = SpriteRenderer_Switch.transform;
        Vector3 pressedPosition = _visualDefaultPosition + Vector3.down * _pressDepth;

        bool isCanceled = await MoveVisualAsync(visualTransform, _visualDefaultPosition, pressedPosition, cancellationToken);
        if (isCanceled)
            return;

        await MoveVisualAsync(visualTransform, pressedPosition, _visualDefaultPosition, cancellationToken);
    }

    /// <summary>
    /// Visual을 시작 위치에서 목표 위치로 _pressDuration 동안 이동시킵니다. 중간에 취소되면 true를 돌려줍니다.
    /// </summary>
    private async UniTask<bool> MoveVisualAsync(Transform target, Vector3 from, Vector3 to, CancellationToken cancellationToken)
    {
        float elapsedTime = 0f;

        while (elapsedTime < _pressDuration)
        {
            elapsedTime += Time.deltaTime;
            target.localPosition = Vector3.Lerp(from, to, elapsedTime / _pressDuration);

            bool isCanceled = await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken).SuppressCancellationThrow();
            if (isCanceled)
                return true;
        }

        target.localPosition = to;
        return false;
    }
}
