using System;
using UnityEngine;
using System.Collections.Generic;

public class MultipleObjectPoolAdapter<T> where T : class, new() {
    List<MultiObjectPool<T>> pools;

    Func<int, T> crtFunc;

    public virtual T GetObject(int choice) {
        return pools[choice].GetObject();
    }

    public virtual bool StoreObject(T obj, int choice) {
        return pools[choice].StoreObject(obj);
    }

    public virtual void SetPool(int choicesCount, int size, 
        Func<int, T> crtFunc = null, Action<T> rstFunc = null, 
        Action<T> storeFunc = null) {

        this.crtFunc = crtFunc;
        pools = new List<MultiObjectPool<T>>();
        for (int i = 0; i < choicesCount; i++) 
            pools.Add(new MultiObjectPool<T>(i, size, crtFunc, rstFunc, storeFunc));
    }
}
