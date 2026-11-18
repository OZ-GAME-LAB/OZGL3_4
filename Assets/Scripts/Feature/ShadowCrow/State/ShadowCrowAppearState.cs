using UnityEngine;

public class ShadowCrowAppearState : ShadowCrowStateBase
{
    private readonly int _animationHash = Animator.StringToHash("CrowAppear");

    public ShadowCrowAppearState(ShadowCrow crow) : base(crow)
    {
    }

    public override void Enter()
    {
        _crow.SetVisible(true);
        _crow.PlayAnimation(_animationHash);
    }

    public override void Update()
    {
        if (_crow.IsAnimationFinished(_animationHash))
        {
            _crow.ChangeState(ShadowCrowState.Idle);
        }
    }
}
