using UnityEngine;

public class PointUI : UIBase
{
    [SerializeField] private UIButton _exitButton;

    private PointDefaultMenuUI _pointDefaultMenuUI;
    private TeleportMenuUI _teleportMenuUI;

    private void Awake()
    {
        _pointDefaultMenuUI = GetComponentInChildren<PointDefaultMenuUI>();
        _teleportMenuUI = GetComponentInChildren<TeleportMenuUI>();
    }

    private void OnEnable()
    {
        _exitButton.BindOnClickButtonEvent(ClosePointUI);

        _pointDefaultMenuUI.gameObject.SetActive(true);
        _teleportMenuUI.gameObject.SetActive(false);

        _pointDefaultMenuUI.OnClickTeleport += OpenTeleportMenu;
    }

    private void OnDisable()
    {
        _exitButton.UnBindOnClickButtonEvent(ClosePointUI);
        _pointDefaultMenuUI.OnClickTeleport -= OpenTeleportMenu;
    }

    private void OpenTeleportMenu()
    {
        _pointDefaultMenuUI.gameObject.SetActive(false);
        _teleportMenuUI.gameObject.SetActive(true);
    }

    private void ClosePointUI()
    {
        UIManager.Instance.CloseUI(UIRootType.PopupUI, UIType.PointUI);
    }
}
