using UnityEngine;

public class SpawnState : PlayerStateBase
{
    private const string _spawnAnimName = "Spawn";
    private const string _idleAnimName = "Idle";

    private float _zoomSize = 3.5f;     
    private float _zoomInTime = 0.6f;
    private float _zoomOutTime = 0.6f;

    private CameraController _cameraController;

    public SpawnState(Player player) : base(player) { }

    public override void Enter()
    {
        _player.Animator.Play(_spawnAnimName, 0, 0f);
        StartDirecting();
    }

    public override void Update()
    {
        AnimatorStateInfo stateInfo = _player.Animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName(_spawnAnimName) == false)
            return;

        if (stateInfo.normalizedTime < 1f)
            return;

        _player.Animator.Play(_idleAnimName, 0, 0f);
        ChangeToLocomotion();
    }

    public override void FixedUpdate()
    {
        _player.SetVelocity(0, true);
    }

    public override void Exit()
    {
        EndDirecting();
    }

    private void StartDirecting()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[SpawnState] MainCamera가 없어 생성 연출 줌을 생략합니다.");
            return;
        }

        _cameraController = mainCamera.GetComponent<CameraController>();
        if (_cameraController == null)
        {
            Debug.LogWarning("[SpawnState] Main Camera에 CameraController가 없어 줌과 입력 막기를 생략합니다.");
            return;
        }

        _cameraController.ZoomIn(_zoomSize, _zoomInTime, true);
    }

    private void EndDirecting()
    {
        if (_cameraController == null)
            return;

        _cameraController.ZoomOut(_zoomOutTime);
        _cameraController = null;
    }
}
