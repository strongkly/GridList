using System;
using System.Collections.Generic;

public class ObjectPool<T> where T : class, new() {
    const int defaultPoolSize = 7;
    Stack<T> objects;
    int _poolSize;

    #region for convenience
    public int size {
        protected set {
            if (value <= 0)
                _poolSize = defaultPoolSize;
            else
                _poolSize = value;
        }
        get {
            return _poolSize;
        }
    }
    public int count {
        get {
            return objects.Count;
        }
    }
    public bool isFull {
        get {
            return count == size;
        }
    }
    public bool isEmpty {
        get {
            return count == 0;
        }
    }
    #endregion

    protected Func<T> createFunction;
    protected Action<T> resetFunction;
    protected Action<T> storeFunction;

    public ObjectPool(int size, Func<T> crtFunc = null, Action<T> rstFunc = null, Action<T> storeFunc = null) {
        this.size = size;
        objects = new Stack<T>(size);
        createFunction = crtFunc;
        resetFunction = rstFunc;
        storeFunction = storeFunc;
    }

    public T GetObject() {
        T obj;
        if (isEmpty)
            obj = NewObject();
        else {
            obj = objects.Pop();
            if (resetFunction != null)
                resetFunction(obj);
        }
        return obj;
    }

    public bool StoreObject(T obj) {
        if (isFull) 
            return false;
        if (storeFunction != null)
            storeFunction(obj);
        objects.Push(obj);
        return true;
    }

    protected T NewObject() {
        return createFunction == null ? new T() : createFunction();
    }
}
