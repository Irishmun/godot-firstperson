using Godot;
using System;

public partial class PathFollower : PathFollow3D
{
    [Export] private float CircuitTime = 10;

    //private float _t = 0;
    private Tween _tween;

    public override void _Ready()
    {
        _tween = GetTree().CreateTween();
        //_tween.SetEase(Tween.EaseType.OutIn);
        _tween.SetTrans(Tween.TransitionType.Sine);
        _tween.BindNode(this);
        _tween.SetLoops();
        _tween.TweenProperty(this, "progress_ratio", 1, CircuitTime);
        _tween.TweenProperty(this, "progress_ratio", 0, CircuitTime);
        //_t = ProgressRatio * CircuitTime;
    }

    /*public override void _PhysicsProcess(double delta)
    {
        _t += (float)delta;
        _t %= CircuitTime;
        ProgressRatio = _t / CircuitTime;
    }*/
}
