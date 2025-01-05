using Godot;
using System;

public partial class Grabber : Node3D
{
    private const float POSITION_TWEEN = 0.15f, ROTATION_TWEEN = 0.15f;

    [Export] private InteractionRay interactRay;
    [Export] private float throwForce = 15;
    //[Export] private Curve forceMultiplier;
    [Export] private Vector3 heldOffset = Vector3.Zero;
    [Export] private bool centerBeforeThrow = false;

    private RigidBody3D _heldItem;
    private Node _heldParent;
    private uint _heldCollisionLayer;
    private bool _holding, lastContact;
    private int lastContactReport, collideCount;

    private Tween _tween;

    public override void _PhysicsProcess(double delta)
    {
        if (_heldItem == null || _holding == false)
        { return; }
        _heldItem.Position = heldOffset;
        _heldItem.Rotation = Vector3.Zero;
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed(Keys.USE))
        {
            if (GrabRelease())
            { GetViewport().SetInputAsHandled(); }
            return;
        }
        if (_heldItem == null)
        { return; }
        if (Input.IsActionJustPressed(Keys.MOUSE_1))
        {
            ThrowHeld();
            return;
        }
        if (Input.IsActionJustPressed(Keys.MOUSE_2))
        {
            DropHeld();
            return;
        }
    }

    private bool GrabRelease()
    {
        if (_heldItem == null)
        {
            interactRay.ForceRaycastUpdate();
            if (interactRay.GetCollider() is RigidBody3D)
            {
                GrabItem(interactRay.GetCollider() as RigidBody3D, interactRay.GetColliderShape());
                return true;
            }
        }
        else
        {
            DropHeld();
            return true;
        }
        return false;
    }

    private void GrabItem(RigidBody3D item, int shapeId)
    {
        if (!IsGrabable(item))
        { return; }
        _heldItem = item;
        _heldParent = item.GetParent();
        lastContactReport = item.MaxContactsReported;
        lastContact = item.ContactMonitor;
        item.MaxContactsReported = 1;
        item.ContactMonitor = true;
        _heldCollisionLayer = item.CollisionLayer;
        //_heldShape = item.ShapeOwnerGetShape(item.ShapeFindOwner(shapeId), shapeId);


        item.CollisionLayer = 0;
        item.SetCollisionLayerValue(16, true);
        //item.Freeze = true;
        item.GravityScale = 0;
        ReparentHeld(_heldParent, this);
        /*Vector3 Zpos = item.GlobalPosition - this.GetCollisionPoint();
        GD.Print(Zpos.Length());
        SetHeld(new Vector3(heldOffset.X, heldOffset.Y, heldOffset.Z - Zpos.Length()));*/
        SetHeld(Vector3.Zero);//SetHeld(heldOffset);

    }
    /// <summary>Drops the held item, releasing it</summary>
    private void DropHeld()
    {
        if (CanRelease() == false)
        { return; }
        ReleaseHeld();
    }
    /// <summary>Throws the held item, applying a centlral impulse in the facing direction</summary>
    private void ThrowHeld()
    {
        if (CanRelease() == false)
        { return; }
        if (centerBeforeThrow == true)
        { _heldItem.Position = new Vector3(-Position.X, -Position.Y, 0); }
        RigidBody3D held = _heldItem;
        ReleaseHeld();
        //(raycast.global_basis * raycast.target_position).normalized()
        float force = throwForce * CalcThrowForce(held.Mass);
        GD.Print($"({held.Mass}kg)(X{CalcThrowForce(held.Mass)})Throw: " + force * held.Mass);
        GD.Print("player:" + Player.Instance.HorizontalVelocity.Length());
        if (Player.Instance.HorizontalVelocity.LengthSquared() > 0.0001f)
        { force += Player.Instance.Velocity.Length(); }
        held.ApplyCentralImpulse((this.GlobalBasis * interactRay.TargetPosition).Normalized() * (force * held.Mass));
    }

    private bool CanRelease()
    {
        collideCount = _heldItem.GetCollidingBodies().Count;
        GD.Print($"Colliding with: ({collideCount}){_heldItem.GetCollidingBodies()}");

        return collideCount <= 0;
        /*interactRay.ForceRaycastUpdate();
        GD.Print($"colliding ({interactRay.IsColliding()}) with: {(interactRay.GetCollider() as Node)?.Name}");
        if (!interactRay.IsColliding())
        { return true; }
        return interactRay.GetCollider() is Area3D;*/
    }

    private void ReleaseHeld()
    {
        ReparentHeld(this, _heldParent);
        //_heldItem.Freeze = false;
        _heldItem.ContactMonitor = lastContact;
        _heldItem.MaxContactsReported = lastContactReport;
        _heldItem.GravityScale = 1;
        _heldItem.CollisionLayer = _heldCollisionLayer;
        _heldItem = null;
        _holding = false;
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
        _tween.Finished += Tween_Finished;
        //_tween.TweenProperty(this, "dotRadius", radius, time);
        _tween.TweenProperty(_heldItem, "rotation", Vector3.Zero, ROTATION_TWEEN);
        _tween.SetParallel();
        _tween.TweenProperty(_heldItem, "position:x", heldPosition.X, POSITION_TWEEN);
        _tween.TweenProperty(_heldItem, "position:y", heldPosition.Y, POSITION_TWEEN);
        _tween.TweenProperty(_heldItem, "position:z", heldPosition.Z, POSITION_TWEEN);
    }
    private void Tween_Finished()
    {
        _holding = true;
        _tween.Kill();
        _tween = null;
    }

    private float CalcThrowForce(float mass)
    {
        return 1f - (mass * 0.01f);//0.01f to (in practice) have a 100kg limit on throwing stuff
    }

    private bool IsGrabable(RigidBody3D item)
    {
        return item.GetGroups().Contains("Interactable");
    }
}
