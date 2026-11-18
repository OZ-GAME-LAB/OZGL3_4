using UnityEngine;

// 플레이어가 영역에 들어오면 메인 카메라의 CameraController로 줌 연출을 재생하는 컴포넌트
// 사용법: 빈 오브젝트에 붙이고 BoxCollider2D 크기를 조절한다. Main Camera에 CameraController가 있어야 한다.
[RequireComponent(typeof(BoxCollider2D))]
public class CameraZoomTrigger : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField, Min(0.1f)] private float _zoomSize = 3f;       // 작을수록 더 확대 (카메라 기본값 5)
    [SerializeField, Min(0f)] private float _zoomInTime = 0.5f;
    [SerializeField, Min(0f)] private float _holdTime = 1f;
    [SerializeField, Min(0f)] private float _zoomOutTime = 0.5f;
    [SerializeField] private bool _isBlockInput = true;               // 연출 동안 플레이어 입력 막기

    [Header("Trigger")]
    [SerializeField] private bool _isPlayOnce = true;                 // 한 번만 재생

    [Header("에디터 표시")]
    [SerializeField] private Color _gizmoColor = Color.cyan;

    private bool _isPlayed;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (GimmickUtil.TryGetFromPlayerCollider(collision, out Player player) == false)
            return;

        if (_isPlayOnce && _isPlayed)
            return;

        CameraController cameraController = GetCameraController();
        if (cameraController == null)
            return;

        _isPlayed = true;
        cameraController.PlayZoom(_zoomSize, _zoomInTime, _holdTime, _zoomOutTime, _isBlockInput);
    }

    private CameraController GetCameraController()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[CameraZoomTrigger] MainCamera 태그가 붙은 카메라가 없습니다.", this);
            return null;
        }

        CameraController cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraController == null)
        {
            Debug.LogError("[CameraZoomTrigger] Main Camera에 CameraController 컴포넌트를 붙이세요.", this);
            return null;
        }
        return cameraController;
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null)
            return;

        Gizmos.color = _gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.offset, box.size);
    }
}
