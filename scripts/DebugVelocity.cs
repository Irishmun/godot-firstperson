using Godot;
using System;

public partial class DebugVelocity : RigidBody3D
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        GD.Print($"{Name}'s LinearVelocity:{this.LinearVelocity.Length().ToString("0.000")} | AngularVelocity:{this.AngularVelocity.Length().ToString("0.000")}");
    }
}
