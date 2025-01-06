using Godot;
using System;

public partial class GenericJumpPad : JumpPad
{
    [Export] private float randomIntensity = 1;
    [Export] private bool keepMomentum = true;
    protected override void PlayerEntered(Player body, float force)
    {
        Vector3 ran, vel;
        ran = randomIntensity <= 0 ? Vector3.Zero : GetRandomRotation(randomIntensity);
        GD.Print((GetLocalUp() + ran) * force);
        vel = (GetLocalUp() + ran) * force;
        if (keepMomentum == true)
        {
            vel += body.Velocity * new Vector3(1, 0, 1);
        }
        body.OverrideVelocity(vel);// (GlobalTransform.Basis.Y + ran) * force);
    }

    protected override void RigidBodyEntered(RigidBody3D body, float force)
    {
        body.ApplyImpulse(GetLocalUp() * force);
    }
}
