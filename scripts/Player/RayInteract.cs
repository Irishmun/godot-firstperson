using Godot;
using System;

public partial class RayInteract : Node
{
    [Export] private InteractionRay interactRay;
    [Export] private Player player;
    public override void _UnhandledInput(InputEvent e)
    {
        if (!e.IsActionPressed(Keys.USE))
        { return; }
        interactRay.ForceRaycastUpdate();
        if (interactRay.IsColliding() == false)
        { return; }
        GodotObject body = interactRay.GetCollider();
        GD.Print("body: " + body);//provide interacted object body
        TryInteract(body);
    }

    private void TryInteract(GodotObject body)
    {
        if (body.HasMethod("Interact"))
        {
            GD.Print(player);
            body.Call("Interact", player);
            GD.Print("interact");
        }
    }

    private bool CanInteract(GodotObject body)
    {
        return body != null && body.HasMethod("Interact");

    }
    /*
     *   public override void _UnhandledInput(InputEvent e)
    {
        GodotObject body = GetCollider();
        if (body != null && e.IsActionPressed(Keys.Use))
        {
            GD.Print("body: " + body);//provide interacted object body
            TryUse(body);
        }
    }

    private void TryUse(GodotObject body)
    {
        if (body.HasMethod("press"))
        {
            GD.Print("pressed");
            body.Call("press");
        }
        else if (body.HasMethod("use"))
        {
            GD.Print("used");
            body.Call("use");
        }
    }
     */
}
