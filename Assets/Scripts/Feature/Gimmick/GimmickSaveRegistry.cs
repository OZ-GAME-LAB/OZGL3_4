using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// 기믹 하나의 저장 내용. 기믹 종류마다 필요한 칸만 사용한다.
[System.Serializable]
public class GimmickSaveState
{
    public string Id;
    public bool IsOn;       // 스위치 켜짐, 문 열림, 빛 켜짐 등
    public bool IsUsed;     // 한 번만 눌리는 스위치의 사용 여부, 발판의 눌림 고정
    public float ValueA;    // 이동 광원의 현재 위치, 수레의 가로 위치 등
    public float ValueB;    // 이동 광원의 목표 위치
    public int IntValue;    // 회전 광원의 단계
}

// 세이브에 상태를 남기는 기믹이 구현한다.
public interface ISaveableGimmick
{
    void CaptureState(GimmickSaveState state);   // 지금 상태를 state에 적는다
    void RestoreState(GimmickSaveState state);   // state에 적힌 상태로 되돌린다
}

/// <summary>
/// 기믹 상태 저장/복원 창구입니다.
/// - 기믹은 Start에서 Register, OnDestroy에서 Unregister를 호출합니다.
/// - NetworkManager.SaveGame에서 CollectStates로 전체 상태를 모아 저장합니다.
/// - NetworkManager.LoadGame에서 ApplyLoadedStates로 불러온 상태를 넘깁니다.
///   이미 떠 있는 기믹은 바로 복원하고, 아직 없는 기믹(다른 지역 등)은 나중에 등록될 때 복원합니다.
/// - 기믹 ID는 종류 + 하이어라키 경로 + 처음 위치로 자동으로 만듭니다.
///   (기믹 이름을 바꾸거나 위치를 옮기면 그 기믹의 예전 저장 내용은 적용되지 않습니다)
/// - 복원은 불러올 때 한 번만 합니다. 이후 사망 초기화(PuzzleResetZone)로 꺼졌다 켜지면 시작 상태로 돌아갑니다.
/// </summary>
public static class GimmickSaveRegistry
{
    private static Dictionary<string, ISaveableGimmick> _gimmicksById = new Dictionary<string, ISaveableGimmick>();
    private static Dictionary<ISaveableGimmick, string> _idsByGimmick = new Dictionary<ISaveableGimmick, string>();

    // 아직 등록되지 않은 기믹의 저장 내용 (불러온 것, 또는 사라진 기믹이 남기고 간 것)
    private static Dictionary<string, GimmickSaveState> _pendingStates = new Dictionary<string, GimmickSaveState>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        _gimmicksById.Clear();
        _idsByGimmick.Clear();
        _pendingStates.Clear();
    }

    /// <summary>
    /// 기믹을 등록합니다. 이 기믹의 저장 내용이 대기 중이면 바로 복원합니다.
    /// </summary>
    public static void Register(ISaveableGimmick gimmick, Component owner)
    {
        if (gimmick == null || owner == null || _idsByGimmick.ContainsKey(gimmick))
            return;

        string id = BuildId(owner);
        if (_gimmicksById.ContainsKey(id))
        {
            Debug.LogWarning($"[GimmickSaveRegistry] 저장 ID가 겹쳐서 이 기믹은 저장되지 않습니다: {id}", owner);
            return;
        }

        _gimmicksById.Add(id, gimmick);
        _idsByGimmick.Add(gimmick, id);

        GimmickSaveState pendingState;
        if (_pendingStates.TryGetValue(id, out pendingState))
        {
            _pendingStates.Remove(id);
            gimmick.RestoreState(pendingState);
        }
    }

    /// <summary>
    /// 기믹 등록을 해제합니다. 마지막 상태는 남겨 두었다가 저장에 포함하고, 다시 등록되면 복원합니다.
    /// </summary>
    public static void Unregister(ISaveableGimmick gimmick)
    {
        string id;
        if (gimmick == null || _idsByGimmick.TryGetValue(gimmick, out id) == false)
            return;

        // 지역만 내려간 경우에는 마지막 상태를 보관합니다. 씬 전체가 닫히는 중이면(로비 이동, 종료) 보관하지 않습니다.
        Component owner = gimmick as Component;
        if (owner != null && owner.gameObject.scene.isLoaded)
        {
            _pendingStates[id] = Capture(id, gimmick);
        }

        _idsByGimmick.Remove(gimmick);
        _gimmicksById.Remove(id);
    }

    /// <summary>
    /// 저장할 전체 기믹 상태를 모읍니다.
    /// </summary>
    public static List<GimmickSaveState> CollectStates()
    {
        List<GimmickSaveState> states = new List<GimmickSaveState>();

        foreach (KeyValuePair<string, GimmickSaveState> pair in _pendingStates)
        {
            states.Add(pair.Value);
        }

        foreach (KeyValuePair<string, ISaveableGimmick> pair in _gimmicksById)
        {
            states.Add(Capture(pair.Key, pair.Value));
        }

        return states;
    }

    /// <summary>
    /// 불러온 기믹 상태를 적용합니다. 등록된 기믹은 바로 복원하고, 나머지는 등록될 때까지 보관합니다.
    /// </summary>
    public static void ApplyLoadedStates(List<GimmickSaveState> states)
    {
        _pendingStates.Clear();

        if (states == null)
            return;

        for (int i = 0; i < states.Count; i++)
        {
            GimmickSaveState state = states[i];
            if (state == null || string.IsNullOrEmpty(state.Id))
                continue;

            ISaveableGimmick gimmick;
            if (_gimmicksById.TryGetValue(state.Id, out gimmick))
            {
                gimmick.RestoreState(state);
            }
            else
            {
                _pendingStates[state.Id] = state;
            }
        }
    }

    private static GimmickSaveState Capture(string id, ISaveableGimmick gimmick)
    {
        GimmickSaveState state = new GimmickSaveState();
        gimmick.CaptureState(state);
        state.Id = id;
        return state;
    }

    // 종류 + 지역부터의 하이어라키 경로 + 처음 위치로 ID를 만든다.
    private static string BuildId(Component owner)
    {
        Transform current = owner.transform;
        string path = current.name;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;

            if (current.GetComponent<Region>() != null)
                break;
        }

        Vector3 localPosition = owner.transform.localPosition;
        string x = localPosition.x.ToString("F2", CultureInfo.InvariantCulture);
        string y = localPosition.y.ToString("F2", CultureInfo.InvariantCulture);
        return $"{owner.GetType().Name}:{path}@{x},{y}";
    }
}
