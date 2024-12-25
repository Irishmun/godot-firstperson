using Godot;
using System;

public partial class GenericJumpPad : Area3D
{
    [Export] private float jumpHeight = 10;
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
            jumpSound.Play();
            return;
        }
        if (body is RigidBody3D)
        {
            RigidBody3D bod = (body as RigidBody3D);
            float force = CalcVerticalImpulse(bod.Mass, bod.GravityScale);
            bod.ApplyImpulse(Vector3.Up * force);
            jumpSound.Play();
            return;
        }
    }

    private float CalcVerticalImpulse(float mass = 1, float GravityMultiplier = 2)
    {//not realistic, but predictable
        //same as player jump calculation, but with mass added (player mass is "always" 1)
        return Mathf.Sqrt(2 * _gravity * jumpHeight * GravityMultiplier) * mass;
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
