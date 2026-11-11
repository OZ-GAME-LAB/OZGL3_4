using UnityEngine;

public class RevealTargetSender : MonoBehaviour
{
    private int _playerPositionId;
    private bool _isWarned;

    private void Awake()
    {
        _playerPositionId = Shader.PropertyToID("_PlayerPosition");
    }

    private void LateUpdate()
    {
        if (MapManager.Instance == null || MapManager.Instance.Transform_Target == null)
        {
            if (_isWarned == false)
            {
                Debug.LogWarning("[RevealTargetSender] (LateUpdate) : MapManager 또는 Transform_Target 없음");

                _isWarned = true;

                
            }

            return;
        }

        _isWarned = false;

        Shader.SetGlobalVector(_playerPositionId, MapManager.Instance.Transform_Target.position);
    }

    private void OnDisable()
    {
        Shader.SetGlobalVector(_playerPositionId, new Vector4(99999f, 99999f, 0f, 0f));
    }
}
