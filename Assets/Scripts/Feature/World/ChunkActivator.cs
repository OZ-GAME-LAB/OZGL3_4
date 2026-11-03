using System.Collections.Generic;
using UnityEngine;

public class ChunkActivator
{
    private Dictionary<Vector2Int, MapChunk> _chunks = new Dictionary<Vector2Int, MapChunk>();
    private HashSet<Room> _activeRooms = new HashSet<Room>();
    public bool IsDirty { get; private set; }
    public int Count => _chunks.Count;


    public void Register(Vector2Int key, MapChunk chunk)
    {
        if (_chunks.ContainsKey(key))
        {
            Debug.LogWarning($"[ChunkActivator] (Register) 청크 좌표 중복: {key} / {chunk.name}");

            return;
        }

        _chunks.Add(key, chunk);

        Debug.Log($"[ChunkActivator] (Register) 등록: {key} / {chunk.name} / 총 {_chunks.Count}개");

        IsDirty = true;
    }

    public void Unregister(Vector2Int key, MapChunk chunk)
    {
        if (_chunks.TryGetValue(key, out MapChunk saved) && saved == chunk)
        {
            _chunks.Remove(key);

            IsDirty = true;

            Debug.Log($"[ChunkActivator] (Unregister) 해제: {key} / {chunk.name} / 총 {_chunks.Count}개");
        }
    }

    public bool TryGetChunk(Vector2Int key, out MapChunk chunk)
    {
        return _chunks.TryGetValue(key, out chunk);
    }

    public void Refresh(Vector2Int center, int activeRadius)
    {
        CollectActiveRooms(center, activeRadius);

        foreach (KeyValuePair<Vector2Int, MapChunk> pair in _chunks)
        {
            pair.Value.SetObjectActive(ShouldBeActive(pair.Key, pair.Value, center, activeRadius));
        }

        IsDirty = false;
    }


    private bool IsInActiveRange(Vector2Int chunkIndex, Vector2Int center, int activeRadius) //사거리 내의 청크들의 On/Off를 지시
    {
        int distanceX = Mathf.Abs(chunkIndex.x - center.x);
        int distanceY = Mathf.Abs(chunkIndex.y - center.y);

        return distanceX <= activeRadius && distanceY <= activeRadius;
    }

    private void CollectActiveRooms(Vector2Int center, int activeRadius)
    {
        _activeRooms.Clear();
        foreach (KeyValuePair<Vector2Int, MapChunk> pair in _chunks)
        {
            if (IsGroupChunk(pair.Value) && IsInActiveRange(pair.Key, center, activeRadius))
            {
                _activeRooms.Add(pair.Value.Room);
            }
        }
    }

    private bool IsGroupChunk(MapChunk chunk)
    {
        return chunk.Room != null && chunk.Room.ActivationMode == RoomActivationMode.Group;
    }

    private bool ShouldBeActive(Vector2Int key, MapChunk chunk, Vector2Int center, int activeRadius)
    {
        if (IsGroupChunk(chunk))
        {
            return _activeRooms.Contains(chunk.Room);
        }

        return IsInActiveRange(key, center, activeRadius);
    }
}
