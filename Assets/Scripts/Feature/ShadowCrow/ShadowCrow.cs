using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ShadowCrowState
{
    None,
    Hidden,
    Appear,
    Idle,
    Talk,
}

public class ShadowCrow : MonoBehaviour, IInteractable
{
    [SerializeField] private Animator Animator_Crow;
    [SerializeField] private SpriteRenderer SpriteRenderer_Crow;
    [SerializeField] private Collider2D Collider_Interact;
    [SerializeField] private ShadowCrowSpeechBubble SpeechBubble_Crow;
    [SerializeField] private float _appearDistance = 4f;
    [SerializeField] private string _dialogId;

    [Header("대화 연출")]
    [SerializeField] private bool _isAutoTalk = true;                 
    [SerializeField] private bool _isDirectOnTalk = true;             
    [SerializeField, Min(0.1f)] private float _talkZoomSize = 4f;    
    [SerializeField, Min(0f)] private float _talkZoomTime = 0.5f;
    [SerializeField] private bool _isBlockInputOnTalk = true;         

    private const string _talkInputActionName = "Player/Interaction";
    private const string _defaultAppearAnimationName = "CrowAppear";

    private bool _isAutoTalked;
    private int _talkStartFrame = -1;
    private InputAction _talkInputAction;
    private CameraController _cameraController;

    private ShadowCrowStateBase _currentState;
    private ShadowCrowState _currentStateType = ShadowCrowState.None;
    private Dictionary<ShadowCrowState, ShadowCrowStateBase> _stateList = new Dictionary<ShadowCrowState, ShadowCrowStateBase>();

    public ShadowCrowState CurrentStateType => _currentStateType;
    public Vector2 Position => transform.position;

    public bool IsDialogPlaying => SpeechBubble_Crow != null && SpeechBubble_Crow.IsPlaying;

    // Appear 상태에서 재생할 Animator 상태. 기본은 CrowAppear(웅덩이에서 등장), 비석 스폰은 CrowSummon.
    public int AppearAnimationHash { get; private set; } = Animator.StringToHash(_defaultAppearAnimationName);

    public event Action OnStartAutoTalk;
    public event Action OnStartAppear;

    #region 수명
    private void Awake()
    {
        if (Animator_Crow == null || SpriteRenderer_Crow == null || Collider_Interact == null || SpeechBubble_Crow == null)
        {
            Debug.LogError($"[ShadowCrow] {name}: Animator_Crow, SpriteRenderer_Crow, Collider_Interact, SpeechBubble_Crow를 연결하세요.");
        }

        _stateList.Add(ShadowCrowState.Hidden, new ShadowCrowHiddenState(this));
        _stateList.Add(ShadowCrowState.Appear, new ShadowCrowAppearState(this));
        _stateList.Add(ShadowCrowState.Idle, new ShadowCrowIdleState(this));
        _stateList.Add(ShadowCrowState.Talk, new ShadowCrowTalkState(this));
    }

    private void OnEnable()
    {
        ChangeState(ShadowCrowState.Hidden);
    }

    private void OnDisable()
    {
        _currentState?.Exit();
        _currentState = null;
        _currentStateType = ShadowCrowState.None;

        // 등장 중(대사 시작 전)에 꺼져도 줌과 이동 막기가 남지 않도록 정리한다.
        EndTalkDirecting();
    }

    private void Update()
    {
        if (_currentState == null)
        {
            return;
        }

        _currentState.Update();
    }

    private void FixedUpdate()
    {
        if (_currentState == null)
        {
            return;
        }

        _currentState.FixedUpdate();
    }
    #endregion

    #region 상태
    public void ChangeState(ShadowCrowState next)
    {
        if (_currentStateType == next)
        {
            return;
        }

        _currentState?.Exit();
        _currentStateType = next;
        _currentState = _stateList[next];
        _currentState.Enter();
    }
    #endregion

    #region 대사
    public void Interact(Player player)
    {
        if (_currentStateType == ShadowCrowState.Talk)
        {
            if (_talkInputAction == null)
            {
                SpeechBubble_Crow.ShowNextDialog();
            }
            return;
        }

        if (_currentStateType != ShadowCrowState.Idle)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_dialogId))
        {
            Debug.LogError($"[ShadowCrow] {name}: 대사 ID(_dialogId)를 입력하세요.");
            return;
        }

        StartDialog(_dialogId);
    }

    public bool StartDialog(string dialogId)
    {
        if (SpeechBubble_Crow == null)
        {
            Debug.LogError($"[ShadowCrow] {name}: SpeechBubble_Crow가 없습니다.");
            return false;
        }

        if (_currentStateType != ShadowCrowState.Idle && _currentStateType != ShadowCrowState.Talk)
        {
            Debug.LogWarning($"[ShadowCrow] {name}: 등장이 끝난 뒤에 대사를 시작할 수 있습니다. 현재 상태: {_currentStateType}");
            return false;
        }

        if (SpeechBubble_Crow.StartDialog(dialogId) == false)
        {
            return false;
        }

        _talkStartFrame = Time.frameCount;
        ChangeState(ShadowCrowState.Talk);
        return true;
    }

    public void SetSpawnData(string dialogId, bool isAutoTalk)
    {
        _dialogId = dialogId;
        _isAutoTalk = isAutoTalk;
        _isAutoTalked = false;
    }

    // 등장 애니메이션(Animator 상태 이름)을 바꾼다. 비어 있으면 기본 CrowAppear.
    public void SetAppearAnimation(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            stateName = _defaultAppearAnimationName;
        }

        AppearAnimationHash = Animator.StringToHash(stateName);
    }

    // Appear 상태 진입 시 호출. 스폰 지점이 깃털 연출 등을 같은 순간에 시작할 수 있게 알린다.
    public void NotifyStartAppear()
    {
        OnStartAppear?.Invoke();
    }

    public void TryStartAutoTalk()
    {
        if (_isAutoTalk == false || _isAutoTalked)
        {
            return;
        }

        _isAutoTalked = true;

        if (string.IsNullOrWhiteSpace(_dialogId))
        {
            Debug.LogError($"[ShadowCrow] {name}: 자동 대화에 사용할 대사 ID(_dialogId)를 입력하세요.");
            // 등장 때 시작한 줌·이동 막기가 풀리지 않는 일이 없도록 되돌린다.
            EndTalkDirecting();
            return;
        }

        if (StartDialog(_dialogId))
        {
            OnStartAutoTalk?.Invoke();
            return;
        }

        EndTalkDirecting();
    }

    // Appear 상태 진입 시 호출. 자동 대화가 이어질 등장이면 등장 애니메이션부터 줌·이동 막기를 시작한다.
    public void StartAppearDirecting()
    {
        if (_isAutoTalk == false || _isAutoTalked)
        {
            return;
        }

        StartTalkDirecting();
    }

    public void BindTalkInput()
    {
        UnBindTalkInput();

        if (GameObjectManager.Instance == null || GameObjectManager.Instance.CurrentPlayer == null)
        {
            Debug.LogWarning($"[ShadowCrow] {name}: 플레이어를 찾을 수 없어 F 입력을 직접 연결하지 못했습니다.");
            return;
        }

        PlayerInput playerInput = GameObjectManager.Instance.CurrentPlayer.GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning($"[ShadowCrow] {name}: 플레이어에 PlayerInput이 없습니다.");
            return;
        }

        _talkInputAction = playerInput.actions.FindAction(_talkInputActionName);
        if (_talkInputAction == null)
        {
            Debug.LogWarning($"[ShadowCrow] {name}: 입력 액션 {_talkInputActionName}을 찾을 수 없습니다.");
            return;
        }

        _talkInputAction.performed += OnPerformTalkInput;
    }

    public void UnBindTalkInput()
    {
        if (_talkInputAction == null)
        {
            return;
        }

        _talkInputAction.performed -= OnPerformTalkInput;
        _talkInputAction = null;
    }

    private void OnPerformTalkInput(InputAction.CallbackContext context)
    {
        if (Time.frameCount == _talkStartFrame)
        {
            return;
        }

        SpeechBubble_Crow.ShowNextDialog();
    }

    public void StartTalkDirecting()
    {
        if (_isDirectOnTalk == false)
        {
            return;
        }

        // 등장 때 이미 시작했다면 대사가 시작돼도 다시 줌하지 않고 그대로 이어간다.
        if (_cameraController != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning($"[ShadowCrow] {name}: MainCamera가 없어 대화 연출을 생략합니다.");
            return;
        }

        _cameraController = mainCamera.GetComponent<CameraController>();
        if (_cameraController == null)
        {
            Debug.LogWarning($"[ShadowCrow] {name}: Main Camera에 CameraController가 없어 대화 연출을 생략합니다.");
            return;
        }

        _cameraController.ZoomIn(_talkZoomSize, _talkZoomTime, _isBlockInputOnTalk);
    }

    public void EndTalkDirecting()
    {
        if (_cameraController == null)
        {
            return;
        }

        _cameraController.ZoomOut(_talkZoomTime);
        _cameraController = null;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (_currentStateType != ShadowCrowState.Talk)
        {
            return;
        }

        if (collision.GetComponent<Player>() == null)
        {
            return;
        }

        SpeechBubble_Crow.StopDialog();
    }
    #endregion

    #region 상태에서 사용하는 기능
    public bool IsPlayerInRange()
    {
        if (GameObjectManager.Instance == null)
        {
            return false;
        }

        Player player = GameObjectManager.Instance.CurrentPlayer;
        if (player == null)
        {
            return false;
        }

        Vector2 offset = player.transform.position - transform.position;
        return offset.sqrMagnitude <= _appearDistance * _appearDistance;
    }

    public void SetVisible(bool isVisible)
    {
        if (SpriteRenderer_Crow == null)
        {
            return;
        }

        SpriteRenderer_Crow.enabled = isVisible;
    }

    public void SetInteractable(bool isInteractable)
    {
        if (Collider_Interact == null)
        {
            return;
        }

        Collider_Interact.enabled = isInteractable;
    }

    public void PlayAnimation(int stateHash)
    {
        if (Animator_Crow == null)
        {
            return;
        }

        Animator_Crow.Play(stateHash, 0, 0f);
    }

    public bool IsAnimationFinished(int stateHash)
    {
        if (Animator_Crow == null)
        {
            return true;
        }

        AnimatorStateInfo stateInfo = Animator_Crow.GetCurrentAnimatorStateInfo(0);
        return stateInfo.shortNameHash == stateHash && stateInfo.normalizedTime >= 1f;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.2f, 0.8f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _appearDistance);
    }
}
