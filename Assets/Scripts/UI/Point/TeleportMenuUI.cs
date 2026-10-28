using System.Collections.Generic;
using UnityEngine;

public class TeleportMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject teleportSlotPrefab;

    private Queue<TeleportSlot> _createdSlotList = new();
    private List<TeleportSlot> _activeSlotList = new();

    private void OnEnable()
    {
        UpdateSlot();
    }

    private void OnDisable()
    {
        ClearSlot();
    }

    private void UpdateSlot()
    {
        TeleportSlot slot = null;
        if (_createdSlotList.Count < 0)
        {
            //slot.
        }
    }

    private void ClearSlot()
    {
        foreach(var slot in _activeSlotList)
        {
            slot.gameObject.SetActive(false);
            _activeSlotList.Remove(slot);
            _createdSlotList.Enqueue(slot);
        }
    }
}