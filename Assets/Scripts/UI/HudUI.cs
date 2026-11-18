using UnityEngine;

public class HudUI : UIBase
{
    [SerializeField] private UIButton _menuButton;

    private void OnEnable()
    {
        _menuButton.BindOnClickButtonEvent(OnClickMenu);
    }

    private void OnDisable()
    {
        _menuButton.UnBindOnClickButtonEvent(OnClickMenu);
    }

    private void OnClickMenu()
    {
        UIManager.Instance.OpenUI(UIRootType.PopupUI, UIType.MenuUI);
    }
}
