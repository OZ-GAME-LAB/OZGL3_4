using TMPro;
using UnityEngine;

public class ShadowCrowSpeechBubble : MonoBehaviour
{
    [SerializeField] private GameObject GameObject_Bubble;
    [SerializeField] private TextMeshProUGUI Text_Dialog;

    private DialogData _currentDialogData;

    public bool IsPlaying => _currentDialogData != null;
    public string CurrentId => IsPlaying ? _currentDialogData.Id : string.Empty;

    private void Awake()
    {
        if (GameObject_Bubble == null || Text_Dialog == null)
        {
            Debug.LogError($"[ShadowCrowSpeechBubble] {name}: GameObject_Bubble과 Text_Dialog를 연결하세요.");
            return;
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
    }
}
