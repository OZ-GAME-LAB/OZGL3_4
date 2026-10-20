using UnityEngine;

/// <summary>
/// 테스트 전용입니다. 더미 플레이어에 붙여 빛 영역에 들어가고 나가는 것을 확인합니다.
/// 빛 안에 있으면 색이 바뀌고 콘솔에 로그를 남깁니다.
/// 더미 플레이어에는 Rigidbody2D와 Collider2D가 있어야 합니다.
/// </summary>
public class LightZoneProbe : MonoBehaviour
{
    private const string LightZoneLayerName = "LightZone";

    [SerializeField] private SpriteRenderer SpriteRenderer_Body;
    [SerializeField] private Color _inLightColor = Color.red;

    private int _lightZoneLayer;
    private int _overlapCount;
    private Color _defaultColor;

    private void Awake()
    {
        _lightZoneLayer = LayerMask.NameToLayer(LightZoneLayerName);

        if (SpriteRenderer_Body != null)
        {
            _defaultColor = SpriteRenderer_Body.color;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer != _lightZoneLayer)
        {
            return;
        }

        _overlapCount++;
        UpdateView();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer != _lightZoneLayer)
        {
            return;
        }

        _overlapCount = Mathf.Max(0, _overlapCount - 1);
        UpdateView();
    }

    private void UpdateView()
    {
        bool isInLight = _overlapCount > 0;

        if (SpriteRenderer_Body != null)
        {
            SpriteRenderer_Body.color = isInLight ? _inLightColor : _defaultColor;
        }

        Debug.Log($"[LightZoneProbe] 빛 영역 {(isInLight ? "진입" : "이탈")} (겹친 영역 수: {_overlapCount})");
    }
}
