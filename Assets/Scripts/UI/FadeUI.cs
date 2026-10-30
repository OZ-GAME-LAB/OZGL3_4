using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeUI : UIBase
{
    [SerializeField] private Image _fadeImage;

    private float _duration = 0.3f; 

    public IEnumerator FadeOut()
    {
        return Fade(0f);
    }

    public IEnumerator FadeIn()
    {
        return Fade(1f);
    }

    private void OnEnable()
    {
        _fadeImage.color = new Color(0, 0, 0, 0);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = _fadeImage.color.a;
        float time = 0f;
        Color color = new Color(0, 0, 0, startAlpha);

        while(time < _duration)
        {
            time += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, time / _duration);
            _fadeImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        _fadeImage.color = color;
    }
}
