using Godot;
using System;

public partial class DebugExit : Node
{
    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed(Keys.DEBUG_EXIT))
        {
            Quit();
            return;
        }
        if (e is InputEventJoypadButton && e.IsActionReleased(Keys.PAUSE))
        {
            Quit();
            return;
        }
    }
    private void Quit()
    {
        GetViewport().SetInputAsHandled();
        GetTree().Quit();
    }
}
