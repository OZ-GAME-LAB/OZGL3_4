/// <summary>
/// 토글 신호를 받아 동작하는 기믹이 구현하는 인터페이스입니다. (문 등)
/// 켜짐/꺼짐 값을 받는 ISignalReceiver 와 달리, 호출될 때마다 현재 상태의 반대로 바뀝니다.
/// </summary>
public interface IToggleReceiver
{
    /// <summary>
    /// 토글 신호를 받았을 때 호출됩니다. 열려 있으면 닫히고, 닫혀 있으면 열립니다.
    /// </summary>
    void ToggleSignal();
}
