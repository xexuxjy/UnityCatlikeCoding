using C5;
using System.Collections.Generic;

public class HexCellPriorityQueue
{


    public void Enqueue(HexCell cell)
    {
        IPriorityQueueHandle<HexCell> handle = null;
        m_heap.Add(ref handle, cell);
        m_handleMap.Add(cell, handle);
    }

    public HexCell Dequeue()
    {
        HexCell cell = m_heap.DeleteMin();
        m_handleMap.Remove(cell);
        return cell;
    }

    public void Change(HexCell cell)
    {
        IPriorityQueueHandle<HexCell> handle;
        if (m_handleMap.TryGetValue(cell, out handle))
        {
            m_heap.Replace(handle, cell);
        }
    }

    public void Clear()
    {
        while (!m_heap.IsEmpty)
        {
            m_heap.DeleteMin();
        }
        m_handleMap.Clear();
    }

    public int Count
    {
        get { return m_heap.Count; }
    }

    Dictionary<HexCell, IPriorityQueueHandle<HexCell>> m_handleMap = new Dictionary<HexCell, IPriorityQueueHandle<HexCell>>();
    C5.IntervalHeap<HexCell> m_heap = new C5.IntervalHeap<HexCell>();

}