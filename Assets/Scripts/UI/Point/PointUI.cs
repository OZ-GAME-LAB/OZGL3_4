using UnityEngine;

public class PointUI : UIBase
{
    private PointDefaultMenuUI _pointDefaultMenuUI;
    private TeleportMenuUI _teleportMenuUI;

    private void Awake()
    {
        _pointDefaultMenuUI = GetComponentInChildren<PointDefaultMenuUI>();
        _teleportMenuUI = GetComponentInChildren<TeleportMenuUI>();
    }

    private void OnEnable()
    {
        _pointDefaultMenuUI.gameObject.SetActive(true);
        _teleportMenuUI.gameObject.SetActive(false);

        _pointDefaultMenuUI.OnClickTeleport += OpenTeleportMenu;
    }

    private void OnDisable()
    {
        _pointDefaultMenuUI.OnClickTeleport -= OpenTeleportMenu;
    }

    private void OpenTeleportMenu()
    {
        _pointDefaultMenuUI.gameObject.SetActive(false);
        _teleportMenuUI.gameObject.SetActive(true);
    }
}
