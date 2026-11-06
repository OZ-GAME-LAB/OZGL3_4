using System;
using UnityEngine;

public enum UIRootType 
{
    None,
    BackgroundUI,
    MainUI,
    ContentUI,
    PopupUI,
    VeryFrontUI,
}

public enum UIType
{
    PointUI,
    Map,
    LobbyUI,
    FadeUI,
    SettingUI,
    ChangeCardUI,
}

public static class UIManagerExtension
{
    public static string GetUIPath(this UIManager uiManager, UIRootType uiRootType, UIType uiType)
    {
        string path = string.Empty;

        path = $"UI/{uiRootType}/{uiType}";
        return path;
    }

    public static void OpenPointUI(this UIManager uIManager, string teleportId)
    {
        var uiBase = uIManager.OpenUI(UIRootType.PopupUI, UIType.PointUI);
        if(uiBase is PointUI pointUI)
        {
            pointUI.SetCurrentTeleportId(teleportId);
        }
    }
}