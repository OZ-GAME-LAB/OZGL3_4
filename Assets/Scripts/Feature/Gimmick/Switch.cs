using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 스위치 기믹입니다. 플레이어가 상호작용 키를 누르면 연결된 기믹에 신호를 보냅니다.
///
/// 구조
/// Gimmick_Switch               ← Switch, InteractionPrompt, BoxCollider2D(Is Trigger, 상호작용 범위)
/// ├─ Visual                    ← 버튼 그림 (SpriteRenderer_Switch에 연결)
/// └─ Prompt                    ← 말주머니 (InteractionPrompt의 GameObject_Prompt에 연결)
///
/// - List_Receiver에 빛 영역, 문 등 신호를 받을 기믹을 넣습니다.
/// - Is Toggle Receiver 를 켜면 누를 때마다 연결된 문이 반전됩니다. (한 문에 여러 스위치를 물리는 조합 퍼즐용)
///   이 경우 문은 스위치 상태와 무관하게 자기 Is Start Open 으로 시작하고, 첫 클릭부터 항상 반응합니다.
/// - 플레이어가 트리거 범위 안에서 상호작용 키를 누르면 PlayerInteractor가 Interact(player)를 호출합니다.
///   (IInteractable은 트리거 콜라이더와 같은 오브젝트에 있어야 합니다)
///
/// 외형 표현
/// - Sprite_On, Sprite_Off를 둘 다 넣으면 상태에 따라 그림이 바뀌고, 비워 두면 색으로 표시합니다.
/// - 누를 때마다 Visual이 살짝 내려갔다 올라오는 눌림 연출이 재생됩니다.
/// - Is Important 를 켜면(보상 스위치 등) 켜져 있는 동안 버튼 그림과 조명이 강조 색(파란색)으로 바뀝니다.
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
    [SerializeField] private Animator _anim;
    private int _isOnHash = Animator.StringToHash("IsOn"); // 애니메이션 관련 변수 추가 - 유민형

    [Header("Behaviour")]
    [SerializeField] private SwitchMode _switchMode = SwitchMode.Toggle;
    [Tooltip("켜면 누를 때마다 연결된 문을 '반전'시킵니다. (켜짐/꺼짐을 써 넣지 않음)\n여러 스위치가 같은 문에 물린 조합 퍼즐용. 문은 자기 Is Start Open 으로 시작합니다.")]
    [SerializeField] private bool _isToggleReceiver = false;

    [Header("State View")]
    [SerializeField] private Sprite Sprite_On;
    [SerializeField] private Sprite Sprite_Off;
    [SerializeField] private Color _onColor = Color.yellow;
    [SerializeField] private Color _offColor = Color.gray;

    [Header("Important (보상 스위치 등 강조)")]
    [SerializeField] private Light2D Light2D_Switch;                                        // 스위치를 비추는 조명
    [SerializeField] private bool _isImportant = false;                                      // 켜면 눌린 동안 그림과 조명 색이 바뀜
    [SerializeField] private Color _importantOnVisualColor = new Color(0.25f, 0.6f, 1f, 1f); // 눌렸을 때 버튼 그림 색
    [SerializeField] private Color _importantOnColor = new Color(0.25f, 0.6f, 1f, 1f);       // 눌렸을 때 조명 색
    [SerializeField, Min(0f)] private float _importantOnIntensity = 8f;                      // 눌렸을 때 조명 세기

    [Header("Press Animation")]
    [SerializeField] private float _pressDepth = 0.1f;
    [SerializeField] private float _pressDuration = 0.06f;

    private bool _isUsed;
    private Vector3 _visualDefaultPosition;
    private Color _visualDefaultColor = Color.white;
    private Color _lightDefaultColor;
    private float _lightDefaultIntensity;

    /// <summary>
    /// 토글 방식이면 시작할 때 문 상태를 덮어쓰지 않습니다. (문은 자기 시작 상태를 유지)
    /// </summary>
    protected override bool IsSyncReceiversOnEnable
    {
        get { return _isToggleReceiver == false; }
    }
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
            _visualDefaultColor = SpriteRenderer_Switch.color;
        }

        if (Light2D_Switch != null)
        {
            _lightDefaultColor = Light2D_Switch.color;
            _lightDefaultIntensity = Light2D_Switch.intensity;
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

        if (_isToggleReceiver)
        {
            // 토글 방식: 연결된 문마다 지금 상태의 반대로. 스위치 자체의 켜짐/꺼짐 표시도 같이 바뀝니다.
            ToggleReceivers();
            return;
        }

        SendSignal(!IsOn);
    }

    protected override void HandleSignalSent(bool isOn)
    {
        RefreshImportantLight(isOn);

        if (SpriteRenderer_Switch == null)
            return;

        // 켜짐/꺼짐 그림이 둘 다 있으면 그림을 바꾸고, 없으면 색으로 표시합니다.
        if (Sprite_On != null && Sprite_Off != null)
        {
            SpriteRenderer_Switch.sprite = isOn ? Sprite_On : Sprite_Off;

            // 중요한 스위치는 그림을 바꾸는 방식이어도 켜진 동안 강조 색을 입힙니다.
            if (_isImportant)
            {
                SpriteRenderer_Switch.color = isOn ? _importantOnVisualColor : _visualDefaultColor;
            }
        }
        else
        {
            // 중요한 스위치는 켜졌을 때 노란색 대신 강조 색(파란색)으로 표시합니다.
            Color onColor = _isImportant ? _importantOnVisualColor : _onColor;
            SpriteRenderer_Switch.color = isOn ? onColor : _offColor;
        }
    }

    // 중요한 스위치는 켜져 있는 동안 조명을 강조 색으로 바꿔서 눌렸다는 것을 알려 줍니다.
    private void RefreshImportantLight(bool isOn)
    {
        if (_isImportant == false || Light2D_Switch == null)
            return;

        Light2D_Switch.color = isOn ? _importantOnColor : _lightDefaultColor;
        Light2D_Switch.intensity = isOn ? _importantOnIntensity : _lightDefaultIntensity;
    }

    private void PlayPressAnimation()
    {
        // 버튼 On/Off 애니메이션 실행 - 유민형
        if (_anim == null)
            return;

        _anim.SetBool(_isOnHash, !IsOn);

        //if (SpriteRenderer_Switch == null)
        //    return;

        //// 연타했을 때 이전 연출을 멈추고 처음부터 다시 재생합니다.
        //StopPressAnimation();

        //_pressCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        //PressAsync(_pressCancellationSource.Token).Forget();
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

    // 세이브: 한 번만 눌리는 스위치는 사용 여부도 함께 저장합니다.
    public override void CaptureState(GimmickSaveState state)
    {
        base.CaptureState(state);
        state.IsUsed = _isUsed;
    }

    public override void RestoreState(GimmickSaveState state)
    {
        _isUsed = state.IsUsed;
        base.RestoreState(state);

        if (_anim != null)
        {
            _anim.SetBool(_isOnHash, IsOn);
        }
    }
}
