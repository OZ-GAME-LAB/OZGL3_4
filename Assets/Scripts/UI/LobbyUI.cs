using UnityEngine;

public class LobbyUI : UIBase
{
    [SerializeField] private UIButton _continueButton;
    [SerializeField] private UIButton _startButton;
    [SerializeField] private UIButton _settingButton;
    [SerializeField] private UIButton _exitButton;

    private readonly SaveRepository _saveRepository = new SaveRepository();

    private void OnEnable()
    {
        _continueButton.gameObject.SetActive(_saveRepository.TryLoad(out _));

        _continueButton.BindOnClickButtonEvent(OnClickContinue);
        _startButton.BindOnClickButtonEvent(OnClickNewGame);
        _settingButton.BindOnClickButtonEvent(OpenSettingUI);
        _exitButton.BindOnClickButtonEvent(ExitGame);
    }

    private void OnDisable()
    {
        _continueButton.UnBindOnClickButtonEvent(OnClickContinue);
        _startButton.UnBindOnClickButtonEvent(OnClickNewGame);
        _settingButton.UnBindOnClickButtonEvent(OpenSettingUI);
        _exitButton.UnBindOnClickButtonEvent(ExitGame);
    }

    private void OnClickContinue()
    {

    }

    private void OnClickNewGame()
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
