using System.Collections.Generic;

// 플레이어의 현재 형태와 소유한 그림자를 보관한다. 변경 알림은 ViewModel이 담당한다.
public class PlayerModel
{
    public ShadowType CurrentShadowType { get; set; }
    public HashSet<ShadowType> OwnerShadowType { get; set; } = new HashSet<ShadowType>();
}
