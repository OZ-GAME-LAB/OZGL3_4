using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 테스트 전용입니다. 지정한 키를 누르면 연결된 기믹에 켜기/끄기 신호를 번갈아 보냅니다.
/// 스위치 기믹이 완성되기 전 테스트용이며, 실제 맵에는 배치하지 마세요.
/// </summary>
public class DebugSignalSender : SignalSender
{
    [SerializeField] private Key _toggleKey = Key.T;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[_toggleKey].wasPressedThisFrame)
        {
            SendSignal(!IsOn);
        }
    }
}
