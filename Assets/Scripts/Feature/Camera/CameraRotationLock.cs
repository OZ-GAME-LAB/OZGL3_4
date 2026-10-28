using UnityEngine;

// 부모(Player)가 경사면에서 회전해도 자식 카메라의 회전·위치가 따라 돌지 않도록 고정하는 컴포넌트
// 사용법: Player 하위의 카메라 오브젝트에 붙이기만 하면 된다
public class CameraRotationLock : MonoBehaviour
{
    [Header("Lock Option")]
    // 켜두면 부모가 기울어도 카메라가 옆으로 밀리지 않도록 위치도 보정한다
    [SerializeField] private bool _isLockPosition = true;

    // 시작 시점의 월드 회전값 (계속 유지할 회전)
    private Quaternion _fixedRotation;

    // 시작 시점의 부모 기준 월드 오프셋 (부모 회전과 무관하게 유지할 거리)
    private Vector3 _worldOffset;

    private Transform _parent;

    private void Awake()
    {
        _fixedRotation = transform.rotation;
        _parent = transform.parent;

        if (_parent == null)
        {
            Debug.LogWarning($"[CameraRotationLock] {gameObject.name}에 부모가 없습니다. 위치 보정은 동작하지 않고 회전만 고정합니다.");
            return;
        }

        _worldOffset = transform.position - _parent.position;
    }

    private void LateUpdate()
    {
        // 플레이어의 이동·회전이 모두 끝난 뒤에 덮어써야 떨림이 없다
        LockRotation();

        if (_isLockPosition)
        {
            LockPosition();
        }
    }

    private void LockRotation()
    {
        transform.rotation = _fixedRotation;
    }

    private void LockPosition()
    {
        if (_parent == null)
            return;

        transform.position = _parent.position + _worldOffset;
    }
}
