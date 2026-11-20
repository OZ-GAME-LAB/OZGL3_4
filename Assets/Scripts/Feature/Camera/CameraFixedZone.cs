using System.Collections.Generic;
using UnityEngine;

// 플레이어가 영역 안에 있는 동안 카메라를 줌 아웃하고 한 지점에 고정하는 컴포넌트
// 사용법: 원하는 타일 아래에 빈 오브젝트를 만들어 붙이고 BoxCollider2D 크기를 영역에 맞춘다.
//         Main Camera에 CameraFollow와 CameraController가 함께 있어야 한다.
// - 들어오면: 화면 크기를 _zoomSize로 바꾸고 고정 지점(기본값: 영역 중심)을 비춘다
// - 나가면: 원래 크기로 돌아가고 다시 플레이어를 따라간다
// - Hide Outside를 켜면 고정된 동안 영역 바깥(옆 타일)을 검은 가림막으로 덮는다
[RequireComponent(typeof(BoxCollider2D))]
public class CameraFixedZone : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField, Min(0.1f)] private float _zoomSize = 16f;     // 화면 세로 절반 크기. 32칸 타일 전체는 16
    [SerializeField, Min(0f)] private float _zoomInTime = 0.6f;
    [SerializeField, Min(0f)] private float _zoomOutTime = 0.6f;

    [Header("Fixed Point")]
    [SerializeField] private Transform Transform_FixedPoint;        // 비워두면 영역(콜라이더) 중심에 고정

    [Header("Outside Mask (영역 바깥 가리기)")]
    [SerializeField] private bool _isHideOutside = true;
    [SerializeField] private Color _maskColor = Color.black;
    [SerializeField, Min(1f)] private float _maskWidth = 40f;        // 영역 바깥으로 덮는 폭. 화면보다 넉넉하게
    [SerializeField, Min(0f)] private float _maskFadeInTime = 0.3f;
    [SerializeField, Min(0f)] private float _maskFadeOutTime = 0.15f;
    [SerializeField] private int _maskSortingOrder = 10000;          // 맵, 기믹, 플레이어보다 위에 그린다

    [Header("에디터 표시")]
    [SerializeField] private Color _gizmoColor = Color.yellow;

    // 지금 카메라를 잡고 있는 영역. 영역이 맞닿아 있으면 나중에 들어간 쪽이 우선한다.
    private static CameraFixedZone _currentZone;

    // 플레이어가 지금 들어가 있는 영역들 (들어간 순서). 영역 경계에 걸쳐 있으면 2개가 될 수 있다.
    private static List<CameraFixedZone> _insideZones = new List<CameraFixedZone>();

    private BoxCollider2D _box;
    private int _playerColliderCount;

    // 배관 이동처럼 플레이어의 물리가 잠깐 꺼져 트리거에서 빠졌지만, 위치는 아직 영역 안일 때 지켜보는 대상
    private Transform _trackedPlayer;

    // 가림막: 영역의 왼쪽, 오른쪽, 위, 아래를 덮는 검은 사각형 4개
    private static Sprite _maskSprite;
    private SpriteRenderer[] _maskRenderers;
    private float _maskAlpha;
    private float _maskTargetAlpha;

    private void Awake()
    {
        _box = GetComponent<BoxCollider2D>();
    }

    private void Reset()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(32f, 32f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (GimmickUtil.TryGetFromPlayerCollider(collision, out Player player) == false)
            return;

        _playerColliderCount++;
        if (_playerColliderCount > 1)
            return;

        // 배관에서 나와 다시 감지된 경우: 고정을 유지하고 있었으므로 새로 적용하지 않는다
        if (_trackedPlayer != null)
        {
            _trackedPlayer = null;
            if (_currentZone == this)
                return;
        }

        if (_insideZones.Contains(this) == false)
        {
            _insideZones.Add(this);
        }

        Apply();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (GimmickUtil.TryGetFromPlayerCollider(collision, out Player player) == false)
            return;

        _playerColliderCount = Mathf.Max(0, _playerColliderCount - 1);
        if (_playerColliderCount > 0)
            return;

        // 트리거에서는 빠졌지만 위치가 아직 영역 안이면(배관 이동 등) 고정과 가림막을 유지하고 위치만 지켜본다
        if (IsInside(player.transform.position))
        {
            _trackedPlayer = player.transform;
            return;
        }

        Leave();
    }

    private void OnDisable()
    {
        _playerColliderCount = 0;
        _trackedPlayer = null;
        Leave();

        _maskAlpha = 0f;
        ApplyMaskAlpha();
    }

    private void Update()
    {
        UpdateTrackedPlayer();
        UpdateMaskFade();
    }

    // 지켜보던 플레이어가 실제로 영역 밖으로 나가면 그때 고정을 푼다
    private void UpdateTrackedPlayer()
    {
        if (ReferenceEquals(_trackedPlayer, null))
            return;

        if (_trackedPlayer != null && IsInside(_trackedPlayer.position))
            return;

        _trackedPlayer = null;
        Leave();
    }

    private bool IsInside(Vector2 worldPosition)
    {
        return _box.OverlapPoint(worldPosition);
    }

    private void UpdateMaskFade()
    {
        if (Mathf.Approximately(_maskAlpha, _maskTargetAlpha))
            return;

        float fadeTime = _maskTargetAlpha > _maskAlpha ? _maskFadeInTime : _maskFadeOutTime;
        if (fadeTime <= 0f)
        {
            _maskAlpha = _maskTargetAlpha;
        }
        else
        {
            _maskAlpha = Mathf.MoveTowards(_maskAlpha, _maskTargetAlpha, Time.deltaTime / fadeTime);
        }

        ApplyMaskAlpha();
    }

    private void Apply()
    {
        CameraFollow cameraFollow;
        CameraController cameraController;
        if (TryGetCamera(out cameraFollow, out cameraController) == false)
            return;

        // 다른 영역이 카메라를 잡고 있었다면 그 영역의 가림막부터 걷는다
        if (_currentZone != null && _currentZone != this)
        {
            _currentZone.HideMask();
        }

        _currentZone = this;
        cameraFollow.SetFixedPosition(GetFixedPosition());
        cameraController.ZoomIn(_zoomSize, _zoomInTime, false);

        if (_isHideOutside)
        {
            CreateMask();
            _maskTargetAlpha = 1f;
        }
    }

    // 플레이어가 이 영역을 벗어났을 때: 내 가림막은 항상 걷고, 내가 카메라를 잡고 있었을 때만 카메라를 넘기거나 푼다
    private void Leave()
    {
        _insideZones.Remove(this);
        HideMask();

        if (_currentZone != this)
            return;

        _currentZone = null;

        // 아직 다른 영역 안에 있으면(경계에서 되돌아간 경우) 그 영역이 카메라를 다시 잡는다
        if (_insideZones.Count > 0)
        {
            _insideZones[_insideZones.Count - 1].Apply();
            return;
        }

        ReleaseCamera();
    }

    private void HideMask()
    {
        _maskTargetAlpha = 0f;
    }

    private void ReleaseCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.ClearFixedPosition();
        }

        CameraController cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.ZoomOut(_zoomOutTime);
        }
    }

    // 가림막을 처음 필요할 때 한 번만 만든다. 영역(콜라이더) 바깥 네 방향을 덮는다.
    private void CreateMask()
    {
        if (_maskRenderers != null)
            return;

        if (_maskSprite == null)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _maskSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            _maskSprite.name = "CameraFixedZone_Mask";
        }

        Vector2 center = _box.offset;
        Vector2 half = _box.size * 0.5f;
        float width = _maskWidth;

        _maskRenderers = new SpriteRenderer[4];
        _maskRenderers[0] = CreateMaskPart("Mask_Left", new Vector2(center.x - half.x - width * 0.5f, center.y), new Vector2(width, _box.size.y + width * 2f));
        _maskRenderers[1] = CreateMaskPart("Mask_Right", new Vector2(center.x + half.x + width * 0.5f, center.y), new Vector2(width, _box.size.y + width * 2f));
        _maskRenderers[2] = CreateMaskPart("Mask_Top", new Vector2(center.x, center.y + half.y + width * 0.5f), new Vector2(_box.size.x, width));
        _maskRenderers[3] = CreateMaskPart("Mask_Bottom", new Vector2(center.x, center.y - half.y - width * 0.5f), new Vector2(_box.size.x, width));

        ApplyMaskAlpha();
    }

    private SpriteRenderer CreateMaskPart(string partName, Vector2 localPosition, Vector2 size)
    {
        GameObject part = new GameObject(partName);
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer spriteRenderer = part.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = _maskSprite;
        spriteRenderer.sortingOrder = _maskSortingOrder;
        return spriteRenderer;
    }

    private void ApplyMaskAlpha()
    {
        if (_maskRenderers == null)
            return;

        Color color = _maskColor;
        color.a *= _maskAlpha;
        bool isVisible = _maskAlpha > 0f;

        for (int i = 0; i < _maskRenderers.Length; i++)
        {
            if (_maskRenderers[i] == null)
                continue;

            _maskRenderers[i].color = color;
            _maskRenderers[i].enabled = isVisible;
        }
    }

    private Vector2 GetFixedPosition()
    {
        if (Transform_FixedPoint != null)
            return Transform_FixedPoint.position;

        return transform.TransformPoint(_box.offset);
    }

    private bool TryGetCamera(out CameraFollow cameraFollow, out CameraController cameraController)
    {
        cameraFollow = null;
        cameraController = null;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[CameraFixedZone] MainCamera 태그가 붙은 카메라가 없습니다.", this);
            return false;
        }

        cameraFollow = mainCamera.GetComponent<CameraFollow>();
        cameraController = mainCamera.GetComponent<CameraController>();
        if (cameraFollow == null || cameraController == null)
        {
            Debug.LogError("[CameraFixedZone] Main Camera에 CameraFollow와 CameraController를 모두 붙이세요.", this);
            return false;
        }
        return true;
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
