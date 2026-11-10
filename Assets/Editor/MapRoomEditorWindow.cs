using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class MapRoomEditorWindow : EditorWindow
{
    private const string DataPath = "Assets/Resources/JsonOutput/MapRoomData.json";
    private const string RegionPath = "Assets/Resources/JsonOutput/MapRegionData.json";

    [Serializable]
    private class RegionList
    {
        public List<MapRegionData> items = new List<MapRegionData>();
    }

    [SerializeField] private MapRoomEditorState _editState;
    private MapRoomEditorDocument _data { get { return _editState.Data; } }
    [SerializeField] private int _selected = -1;
    [SerializeField] private string _loadedText;
    [SerializeField] private string _savedState;
    [SerializeField] private bool _loaded;
    [SerializeField] private Vector2Int _gridCenter;
    [SerializeField] private int _gridRadius = 32;
    [SerializeField] private float _cellSize = 28f;
    [SerializeField] private float _gridHeight = 430f;
    [SerializeField] private bool _largeGrid;
    [SerializeField] private Vector2 _gridScroll;
    private bool _centerGridScroll = true;
    private Vector2 _listScroll;
    private Vector2 _detailScroll;
    private string _search = "";
    private string _message;
    private MessageType _messageType;
    private readonly List<MapRegionData> _regions = new List<MapRegionData>();

    [MenuItem("Tools/Map/Room Data Editor")]
    public static void OpenWindow()
    {
        MapRoomEditorWindow window = GetWindow<MapRoomEditorWindow>("방 데이터 편집기");
        window.minSize = new Vector2(900, 650);
    }

    private void OnEnable()
    {
        if (_editState == null)
        {
            _editState = CreateInstance<MapRoomEditorState>();
            _editState.hideFlags = HideFlags.HideAndDontSave;
            _loaded = false;
        }
        saveChangesMessage = "저장하지 않은 방 데이터가 있습니다.";
        Undo.undoRedoPerformed += OnUndoRedo;
        LoadRegions();
        if (!_loaded) LoadData();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnDestroy()
    {
        if (_editState != null) DestroyImmediate(_editState);
    }

    private void OnUndoRedo()
    {
        UpdateDirtyState();
        Repaint();
    }

    private void LoadRegions()
    {
        _regions.Clear();
        try
        {
            RegionList data = JsonUtility.FromJson<RegionList>("{\"items\":" + File.ReadAllText(RegionPath) + "}");
            if (data != null && data.items != null) _regions.AddRange(data.items);
        }
        catch (Exception ex)
        {
            SetMessage("지역 목록을 읽지 못했습니다: " + ex.Message, MessageType.Error);
        }
    }

    private void LoadData()
    {
        try
        {
            string text = File.ReadAllText(DataPath);
            MapRoomEditorDocument data = JsonUtility.FromJson<MapRoomEditorDocument>("{\"items\":" + text + "}");
            if (data == null || data.items == null || !text.TrimStart().StartsWith("["))
                throw new InvalidDataException("방 데이터는 JSON 배열이어야 합니다.");
            foreach (MapRoomData room in data.items)
            {
                if (room == null || room.OccupiedChunks == null || room.RewardIds == null
                    || room.HiddenIds == null || room.Exits == null)
                    throw new InvalidDataException("비어 있거나 잘못된 방 항목/목록이 있습니다.");
            }
            Undo.ClearUndo(_editState);
            _editState.Data = data;
            _loadedText = text;
            _savedState = JsonUtility.ToJson(_data);
            _loaded = true;
            _selected = _data.items.Count > 0 ? 0 : -1;
            hasUnsavedChanges = false;
            CenterGrid();
            SetMessage("방 데이터 " + _data.items.Count + "개를 불러왔습니다.", MessageType.Info);
        }
        catch (Exception ex)
        {
            SetMessage("불러오기 실패: " + ex.Message, MessageType.Error);
        }
    }

    private MapRoomData SelectedRoom
    {
        get { return _selected >= 0 && _selected < _data.items.Count ? _data.items[_selected] : null; }
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            using (new EditorGUI.DisabledScope(!_loaded || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("저장", EditorStyles.toolbarButton, GUILayout.Width(60))) SaveData();
            }
            if (GUILayout.Button("다시 불러오기", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                if (!hasUnsavedChanges || EditorUtility.DisplayDialog("다시 불러오기", "저장하지 않은 편집을 버리고 다시 불러올까요?", "불러오기", "취소"))
                {
                    LoadRegions();
                    LoadData();
                }
            }
            if (GUILayout.Button("데이터 검사", EditorStyles.toolbarButton, GUILayout.Width(85))) ShowValidation();
            _largeGrid = GUILayout.Toggle(_largeGrid, "격자 크게 보기", EditorStyles.toolbarButton, GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.Label(hasUnsavedChanges ? "● 저장하지 않은 변경" : "저장됨", EditorStyles.miniLabel);
        }
        if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, _messageType);
        if (!_loaded) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            EditorGUILayout.HelpBox("플레이 중에는 저장할 수 없습니다. 저장한 데이터는 플레이를 다시 시작하면 적용됩니다.", MessageType.Info);

        // 저장 기준과 창 상태는 제외하고 실제 편집 데이터만 되돌린다.
        Undo.RecordObject(_editState, "방 데이터 편집");
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawRoomList();
            using (new EditorGUILayout.VerticalScope())
            {
                MapRoomData room = SelectedRoom;
                if (_largeGrid && room != null)
                {
                    EditorGUILayout.LabelField(room.Id + " / " + room.RegionId + " / " + room.Description, EditorStyles.boldLabel);
                    DrawGrid(room);
                }
                else
                {
                    _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                    if (room != null) DrawRoom(room);
                    else EditorGUILayout.HelpBox("왼쪽에서 방을 선택하거나 추가하세요.", MessageType.Info);
                    EditorGUILayout.EndScrollView();
                }
            }
        }
        UpdateDirtyState();
    }

    private void DrawRoomList()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(215)))
        {
            _search = EditorGUILayout.TextField("검색", _search);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            for (int i = 0; i < _data.items.Count; i++)
            {
                MapRoomData room = _data.items[i];
                string label = room.Id + "\n" + room.RegionId + " / " + room.Description;
                if (!string.IsNullOrEmpty(_search) && label.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                Color previous = GUI.backgroundColor;
                if (i == _selected) GUI.backgroundColor = new Color(0.4f, 0.85f, 1f);
                if (GUILayout.Button(label, GUILayout.Height(44)))
                {
                    _selected = i;
                    CenterGrid();
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = previous;
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("+ 방 추가"))
            {
                string region = SelectedRoom != null ? SelectedRoom.RegionId : (_regions.Count > 0 ? _regions[0].Id : "Region_01");
                _data.items.Add(new MapRoomData { Id = NewRoomId("Room_New"), RegionId = region });
                _selected = _data.items.Count - 1;
            }
            using (new EditorGUI.DisabledScope(SelectedRoom == null))
            {
                if (GUILayout.Button("선택 방 복제"))
                {
                    MapRoomData copy = JsonUtility.FromJson<MapRoomData>(JsonUtility.ToJson(SelectedRoom));
                    copy.Id = NewRoomId(copy.Id + "_Copy");
                    // 위치와 게임 요소 ID는 새 방에 맞게 지정해야 한다.
                    copy.OccupiedChunks.Clear();
                    copy.RewardIds.Clear();
                    copy.HiddenIds.Clear();
                    copy.Exits.Clear();
                    _data.items.Add(copy);
                    _selected = _data.items.Count - 1;
                }
                if (GUILayout.Button("선택 방 삭제") && EditorUtility.DisplayDialog("방 삭제", SelectedRoom.Id + " 방을 삭제할까요? 연결된 출입구는 저장 검사에서 확인합니다.", "삭제", "취소"))
                {
                    _data.items.RemoveAt(_selected);
                    _selected = Mathf.Min(_selected, _data.items.Count - 1);
                }
            }
        }
    }

    private string NewRoomId(string prefix)
    {
        string candidate = prefix;
        int suffix = 1;
        while (_data.items.Exists(room => room.Id == candidate)) candidate = prefix + "_" + suffix++;
        return candidate;
    }

    private void DrawRoom(MapRoomData room)
    {
        EditorGUILayout.LabelField("방 기본 정보", EditorStyles.boldLabel);
        room.Id = EditorGUILayout.TextField("방 ID", room.Id);
        room.Description = EditorGUILayout.TextField("방 이름 / 설명", room.Description);
        room.RegionId = EditorGUILayout.TextField("지역 ID", room.RegionId);
        if (_regions.Count > 0)
        {
            string[] labels = new string[_regions.Count + 1];
            labels[0] = "지역 목록에서 선택…";
            for (int i = 0; i < _regions.Count; i++) labels[i + 1] = _regions[i].Id + " / " + _regions[i].Description;
            int choice = EditorGUILayout.Popup("등록된 지역", 0, labels);
            if (choice > 0) room.RegionId = _regions[choice - 1].Id;
        }
        EditorGUILayout.HelpBox("방 ID를 바꾸면 지도 프리팹과 텔레포트 데이터의 참조도 별도로 변경해야 합니다.", MessageType.Info);
        DrawGrid(room);
        EditorGUILayout.LabelField("점유 청크 좌표 (" + room.OccupiedChunks.Count + ")", EditorStyles.boldLabel);
        for (int i = 0; i < room.OccupiedChunks.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                ChunkIndexData chunk = room.OccupiedChunks[i];
                if (chunk == null) { EditorGUILayout.LabelField("잘못된 청크"); }
                else
                {
                    Vector2Int value = EditorGUILayout.Vector2IntField("청크 " + i, new Vector2Int(chunk.X, chunk.Y));
                    chunk.X = value.x;
                    chunk.Y = value.y;
                }
                if (GUILayout.Button("−", GUILayout.Width(24))) { room.OccupiedChunks.RemoveAt(i); break; }
            }
        }
        if (GUILayout.Button("청크 좌표 직접 추가")) room.OccupiedChunks.Add(new ChunkIndexData());
        EditorGUILayout.Space();
        DrawIds("보상 ID", room.RewardIds);
        DrawIds("히든 ID", room.HiddenIds);
        EditorGUILayout.LabelField("출입구", EditorStyles.boldLabel);
        for (int i = 0; i < room.Exits.Count; i++)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                MapRoomExitData exit = room.Exits[i];
                if (exit != null)
                {
                    exit.Id = EditorGUILayout.TextField("출입구 ID", exit.Id);
                    exit.TargetRoomId = EditorGUILayout.TextField("연결 대상 방 ID", exit.TargetRoomId);
                }
                if (GUILayout.Button("출입구 삭제")) { room.Exits.RemoveAt(i); break; }
            }
        }
        if (GUILayout.Button("출입구 추가")) room.Exits.Add(CreateDefaultExit(room));
        if (GUILayout.Button("이전·다음 방 출입구 자동 추가"))
        {
            int added = AddPreviousNextExits(room, _data.items);
            SetMessage("출입구 " + added + "개를 추가했습니다. 없는 방과 이미 연결된 방은 건너뛰었습니다. 대상은 직접 수정할 수 있으며 저장은 별도입니다.", MessageType.Info);
        }
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("이 도구는 방 데이터 JSON을 편집합니다. 실제 월드 방이나 지도 이미지 프리팹을 자동 생성하지 않습니다.", MessageType.Info);
    }

    public static MapRoomExitData CreateDefaultExit(MapRoomData room)
    {
        if (room == null) throw new ArgumentNullException(nameof(room));
        string prefix = (room.Id ?? "") + "_Exit_";
        int suffix = 1;
        string exitId;
        do { exitId = prefix + (suffix++).ToString("D2", CultureInfo.InvariantCulture); }
        while (room.Exits.Exists(exit => exit != null && exit.Id == exitId));

        return new MapRoomExitData { Id = exitId, TargetRoomId = GetNumberedRoomId(room.Id, 1) };
    }

    public static int AddPreviousNextExits(MapRoomData room, IReadOnlyList<MapRoomData> rooms)
    {
        if (room == null) throw new ArgumentNullException(nameof(room));
        if (rooms == null) throw new ArgumentNullException(nameof(rooms));
        int added = 0;
        foreach (int offset in new[] { -1, 1 })
        {
            string targetId = GetNumberedRoomId(room.Id, offset);
            if (string.IsNullOrEmpty(targetId) || room.Exits.Exists(exit => exit != null && exit.TargetRoomId == targetId)) continue;
            bool exists = false;
            foreach (MapRoomData target in rooms)
                if (target != null && target.Id == targetId) { exists = true; break; }
            if (!exists) continue;
            MapRoomExitData exitData = CreateDefaultExit(room);
            exitData.TargetRoomId = targetId;
            room.Exits.Add(exitData);
            added++;
        }
        return added;
    }

    private static string GetNumberedRoomId(string roomId, int offset)
    {
        roomId = roomId ?? "";
        int numberStart = roomId.Length;
        while (numberStart > 0 && roomId[numberStart - 1] >= '0' && roomId[numberStart - 1] <= '9') numberStart--;
        if (numberStart < roomId.Length
            && long.TryParse(roomId.Substring(numberStart), NumberStyles.None, CultureInfo.InvariantCulture, out long number)
            && (offset == -1 ? number > 0 : number < long.MaxValue))
        {
            string nextNumber = (number + offset).ToString(CultureInfo.InvariantCulture).PadLeft(roomId.Length - numberStart, '0');
            return roomId.Substring(0, numberStart) + nextNumber;
        }
        return "";
    }

    private static void DrawIds(string title, List<string> ids)
    {
        EditorGUILayout.LabelField(title + " (" + ids.Count + ")", EditorStyles.boldLabel);
        for (int i = 0; i < ids.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                ids[i] = EditorGUILayout.TextField(ids[i]);
                if (GUILayout.Button("−", GUILayout.Width(24))) { ids.RemoveAt(i); break; }
            }
        }
        if (GUILayout.Button(title + " 추가")) ids.Add("");
        EditorGUILayout.Space();
    }

    private void CenterGrid()
    {
        MapRoomData room = SelectedRoom;
        if (room == null || room.OccupiedChunks.Count == 0 || room.OccupiedChunks[0] == null) return;
        _gridCenter = new Vector2Int(room.OccupiedChunks[0].X, room.OccupiedChunks[0].Y);
        _centerGridScroll = true;
    }

    private void DrawGrid(MapRoomData room)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("청크 배치 — 클릭으로 추가 / 제거", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            _gridCenter = EditorGUILayout.Vector2IntField("격자 중심", _gridCenter);
            if (EditorGUI.EndChangeCheck()) _centerGridScroll = true;
            if (GUILayout.Button("선택 방으로", GUILayout.Width(90))) CenterGrid();
        }
        EditorGUI.BeginChangeCheck();
        _gridRadius = EditorGUILayout.IntSlider("탐색 범위 (중심 ±청크)", _gridRadius, 8, 256);
        if (EditorGUI.EndChangeCheck()) _centerGridScroll = true;
        float oldCellSize = _cellSize;
        _cellSize = EditorGUILayout.Slider("확대 / 축소", _cellSize, 18f, 52f);
        if (!_largeGrid) _gridHeight = EditorGUILayout.Slider("격자 표시 높이", _gridHeight, 260f, 900f);
        EditorGUILayout.LabelField("휠: 세로 이동 / Shift+휠: 가로 이동 / 가운데 버튼 드래그: 이동", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("청록 ●: 선택 방 / 리전별 색 ■: 다른 방 / 빨강 !: 좌표 겹침 / 위쪽: +Y", EditorStyles.miniLabel);
        DrawRegionLegend();

        Rect viewport = _largeGrid
            ? GUILayoutUtility.GetRect(100f, 160f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true))
            : EditorGUILayout.GetControlRect(false, _gridHeight);
        float cell = _cellSize;
        int side = _gridRadius * 2 + 1;
        Rect canvas = new Rect(0, 0, (side + 1) * cell, (side + 1) * cell);
        Vector2 visibleSize = new Vector2(Mathf.Max(1, viewport.width - 16), Mathf.Max(1, viewport.height - 16));
        if (oldCellSize != cell)
            _gridScroll = (_gridScroll + visibleSize * 0.5f) * (cell / oldCellSize) - visibleSize * 0.5f;
        if (_centerGridScroll && Event.current.type == EventType.Repaint)
        {
            _gridScroll = new Vector2((_gridRadius + 1.5f) * cell, (_gridRadius + 1.5f) * cell) - visibleSize * 0.5f;
            _centerGridScroll = false;
        }
        HandleGridPan(viewport, cell);
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && viewport.Contains(Event.current.mousePosition)
            && (Event.current.mousePosition.x < viewport.x + cell || Event.current.mousePosition.y < viewport.y + cell))
            Event.current.Use();
        _gridScroll.x = Mathf.Clamp(_gridScroll.x, 0, Mathf.Max(0, canvas.width - visibleSize.x));
        _gridScroll.y = Mathf.Clamp(_gridScroll.y, 0, Mathf.Max(0, canvas.height - visibleSize.y));
        Dictionary<Vector2Int, List<MapRoomData>> occupied = BuildOccupancy(_data.items);
        // 좌표 범위가 커도 현재 스크롤 화면에 들어오는 셀만 그린다.
        _gridScroll = GUI.BeginScrollView(viewport, _gridScroll, canvas, true, true);
        int minColumn = Mathf.Clamp(Mathf.FloorToInt(_gridScroll.x / cell) - 1, 0, side - 1);
        int maxColumn = Mathf.Clamp(Mathf.CeilToInt((_gridScroll.x + visibleSize.x) / cell), 0, side - 1);
        int minRow = Mathf.Clamp(Mathf.FloorToInt(_gridScroll.y / cell) - 1, 0, side - 1);
        int maxRow = Mathf.Clamp(Mathf.CeilToInt((_gridScroll.y + visibleSize.y) / cell), 0, side - 1);
        for (int row = minRow; row <= maxRow; row++)
        {
            for (int column = minColumn; column <= maxColumn; column++)
            {
                long chunkX = (long)_gridCenter.x - _gridRadius + column;
                long chunkY = (long)_gridCenter.y + _gridRadius - row;
                if (chunkX < int.MinValue || chunkX > int.MaxValue || chunkY < int.MinValue || chunkY > int.MaxValue) continue;
                Vector2Int index = new Vector2Int((int)chunkX, (int)chunkY);
                occupied.TryGetValue(index, out List<MapRoomData> owners);
                bool selected = owners != null && owners.Contains(room);
                bool conflict = owners != null && owners.Count > 1;
                Rect rect = new Rect((column + 1) * cell, (row + 1) * cell, cell - 2, cell - 2);
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = conflict ? new Color(1f, 0.25f, 0.3f)
                    : selected ? new Color(0.3f, 0.85f, 1f) : owners != null ? RegionColor(owners[0].RegionId) : previous;
                string tooltip = index.ToString();
                if (owners != null)
                    foreach (MapRoomData owner in owners) tooltip += "\n" + owner.RegionId + " / " + owner.Id + " / " + owner.Description;
                if (GUI.Button(rect, new GUIContent(conflict ? "!" : selected ? "●" : owners != null ? "■" : "", tooltip)))
                {
                    if (selected) room.OccupiedChunks.RemoveAll(c => c != null && c.X == index.x && c.Y == index.y);
                    else if (owners == null) room.OccupiedChunks.Add(new ChunkIndexData { X = index.x, Y = index.y });
                    else SetMessage(owners[0].RegionId + " / " + owners[0].Id + " 방이 이미 " + index + " 청크를 점유합니다. 리전이 달라도 같은 좌표를 사용할 수 없습니다.", MessageType.Warning);
                }
                GUI.backgroundColor = previous;
            }
        }
        // 축 라벨은 스크롤 화면 가장자리에 고정해 먼 좌표에서도 위치를 읽을 수 있다.
        float axisY = _gridScroll.y;
        float axisX = _gridScroll.x;
        EditorGUI.DrawRect(new Rect(axisX, axisY, visibleSize.x, cell), new Color(0.18f, 0.18f, 0.18f));
        EditorGUI.DrawRect(new Rect(axisX, axisY, cell, visibleSize.y), new Color(0.18f, 0.18f, 0.18f));
        for (int column = minColumn; column <= maxColumn; column++)
            GUI.Label(new Rect((column + 1) * cell, axisY, cell, cell), ((long)_gridCenter.x - _gridRadius + column).ToString(), EditorStyles.centeredGreyMiniLabel);
        for (int row = minRow; row <= maxRow; row++)
            GUI.Label(new Rect(axisX, (row + 1) * cell, cell, cell), ((long)_gridCenter.y + _gridRadius - row).ToString(), EditorStyles.centeredGreyMiniLabel);
        EditorGUI.DrawRect(new Rect(axisX, axisY, cell, cell), new Color(0.18f, 0.18f, 0.18f));
        GUI.Label(new Rect(axisX, axisY, cell, cell), "Y/X", EditorStyles.centeredGreyMiniLabel);
        GUI.EndScrollView();
    }

    private void HandleGridPan(Rect viewport, float cell)
    {
        Event current = Event.current;
        int control = GUIUtility.GetControlID(FocusType.Passive);
        if (current.type == EventType.ScrollWheel && current.shift && viewport.Contains(current.mousePosition))
        {
            _gridScroll.x += current.delta.y * cell * 2;
            current.Use();
        }
        if (current.type == EventType.MouseDown && current.button == 2 && viewport.Contains(current.mousePosition))
        {
            GUIUtility.hotControl = control;
            current.Use();
        }
        if (GUIUtility.hotControl != control) return;
        if (current.type == EventType.MouseDrag)
        {
            _gridScroll -= current.delta;
            current.Use();
            Repaint();
        }
        if (current.type == EventType.MouseUp && current.button == 2)
        {
            GUIUtility.hotControl = 0;
            current.Use();
        }
    }

    private static Color RegionColor(string regionId)
    {
        uint hash = 0;
        foreach (char letter in regionId ?? "") hash = unchecked(hash * 31 + letter);
        return Color.HSVToRGB((hash % 12) / 12f, 0.45f, 0.85f);
    }

    private void DrawRegionLegend()
    {
        HashSet<string> shown = new HashSet<string>();
        using (new EditorGUILayout.HorizontalScope())
        {
            foreach (MapRoomData entry in _data.items)
            {
                if (entry == null || !shown.Add(entry.RegionId ?? "")) continue;
                Color previous = GUI.contentColor;
                GUI.contentColor = RegionColor(entry.RegionId);
                GUILayout.Label("■ " + entry.RegionId, EditorStyles.miniLabel, GUILayout.MaxWidth(140));
                GUI.contentColor = previous;
            }
            GUILayout.FlexibleSpace();
        }
    }

    public static Dictionary<Vector2Int, List<MapRoomData>> BuildOccupancy(IReadOnlyList<MapRoomData> rooms)
    {
        Dictionary<Vector2Int, List<MapRoomData>> occupied = new Dictionary<Vector2Int, List<MapRoomData>>();
        if (rooms == null) return occupied;
        foreach (MapRoomData room in rooms)
        {
            if (room == null || room.OccupiedChunks == null) continue;
            foreach (ChunkIndexData chunk in room.OccupiedChunks)
            {
                if (chunk == null) continue;
                Vector2Int key = new Vector2Int(chunk.X, chunk.Y);
                if (!occupied.TryGetValue(key, out List<MapRoomData> owners))
                {
                    owners = new List<MapRoomData>();
                    occupied.Add(key, owners);
                }
                if (!owners.Contains(room)) owners.Add(room);
            }
        }
        return occupied;
    }

    // 리전에 관계없이 실제 월드의 청크 좌표를 공유한다.
    public static List<string> ValidateRooms(IReadOnlyList<MapRoomData> rooms)
    {
        List<string> errors = new List<string>();
        HashSet<string> roomIds = new HashSet<string>();
        Dictionary<Vector2Int, MapRoomData> occupied = new Dictionary<Vector2Int, MapRoomData>();
        if (rooms == null) { errors.Add("방 목록이 없습니다."); return errors; }
        foreach (MapRoomData room in rooms)
        {
            if (room == null) { errors.Add("비어 있는 방 항목이 있습니다."); continue; }
            if (string.IsNullOrWhiteSpace(room.Id)) errors.Add("방 ID가 비어 있습니다.");
            else if (!roomIds.Add(room.Id)) errors.Add("중복 방 ID: " + room.Id);
        }
        foreach (MapRoomData room in rooms)
        {
            if (room == null) continue;
            string prefix = "[" + room.Id + "] ";
            if (string.IsNullOrWhiteSpace(room.RegionId)) errors.Add(prefix + "지역 ID가 비어 있습니다.");
            if (room.OccupiedChunks == null || room.OccupiedChunks.Count == 0) errors.Add(prefix + "청크를 1개 이상 지정하세요.");
            else
            {
                foreach (ChunkIndexData chunk in room.OccupiedChunks)
                {
                    if (chunk == null) { errors.Add(prefix + "비어 있는 청크가 있습니다."); continue; }
                    Vector2Int key = new Vector2Int(chunk.X, chunk.Y);
                    if (occupied.TryGetValue(key, out MapRoomData owner))
                        errors.Add(prefix + "청크 " + key + " 중복: " + owner.RegionId + " / " + owner.Id + "와 겹칩니다.");
                    else occupied.Add(key, room);
                }
            }
            HashSet<string> elementIds = new HashSet<string>();
            ValidateIds(room.RewardIds, elementIds, prefix + "보상", errors);
            ValidateIds(room.HiddenIds, elementIds, prefix + "히든", errors);
            HashSet<string> exitIds = new HashSet<string>();
            if (room.Exits == null) { errors.Add(prefix + "출입구 목록이 없습니다."); continue; }
            foreach (MapRoomExitData exit in room.Exits)
            {
                if (exit == null) { errors.Add(prefix + "비어 있는 출입구가 있습니다."); continue; }
                if (string.IsNullOrWhiteSpace(exit.Id) || !exitIds.Add(exit.Id)) errors.Add(prefix + "출입구 ID가 비어 있거나 중복됩니다.");
                if (string.IsNullOrWhiteSpace(exit.TargetRoomId) || !roomIds.Contains(exit.TargetRoomId)) errors.Add(prefix + "연결 대상 방이 없습니다: " + exit.TargetRoomId);
            }
        }
        return errors;
    }

    private static void ValidateIds(List<string> ids, HashSet<string> used, string prefix, List<string> errors)
    {
        if (ids == null) { errors.Add(prefix + " 목록이 없습니다."); return; }
        foreach (string id in ids)
            if (string.IsNullOrWhiteSpace(id) || !used.Add(id)) errors.Add(prefix + " ID가 비어 있거나 보상/히든 내에서 중복됩니다: " + id);
    }

    private List<string> ValidateData()
    {
        List<string> errors = ValidateRooms(_data.items);
        foreach (MapRoomData room in _data.items)
            if (room != null && !_regions.Exists(region => region != null && region.Id == room.RegionId))
                errors.Add("[" + room.Id + "] 등록되지 않은 지역: " + room.RegionId);
        return errors;
    }

    private void ShowValidation()
    {
        List<string> errors = ValidateData();
        SetMessage(errors.Count == 0 ? "검사 통과: 방 ID, 지역, 청크, 보상·히든 ID, 출입구 연결이 유효합니다."
            : string.Join("\n", errors), errors.Count == 0 ? MessageType.Info : MessageType.Error);
    }

    private bool SaveData()
    {
        if (!_loaded || EditorApplication.isPlayingOrWillChangePlaymode) return false;
        List<string> errors = ValidateData();
        if (errors.Count > 0) { SetMessage("저장할 수 없습니다.\n" + string.Join("\n", errors), MessageType.Error); return false; }
        string temporary = DataPath + ".tmp";
        try
        {
            if (File.ReadAllText(DataPath) != _loadedText)
                throw new IOException("다른 도구에서 원본 JSON을 변경했습니다. 편집 내용을 확인한 후 다시 불러오세요.");
            string wrapped = JsonUtility.ToJson(_data, true);
            string text = wrapped.Substring(wrapped.IndexOf('['), wrapped.LastIndexOf(']') - wrapped.IndexOf('[') + 1) + "\n";
            string backupFolder = "Library/MapRoomEditorBackups";
            Directory.CreateDirectory(backupFolder);
            string backup = Path.Combine(backupFolder, "MapRoomData-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fffffff") + ".json");
            File.WriteAllText(temporary, text, new UTF8Encoding(false));
            File.Replace(temporary, DataPath, backup);
            _loadedText = text;
            _savedState = JsonUtility.ToJson(_data);
            hasUnsavedChanges = false;
            AssetDatabase.ImportAsset(DataPath);
            SetMessage("저장했습니다. 이전 파일 백업: " + backup + "\n게임 반영은 플레이를 다시 시작하세요.", MessageType.Info);
            return true;
        }
        catch (Exception ex)
        {
            SetMessage("저장 실패: " + ex.Message, MessageType.Error);
            return false;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public override void SaveChanges()
    {
        if (SaveData()) base.SaveChanges();
    }

    public override void DiscardChanges()
    {
        _loaded = false;
        base.DiscardChanges();
    }

    private void UpdateDirtyState()
    {
        hasUnsavedChanges = _loaded && JsonUtility.ToJson(_data) != _savedState;
    }

    private void SetMessage(string message, MessageType type)
    {
        _message = message;
        _messageType = type;
    }
}
