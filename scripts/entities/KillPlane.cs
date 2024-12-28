using Godot;
using System;

public partial class KillPlane : Area3D
{
    [Export] private Node3D respawnLocation;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.BodyEntered += KillPlane_BodyEntered;
    }

    private void KillPlane_BodyEntered(Node3D body)
    {
        body.GlobalPosition = respawnLocation.GlobalPosition;
        if (body is RigidBody3D)
        {
            (body as RigidBody3D).LinearVelocity = Vector3.Zero;
        }
    }
}
