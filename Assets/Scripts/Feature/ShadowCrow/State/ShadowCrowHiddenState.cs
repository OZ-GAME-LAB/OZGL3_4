public class ShadowCrowHiddenState : ShadowCrowStateBase
{
    public ShadowCrowHiddenState(ShadowCrow crow) : base(crow)
    {
    }

    public override void Enter()
    {
        _crow.SetVisible(false);
        _crow.SetInteractable(false);
    }

    public override void Update()
    {
        // 플레이어 생성 연출이 끝날 때까지 기다린다. 끝나는 순간 생성 줌아웃을 이 까마귀의 줌인이 이어받는다.
        if (_crow.IsPlayerSpawning())
        {
            return;
        }

        if (_crow.IsPlayerInRange())
        {
            _crow.ChangeState(ShadowCrowState.Appear);
        }
    }
}
