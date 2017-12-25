using System;
using System.Collections.Generic;

public class CacheQueue<T> where T : class {
    const int defaultCacheNum = 7;
    Queue<T> items;

    #region for optimistic
    int _maxCacheNum;
    public int size {
        private set {
            if (value == -1)
                _maxCacheNum = defaultCacheNum;
            else
                _maxCacheNum = value;
        }
        get {
            return _maxCacheNum;
        }
    }

    public int count {
        get {
            return items.Count;
        }
    }

    public bool isFull {
        get {
            return count == size;
        }
    }
    #endregion

    Action<T> storeFunction;
    Action<T> resetFunction;

    public CacheQueue(int cacheNum = -1, Action<T> storeFunction = null, Action<T> resetFunction = null) {
        size = cacheNum;
        items = new Queue<T>(cacheNum);
        this.storeFunction = storeFunction;
        this.resetFunction = resetFunction;
    }

    public void SetCacheNum(int cacheNum = -1) {
        this.size = cacheNum;
    }

    public bool TryStoreItem(T item) {
        if (!isFull) {
            items.Enqueue(item);
            if (storeFunction != null)
                storeFunction(item);
            return true;
        }
        return false;
    }

    public bool TryGetItem(out T item) {
        if (items.Count == 0) {
            item = null;
            return false;
        }
        else {
            item = items.Dequeue();
            if (resetFunction != null)
                resetFunction(item);
            return true;
        }
    }
}