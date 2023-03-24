using System.Collections.Concurrent;

ObjectCreator<TestObject> creator = new();
ObjectPool<TestObject> pool = ObjectPool<TestObject>.GetObjectPool(creator, 2);
Console.WriteLine($"Pool count: {pool.Count}");
TestObject o1 = pool.GetObject();
o1.X = 1; o1.Y = 2;
TestObject o2 = pool.GetObject();
o2.X = 3; o2.Y = 4;
TestObject o3 = pool.GetObject();
o3.X = 5; o3.Y = 6;
Console.WriteLine($"Pool count: {pool.Count}");
Console.WriteLine($"o1: {o1}; o2: {o2}; o3: {o3}");
pool.ReturnObject(ref o1);
Console.WriteLine($"{o1}");
Console.WriteLine($"Pool count: {pool.Count}");
TestObject o4 = pool.GetObject();
Console.WriteLine($"o4: {o4}");

public interface IPoolable
{
    void ResetState();
}
public class TestObject : IPoolable
{
    public int X { get; set; }
    public int Y { get; set; }

    public TestObject()
    {
        X = 0;
        Y = 0;
    }
    void IPoolable.ResetState()
    {
        X = 0;
        Y = 0;
    }
    public override string ToString()
    {
        return $"({X},{Y})";
    }
}

public interface IPoolObjectCreator<T>
{
    T CreateObject();
}
public class ObjectCreator<T> : IPoolObjectCreator<T> where T : class, new()
{
    T IPoolObjectCreator<T>.CreateObject()
    {
        return new T();
    }
}

public class ObjectPool<T> where T : class, IPoolable
{
    private static ObjectPool<T>? _objectPool = null;
    private static object syncRoot = new();

    private static ConcurrentBag<T> _pool = new ConcurrentBag<T>();
    private static IPoolObjectCreator<T>? _objectCreator = null;

    public int Count { get { return _pool.Count; } }
    private ObjectPool(IPoolObjectCreator<T> creator, int count)
    {
        if (creator == null)
            throw new ArgumentNullException("creator can't be null");
        _objectCreator = creator;
        for (int i = 0; i < count; i++)
            _pool.Add(creator.CreateObject());
    }
    public static ObjectPool<T> GetObjectPool(IPoolObjectCreator<T> creator, int count)
    {
        if (_objectPool == null)
            lock(syncRoot)
                _objectPool ??= new ObjectPool<T>(creator, count);
        return _objectPool;
    }
    public T GetObject()
    {
        T obj;
        if (_pool.TryTake(out obj))
            return obj;

        return _objectCreator.CreateObject();
    }
    public void ReturnObject(ref T obj)
    {
        obj.ResetState();
        _pool.Add(obj);
        obj = null;
    }
}