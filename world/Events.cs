namespace Jaket.World;

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Safe event that will output all exceptions to the Unity console and guarantee the execution of each listener, regardless of errors. </summary>
public class SafeEvent
{
    /// <summary> List of all event listeners. </summary>
    private List<Action> listeners = new();

    /// <summary> Fires the event, i.e. fires its listeners, ensuring that they all will be executed regardless of exceptions. </summary>
    public void Fire() => listeners.ForEach(listener =>
    {
        try { listener(); }
        catch (Exception ex) { Debug.LogException(ex); }
    });

    /// <summary> Subscribes to the safe event: the listener can throw exceptions safely. </summary>
    public static SafeEvent operator +(SafeEvent e, Action listener) { e.listeners.Add(listener); return e; }

    /// <summary> Unsubscribes from the safe event if it finds the listener in the list. </summary>
    public static SafeEvent operator -(SafeEvent e, Action listener) { e.listeners.Remove(listener); return e; }
}
