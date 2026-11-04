#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 생성된 맵 프리팹의 룰 타일 모양을 다시 계산하는 에디터 도구입니다.
/// 프리팹 모드에서 Region 루트(또는 Room, Tile)를 선택한 뒤 메뉴 Tools > Map > Refresh Tilemaps In Selection 을 실행하세요.
/// (생성된 Ground 타일맵은 기본 스프라이트 하나로 저장되어 있어, 한 번 새로 고쳐야 룰 타일이 이웃에 맞게 바뀝니다)
/// </summary>
public static class TilemapRefreshTool
{
    [MenuItem("Tools/Map/Refresh Tilemaps In Selection")]
    private static void RefreshTilemapsInSelection()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            Debug.LogWarning("[TilemapRefreshTool] 선택된 오브젝트가 없습니다. 하이어라키에서 Region 루트를 선택해 주세요.");
            return;
        }

        int refreshCount = 0;

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            Tilemap[] tilemaps = selectedObjects[i].GetComponentsInChildren<Tilemap>(true);
            for (int j = 0; j < tilemaps.Length; j++)
            {
                Undo.RegisterCompleteObjectUndo(tilemaps[j], "Refresh Tilemap");
                tilemaps[j].RefreshAllTiles();
                EditorUtility.SetDirty(tilemaps[j]);
                refreshCount++;
            }
        }

        Debug.Log($"[TilemapRefreshTool] 타일맵 {refreshCount}개를 새로 고쳤습니다. 프리팹 모드라면 저장(Ctrl+S)해 주세요.");
    }
}
#endif
