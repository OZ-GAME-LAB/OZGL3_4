using UnityEngine;

public class ShadowCrowIdleState : ShadowCrowStateBase
{
    private readonly int _animationHash = Animator.StringToHash("CrowIdle");

    public ShadowCrowIdleState(ShadowCrow crow) : base(crow)
    {
    }

    public override void Enter()
    {
        _crow.PlayAnimation(_animationHash);
        _crow.SetInteractable(true);
    }
}
