using UnityEngine;

public class LobbyUI : UIBase
{
    [SerializeField] private UIButton _startButton;
    [SerializeField] private UIButton _settingButton;

    private void OnEnable()
    {
        _startButton.BindOnClickButtonEvent(StartGame);
        _settingButton.BindOnClickButtonEvent(OpenSettingUI);
    }

    private void OnDisable()
    {
        _startButton.UnBindOnClickButtonEvent(StartGame);
        _settingButton.UnBindOnClickButtonEvent(OpenSettingUI);
    }

    private void StartGame()
    {
        GameManager.Instance.StartGame();
    }

    private void OpenSettingUI()
    {
        UIManager.Instance.OpenUI(UIRootType.PopupUI, UIType.SettingUI);
    }
}
