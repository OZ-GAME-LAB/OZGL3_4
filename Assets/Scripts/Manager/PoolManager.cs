using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class Pool
{
    private GameObject _prefab;
    private IObjectPool<GameObject> _pool;
        
    public Pool(GameObject prefab)
    {
        _prefab = prefab;
        _pool = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease, OnDestroy, maxSize: 100);
    }

    public void Push(GameObject gameObject)
    {
        _pool.Release(gameObject);
    }

    public GameObject Pop()
    {
        return _pool.Get();
    }

    public GameObject OnCreate()
    {
        GameObject gameObject = GameObject.Instantiate(_prefab);
        gameObject.name = _prefab.name;
        return gameObject;
    }

    public void OnGet(GameObject gameObject)
    {
        gameObject.SetActive(true);
    }

    public void OnRelease(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    public void OnDestroy(GameObject gameObject)
    {
        GameObject.Destroy(gameObject);
    }

    public void Clear()
    {
        _pool.Clear();
    }
}

public class PoolManager
{
    private Dictionary<string, Pool> _pools = new Dictionary<string, Pool>();

    /// <summary>
    /// 오브젝트 생성/꺼내기
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    public GameObject Pop(GameObject gameObject)
    {
        if(_pools.ContainsKey(gameObject.name) == false)
        {
            CreatePool(gameObject);
        }

        return _pools[gameObject.name].Pop();
    }

    /// <summary>
    /// 오브젝트 반납
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    public bool Push(GameObject gameObject)
    {
        if (_pools.ContainsKey(gameObject.name) == false)
        {
            return false;
        }

        _pools[gameObject.name].Push(gameObject);
        return true;
    }

    private void CreatePool(GameObject gameObject)
    {
        Pool pool = new Pool(gameObject);
        _pools.Add(gameObject.name, pool);
    }

    public void Clear()
    {
        foreach(var pool in _pools.Values)
        {
            pool.Clear();
        }

        _pools.Clear();
    }
}
