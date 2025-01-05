using Godot;
using System;

public abstract partial class JumpPad : Area3D
{
    [Export] private float jumpHeight = 10;
    private AudioStreamPlayer3D jumpSound;

    protected float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    private RandomNumberGenerator _rand;
    protected abstract void PlayerEntered(Player body, float force);
    protected abstract void RigidBodyEntered(RigidBody3D body, float force);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        jumpSound = this.GetChildWithComponent<AudioStreamPlayer3D>();
        this.BodyEntered += JumpPad_BodyEntered;
        _rand = new RandomNumberGenerator();
    }

    private void JumpPad_BodyEntered(Node3D body)
    {
        if ((body is PhysicsBody3D) == false)
        { return; }
        if (body is Player)
        {
            Player bod = (body as Player);
            float force = CalcVerticalImpulse();
            //GD.Print(force);
            PlayerEntered(bod, force);
            //bod.OverrideVelocity(new Vector3(bod.Velocity.X, force, bod.Velocity.Z));
            jumpSound.Play();
            return;
        }
        if (body is RigidBody3D)
        {
            RigidBody3D bod = (body as RigidBody3D);
            float force = CalcVerticalImpulse(bod.Mass, bod.GravityScale);
            RigidBodyEntered(bod, force);
            //bod.ApplyImpulse(Vector3.Up * force);
            jumpSound.Play();
            return;
        }
    }

    private float CalcVerticalImpulse(float mass = 1, float GravityMultiplier = 2)
    {//not realistic, but predictable
        //same as player jump calculation, but with mass added (player mass is "always" 1)
        return Mathf.Sqrt(2 * _gravity * jumpHeight * GravityMultiplier) * mass;
    }

    protected Vector3 GetRandomRotation(float intensity = 1)
    {
        _rand.Randomize();
        float TAU = Mathf.Tau;
        return new Vector3(_rand.RandfRange(-TAU, TAU), _rand.RandfRange(0, TAU), _rand.RandfRange(-TAU, TAU)) * intensity;
    }

    protected Vector3 GetLocalUp()
    {
        return GlobalTransform.Basis.Y / this.Scale;
    }

    protected Vector3 GetLocalForward()
    {
        return GlobalTransform.Basis.X / this.Scale;
    }
}
