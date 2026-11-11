using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 플레이어 사망(Died)을 받아 부활 위치를 찾고 Revive까지 처리한다. Player 프리팹에 붙인다.
[RequireComponent(typeof(Player))]
public class PlayerRespawner : MonoBehaviour
{
    [Header("부활")]
    [SerializeField] private float _respawnDelay = 1f;

    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void OnEnable()
    {
        _player.Died += OnPlayerDied;
    }

    private void OnDisable()
    {
        _player.Died -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        RespawnAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid RespawnAsync(CancellationToken token)
    {
        // ① 사망 연출 대기
        bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(_respawnDelay), cancellationToken: token).SuppressCancellationThrow();
        if (isCanceled)
            return;

        // ② 페이드 인
        FadeUI fadeUI = UIManager.Instance.OpenUI(UIRootType.VeryFrontUI, UIType.FadeUI) as FadeUI;

        if (fadeUI == null)
        {
            Debug.LogWarning("[PlayerRespawner] (RespawnAsync) - FadeUI 열기 실패, 페이드 없이 부활");
        }
        else
        {
            await fadeUI.FadeIn().ToUniTask(fadeUI);
        }

        // ③ 부활 위치 찾기
        Vector2 position;

        if (MapManager.Instance == null || !MapManager.Instance.TryGetRespawnPosition(out position))
        {
            Debug.LogError("[PlayerRespawner] (RespawnAsync) - 부활 위치 없음, 현재 위치에서 부활");

            position = transform.position;
        }

        // ④ 부활
        _player.Revive(position);

        // ⑤ 물리 위치 반영 후 페이드 아웃
        await UniTask.WaitForFixedUpdate();

        if (fadeUI != null)
        {
            await fadeUI.FadeOut().ToUniTask(fadeUI);

            UIManager.Instance.CloseUI(UIRootType.VeryFrontUI, UIType.FadeUI);
        }
    }

    // 테스트용. 확인 후 주석 처리
    [ContextMenu("사망 테스트")]
    private void TestDie()
    {
        _player.Die();
    }
}