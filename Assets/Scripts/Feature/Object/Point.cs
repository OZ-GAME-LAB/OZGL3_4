using UnityEngine;

public class Point : MonoBehaviour, IInteractable
{
    public Vector2 Position => transform.position;

    [SerializeField] private string _pointId;
    [SerializeField] private ShadowType _acquireShadow;
    private bool _isTouched = false;

    public void Interact(Player player)
    {
        if(_isTouched == false)
        {
            _isTouched = true;
            var playerViewModel = NetworkManager.Instance.PlayerService.GetPlayerViewModel();
            playerViewModel.AddShadow(_acquireShadow);
            Debug.Log($"{_acquireShadow.ToString()} 획득");
        }

        Debug.Log("포인트 접촉");
    }
}