using UnityEngine;
using UnityEngine.Tilemaps;

public class SecretArea : MonoBehaviour
{
    [Header("비밀방 설정")]
    [SerializeField] private string _hiddenId;
    [SerializeField] private Tilemap Tilemap_Cover;

    [Header("페이드")]
    [SerializeField] private bool _isFadeEnabled = true;
    [SerializeField, Range(0f, 1f)] private float _minAlpha = 0f;
    [SerializeField] private float _fadeSpeed = 3f;

    private int _playerOverlapCount;
    private float _targetAlpha = 1f;


    private void Awake()
    {
        if (Tilemap_Cover == null)
        {
            Tilemap_Cover = GetComponent<Tilemap>();

            if (Tilemap_Cover == null)
            {
                Debug.LogError($"[SecretArea] (Awake) - Tilemap 없음: {gameObject.name}");
            }
        }
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(_hiddenId))
        {
            Debug.LogError($"[SecretArea] (Start) - HiddenId가 비어 있음: {gameObject.name}");

            return;
        }

        if (!MapUtil.TryGetRoomId(this, nameof(Start), out _))
            return;

        if (!MapUtil.TryGetMapService(this, nameof(Start), out _))
            return;
    }

    private void Update()
    {
        if (!_isFadeEnabled || Tilemap_Cover == null)
            return;

        Color color = Tilemap_Cover.color;

        if (Mathf.Approximately(color.a, _targetAlpha))
            return;

        color.a = Mathf.MoveTowards(color.a, _targetAlpha, _fadeSpeed * Time.deltaTime);
        Tilemap_Cover.color = color;
    }

    private void OnDisable()
    {
        _playerOverlapCount = 0;
        _targetAlpha = 1f;

        if (Tilemap_Cover != null)
        {
            Color color = Tilemap_Cover.color;
            color.a = 1f;
            Tilemap_Cover.color = color;
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!GimmickUtil.TryGetFromPlayerCollider(collision, out Player player))
            return;

        _playerOverlapCount++;
        _targetAlpha = _minAlpha;

        DiscoverHidden();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!GimmickUtil.TryGetFromPlayerCollider(collision, out Player player))
            return;

        _playerOverlapCount = Mathf.Max(0, _playerOverlapCount - 1);

        if (_playerOverlapCount == 0)
        {
            _targetAlpha = 1f;
        }
    }


    private void DiscoverHidden()
    {
        if (!MapUtil.TryGetRoomId(this, nameof(DiscoverHidden), out string roomId))
            return;

        if (!MapUtil.TryGetMapService(this, nameof(DiscoverHidden), out MapService mapService))
            return;

        if (mapService.RequestDiscoverHidden(roomId, _hiddenId))
        {
            Debug.Log($"[SecretArea] (DiscoverHidden) - 비밀방 발견: {_hiddenId}");
        }
    }
}
