using UnityEngine;

public class SettingUI : UIBase
{
    [SerializeField] private UIButton _exitButton;

    private void OnEnable()
    {
        _exitButton.BindOnClickButtonEvent(ExitSettingUI);
    }

    private void OnDisable()
    {
        _exitButton.UnBindOnClickButtonEvent(ExitSettingUI);
    }

    private void ExitSettingUI()
    {
        UIManager.Instance.CloseUI(UIRootType.PopupUI, UIType.SettingUI);
    }
}
