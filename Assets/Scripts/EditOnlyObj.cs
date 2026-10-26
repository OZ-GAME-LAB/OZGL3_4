using UnityEngine;

/// <summary>
/// 편집용 오브젝트에 붙이면 플레이 모드 시작 시 자동으로 비활성화된다.
/// 맵 편집용으로 씬에 배치한 지역이 RegionRoot에 동적 생성되는 지역과 겹치지 않게 하기 위한 용도.
/// </summary>
public class EditOnlyObj : MonoBehaviour
{
    private void Awake()
    {
        gameObject.SetActive(false);
    }
}