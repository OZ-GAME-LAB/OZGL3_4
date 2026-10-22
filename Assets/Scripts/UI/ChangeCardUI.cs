using DG.Tweening;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.UI;

public class ChangeCardUI : MonoBehaviour
{
    [Header("시작할 때 각 자리에 있는 카드")]
    [SerializeField] private RectTransform RectTransform_LeftCard;
    [SerializeField] private RectTransform RectTransform_RightCard;
    [SerializeField] private RectTransform RectTransform_CenterCard;
    [SerializeField] private Button Button_ChangeCard;

    [Header("이동 연출")]
    [SerializeField, Min(0.01f)] private float _moveDuration = 0.35f;
    [SerializeField] private Ease _moveEase = Ease.InOutSine;

    private Vector3 _leftPosition;
    private Vector3 _centerPosition;
    private Vector3 _rightPosition;
    private Sequence _moveSequence;
    private bool _isMoving;

    public void Awake()
    {
        if(Init() == false)
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {
        Bind();
    }

    private void OnDisable()
    {
        UnBind();
        if(_moveSequence != null )
        {
            _moveSequence.Kill(true);
            _moveSequence = null;

        }

        _isMoving = false;
    }

    private bool Init()
    {
        if(RectTransform_LeftCard == null || RectTransform_CenterCard == null || RectTransform_RightCard == null || Button_ChangeCard == null)
        {
            Debug.LogError("[ChangeCardUI]: 왼쪽, 중앙, 오른쪽 카드와 버튼을 연결하세요.", this);
            return false;
        }

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
    }

    private void UnBind()
    {
        if(Button_ChangeCard != null)
        {
            Button_ChangeCard.onClick.RemoveListener(OnClickChangeCard);
        }
    }

    private void OnClickChangeCard()
    {
        if (_isMoving) return;

        _isMoving = true;

        RectTransform previousLeftCard = RectTransform_LeftCard;
        RectTransform_LeftCard = RectTransform_RightCard;
        RectTransform_RightCard = RectTransform_CenterCard;
        RectTransform_CenterCard = previousLeftCard;

        UpdateCardOrder();

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
}
