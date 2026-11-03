using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 빛 가림 계산 컴포넌트입니다. 광원에서 광선을 여러 갈래로 쏴서 장애물에 막힌 곳까지만 빛 영역을 만듭니다.
/// 계산한 모양을 판정(PolygonCollider2D)과 화면의 빛(Light2D, 자유형)에 함께 적용하므로 보이는 빛과 판정이 항상 같습니다.
/// 광선이 거울(Mirror)에 닿으면 거울에 알려 반사광을 만들게 합니다.
///
/// 배치 방법 (Gimmick_LightZone 부모 오브젝트에 추가)
/// - 판정 콜라이더는 BoxCollider2D 대신 PolygonCollider2D(Is Trigger)를 사용하고 LightZone의 Collider2D_Zone에도 연결합니다.
/// - LightZone의 Is Sync Shape With Collider는 꺼 주세요. (이 컴포넌트가 모양을 직접 정합니다)
/// - 이 오브젝트의 위치가 광원 위치입니다. 장애물 콜라이더 안에 두지 마세요.
/// - _obstacleLayer에 빛을 막을 레이어(예: Ground, Grabbable)를 지정합니다. 플레이어 레이어는 넣지 마세요.
/// - 오브젝트를 선택하면 씬 뷰에 빛이 닿을 수 있는 최대 범위가 노란 선으로 표시됩니다.
/// </summary>
[DefaultExecutionOrder(10)]
public class LightRayCaster : MonoBehaviour
{
    public enum EmissionShape
    {
        Beam, // 평행한 빛줄기 (창문 빛, 레이저)
        Cone, // 한 점에서 퍼지는 빛 (램프)
    }

    private const float MinHitDistance = 0.01f;
    private const float ShapeChangeThreshold = 0.001f;

    [Header("Reference")]
    [SerializeField] private LightZone LightZone_Target;
    [SerializeField] private PolygonCollider2D PolygonCollider2D_Zone;
    [SerializeField] private Light2D Light2D_Zone;

    [Header("Emission")]
    [SerializeField] private EmissionShape _emissionShape = EmissionShape.Beam;
    [SerializeField] private Vector2 _localDirection = Vector2.down;
    [SerializeField] private float _range = 6f;
    [SerializeField] private float _beamWidth = 2f;
    [SerializeField] private float _coneAngle = 60f;
    [SerializeField] private int _rayCount = 32;

    [Header("Obstacle")]
    [SerializeField] private LayerMask _obstacleLayer;

    private readonly List<RaycastHit2D> _hitResults = new List<RaycastHit2D>();
    private readonly List<Vector2> _worldPoints = new List<Vector2>();
    private readonly List<Vector2> _beamEndPoints = new List<Vector2>();
    private readonly List<Vector2> _colliderPoints = new List<Vector2>();
    private readonly List<Vector2> _previousColliderPoints = new List<Vector2>();
    private ContactFilter2D _contactFilter;
    private Collider2D _ignoredCollider;
    private Vector3[] _lightPath;

    public Vector2 LocalDirection
    {
        get { return _localDirection; }
    }

    public float BeamWidth
    {
        get { return _beamWidth; }
    }

    private void Reset()
    {
        LightZone_Target = GetComponent<LightZone>();
        PolygonCollider2D_Zone = GetComponent<PolygonCollider2D>();
        Light2D_Zone = GetComponentInChildren<Light2D>();
        _obstacleLayer = LayerMask.GetMask("Ground", "Grabbable");
    }

    private void FixedUpdate()
    {
        // 빛이 꺼져 있으면 계산하지 않습니다.
        if (LightZone_Target != null && LightZone_Target.IsOn == false)
            return;

        RecalculateShape();
    }

    /// <summary>
    /// 빔 너비를 바꿉니다. (거울의 반사광 크기 조절용)
    /// </summary>
    public void SetBeamWidth(float width)
    {
        _beamWidth = Mathf.Max(0.01f, width);
    }

    /// <summary>
    /// 광선이 무시할 콜라이더를 지정합니다. (거울의 반사광이 자기 거울에 막히지 않도록)
    /// </summary>
    public void SetIgnoredCollider(Collider2D ignoredCollider)
    {
        _ignoredCollider = ignoredCollider;
    }

    /// <summary>
    /// 장애물을 고려해 빛 모양을 다시 계산하고 판정과 빛에 적용합니다.
    /// 에디터에서는 컴포넌트 우클릭 → "빛 모양 다시 계산"으로 미리 볼 수 있습니다.
    /// </summary>
    [ContextMenu("빛 모양 다시 계산")]
    public void RecalculateShape()
    {
        RefreshContactFilter();
        _worldPoints.Clear();

        Vector2 origin = transform.position;
        Vector2 direction = GetWorldDirection();
        int rayCount = Mathf.Max(2, _rayCount);

        if (_emissionShape == EmissionShape.Beam)
        {
            BuildBeamPoints(origin, direction, rayCount);
        }
        else
        {
            BuildConePoints(origin, direction, rayCount);
        }

        ApplyShape();
    }

    private void BuildBeamPoints(Vector2 origin, Vector2 direction, int rayCount)
    {
        // 빛줄기의 시작선을 따라 평행한 광선을 쏩니다.
        Vector2 side = new Vector2(-direction.y, direction.x);
        _beamEndPoints.Clear();

        for (int i = 0; i < rayCount; i++)
        {
            float ratio = (float)i / (rayCount - 1) - 0.5f;
            Vector2 start = origin + side * (ratio * _beamWidth);
            _worldPoints.Add(start);
            _beamEndPoints.Add(CastRay(start, direction));
        }

        // 시작점들 → 끝점들(역순)으로 이어서 하나의 다각형을 만듭니다.
        for (int i = _beamEndPoints.Count - 1; i >= 0; i--)
        {
            _worldPoints.Add(_beamEndPoints[i]);
        }
    }

    private void BuildConePoints(Vector2 origin, Vector2 direction, int rayCount)
    {
        // 광원 한 점에서 부채꼴로 광선을 쏩니다.
        _worldPoints.Add(origin);
        float halfAngle = _coneAngle * 0.5f;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / (rayCount - 1));
            Vector2 rayDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            _worldPoints.Add(CastRay(origin, rayDirection));
        }
    }

    /// <summary>
    /// 광선을 쏴서 가장 가까운 장애물 지점을 돌려줍니다. 거울에 닿으면 거울에 알립니다.
    /// </summary>
    private Vector2 CastRay(Vector2 start, Vector2 direction)
    {
        int count = Physics2D.Raycast(start, direction, _contactFilter, _hitResults, _range);

        int nearestIndex = -1;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = _hitResults[i];
            if (hit.distance < MinHitDistance)
            {
                continue;
            }

            if (_ignoredCollider != null && hit.collider == _ignoredCollider)
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestIndex = i;
            }
        }

        if (nearestIndex == -1)
        {
            return start + direction * _range;
        }

        RaycastHit2D nearestHit = _hitResults[nearestIndex];

        Mirror mirror;
        if (nearestHit.collider.TryGetComponent(out mirror))
        {
            mirror.RegisterHit(this, nearestHit.point, direction);
        }

        return nearestHit.point;
    }

    private void ApplyShape()
    {
        if (PolygonCollider2D_Zone == null)
            return;

        // 콜라이더 기준 좌표로 변환합니다.
        _colliderPoints.Clear();
        Transform colliderTransform = PolygonCollider2D_Zone.transform;
        for (int i = 0; i < _worldPoints.Count; i++)
        {
            Vector2 localPoint = colliderTransform.InverseTransformPoint(_worldPoints[i]);
            _colliderPoints.Add(localPoint - PolygonCollider2D_Zone.offset);
        }

        // 모양이 거의 그대로면 다시 적용하지 않습니다. (불필요한 콜라이더 재생성 방지)
        if (IsSameShape())
            return;

        PolygonCollider2D_Zone.pathCount = 1;
        PolygonCollider2D_Zone.SetPath(0, _colliderPoints);

        _previousColliderPoints.Clear();
        _previousColliderPoints.AddRange(_colliderPoints);

        ApplyLightShape();
    }

    private void ApplyLightShape()
    {
        if (Light2D_Zone == null || Light2D_Zone.lightType != Light2D.LightType.Freeform)
            return;

        if (_lightPath == null || _lightPath.Length != _worldPoints.Count)
        {
            _lightPath = new Vector3[_worldPoints.Count];
        }

        Transform lightTransform = Light2D_Zone.transform;
        for (int i = 0; i < _worldPoints.Count; i++)
        {
            _lightPath[i] = lightTransform.InverseTransformPoint(_worldPoints[i]);
        }

        Light2D_Zone.SetShapePath(_lightPath);
    }

    private bool IsSameShape()
    {
        if (_previousColliderPoints.Count != _colliderPoints.Count)
            return false;

        for (int i = 0; i < _colliderPoints.Count; i++)
        {
            if ((_previousColliderPoints[i] - _colliderPoints[i]).sqrMagnitude > ShapeChangeThreshold * ShapeChangeThreshold)
            {
                return false;
            }
        }

        return true;
    }

    private void RefreshContactFilter()
    {
        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(_obstacleLayer);
        _contactFilter.useTriggers = false;
    }

    private Vector2 GetWorldDirection()
    {
        Vector2 worldDirection = transform.TransformDirection(_localDirection);
        if (worldDirection.sqrMagnitude <= 0f)
        {
            return Vector2.down;
        }

        return worldDirection.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 origin = transform.position;
        Vector2 direction = GetWorldDirection();

        if (_emissionShape == EmissionShape.Beam)
        {
            Vector2 side = new Vector2(-direction.y, direction.x) * (_beamWidth * 0.5f);
            Vector2 farOffset = direction * _range;
            Gizmos.DrawLine(origin - side, origin + side);
            Gizmos.DrawLine(origin - side, origin - side + farOffset);
            Gizmos.DrawLine(origin + side, origin + side + farOffset);
            Gizmos.DrawLine(origin - side + farOffset, origin + side + farOffset);
            return;
        }

        float halfAngle = _coneAngle * 0.5f;
        Vector2 leftEdge = Quaternion.Euler(0f, 0f, -halfAngle) * direction;
        Vector2 rightEdge = Quaternion.Euler(0f, 0f, halfAngle) * direction;
        Gizmos.DrawLine(origin, origin + leftEdge * _range);
        Gizmos.DrawLine(origin, origin + rightEdge * _range);
        Gizmos.DrawLine(origin + leftEdge * _range, origin + rightEdge * _range);
    }

    private void OnValidate()
    {
        if (_obstacleLayer.value == 0)
        {
            Debug.LogWarning($"[{name}] 빛을 막을 레이어(_obstacleLayer)를 지정해 주세요. (예: Ground)", this);
        }

        if (Light2D_Zone != null && Light2D_Zone.lightType != Light2D.LightType.Freeform)
        {
            Debug.LogWarning($"[{name}] 빛 가림 계산은 자유형 광원(Freeform Light 2D)에서만 화면에 반영됩니다.", this);
        }

        if (PolygonCollider2D_Zone == null)
        {
            Debug.LogWarning($"[{name}] 판정용 PolygonCollider2D(Is Trigger)를 연결해 주세요.", this);
        }
    }
}
