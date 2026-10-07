using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : SingletonBase<UIManager>
{
    [SerializeField] private Canvas _canvasBgRoot;
    [SerializeField] private Canvas _canvasMainRoot;
    [SerializeField] private Canvas _canvasContentRoot;
    [SerializeField] private Canvas _canvasPopupRoot;
    [SerializeField] private Canvas _canvasVeryFrontRoot;

    private Dictionary<UIType, UIBase> _createdUIList = new Dictionary<UIType, UIBase>();
    private HashSet<UIType> _opendUIList = new HashSet<UIType>();

    // [나라] TODO : 게임 시작하자마자 LoadingUI 열리도록
    private void Start()
    {
    }

    public UIBase OpenUI(UIRootType uiRootType, UIType uiType, bool isInitialHide = false)
    {
        // 딱히 요청이 있진 않고 오픈만 하면 되는 UI에서 사용

        var openedUI = GetCreatedUI(uiRootType, uiType);

        bool isSetActiveOnOpen = (isInitialHide == false); // 열었을 때 기본적으로 숨겨서 열 것인지 체크
        if (_opendUIList.Contains(uiType) == false)
        {
            openedUI.gameObject.SetActive(isSetActiveOnOpen);
            _opendUIList.Add(uiType);
        }

        return openedUI;
    }

    public void CloseUI(UIRootType uiRootType, UIType uiType)
    {
        if (_opendUIList.Contains(uiType))
        {
            var openedUi = _createdUIList[uiType];
            openedUi.gameObject.SetActive(false);
            _opendUIList.Remove(uiType);
        }
    }

    private Transform GetRootTransform(UIRootType uiRootType)
    {
        Transform root = null;
        switch (uiRootType)
        {
            case UIRootType.BackgroundUI:
                root = _canvasBgRoot.transform;
                break;
            case UIRootType.MainUI:
                root = _canvasMainRoot.transform;
                break;
            case UIRootType.ContentUI:
                root = _canvasContentRoot.transform;
                break;
            case UIRootType.PopupUI:
                root = _canvasPopupRoot.transform;
                break;
            case UIRootType.VeryFrontUI:
                root = _canvasVeryFrontRoot.transform;
                break;
        }
        return root;
    }

    private void CreateUI(UIRootType uiRootType, UIType uiType)
    {
        if (_createdUIList.ContainsKey(uiType) == false)
        {
            string path = this.GetUIPath(uiRootType, uiType);
            GameObject loadedObj = Resources.Load<GameObject>(path);
            Transform root = GetRootTransform(uiRootType);
            GameObject gObj = Instantiate(loadedObj, root);
            if (gObj != null)
            {
                var uiBase = gObj.GetComponent<UIBase>();
                _createdUIList.Add(uiType, uiBase);
            }
        }
    }

    private UIBase GetCreatedUI(UIRootType uiRootType, UIType uiType)
    {
        if (_createdUIList.ContainsKey(uiType) == false)
        {
            CreateUI(uiRootType, uiType);
        }
        return _createdUIList[uiType];
    }

    public UIBase GetOpenUI(UIRootType uiRootType, UIType uiType)
    {
        return GetCreatedUI(uiRootType, uiType);
    }

    public bool IsOpenUI(UIType uiType)
    {
        return _opendUIList.Contains(uiType);
    }
}
