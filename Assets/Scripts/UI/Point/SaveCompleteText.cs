using System.Collections;
using UnityEngine;

public class SaveCompleteText : UIBase
{
    [SerializeField] private float _showTime = 2f;

    private Coroutine _hideRoutine;

    private void OnEnable()
    {
        Show();
    }

    private void Show()
    {
        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);   // 연달아 호출하면 시간을 처음부터 다시 셈
        }
        _hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(_showTime);   // 일시정지 중에도 시간이 흐
        UIManager.Instance.CloseUI(UIRootType.PopupUI, UIType.SaveCompleteText);
        _hideRoutine = null;
    }
}
