using UnityEngine;

// 그림자 수레가 바닥에 걸리거나 위로 밀려 올라가지 않도록 높이를 고정하는 컴포넌트
// 사용법: Gimmick_ShadowCart 프리팹의 루트(Rigidbody2D, Grabbable이 있는 오브젝트)에 붙인다.
// - 중력을 끄고 세로 위치를 처음 높이에 고정한다. 가로로는 지금처럼 잡고 밀거나 끌 수 있다.
// - 벽이나 낮은 턱과의 충돌은 그대로라서, 수레가 지형을 뚫고 지나가지는 않는다.
// - Grabbable이 잡을 때/놓을 때 고정 설정을 다시 쓰기 때문에, 물리 계산 직전(FixedUpdate)마다 세로 고정을 다시 건다.
[RequireComponent(typeof(Rigidbody2D))]
public class ShadowCart : MonoBehaviour
{
    private Rigidbody2D _rigid;
    private float _fixedLocalY;

    private void Awake()
    {
        _rigid = GetComponent<Rigidbody2D>();
        _rigid.gravityScale = 0f;
        _fixedLocalY = transform.localPosition.y;
    }

    private void FixedUpdate()
    {
        if ((_rigid.constraints & RigidbodyConstraints2D.FreezePositionY) == 0)
        {
            _rigid.constraints |= RigidbodyConstraints2D.FreezePositionY;
        }

        if (_rigid.linearVelocity.y != 0f)
        {
            _rigid.linearVelocity = new Vector2(_rigid.linearVelocity.x, 0f);
        }

        // 한 프레임이라도 높이가 어긋났다면 처음 높이로 되돌린다
        Vector3 localPosition = transform.localPosition;
        if (Mathf.Abs(localPosition.y - _fixedLocalY) > 0.001f)
        {
            localPosition.y = _fixedLocalY;
            transform.localPosition = localPosition;
        }
    }
}
