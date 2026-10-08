using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private float _speed = 50f;
    private float _jumpPower = 1f;

    private Vector2 _movement = Vector2.zero;

    private PlayerInput _playerInput;
    private Rigidbody2D _rigid;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _rigid = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        _movement = _movement.normalized;
        float xOffset = _movement.x * _speed * Time.fixedDeltaTime;

        _rigid.linearVelocity = new Vector2(xOffset, _rigid.linearVelocity.y);
    }

    private void OnMove(InputValue value)
    {
        _movement = value.Get<Vector2>();
    }
}
