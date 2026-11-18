using System;
using DG.Tweening;
using UnityEngine;

// 메인 카메라의 화면 크기(orthographicSize)를 바꿔 줌 연출을 하는 컴포넌트
// 사용법: CameraFollow가 붙은 Main Camera에 함께 붙이고 Camera.main.GetComponent<CameraController>()로 호출한다.
// - 자동으로 돌아오는 줌: PlayZoom(줌 크기, 들어가는 시간, 유지 시간, 돌아오는 시간, 입력 막기)
// - 직접 끝내는 줌: ZoomIn(줌 크기, 시간, 입력 막기) → 상황이 끝나면 ZoomOut(시간)
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField] private Ease _zoomEase = Ease.InOutSine;

    private Camera _camera;
    private float _defaultSize;
    private Sequence _zoomSequence;
    private bool _isBlockingInput;

    // 화면이 원래 크기가 아닌 동안 true (들어가는 중, 유지 중, 돌아오는 중 모두 포함)
    public bool IsZoomed { get; private set; }

    // 줌이 끝나고 원래 크기로 돌아왔을 때 발생
    public event Action OnEndZoom;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _defaultSize = _camera.orthographicSize;
    }

    private void OnDisable()
    {
        StopZoom();
    }

    /// <summary>
    /// 줌 크기까지 들어갔다가 잠시 유지한 뒤 원래 크기로 돌아온다.
    /// zoomSize는 orthographicSize 값이다. 기본값(보통 5)보다 작으면 확대, 크면 축소된다.
    /// </summary>
    public void PlayZoom(float zoomSize, float zoomInTime, float holdTime, float zoomOutTime, bool isBlockInput)
    {
        if (IsValidZoomSize(zoomSize) == false)
            return;

        StartZoom(isBlockInput);
        _zoomSequence.Append(_camera.DOOrthoSize(zoomSize, zoomInTime).SetEase(_zoomEase));
        _zoomSequence.AppendInterval(holdTime);
        _zoomSequence.Append(_camera.DOOrthoSize(_defaultSize, zoomOutTime).SetEase(_zoomEase));
        _zoomSequence.OnComplete(OnCompleteZoomOut);
    }

    /// <summary>
    /// 줌 크기까지 들어간 뒤 그대로 유지한다. 상황이 끝나면 ZoomOut을 호출해야 원래대로 돌아온다.
    /// </summary>
    public void ZoomIn(float zoomSize, float duration, bool isBlockInput)
    {
        if (IsValidZoomSize(zoomSize) == false)
            return;

        StartZoom(isBlockInput);
        _zoomSequence.Append(_camera.DOOrthoSize(zoomSize, duration).SetEase(_zoomEase));
        _zoomSequence.OnComplete(OnCompleteZoomIn);
    }

    /// <summary>
    /// 원래 크기로 돌아온다. ZoomIn으로 막은 입력은 다 돌아온 뒤에 풀린다.
    /// </summary>
    public void ZoomOut(float duration)
    {
        if (IsZoomed == false)
            return;

        KillZoomSequence();
        _zoomSequence = DOTween.Sequence();
        _zoomSequence.Append(_camera.DOOrthoSize(_defaultSize, duration).SetEase(_zoomEase));
        _zoomSequence.OnComplete(OnCompleteZoomOut);
    }

    /// <summary>
    /// 연출을 즉시 멈추고 원래 크기와 입력 상태로 되돌린다.
    /// </summary>
    public void StopZoom()
    {
        KillZoomSequence();

        if (IsZoomed == false)
            return;

        _camera.orthographicSize = _defaultSize;
        EndZoom();
    }

    private bool IsValidZoomSize(float zoomSize)
    {
        if (zoomSize <= 0f)
        {
            Debug.LogError($"[CameraController] 줌 크기는 0보다 커야 합니다. 입력값: {zoomSize}", this);
            return false;
        }
        return true;
    }

    // 진행 중인 줌을 정리하고 새 시퀀스를 만든다. 이미 줌 상태여도 현재 크기에서 이어서 움직인다.
    private void StartZoom(bool isBlockInput)
    {
        KillZoomSequence();
        IsZoomed = true;

        if (isBlockInput)
        {
            BlockInput();
        }
        else
        {
            ReleaseInput();
        }

        _zoomSequence = DOTween.Sequence();
    }

    private void OnCompleteZoomIn()
    {
        _zoomSequence = null;
    }

    private void OnCompleteZoomOut()
    {
        _zoomSequence = null;
        EndZoom();
    }

    private void EndZoom()
    {
        IsZoomed = false;
        ReleaseInput();
        OnEndZoom?.Invoke();
    }

    private void KillZoomSequence()
    {
        if (_zoomSequence == null)
            return;

        _zoomSequence.Kill();
        _zoomSequence = null;
    }

    // 플레이 중일 때만 연출 상태로 바꾼다. 다른 곳에서 이미 연출 중이면 그쪽 상태를 건드리지 않는다.
    private void BlockInput()
    {
        if (_isBlockingInput)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[CameraController] GameManager가 없어 입력을 막지 않고 줌만 실행합니다.", this);
            return;
        }

        if (GameManager.Instance.CurrentGameState != GameState.Playing)
            return;

        GameManager.Instance.ChangeGameState(GameState.Cutscene);
        _isBlockingInput = true;
    }

    // 이 컴포넌트가 막은 입력만 다시 푼다.
    private void ReleaseInput()
    {
        if (_isBlockingInput == false)
            return;

        _isBlockingInput = false;

        // 그사이 다른 곳에서 상태를 바꿨다면(예: 로비로 이동) 되돌리지 않는다.
        if (GameManager.Instance == null || GameManager.Instance.CurrentGameState != GameState.Cutscene)
            return;

        GameManager.Instance.ChangeGameState(GameState.Playing);
    }
}
