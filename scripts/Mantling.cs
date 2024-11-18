using Godot;
using System;

public partial class Mantling : Node
{
    [Export] private float mantleSpeed = 0.5f;
    [Export] private RayCast3D wallRay;
    [Export] private RayCast3D floorRay;
    [Export] private RayCast3D edgeRay;
    [Export] private Node3D mantleHit;

    private Player _player;
    private Tween _tween;

    public override void _Ready()
    {
        base._Ready();
        if (mantleHit != null)
        {
            mantleHit.TopLevel = true;
        }
        _player = this.GetParent<Player>();
    }

    public bool HandleMantle(float delta, out Vector3 mantlePos, bool ApplyMantle = true)
    {
        mantlePos = Vector3.Inf;
        wallRay.ForceRaycastUpdate();
        GodotObject hit = wallRay.GetCollider();
        if (hit is StaticBody3D || hit is CsgShape3D)
        { return false; }
        floorRay.ForceRaycastUpdate();
        hit = floorRay.GetCollider();
        if (hit is StaticBody3D || hit is CsgShape3D)
        {
            //set edge ray height, then force update
            Vector3 edgePos = edgeRay.GlobalPosition;
            edgePos.Y = floorRay.GetCollisionPoint().Y - 0.01f;
            edgeRay.GlobalPosition = edgePos;
            edgeRay.ForceRaycastUpdate();
            GodotObject edgeHit = edgeRay.GetCollider();
            if (edgeHit != hit)
            { return false; }
            mantlePos = edgeRay.GetCollisionPoint();
            mantlePos.Y = floorRay.GetCollisionPoint().Y;

            if (mantleHit != null)
            { mantleHit.GlobalPosition = mantlePos; }
            if (ApplyMantle == true)
            {
                if (_tween != null)
                { return true; }
                GD.Print("Mantling to position");
                MantleToPosition(mantlePos);
            }
            return true;
        }
        return false;
    }

    public void MantleToPosition(Vector3 globalPosition)
    {
        _player.CanMove = false;
        _player.ForceCrouch = true;
        _tween = GetTree().CreateTween();
        GD.Print("Start Tween");
        _tween.SetEase(Tween.EaseType.In);
        _tween.SetTrans(Tween.TransitionType.Back);
        _tween.SetParallel(false);
        _tween.Finished += _tween_Finished;
        _tween.TweenProperty(_player, "global_position", globalPosition, 0.5f);
        GD.Print("Finished Tween");
    }

    private void _tween_Finished()
    {
        _player.ForceCrouch = false;
        _player.CanMove = true;
        GD.Print("Kill Tween");
        _tween.Kill();
        _tween = null;
    }
}
