using Godot;
using System;

public partial class FpsGui : Label
{
    public override void _Process(double delta)
    {
        Text = $"FPS:{Engine.GetFramesPerSecond()}\n" +
               $"Phys:{Engine.GetPhysicsFrames()}\n" +
               $"Drawn:{Engine.GetFramesDrawn()}";
    }
}
