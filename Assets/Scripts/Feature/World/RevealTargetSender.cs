using UnityEngine;
using UnityEngine.Tilemaps;

// 비밀방 가림막(Reveal 셰이더)에 플레이어 위치를 알려 주는 컴포넌트. 가림막 Tilemap과 같은 오브젝트에 붙인다.
// - 플레이어가 가림막 칸 위에 들어와 있을 때만 실제 위치를 넘긴다. (들어서는 순간부터 주변이 비쳐 보임)
// - 가림막 밖에 있을 때는 아주 먼 좌표를 넘겨서, 가까이 다가가도 안쪽이 미리 비치지 않게 한다.
// - 셰이더 값(_PlayerPosition)은 모든 가림막이 함께 쓰므로, 어느 한 가림막에라도 들어가 있으면 위치를 넘긴다.
public class RevealTargetSender : MonoBehaviour
{
    private static readonly Vector4 _hiddenPosition = new Vector4(99999f, 99999f, 0f, 0f);

    // 같은 프레임에 여러 가림막이 판정한 결과를 모은다
    private static int _checkedFrame = -1;
    private static bool _isPlayerInsideAnyCover;

    private int _playerPositionId;
    private bool _isWarned;
    private Tilemap _tilemap;
    private Transform _cachedTarget;
    private Collider2D _cachedTargetCollider;

    private void Awake()
    {
        _playerPositionId = Shader.PropertyToID("_PlayerPosition");
        _tilemap = GetComponent<Tilemap>();
    }

    private void LateUpdate()
    {
        if (MapManager.Instance == null || MapManager.Instance.Transform_Target == null)
        {
            if (_isWarned == false)
            {
                Debug.LogWarning("[RevealTargetSender] (LateUpdate) : MapManager 또는 Transform_Target 없음");

                _isWarned = true;
            }

            return;
        }

        _isWarned = false;

        Transform target = MapManager.Instance.Transform_Target;

        // 프레임이 바뀌면 판정 결과를 새로 모은다
        if (Time.frameCount != _checkedFrame)
        {
            _checkedFrame = Time.frameCount;
            _isPlayerInsideAnyCover = false;
        }

        if (IsInsideCover(GetTargetCenter(target)))
        {
            _isPlayerInsideAnyCover = true;
        }

        Vector4 position = _isPlayerInsideAnyCover ? (Vector4)target.position : _hiddenPosition;
        Shader.SetGlobalVector(_playerPositionId, position);
    }

    private void OnDisable()
    {
        Shader.SetGlobalVector(_playerPositionId, _hiddenPosition);
    }

    // 플레이어 몸 중심이 이 가림막의 타일 칸 위에 있는지 확인한다
    private bool IsInsideCover(Vector3 worldPosition)
    {
        if (_tilemap == null)
            return false;

        return _tilemap.HasTile(_tilemap.WorldToCell(worldPosition));
    }

    // 발끝 좌표는 바닥 칸과 겹칠 수 있으므로 콜라이더 중심을 쓴다. 콜라이더가 없으면 위치를 그대로 쓴다.
    private Vector3 GetTargetCenter(Transform target)
    {
        if (_cachedTarget != target)
        {
            _cachedTarget = target;
            _cachedTargetCollider = target.GetComponent<Collider2D>();
        }

        if (_cachedTargetCollider != null)
            return _cachedTargetCollider.bounds.center;

        return target.position;
    }
}
