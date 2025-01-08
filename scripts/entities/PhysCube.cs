using Godot;
using System;

public partial class PhysCube : RigidBody3D
{

    private AudioStreamPlayer3D _audio;
    private Label3D _label;
    private bool _active = true;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _audio = this.GetChildWithComponent<AudioStreamPlayer3D>();
        _label = this.GetChildWithComponent<Label3D>();
        if (_label != null)
        { _label.Text = $"{this.Mass} KG"; }
        if (_audio != null && _audio.PitchScale == 1)
        {
            _audio.PitchScale = Mathf.Clamp(this.Mass.ReMap(25, 100, 1, 0.4f), 0.4f, 1f);
        }

        this.BodyEntered += PhysCube_BodyEntered;
    }

    private void PhysCube_BodyEntered(Node body)
    {
        if (_active == false || _audio == null || this.LinearVelocity.LengthSquared().IsApproxZero())
        { return; }
        _audio.Play();
    }

    public bool Active { get => _active; set => _active = value; }
}
