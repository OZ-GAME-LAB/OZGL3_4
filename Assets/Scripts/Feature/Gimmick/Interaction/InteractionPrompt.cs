using UnityEngine;

/// <summary>
/// 상호작용 말주머니 표시 컴포넌트입니다. 플레이어가 이 오브젝트의 트리거 범위 안에 있는 동안 말주머니를 보여줍니다.
/// 상호작용 자체는 플레이어의 PlayerInteractor가 처리하고, 이 컴포넌트는 표시만 담당합니다.
///
/// 배치 방법
/// - IInteractable 기믹과 같은 오브젝트(트리거 콜라이더가 있는 곳)에 추가합니다.
/// - GameObject_Prompt에 말주머니 오브젝트를 연결합니다.
/// </summary>
public class InteractionPrompt : MonoBehaviour
{
    [SerializeField] private GameObject GameObject_Prompt;

    private int _playerOverlapCount;

    public bool IsPlayerInRange
    {
        get { return _playerOverlapCount > 0; }
    }

    private void OnEnable()
    {
        _playerOverlapCount = 0;
        GimmickUtil.SetActiveSafe(GameObject_Prompt, false);
    }

    private void OnDisable()
    {
        _playerOverlapCount = 0;
        GimmickUtil.SetActiveSafe(GameObject_Prompt, false);
    }

    /// <summary>
    /// 말주머니를 강제로 숨기거나 다시 보여줍니다. (기믹이 작동 중일 때 숨기는 용도)
    /// 플레이어가 범위 밖이면 isVisible이 true여도 표시되지 않습니다.
    /// </summary>
    public void SetVisible(bool isVisible)
    {
        GimmickUtil.SetActiveSafe(GameObject_Prompt, isVisible && IsPlayerInRange);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player;
        if (GimmickUtil.TryGetFromPlayerCollider(other, out player) == false)
            return;

        _playerOverlapCount++;
        GimmickUtil.SetActiveSafe(GameObject_Prompt, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Player player;
        if (GimmickUtil.TryGetFromPlayerCollider(other, out player) == false)
            return;

        _playerOverlapCount = Mathf.Max(0, _playerOverlapCount - 1);
        if (_playerOverlapCount == 0)
        {
            GimmickUtil.SetActiveSafe(GameObject_Prompt, false);
        }
    }
}
