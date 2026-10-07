using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    [SerializeField] private Button Button_Base;
    [SerializeField] private Image Image_Base;
    [SerializeField] private TMP_Text Text_Base;

    // true면 외부에서 직접 이벤트 해제
    private bool _isSlotManualUnbindEvent = false;

    private void Awake()
    {
        InitUIButton();
    }

    private void OnDisable()
    {
        if (_isSlotManualUnbindEvent == false)
        {
            Button_Base.onClick.RemoveAllListeners();
        }
    }

    private void InitUIButton()
    {
        if (Button_Base != null)
        {
            return;
        }

        var button = this.gameObject.GetComponentInChildren<Button>();
        if (button != null)
        {
            this.Button_Base = button;
        }
    }

    public void BindOnClickButtonEvent(Action onClickCallback, bool isManualUnbindEvent = false)
    {
        if (Button_Base == null) return;

        Button_Base.onClick.AddListener(new UnityEngine.Events.UnityAction(onClickCallback));
        _isSlotManualUnbindEvent = isManualUnbindEvent;
    }

    public void UnBindOnClickButtonEvent(Action onClickCallback)
    {
        if (Button_Base == null) return;

        Button_Base.onClick.RemoveListener(new UnityEngine.Events.UnityAction(onClickCallback));
    }

    public void SetInteractable(bool isInteractable)
    {
        if(isInteractable == true)
        {
            Button_Base.interactable = true;
        }
        else
        {
            Button_Base.interactable = false;
        }
    }

}
