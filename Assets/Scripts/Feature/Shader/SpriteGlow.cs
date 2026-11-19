using UnityEngine;

/// <summary>
/// Sprites/Glow 셰이더의 값을 이 오브젝트에만 따로 적용합니다. (같은 머티리얼을 여러 캐릭터가 써도 각자 다른 색 가능)
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteGlow : MonoBehaviour
{
    [Header("글로우")]
    [SerializeField] private bool _isGlowEnabled = true;
    [SerializeField] private Color _innerColor = new Color(0.80f, 0.74f, 1.00f, 1f);
    [SerializeField] private Color _outerColor = new Color(0.42f, 0.33f, 0.78f, 1f);
    [SerializeField, Range(0f, 32f)] private float _glowSize = 8f;
    [SerializeField, Range(0f, 4f)] private float _glowStrength = 1.2f;

    private static readonly int GlowEnabledId = Shader.PropertyToID("_GlowEnabled");
    private static readonly int InnerColorId = Shader.PropertyToID("_GlowInnerColor");
    private static readonly int OuterColorId = Shader.PropertyToID("_GlowOuterColor");
    private static readonly int GlowSizeId = Shader.PropertyToID("_GlowSize");
    private static readonly int GlowStrengthId = Shader.PropertyToID("_GlowStrength");

    private SpriteRenderer _spriteRenderer;
    private MaterialPropertyBlock _propertyBlock;

    public void SetGlowEnabled(bool isEnabled)
    {
        _isGlowEnabled = isEnabled;
        ApplyGlow();
    }

    private void OnEnable()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _propertyBlock = new MaterialPropertyBlock();
        ApplyGlow();
    }

    private void OnDisable()
    {
        if (_spriteRenderer == null)
        {
            return;
        }

        // 꺼지면 이 오브젝트에 넣었던 값을 지워서 머티리얼 기본값으로 돌아가게 합니다.
        _spriteRenderer.SetPropertyBlock(null);
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ApplyGlow();
    }

    private void LateUpdate()
    {
        // 애니메이션으로 스프라이트가 바뀌어도 값이 유지되도록 매 프레임 다시 넣습니다. (할당 없음)
        ApplyGlow();
    }

    private void ApplyGlow()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                Debug.LogError($"[SpriteGlow] (ApplyGlow) - SpriteRenderer 없음: {gameObject.name}");
                return;
            }
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        _spriteRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetFloat(GlowEnabledId, _isGlowEnabled ? 1f : 0f);
        _propertyBlock.SetColor(InnerColorId, _innerColor);
        _propertyBlock.SetColor(OuterColorId, _outerColor);
        _propertyBlock.SetFloat(GlowSizeId, _glowSize);
        _propertyBlock.SetFloat(GlowStrengthId, _glowStrength);
        _spriteRenderer.SetPropertyBlock(_propertyBlock);
    }
}