using Godot;
#if IMGUI
using ImGuiNET;
#endif
using System;
using static System.Net.Mime.MediaTypeNames;

public partial class DebugGUi : Node
{
    private const int KEYBOARD_WINDOW_HEIGHT = 146;
    private const int TOGGLE_INFO_WINDOW_HEIGHT = 32;
    private const int CONTROLLER_WINDOW_HEIGHT = 146;
    private const string KEYBOARD_INPUTS = "Move: WASD\nLook: Mouse\nCrouch: Control\nRun: Shift\nJump: Space\nQuit: Shift+Escape\nFlashlight: F\nInteract: E\nThrow: Mouse 1\nDrop: Mouse 2";
    private const string CONTROLLER_INPUTS = "Move: Left stick\r\nLook: Right stick\r\nCrouch: B\r\nRun: L3\r\nJump: A\r\nQuit: start\r\nFlashlight: Y\r\nInteract: X\r\nThrow: RT\r\nDrop: LT";
    private const string KEYBOARD_TOGGLE_INFO = "Toggle Info: Z", CONTROLLER_TOGGLE_INFO = "Toggle Info: Select";
    private const byte MAX_VISIBILTY = 2;

    private byte startState = 1;

    private bool _usingController = false;
    private byte _visibilityState = 0;//0:None | 1: Controls only | 2:ALL 
    private System.Numerics.Vector2 _eightByEight = new System.Numerics.Vector2(8, 8);
    public override void _Ready()
    {
        this._visibilityState = startState;
    }
    public override void _Process(double delta)
    {
        //NOTE: some if not all of these values are not available unless the game is in debug mode
        //Text = $"Draw calls: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame)}";
#if IMGUI
        float height = GetViewport().GetVisibleRect().Size.Y;
        switch (_visibilityState)
        {
            case 0:
                ToggleInfo(height);
                break;
            case 1:
                KeyboardController(height);
                ToggleInfo(height);
                break;
            case 2:
                ImGuiDebugInfo();
                KeyboardController(height);
                ToggleInfo(height);
                break;
            default:
                break;
        }
#endif
    }
#if IMGUI
    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionReleased(Keys.DEBUG_TOGGLE_INFO))
        {
            _visibilityState = _visibilityState == MAX_VISIBILTY ? byte.MinValue : (byte)(_visibilityState + 1);
        }
        if (e is InputEventJoypadMotion)
        {
            InputEventJoypadMotion m = (InputEventJoypadMotion)e;
            float x, y, dead;
            dead = InputMap.ActionGetDeadzone(Keys.CONTROLLER_LOOK);
            x = Input.GetJoyAxis(m.Device, JoyAxis.RightX) + Input.GetJoyAxis(m.Device, JoyAxis.LeftX);
            y = Input.GetJoyAxis(m.Device, JoyAxis.RightY) + Input.GetJoyAxis(m.Device, JoyAxis.LeftY);
            if (Mathf.Abs(x) > dead || Mathf.Abs(y) > dead)
            { _usingController = true; }
        }
        else if (e is InputEventJoypadButton)
        {
            _usingController = true;
        }
        else
        { _usingController = false; }
    }

    private void KeyboardController(float height)
    {
        if (_usingController)
        { ImGuiControllerControls(height); }
        else
        { ImGuiKeyboardControls(height); }
    }
    private void ToggleInfo(float height)
    {
        if (_usingController)
        { ImGuiControllerToggleInfo(height); }
        else
        { ImGuiKeyboardToggleInfo(height); }
    }

    private void ImGuiDebugInfo()
    {
        float horizontal = (Player.Instance.Velocity /** new Vector3(1, 0, 1)*/).Length();
        Vector3 velocity = Player.Instance.Velocity * Player.Instance.Transform.Basis;
        string text = $"FPS: {Engine.GetFramesPerSecond()}\n" +
               $"Draw calls: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame)}\n" +
               $"Objects: {RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalObjectsInFrame)}\n" +
               $"VRAM: {(int)(RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.VideoMemUsed) * 0.000001d)} MB\n" +
               $"State: {Player.Instance.PlayerState.ToString()}\n" +
               $"FOV: {Player.Instance.FOV.ToString("0.0")}\n" +
               $"Vel: {velocity.ToString("0.000")}({horizontal.ToString("0.000")})\n" +
               $"Pos: {Player.Instance?.GlobalPosition.ToString("0.000")}";
        ImGui.Begin("Debug Info", ImGuiWindowFlags.NoDecoration);
        ImGui.SetWindowPos(_eightByEight);
        ImGui.Text(text);
        ImGui.End();
    }

    private void ImGuiKeyboardControls(float height)
    {
        ImGui.Begin("Keyboard inputs", ImGuiWindowFlags.NoDecoration);
        ImGui.SetWindowPos(new System.Numerics.Vector2(8, height - KEYBOARD_WINDOW_HEIGHT - TOGGLE_INFO_WINDOW_HEIGHT - 8));
        ImGui.Text(KEYBOARD_INPUTS);
        ImGui.End();
    }

    private void ImGuiControllerControls(float height)
    {
        ImGui.Begin("Controller inputs", ImGuiWindowFlags.NoDecoration);
        ImGui.SetWindowPos(new System.Numerics.Vector2(8, height - CONTROLLER_WINDOW_HEIGHT - TOGGLE_INFO_WINDOW_HEIGHT - 8));
        ImGui.Text(CONTROLLER_INPUTS);
        ImGui.End();
    }

    private void ImGuiKeyboardToggleInfo(float height)
    {
        ImGui.Begin("KeyboardToggleInfo", ImGuiWindowFlags.NoDecoration);
        ImGui.SetWindowPos(new System.Numerics.Vector2(8, height - TOGGLE_INFO_WINDOW_HEIGHT - 8));
        ImGui.Text(KEYBOARD_TOGGLE_INFO);
        ImGui.End();
    }
    private void ImGuiControllerToggleInfo(float height)
    {
        ImGui.Begin("ControllerToggleInfo", ImGuiWindowFlags.NoDecoration);
        ImGui.SetWindowPos(new System.Numerics.Vector2(8, height - TOGGLE_INFO_WINDOW_HEIGHT - 8));
        ImGui.Text(CONTROLLER_TOGGLE_INFO);
        ImGui.End();
    }
#endif
}