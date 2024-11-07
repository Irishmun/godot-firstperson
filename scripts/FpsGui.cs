using Godot;

public partial class FpsGui : Label
{
#if DEBUG
    public override void _Process(double delta)
    {
        //Text = $"Draw calls: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame)}";
        Text = $"FPS: {Engine.GetFramesPerSecond()}\n" +
               $"Draw calls: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame)}\n" +
               $"Objects: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalObjectsInFrame)}\n" +
               $"VRAM: {(int)(RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.VideoMemUsed) * 0.000001d)} MB";
    }
#endif

    /*
     * Text = $"FPS: {Engine.GetFramesPerSecond()}\n" +
               $"Draw calls: {Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame)}\n" +
               $"Objects: {Performance.GetMonitor(Performance.Monitor.ObjectCount)}\n" +
               $"VRAM: {(int)(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) * 0.000001d)} MB";
     */
}
