namespace Jaket.Tools;

/// <summary> Performs an action with no arguments. </summary>
public delegate void Runnable();

/// <summary> Performs an action with one argument. </summary>
public delegate void Cons<T>(T t);

/// <summary> Performs an action with two arguments. </summary>
public delegate void Cons<T, K>(T t, K k);

/// <summary> Predicate that expends one value. </summary>
public delegate bool Pred<T>(T t);

/// <summary> Provider that supplies one value. </summary>
public delegate T Prov<T>();

/// <summary> Function that supplies one value by expending another. </summary>
public delegate K Func<T, K>(T t);
