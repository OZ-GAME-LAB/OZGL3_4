using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private float _speed = 75f;
    private float _jumpPower = 100f;

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
        float xOffset = _movement.x * _speed * Time.fixedDeltaTime;

        _rigid.linearVelocity = new Vector2(xOffset, _rigid.linearVelocity.y);
    }

    private void OnMove(InputValue value)
    {
        _movement = value.Get<Vector2>();
    }


    private void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            Jump();
        }
    }

    private void Jump()
    {
        _rigid.AddForce(Vector2.up * _jumpPower);
    }
}
