using System;
using UnityEngine;

public class PointDefaultMenuUI : MonoBehaviour
{
    [SerializeField] private UIButton _saveButton;
    [SerializeField] private UIButton _teleportButton;

    private string _currentTeleportId;

    public event Action OnClickTeleport;

    private void OnEnable()
    {
        _saveButton.BindOnClickButtonEvent(OnClickedSaveButton);
        _teleportButton.BindOnClickButtonEvent(OnClickedTeleportButton);
    }

    private void OnDisable()
    {
        _saveButton.UnBindOnClickButtonEvent(OnClickedSaveButton);
        _teleportButton.UnBindOnClickButtonEvent(OnClickedTeleportButton);
    }

    public void SetCurrentTeleportId(string teleportId)
    {
        _currentTeleportId = teleportId;
    }

    private void OnClickedSaveButton()
    {
        NetworkManager.Instance.SaveGame(_currentTeleportId);
    }

    private void OnClickedTeleportButton()
    {
        OnClickTeleport?.Invoke();
    }
}