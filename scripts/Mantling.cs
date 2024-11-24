using Godot;
using System;

public partial class Mantling : Node3D
{
    [Export] private float mantleSpeed = 0.5f;
    [Export] private RayCast3D wallRay;
    [Export] private RayCast3D floorRay;
    [Export] private RayCast3D ceilingRay;
    [Export] private RayCast3D edgeRay;
    [Export] private Node3D mantleHit;

    private Player _player;
    private Node _playerBaseParent;
    private Tween _tween;
    private float _headOffset;

    public override void _Ready()
    {
        base._Ready();
        if (mantleHit != null)
        {
            mantleHit.TopLevel = true;
        }
        _player = this.GetParent<Player>();
        _playerBaseParent = _player.GetParent();
        _headOffset = Position.Y;
        GD.Print(_headOffset);
    }

    public bool HandleMantle(float delta, out Vector3 mantlePos, bool ApplyMantle = true)
    {
        Vector3 pos = this.Position;
        float offset = _headOffset - _player.StandingHeight;
        pos.Y = _player.IsCrouching ? _player.CrouchHeight + offset : _player.StandingHeight + offset;
        this.Position = pos;
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
            mantlePos.Y = floorRay.GetCollisionPoint().Y + 0.01f;

            if (mantleHit != null)
            { mantleHit.GlobalPosition = floorRay.GetCollisionPoint(); }
            if (ApplyMantle == true)
            {
                if (_tween != null)
                { return true; }
                ceilingRay.GlobalPosition = floorRay.GetCollisionPoint();
                ceilingRay.TargetPosition = Vector3.Up * _player.StandingHeight;
                ceilingRay.ForceRaycastUpdate();
                GodotObject crouchHit = ceilingRay.GetCollider();
                mantlePos = mantlePos - ((Node3D)hit).GlobalPosition;
                MantleToPosition(mantlePos, (Node)hit, crouchHit is StaticBody3D || crouchHit is CsgShape3D);
            }
            return true;
        }
        return false;
    }

    public void MantleToPosition(Vector3 globalPosition, Node mantleObject, bool forceCrouch = false)
    {
        _player.CanMove = false;
        //_player.ForceVelocity(Vector3.Zero);
        ReparentPlayer(mantleObject, _player.GetParent());
        ReparentMantle(mantleObject);
        _tween = GetTree().CreateTween();
        _tween.SetEase(Tween.EaseType.In);
        if (forceCrouch == false)
        { _tween.SetTrans(Tween.TransitionType.Back); }
        _tween.Finished += _tween_Finished;
        _tween.TweenProperty(_player, "position", globalPosition, 0.5f);
        _player.ForceCrouch = forceCrouch;
    }

    private void _tween_Finished()
    {
        ReparentPlayer(_playerBaseParent, _player.GetParent());
        _player.ForceCrouch = false;
        _player.CanMove = true;
        _tween.Kill();
        _tween = null;
    }

    private void ReparentPlayer(Node newParrent, Node oldParrent)
    {
        if (_player.GetParent() == newParrent)
        { return; }
        Vector3 playerPos = _player.GlobalPosition;
        Vector3 playerRot = _player.GlobalRotation;
        //GD.Print($"Player Old Rotation: (Global){playerRot} (Local){_player.Rotation}");
        oldParrent.RemoveChild(_player);
        newParrent.AddChild(_player);
        _player.GlobalPosition = playerPos;
        _player.GlobalRotation = playerRot;
        //GD.Print($"Player New Rotation: (Global){playerRot} (Local){_player.Rotation}");
    }

    private void ReparentMantle(Node newParrent)
    {
        if (mantleHit == null)
        { return; }
        mantleHit.TopLevel = false;
        Vector3 mantlePos = mantleHit.GlobalPosition;
        mantleHit.GetParent()?.RemoveChild(mantleHit);
        newParrent.AddChild(mantleHit);
        mantleHit.GlobalPosition = mantlePos;
    }
}
