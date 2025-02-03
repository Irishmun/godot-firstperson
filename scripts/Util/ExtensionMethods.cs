using Godot;
using System;

public static class ExtensionMethods
{
    /// <summary>Remaps value from range 1 to range 2</summary>
    /// <param name="value">value to remap</param>
    /// <param name="from1">low end of range 1</param>
    /// <param name="to1">high end of ranfe 1</param>
    /// <param name="from2">low end of range 2</param>
    /// <param name="to2">high end of range 2</param>
    /// <returns>The remapped value</returns>
    public static float ReMap(this float value, float from1, float to1, float from2, float to2)
    {//value would be "this float value" instead
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
    public static float RoundToDec(this float value, int digit)
    {//value would be "this float value" instead
        float pow = Mathf.Pow(10, digit);
        return Mathf.Floor(value * pow) / pow;
    }
    /// <summary>Returns if value is approximately zero, for floating point errors</summary>
    /// <param name="value">value to check</param>
    /// <returns>True if value is approximately zero</returns>
    public static bool IsApproxZero(this float value, float almostZero = 0.0001f)
    {//value would be "this float value" instead
        return value < almostZero && value > -almostZero;
    }
    /// <summary>Gets first child in parent with given component (and optionally, name)</summary>
    /// <typeparam name="T">node type</typeparam>
    /// <param name="parent">parent to check children for</param>
    /// <param name="name">(optional) name of node</param>
    /// <returns>Node of type (and name) if found. Null if not found</returns>
    public static T GetChildWithComponent<T>(this Node defParent, Node parent = null, string name = "", bool includeInternal = false) where T : class
    {//parent would be "this Node parent" instead
        parent ??= defParent;
        foreach (Node item in parent.GetChildren(includeInternal))
        {
            if (!string.IsNullOrWhiteSpace(name) && !item.Name.Equals(name))
            { continue; }
            if (item is T)
            { return item as T; }
        }
        return null;
    }

    public static bool IsStaticOrCSG(this GodotObject val)
    {
        return val is StaticBody3D || val is CsgShape3D;
    }
}
