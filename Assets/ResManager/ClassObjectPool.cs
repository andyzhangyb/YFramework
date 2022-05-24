using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassObjectPool<T> where T : BasePoolObject, new()
{
    protected Stack<T> dataStack = new Stack<T>();
    protected int maxCount = 0;
    protected int noRecycleCount = 0;

    public ClassObjectPool(int maxCount)
    {
        this.maxCount = maxCount;
        for (int i = 0; i < maxCount; i++)
        {
            dataStack.Push(new T());
        }
    }

    public T Spawn(bool forceSpawn = true)
    {
        T result = null;
        if (dataStack.Count > 0)
        {
            result = dataStack.Pop();
            ++noRecycleCount;
        }
        else if (forceSpawn)
        {
            result = new T();
            ++noRecycleCount;
        }
        return result;
    }

    public bool Recycle(T tObject)
    {
        if (tObject == null)
            return false;
        tObject.Reset();
        --noRecycleCount;
        if (dataStack.Count >= maxCount && maxCount > 0)
        {
            return false;
        }
        dataStack.Push(tObject);
        return true;
    }

}

public abstract class BasePoolObject
{
    public abstract void Reset();
}