namespace Jaket.Harmony;

using HarmonyLib;
using System;
using System.Reflection;

/// <summary> Attribute that defines a target of the patch. </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class Patch : Attribute
{
    /// <summary> Class containing the target. </summary>
    private Type type;
    /// <summary> Name of the property/method. </summary>
    private string name;
    /// <summary> Arguments, null if any type. </summary>
    private Type[] args;
    /// <summary> Signature, null if any type. </summary>
    private MethodType? sign;

    /// <summary> Target method to be patched. </summary>
    public MethodBase Target => sign switch
    {
        MethodType.Constructor => AccessTools.Constructor   (type, args),
        MethodType.Getter      => AccessTools.PropertyGetter(type, name),
        MethodType.Setter      => AccessTools.PropertySetter(type, name),
        _                      => AccessTools.Method        (type, name, args),
    };

    public Patch(Type type, string name)
    {
        this.type = type;
        this.name = name;
    }

    public Patch(Type type, string name, params Type[] args) : this(type, name)
    {
        this.args = args;
    }

    public Patch(Type type, string name, MethodType sign) : this(type, name)
    {
        this.sign = sign;
    }

    public HarmonyMethod GetPatch(MethodInfo method) => new(method, Priority.High) { methodType = sign };
}

/// <summary> Attribute that defines a dynamic patch. </summary>
public class DynamicPatch : Patch
{
    public DynamicPatch(Type type, string name) : base(type, name) { }

    public DynamicPatch(Type type, string name, params Type[] args) : base(type, name, args) { }

    public DynamicPatch(Type type, string name, MethodType sign) : base(type, name, sign) { }
}

/// <summary> Attribute that defines a static patch. </summary>
public class StaticPatch : Patch
{
    public StaticPatch(Type type, string name) : base(type, name) { }

    public StaticPatch(Type type, string name, params Type[] args) : base(type, name, args) { }

    public StaticPatch(Type type, string name, MethodType sign) : base(type, name, sign) { }
}

/// <summary> Attribute that defines a prefix. </summary>
public class Prefix : Attribute { }

/// <summary> Attribute that defines a postfix. </summary>
public class Postfix : Attribute { }

/// <summary> Attribute that defines a transpiler. </summary>
public class Transpiler : Attribute { }
