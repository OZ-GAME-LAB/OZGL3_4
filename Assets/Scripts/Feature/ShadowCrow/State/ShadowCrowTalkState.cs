using UnityEngine;

public class ShadowCrowTalkState : ShadowCrowStateBase
{
    private readonly int _animationHash = Animator.StringToHash("CrowTalk");

    public ShadowCrowTalkState(ShadowCrow crow) : base(crow)
    {
    }

    public override void Enter()
    {
        _crow.PlayAnimation(_animationHash);
        _crow.BindTalkInput();
        _crow.StartTalkDirecting();
    }

    public override void Update()
    {
        if (_crow.IsDialogPlaying == false)
        {
            _crow.ChangeState(ShadowCrowState.Idle);
        }
    }

    public override void Exit()
    {
        _crow.UnBindTalkInput();
        _crow.EndTalkDirecting();
    }
}
