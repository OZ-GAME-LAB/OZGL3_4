using System.Collections.Generic;
using UnityEngine;

public class TeleportMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject _teleportSlotPrefab;
    [SerializeField] private Transform _slotContent;

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
        TeleportService teleportService = NetworkManager.Instance.TeleportService;
        IReadOnlyList<TeleportInfo> infos = teleportService.GetTeleportInfos(true);
        foreach (TeleportInfo info in infos)
        {
            TeleportSlot slot = GetSlot();
            slot.Bind(info, OnClickSlot);
            _activeSlotList.Add(slot);
        }
    }

    private TeleportSlot GetSlot()
    {
        TeleportSlot slot = _createdSlotList.Count > 0 ? _createdSlotList.Dequeue() : Instantiate(_teleportSlotPrefab, _slotContent).GetComponent<TeleportSlot>();
        slot.transform.SetAsLastSibling();   // 재사용 슬롯도 순서대로 정렬
        slot.gameObject.SetActive(true);
        return slot;
    }

    private void ClearSlot()
    {
        foreach(var slot in _activeSlotList)
        {
            slot.gameObject.SetActive(false);
            _createdSlotList.Enqueue(slot);
        }
        _activeSlotList.Clear();
    }

    private void OnClickSlot(TeleportInfo info)
    {
        Debug.Log($"{info.ThemeNumber} - {info.TeleportNumber}로 텔레포트");
        NetworkManager.Instance.TeleportService.RequestTeleport(info.Id);
    }
}