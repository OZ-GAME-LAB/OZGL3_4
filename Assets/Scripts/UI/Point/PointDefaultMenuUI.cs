using System;
using UnityEngine;

public class PointDefaultMenuUI : MonoBehaviour
{
    [SerializeField] private UIButton _saveButton;
    [SerializeField] private UIButton _loadButton;
    [SerializeField] private UIButton _teleportButton;

    public event Action OnClickTeleport;

    public void OnEnable()
    {
        _saveButton.BindOnClickButtonEvent(OnClickedSaveButton);
        _loadButton.BindOnClickButtonEvent(OnClickedLoadButton);
        _teleportButton.BindOnClickButtonEvent(OnClickedTeleportButton);
    }

    public void OnDisable()
    {
        _saveButton.UnBindOnClickButtonEvent(OnClickedSaveButton);
        _loadButton.UnBindOnClickButtonEvent(OnClickedLoadButton);
        _teleportButton.UnBindOnClickButtonEvent(OnClickedTeleportButton);
    }

    private void OnClickedSaveButton()
    {

    }

    private void OnClickedLoadButton()
    {

    }

    private void OnClickedTeleportButton()
    {
        OnClickTeleport?.Invoke();
    }
}