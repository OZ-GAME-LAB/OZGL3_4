using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 밀고 당길 수 있는 오브젝트입니다. (상자 등)
/// - 밀기: 플레이어가 옆에서 걸어가면 물리적으로 밀립니다.
/// - 당기기: 옆의 잡는 범위에서 E 키를 누르고 있으면 플레이어를 따라옵니다.
/// - 플레이어가 밀지도 당기지도 않으면 미끄러지지 않고 멈춥니다.
///
/// 구조
/// Gimmick_Pushable             ← PushableObject, Rigidbody2D(Dynamic), BoxCollider2D, 레이어 Ground
/// ├─ Visual                    ← 상자 그림
/// └─ Prompt                    ← 'E' 말주머니 (선택)
///
/// - 레이어를 Ground로 두면 플레이어가 위에 올라설 수 있고, 빛 가림 계산에서 빛을 막는 장애물이 됩니다.
/// - 무거운 느낌을 주려면 Rigidbody2D의 Mass를 올리세요. (밀 때 더 느려짐)
/// - 현재는 임시로 E 키를 직접 감지합니다. 플레이어 상호작용 시스템이 완성되면 교체합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PushableObject : MonoBehaviour
{
    private const string PlayerTag = "Player";
    private const float SideContactThreshold = 0.7f;

    [Header("Reference")]
    [SerializeField] private Rigidbody2D Rigidbody2D_Body;
    [SerializeField] private Collider2D Collider2D_Body;
    [SerializeField] private GameObject GameObject_Prompt;

    [Header("Move")]
    [SerializeField] private float _maxSpeed = 4f;
    [SerializeField] private float _stopDeceleration = 30f;

    [Header("Pull")]
    [SerializeField] private bool _isPullable = true;
    [SerializeField] private Key _pullKey = Key.E;
    [SerializeField] private float _grabAreaPadding = 0.4f;

    private readonly List<Collider2D> _overlapResults = new List<Collider2D>();
    private ContactFilter2D _contactFilter;
    private bool _isPushedThisStep;
    private Rigidbody2D _pullingPlayer;

    public bool IsPulled
    {
        get { return _pullingPlayer != null; }
    }

    private void Reset()
    {
        Rigidbody2D_Body = GetComponent<Rigidbody2D>();
        Collider2D_Body = GetComponent<Collider2D>();
    }

    private void Awake()
    {
        _contactFilter = new ContactFilter2D();
        _contactFilter.NoFilter();

        if (Rigidbody2D_Body != null)
        {
            // 굴러가지 않도록 회전을 막습니다.
            Rigidbody2D_Body.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void OnEnable()
    {
        _isPushedThisStep = false;
        _pullingPlayer = null;
        SetPromptVisible(false);
    }

    private void Update()
    {
        Rigidbody2D playerInArea = FindPlayerInGrabArea();
        UpdatePull(playerInArea);
        SetPromptVisible(_isPullable && playerInArea != null && IsPulled == false);
    }

    private void FixedUpdate()
    {
        if (Rigidbody2D_Body == null)
            return;

        Vector2 velocity = Rigidbody2D_Body.linearVelocity;

        if (IsPulled)
        {
            // 당기는 동안은 플레이어의 가로 속도를 그대로 따라갑니다.
            velocity.x = _pullingPlayer.linearVelocity.x;
        }
        else if (_isPushedThisStep == false)
        {
            // 밀지 않으면 미끄러지지 않고 빠르게 멈춥니다.
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, _stopDeceleration * Time.fixedDeltaTime);
        }

        velocity.x = Mathf.Clamp(velocity.x, -_maxSpeed, _maxSpeed);
        Rigidbody2D_Body.linearVelocity = velocity;

        // 충돌 정보는 물리 계산 뒤에 들어오므로 다음 물리 프레임에서 다시 확인합니다.
        _isPushedThisStep = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.collider.CompareTag(PlayerTag) == false)
            return;

        // 옆면에 닿아 있을 때만 미는 중으로 봅니다. (위에 올라서 있는 경우 제외)
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Mathf.Abs(collision.GetContact(i).normal.x) >= SideContactThreshold)
            {
                _isPushedThisStep = true;
                return;
            }
        }
    }

    private void UpdatePull(Rigidbody2D playerInArea)
    {
        if (_isPullable == false || Keyboard.current == null)
        {
            _pullingPlayer = null;
            return;
        }

        bool isKeyHeld = Keyboard.current[_pullKey].isPressed;

        if (IsPulled)
        {
            // 키를 떼거나 범위를 벗어나면 놓습니다.
            if (isKeyHeld == false || playerInArea == null)
            {
                _pullingPlayer = null;
            }
            return;
        }

        if (playerInArea != null && Keyboard.current[_pullKey].wasPressedThisFrame)
        {
            _pullingPlayer = playerInArea;
        }
    }

    private Rigidbody2D FindPlayerInGrabArea()
    {
        if (Collider2D_Body == null)
            return null;

        // 오브젝트 양옆으로 조금 넓힌 범위에서 플레이어를 찾습니다.
        Bounds bounds = Collider2D_Body.bounds;
        Vector2 areaSize = new Vector2(bounds.size.x + _grabAreaPadding * 2f, bounds.size.y);
        int count = Physics2D.OverlapBox(bounds.center, areaSize, 0f, _contactFilter, _overlapResults);

        for (int i = 0; i < count; i++)
        {
            Collider2D other = _overlapResults[i];
            if (other.CompareTag(PlayerTag) && other.attachedRigidbody != null)
            {
                return other.attachedRigidbody;
            }
        }

        return null;
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (GameObject_Prompt != null && GameObject_Prompt.activeSelf != isVisible)
        {
            GameObject_Prompt.SetActive(isVisible);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Collider2D_Body == null)
            return;

        Gizmos.color = Color.green;
        Bounds bounds = Collider2D_Body.bounds;
        Gizmos.DrawWireCube(bounds.center, new Vector3(bounds.size.x + _grabAreaPadding * 2f, bounds.size.y, 0f));
    }
}
