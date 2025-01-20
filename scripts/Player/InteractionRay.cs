using Godot;
using System;

public partial class InteractionRay : RayCast3D
{
    [Export] private Node3D followNode;
    public override void _Process(double delta)
    {
        //this prevents it from casting against the player, whilst still following the camera
        this.GlobalPosition = followNode.GlobalPosition;
        this.GlobalRotation = followNode.GlobalRotation;
    }
}
