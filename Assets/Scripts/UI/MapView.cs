using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class MapView : UIBase
{
    [SerializeField] private RectTransform Transform_MapContent;
    [SerializeField] private RectTransform Transform_MapOrigin;
    [SerializeField] private Vector2 _chunkSize = new Vector2(250f, 250f);
    [SerializeField] private string _roomSpriteFolder = "UI/MapRooms";
    [SerializeField] private float _viewPadding = 24f;
    [SerializeField, Range(0.01f, 1f)] private float _displayScale = 1f;

    private MapViewModel _mapViewModel;
    private readonly Dictionary<string, MapRoomView> _roomViews = new Dictionary<string, MapRoomView>();

    public void Init(MapViewModel mapViewModel)
    {
        if (mapViewModel == null || Transform_MapContent == null || Transform_MapOrigin == null || _chunkSize.x <= 0f
            || !Mathf.Approximately(_chunkSize.x, _chunkSize.y))
        {
            Debug.LogError("[MapView] ViewModel, MapContent, MapOrigin과 동일한 양수 청크 가로·세로 크기를 설정하세요.");
            return;
        }
        if (_mapViewModel == mapViewModel)
        {
            FitCurrentRegion();
            return;
        }

        UnBind();
        ClearRoomViews();
        _mapViewModel = mapViewModel;
        Transform_MapContent.SetParent(Transform_MapOrigin, false);
        Transform_MapContent.anchorMin = Transform_MapContent.anchorMax = Transform_MapOrigin.pivot;
        Transform_MapContent.pivot = Vector2.zero;
        Transform_MapContent.sizeDelta = Vector2.zero;

        Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        foreach (Sprite sprite in Resources.LoadAll<Sprite>(_roomSpriteFolder))
        {
            string roomId = GetRoomIdFromImageName(sprite.name);
            if (!sprites.TryAdd(roomId, sprite)) Debug.LogWarning("[MapView] 이미지 방 ID 중복: " + roomId);
        }
        foreach (MapRoomViewModel room in _mapViewModel.Rooms)
        {
            if (!sprites.TryGetValue(room.RoomId, out Sprite sprite)) continue;
            Rect layout = GetRoomLayout(room.OccupiedChunks, _chunkSize.x);
            if (!Mathf.Approximately(sprite.rect.width / sprite.rect.height, layout.width / layout.height))
            {
                Debug.LogError("[MapView] 이미지와 청크 영역의 가로세로 비율이 다릅니다: " + room.RoomId);
                continue;
            }
            CreateRoomView(room, sprite, layout);
        }
        if (_roomViews.Count == 0) Debug.LogWarning("[MapView] 표시할 방 이미지가 없습니다: Resources/" + _roomSpriteFolder);
        if (isActiveAndEnabled) Bind();
        FitCurrentRegion();
    }

    public static string GetRoomIdFromImageName(string imageName)
    {
        const string suffix = "_minimap";
        return imageName != null && imageName.EndsWith(suffix, StringComparison.Ordinal)
            ? imageName.Substring(0, imageName.Length - suffix.Length) : imageName;
    }

    public static Rect GetRoomLayout(IReadOnlyList<Vector2Int> chunks, float chunkSize)
    {
        if (chunks == null || chunks.Count == 0 || chunkSize <= 0f)
            throw new ArgumentException("방의 점유 청크와 양수 청크 크기가 필요합니다.");
        int minX = chunks[0].x, minY = chunks[0].y, maxX = minX, maxY = minY;
        foreach (Vector2Int chunk in chunks)
        {
            minX = Mathf.Min(minX, chunk.x);
            minY = Mathf.Min(minY, chunk.y);
            maxX = Mathf.Max(maxX, chunk.x);
            maxY = Mathf.Max(maxY, chunk.y);
        }
        return new Rect(minX * chunkSize, minY * chunkSize,
            ((long)maxX - minX + 1) * chunkSize, ((long)maxY - minY + 1) * chunkSize);
    }

    private void CreateRoomView(MapRoomViewModel room, Sprite sprite, Rect layout)
    {
        GameObject roomObject = new GameObject(room.RoomId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MapRoomView));
        roomObject.layer = gameObject.layer;
        RectTransform roomTransform = (RectTransform)roomObject.transform;
        roomTransform.SetParent(Transform_MapContent, false);
        roomTransform.anchorMin = roomTransform.anchorMax = Vector2.zero;
        roomTransform.pivot = Vector2.zero;
        roomTransform.anchoredPosition = layout.position;
        roomTransform.sizeDelta = layout.size;
        Image image = roomObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.useSpriteMesh = false;
        image.raycastTarget = false;
        MapRoomView roomView = roomObject.GetComponent<MapRoomView>();
        roomView.Init(_mapViewModel, room, true);
        _roomViews.Add(room.RoomId, roomView);
    }

    private void OnEnable()
    {
        Bind();
        FitCurrentRegion();
    }

    private void OnDisable()
    {
        UnBind();
    }

    private void OnDestroy()
    {
        UnBind();
    }

    private void Bind()
    {
        if (_mapViewModel == null) return;
        _mapViewModel.PropertyChanged -= OnChangeMapProperty;
        _mapViewModel.PropertyChanged += OnChangeMapProperty;
    }

    private void UnBind()
    {
        if (_mapViewModel != null) _mapViewModel.PropertyChanged -= OnChangeMapProperty;
    }

    private void OnChangeMapProperty(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(MapViewModel.CurrentRegionId)) FitCurrentRegion();
    }

    private void OnRectTransformDimensionsChange()
    {
        FitCurrentRegion();
    }

    private void FitCurrentRegion()
    {
        if (_mapViewModel == null || Transform_MapContent == null) return;
        bool hasBounds = false;
        Vector2 min = Vector2.zero, max = Vector2.zero;
        foreach (MapRoomViewModel room in _mapViewModel.Rooms)
        {
            if (!_roomViews.TryGetValue(room.RoomId, out MapRoomView view)) continue;
            bool visible = room.RegionId == _mapViewModel.CurrentRegionId;
            view.gameObject.SetActive(visible);
            if (!visible) continue;
            Rect rect = GetRoomLayout(room.OccupiedChunks, _chunkSize.x);
            min = hasBounds ? Vector2.Min(min, rect.min) : rect.min;
            max = hasBounds ? Vector2.Max(max, rect.max) : rect.max;
            hasBounds = true;
        }
        if (!hasBounds) return;
        Rect viewport = ((RectTransform)transform).rect;
        Vector2 size = max - min;
        float scale = Mathf.Min(1f, Mathf.Max(1f, viewport.width - _viewPadding * 2f) / size.x,
            Mathf.Max(1f, viewport.height - _viewPadding * 2f) / size.y);
        scale *= Mathf.Clamp(_displayScale, 0.01f, 1f);
        Transform_MapContent.localScale = new Vector3(scale, scale, 1f);
        Transform_MapContent.anchoredPosition = Vector2.zero;
    }

    private void ClearRoomViews()
    {
        foreach (MapRoomView view in _roomViews.Values)
        {
            if (view == null) continue;
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
        }
        _roomViews.Clear();
    }
}
