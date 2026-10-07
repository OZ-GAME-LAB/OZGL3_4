using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();
        
    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(Instance);
            return;
        }

        Instance = this;
    }

    public async UniTask<T> LoadAsset<T>(string address) where T : UnityEngine.Object
    {
        if(_handles.TryGetValue(address, out AsyncOperationHandle handle))
        {
            return handle.Result as T;
        }

        AsyncOperationHandle<T> loadHandle = Addressables.LoadAssetAsync<T>(address);

        try
        {
            T result = await loadHandle.ToUniTask();

            _handles[address] = loadHandle;
            return result;
        }
        catch(System.Exception e)
        {
            Debug.LogError($" 에셋 로드 실패ㅣ: {address} / Error: {e.Message}");

            if (loadHandle.IsValid())
            {
                Addressables.Release(loadHandle);
            }
            return null;
        }
    }

    public void Release(string address)
    {
        if (_handles.TryGetValue(address, out AsyncOperationHandle handle) == false)
            return;

        Addressables.Release(handle);
        _handles.Remove(address);
        Debug.Log($"에셋 메모리 해체 완료: {address}");
    }
}