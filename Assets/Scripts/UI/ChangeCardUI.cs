using DG.Tweening;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class ChangeCardUI : UIBase
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

    // 추가 - 김동혁
    [Header("비활성 카드 이미지 (시작할 때 각 자리 기준)")]
    [SerializeField] private Sprite Sprite_LeftInactive;
    [SerializeField] private Sprite Sprite_CenterInactive;
    [SerializeField] private Sprite Sprite_RightInactive;
    [Tooltip("비활성 이미지가 없을 때 대신 쓰는 색입니다.")]
    [SerializeField] private Color _inactiveColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    // -----

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
    // 추가 - 김동혁
    private Image[] _cardImages;
    private Sprite[] _activeSprites;
    private Sprite[] _inactiveSprites;
    // -----

    public void Awake()
    {
        if (Init() == false)
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
        UpdateUnlockView(); // 추가 - 김동혁
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
        UpdateUnlockView(); // 추가 - 김동혁
    }

    private void OnDisable()
    {
        UnBind();
        StopMoveCards();
    }

    private bool Init()
    {
        if (RectTransform_LeftCard == null || RectTransform_CenterCard == null || RectTransform_RightCard == null || Button_ChangeCard == null)
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

        // 추가 - 김동혁
        _cardImages = new Image[3];
        _activeSprites = new Sprite[3];
        _inactiveSprites = new[] { Sprite_LeftInactive, Sprite_CenterInactive, Sprite_RightInactive };
        for (int i = 0; i < _cards.Length; i++)
        {
            _cardImages[i] = _cards[i].GetComponent<Image>();

            if (_cardImages[i] == null)
            {
                Debug.LogWarning($"[ChangeCardUI] (Init) - 카드에 Image 없음, 활성 표시 생략: {_cards[i].name}");
                continue;
            }

            _activeSprites[i] = _cardImages[i].sprite;
        }
        // -----

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
        // 추가 - 김동혁
        _playerViewModel.ContainerPropertyChanged -= OnChangePlayerContainer;
        _playerViewModel.ContainerPropertyChanged += OnChangePlayerContainer;
        // -----
    }

    private void UnBind()
    {
        if (_playerViewModel != null)
        {
            _playerViewModel.PropertyChanged -= OnChangePlayerProperty;
            _playerViewModel.ContainerPropertyChanged -= OnChangePlayerContainer; // 추가 - 김동혁
        }

        if (Button_ChangeCard != null)
        {
            Button_ChangeCard.onClick.RemoveListener(OnClickChangeCard);
        }
    }

    private void OnClickChangeCard()
    {
        Debug.Log("버튼 눌림");
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

    // 추가 - 김동혁, 그림자 해금 목록이 바뀌면 카드 활성/비활성 표시 갱신
    private void OnChangePlayerContainer(string containerName, ContainerEventType eventType, ShadowType shadowType)
    {
        if (containerName == nameof(PlayerViewModel.OwnerShadowType))
        {
            UpdateUnlockView();
        }
    }

    // 추가 - 김동혁, 해금 여부에 따라 카드 이미지를 활성/비활성으로 표시
    private void UpdateUnlockView()
    {
        if (_playerViewModel == null)
        {
            return;
        }

        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cardImages[i] == null)
            {
                continue;
            }

            bool isUnlocked = _playerViewModel.OwnerShadowType.Contains(_cardShadowTypes[i]);

            if (isUnlocked)
            {
                _cardImages[i].sprite = _activeSprites[i];
                _cardImages[i].color = Color.white;
            }
            else if (_inactiveSprites[i] != null)
            {
                _cardImages[i].sprite = _inactiveSprites[i];
                _cardImages[i].color = Color.white;
            }
            else
            {
                // 비활성 이미지가 없으면 활성 이미지를 어둡게 표시합니다.
                _cardImages[i].sprite = _activeSprites[i];
                _cardImages[i].color = _inactiveColor;
            }
        }
    }
    // -----

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
