using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    Lobby = 0,  
    Playing,    
    Cutscene,   
}

public class GameManager : SingletonBase<GameManager>
{
    private const string _playerPath = "Player";

    private PlayerInput _playerInput;

    // Cutscene 등 Playing이 아닐 때 막는 플레이어 입력 (액션맵/액션 이름)
    private readonly List<string> _blockActionNames = new List<string>
    {
        "Player/Move",
        "Player/Jump",
        "Player/Dash",
        "Player/Ability",
    };

    public bool IsPlaying { get; private set; }
    public GameState CurrentGameState { get; private set; } = GameState.Lobby;

    public event Action OnStartGame;
    public event Action<GameState> OnChangeGameState;

    public void StartGame(bool isContinue)
    {
        if (IsPlaying)
        {
            return;
        }
        StartGameAsync(isContinue).Forget();
    }

    public void ChangeGameState(GameState next)
    {
        if (CurrentGameState == next)
        {
            return;
        }

        CurrentGameState = next;
        SetPlayerInput(next == GameState.Playing);
        OnChangeGameState?.Invoke(next);
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
            ChangeGameState(GameState.Lobby);
            UIManager.Instance.OpenUI(UIRootType.MainUI, UIType.LobbyUI);
            return;
        }

        _playerInput = player.GetComponent<PlayerInput>();
        ChangeGameState(GameState.Playing);

        UIManager.Instance.OpenUI(UIRootType.ContentUI, UIType.ChangeCardUI);
        OnStartGame?.Invoke();
    }

    private void SetPlayerInput(bool isEnable)
    {
        if (_playerInput == null)
        {
            Player player = FindAnyObjectByType<Player>();
            if (player == null)
            {
                return;
            }
            _playerInput = player.GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogWarning("[GameManager] Player에 PlayerInput이 없어 입력을 제어할 수 없습니다.");
                return;
            }
        }

        // 이동 관련 액션만 켜고 끈다. Interaction(F)은 연출 중에도 대사 넘기기에 쓰므로 그대로 둔다.
        for (int i = 0; i < _blockActionNames.Count; i++)
        {
            InputAction action = _playerInput.actions.FindAction(_blockActionNames[i]);
            if (action == null)
            {
                Debug.LogWarning($"[GameManager] 입력 액션을 찾을 수 없습니다: {_blockActionNames[i]}");
                continue;
            }

            if (isEnable)
            {
                action.Enable();
            }
            else
            {
                action.Disable();
            }
        }
    }
}
