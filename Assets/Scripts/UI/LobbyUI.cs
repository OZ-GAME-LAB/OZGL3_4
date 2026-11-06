using UnityEngine;

public class LobbyUI : UIBase
{
    [SerializeField] private UIButton _continueButton;
    [SerializeField] private UIButton _startButton;
    [SerializeField] private UIButton _settingButton;
    [SerializeField] private UIButton _exitButton;

    private void OnEnable()
    {
        _startButton.BindOnClickButtonEvent(StartGame);
        _settingButton.BindOnClickButtonEvent(OpenSettingUI);
        _exitButton.BindOnClickButtonEvent(ExitGame);
    }

    private void OnDisable()
    {
        _startButton.UnBindOnClickButtonEvent(StartGame);
        _settingButton.UnBindOnClickButtonEvent(OpenSettingUI);
        _exitButton.UnBindOnClickButtonEvent(ExitGame);
    }

    private void StartGame()
    {
        GameManager.Instance.StartGame();
    }

    private void OpenSettingUI()
    {
        UIManager.Instance.OpenUI(UIRootType.PopupUI, UIType.SettingUI);
    }

    private void ExitGame()
    {
        Application.Quit();
    }
}
