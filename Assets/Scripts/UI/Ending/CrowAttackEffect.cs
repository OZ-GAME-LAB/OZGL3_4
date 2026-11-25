using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 엔딩 연출: 까마귀가 화면을 여러 번 빠르게 가로지르며 공격하고, 지나간 자리가 검게 물든 뒤
// 화면 전체가 검게 덮이면 로비로 돌아간다.
// 프리팹 루트에 Screen Space - Overlay Canvas와 함께 붙인다. Play()를 호출하면 시작한다.
public class CrowAttackEffect : MonoBehaviour
{
    [Header("연출 구성")]
    [SerializeField] private List<CrowAttackSlash> List_Slash;   // 위에서부터 순서대로 지나간다 (2~3개 권장)
    [SerializeField] private Image Image_Black;                   // 화면 전체를 덮는 검은 이미지. 가장 위(마지막 자식)에 둔다
    [SerializeField] private RectTransform Transform_Shake;       // 공격 중 흔들 영역. 비우면 흔들지 않음

    [Header("까마귀 날갯짓")]
    [SerializeField] private List<Sprite> _crowFlySprites = new List<Sprite>();   // 비우면 Image_Crow에 넣어 둔 스프라이트 그대로
    [SerializeField, Min(1f)] private float _crowFrameRate = 12f;

    [Header("시간 (초)")]
    [SerializeField, Min(0.05f)] private float _slashTime = 0.35f;      // 한 번 가로지르는 시간
    [SerializeField, Min(0f)] private float _slashInterval = 0.15f;     // 다음 공격까지 쉬는 시간
    [SerializeField, Min(0.05f)] private float _blackFadeTime = 0.6f;   // 마지막에 화면이 완전히 검게 되는 시간
    [SerializeField, Min(0f)] private float _blackHoldTime = 0.5f;      // 검은 화면 유지 후 로비 이동

    [Header("흔들림")]
    [SerializeField, Min(0f)] private float _shakeStrength = 15f;

    private bool _isPlaying;
    private Vector2 _shakeBasePosition;

    private void Awake()
    {
        if (List_Slash == null || List_Slash.Count == 0 || Image_Black == null)
        {
            Debug.LogError($"[CrowAttackEffect] {name}: List_Slash(1개 이상)와 Image_Black을 연결하세요.", this);
        }
    }

    /// <summary>
    /// 까마귀 공격 연출을 시작한다. 끝나면 로비(현재 씬 다시 불러오기)로 이동한다.
    /// 시작하는 순간 이동 입력을 막는다.
    /// 테스트: Play Mode 중 컴포넌트 우클릭(⋮) → Play Crow Attack.
    /// </summary>
    [ContextMenu("Play Crow Attack")]
    public void Play()
    {
        if (_isPlaying)
        {
            return;
        }

        if (List_Slash == null || List_Slash.Count == 0 || Image_Black == null)
        {
            Debug.LogError($"[CrowAttackEffect] {name}: 연출 구성이 비어 있어 바로 로비로 이동합니다.", this);
            ReturnLobby();
            return;
        }

        _isPlaying = true;
        BlockPlayerInput();
        PlayAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid PlayAsync(CancellationToken token)
    {
        InitEffect();

        for (int i = 0; i < List_Slash.Count; i++)
        {
            await PlaySlashAsync(List_Slash[i], token);
            await WaitAsync(_slashInterval, token);
        }

        await FadeBlackAsync(token);
        await WaitAsync(_blackHoldTime, token);

        ReturnLobby();
    }

    private void InitEffect()
    {
        for (int i = 0; i < List_Slash.Count; i++)
        {
            if (List_Slash[i] == null)
            {
                continue;
            }

            List_Slash[i].Init();
            List_Slash[i].SetProgress(0f);
        }

        SetBlackAlpha(0f);
        Image_Black.gameObject.SetActive(true);

        if (Transform_Shake != null)
        {
            _shakeBasePosition = Transform_Shake.anchoredPosition;
        }
    }

    private async UniTask PlaySlashAsync(CrowAttackSlash slash, CancellationToken token)
    {
        if (slash == null)
        {
            Debug.LogWarning($"[CrowAttackEffect] {name}: List_Slash에 빈 칸이 있어 건너뜁니다.", this);
            return;
        }

        float time = 0f;
        while (time < _slashTime)
        {
            time += Time.unscaledDeltaTime;
            slash.SetProgress(time / _slashTime);
            UpdateCrowSprite(slash, time);
            ShakeScreen();
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        slash.SetProgress(1f);
        ResetShake();
    }

    private async UniTask FadeBlackAsync(CancellationToken token)
    {
        float time = 0f;
        while (time < _blackFadeTime)
        {
            time += Time.unscaledDeltaTime;
            SetBlackAlpha(time / _blackFadeTime);
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        SetBlackAlpha(1f);
    }

    private async UniTask WaitAsync(float seconds, CancellationToken token)
    {
        float time = 0f;
        while (time < seconds)
        {
            time += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private void UpdateCrowSprite(CrowAttackSlash slash, float time)
    {
        if (_crowFlySprites.Count == 0)
        {
            return;
        }

        int frameIndex = (int)(time * _crowFrameRate) % _crowFlySprites.Count;
        slash.SetCrowSprite(_crowFlySprites[frameIndex]);
    }

    private void ShakeScreen()
    {
        if (Transform_Shake == null || _shakeStrength <= 0f)
        {
            return;
        }

        Transform_Shake.anchoredPosition = _shakeBasePosition + UnityEngine.Random.insideUnitCircle * _shakeStrength;
    }

    private void ResetShake()
    {
        if (Transform_Shake == null)
        {
            return;
        }

        Transform_Shake.anchoredPosition = _shakeBasePosition;
    }

    private void SetBlackAlpha(float alpha)
    {
        Color color = Image_Black.color;
        color.a = Mathf.Clamp01(alpha);
        Image_Black.color = color;
    }

    // 로비로 갈 연출이므로 Lobby 상태로 바꿔 이동 입력을 막는다.
    // Cutscene이 아닌 Lobby로 두어야 대화 줌아웃이 끝날 때 CameraController가 Playing으로 되돌리지 않는다.
    private void BlockPlayerInput()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"[CrowAttackEffect] {name}: GameManager가 없어 입력을 막지 못했습니다.", this);
            return;
        }

        GameManager.Instance.ChangeGameState(GameState.Lobby);
    }

    // MenuUI의 로비 버튼과 같은 방식: 현재 씬을 다시 불러와 로비 화면부터 시작한다.
    private void ReturnLobby()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
