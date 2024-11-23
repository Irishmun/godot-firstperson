using Godot;
using System;

public partial class PathFollower : PathFollow3D
{
    [Export] private float CircuitTime = 10;
    private float _t = 0;

    public override void _Ready()
    {
        _t = ProgressRatio * CircuitTime;
    }

    public override void _PhysicsProcess(double delta)
    {
        _t += (float)delta;
        _t %= CircuitTime;
        ProgressRatio = _t / CircuitTime;
    }
}
