using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TeleportModel
{
    [SerializeField] private List<string> _activatedTeleportIds = new List<string>();

    public List<string> ActivatedTeleportIds
    {
        get
        {
            return _activatedTeleportIds;
        }
    }
}
