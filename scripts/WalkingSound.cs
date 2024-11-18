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
        if (_player.StartJump && _t < normalStepInterval - 0.001f)
        {
            playStep();
            return;
        }
        if (_player.IsInAir && !_player.WasOnFloor)
        { return; }
        _t += ((float)delta * _player.Velocity.Length()) / _normalMovementSpeed;
        if (_player.JustLanded == true || _t >= normalStepInterval)
        { playStep(); }
        void playStep()
        {
            this.Play();
            _t = 0;
        }
    }
}
