using Godot;
using System;

public partial class CrossHairs : CenterContainer
{
    public static CrossHairs Instance;

    [Export] private float dotRadius = 1;
    [Export] private Color crossHairColor = Colors.White;
    private float _currentRadius;
    private Tween _tween;

    private Vector2 shadowOffset = new Vector2(1, 1);
    private Vector2 _center;
    private bool _request = false;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _center = this.Size * 0.5f;
        Instance = this;
        ResizeReticle(dotRadius);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_request)
        {
            if (CrossHairs.Instance.CurrentRadius != 5 && !CrossHairs.Instance.IsTweening)
            { CrossHairs.Instance.ChangeRadius(5); }
        }
        else
        {
            if (CrossHairs.Instance.CurrentRadius != 1 && !CrossHairs.Instance.IsTweening)
            { CrossHairs.Instance.ChangeRadius(1); }
        }
        _request = false;
    }


    public override void _Draw()
    {
        DrawCircle(_center, _currentRadius + 1, Colors.Black, false, -1);
        DrawCircle(_center, _currentRadius, crossHairColor, false, -1);
    }

    public void ChangeRadius(float radius = 1, float time = 0.1f)
    {
        //GD.Print($"dotradius:{_currentRadius} change to:{radius}");
        if (_currentRadius == radius)
        { return; }
        Callable call = new Callable(this, "ResizeReticle");
        _tween = GetTree().CreateTween();
        _tween.Finished += Tween_Finished;
        //_tween.TweenProperty(this, "dotRadius", radius, time);
        _tween.TweenMethod(call, _currentRadius, radius, time);

    }

    private void ResizeReticle(float radius)
    {
        _currentRadius = radius;
        QueueRedraw();
    }

    private void Tween_Finished()
    {
        _tween.Kill();
        _tween = null;
    }

    public bool IsTweening => _tween != null && _tween.IsRunning();
    public float CurrentRadius => _currentRadius;

    public bool RequestIndicate { set => _request = value; }
}
