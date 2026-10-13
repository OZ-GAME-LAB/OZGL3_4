using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameObjectManager : SingletonBase<GameObjectManager>
{
    private static int _objectInstanceKeyGenerator = 0;

    public int PlayerInstanceId { get; private set; } = -1;
    public Player CurrentPlayer { get; private set; }

    private Transform _rootTransform;
    private PoolManager _poolManager;

    private void OnEnable()
    {
        _poolManager = new PoolManager();
        _rootTransform = this.transform;
    }

    public void CreateObject(string dataId, string path, Vector3 spawnSpot)
    {
        CreateObjectAsync(dataId, path, spawnSpot).Forget();
    }

    public async UniTask LoadObjectAsync(string path)
    {
        await ResourceManager.Instance.LoadAsset<GameObject>(path);
    }

    public async UniTask<GameObject> CreateObjectAsync(string dataId, string path, Vector3 spawnSpot)
    {
        Debug.Log($"CreateObjectAsync 호출됨, path: {path}");
        GameObject prefab = await ResourceManager.Instance.LoadAsset<GameObject>(path);
        Debug.Log($"프리팹 로드 결과: {prefab}");

        if (prefab == null)
            return null;

        GameObject gameObject = _poolManager.Pop(prefab);
        if (gameObject == null)
            return null;

        gameObject.transform.SetParent(_rootTransform);
        gameObject.transform.position = spawnSpot;

        int instanceId = _objectInstanceKeyGenerator + 1;
        Player player = gameObject.GetComponent<Player>();
        if (player != null)
        {
            PlayerInstanceId = instanceId;
            CurrentPlayer = player;
        }

        return gameObject;
    }

    public void RequestDestroyObject(GameObject gameObject)
    {
        if (!_poolManager.Push(gameObject))
            return;

        if (CurrentPlayer != null && CurrentPlayer.gameObject == gameObject)
        {
            PlayerInstanceId = -1;
            CurrentPlayer = null;
        }
    }

    public void RemoveAllObject()
    {
        PlayerInstanceId = -1;
        CurrentPlayer = null;

        _poolManager.Clear();

        if (_rootTransform == null)
            return;

        for(int i = _rootTransform.childCount - 1; i >= 0; i--)
        {
            GameObject.Destroy(_rootTransform.GetChild(i).gameObject);
        }
    }
}
