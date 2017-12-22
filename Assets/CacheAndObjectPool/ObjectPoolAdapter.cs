using UnityEngine;
using System;

public class ObjectPoolAdapter<T> : MonoBehaviour where T : class, new() {
    ObjectPool<T> pool;

    public virtual T GetObject() {
        return pool.GetObject();
    }

    public virtual bool StoreObject(T obj) {
        return pool.StoreObject(obj);
    }

    public virtual void SetPool(int size, Func<T> crtFunc = null, Action<T> rstFunc = null, Action<T> storeFunc = null) {
        pool = new ObjectPool<T>(size, crtFunc, rstFunc, storeFunc);
    }
}
