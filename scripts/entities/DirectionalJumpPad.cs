using Godot;
using System;

public partial class DirectionalJumpPad : JumpPad
{
    [Export] private float jumpForward = 5;

    protected override void PlayerEntered(Player body, float force)
    {
        body.OverrideVelocity(GetLocalUp() * force);
        Vector3 vel = new Vector3(body.Velocity.X, 0, body.Velocity.Z);
        vel += GetLocalForward() * CalcForwardImpulse();
        vel.Y = body.Velocity.Y;
        body.OverrideVelocity(vel);
    }

    protected override void RigidBodyEntered(RigidBody3D body, float force)
    {
        body.ApplyCentralImpulse(GetLocalUp() * force);
        body.ApplyImpulse(GetLocalForward() * CalcForwardImpulse(body.Mass));
    }
    private float CalcForwardImpulse(float mass = 1)
    {
        return mass * jumpForward;
    }
}
