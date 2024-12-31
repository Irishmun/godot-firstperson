using Godot;
using System;

public partial class UnGrabable : Node
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.SetMeta("Grabable", false);
    }
}
