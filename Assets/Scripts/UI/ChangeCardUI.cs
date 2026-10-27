using DG.Tweening;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class ChangeCardUI : MonoBehaviour
{
    [Header("시작할 때 각 자리에 있는 카드")]
    [SerializeField] private RectTransform RectTransform_LeftCard;
    [SerializeField] private RectTransform RectTransform_RightCard;
    [SerializeField] private RectTransform RectTransform_CenterCard;
    [SerializeField] private Button Button_ChangeCard;

    [Header("시작할 때 각 카드의 그림자 종류")]
    [SerializeField] private ShadowType _leftShadowType = ShadowType.Mouse;
    [SerializeField] private ShadowType _centerShadowType = ShadowType.Default;
    [SerializeField] private ShadowType _rightShadowType = ShadowType.Rabbit;

    [Header("이동 연출")]
    [SerializeField, Min(0.01f)] private float _moveDuration = 0.35f;
    [SerializeField] private Ease _moveEase = Ease.InOutSine;

    private Vector3 _leftPosition;
    private Vector3 _centerPosition;
    private Vector3 _rightPosition;
    private Sequence _moveSequence;
    private bool _isMoving;
    private RectTransform[] _cards;
    private ShadowType[] _cardShadowTypes;
    private NetworkPlayerService _playerService;
    private PlayerViewModel _playerViewModel;

    public void Awake()
    {
        if(Init() == false)
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // 최초 OnEnable은 다른 객체의 Awake보다 먼저 실행될 수 있어 Start에서 연결한다.
        if (_playerViewModel == null) return;

        Bind();
        UpdateView(false);
    }

    private void Start()
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.PlayerService == null)
        {
            Debug.LogError("[ChangeCardUI] NetworkManager와 PlayerService가 필요합니다.", this);
            enabled = false;
            return;
        }

        _playerService = NetworkManager.Instance.PlayerService;
        _playerViewModel = _playerService.GetPlayerViewModel();
        Bind();
        UpdateView(false);
    }

    private void OnDisable()
    {
        UnBind();
        StopMoveCards();
    }

    private bool Init()
    {
        if(RectTransform_LeftCard == null || RectTransform_CenterCard == null || RectTransform_RightCard == null || Button_ChangeCard == null)
        {
            Debug.LogError("[ChangeCardUI]: 왼쪽, 중앙, 오른쪽 카드와 버튼을 연결하세요.", this);
            return false;
        }

        if (RectTransform_LeftCard == RectTransform_CenterCard || RectTransform_LeftCard == RectTransform_RightCard
            || RectTransform_CenterCard == RectTransform_RightCard
            || !IsPlayableShadow(_leftShadowType) || !IsPlayableShadow(_centerShadowType) || !IsPlayableShadow(_rightShadowType)
            || _leftShadowType == _centerShadowType || _leftShadowType == _rightShadowType || _centerShadowType == _rightShadowType)
        {
            Debug.LogError("[ChangeCardUI] 서로 다른 카드 3개에 기본, 쥐, 토끼를 하나씩 지정하세요.", this);
            return false;
        }

        // 카드와 종류의 연결은 고정하고, 화면상의 자리만 변경한다.
        _cards = new[] { RectTransform_LeftCard, RectTransform_CenterCard, RectTransform_RightCard };
        _cardShadowTypes = new[] { _leftShadowType, _centerShadowType, _rightShadowType };

        _leftPosition = RectTransform_LeftCard.localPosition;
        _centerPosition = RectTransform_CenterCard.localPosition;
        _rightPosition = RectTransform_RightCard.localPosition;

        UpdateCardOrder();
        return true;

    }

    private void Bind()
    {
        Button_ChangeCard.onClick.RemoveListener(OnClickChangeCard);
        Button_ChangeCard.onClick.AddListener(OnClickChangeCard);
        _playerViewModel.PropertyChanged -= OnChangePlayerProperty;
        _playerViewModel.PropertyChanged += OnChangePlayerProperty;
    }

    private void UnBind()
    {
        if (_playerViewModel != null)
        {
            _playerViewModel.PropertyChanged -= OnChangePlayerProperty;
        }

        if(Button_ChangeCard != null)
        {
            Button_ChangeCard.onClick.RemoveListener(OnClickChangeCard);
        }
    }

    private void OnClickChangeCard()
    {
        if (_isMoving || _playerService == null) return;

        // 왼쪽 카드가 가운데로 오는 기존 순서를 유지하며 미획득 형태는 건너뛴다.
        if (_playerService.RequestChangeShadow(GetShadowType(RectTransform_LeftCard))) return;

        _playerService.RequestChangeShadow(GetShadowType(RectTransform_RightCard));
    }

    private void OnChangePlayerProperty(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(PlayerViewModel.CurrentShadowType))
        {
            UpdateView(true);
        }
    }

    private void UpdateView(bool animate)
    {
        ShadowType currentType = _playerViewModel.CurrentShadowType;
        if (!IsPlayableShadow(currentType)) return;

        StopMoveCards();
        for (int i = 0; i < _cards.Length; i++)
        {
            if (GetShadowType(RectTransform_CenterCard) == currentType) break;

            RotateCards();
        }

        UpdateCardOrder();
        if (animate == false)
        {
            RectTransform_LeftCard.localPosition = _leftPosition;
            RectTransform_CenterCard.localPosition = _centerPosition;
            RectTransform_RightCard.localPosition = _rightPosition;
            return;
        }

        MoveCards();
    }

    private void RotateCards()
    {
        RectTransform previousLeftCard = RectTransform_LeftCard;
        RectTransform_LeftCard = RectTransform_RightCard;
        RectTransform_RightCard = RectTransform_CenterCard;
        RectTransform_CenterCard = previousLeftCard;
    }

    private void MoveCards()
    {
        _isMoving = true;
        float moveDuration = Mathf.Max(0.01f, _moveDuration);
        _moveSequence = DOTween.Sequence();
        _moveSequence.Join(RectTransform_LeftCard.DOLocalMove(_leftPosition, moveDuration).SetEase(_moveEase));
        _moveSequence.Join(RectTransform_CenterCard.DOLocalMove(_centerPosition, moveDuration).SetEase(_moveEase));
        _moveSequence.Join(RectTransform_RightCard.DOLocalMove(_rightPosition, moveDuration).SetEase(_moveEase));
        _moveSequence.SetUpdate(true);
        _moveSequence.OnComplete(OnCompleteMoveCards);
    }

    private void UpdateCardOrder()
    {
        RectTransform_LeftCard.SetAsLastSibling();
        RectTransform_RightCard.SetAsLastSibling();
        RectTransform_CenterCard.SetAsLastSibling();

        if (Button_ChangeCard.transform.parent == RectTransform_CenterCard.parent)
        {
            Button_ChangeCard.transform.SetAsLastSibling();
        }
    }
    private void OnCompleteMoveCards()
    {
        _moveSequence = null;
        _isMoving = false;
    }

    private void StopMoveCards()
    {
        if (_moveSequence != null)
        {
            _moveSequence.Kill();
            _moveSequence = null;
        }
        _isMoving = false;
    }

    private ShadowType GetShadowType(RectTransform card)
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] == card) return _cardShadowTypes[i];
        }
        return ShadowType.None;
    }

    private static bool IsPlayableShadow(ShadowType type)
    {
        return type == ShadowType.Default || type == ShadowType.Mouse || type == ShadowType.Rabbit;
    }
}
