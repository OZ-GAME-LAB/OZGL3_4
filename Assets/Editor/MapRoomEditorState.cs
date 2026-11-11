using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MapRoomEditorDocument
{
    public List<MapRoomData> items = new List<MapRoomData>();
}

// 저장 대상 에셋과 분리한 임시 편집 상태. Undo 및 스크립트 리로드 시 복원에 사용한다.
public class MapRoomEditorState : ScriptableObject
{
    public MapRoomEditorDocument Data = new MapRoomEditorDocument();
}
