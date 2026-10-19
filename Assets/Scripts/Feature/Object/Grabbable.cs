using UnityEngine;

public class Grabbable : MonoBehaviour
{
    private float _speedMultiplier = 0.6f;
    public float SpeedMultiplier => _speedMultiplier;

    private Rigidbody2D _rigid;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
    }

    public void SetVelocity(float speed)
    {
        float xOffset = 1 * speed;
        _rigid.linearVelocity = new Vector2(xOffset, _rigid.linearVelocity.y);
    }
}
