using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 점멸 광원 기믹입니다. 연결된 빛 영역(LightZone)을 일정 주기로 켜고 끕니다.
/// 켜지기 직전에는 판정 없이 빛이 약하게 깜빡이는 예고(Warning)를 보여줍니다.
///
/// 배치 방법
/// - 빛 영역(Gimmick_LightZone) 부모 오브젝트에 이 컴포넌트를 함께 추가합니다. (LightZone 자동 연결)
/// - 여러 개를 엇갈려 깜빡이게 하려면 _startDelay를 서로 다르게 줍니다.
///
/// 스위치와 연결할 때
/// - 스위치의 List_Receiver에는 LightZone이 아니라 이 BlinkingLight를 넣으세요.
///   신호 켜짐 → 점멸 시작, 신호 꺼짐 → 점멸 정지(빛 꺼짐)
/// </summary>
public class BlinkingLight : MonoBehaviour, ISignalReceiver
{
    [Header("Reference")]
    [SerializeField] private LightZone LightZone_Target;

    [Header("Timing (초)")]
    [SerializeField] private float _onDuration = 1.5f;
    [SerializeField] private float _offDuration = 1.5f;
    [SerializeField] private float _warningDuration = 0.4f;
    [SerializeField] private float _startDelay = 0f;

    [Header("State")]
    [SerializeField] private bool _isStartBlinking = true;

    private CancellationTokenSource _blinkCancellationSource;

    public bool IsBlinking { get; private set; }

    private void Reset()
    {
        LightZone_Target = GetComponent<LightZone>();
    }

    private void OnEnable()
    {
        // 풀에서 다시 꺼낼 때도 시작 상태로 돌아갑니다.
        IsBlinking = false;

        if (_isStartBlinking)
        {
            StartBlinking();
        }
    }

    private void OnDisable()
    {
        CancelBlink();
        IsBlinking = false;
    }

    /// <summary>
    /// 신호 켜짐이면 점멸을 시작하고, 꺼짐이면 점멸을 멈추고 빛을 끕니다.
    /// </summary>
    public void ReceiveSignal(bool isOn)
    {
        if (isOn)
        {
            StartBlinking();
        }
        else
        {
            StopBlinking();
        }
    }

    /// <summary>
    /// 점멸을 시작합니다. 이미 점멸 중이면 아무 일도 하지 않습니다.
    /// </summary>
    public void StartBlinking()
    {
        if (IsBlinking || LightZone_Target == null)
            return;

        IsBlinking = true;

        CancelBlink();
        _blinkCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        BlinkLoopAsync(_blinkCancellationSource.Token).Forget();
    }

    /// <summary>
    /// 점멸을 멈추고 빛을 끕니다.
    /// </summary>
    public void StopBlinking()
    {
        if (IsBlinking == false)
            return;

        IsBlinking = false;
        CancelBlink();

        if (LightZone_Target != null)
        {
            LightZone_Target.ReceiveSignal(false);
        }
    }

    private void CancelBlink()
    {
        if (_blinkCancellationSource == null)
            return;

        _blinkCancellationSource.Cancel();
        _blinkCancellationSource.Dispose();
        _blinkCancellationSource = null;
    }

    private async UniTaskVoid BlinkLoopAsync(CancellationToken cancellationToken)
    {
        // 같은 오브젝트의 LightZone이 OnEnable에서 초기화를 마친 뒤에 시작하도록 한 프레임 기다립니다.
        bool isCanceled = await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken).SuppressCancellationThrow();
        if (isCanceled)
            return;

        if (await WaitAsync(_startDelay, cancellationToken))
            return;

        while (true)
        {
            LightZone_Target.ReceiveSignal(true);
            if (await WaitAsync(_onDuration, cancellationToken))
                return;

            LightZone_Target.ReceiveSignal(false);
            float waitBeforeWarning = Mathf.Max(0f, _offDuration - _warningDuration);
            if (await WaitAsync(waitBeforeWarning, cancellationToken))
                return;

            if (_warningDuration > 0f)
            {
                LightZone_Target.ShowWarning();
                if (await WaitAsync(_warningDuration, cancellationToken))
                    return;
            }
        }
    }

    /// <summary>
    /// 지정한 시간만큼 기다립니다. 중간에 취소되면 true를 돌려줍니다.
    /// </summary>
    private async UniTask<bool> WaitAsync(float seconds, CancellationToken cancellationToken)
    {
        if (seconds <= 0f)
            return cancellationToken.IsCancellationRequested;

        return await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: cancellationToken).SuppressCancellationThrow();
    }

    private void OnValidate()
    {
        if (_warningDuration > _offDuration)
        {
            Debug.LogWarning($"[{name}] 예고 시간(_warningDuration)이 꺼져 있는 시간(_offDuration)보다 깁니다. 예고는 꺼져 있는 시간 안에서만 표시됩니다.", this);
        }
    }
}
