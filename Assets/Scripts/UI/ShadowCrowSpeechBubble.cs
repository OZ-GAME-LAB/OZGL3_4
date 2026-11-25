using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class ShadowCrowSpeechBubble : MonoBehaviour
{
    [SerializeField] private GameObject GameObject_Bubble;
    [SerializeField] private TextMeshProUGUI Text_Dialog;

    [Header("다음 대사 안내")]
    [SerializeField] private TextMeshProUGUI Text_NextHint;
    [SerializeField] private string _nextHintText = "F 다음";
    [SerializeField] private string _endHintText = "F 닫기";
    [SerializeField, Min(0f)] private float _hintMoveDistance = 6f;
    [SerializeField, Min(0.05f)] private float _hintMoveTime = 0.5f;

    private DialogData _currentDialogData;
    private Vector3 _hintBasePosition;
    private Tween _hintTween;

    public bool IsPlaying => _currentDialogData != null;
    public string CurrentId => IsPlaying ? _currentDialogData.Id : string.Empty;

    // 마지막 대사(NextId "-1")까지 넘겨서 정상 종료했을 때만 발생한다. 범위를 벗어나 끊긴 경우(StopDialog)는 발생하지 않는다.
    public event Action OnEndDialog;

    private void Awake()
    {
        if (GameObject_Bubble == null || Text_Dialog == null)
        {
            Debug.LogError($"[ShadowCrowSpeechBubble] {name}: GameObject_Bubble과 Text_Dialog를 연결하세요.");
            return;
        }

        if (Text_NextHint != null)
        {
            _hintBasePosition = Text_NextHint.transform.localPosition;
        }

        StopDialog();
    }

    private void OnDisable()
    {
        StopDialog();
    }

    public bool StartDialog(string startId)
    {
        return SetDialog(startId);
    }

    public void ShowNextDialog()
    {
        if (IsPlaying == false)
        {
            return;
        }

        string nextId = _currentDialogData.NextId;
        if (nextId == "-1")
        {
            StopDialog();
            OnEndDialog?.Invoke();
            return;
        }

        SetDialog(nextId);
    }

    public void StopDialog()
    {
        _currentDialogData = null;

        if (GameObject_Bubble == null || Text_Dialog == null)
        {
            return;
        }

        Text_Dialog.text = string.Empty;
        GameObject_Bubble.SetActive(false);
        HideNextHint();
    }

    private bool SetDialog(string dialogId)
    {
        if (string.IsNullOrWhiteSpace(dialogId) || dialogId == "-1")
        {
            Debug.LogError($"[ShadowCrowSpeechBubble] 표시할 대사 ID가 올바르지 않습니다: {dialogId}");
            return false;
        }

        if (GameObject_Bubble == null || Text_Dialog == null)
        {
            return false;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogError("[ShadowCrowSpeechBubble] GameDataManager가 존재하지 않습니다.");
            return false;
        }

        DialogData dialogData = GameDataManager.Instance.GetData<DialogData>(dialogId);
        if (dialogData == null)
        {
            Debug.LogError($"[ShadowCrowSpeechBubble] 대사를 찾을 수 없습니다. Id: {dialogId}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(dialogData.Dialog))
        {
            Debug.LogError($"[ShadowCrowSpeechBubble] 대사 내용이 비어 있습니다. Id: {dialogId}");
            return false;
        }

        _currentDialogData = dialogData;
        UpdateDialog();
        return true;
    }

    private void UpdateDialog()
    {
        Text_Dialog.text = _currentDialogData.Dialog;
        GameObject_Bubble.SetActive(true);
        ShowNextHint();
    }

    // 다음 대사가 있으면 "F 다음", 마지막 대사면 "F 닫기"를 보여주고 위아래로 살짝 움직인다.
    private void ShowNextHint()
    {
        if (Text_NextHint == null)
        {
            return;
        }

        bool isLastDialog = _currentDialogData.NextId == "-1";
        Text_NextHint.text = isLastDialog ? _endHintText : _nextHintText;
        Text_NextHint.gameObject.SetActive(true);

        if (_hintTween != null && _hintTween.IsActive())
        {
            return;
        }

        Text_NextHint.transform.localPosition = _hintBasePosition;
        _hintTween = Text_NextHint.transform
            .DOLocalMoveY(_hintBasePosition.y + _hintMoveDistance, _hintMoveTime)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void HideNextHint()
    {
        _hintTween?.Kill();
        _hintTween = null;

        if (Text_NextHint == null)
        {
            return;
        }

        Text_NextHint.transform.localPosition = _hintBasePosition;
        Text_NextHint.gameObject.SetActive(false);
    }
}
