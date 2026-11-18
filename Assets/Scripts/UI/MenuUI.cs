using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuUI: UIBase
{
    [SerializeField] private UIButton _settingButton;
    [SerializeField] private UIButton _lobbyButton;
    [SerializeField] private UIButton _quitButton;
    [SerializeField] private UIButton _exitButton;

    private void OnEnable()
    {
        _settingButton.BindOnClickButtonEvent(OpenSettingUI);
        _lobbyButton.BindOnClickButtonEvent(OnClickLobby);
        _quitButton.BindOnClickButtonEvent(OnClickQuitGame);
        _exitButton.BindOnClickButtonEvent(ExitGame);
    }

    private void OnDisable()
    {
        _settingButton.UnBindOnClickButtonEvent(OpenSettingUI);
        _lobbyButton.UnBindOnClickButtonEvent(OnClickLobby);
        _quitButton.UnBindOnClickButtonEvent(OnClickQuitGame);
        _exitButton.UnBindOnClickButtonEvent(ExitGame);
    }

    private void OpenSettingUI()
    {
        UIManager.Instance.OpenUI(UIRootType.PopupUI, UIType.SettingUI);
    }


    private void OnClickLobby()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnClickQuitGame()
    {
        Application.Quit();
    }

    private void ExitGame()
    {
        UIManager.Instance.CloseUI(UIRootType.PopupUI, UIType.MenuUI);
    }
}
