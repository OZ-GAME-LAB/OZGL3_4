using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.AdaptivePerformance.Provider.AdaptivePerformanceSubsystemDescriptor;

public class TeleportSlot : MonoBehaviour
{
    [SerializeField] private UIButton _teleportButton;
    [SerializeField] private TextMeshProUGUI _teleportName;

    private TeleportInfo _info;
    private event Action<TeleportInfo> _onClick;

    private void OnEnable()
    {
        _teleportButton.BindOnClickButtonEvent(OnClickTeleport);
    }

    private void OnDisable()
    {
        _teleportButton.UnBindOnClickButtonEvent(OnClickTeleport);
    }

    public void Bind(TeleportInfo info, Action<TeleportInfo> onClick)
    {
        _info = info;
        _onClick = onClick;
        _teleportName.text = "테마 " + info.ThemeNumber + " - " + info.TeleportNumber + "번";
    }

    private void OnClickTeleport()
    {
        _onClick?.Invoke(_info);
    }
}
