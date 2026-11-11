using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : SingletonBase<GameManager>
{
    private const string _playerPath = "Player";

    public bool IsPlaying { get; private set; }

    public event Action OnStartGame;

    public void StartGame(bool isContinue)
    {
        if (IsPlaying)
        {
            return;
        }
        StartGameAsync(isContinue).Forget();
    }

    private async UniTaskVoid StartGameAsync(bool isContinue)
    {
        if (UIManager.Instance == null || GameObjectManager.Instance == null)
        {
            Debug.LogError("[GameManager] UIManager와 GameObjectManager가 필요합니다.");
            return;
        }

        IsPlaying = true;
        UIManager.Instance.CloseUI(UIRootType.MainUI, UIType.LobbyUI);


        Vector2 spawnPosition = Vector3.zero;
        if (isContinue)
        {
            NetworkManager.Instance.LoadGame(out string lastSaveTeleportId);
            NetworkManager.Instance.TeleportService.TryGetTeleportPosition(lastSaveTeleportId, out spawnPosition);
        }

        GameObject player = await GameObjectManager.Instance.CreateObjectAsync(_playerPath, spawnPosition);
        if (player == null)
        {
            Debug.LogError("[GameManager] 플레이어 생성에 실패했습니다. 로비로 돌아갑니다.");
            IsPlaying = false;
            UIManager.Instance.OpenUI(UIRootType.MainUI, UIType.LobbyUI);
            return;
        }

        UIManager.Instance.OpenUI(UIRootType.ContentUI, UIType.ChangeCardUI);
        OnStartGame?.Invoke();
    }
}
