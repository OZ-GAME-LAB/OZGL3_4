using UnityEngine;
using UnityEngine.InputSystem;

public class MapUIInput : MonoBehaviour
{
    [SerializeField] private InputActionAsset InputActionAsset_Actions;

    private InputAction _mapAction;
    private MapViewModel _mapViewModel;

    public void Init(MapViewModel mapViewModel)
    {
        _mapViewModel = mapViewModel;
    }

    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        Bind();
    }

    private void OnDisable()
    {
        UnBind();
    }

    private void OnDestroy()
    {
        if (_mapAction != null)
        {
            _mapAction.Dispose();
        }
    }

    private void Init()
    {
        if (InputActionAsset_Actions == null)
        {
            Debug.LogError("[MapUIInput] InputSystem_Actions 에셋을 연결하세요.");
            return;
        }

        InputAction mapAction = InputActionAsset_Actions.FindAction("UI/Map");
        if (mapAction == null)
        {
            Debug.LogError("[MapUIInput] UI/Map 액션이 없습니다.");
            return;
        }

        _mapAction = mapAction.Clone();
    }

    private void Bind()
    {
        if (_mapAction == null)
        {
            return;
        }

        _mapAction.performed -= OnToggleMap;
        _mapAction.performed += OnToggleMap;
        _mapAction.Enable();
    }

    private void UnBind()
    {
        if (_mapAction == null)
        {
            return;
        }

        _mapAction.performed -= OnToggleMap;
        _mapAction.Disable();
    }

    private void OnToggleMap(InputAction.CallbackContext context)
    {
        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            Debug.LogError("[MapUIInput] UIManager가 없습니다.");
            return;
        }

        if (uiManager.IsOpenUI(UIType.Map))
        {
            uiManager.CloseUI(UIRootType.PopupUI, UIType.Map);
            return;
        }

        if (_mapViewModel == null)
        {
            Debug.LogError("[MapUIInput] MapSceneSetup에서 공용 ViewModel을 연결하세요.");
            return;
        }
        UIBase openedUI = uiManager.OpenUI(UIRootType.PopupUI, UIType.Map);
        MapView mapView = openedUI.GetComponent<MapView>();
        if (mapView == null)
        {
            Debug.LogError("[MapUIInput] 지도 프리팹에 MapView가 없습니다.");
            uiManager.CloseUI(UIRootType.PopupUI, UIType.Map);
            return;
        }
        mapView.Init(_mapViewModel);
    }
}
