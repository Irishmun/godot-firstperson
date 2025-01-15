using Godot;
using System;

public partial class Zipline : StaticBody3D
{
    private const float MAX_ZIP_TIME = 5f;
    private const float ZIPLINE_VELOCITY = 7f;//fast than running
    [Export] private Node3D otherEnd;
    [Export] private bool isMain;
    [Export] private Node3D cablePoint;
    [Export] private Material cableMaterial;
    [Export] private float ExitVelocity = 10;

    private float _lineTime;

    private Tween _tween;
    private Player _zippingPlayer;
    private Node3D _cableEnd;
    private bool _isVertical = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (otherEnd == null)
        { return; }
        if (isMain == false)
        { return; }
        if (otherEnd is Zipline)
        {
            _cableEnd = (otherEnd as Zipline).CablePoint;
            _lineTime = Mathf.Clamp(lineDelta().Length() / ZIPLINE_VELOCITY, 0, MAX_ZIP_TIME);
            (otherEnd as Zipline).CableEnd = cablePoint;
            (otherEnd as Zipline).LineTime = _lineTime;
        }
        else
        {
            _cableEnd = otherEnd;
            _lineTime = lineDelta().Length() / ZIPLINE_VELOCITY;
        }
        if (cablePoint.GlobalPosition.X.RoundToDec(3) == _cableEnd.GlobalPosition.X.RoundToDec(3) && cablePoint.GlobalPosition.Z.RoundToDec(3) == _cableEnd.GlobalPosition.Z.RoundToDec(3))
        {
            _isVertical = true;
        }
        GenerateCable(_cableEnd);
    }
    public override void _Input(InputEvent e)
    {
        if (_zippingPlayer == null)
        { return; }
        if (e.IsActionPressed(Keys.JUMP))
        {
            EndTween();
            _zippingPlayer.SetPlayerState(PlayerStates.STANDING, true);
            Vector3 endVelocity = -lineDelta().Normalized() * ExitVelocity;
            endVelocity.Y += _zippingPlayer.JumpForce;
            _zippingPlayer.OverrideVelocity(endVelocity);
            _zippingPlayer.ForceInputCheck();
            _zippingPlayer = null;
        }
    }
    private void GenerateCable(Node3D otherPoint)
    {
        MeshInstance3D mesh = new MeshInstance3D();
        CylinderMesh cyl = new CylinderMesh();
        cyl.BottomRadius = 0.02f;
        cyl.TopRadius = 0.02f;
        cyl.Height = lineDelta().Length();
        cyl.Rings = 0;
        cyl.RadialSegments = 8;
        cyl.Material = cableMaterial;
        mesh.Mesh = cyl;
        cablePoint.AddChild(mesh);
        if (_isVertical == false)
        {
            cablePoint.LookAt(otherPoint.GlobalPosition, Vector3.Up);
            mesh.RotateX(Mathf.DegToRad(90));
        }
        mesh.GlobalPosition = center();
        //mesh.GlobalRotation = cablePoint.GlobalPosition.Angle
    }
    public void Interact(Player player)
    {
        _zippingPlayer = player;
        _zippingPlayer.SetPlayerState(PlayerStates.ZIPLINING);
        _zippingPlayer.OverrideVelocity(Vector3.Zero);
        Vector3 pos = cablePoint.GlobalPosition;
        pos.Y -= _zippingPlayer.StandingHeight;
        _zippingPlayer.GlobalPosition = pos;
        GD.Print("ziplining for:" + _lineTime);
        Zip();
        //tween to other end global position at player zipline speed
    }
    public void Zip()
    {
        if (_tween != null && _tween.IsRunning())
        { return; }
        //GD.Print("tweening to:" + fov);
        _tween = GetTree().CreateTween();
        _tween.SetEase(Tween.EaseType.In);
        _tween.SetTrans(Tween.TransitionType.Sine);
        _tween.Finished += Tween_Finished;
        //_tween.TweenProperty(this, "dotRadius", radius, time);
        Vector3 pos = _cableEnd.GlobalPosition;
        if (_isVertical == false)
        {
            pos.Y -= _zippingPlayer.StandingHeight;
        }
        _tween.TweenProperty(_zippingPlayer, "global_position", pos, _lineTime);
    }
    private void Tween_Finished()
    {
        _zippingPlayer.SetPlayerState(PlayerStates.STANDING, true);
        if (_isVertical == false)
        {
            _zippingPlayer.OverrideVelocity(-lineDelta().Normalized() * ExitVelocity);
        }
        _zippingPlayer = null;
        EndTween();
    }
    private void EndTween()
    {
        _tween.Kill();
        _tween = null;
    }
    private Vector3 lineDelta()
    {
        return cablePoint.GlobalPosition - _cableEnd.GlobalPosition;
    }
    private Vector3 center()
    {
        return new Vector3(Mathf.Lerp(cablePoint.GlobalPosition.X, _cableEnd.GlobalPosition.X, 0.5f),
                           Mathf.Lerp(cablePoint.GlobalPosition.Y, _cableEnd.GlobalPosition.Y, 0.5f),
                           Mathf.Lerp(cablePoint.GlobalPosition.Z, _cableEnd.GlobalPosition.Z, 0.5f));
    }

    public float LineTime { get => _lineTime; set => _lineTime = value; }
    public Node3D CableEnd { get => _cableEnd; set => _cableEnd = value; }
    public Node3D CablePoint => cablePoint;
}
