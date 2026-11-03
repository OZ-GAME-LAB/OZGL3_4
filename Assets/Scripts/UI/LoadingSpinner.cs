using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
    [SerializeField] private RectTransform _dots;
    [SerializeField] private int _dotCount = 8;
    [SerializeField] private float _stepsPerSecond = 12f;

    private float _elapsed;

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;

        int step = Mathf.FloorToInt(_elapsed * _stepsPerSecond) % _dotCount;
        float stepAngle = 360f / _dotCount;
        _dots.localRotation = Quaternion.Euler(0f, 0f, -step * stepAngle);
    }

    public void Show()
    {
        _elapsed = 0f;
        _dots.localRotation = Quaternion.identity;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
