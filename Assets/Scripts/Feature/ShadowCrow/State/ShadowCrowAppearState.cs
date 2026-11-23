public class ShadowCrowAppearState : ShadowCrowStateBase
{
    private int _animationHash;

    public ShadowCrowAppearState(ShadowCrow crow) : base(crow)
    {
    }

    public override void Enter()
    {
        _animationHash = _crow.AppearAnimationHash;

        _crow.SetVisible(true);
        _crow.PlayAnimation(_animationHash);
        _crow.StartAppearDirecting();
        _crow.NotifyStartAppear();
    }

    public override void Update()
    {
        if (_crow.IsAnimationFinished(_animationHash))
        {
            _crow.ChangeState(ShadowCrowState.Idle);
        }
    }
}
