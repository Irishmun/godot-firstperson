using Godot;
using ImGuiNET;
using System;
using static System.Net.Mime.MediaTypeNames;

public partial class DebugGUi : Node
{
    [Export] private bool startVisible = true;
    private bool _visible = false;
    public override void _Ready()
    {
        this._visible = startVisible;
    }
    public override void _Process(double delta)
    {
        //NOTE: some if not all of these values are not available unless the game is in debug mode
        //Text = $"Draw calls: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame)}";
        if (_visible == false)
        { return; }
        float horizontal = (Player.Instance.Velocity /** new Vector3(1, 0, 1)*/).Length();
        Vector3 velocity = Player.Instance.Velocity * Player.Instance.Transform.Basis;
        string text = $" FPS: {Engine.GetFramesPerSecond()}\n" +
               $" Draw calls: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame)}\n" +
               $" Objects: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalObjectsInFrame)}\n" +
               $" VRAM: {(int)(RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.VideoMemUsed) * 0.000001d)} MB\n" +
               $" State: {Player.Instance.PlayerState.ToString()}\n" +
               $" FOV: {Player.Instance.FOV.ToString("0.0")}\n" +
               $" Vel: {velocity.ToString("0.000")}({horizontal.ToString("0.000")})\n" +
               $" Pos: {Player.Instance?.GlobalPosition.ToString("0.000")}";
        ImGui.Begin("Debug Info", ImGuiWindowFlags.NoDecoration);
        ImGui.SetWindowPos(System.Numerics.Vector2.Zero);
        ImGui.Text(text);
        ImGui.End();

    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionReleased(Keys.DEBUG_TOGGLE_INFO))
        {
            this._visible = !this._visible;
        }
    }
}
