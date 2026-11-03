using UnityEngine;

public class LobbyUI : UIBase
{
    [SerializeField] private UIButton _startButton;

    private void OnEnable()
    {
        _startButton.BindOnClickButtonEvent(StartGame);
    }

    private void OnDisable()
    {
        _startButton.UnBindOnClickButtonEvent(StartGame);
    }

    private void StartGame()
    {
        UIManager.Instance.CloseUI(UIRootType.MainUI, UIType.LobbyUI);

        LoadAsset();
    }

    private async void LoadAsset()
    {
        GameObjectManager.Instance.CreateObject("Player", Vector3.zero);
    }
}
