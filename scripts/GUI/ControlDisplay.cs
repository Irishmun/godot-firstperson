using Godot;
using System;
using static System.Net.Mime.MediaTypeNames;

public partial class ControlDisplay : Control
{

    [Export] private Control keyboardInputs, controllerInputs;
    private bool _usingController = false;

    public override void _Ready()
    {
        ChangeDisplay();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventJoypadMotion)
        {
            InputEventJoypadMotion m = (InputEventJoypadMotion)e;
            float x, y, dead;
            dead = InputMap.ActionGetDeadzone(Keys.CONTROLLER_LOOK);
            x = Input.GetJoyAxis(m.Device, JoyAxis.RightX);
            y = Input.GetJoyAxis(m.Device, JoyAxis.RightY);
            if (Mathf.Abs(x) > dead || Mathf.Abs(y) > dead)
            {
                _usingController = true;
            }
        }
        else if (e is InputEventJoypadButton)
        {
            _usingController = true;
        }
        else
        { _usingController = false; }
        ChangeDisplay();
    }

    private void ChangeDisplay()
    {
        if (_usingController == true)
        {
            keyboardInputs.Visible = false;
            controllerInputs.Visible = true;
        }
        else
        {
            keyboardInputs.Visible = true;
            controllerInputs.Visible = false;
        }
        //GetTree().CreateTimer(0.5).Timeout += ControlDisplay_Timeout;
    }

    private void ControlDisplay_Timeout()
    {
        ChangeDisplay();
    }
}
