using Godot;
using System;

public partial class DirectionalJumpPad : Area3D
{
    [Export] private float jumpHeight = 10;
    [Export] private float jumpForward = 5;
    private AudioStreamPlayer3D jumpSound;

    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        jumpSound = GetChildWithComponent<AudioStreamPlayer3D>();
        this.BodyEntered += GenericJumpPad_BodyEntered;
    }

    private void GenericJumpPad_BodyEntered(Node3D body)
    {
        if ((body is PhysicsBody3D) == false)
        { return; }
        if (body is Player)
        {
            Player bod = (body as Player);
            float force = CalcVerticalImpulse();
            bod.OverrideVelocity(new Vector3(bod.Velocity.X, force, bod.Velocity.Z));
            Vector3 vel = new Vector3(bod.Velocity.X, 0, bod.Velocity.Z);
            vel += GlobalTransform.Basis.X * CalcForwardImpulse();
            vel.Y = bod.Velocity.Y;
            GD.Print("Overide velocity:" + vel);
            bod.OverrideVelocity(vel);
            jumpSound.Play();
            return;
        }
        if (body is RigidBody3D)
        {
            RigidBody3D bod = (body as RigidBody3D);
            float force = CalcVerticalImpulse(bod.Mass, bod.GravityScale);
            bod.ApplyCentralImpulse(Vector3.Up * force);
            bod.ApplyImpulse(GlobalTransform.Basis.X * CalcForwardImpulse(bod.Mass));
            jumpSound.Play();
            return;
        }
    }

    private float CalcVerticalImpulse(float mass = 1, float GravityMultiplier = 2)
    {//initial_velocity^2 =  final_velocity^2 - 2*acceleration*displacement
        //Sqrt(2*Gravity*JumpHeight*Mass);//account for gravity applied to player
        return Mathf.Sqrt(2 * _gravity * jumpHeight * GravityMultiplier) * mass;
    }
    private float CalcForwardImpulse(float mass = 1)
    {
        return mass * jumpForward;
    }

    private T GetChildWithComponent<T>(Node parent = null, string name = "") where T : class
    {//parent would be "this Node parent" instead
        parent ??= this;
        foreach (Node item in parent.GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(name) && !item.Name.Equals(name))
            { continue; }
            if (item is T)
            { return item as T; }
        }
        return null;
    }
}
