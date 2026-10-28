using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 스위치 기믹입니다. 플레이어가 범위 안에서 상호작용 키를 누르면 연결된 기믹에 신호를 보냅니다.
///
/// 배치 방법
/// 1. 상호작용 범위만큼 Collider2D를 추가하고 Is Trigger를 체크합니다.
/// 2. 버튼 그림은 자식 오브젝트(Visual)의 SpriteRenderer로 두고 SpriteRenderer_Switch에 연결합니다.
/// 3. List_Receiver에 빛 영역, 문 등 신호를 받을 기믹을 넣습니다.
/// 4. 플레이어 오브젝트의 태그가 Player여야 범위 안에 들어온 것을 감지합니다.
///
/// 외형 표현
/// - Sprite_On, Sprite_Off를 둘 다 넣으면 상태에 따라 그림이 바뀌고, 비워 두면 색으로 표시합니다.
/// - 누를 때마다 Visual이 살짝 내려갔다 올라오는 눌림 연출이 재생됩니다.
///
/// 현재는 임시로 상호작용 키(E)를 스위치에서 직접 감지합니다.
/// 플레이어 상호작용 시스템이 완성되면 플레이어 쪽에서 Interact()를 호출하는 방식으로 교체합니다.
/// </summary>
public class Switch : SignalSender
{
    public enum SwitchMode
    {
        Toggle, // 누를 때마다 켜짐/꺼짐 전환
        Once,   // 한 번만 작동하고 고정
    }

    private const string PlayerTag = "Player";

    [Header("Reference")]
    [SerializeField] private SpriteRenderer SpriteRenderer_Switch;
    [SerializeField] private GameObject GameObject_Prompt;

    [Header("Behaviour")]
    [SerializeField] private SwitchMode _switchMode = SwitchMode.Toggle;
    [SerializeField] private Key _interactKey = Key.E;

    [Header("State View")]
    [SerializeField] private Sprite Sprite_On;
    [SerializeField] private Sprite Sprite_Off;
    [SerializeField] private Color _onColor = Color.yellow;
    [SerializeField] private Color _offColor = Color.gray;

    [Header("Press Animation")]
    [SerializeField] private float _pressDepth = 0.1f;
    [SerializeField] private float _pressDuration = 0.06f;

    private int _playerOverlapCount;
    private bool _isUsed;
    private Vector3 _visualDefaultPosition;
    private CancellationTokenSource _pressCancellationSource;

    public bool IsPlayerInRange
    {
        get { return _playerOverlapCount > 0; }
    }

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
        _playerOverlapCount = 0;
        _isUsed = false;
        RefreshPrompt();
    }

    private void OnDisable()
    {
        StopPressAnimation();
    }

    private void Update()
    {
        if (IsPlayerInRange == false)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[_interactKey].wasPressedThisFrame)
        {
            Interact();
        }
    }

    /// <summary>
    /// 스위치를 작동시킵니다. 플레이어 상호작용 시스템에서 호출할 메서드입니다.
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(PlayerTag) == false)
            return;

        _playerOverlapCount++;
        RefreshPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(PlayerTag) == false)
            return;

        _playerOverlapCount = Mathf.Max(0, _playerOverlapCount - 1);
        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (GameObject_Prompt != null)
        {
            GameObject_Prompt.SetActive(IsPlayerInRange);
        }
    }
}
