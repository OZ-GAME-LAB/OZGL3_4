using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegionBackgroundController : MonoBehaviour
{
    [Serializable]
    private class RegionBackground
    {
        public string RegionId;
        public Sprite Sprite_Background;
    }

    [Header("References")]
    [SerializeField] private SpriteRenderer SpriteRenderer_Front;   // 새 배경 (페이드 인)
    [SerializeField] private SpriteRenderer SpriteRenderer_Back;    // 이전 배경 (뒤에 유지)

    [Header("Backgrounds")]
    [SerializeField] private List<RegionBackground> _regionBackgrounds = new List<RegionBackground>();
    [SerializeField] private Sprite Sprite_Default;

    [Header("Fade")]
    [SerializeField] private float _fadeDuration = 0.5f;            // 0이면 즉시 교체

    private MapManager _mapManager;
    private Dictionary<string, Sprite> _lookup;
    private Coroutine _fadeRoutine;

    private void Start()
    {
        if (SpriteRenderer_Front == null || SpriteRenderer_Back == null)
        {
            Debug.LogError("[RegionBackgroundController] (Start) - SpriteRenderer 참조 없음");
            enabled = false;
            return;
        }

        BuildLookup();

        SpriteRenderer_Back.enabled = false;
        SetAlpha(SpriteRenderer_Front, 1f);
        SpriteRenderer_Front.sprite = Sprite_Default;

        _mapManager = MapManager.Instance;
        if (_mapManager == null)
        {
            Debug.LogError("[RegionBackgroundController] (Start) - MapManager 없음");
            return;
        }

        _mapManager.OnChangeRegion += HandleRegionChanged;

        // 구독 전에 이미 지역이 정해져 있었다면 즉시 적용
        if (!string.IsNullOrEmpty(_mapManager.CurrentRegionId))
        {
            SpriteRenderer_Front.sprite = GetSprite(_mapManager.CurrentRegionId);
        }
    }

    private void OnDestroy()
    {
        if (_mapManager != null)
        {
            _mapManager.OnChangeRegion -= HandleRegionChanged;
        }
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, Sprite>();
        foreach (RegionBackground entry in _regionBackgrounds)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.RegionId))
            {
                continue;
            }
            if (entry.Sprite_Background == null)
            {
                Debug.LogWarning($"[RegionBackgroundController] (BuildLookup) - 스프라이트 없음: {entry.RegionId}");
                continue;
            }
            _lookup[entry.RegionId] = entry.Sprite_Background;
        }
    }

    private Sprite GetSprite(string regionId)
    {
        if (!string.IsNullOrEmpty(regionId) && _lookup.TryGetValue(regionId, out Sprite found))
        {
            return found;
        }
        return Sprite_Default;
    }

    private void HandleRegionChanged(string regionId)
    {
        Sprite next = GetSprite(regionId);
        if (next == SpriteRenderer_Front.sprite)
        {
            return;
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        if (_fadeDuration <= 0f || SpriteRenderer_Front.sprite == null)
        {
            SpriteRenderer_Back.enabled = false;
            SpriteRenderer_Front.sprite = next;
            SetAlpha(SpriteRenderer_Front, 1f);
            return;
        }

        _fadeRoutine = StartCoroutine(FadeRoutine(next));
    }

    private IEnumerator FadeRoutine(Sprite next)
    {
        // 현재 배경을 뒤로 넘기고, 앞에서 새 배경을 페이드 인
        SpriteRenderer_Back.sprite = SpriteRenderer_Front.sprite;
        SetAlpha(SpriteRenderer_Back, 1f);
        SpriteRenderer_Back.enabled = true;

        SpriteRenderer_Front.sprite = next;
        SetAlpha(SpriteRenderer_Front, 0f);

        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(SpriteRenderer_Front, Mathf.Clamp01(elapsed / _fadeDuration));
            yield return null;
        }

        SetAlpha(SpriteRenderer_Front, 1f);
        SpriteRenderer_Back.enabled = false;
        _fadeRoutine = null;
    }

    private void SetAlpha(SpriteRenderer target, float alpha)
    {
        Color color = target.color;
        color.a = alpha;
        target.color = color;
    }
}