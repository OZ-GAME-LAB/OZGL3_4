using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 회전 광원의 각도 단계 하나입니다. 해당 각도에 도착하면 연결된 그림자 오브젝트가 켜집니다.
/// </summary>
[Serializable]
public class AngleStep
{
    [SerializeField] private float _angle;
    [SerializeField] private List<GameObject> List_ShadowObject = new List<GameObject>();

    public float Angle
    {
        get { return _angle; }
    }

    public List<GameObject> ShadowObjects
    {
        get { return List_ShadowObject; }
    }
}

/// <summary>
/// 회전 광원 기믹입니다. 램프(Pivot)가 정해진 각도 단계 사이를 회전하고,
/// 도착한 각도에 맞는 그림자 오브젝트(그림자 발판 등)만 켭니다.
///
/// 구조
/// Gimmick_RotatingLight        ← RotatingLight, 스케일 (1, 1, 1)
/// └─ Pivot                     ← 회전 중심 (각도 0 = 아래를 비춤)
///     ├─ Visual                ← 램프 그림
///     └─ Gimmick_LightZone     ← 빛 영역 프리팹 (함께 회전, 아래 방향으로 배치)
///
/// - List_Step에 각도(도)와 그 각도에서 생길 그림자 오브젝트를 등록합니다.
/// - 그림자 오브젝트는 회전을 시작하면 바로 꺼지고, 목표 각도에 도착하면 켜집니다.
/// - 오브젝트를 선택하면 씬 뷰에 각 단계의 방향이 표시됩니다. (초록: 현재, 노랑: 나머지)
///
/// 신호 모드
/// - Advance: 신호가 올 때마다 다음 단계로 회전합니다. (스위치를 누를 때마다 다음 각도)
///   스위치가 시작할 때 보내는 첫 신호는 상태 기록용이라 회전하지 않습니다.
/// - OnOff: 신호 켜짐 → 마지막 단계, 꺼짐 → 첫 단계
/// - Continuous: 신호 켜짐 → 일정 속도로 계속 회전, 꺼짐 → 회전 정지
///   _isSnapToStepOnStop이 켜져 있으면 멈출 때 가장 가까운 단계 각도로 맞춰 들어가고 그 단계의 그림자가 켜집니다.
///   스위치와 연결할 때는 스위치의 Is Start On으로 시작 시 회전 여부를 정합니다. (꺼짐 = 멈춘 상태로 시작)
/// </summary>
public class RotatingLight : MonoBehaviour, ISignalReceiver
{
    public enum SignalMode
    {
        Advance,    // 신호가 올 때마다 다음 단계
        OnOff,      // 켜짐 → 마지막 단계, 꺼짐 → 첫 단계
        Continuous, // 켜짐 → 계속 회전, 꺼짐 → 정지
    }

    [Header("Reference")]
    [SerializeField] private Transform Transform_Pivot;

    [Header("Steps")]
    [SerializeField] private List<AngleStep> List_Step = new List<AngleStep>();
    [SerializeField] private int _startStepIndex = 0;
    [SerializeField] private float _rotateSpeed = 90f;

    [Header("Signal")]
    [SerializeField] private SignalMode _signalMode = SignalMode.Advance;

    [Header("Continuous (Continuous 모드 전용)")]
    [SerializeField] private float _continuousSpeed = 45f; // 초당 각도. 음수면 반대 방향
    [SerializeField] private bool _isSnapToStepOnStop = true;
    [SerializeField] private bool _isStartRotating = false;

    private const float ArriveThreshold = 0.05f;
    private const float GizmoLineLength = 3f;

    private int _currentStepIndex;
    private bool _hasReceivedSignal;

    public int CurrentStepIndex
    {
        get { return _currentStepIndex; }
    }

    // 단계 사이를 이동 중인지 (Advance, OnOff, 멈출 때 단계 맞추기)
    public bool IsRotating { get; private set; }

    // Continuous 모드에서 계속 회전 중인지
    public bool IsContinuousRotating { get; private set; }

    // 목표 각도에 도착했을 때 호출됩니다. (효과음, 연출 연결용)
    public event Action<int> OnArriveStep;

    private void Reset()
    {
        if (transform.childCount > 0)
        {
            Transform_Pivot = transform.GetChild(0);
        }
    }

    private void OnEnable()
    {
        // 풀에서 다시 꺼낼 때도 시작 상태로 즉시 돌아갑니다.
        _hasReceivedSignal = false;
        IsRotating = false;
        IsContinuousRotating = false;

        if (Transform_Pivot == null)
            return;

        if (List_Step.Count > 0)
        {
            _currentStepIndex = Mathf.Clamp(_startStepIndex, 0, List_Step.Count - 1);
            SetPivotAngle(List_Step[_currentStepIndex].Angle);
            ApplyShadowObjects(_currentStepIndex);
        }

        if (_signalMode == SignalMode.Continuous && _isStartRotating)
        {
            StartContinuousRotation();
        }
    }

    private void Update()
    {
        if (Transform_Pivot == null)
            return;

        if (IsContinuousRotating)
        {
            float continuousAngle = Transform_Pivot.localEulerAngles.z + _continuousSpeed * Time.deltaTime;
            SetPivotAngle(continuousAngle);
            return;
        }

        if (IsRotating == false)
            return;

        float targetAngle = List_Step[_currentStepIndex].Angle;
        float currentAngle = Transform_Pivot.localEulerAngles.z;
        float nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, _rotateSpeed * Time.deltaTime);
        SetPivotAngle(nextAngle);

        if (Mathf.Abs(Mathf.DeltaAngle(nextAngle, targetAngle)) > ArriveThreshold)
            return;

        SetPivotAngle(targetAngle);
        IsRotating = false;
        ApplyShadowObjects(_currentStepIndex);

        if (OnArriveStep != null)
        {
            OnArriveStep.Invoke(_currentStepIndex);
        }
    }

    /// <summary>
    /// 신호를 받아 회전합니다. 동작 방식은 _signalMode를 따릅니다.
    /// </summary>
    public void ReceiveSignal(bool isOn)
    {
        if (_signalMode == SignalMode.Continuous)
        {
            if (isOn)
            {
                StartContinuousRotation();
            }
            else
            {
                StopContinuousRotation();
            }
            return;
        }

        if (_signalMode == SignalMode.OnOff)
        {
            ChangeStep(isOn ? List_Step.Count - 1 : 0);
            return;
        }

        // Advance 모드: 스위치가 시작할 때 보내는 첫 신호는 기록만 하고 회전하지 않습니다.
        if (_hasReceivedSignal == false)
        {
            _hasReceivedSignal = true;
            return;
        }

        NextStep();
    }

    /// <summary>
    /// 다음 단계로 회전합니다. 마지막 단계에서는 첫 단계로 돌아갑니다.
    /// </summary>
    public void NextStep()
    {
        if (List_Step.Count == 0)
            return;

        ChangeStep((_currentStepIndex + 1) % List_Step.Count);
    }

    /// <summary>
    /// 지정한 단계로 회전합니다.
    /// </summary>
    public void ChangeStep(int stepIndex)
    {
        if (List_Step.Count == 0 || Transform_Pivot == null)
            return;

        stepIndex = Mathf.Clamp(stepIndex, 0, List_Step.Count - 1);
        if (stepIndex == _currentStepIndex && IsContinuousRotating == false)
            return;

        IsContinuousRotating = false;
        _currentStepIndex = stepIndex;

        // 회전을 시작하면 그림자가 사라지고, 도착하면 새 그림자가 생깁니다.
        DeactivateAllShadowObjects();
        IsRotating = true;
    }

    /// <summary>
    /// 일정 속도(_continuousSpeed)로 계속 회전을 시작합니다. 회전하는 동안 그림자 오브젝트는 모두 꺼집니다.
    /// </summary>
    public void StartContinuousRotation()
    {
        if (Transform_Pivot == null || IsContinuousRotating)
            return;

        IsRotating = false;
        IsContinuousRotating = true;
        DeactivateAllShadowObjects();
    }

    /// <summary>
    /// 계속 회전을 멈춥니다. _isSnapToStepOnStop이 켜져 있으면 가장 가까운 단계 각도로 맞춰 들어갑니다.
    /// </summary>
    public void StopContinuousRotation()
    {
        if (IsContinuousRotating == false)
            return;

        IsContinuousRotating = false;

        if (_isSnapToStepOnStop == false || List_Step.Count == 0)
            return;

        // 가장 가까운 단계로 이동하고, 도착하면 그 단계의 그림자가 켜집니다. (Update에서 처리)
        _currentStepIndex = FindNearestStepIndex(Transform_Pivot.localEulerAngles.z);
        IsRotating = true;
    }

    private int FindNearestStepIndex(float angle)
    {
        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < List_Step.Count; i++)
        {
            float distance = Mathf.Abs(Mathf.DeltaAngle(angle, List_Step[i].Angle));
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private void SetPivotAngle(float angle)
    {
        Vector3 euler = Transform_Pivot.localEulerAngles;
        Transform_Pivot.localEulerAngles = new Vector3(euler.x, euler.y, angle);
    }

    private void ApplyShadowObjects(int activeStepIndex)
    {
        // 여러 단계에 같은 오브젝트가 들어 있어도 올바르게 켜지도록 먼저 모두 끄고 켭니다.
        DeactivateAllShadowObjects();

        List<GameObject> activeObjects = List_Step[activeStepIndex].ShadowObjects;
        for (int i = 0; i < activeObjects.Count; i++)
        {
            if (activeObjects[i] != null)
            {
                activeObjects[i].SetActive(true);
            }
        }
    }

    private void DeactivateAllShadowObjects()
    {
        for (int i = 0; i < List_Step.Count; i++)
        {
            List<GameObject> shadowObjects = List_Step[i].ShadowObjects;
            for (int j = 0; j < shadowObjects.Count; j++)
            {
                if (shadowObjects[j] != null)
                {
                    shadowObjects[j].SetActive(false);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Transform_Pivot == null)
            return;

        Quaternion parentRotation = Transform_Pivot.parent != null ? Transform_Pivot.parent.rotation : Quaternion.identity;

        for (int i = 0; i < List_Step.Count; i++)
        {
            bool isCurrent = Application.isPlaying ? i == _currentStepIndex : i == _startStepIndex;
            Gizmos.color = isCurrent ? Color.green : Color.yellow;

            // 각도 0 = 아래 방향
            Vector3 direction = parentRotation * Quaternion.Euler(0f, 0f, List_Step[i].Angle) * Vector3.down;
            Gizmos.DrawLine(Transform_Pivot.position, Transform_Pivot.position + direction * GizmoLineLength);
        }
    }

    private void OnValidate()
    {
        // Continuous 모드는 단계 없이도 회전할 수 있습니다.
        if (List_Step.Count == 0)
        {
            if (_signalMode != SignalMode.Continuous)
            {
                Debug.LogWarning($"[{name}] 회전 단계(List_Step)를 1개 이상 등록해 주세요.", this);
            }
            return;
        }

        if (_startStepIndex < 0 || _startStepIndex >= List_Step.Count)
        {
            Debug.LogWarning($"[{name}] 시작 단계(_startStepIndex)가 등록된 단계 범위를 벗어났습니다. (0 ~ {List_Step.Count - 1})", this);
        }

        if (_signalMode == SignalMode.Advance && List_Step.Count < 2)
        {
            Debug.LogWarning($"[{name}] Advance 모드는 단계가 2개 이상이어야 회전합니다.", this);
        }
    }
}
