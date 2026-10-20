using UnityEngine;

public class Grabbable : MonoBehaviour
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
}
