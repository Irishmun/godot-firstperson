using Godot;
using System;

public partial class WalkingSound : AudioStreamPlayer3D
{
    [Export] private float normalStepInterval = 0.5f;

    private float _normalMovementSpeed;
    private Player _player;
    private float _t;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _player = this.GetParent<Player>();
        _normalMovementSpeed = _player.MovementSpeed;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (_player.CanMove == false)
        { return; }
        if (HandleJump()) { return; }
        if (_player.IsInAir)
        { return; }
        if (HandleLand()) { return; }
        if (HandleGrounded(delta)) { return; }
    }

    /// <summary>Handles sounds when player might land</summary>
    /// <returns>True if sound played</returns>
    private bool HandleLand()
    {
        if (_player.JustLanded == false)
        { return false; }
        PlayStep();
        return true;
    }

    /// <summary>Handles sounds when walking on ground</summary>
    /// <returns>True if sound played</returns>
    private bool HandleGrounded(double delta)
    {
        UpdateWalkTimer(delta);
        if (_t < normalStepInterval)
        { return false; }
        PlayStep();
        return true;
    }
    /// <summary>Handles sounds when jumping</summary>
    /// <returns>True if sound played</returns>
    private bool HandleJump()
    {
        if (_player.StartJumpSound && _t < normalStepInterval - 0.001f)
        {
            _player.StartJumpSound = false;
            PlayStep();
            return true;
        }
        return false;
    }

    private void UpdateWalkTimer(double delta)
    {
        _t += ((float)delta * _player.Velocity.Length()) / _normalMovementSpeed;
    }
    private void PlayStep()
    {
        this.Play();
        _t = 0;
    }
}
