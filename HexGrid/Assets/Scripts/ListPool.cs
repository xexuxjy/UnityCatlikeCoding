using System.Collections.Generic;

public static class ListPool<T>
{
    static Stack<List<T>> m_stack = new Stack<List<T>>();


    public static List<T> Get()
    {
        if (m_stack.Count > 0)
        {
            return m_stack.Pop();
        }
        return new List<T>();
    }

    public static void Return(List<T> l)
    {
        l.Clear();
        m_stack.Push(l);
    }



}
