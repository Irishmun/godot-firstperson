using Godot;

public partial class FpsGui : Label
{
#if DEBUG
    public override void _Process(double delta)
    {
        //NOTE: some if not all of these values are not available unless the game is in debug mode
        //Text = $"Draw calls: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame)}";
        float horizontal = (Player.Instance.Velocity /** new Vector3(1, 0, 1)*/).Length();
        Vector3 velocity = Player.Instance.Velocity * Player.Instance.Transform.Basis;
        Text = $" FPS: {Engine.GetFramesPerSecond()}\n" +
               $" Draw calls: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame)}\n" +
               $" Objects: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalObjectsInFrame)}\n" +
               $" VRAM: {(int)(RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.VideoMemUsed) * 0.000001d)} MB\n" +
               $" State: {Player.Instance.PlayerState.ToString()}\n" +
               $" FOV: {Player.Instance.FOV.ToString("0.0")}\n" +
               $" Vel: {velocity.ToString("0.000")}({horizontal.ToString("0.000")})\n" +
               $" Pos: {Player.Instance?.GlobalPosition.ToString("0.000")}";
    }
#endif

    /*
     * Text = $"FPS: {Engine.GetFramesPerSecond()}\n" +
               $"Draw calls: {Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)}\n" +
               $"Objects: {Performance.GetMonitor(Performance.Monitor.ObjectCount)}\n" +
               $"VRAM: {(int)(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) * 0.000001d)} MB";
     */
}
