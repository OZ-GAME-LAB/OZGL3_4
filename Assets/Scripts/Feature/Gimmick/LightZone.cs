using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 빛 영역 기믹입니다. 빛(Light2D)과 판정용 트리거(Collider2D)를 함께 켜고 끕니다.
/// 피해 처리는 플레이어가 LightZone 레이어를 감지해서 처리합니다.
///
/// 배치 방법
/// 1. 이 오브젝트의 레이어를 LightZone으로 설정합니다.
/// 2. 빛이 닿는 범위에 맞춰 BoxCollider2D(또는 PolygonCollider2D)를 추가하고 Is Trigger를 체크합니다.
/// 3. 자식 오브젝트에 자유형 광원 2D(Freeform Light 2D)를 추가합니다.
/// 4. 컴포넌트 우클릭 → "빛 모양을 콜라이더에 맞추기"를 누르면 빛 모양이 콜라이더와 같아집니다.
///    (게임 실행 시에는 자동으로 맞춰집니다.)
/// </summary>
public class LightZone : MonoBehaviour, ISignalReceiver
{
    private const string LightZoneLayerName = "LightZone";

    [SerializeField] private Light2D Light2D_Zone;
    [SerializeField] private Collider2D Collider2D_Zone;
    [SerializeField] private bool _isStartOn = true;
    [SerializeField] private bool _isSyncShapeWithCollider = true;

    public bool IsOn { get; private set; }

    // 빛이 켜지거나 꺼질 때 호출됩니다. (효과음, 연출 연결용)
    public event Action<bool> OnChangeLightState;

    private void Reset()
    {
        Light2D_Zone = GetComponentInChildren<Light2D>();
        Collider2D_Zone = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (_isSyncShapeWithCollider)
        {
            SyncLightShape();
        }

        // 풀에서 다시 꺼낼 때도 시작 상태로 돌아가도록 OnEnable에서 초기화합니다.
        ApplyState(_isStartOn);
    }

    /// <summary>
    /// 신호를 받아 빛을 켜거나 끕니다.
    /// </summary>
    public void ReceiveSignal(bool isOn)
    {
        ApplyState(isOn);
    }

    /// <summary>
    /// 현재 상태를 반대로 바꿉니다. (점멸 광원 등에서 사용)
    /// </summary>
    public void ToggleLight()
    {
        ApplyState(!IsOn);
    }

    /// <summary>
    /// 자유형 광원(Freeform)의 모양을 콜라이더 모양에 맞춥니다.
    /// 에디터에서는 컴포넌트 우클릭 → "빛 모양을 콜라이더에 맞추기"로 실행할 수 있습니다.
    /// BoxCollider2D와 PolygonCollider2D를 지원합니다.
    /// </summary>
    [ContextMenu("빛 모양을 콜라이더에 맞추기")]
    public void SyncLightShape()
    {
        if (Light2D_Zone == null || Collider2D_Zone == null)
            return;

        if (Light2D_Zone.lightType != Light2D.LightType.Freeform)
        {
            Debug.LogWarning($"[{name}] 빛 모양 맞추기는 자유형 광원(Freeform)에서만 동작합니다.", this);
            return;
        }

        List<Vector2> colliderPoints = GetColliderPoints();
        if (colliderPoints.Count < 3)
            return;

        // 콜라이더 기준 좌표 → 월드 좌표 → 빛 오브젝트 기준 좌표로 변환합니다.
        Vector3[] lightPath = new Vector3[colliderPoints.Count];
        for (int i = 0; i < colliderPoints.Count; i++)
        {
            Vector3 worldPoint = Collider2D_Zone.transform.TransformPoint(colliderPoints[i]);
            lightPath[i] = Light2D_Zone.transform.InverseTransformPoint(worldPoint);
        }

#if UNITY_EDITOR
        if (Application.isPlaying == false)
        {
            Undo.RecordObject(Light2D_Zone, "Sync Light Shape");
        }
#endif

        Light2D_Zone.SetShapePath(lightPath);

#if UNITY_EDITOR
        if (Application.isPlaying == false)
        {
            EditorUtility.SetDirty(Light2D_Zone);
        }
#endif
    }

    private List<Vector2> GetColliderPoints()
    {
        List<Vector2> points = new List<Vector2>();

        BoxCollider2D boxCollider = Collider2D_Zone as BoxCollider2D;
        if (boxCollider != null)
        {
            Vector2 halfSize = boxCollider.size * 0.5f;
            Vector2 offset = boxCollider.offset;

            points.Add(offset + new Vector2(-halfSize.x, -halfSize.y));
            points.Add(offset + new Vector2(halfSize.x, -halfSize.y));
            points.Add(offset + new Vector2(halfSize.x, halfSize.y));
            points.Add(offset + new Vector2(-halfSize.x, halfSize.y));
            return points;
        }

        PolygonCollider2D polygonCollider = Collider2D_Zone as PolygonCollider2D;
        if (polygonCollider != null)
        {
            Vector2[] path = polygonCollider.GetPath(0);
            for (int i = 0; i < path.Length; i++)
            {
                points.Add(path[i] + polygonCollider.offset);
            }
            return points;
        }

        Debug.LogWarning($"[{name}] 빛 모양 맞추기는 BoxCollider2D와 PolygonCollider2D만 지원합니다.", this);
        return points;
    }

    private void ApplyState(bool isOn)
    {
        IsOn = isOn;

        if (Light2D_Zone != null)
        {
            Light2D_Zone.enabled = isOn;
        }

        if (Collider2D_Zone != null)
        {
            Collider2D_Zone.enabled = isOn;
        }

        if (OnChangeLightState != null)
        {
            OnChangeLightState.Invoke(isOn);
        }
    }

    private void OnValidate()
    {
        int lightZoneLayer = LayerMask.NameToLayer(LightZoneLayerName);
        if (lightZoneLayer == -1)
        {
            Debug.LogWarning($"[{name}] '{LightZoneLayerName}' 레이어가 프로젝트에 없습니다. develop 브랜치를 받아와 주세요.", this);
        }
        else if (gameObject.layer != lightZoneLayer)
        {
            Debug.LogWarning($"[{name}] 빛 영역의 레이어를 '{LightZoneLayerName}'으로 설정해 주세요.", this);
        }

        if (Collider2D_Zone != null && Collider2D_Zone.isTrigger == false)
        {
            Debug.LogWarning($"[{name}] 빛 영역의 Collider2D에 Is Trigger를 체크해 주세요.", this);
        }
    }
}
