using UnityEngine;

/// <summary>
/// 거울 기믹입니다. 빛 가림 계산(LightRayCaster)의 광선이 거울 면에 닿으면 반사광을 만듭니다.
/// 반사광의 위치·방향·너비는 매 물리 프레임마다 들어온 빛에 맞춰 바뀌고, 빛이 들어오지 않으면 꺼집니다.
///
/// 구조
/// Gimmick_Mirror               ← Mirror, BoxCollider2D(반사면, Is Trigger 해제), 레이어 Ground
/// ├─ Visual                    ← 거울 그림
/// └─ Gimmick_LightZone         ← 반사광. LightZone(Is Start On 해제) + LightRayCaster(Beam)
///
/// - _localNormal은 반사면이 바라보는 방향입니다. (씬 뷰에 하늘색 선으로 표시)
/// - 거울을 회전시키려면 Gimmick_RotatingLight의 Pivot 아래에 거울을 넣으면 됩니다.
/// - 거울을 밀고 당기려면 잡을 수 있는 오브젝트(Grabbable) 아래에 거울을 넣으면 됩니다.
/// - 거울끼리 반사가 이어질 수 있으며, 반사 한 번마다 물리 프레임 1번만큼 늦게 반영됩니다.
/// </summary>
[DefaultExecutionOrder(-10)]
public class Mirror : MonoBehaviour
{
    private const float GizmoNormalLength = 1f;

    [Header("Reference")]
    [SerializeField] private Collider2D Collider2D_Mirror;
    [SerializeField] private LightZone LightZone_Reflection;
    [SerializeField] private LightRayCaster LightRayCaster_Reflection;

    [Header("Reflection")]
    [SerializeField] private Vector2 _localNormal = Vector2.up;
    [SerializeField] private bool _isDoubleSided = false;
    [SerializeField] private float _minReflectionWidth = 0.2f;
    [SerializeField] private float _surfaceOffset = 0.05f;

    private bool _hasHit;
    private float _minProjection;
    private float _maxProjection;
    private Vector2 _incomingDirection;

    public bool IsReflecting
    {
        get { return LightZone_Reflection != null && LightZone_Reflection.IsOn; }
    }

    private void Reset()
    {
        Collider2D_Mirror = GetComponent<Collider2D>();
        LightZone_Reflection = GetComponentInChildren<LightZone>();
        LightRayCaster_Reflection = GetComponentInChildren<LightRayCaster>();
    }

    private void Awake()
    {
        if (LightRayCaster_Reflection != null)
        {
            LightRayCaster_Reflection.SetIgnoredCollider(Collider2D_Mirror);
        }
    }

    private void OnEnable()
    {
        _hasHit = false;
    }

    private void FixedUpdate()
    {
        // 실행 순서가 빛 계산보다 앞이라, 이전 물리 프레임에 모인 빛을 반영합니다.
        if (_hasHit)
        {
            ApplyReflection();
        }
        else
        {
            TurnOffReflection();
        }

        _hasHit = false;
    }

    /// <summary>
    /// 광선이 거울에 닿았을 때 LightRayCaster가 호출합니다.
    /// </summary>
    public void RegisterHit(LightRayCaster source, Vector2 hitPoint, Vector2 direction)
    {
        // 자기 반사광은 무시합니다.
        if (source == LightRayCaster_Reflection)
            return;

        Vector2 normal = GetWorldNormal();

        // 뒷면에 닿은 빛은 반사하지 않습니다. (양면 거울 제외)
        if (_isDoubleSided == false && Vector2.Dot(direction, normal) >= 0f)
            return;

        Vector2 tangent = new Vector2(normal.y, -normal.x);
        float projection = Vector2.Dot(hitPoint - (Vector2)transform.position, tangent);

        if (_hasHit == false)
        {
            _hasHit = true;
            _minProjection = projection;
            _maxProjection = projection;
            _incomingDirection = direction;
            return;
        }

        _minProjection = Mathf.Min(_minProjection, projection);
        _maxProjection = Mathf.Max(_maxProjection, projection);
    }

    private void ApplyReflection()
    {
        if (LightZone_Reflection == null || LightRayCaster_Reflection == null)
            return;

        Vector2 normal = GetWorldNormal();

        // 양면 거울에서 뒤쪽으로 들어온 빛은 뒤쪽 면 기준으로 반사합니다.
        if (Vector2.Dot(_incomingDirection, normal) > 0f)
        {
            normal = -normal;
        }

        Vector2 reflectedDirection = Vector2.Reflect(_incomingDirection.normalized, normal);
        Vector2 tangent = new Vector2(normal.y, -normal.x);
        float centerProjection = (_minProjection + _maxProjection) * 0.5f;
        float reflectionWidth = Mathf.Max(_maxProjection - _minProjection, _minReflectionWidth);

        // 반사광의 시작 위치와 방향을 맞춥니다.
        Transform reflectionTransform = LightRayCaster_Reflection.transform;
        Vector2 origin = (Vector2)transform.position + tangent * centerProjection + normal * _surfaceOffset;
        reflectionTransform.position = new Vector3(origin.x, origin.y, reflectionTransform.position.z);

        float angle = Vector2.SignedAngle(LightRayCaster_Reflection.LocalDirection, reflectedDirection);
        reflectionTransform.rotation = Quaternion.Euler(0f, 0f, angle);

        LightRayCaster_Reflection.SetBeamWidth(reflectionWidth);

        if (LightZone_Reflection.IsOn == false)
        {
            LightZone_Reflection.ReceiveSignal(true);
        }
    }

    private void TurnOffReflection()
    {
        if (LightZone_Reflection != null && LightZone_Reflection.IsOn)
        {
            LightZone_Reflection.ReceiveSignal(false);
        }
    }

    private Vector2 GetWorldNormal()
    {
        Vector2 worldNormal = transform.TransformDirection(_localNormal);
        if (worldNormal.sqrMagnitude <= 0f)
        {
            return Vector2.up;
        }

        return worldNormal.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 normal = GetWorldNormal();
        Gizmos.DrawLine(transform.position, transform.position + normal * GizmoNormalLength);
    }
}
