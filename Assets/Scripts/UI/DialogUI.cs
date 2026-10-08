using System;
using TMPro;
using UnityEngine;

public class DialogUI : UIBase
{
    [Header("Dialog UI")]
    [SerializeField] private GameObject GameObject_DialogPanel;
    [SerializeField] private TextMeshProUGUI Text_Dialog;
    [SerializeField] private UIButton Button_Next;

    [Header("Tutorial Test")]
    [SerializeField] private string _startDialogId = "Tutorial_Move_01";
    [SerializeField] private bool _playOnStart;

    private DialogData _currentDialogData;
    private bool _isInitialized;

    public bool IsPlaying => _currentDialogData != null;
    public string CurrentId => IsPlaying ? _currentDialogData.Id : string.Empty;

    public event Action OnEndDialog;

    private void Start()
    {
        if (Init() == false) return;

        if (_playOnStart && IsPlaying == false)
        {
            StartTutorialDialog();
        }
    }

    private void OnEnable()
    {
        if (_isInitialized)
        {
            Bind();
        }
    }

    private void OnDisable()
    {
        UnBind();
        StopDialog();
    }

    private bool Init()
    {
        if (_isInitialized)
        {
            return true;
        }

        if (GameObject_DialogPanel == null || Text_Dialog == null || Button_Next == null)
        {
            Debug.LogError("[DialogUI] 패널, TMP 텍스트, 다음 버튼을 연결하세요.");
            return false;
        }

        // 루트를 숨기면 DialogUI도 꺼지므로, 표시 패널은 자식으로 연결한다.
        if (GameObject_DialogPanel == gameObject || GameObject_DialogPanel.transform.IsChildOf(transform) == false)
        {
            Debug.LogError("[DialogUI] 패널은 DialogUI 오브젝트의 자식이어야 합니다.");
            return false;
        }

        _isInitialized = true;
        Bind();
        StopDialog();
        return true;
    }

    private void Bind()
    {
        Button_Next.UnBindOnClickButtonEvent(OnClickNextDialog);
        Button_Next.BindOnClickButtonEvent(OnClickNextDialog, true);
    }

    private void UnBind()
    {
        if (Button_Next != null)
        {
            Button_Next.UnBindOnClickButtonEvent(OnClickNextDialog);
        }
    }

    [ContextMenu("Start Tutorial Dialog")]
    public void StartTutorialDialog()
    {
        StartDialog(_startDialogId);
    }

    public bool StartDialog(string startId)
    {
        if (Application.isPlaying == false || isActiveAndEnabled == false) return false;
        if (Init() == false) return false;

        return SetDialog(startId);
    }

    private bool SetDialog(string dialogId)
    {
        if (string.IsNullOrWhiteSpace(dialogId) || dialogId == "-1")
        {
            Debug.LogError($"[DialogUI] 표시할 대사 ID가 올바르지 않습니다: {dialogId}");
            return false;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogError("[DialogUI] GameDataManager가 존재하지 않습니다.");
            return false;
        }

        DialogData dialogData = GameDataManager.Instance.GetData<DialogData>(dialogId);
        if (dialogData == null)
        {
            Debug.LogError($"[DialogUI] 대사를 찾을 수 없습니다. Id: {dialogId}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(dialogData.Dialog))
        {
            Debug.LogError($"[DialogUI] 대사 내용이 비어 있습니다. Id: {dialogId}");
            return false;
        }

        _currentDialogData = dialogData;
        UpdateDialog();
        return true;
    }

    private void UpdateDialog()
    {
        Text_Dialog.text = _currentDialogData.Dialog;
        GameObject_DialogPanel.SetActive(true);
    }

    private void OnClickNextDialog()
    {
        ShowNextDialog();
    }

    public void ShowNextDialog()
    {
        if (IsPlaying == false) return;

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

        if (_isInitialized == false) return;

        Text_Dialog.text = string.Empty;
        GameObject_DialogPanel.SetActive(false);
    }
}

