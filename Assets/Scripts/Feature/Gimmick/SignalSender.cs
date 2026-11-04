using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 신호를 보내는 기믹의 공통 부모입니다. (스위치, 발판 등)
/// 인스펙터의 List_Receiver에 ISignalReceiver를 구현한 기믹을 넣어 주세요.
/// 연결된 기믹의 시작 상태는 이 컴포넌트의 _isStartOn 값을 따릅니다. (자식 클래스가 다르게 정할 수 있음)
///
/// Invert Signal을 켜면 연결된 기믹에 반대 신호를 보냅니다.
/// 예) 발판을 밟으면 빛이 꺼지게 하려면: 발판의 Invert Signal 켜기
///     (발판 자체는 밟힘 = 켜짐으로 동작하고, 빛에는 꺼짐 신호가 전달됩니다)
/// </summary>
public abstract class SignalSender : MonoBehaviour
{
    [SerializeField] private List<MonoBehaviour> List_Receiver = new List<MonoBehaviour>();
    [SerializeField] private bool _isStartOn = true;
    [SerializeField] private bool _isInvertSignal = false;

    /// <summary>
    /// 이 기믹 자체의 상태입니다. (Invert Signal과 상관없이 스위치가 켜져 있는지, 발판이 눌려 있는지)
    /// </summary>
    public bool IsOn { get; private set; }

    protected virtual void OnEnable()
    {
        SyncReceiversAsync().Forget();
    }

    /// <summary>
    /// 상태를 바꾸고 연결된 모든 수신 기믹에 신호를 보냅니다. (Invert Signal이 켜져 있으면 반대 신호)
    /// </summary>
    protected void SendSignal(bool isOn)
    {
        IsOn = isOn;

        bool signalToSend = _isInvertSignal ? !isOn : isOn;

        for (int i = 0; i < List_Receiver.Count; i++)
        {
            ISignalReceiver receiver = List_Receiver[i] as ISignalReceiver;
            if (receiver == null)
            {
                continue;
            }

            receiver.ReceiveSignal(signalToSend);
        }

        HandleSignalSent(isOn);
    }

    /// <summary>
    /// 신호를 보낸 직후 호출됩니다. 자식 클래스에서 외형 변경 등에 사용합니다. (시작 상태를 맞출 때도 호출됩니다.)
    /// 전달되는 값은 이 기믹 자체의 상태입니다. (Invert Signal 적용 전)
    /// </summary>
    protected virtual void HandleSignalSent(bool isOn)
    {
    }

    /// <summary>
    /// 시작할 때 연결된 기믹에 보낼 상태입니다. 기본은 _isStartOn 값이며,
    /// 자식 클래스에서 실제 상태로 바꿀 수 있습니다. (예: 발판은 눌려 있는지 여부)
    /// </summary>
    protected virtual bool GetStartState()
    {
        return _isStartOn;
    }

    /// <summary>
    /// 수신 기믹들이 OnEnable에서 초기화를 마친 뒤 상태를 맞추도록 한 프레임 기다렸다가 신호를 보냅니다.
    /// 풀에서 다시 꺼낼 때도 시작 상태로 돌아갑니다.
    /// </summary>
    private async UniTaskVoid SyncReceiversAsync()
    {
        await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
        SendSignal(GetStartState());
    }

    private void OnValidate()
    {
        for (int i = 0; i < List_Receiver.Count; i++)
        {
            if (List_Receiver[i] == null)
            {
                continue;
            }

            if (List_Receiver[i] is ISignalReceiver)
            {
                continue;
            }

            Debug.LogWarning($"[{name}] {List_Receiver[i].name}의 {List_Receiver[i].GetType().Name}은(는) 신호를 받을 수 없는 컴포넌트입니다. ISignalReceiver를 구현한 컴포넌트를 넣어 주세요.", this);
        }
    }
}
