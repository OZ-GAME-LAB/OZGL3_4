using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UI.Image;

public enum PlayerState
{
    None = 0,
    Idle,
    Walk,
    Jump,
    Dash,
    Grab,
    Change,
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
    private ShadowBase _pendingShadow;
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
    private ContactFilter2D _groundFilter;
    private RaycastHit2D[] _groundHits = new RaycastHit2D[4];
    private RaycastHit2D _groundHit;
    private Vector2 _groundCheckSize = new Vector2(0.8f, 0.1f);
    private float _groundLockTime = 0.1f;
    private float _groundLockTimer;
    public LayerMask GroundLayer => _groundLayer;
    public RaycastHit2D GroundHit => _groundHit;

    [Header("Dash")]
    private float _dashCooldownTimer;
    private float _defaultGravity;
    private float _facing = 1f;
    public bool CanDash => _dashCooldownTimer <= 0f;
    public float DefaultGravity => _defaultGravity;
    public float Facing => _facing;

    [Header("Grab")]
    public Grabbable GrabTarget { get; private set; }

    public bool IsDead { get; private set; }
    public event Action Died;

    [Header("Component")]
    private PlayerInteractor _playerInteractor;
    private CapsuleCollider2D _collider;
    private Rigidbody2D _rigid;
    private Animator _anim;
    private SpriteRenderer _sprite;
    public CapsuleCollider2D Collider => _collider;
    public Rigidbody2D Rigid => _rigid;
    public Animator Animator => _anim;

    #region Unity LifeCycle
    private void Awake()
    {
        _playerInteractor = GetComponent<PlayerInteractor>();
        _collider = GetComponent<CapsuleCollider2D>();
        _rigid = GetComponent<Rigidbody2D>();
        _anim = GetComponentInChildren<Animator>();
        _sprite = GetComponentInChildren<SpriteRenderer>();
        _defaultGravity = _rigid.gravityScale;

        // 상태들 초기화
        _stateList.Add(PlayerState.Idle, new IdleState(this));
        _stateList.Add(PlayerState.Walk, new WalkState(this));
        _stateList.Add(PlayerState.Jump, new JumpState(this));
        _stateList.Add(PlayerState.Dash, new DashState(this));
        _stateList.Add(PlayerState.Grab, new GrabState(this));
        _stateList.Add(PlayerState.Change, new ChangeState(this));

        // 그림자 초기화
        //  _shadowList.Add(ShadowType.Default, new DefaultShadow(this));
        // _shadowList.Add(ShadowType.Mouse, new MouseShadow(this));
        // _shadowList.Add(ShadowType.Rabbit, new RabbitShadow(this));*/

        // 그림자 애니메이션
        foreach (var entry in _shadowAnimators)
        {
            if(ShadowAnimatorList.TryAdd(entry.Type, entry.Controller) == false)
            {
                Debug.LogWarning("애니메이션 중복");
            }
        }

        _groundFilter = new ContactFilter2D();
        _groundFilter.SetLayerMask(_groundLayer);
        _groundFilter.useTriggers = false;
    }

    private void OnEnable()
    {
        // 텔레포트 이벤트 등록
        NetworkManager.Instance.TeleportService.OnTeleportRequested += TeleportTo;
    }

    private void OnDisable()
    {
        NetworkManager.Instance.TeleportService.OnTeleportRequested -= TeleportTo;
    }

    private void Start()
    {
        _viewModel = NetworkManager.Instance.PlayerService.GetPlayerViewModel();

        _viewModel.PropertyChanged += OnPropertyChanged;
        _viewModel.ContainerPropertyChanged += OnContainerPropChanged;

        // 초기 그림자 설정
        _viewModel.AddShadow(ShadowType.Default);
        _viewModel.CurrentShadowType = ShadowType.Default;
        // 초기 상태 설정
        ChangeState(PlayerState.Idle);
        

        MapManager.Instance.Transform_Target = transform;
        Camera.main.GetComponent<CameraFollow>().SetTarget(transform);
    }

    private void Update()
    {
        _currentState.Update();
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

        if (_currentShadow == null)
        {
            _currentShadow = nextShadow;
            _currentShadow.Enter();
            return;
        }

        _pendingShadow = nextShadow;
        ChangeState(PlayerState.Change);
    }

    public void ApplyPendingShadow()
    {
        if (_pendingShadow == null)
            return;

        _currentShadow.Exit();
        _currentShadow = _pendingShadow;
        _pendingShadow = null;
        _currentShadow.Enter();
    }

    public void AddShadow(ShadowType type)
    {
        if (_shadowList.ContainsKey(type) == true)
            return;

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

    public void SetVelocity(float speed, bool isFixed = false)
    {
        float xOffset = _movement.x * speed;

        if (_currentState is not JumpState && _isGrounded)
        {
            // 바닥 위: 경사면을 따라가는 방향으로 이동 (평지면 slopeDir = (1, 0) → y = 0)
            // 멈추면 xOffset = 0 → 속도 (0, 0) 이라 튀거나 미끄러지지 않음
            Vector2 slopeDir = -Vector2.Perpendicular(_groundHit.normal).normalized;
            _rigid.linearVelocity = slopeDir * xOffset;
        }
        else
        {
            // 점프 중이거나 공중: y는 중력에 맡김
            _rigid.linearVelocity = new Vector2(xOffset, _rigid.linearVelocity.y);
        }

        if (isFixed == false)
        {
            _sprite.flipX = _facing < 0;
        }
    }

    public void SetJump()
    {
        _rigid.linearVelocityY = 0;
        _rigid.AddForce(Vector2.up * CurrentShadow.JumpPower, ForceMode2D.Impulse);
        _jumpCount++;
        _groundLockTimer = _groundLockTime;
    }

    public void TryStartGrab()
    {
        if (IsGrounded == false)
            return;

        Grabbable target = null;
        float dir = 0;
        if (FindGrabbalbe(out target, out dir) == false)
            return;

        _sprite.flipX = dir < 0;
        GrabTarget = target;
        ChangeState(PlayerState.Grab);
    }

    public void EndGrab()
    {
        if (_currentStateType != PlayerState.Grab)
            return;

        ChangeState(PlayerState.Idle);
    }

    private bool FindGrabbalbe(out Grabbable target, out float dir)
    {
        // 바라보는 방향 체크
        dir = _facing;
        target = FindGrabbableAt(dir);
        if (target != null)
            return true;

        // 없으면 반대쪽
        dir = -_facing;
        target = FindGrabbableAt(dir);
        return target != null;
    }

    private Grabbable FindGrabbableAt(float dir)
    {
        Bounds b = _collider.bounds;

        Vector2 center = new Vector2(b.center.x + dir * (b.extents.x + 0.2f * 0.5f), b.center.y);
        Vector2 size = new Vector2(0.2f, b.size.y + 0.8f);

        Collider2D hit = Physics2D.OverlapBox(center, size, 0f, LayerMask.GetMask("Grabbable"));
        if (hit == null)
            return null;

        return hit.GetComponent<Grabbable>();
    }

    public void TeleportTo(Vector2 dest)
    {
        _rigid.linearVelocity = Vector2.zero;
        _rigid.position = dest;
        transform.position = dest;
        _rigid.rotation = 0f;
        ChangeState(PlayerState.Idle);
    }

    public void Die()
    {
        if (IsDead)
            return;
        IsDead = true;

        _rigid.linearVelocity = Vector2.zero;
        _rigid.simulated = false;
        _anim.Play("Dead");

        DeadAsync().Forget();
        
    }

    private async UniTaskVoid DeadAsync()
    {
        float delayTime = _anim.GetCurrentAnimatorClipInfo(0).Length;
        await UniTask.Delay(TimeSpan.FromSeconds(delayTime));

        Died?.Invoke();
    }

    public void Revive(Vector2 position)
    {
        _rigid.simulated = true;     // 물리를 먼저 켜야 위치 이동이 제대로 반영됨
        TeleportTo(position);        // 속도 0, 위치 이동, 회전 초기화
        IsDead = false;

        ChangeState(PlayerState.Idle);     // 프로젝트의 상태 전환 방식에 맞게
        Animator.Play("Idle");
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
        if (_groundLockTimer > 0f)
        {
            _isGrounded = false;
            return;
        }

        int count = _collider.Cast(Vector2.down, _groundFilter, _groundHits, 0.1f);

        float minNormalY = Mathf.Cos(50f * Mathf.Deg2Rad);
        bool found = false;
        Vector2 bestNormal = Vector2.up;

        // 여러 개가 맞으면 가장 평평한 바닥을 선택 (벽은 제외)
        for (int i = 0; i < count; i++)
        {
            Vector2 normal = _groundHits[i].normal;
            if (normal.y < minNormalY)
            {
                continue;
            }
            if (!found || normal.y > bestNormal.y)
            {
                _groundHit = _groundHits[i];
                bestNormal = normal;
                found = true;
            }
        }

        _isGrounded = found;

        if (_isGrounded && Mathf.Abs(Rigid.linearVelocity.y) <= 0.01f)
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
        else
        {
            _currentShadow.ReleaseAbility();
        }
    }

    private void OnInteraction(InputValue value)
    {
        if (value.isPressed)
        {
            _playerInteractor.TryInteract();
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (_groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(_groundCheck.position, _groundCheckSize);

        if (_collider == null) _collider = GetComponent<CapsuleCollider2D>();
        if (_collider == null) return;



        Gizmos.color = Color.cyan;

        Bounds b = _collider.bounds;

        Vector2 center = new Vector2(b.center.x + Facing * (b.extents.x + 0.2f * 0.5f), b.center.y);
        Vector2 size = new Vector2(0.2f, b.size.y + 0.8f);
        Gizmos.DrawWireCube(center, size);

        center = new Vector2(b.center.x + -Facing * (b.extents.x + 0.2f * 0.5f), b.center.y);
        size = new Vector2(0.2f, b.size.y + 0.8f);
        Gizmos.DrawWireCube(center, size);
    }
}
