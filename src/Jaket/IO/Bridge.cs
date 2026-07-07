namespace Jaket.IO;

using System.Threading;

/// <summary> Thread-safe stack of runnables connecting network and game threads. </summary>
public class Bridge
{
    /// <summary> Head of the stack. </summary>
    private Entry head;

    /// <summary> Enqueues a single runnable task. </summary>
    public void Enqueue(Runnable task)
    {
        Entry entry = new() { Task = task };

        while (true)
        {
            entry.Next = head;
            if (Interlocked.CompareExchange(ref head, entry, entry.Next) == entry.Next) break;
        }
    }

    /// <summary> Dequeues and executes all tasks. </summary>
    public void Dequeue()
    {
        Entry entry = Interlocked.Exchange(ref head, null);

        while (entry != null)
        {
            entry.Task();
            entry = entry.Next;
        }
    }

    /// <summary> Stack entry, next is nullable. </summary>
    private class Entry
    {
        public Runnable Task;
        public Entry Next;
    }
}
