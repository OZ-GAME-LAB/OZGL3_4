using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 빛 영역(LightZone)이 켜져 있을 때 외곽선과 떠다니는 입자를 표시합니다.
/// LightZone과 같은 오브젝트에 붙입니다. 효과는 Inspector에서 각각 켜고 끌 수 있습니다.
/// 입자는 빛이 꺼져 있어도 계속 생성·이동하고, 보이는 것만 빛 상태를 따릅니다.
/// </summary>
public class LightZoneVisual : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private LightZone LightZone_Target;
    [SerializeField] private Collider2D Collider2D_Zone;
    [SerializeField] private LineRenderer LineRenderer_Outline;
    [SerializeField] private ParticleSystem ParticleSystem_Dust;
    [SerializeField] private Light2D Light2D_Zone;

    [Header("효과 선택")]
    [SerializeField] private bool _isOutlineEnabled = true;
    [SerializeField] private bool _isDustEnabled = true;

    [Header("외곽선")]
    [SerializeField] private float _outlineWidth = 0.08f;
    [SerializeField] private Color _outlineColor = new Color(1f, 0.9f, 0.3f, 1f);
    [Tooltip("0이면 깜빡이지 않습니다.")]
    [SerializeField, Min(0f)] private float _pulseSpeed = 4f;
    [SerializeField, Range(0f, 1f)] private float _pulseMinAlpha = 0.4f;

    [Header("입자")]
    [SerializeField, Min(0f)] private float _dustPerSecond = 15f;
    [SerializeField, Min(1)] private int _maxPointTryCount = 10;
    [SerializeField, Min(1)] private int _maxEmitPerFrame = 5;
    [Tooltip("빛 밝기를 따라가는 속도입니다. 0이면 즉시 따라갑니다.")]
    [SerializeField, Min(0f)] private float _dustFadeSpeed = 4f;

    private bool _isVisible;
    private float _emitAccumulator;
    private List<Vector2> _pathPoints = new List<Vector2>();
    private ParticleSystem.EmitParams _emitParams;
    private ParticleSystemRenderer _dustRenderer;
    private MaterialPropertyBlock _dustPropertyBlock;
    private int _fadeId;
    private float _dustFade;
    private Color _dustBaseColor = Color.white;


    private void Reset()
    {
        LightZone_Target = GetComponent<LightZone>();
        Collider2D_Zone = GetComponent<Collider2D>();
        LineRenderer_Outline = GetComponentInChildren<LineRenderer>();
        ParticleSystem_Dust = GetComponentInChildren<ParticleSystem>();
        Light2D_Zone = GetComponentInChildren<Light2D>();
    }

    private void Awake()
    {
        if (LightZone_Target == null)
        {
            Debug.LogError($"[LightZoneVisual] (Awake) - LightZone 없음: {gameObject.name}");
        }

        if (Collider2D_Zone == null)
        {
            Debug.LogError($"[LightZoneVisual] (Awake) - Collider2D 없음: {gameObject.name}");
            _isOutlineEnabled = false;
            _isDustEnabled = false;
        }

        SetupOutline();
        SetupDust();
    }

    private void OnEnable()
    {
        if (LightZone_Target == null)
        {
            return;
        }

        LightZone_Target.OnChangeLightState += HandleLightStateChanged;

        // LightZone이 먼저 켜져서 이벤트를 놓쳤을 수 있으므로 현재 상태를 맞춥니다.
        ApplyVisual(LightZone_Target.IsOn);
    }

    private void OnDisable()
    {
        if (LightZone_Target == null)
        {
            return;
        }

        LightZone_Target.OnChangeLightState -= HandleLightStateChanged;
    }

    private void LateUpdate()
    {
        // 입자는 빛 상태와 상관없이 계속 생성하고, 밝기는 빛을 따라갑니다.
        // 빛이 꺼지거나 예고 중일 때도 동작해야 하므로 _isVisible 확인보다 먼저 실행합니다.
        if (_isDustEnabled)
        {
            EmitDust();
            UpdateDustFade();
        }

        if (!_isVisible)
        {
            return;
        }

        if (_isOutlineEnabled)
        {
            UpdateOutline();
            UpdatePulse();
        }
    }

    #region 초기 설정
    private void SetupOutline()
    {
        if (!_isOutlineEnabled)
        {
            if (LineRenderer_Outline != null)
            {
                LineRenderer_Outline.enabled = false;
            }
            return;
        }

        if (LineRenderer_Outline == null)
        {
            Debug.LogWarning($"[LightZoneVisual] (SetupOutline) - 외곽선 켜짐인데 LineRenderer 없음, 외곽선 생략: {gameObject.name}");
            _isOutlineEnabled = false;
            return;
        }

        LineRenderer_Outline.loop = true;
        LineRenderer_Outline.useWorldSpace = false;
        LineRenderer_Outline.startWidth = _outlineWidth;
        LineRenderer_Outline.endWidth = _outlineWidth;
        LineRenderer_Outline.startColor = _outlineColor;
        LineRenderer_Outline.endColor = _outlineColor;
        LineRenderer_Outline.enabled = false;
    }

    private void SetupDust()
    {
        if (!_isDustEnabled)
        {
            if (ParticleSystem_Dust != null)
            {
                ParticleSystem_Dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            return;
        }

        if (ParticleSystem_Dust == null)
        {
            Debug.LogWarning($"[LightZoneVisual] (SetupDust) - 입자 켜짐인데 ParticleSystem 없음, 입자 생략: {gameObject.name}");
            _isDustEnabled = false;
            return;
        }

        _dustRenderer = ParticleSystem_Dust.GetComponent<ParticleSystemRenderer>();
        if (_dustRenderer == null)
        {
            Debug.LogWarning($"[LightZoneVisual] (SetupDust) - ParticleSystemRenderer 없음, 입자 생략: {gameObject.name}");
            _isDustEnabled = false;
            return;
        }

        // 자동 생성은 끄고, 구역 안에서 직접 생성(Emit)합니다.
        ParticleSystem.EmissionModule emission = ParticleSystem_Dust.emission;
        emission.enabled = false;

        _emitParams = new ParticleSystem.EmitParams();
        _emitParams.applyShapeToPosition = false;

        _dustRenderer.enabled = false;
        ParticleSystem_Dust.Play();

        _dustPropertyBlock = new MaterialPropertyBlock();
        _fadeId = Shader.PropertyToID("_BaseColor");

        Material dustMaterial = _dustRenderer.sharedMaterial;
        if (dustMaterial != null && dustMaterial.HasProperty(_fadeId))
        {
            _dustBaseColor = dustMaterial.GetColor(_fadeId);
        }
        else
        {
            Debug.LogWarning($"[LightZoneVisual] (SetupDust) - 입자 머티리얼에 _BaseColor 없음, 밝기 단계 미적용: {gameObject.name}");
        }
    }
    #endregion

    #region 상태 반영
    private void HandleLightStateChanged(bool isOn)
    {
        ApplyVisual(isOn);
    }

    private void ApplyVisual(bool isOn)
    {
        _isVisible = isOn;

        if (_isOutlineEnabled)
        {
            if (isOn)
            {
                // 켜지는 순간의 모양을 먼저 그려서 이전 모양이 한 프레임 보이는 것을 막습니다.
                UpdateOutline();
            }

            LineRenderer_Outline.enabled = isOn;
        }
    }
    #endregion

    #region 외곽선
    private void UpdateOutline()
    {
        if (!TryGetColliderPoints())
        {
            return;
        }

        LineRenderer_Outline.positionCount = _pathPoints.Count;

        for (int i = 0; i < _pathPoints.Count; i++)
        {
            Vector3 worldPoint = Collider2D_Zone.transform.TransformPoint(_pathPoints[i]);
            Vector3 localPoint = LineRenderer_Outline.transform.InverseTransformPoint(worldPoint);
            LineRenderer_Outline.SetPosition(i, localPoint);
        }
    }

    private void UpdatePulse()
    {
        Color color = _outlineColor;

        if (_pulseSpeed > 0f)
        {
            float wave = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f;
            color.a = _outlineColor.a * Mathf.Lerp(_pulseMinAlpha, 1f, wave);
        }

        LineRenderer_Outline.startColor = color;
        LineRenderer_Outline.endColor = color;
    }

    /// <summary>
    /// 콜라이더 모양의 꼭짓점을 콜라이더 기준 좌표로 _pathPoints에 채웁니다.
    /// 콜라이더가 꺼져 있어도 모양 정보는 읽을 수 있습니다.
    /// </summary>
    private bool TryGetColliderPoints()
    {
        _pathPoints.Clear();

        if (Collider2D_Zone is BoxCollider2D box)
        {
            Vector2 half = box.size * 0.5f;
            Vector2 offset = box.offset;

            _pathPoints.Add(offset + new Vector2(-half.x, -half.y));
            _pathPoints.Add(offset + new Vector2(half.x, -half.y));
            _pathPoints.Add(offset + new Vector2(half.x, half.y));
            _pathPoints.Add(offset + new Vector2(-half.x, half.y));
            return true;
        }

        if (Collider2D_Zone is PolygonCollider2D polygon)
        {
            if (polygon.pathCount == 0)
            {
                return false;
            }

            polygon.GetPath(0, _pathPoints);

            for (int i = 0; i < _pathPoints.Count; i++)
            {
                _pathPoints[i] += polygon.offset;
            }

            return _pathPoints.Count >= 3;
        }

        Debug.LogWarning($"[LightZoneVisual] (TryGetColliderPoints) - 지원하지 않는 콜라이더(Box/Polygon만 가능), 효과 끔: {gameObject.name}");
        _isOutlineEnabled = false;
        _isDustEnabled = false;

        if (LineRenderer_Outline != null)
        {
            LineRenderer_Outline.enabled = false;
        }

        return false;
    }
    #endregion

    #region 입자
    private void EmitDust()
    {
        _emitAccumulator += _dustPerSecond * Time.deltaTime;

        // 프레임이 끊겼을 때 한 번에 너무 많이 생성되지 않도록 제한합니다.
        _emitAccumulator = Mathf.Min(_emitAccumulator, _maxEmitPerFrame);

        if (_emitAccumulator < 1f)
        {
            return;
        }

        // 이번 프레임의 구역 모양을 한 번만 읽어 둡니다.
        if (!TryGetColliderPoints())
        {
            return;
        }

        while (_emitAccumulator >= 1f)
        {
            _emitAccumulator -= 1f;

            if (!TryGetRandomPointInZone(out Vector2 point))
            {
                continue;
            }

            if (ParticleSystem_Dust.main.simulationSpace == ParticleSystemSimulationSpace.World)
            {
                _emitParams.position = point;
            }
            else
            {
                _emitParams.position = ParticleSystem_Dust.transform.InverseTransformPoint(point);
            }

            ParticleSystem_Dust.Emit(_emitParams, 1);
        }
    }

    /// <summary>
    /// _pathPoints로 만든 모양 안쪽의 무작위 점을 월드 좌표로 돌려줍니다.
    /// 콜라이더가 꺼져 있어도 동작합니다.
    /// </summary>
    private bool TryGetRandomPointInZone(out Vector2 point)
    {
        Vector2 min = _pathPoints[0];
        Vector2 max = _pathPoints[0];

        for (int i = 1; i < _pathPoints.Count; i++)
        {
            min = Vector2.Min(min, _pathPoints[i]);
            max = Vector2.Max(max, _pathPoints[i]);
        }

        for (int i = 0; i < _maxPointTryCount; i++)
        {
            Vector2 localPoint = new Vector2(Random.Range(min.x, max.x), Random.Range(min.y, max.y));

            if (IsPointInPath(localPoint))
            {
                point = Collider2D_Zone.transform.TransformPoint(localPoint);
                return true;
            }
        }

        point = Vector2.zero;
        return false;
    }

    /// <summary>
    /// 점에서 오른쪽으로 선을 그었을 때 테두리와 몇 번 만나는지 세서, 홀수면 안쪽으로 판단합니다.
    /// </summary>
    private bool IsPointInPath(Vector2 point)
    {
        bool isInside = false;
        int count = _pathPoints.Count;

        for (int i = 0, j = count - 1; i < count; j = i, i++)
        {
            Vector2 a = _pathPoints[i];
            Vector2 b = _pathPoints[j];

            bool isCrossingY = (a.y > point.y) != (b.y > point.y);
            if (isCrossingY)
            {
                float crossX = (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (point.x < crossX)
                {
                    isInside = !isInside;
                }
            }
        }

        return isInside;
    }

    /// <summary>
    /// 빛의 현재 밝기 비율(꺼짐 0, 예고 약 0.3, 켜짐 1)에 맞춰 입자 알파를 조절합니다.
    /// </summary>
    private void UpdateDustFade()
    {
        float target = GetLightRatio();

        if (_dustFadeSpeed <= 0f)
        {
            _dustFade = target;
        }
        else
        {
            _dustFade = Mathf.MoveTowards(_dustFade, target, _dustFadeSpeed * Time.deltaTime);
        }

        bool isShown = _dustFade > 0.001f;
        if (_dustRenderer.enabled != isShown)
        {
            _dustRenderer.enabled = isShown;
        }

        if (!isShown)
        {
            return;
        }

        Color fadeColor = _dustBaseColor;
        fadeColor.a *= _dustFade;

        _dustRenderer.GetPropertyBlock(_dustPropertyBlock);
        _dustPropertyBlock.SetColor(_fadeId, fadeColor);
        _dustRenderer.SetPropertyBlock(_dustPropertyBlock);
    }

    private float GetLightRatio()
    {
        // 빛 참조가 없으면 켜짐/꺼짐만 따릅니다.
        if (Light2D_Zone == null || LightZone_Target == null)
        {
            return _isVisible ? 1f : 0f;
        }

        if (!Light2D_Zone.enabled)
        {
            return 0f;
        }

        float fullIntensity = LightZone_Target.LightIntensity;
        if (fullIntensity <= 0f)
        {
            return _isVisible ? 1f : 0f;
        }

        return Mathf.Clamp01(Light2D_Zone.intensity / fullIntensity);
    }
    #endregion
}