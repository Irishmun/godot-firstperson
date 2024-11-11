using Godot;
using System;

public partial class AreaSound : AudioStreamPlayer
{
    [Export] Area3D area;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        area.BodyEntered += Area_BodyEntered;
        area.BodyExited += Area_BodyExited;
        this.StreamPaused = true;
    }
    private void Area_BodyEntered(Node3D body)
    {
        if (body is Player)
        {
            this.StreamPaused = false;
        }
    }

    private void Area_BodyExited(Node3D body)
    {
        if (body is Player)
        {
            this.StreamPaused = true;
        }
    }

}
