using Godot;
using System;

public partial class DebugExit : Node
{
    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("debug_exit"))
        {
            GetViewport().SetInputAsHandled();
            GetTree().Quit();
            return;
        }
    }
}
