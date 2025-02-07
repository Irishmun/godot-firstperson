using Godot;
//NOTE: Mantling EXPECTS that the mesh is not scaled AT ALL, it breaks otherwise

public partial class Mantling : Node3D
{
    [Export] private float mantleDuration = 0.5f;
    [Export] private float mantleBuffer = 0.1f;
    [Export] private RayCast3D wallRay;
    [Export] private RayCast3D floorRay;
    [Export] private RayCast3D ceilingRay;
    [Export] private RayCast3D edgeRay;
    [Export] private Node3D mantleHit;
    [Export] private Node3D handContact;

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
        EnableRays(false);
        EnableHandContacts(false);
    }

    public bool HandleMantle(bool ApplyMantle = true)
    {
        Vector3 mantlePos, pos;
        pos = this.Position;
        float offset = _headOffset - _player.StandingHeight;
        pos.Y = _player.IsCrouching ? _player.CrouchHeight + offset : _player.StandingHeight + offset;
        this.Position = pos;
        //EnableRays(true);
        wallRay.ForceRaycastUpdate();
        GodotObject hit = wallRay.GetCollider();
        if (hit.IsStaticOrCSG())
        { return false; }
        floorRay.ForceRaycastUpdate();
        hit = floorRay.GetCollider();
        if (hit is StaticBody3D == false && hit is CsgShape3D == false)
        {
            //EnableRays(false);
            return false;
        }
        //set edge ray height, then force update
        Vector3 edgePos = edgeRay.GlobalPosition;
        edgePos.Y = floorRay.GetCollisionPoint().Y - 0.01f;
        edgeRay.GlobalPosition = edgePos;
        edgeRay.ForceRaycastUpdate();
        GodotObject edgeHit = edgeRay.GetCollider();
        if (edgeHit != hit)
        { return false; }

        mantlePos = edgeRay.GetCollisionPoint();// + ((edgeRay.GlobalBasis * edgeRay.TargetPosition).Normalized() * 0.1f);
        mantlePos.Y = floorRay.GetCollisionPoint().Y + 0.01f;

        //set debug indicator position if it exists
        if (mantleHit != null) { mantleHit.GlobalPosition = mantlePos; }
        handContact.GlobalPosition = mantlePos;
        if (ApplyMantle == true)
        {
            if (_tween != null)
            { return true; }
            ceilingRay.GlobalPosition = floorRay.GetCollisionPoint();
            ceilingRay.TargetPosition = Vector3.Up * _player.StandingHeight;
            ceilingRay.ForceRaycastUpdate();
            GodotObject crouchHit = ceilingRay.GetCollider();
            EnableHandContacts(true);
            MantleToPosition(mantlePos, (Node3D)hit, crouchHit.IsStaticOrCSG());
        }
        //EnableRays(false);
        return true;
    }

    public void MantleToPosition(Vector3 mantlePosGlobalSpace, Node3D mantleObject, bool forceCrouch = false)
    {
        //GD.Print($"Mantling to: {globalPosition} on {mantleObject.Name}");
        _player.CanMove = false;
        Vector3 mantlePosLocalSpace = mantleObject.ToLocal(mantlePosGlobalSpace);
        ReparentPlayer(mantleObject, _player.GetParent());
        ReparentMantle(mantleObject);
        ReparentHandContacts(mantleObject, false);
        _tween = GetTree().CreateTween();
        _tween.SetEase(Tween.EaseType.In);
        if (forceCrouch == false)
        { _tween.SetTrans(Tween.TransitionType.Back); }
        _tween.Finished += Tween_Finished;
        _tween.TweenProperty(_player, "position", mantlePosLocalSpace, mantleDuration);
        if (forceCrouch)
        {
            _player.ForceCrouch();
        }
    }

    private void Tween_Finished()
    {
        _player.OverrideVelocity(Vector3.Zero);
        //TODO: maybe always reparent back to (current) scene root instead
        ReparentPlayer(_playerBaseParent, _player.GetParent());
        EnableHandContacts(false);
        ReparentHandContacts(floorRay, true);
        _player.CanMove = true;
        _player.ForceInputCheck();
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
        _player.OverridePosition(playerPos);
        _player.OverrideRotation(playerRot);
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
        GD.Print("mantle new local: " + mantleHit.Position);
    }

    private void EnableRays(bool enabled)
    {
        wallRay.Enabled = enabled;
        floorRay.Enabled = enabled;
        ceilingRay.Enabled = enabled;
        edgeRay.Enabled = enabled;
    }

    private void EnableHandContacts(bool enabled)
    {
        handContact.Visible = enabled;
    }

    private void ReparentHandContacts(Node newParent, bool resetRotation = false)
    {
        Vector3 leftHandPos, leftHandRot;
        leftHandPos = handContact.GlobalPosition;
        leftHandRot = handContact.GlobalRotation;
        handContact.GetParent()?.RemoveChild(handContact);
        newParent.AddChild(handContact);
        handContact.GlobalPosition = leftHandPos;
        handContact.GlobalRotation = leftHandRot;
        if (resetRotation)
        {
            handContact.Rotation = Vector3.Zero;
        }
    }
}
