using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform Target;
    [SerializeField] private Vector2 _offset = new Vector2(0f, 1f);

    [Header("Smoothing")]
    [SerializeField] private float _smoothTime = 0.15f;      // 작을수록 빠르게 따라감

    [Header("Look Ahead (바라보는 방향으로 화면을 더 보여줌)")]
    [SerializeField] private float _lookAheadDistance = 1.5f;
    [SerializeField] private float _lookAheadSmoothTime = 0.4f;

    [Header("Map Bounds (선택)")]
    [SerializeField] private bool _useBounds;
    [SerializeField] private Vector2 _minBounds;
    [SerializeField] private Vector2 _maxBounds;

    private Camera _camera;
    private Vector3 _velocity;
    private float _lookAheadX;
    private float _lookAheadVelocity;
    private float _lastTargetX;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
        if (Target != null)
        {
            SnapToTarget();
        }
    }

    private void LateUpdate()
    {
        if (Target == null)
        {
            return;
        }

        // 이동 방향에 따라 앞쪽을 더 보여줌
        float moveX = Target.position.x - _lastTargetX;
        _lastTargetX = Target.position.x;
        if (Mathf.Abs(moveX) > 0.001f)
        {
            float targetLookAhead = Mathf.Sign(moveX) * _lookAheadDistance;
            _lookAheadX = Mathf.SmoothDamp(_lookAheadX, targetLookAhead, ref _lookAheadVelocity, _lookAheadSmoothTime);
        }

        Vector3 desired = GetDesiredPosition();
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, _smoothTime);
    }

    // 텔레포트 직후 호출: 부드럽게 따라가지 않고 즉시 이동
    public void SnapToTarget()
    {
        _lastTargetX = Target.position.x;
        _lookAheadX = 0f;
        _lookAheadVelocity = 0f;
        _velocity = Vector3.zero;
        transform.position = GetDesiredPosition();
    }

    public void SetTarget(Transform target)
    {
        Target = target;
        SnapToTarget();
    }

    private Vector3 GetDesiredPosition()
    {
        Vector3 desired = new Vector3(
            Target.position.x + _offset.x + _lookAheadX,
            Target.position.y + _offset.y,
            transform.position.z);   // z는 카메라 원래 값 유지 (보통 -10)

        if (_useBounds)
        {
            desired = ClampToBounds(desired);
        }
        return desired;
    }

    // 카메라 화면이 맵 밖을 보여주지 않도록 제한
    private Vector3 ClampToBounds(Vector3 position)
    {
        float halfHeight = _camera.orthographicSize;
        float halfWidth = halfHeight * _camera.aspect;

        float minX = _minBounds.x + halfWidth;
        float maxX = _maxBounds.x - halfWidth;
        float minY = _minBounds.y + halfHeight;
        float maxY = _maxBounds.y - halfHeight;

        // 맵이 화면보다 작으면 가운데 고정
        position.x = minX > maxX ? (_minBounds.x + _maxBounds.x) * 0.5f : Mathf.Clamp(position.x, minX, maxX);
        position.y = minY > maxY ? (_minBounds.y + _maxBounds.y) * 0.5f : Mathf.Clamp(position.y, minY, maxY);
        return position;
    }

    private void OnDrawGizmosSelected()
    {
        if (!_useBounds)
        {
            return;
        }
        Gizmos.color = Color.yellow;
        Vector3 center = (_minBounds + _maxBounds) * 0.5f;
        Vector3 size = _maxBounds - _minBounds;
        Gizmos.DrawWireCube(center, size);
    }
}