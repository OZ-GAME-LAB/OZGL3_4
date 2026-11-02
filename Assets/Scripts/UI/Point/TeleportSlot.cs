using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeleportSlot : MonoBehaviour
{
    [SerializeField] private UIButton _teleportButton;
    [SerializeField] private TextMeshProUGUI _teleportName;

    private void OnEnable()
    {
        _teleportButton.BindOnClickButtonEvent(OnClickTeleport);
    }

    private void OnDisable()
    {
        _teleportButton.UnBindOnClickButtonEvent(OnClickTeleport);
    }

    private void OnClickTeleport()
    {

    }
}
