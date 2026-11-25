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
    [SerializeField] private bool _isFocusOnTalk = true;                       // 대화 연출 중 카메라가 플레이어 대신 까마귀를 비춤
    [SerializeField] private Vector2 _talkFocusOffset = new Vector2(0f, 1f);   // 까마귀 발 기준. 말풍선까지 보이도록 위로

    private const string _talkInputActionName = "Player/Interaction";
    private const string _defaultAppearAnimationName = "CrowAppear";

    private bool _isAutoTalked;
    private bool _isEnding;
    private string _endingCompleteDialogId;
    private string _endingIncompleteDialogId;
    private bool _isEndingComplete;
    private bool _isEndingFinished;
    private int _talkStartFrame = -1;
    private InputAction _talkInputAction;
    private CameraController _cameraController;
    private CameraFollow _cameraFollow;   // 이 까마귀가 카메라 초점을 잡았을 때만 값이 있다

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

    // 엔딩 대사를 마지막까지 넘겨 정상 종료했을 때 발생한다. true면 수집품을 모두 모은 갈래(A), false면 B.
    public event Action<bool> OnEndEndingDialog;

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
        if (SpeechBubble_Crow != null)
        {
            SpeechBubble_Crow.OnEndDialog -= OnEndBubbleDialog;
            SpeechBubble_Crow.OnEndDialog += OnEndBubbleDialog;
        }

        ChangeState(ShadowCrowState.Hidden);
    }

    private void OnDisable()
    {
        if (SpeechBubble_Crow != null)
        {
            SpeechBubble_Crow.OnEndDialog -= OnEndBubbleDialog;
        }

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

        // 엔딩 대사는 한 번만. 끝난 뒤에는 연출이 이어지므로 다시 말을 걸 수 없다.
        if (_isEnding && _isEndingFinished)
        {
            return;
        }

        string dialogId = GetInteractDialogId();
        if (string.IsNullOrWhiteSpace(dialogId))
        {
            Debug.LogError($"[ShadowCrow] {name}: 상호작용 대사 ID가 비어 있습니다. 일반은 _dialogId, 엔딩은 ShadowCrowSpot의 엔딩 대사 ID를 입력하세요.");
            return;
        }

        StartDialog(dialogId);
    }

    // F를 누른 순간의 대사 ID. 엔딩 까마귀는 이때 수집품 상태를 확인해서 A/B 대사를 고른다.
    private string GetInteractDialogId()
    {
        if (_isEnding == false)
        {
            return _dialogId;
        }

        _isEndingComplete = IsAllRewardsCollected();
        if (_isEndingComplete)
        {
            return _endingCompleteDialogId;
        }

        return _endingIncompleteDialogId;
    }

    private bool IsAllRewardsCollected()
    {
        if (MapUtil.TryGetMapService(this, nameof(IsAllRewardsCollected), out MapService mapService) == false)
        {
            return false;
        }

        return mapService.IsAllRewardsCollected();
    }

    private void OnEndBubbleDialog()
    {
        if (_isEnding == false)
        {
            return;
        }

        _isEndingFinished = true;
        OnEndEndingDialog?.Invoke(_isEndingComplete);
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

    // 풀에서 재사용될 수 있으므로 일반 스폰 데이터를 넣을 때 엔딩 설정도 함께 끈다.
    public void SetSpawnData(string dialogId, bool isAutoTalk)
    {
        _dialogId = dialogId;
        _isAutoTalk = isAutoTalk;
        _isAutoTalked = false;
        _isEnding = false;
        _endingCompleteDialogId = string.Empty;
        _endingIncompleteDialogId = string.Empty;
        _isEndingComplete = false;
        _isEndingFinished = false;
    }

    // 엔딩 까마귀로 설정한다. 자동 대화 없이 F 상호작용으로만 대사를 시작한다.
    // completeDialogId: 수집품을 모두 모았을 때(End_A_01), incompleteDialogId: 덜 모았을 때(End_B_01)
    public void SetEndingData(string completeDialogId, string incompleteDialogId)
    {
        SetSpawnData(string.Empty, false);
        _isEnding = true;
        _endingCompleteDialogId = completeDialogId;
        _endingIncompleteDialogId = incompleteDialogId;
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
        StartTalkFocus(mainCamera);
    }

    public void EndTalkDirecting()
    {
        EndTalkFocus();

        if (_cameraController == null)
        {
            return;
        }

        _cameraController.ZoomOut(_talkZoomTime);
        _cameraController = null;
    }

    // 카메라가 플레이어 대신 까마귀(+오프셋)를 비추게 한다. 지형·UI에 가려지는 위치에서도 까마귀와 말풍선이 보이도록.
    private void StartTalkFocus(Camera mainCamera)
    {
        if (_isFocusOnTalk == false)
        {
            return;
        }

        CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            Debug.LogWarning($"[ShadowCrow] {name}: Main Camera에 CameraFollow가 없어 까마귀 초점을 생략합니다.");
            return;
        }

        // CameraFixedZone 등이 이미 화면을 고정 중이면 그 화면을 유지하고 건드리지 않는다(대화 후 그쪽 고정이 풀리지 않도록).
        if (cameraFollow.IsFixed)
        {
            return;
        }

        cameraFollow.SetFixedPosition((Vector2)transform.position + _talkFocusOffset);
        _cameraFollow = cameraFollow;
    }

    // 이 까마귀가 잡은 초점만 풀어 다시 플레이어를 따라가게 한다.
    private void EndTalkFocus()
    {
        if (_cameraFollow == null)
        {
            return;
        }

        _cameraFollow.ClearFixedPosition();
        _cameraFollow = null;
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

    // 플레이어가 처음 생성 연출(SpawnState) 중이면 true.
    // 생성 연출도 CameraController로 줌·입력 막기를 하므로, 끝난 뒤에 등장해야 서로 덮어쓰지 않는다.
    public bool IsPlayerSpawning()
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

        return player.IsSpawning;
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
