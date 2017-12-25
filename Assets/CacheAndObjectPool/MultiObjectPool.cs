using System;

public class MultiObjectPool<T> : ObjectPool<T>
    where T : class, new(){
    public int choice {
        private set;
        get;
    }

    public MultiObjectPool(int choice, int size, Func<int, T> crtFunc = null, 
        Action<T> rstFunc = null, Action<T> storeFunc = null)
        :base(size, null, rstFunc, storeFunc){
        createFunction = () => { return crtFunc(choice); };
    }
}
