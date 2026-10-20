using UnityEngine;

public enum RoomActivationMode { Group, PerChunk }

public class Room : MonoBehaviour
{
    [Header("방 정보")]
    [SerializeField] private string _roomId;
    [SerializeField] private RoomActivationMode _activationMode = RoomActivationMode.Group;

    public string RoomId => _roomId;
    public RoomActivationMode ActivationMode => _activationMode;

    public void Start()
    {
        if (string.IsNullOrEmpty(_roomId))
        {
            Debug.LogWarning($"[Room] (Start) {name}: RoomId 없음");
        }
    }
}
