using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState
{
    None = 0,
    Idle,
    Walk,
    Jump,
    Dash,
}

[Serializable]
public struct ShadowAnimEntry
{
    public ShadowType Type;
    public AnimatorOverrideController Controller;
}

public class Player : MonoBehaviour
{
    private PlayerViewModel _viewModel;
    public PlayerViewModel PlayerViewModel => _viewModel;

    [Header("State")]
    private PlayerStateBase _currentState;
    private PlayerState _currentStateType = PlayerState.None;
    private Dictionary<PlayerState, PlayerStateBase> _stateList = new Dictionary<PlayerState, PlayerStateBase>();

    [Header("Shadow")]
    private ShadowBase _currentShadow;
    public ShadowBase CurrentShadow => _currentShadow;
    private Dictionary<ShadowType, ShadowBase> _shadowList = new Dictionary<ShadowType, ShadowBase>();
    [SerializeField] List<ShadowAnimEntry> _shadowAnimators;
    public Dictionary<ShadowType, AnimatorOverrideController> ShadowAnimatorList = new Dictionary<ShadowType, AnimatorOverrideController>();

    [Header("Move")]
    private Vector2 _movement = Vector2.zero;
    public bool HasMoveInput => Mathf.Abs(_movement.x) > 0;

    [Header("Jump")]
    private bool _isGrounded;
    private int _jumpCount;
    public bool IsGrounded => _isGrounded;
    public int JumpCount => _jumpCount;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundLayer;
    private Vector2 _groundCheckSize = new Vector2(0.5f, 0.1f);
    private float _groundLockTime = 0.1f;
    private float _groundLockTimer;

    [Header("Dash")]
    private float _dashCooldownTimer;
    private float _defaultGravity;
    private float _facing = 1f;
    public bool CanDash => _dashCooldownTimer <= 0f;
    public float DefaultGravity => _defaultGravity;
    public float Facing => _facing;

    [Header("Component")]
    private Rigidbody2D _rigid;
    private Animator _anim;
    private SpriteRenderer _sprite;
    public Rigidbody2D Rigid => _rigid;
    public Animator Animator => _anim;

    #region Unity LifeCycle
    private void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
        _defaultGravity = _rigid.gravityScale;

        // 상태들 초기화
        _stateList.Add(PlayerState.Idle, new IdleState(this));
        _stateList.Add(PlayerState.Walk, new WalkState(this));
        _stateList.Add(PlayerState.Jump, new JumpState(this));
        _stateList.Add(PlayerState.Dash, new DashState(this));

        // 그림자 초기화
        
        //AddShadow(ShadowType.Default);

        // 그림자 애니메이션
        foreach (var entry in _shadowAnimators)
        {
            if(ShadowAnimatorList.TryAdd(entry.Type, entry.Controller) == false)
            {
                Debug.LogWarning("애니메이션 중복");
            }
        }
    }

    private void Start()
    {
        _viewModel = NetworkManager.Instance.PlayerService.GetPlayerViewModel();

        _viewModel.PropertyChanged += OnPropertyChanged;
        _viewModel.ContainerPropertyChanged += OnContainerPropChanged;

        // 초기 상태 설정
        ChangeState(PlayerState.Idle);
        // 초기 그림자 설정
        _viewModel.AddShadow(ShadowType.Default);
        _viewModel.CurrentShadowType = ShadowType.Default;
    }

    private void FixedUpdate()
    {
        UpdateTimer();
        UpdateGround();
        _currentState.FixedUpdate();
    }
    #endregion

    #region 플레이어 설정
    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PlayerViewModel.CurrentShadowType):
                ChangeShadow();
                break;
        }
    }

    private void OnContainerPropChanged(string propertyName, ContainerEventType eventType, ShadowType type)
    {
        if (propertyName == nameof(_viewModel.OwnerShadowType) == true)
        {
            switch (eventType)
            {
                case ContainerEventType.Add:
                    AddShadow(type);
                    break;
            }
        }
    }

    public void ChangeState(PlayerState next)
    {
        if (_currentStateType == next)
            return;

        _currentState?.Exit();
        _currentStateType = next;
        _currentState = _stateList[next];
        _currentState.Enter();
    }

    public void ChangeShadow()
    {
        ShadowBase nextShadow = _shadowList[_viewModel.CurrentShadowType];

        _currentShadow?.Exit();
        _currentShadow = nextShadow;
        _currentShadow.Enter();
    }

    public void AddShadow(ShadowType type)
    {
        ShadowBase newShadow = null;

        switch (type)
        {
            case ShadowType.Default:
                newShadow = new DefaultShadow(this);
                break;
            case ShadowType.Mouse:
                newShadow = new MouseShadow(this);
                break;
            case ShadowType.Rabbit:
                newShadow = new RabbitShadow(this);
                break;
        }
        _shadowList.Add(type, newShadow);
    }

    public void SetVelocity(float speed)
    {
        float xOffset = _movement.x * speed * Time.fixedDeltaTime;
        _rigid.linearVelocity = new Vector2(xOffset, _rigid.linearVelocity.y);
        _sprite.flipX = xOffset < 0;
    }

    public void SetJump()
    {
        _rigid.AddForce(Vector2.up * CurrentShadow.JumpPower);
        _jumpCount++;
        _groundLockTimer = _groundLockTime;
    }
    #endregion

    #region 내부 처리
    private void UpdateTimer()
    {
        if(_dashCooldownTimer > 0f)
        {
            _dashCooldownTimer -= Time.fixedDeltaTime;
        }

        if(_groundLockTimer > 0f)
        {
            _groundLockTimer -= Time.fixedDeltaTime;
        }
    }

    private void UpdateGround()
    {
        if(_groundLockTimer > 0f)
        {
            _isGrounded = false;
            return;
        }

        _isGrounded = Physics2D.OverlapBox(_groundCheck.position, _groundCheckSize, 0f, _groundLayer);

        if(_isGrounded == true && Rigid.linearVelocity.y <= 0.01f)
        {
            _jumpCount = 0;
        }
    }
    #endregion

    #region Player Input
    private void OnMove(InputValue value)
    {
        _movement = value.Get<Vector2>();

        if(_movement.x != 0)
        {
            _facing = Mathf.Sign(_movement.x);
        }
    }

    private void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            _currentState.TryJump();
        }
    }

    private void OnDash(InputValue value)
    {
        if (value.isPressed)
        {
            _currentState.TryDash();
        }
    }

    private void OnAbility(InputValue value)
    {
        if (value.isPressed)
        {
            _currentShadow.UseAbility();
        }
    }

    private void OnNext(InputValue value)
    {
        if (value.isPressed)
        {
            //ChangeShadow(ShadowType.Mouse);
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (_groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_groundCheck.position, _groundCheckSize);
    }
}
