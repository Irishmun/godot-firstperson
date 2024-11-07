using Godot;
using System;

public partial class FlashLight : SpotLight3D
{
    [Export] private bool startEnabled = false;
    [Export] private Node3D followTarget;
    [Export] private float rotationSpeed = 1;

    private bool _enabled;
    private float _activeStrenght;

    public override void _Ready()
    {
        this.TopLevel = true;
        _activeStrenght = this.LightEnergy;
        SetEnabled(startEnabled);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        GlobalPosition = followTarget.GlobalPosition;
        Vector3 rot = GlobalRotation;
        rot.X = (float)Mathf.LerpAngle(rot.X, followTarget.GlobalRotation.X, delta * rotationSpeed);
        rot.Y = (float)Mathf.LerpAngle(rot.Y, followTarget.GlobalRotation.Y, delta * rotationSpeed);
        rot.Z = (float)Mathf.LerpAngle(rot.Z, followTarget.GlobalRotation.Z, delta * rotationSpeed);
        GlobalRotation = rot;// GlobalRotation.Lerp(followTarget.GlobalRotation, (float)delta * rotationSpeed);
    }

    public override void _Input(InputEvent e)
    {
        if (Input.IsActionJustPressed("ToggleFlashlight"))
        { SetEnabled(!_enabled); }
    }

    private void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        this.LightEnergy = enabled ? _activeStrenght : 0;
    }
}
