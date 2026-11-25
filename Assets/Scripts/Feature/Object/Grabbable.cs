using UnityEngine;

public class Grabbable : MonoBehaviour, ISaveableGimmick
{
    private float _speedMultiplier = 0.6f;
    public float SpeedMultiplier => _speedMultiplier;

    private Rigidbody2D _rigid;

    private const RigidbodyConstraints2D _lockedConstraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
    private const RigidbodyConstraints2D _grabbedConstraints = RigidbodyConstraints2D.FreezeRotation;


    private void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
        _rigid.constraints = _lockedConstraints;
    }

    public void OnGrabbed()
    {
        _rigid.constraints = _grabbedConstraints;
    }

    public void OnReleased()
    {
        SetVelocity(0);
        _rigid.constraints = _lockedConstraints;
    }

    public void SetVelocity(float speed)
    {
        _rigid.linearVelocity = new Vector2(speed, _rigid.linearVelocity.y);
    }

    // 추가 - 세이브: 밀 수 있는 오브젝트의 위치를 저장하고 복원합니다.
    // 그림자 수레(ShadowCart)는 자기 스크립트에서 위치를 저장하므로 여기서는 등록하지 않습니다.
    private void Start()
    {
        if (GetComponent<ShadowCart>() != null)
            return;

        GimmickSaveRegistry.Register(this, this);
    }

    private void OnDestroy()
    {
        GimmickSaveRegistry.Unregister(this);
    }

    public void CaptureState(GimmickSaveState state)
    {
        state.ValueA = transform.localPosition.x;
        state.ValueB = transform.localPosition.y;
    }

    public void RestoreState(GimmickSaveState state)
    {
        Vector3 localPosition = transform.localPosition;
        localPosition.x = state.ValueA;
        localPosition.y = state.ValueB;
        transform.localPosition = localPosition;

        _rigid.linearVelocity = Vector2.zero;
        _rigid.position = transform.position;
    }
}
