using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : SingletonBase<GameManager>
{
    private const string _playerPath = "Player";

    public bool IsPlaying { get; private set; }

    public event Action OnStartGame;

    public void StartGame()
    {
        if (IsPlaying)
        {
            return;
        }
        StartGameAsync().Forget();
    }

    private async UniTaskVoid StartGameAsync()
    {
        if (UIManager.Instance == null || GameObjectManager.Instance == null)
        {
            Debug.LogError("[GameManager] UIManager와 GameObjectManager가 필요합니다.");
            return;
        }

        IsPlaying = true;
        UIManager.Instance.CloseUI(UIRootType.MainUI, UIType.LobbyUI);

        GameObject player = await GameObjectManager.Instance.CreateObjectAsync(_playerPath, Vector3.zero);
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
