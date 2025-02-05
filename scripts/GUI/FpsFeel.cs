using Godot;
using System;

public partial class FpsFeel : TextureRect
{
    [Export] private int targetFPS = 120;
    [Export] private int icons = 4;
    private int _steps, _stepSize;
    private AtlasTexture _texture;
    private int _textureOffset;

    //   private float _timeSinceAvg = 0;
    //   private int _frames = 0;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _texture = (AtlasTexture)this.Texture;
        GD.Print(_texture);
        _textureOffset = (int)_texture.Region.Size.X;
        GD.Print(_textureOffset);
        _steps = icons - 1;
        GD.Print(_steps);
        _stepSize = targetFPS / _steps;
        GD.Print(_stepSize);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        for (int i = 0; i < _steps; i++)
        {
            if (Engine.GetFramesPerSecond() < _stepSize * (i + 1))
            {
                SetTexturePos(_textureOffset * i);
                return;
            }
        }
        SetTexturePos(_textureOffset * _steps);
    }

    private void SetTexturePos(int offset)
    {
        Rect2 region = _texture.Region;
        region.Position = new Vector2(offset, 0);
        _texture.Region = region;
    }

}
