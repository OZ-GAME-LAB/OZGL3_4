using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class Point : MonoBehaviour, IInteractable
{
    public Vector2 Position => transform.position;

    [SerializeField] private string _pointId;

    [Header("그림자 획득")]
    [SerializeField] private bool _grantsShadow;          // 그림자를 주는 포인트만 체크
    [SerializeField] private ShadowType _acquireShadow = ShadowType.Default;

    [Header("활성화 조건")]
    [SerializeField] private bool _canActivateByTouch = true;           // 1번 포인트는 체크 해제
    [SerializeField] private List<string> _linkedPointIds = new();      // 2번 포인트에 1번 Id 입력

    [Header("표시 (선택)")]
    [SerializeField] private GameObject _activeVisual;
    [SerializeField] private GameObject _inactiveVisual;

    private Animator _animator;
    private TeleportService _teleportService;
    private TeleportViewModel _teleportViewModel;

    private bool IsActivated => _teleportViewModel.GetTeleportActivated(_pointId);
    private static readonly int IsActiveHash = Animator.StringToHash("IsActive");
    private bool ShowActiveMotion => _canActivateByTouch || IsActivated;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();

        _teleportService = NetworkManager.Instance.TeleportService;
        _teleportViewModel = _teleportService.GetTeleportViewModel();

        _teleportService.RegisterTeleportPosition(_pointId, Position);
        _teleportViewModel.PropertyChanged += OnTeleportChanged;
        RefreshVisual();

        _animator.SetBool(IsActiveHash, ShowActiveMotion);
    }

    private void OnDestroy()
    {
        if (_teleportViewModel != null)
        {
            _teleportViewModel.PropertyChanged -= OnTeleportChanged;
        }
    }

    public void Interact(Player player)
    {
        if (IsActivated == false)
        {
            if (_canActivateByTouch == false)
            {
                Debug.Log($"[Point] 아직 비활성화된 포인트: {_pointId}");
                return;   // UI도 열지 않음
            }

            _teleportService.RequestActivateTeleport(_pointId);

            if (_grantsShadow)
            {
                var playerViewModel = NetworkManager.Instance.PlayerService.GetPlayerViewModel();
                playerViewModel.AddShadow(_acquireShadow);
                Debug.Log($"{_acquireShadow} 획득");
            }
        }

        // 연결된 포인트 함께 활성화 (이미 활성화된 건 false 반환되며 무시됨)
        foreach (string linkedId in _linkedPointIds)
        {
            _teleportService.RequestActivateTeleport(linkedId);
            
        }

        Debug.Log("포인트 접촉");
        UIManager.Instance.OpenPointUI(_pointId);
    }

    private void OnTeleportChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TeleportViewModel.ActivatedTeleportIds))
        {
            RefreshVisual();   // 다른 포인트나 세이브 불러오기로 활성화돼도 모습이 바뀜
        }
    }

    private void RefreshVisual()
    {
        if (_activeVisual != null) _activeVisual.SetActive(IsActivated);
        if (_inactiveVisual != null) _inactiveVisual.SetActive(!IsActivated);

        _animator.SetBool(IsActiveHash, ShowActiveMotion);
    }
}