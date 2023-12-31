namespace Jaket.World;

using System;
using UnityEngine;

/// <summary> Abstract activator containing only the level it is intended for and a method for activating an object. </summary>
public abstract class Activator
{
    /// <summary> Level for which the activator is intended. </summary>
    public readonly string Level;
    /// <summary> Whether an object needs to be reactivated after loading to the checkpoint. </summary>
    public readonly bool Disposable;

    public Activator(string level, bool disposable)
    {
        this.Level = level;
        this.Disposable = disposable;
    }

    /// <summary> Initializes the activator at the current level, usually this finds an object at the level and adds an event listener to it. </summary>
    public abstract void Init();

    /// <summary> Activates the object to which this activator is bound. </summary>
    public abstract void Activate();
}

/// <summary> Activator that sends an event to clients when activated by the host and fires an action when activated by the client. </summary>
public class SynchronizedActivator : Activator
{
    /// <summary> Object provider with which the activator will work. </summary>
    public ObjectProv Object;
    /// <summary> Action to be taken when the object is activated on the client. </summary>
    public Action<GameObject> Action;

    public SynchronizedActivator(string level, bool disposable, ObjectProv obj, Action<GameObject> action) : base(level, disposable)
    {
        this.Object = obj;
        this.Action = action;
    }

    public override void Init() => Object()?.events.onActivate.AddListener(() => World.Instance.SendObjectActivation(this));

    public override void Activate() => Action(Object()?.gameObject);
}

/// <summary> Activator that fires immediately when loading into a level. </summary>
public class InstantActivator : Activator
{
    /// <summary> Object provider with which the activator will work. </summary>
    public ObjectProv Object;

    public InstantActivator(string level, ObjectProv obj) : base(level, false) => this.Object = obj;

    public override void Init()
    {
        foreach (var obj in Object()?.events.toActivateObjects) obj.SetActive(true);
    }

    public override void Activate() {}
}

/// <summary> Activator that simply triggers the action when loading into a level. </summary>
public class ActionActivator : Activator
{
    /// <summary> Action to run on the level. </summary>
    public Action Action;

    public ActionActivator(string level, Action action) : base(level, false) => this.Action = action;

    public override void Init() => Action();

    public override void Activate() {}
}

/// <summary> Object activator provider. </summary>
public delegate ObjectActivator ObjectProv();

/// <summary> Class containing methods for getting synchronized and instant activators of different objects. </summary>
public class Activators
{
    /// <summary> Returns an object provider that finds the right one via the predicate. </summary>
    public static ObjectProv Prov(Predicate<ObjectActivator> pred) => () => Array.Find(Resources.FindObjectsOfTypeAll<ObjectActivator>(), pred);

    /// <summary> Wraps the action in a wrapper that activates the given object. </summary>
    public static Action<GameObject> Action(Action<GameObject> action) => action == null ? obj => obj?.SetActive(true) : obj => { obj?.SetActive(true); action(obj); };

    public static SynchronizedActivator FindByNameAndActiveParent(string level, string name, Action<GameObject> action = null, bool disposable = false) =>
            new SynchronizedActivator(level, disposable, Prov(a => a.name == name && a.transform.parent.gameObject.activeInHierarchy), Action(action));

    public static SynchronizedActivator FindByNameAndActiveParentOfParent(string level, string name, Action<GameObject> action = null, bool disposable = false) =>
            new SynchronizedActivator(level, disposable, Prov(a => a.name == name && a.transform.parent.parent.gameObject.activeInHierarchy), Action(action));

    public static SynchronizedActivator FindByNameAndActiveParentOfParentOfParent(string level, string name, Action<GameObject> action = null, bool disposable = false) =>
            new SynchronizedActivator(level, disposable, Prov(a => a.name == name && a.transform.parent.parent.parent.gameObject.activeInHierarchy), Action(action));

    public static SynchronizedActivator FindByParentOfParentNameAndActive(string level, string name, Action<GameObject> action = null, bool disposable = false) =>
            new SynchronizedActivator(level, disposable, Prov(a =>
            {
                var pop = a.transform.parent?.parent?.gameObject;
                return pop != null && pop.name.StartsWith(name) && pop.activeInHierarchy;
            }), Action(action));

    public static SynchronizedActivator FindDoorEveryoneKnowsAbout(string level, string name, Action<GameObject> action = null, bool disposable = false) =>
            new SynchronizedActivator(level, disposable, Prov(a => a.transform.parent?.gameObject.name == name && a.transform.parent.parent == null), Action(action));

    public static InstantActivator FindInstantByName(string level, string name) =>
            new InstantActivator(level, Prov(a => a.name == name));
}
