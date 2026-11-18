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

    public override void Update()
    {
        // 등장 직후 첫 Idle에서 자동 대화를 시작한다. (한 번 시작하면 다시 호출돼도 무시됨)
        _crow.TryStartAutoTalk();
    }
}
