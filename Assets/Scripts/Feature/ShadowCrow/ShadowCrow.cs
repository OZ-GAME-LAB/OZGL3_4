using System.Collections.Generic;
using UnityEngine;

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

    private ShadowCrowStateBase _currentState;
    private ShadowCrowState _currentStateType = ShadowCrowState.None;
    private Dictionary<ShadowCrowState, ShadowCrowStateBase> _stateList = new Dictionary<ShadowCrowState, ShadowCrowStateBase>();

    public ShadowCrowState CurrentStateType => _currentStateType;
    public Vector2 Position => transform.position;

    public bool IsDialogPlaying => SpeechBubble_Crow != null && SpeechBubble_Crow.IsPlaying;

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
        // 풀에서 다시 꺼내져도 항상 숨김 상태부터 시작한다.
        ChangeState(ShadowCrowState.Hidden);
    }

    private void OnDisable()
    {
        _currentState?.Exit();
        _currentState = null;
        _currentStateType = ShadowCrowState.None;
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
            SpeechBubble_Crow.ShowNextDialog();
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

        ChangeState(ShadowCrowState.Talk);
        return true;
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
