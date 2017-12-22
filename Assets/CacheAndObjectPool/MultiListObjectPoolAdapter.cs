using System;
using System.Collections.Generic;

public class MultiListObjectPoolAdapter<T> : MultipleObjectPoolAdapter<T>
    where T : class, new(){

    List<CacheQueue<T>> cacheQueues;

    public override T GetObject(int choice) {
        T result;
        if(!cacheQueues[choice].TryGetItem(out result))
            result = base.GetObject(choice);
        return result;
    }

    public override bool StoreObject(T obj, int choice) {
        if (!cacheQueues[choice].TryStoreItem(obj))
            return true;
        else
            return base.StoreObject(obj, choice);
    }

    public override void SetPool(int choicesCount, int size, Func<int, T> crtFunc = null, Action<T> rstFunc = null, Action<T> storeFunc = null) {
        base.SetPool(choicesCount, size, crtFunc, rstFunc, storeFunc);
        cacheQueues = new List<CacheQueue<T>>();
        for (int i = 0; i < choicesCount; i++)
            cacheQueues.Add(new CacheQueue<T>(size, storeFunc, rstFunc));
    }
}
