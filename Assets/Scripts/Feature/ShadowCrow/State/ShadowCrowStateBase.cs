public abstract class ShadowCrowStateBase : IState
{
    protected readonly ShadowCrow _crow;

    protected ShadowCrowStateBase(ShadowCrow crow)
    {
        _crow = crow;
    }

    public virtual void Enter()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void FixedUpdate()
    {
    }

    public virtual void Exit()
    {
    }
}
