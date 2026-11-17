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
        if (_crow.IsPlayerInRange())
        {
            _crow.ChangeState(ShadowCrowState.Appear);
        }
    }
}
