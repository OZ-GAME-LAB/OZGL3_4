using UnityEngine;
using UnityEngine.UI;

public class PlayerTestUI : MonoBehaviour
{
    private PlayerViewModel _playerViewModel;

    [SerializeField] private Button _defaultButton;
    [SerializeField] private Button _mouseButton;
    [SerializeField] private Button _rabbitButton;

    private void Start()
    {
        _playerViewModel = NetworkManager.Instance.PlayerService.GetPlayerViewModel();
    }

    private void OnEnable()
    {
        _defaultButton.onClick.AddListener(() => ChangeShadow(ShadowType.Default));
        _mouseButton.onClick.AddListener(() => ChangeShadow(ShadowType.Mouse));
        _rabbitButton.onClick.AddListener(() => ChangeShadow(ShadowType.Rabbit));
    }

    private void OnDisable()
    {
        _defaultButton.onClick.RemoveListener(() => ChangeShadow(ShadowType.Default));
        _mouseButton.onClick.RemoveListener(() => ChangeShadow(ShadowType.Mouse));
        _rabbitButton.onClick.RemoveListener(() => ChangeShadow(ShadowType.Rabbit));
    }

    public void ChangeShadow(ShadowType type)
    {
        _playerViewModel.AddShadow(type);
        _playerViewModel.ChangeShadow(type);
    }
}