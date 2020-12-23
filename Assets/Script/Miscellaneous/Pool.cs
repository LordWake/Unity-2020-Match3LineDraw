using System.Collections.Generic;

public class Pool<T>
{
    public delegate T ObjectFactory();
    public delegate void ObjectHandler(T obj);

    private List<T> availables = new List<T>();
    private ObjectFactory factory;
    private ObjectHandler onGet;

    private int count = 0;

    public Pool(int initialCount, ObjectFactory factory, ObjectHandler onGet)
    {
        this.factory = factory;
        this.onGet = onGet;
        availables = new List<T>();
        Populate(initialCount);
    }

    private void Populate(int cant)
    {
        for (int i = 0; i < cant; i++)
        {
            T obj = factory();
            availables.Add(obj);
        }
        count += cant;
    }

    public T GetObject()
    {
        if (availables.Count <= 0)
        {
            Populate(count);
        }

        T current = availables[availables.Count - 1];
        availables.RemoveAt(availables.Count - 1);

        onGet(current);
        return current;
    }

    public void ReleaseObject(T obj)
    {
        availables.Add(obj);
    }
}
