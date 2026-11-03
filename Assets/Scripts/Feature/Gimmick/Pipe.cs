using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 배관 기믹입니다. 입구에서 상호작용 키를 누르면 플레이어가 배관 안으로 들어가고,
/// 배관 겉을 따라 불빛(Blip)이 이동하는 모습을 보여준 뒤 연결된 출구 배관으로 나옵니다. (레인월드 방식)
/// 기본 설정은 쥐 형태(ShadowType.Mouse)일 때만 이용할 수 있습니다.
///
/// 구조
/// Gimmick_Pipe                 ← Pipe, InteractionPrompt, BoxCollider2D(Is Trigger, 입구 범위)
/// ├─ Visual                    ← 배관 그림
/// ├─ ExitPoint                 ← 이 배관으로 나올 때 플레이어가 놓일 위치 (빈 오브젝트)
/// ├─ PathPoints                ← 배관이 꺾이는 지점들 (빈 오브젝트들, 입구 → 출구 순서)
/// │   ├─ Point_0
/// │   └─ Point_1
/// ├─ Blip                      ← 이동하는 불빛 (SpriteRenderer, 선택: Light 2D, TrailRenderer). 평소에는 꺼 둠
/// └─ Prompt                    ← 말주머니 (InteractionPrompt의 GameObject_Prompt에 연결)
///
/// - 왕복하려면 두 배관의 Pipe_Exit에 서로를 넣어 주세요. (한쪽만 넣으면 일방통행)
/// - 경로(List_PathPoint)는 한쪽 배관에만 넣어도 됩니다. 반대쪽은 그 경로를 거꾸로 사용합니다.
/// - _requiredShadowType이 None이면 모든 형태가 이용할 수 있습니다.
/// - 이동하는 동안 플레이어는 보이지 않고 물리가 멈추며, 위치는 불빛을 따라갑니다. (카메라도 따라감)
/// - 오브젝트를 선택하면 씬 뷰에 이동 경로가 분홍 선으로 표시됩니다.
/// </summary>
public class Pipe : MonoBehaviour, IInteractable
{
    [Header("Reference")]
    [SerializeField] private Pipe Pipe_Exit;
    [SerializeField] private Transform Transform_ExitPoint;
    [SerializeField] private Transform Transform_Blip;
    [SerializeField] private List<Transform> List_PathPoint = new List<Transform>();

    [Header("Condition")]
    [SerializeField] private ShadowType _requiredShadowType = ShadowType.Mouse;
    [SerializeField] private float _reenterDelay = 0.3f;

    [Header("Travel")]
    [SerializeField] private float _travelSpeed = 8f;
    [SerializeField] private float _enterDelay = 0.15f;
    [SerializeField] private float _exitDelay = 0.15f;

    private readonly List<Vector3> _travelPath = new List<Vector3>();
    private readonly List<Renderer> _hiddenRenderers = new List<Renderer>();
    private float _lockUntilTime;

    // 플레이어가 배관 안에 있는 동안 다른 배관이 동시에 반응하지 않도록 막습니다.
    private static bool s_isAnyTraveling;

    public bool IsTraveling { get; private set; }

    // 배관에 들어갈 때 / 나올 때 호출됩니다. (효과음, 연출 연결용)
    public event Action OnStartTravel;
    public event Action OnEndTravel;

    // 플레이 모드를 다시 시작할 때 이전 상태가 남지 않도록 초기화합니다. (도메인 리로드를 끈 경우 대비)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        s_isAnyTraveling = false;
    }

    /// <summary>
    /// 이 배관으로 나올 때 플레이어가 놓일 위치입니다.
    /// </summary>
    public Vector3 ExitPosition
    {
        get { return Transform_ExitPoint != null ? Transform_ExitPoint.position : transform.position; }
    }

    /// <summary>
    /// 이 배관에서 출구 배관까지의 경유 지점 목록입니다. (입구 → 출구 순서)
    /// </summary>
    public List<Transform> PathPoints
    {
        get { return List_PathPoint; }
    }

    public Pipe ExitPipe
    {
        get { return Pipe_Exit; }
    }

    private void OnEnable()
    {
        _lockUntilTime = 0f;
        IsTraveling = false;
        SetBlipVisible(false);
    }

    public Vector2 Position
    {
        get { return transform.position; }
    }

    /// <summary>
    /// 지금 이 배관을 이용할 수 있는지 돌려줍니다. (형태 조건, 잠금, 이동 중 여부)
    /// </summary>
    public bool CanInteract(Player player)
    {
        if (Pipe_Exit == null || s_isAnyTraveling || Time.time < _lockUntilTime)
            return false;

        return IsShadowAllowed(player);
    }

    /// <summary>
    /// 플레이어가 상호작용 키를 눌렀을 때 PlayerInteractor가 호출합니다. 조건이 맞지 않으면 아무 일도 하지 않습니다.
    /// </summary>
    public void Interact(Player player)
    {
        if (player == null || CanInteract(player) == false || player.Rigid == null)
            return;

        TravelAsync(player.Rigid, this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// 지정한 시간 동안 이 배관을 이용할 수 없게 합니다.
    /// </summary>
    public void Lock(float duration)
    {
        _lockUntilTime = Time.time + duration;
    }

    private bool IsShadowAllowed(Player player)
    {
        if (_requiredShadowType == ShadowType.None)
            return true;

        if (player == null || player.PlayerViewModel == null)
            return false;

        return player.PlayerViewModel.CurrentShadowType == _requiredShadowType;
    }

    private async UniTaskVoid TravelAsync(Rigidbody2D player, CancellationToken cancellationToken)
    {
        s_isAnyTraveling = true;
        IsTraveling = true;

        if (OnStartTravel != null)
        {
            OnStartTravel.Invoke();
        }

        // 플레이어를 숨기고 물리를 멈춥니다.
        HidePlayer(player);
        BuildTravelPath();

        bool isCanceled = await WaitAsync(_enterDelay, cancellationToken);

        if (isCanceled == false)
        {
            isCanceled = await MoveAlongPathAsync(player, cancellationToken);
        }

        if (isCanceled == false)
        {
            await WaitAsync(_exitDelay, cancellationToken);
        }

        // 중간에 취소되더라도(배관 삭제 등) 플레이어는 반드시 다시 보이게 되돌려 놓습니다.
        SetBlipVisible(false);

        if (Pipe_Exit != null)
        {
            ShowPlayer(player, Pipe_Exit.ExitPosition);
            Pipe_Exit.Lock(_reenterDelay);
        }
        else if (player != null)
        {
            ShowPlayer(player, player.transform.position);
        }

        Lock(_reenterDelay);

        IsTraveling = false;
        s_isAnyTraveling = false;

        if (OnEndTravel != null)
        {
            OnEndTravel.Invoke();
        }
    }

    /// <summary>
    /// 불빛을 경로를 따라 일정 속도로 이동시키고, 플레이어 위치도 함께 옮깁니다. 취소되면 true를 돌려줍니다.
    /// </summary>
    private async UniTask<bool> MoveAlongPathAsync(Rigidbody2D player, CancellationToken cancellationToken)
    {
        SetBlipPosition(_travelPath[0]);
        SetBlipVisible(true);

        for (int i = 0; i < _travelPath.Count - 1; i++)
        {
            Vector3 from = _travelPath[i];
            Vector3 to = _travelPath[i + 1];
            float distance = Vector3.Distance(from, to);
            float duration = _travelSpeed > 0f ? distance / _travelSpeed : 0f;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                Vector3 position = Vector3.Lerp(from, to, elapsedTime / duration);
                SetBlipPosition(position);
                player.transform.position = position;

                bool isCanceled = await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken).SuppressCancellationThrow();
                if (isCanceled)
                    return true;
            }

            SetBlipPosition(to);
            player.transform.position = to;
        }

        return false;
    }

    /// <summary>
    /// 입구 → 경유 지점들 → 출구 순서의 이동 경로를 만듭니다.
    /// 이 배관에 경로가 없고 출구 배관이 이 배관으로 돌아오는 경로를 가지고 있으면 거꾸로 사용합니다.
    /// </summary>
    private void BuildTravelPath()
    {
        _travelPath.Clear();
        _travelPath.Add(transform.position);

        if (List_PathPoint.Count > 0)
        {
            for (int i = 0; i < List_PathPoint.Count; i++)
            {
                if (List_PathPoint[i] != null)
                {
                    _travelPath.Add(List_PathPoint[i].position);
                }
            }
        }
        else if (Pipe_Exit.ExitPipe == this)
        {
            List<Transform> reversePoints = Pipe_Exit.PathPoints;
            for (int i = reversePoints.Count - 1; i >= 0; i--)
            {
                if (reversePoints[i] != null)
                {
                    _travelPath.Add(reversePoints[i].position);
                }
            }
        }

        _travelPath.Add(Pipe_Exit.transform.position);
    }

    private void HidePlayer(Rigidbody2D player)
    {
        _hiddenRenderers.Clear();
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].enabled)
            {
                renderers[i].enabled = false;
                _hiddenRenderers.Add(renderers[i]);
            }
        }

        player.linearVelocity = Vector2.zero;
        player.simulated = false;
    }

    private void ShowPlayer(Rigidbody2D player, Vector3 position)
    {
        if (player == null)
            return;

        player.transform.position = position;
        player.position = position;
        player.linearVelocity = Vector2.zero;
        player.simulated = true;

        for (int i = 0; i < _hiddenRenderers.Count; i++)
        {
            if (_hiddenRenderers[i] != null)
            {
                _hiddenRenderers[i].enabled = true;
            }
        }

        _hiddenRenderers.Clear();
    }

    private void SetBlipPosition(Vector3 position)
    {
        if (Transform_Blip == null)
            return;

        Transform_Blip.position = new Vector3(position.x, position.y, Transform_Blip.position.z);
    }

    private void SetBlipVisible(bool isVisible)
    {
        if (Transform_Blip == null)
            return;

        Transform_Blip.gameObject.SetActive(isVisible);

        // 꼬리 효과가 있으면 이전 위치에서 이어지지 않도록 지웁니다.
        TrailRenderer trail;
        if (isVisible && Transform_Blip.TryGetComponent(out trail))
        {
            trail.Clear();
        }
    }

    /// <summary>
    /// 지정한 시간만큼 기다립니다. 중간에 취소되면 true를 돌려줍니다.
    /// </summary>
    private async UniTask<bool> WaitAsync(float seconds, CancellationToken cancellationToken)
    {
        if (seconds <= 0f)
            return cancellationToken.IsCancellationRequested;

        return await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: cancellationToken).SuppressCancellationThrow();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(ExitPosition, 0.2f);

        if (Pipe_Exit == null)
            return;

        // 경로를 선으로 표시합니다.
        Vector3 previous = transform.position;
        for (int i = 0; i < List_PathPoint.Count; i++)
        {
            if (List_PathPoint[i] == null)
            {
                continue;
            }

            Gizmos.DrawLine(previous, List_PathPoint[i].position);
            Gizmos.DrawWireSphere(List_PathPoint[i].position, 0.1f);
            previous = List_PathPoint[i].position;
        }

        Gizmos.DrawLine(previous, Pipe_Exit.transform.position);
    }
}
