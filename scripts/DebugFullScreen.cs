using Godot;

public partial class DebugFullScreen : Node
{
    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionReleased(Keys.DEBUG_FULLSCREEN))
        {
            if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed)
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
            }
            else
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            }
        }
    }
}
