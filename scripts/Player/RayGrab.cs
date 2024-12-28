using Godot;
using System;

public partial class RayGrab : RayCast3D
{
    private const float POSITION_TWEEN = 0.25f, ROTATION_TWEEN = 0.1f;

    [Export] private float throwForce = 15;
    //[Export] private Curve forceMultiplier;
    [Export] private Vector3 heldOffset = Vector3.Zero;
    [Export] private Node3D followNode;

    private RigidBody3D _heldItem;
    private Node _heldParent;
    private uint _heldCollisionLayer;
    private Tween _tween;

    public override void _Process(double delta)
    {
        //this prevents it from casting against the player, whilst still following the camera
        this.GlobalPosition = followNode.GlobalPosition;
        this.GlobalRotation = followNode.GlobalRotation;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!this.IsColliding())
        {
            if (CrossHairs.Instance.CurrentRadius == 1)
            { return; }
            if (CrossHairs.Instance.IsTweening)
            { return; }
            CrossHairs.Instance.ChangeRadius(1);
            return;
        }
        if (this.IsColliding())
        {
            if (this.GetCollider() is RigidBody3D)
            {
                if (CrossHairs.Instance.CurrentRadius == 5)
                { return; }
                if (CrossHairs.Instance.IsTweening)
                { return; }
                CrossHairs.Instance.ChangeRadius(5);
            }
            else
            {
                if (CrossHairs.Instance.CurrentRadius == 1)
                { return; }
                if (CrossHairs.Instance.IsTweening)
                { return; }
                CrossHairs.Instance.ChangeRadius(1);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("Use"))
        {
            GrabRelease();
            return;
        }
        if (_heldItem == null)
        { return; }
        if (Input.IsActionJustPressed("Mouse_1"))
        {
            ThrowHeld();
            return;
        }
        if (Input.IsActionJustPressed("Mouse_2"))
        {
            DropHeld();
            return;
        }
    }

    private void GrabRelease()
    {
        if (_heldItem == null)
        {
            this.ForceRaycastUpdate();
            if (this.GetCollider() is RigidBody3D)
            {
                GrabItem(this.GetCollider() as RigidBody3D);
            }
        }
        else
        {
            DropHeld();
        }
    }

    private void GrabItem(RigidBody3D item)
    {
        _heldItem = item;
        _heldParent = item.GetParent();
        _heldCollisionLayer = item.CollisionLayer;

        item.CollisionLayer = 0;
        item.SetCollisionLayerValue(16, true);
        item.Freeze = true;
        ReparentHeld(_heldParent, this);
        /*Vector3 Zpos = item.GlobalPosition - this.GetCollisionPoint();
        GD.Print(Zpos.Length());
        SetHeld(new Vector3(heldOffset.X, heldOffset.Y, heldOffset.Z - Zpos.Length()));*/
        SetHeld(heldOffset);
    }

    /// <summary>Throws the held item, applying a centlral impulse in the facing direction</summary>
    private void ThrowHeld()
    {
        if (CanRelease() == false)
        { return; }
        RigidBody3D held = _heldItem;
        ReleaseHeld();
        //(raycast.global_basis * raycast.target_position).normalized()
        float force = throwForce * CalcThrowForce(held.Mass);
        GD.Print($"({held.Mass}kg)(X{CalcThrowForce(held.Mass)})Throw: " + force * held.Mass);
        GD.Print("player:" + Player.Instance.HorizontalVelocity.Length());
        if (Player.Instance.HorizontalVelocity.LengthSquared() > 0.0001f)
        {
            force += Player.Instance.Velocity.Length();
        }
        held.ApplyCentralImpulse((this.GlobalBasis * this.TargetPosition).Normalized() * (force * held.Mass));
    }

    /// <summary>Drops the held item, releasing it</summary>
    private void DropHeld()
    {
        if (CanRelease() == false)
        { return; }
        ReleaseHeld();
    }

    private bool CanRelease()
    {
        this.ForceRaycastUpdate();
        GD.Print($"colliding ({this.IsColliding()}) with: {(this.GetCollider() as Node)?.Name}");
        return !this.IsColliding();
    }

    private void ReleaseHeld()
    {
        ReparentHeld(this, _heldParent);
        _heldItem.Freeze = false;
        _heldItem.CollisionLayer = _heldCollisionLayer;
        _heldItem = null;
    }

    private void ReparentHeld(Node oldParent, Node newParent)
    {
        if (_heldItem.GetParent() == newParent)
        { return; }
        Vector3 heldPos = _heldItem.GlobalPosition;
        Vector3 heldRot = _heldItem.GlobalRotation;
        //GD.Print($"Player Old Rotation: (Global){playerRot} (Local){_player.Rotation}");
        oldParent?.RemoveChild(_heldItem);
        newParent.AddChild(_heldItem);
        _heldItem.GlobalPosition = heldPos;
        _heldItem.GlobalRotation = heldRot;
        //GD.Print($"Player New Rotation: (Global){playerRot} (Local){_player.Rotation}");
    }


    public void SetHeld(Vector3 heldPosition)
    {
        _tween = GetTree().CreateTween();
        _tween.Finished += _tween_Finished;
        //_tween.TweenProperty(this, "dotRadius", radius, time);
        _tween.TweenProperty(_heldItem, "rotation", Vector3.Zero, ROTATION_TWEEN);
        _tween.SetParallel();
        _tween.TweenProperty(_heldItem, "position:x", heldPosition.X, POSITION_TWEEN);
        _tween.TweenProperty(_heldItem, "position:y", heldPosition.Y, POSITION_TWEEN);
        _tween.TweenProperty(_heldItem, "position:z", heldPosition.Z, POSITION_TWEEN);
    }
    private void _tween_Finished()
    {
        _tween.Kill();
        _tween = null;
    }

    private float CalcThrowForce(float mass)
    {
        return 1f - (mass * 0.01f);//0.01f to (in practice) have a 100kg limit on throwing stuff
    }
}
