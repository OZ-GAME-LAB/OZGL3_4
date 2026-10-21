/// <summary>
/// 신호를 받아 동작하는 기믹이 구현하는 인터페이스입니다. (빛 영역, 문 등)
/// </summary>
public interface ISignalReceiver
{
    /// <summary>
    /// 신호를 받았을 때 호출됩니다. isOn이 true면 켜짐, false면 꺼짐입니다.
    /// </summary>
    void ReceiveSignal(bool isOn);
}
