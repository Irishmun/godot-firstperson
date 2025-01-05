using Godot;
using System;

public partial class InteractionRay : RayCast3D
{
    [Export] private Node3D followNode;
    public override void _Process(double delta)
    {
        //this prevents it from casting against the player, whilst still following the camera
        this.GlobalPosition = followNode.GlobalPosition;
        this.GlobalRotation = followNode.GlobalRotation;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!this.IsColliding())
        {
            if (CrossHairs.Instance.CurrentRadius == 1)
            { return; }
            if (CrossHairs.Instance.IsTweening)
            { return; }
            CrossHairs.Instance.ChangeRadius(1);
            return;
        }
        if (this.IsColliding())
        {
            if (IsInteractable(this.GetCollider() as Node3D))
            {
                if (CrossHairs.Instance.CurrentRadius == 5)
                { return; }
                if (CrossHairs.Instance.IsTweening)
                { return; }
                CrossHairs.Instance.ChangeRadius(5);
            }
            else
            {
                if (CrossHairs.Instance.CurrentRadius == 1)
                { return; }
                if (CrossHairs.Instance.IsTweening)
                { return; }
                CrossHairs.Instance.ChangeRadius(1);
            }
        }
    }

    private bool IsInteractable(Node3D item)
    {
        return item.GetGroups().Contains("Interactable");
    }
}
