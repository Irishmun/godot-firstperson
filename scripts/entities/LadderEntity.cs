using Godot;
using System;

public partial class LadderEntity : Area3D
{
    [Export] private float interactionAngle = 30f;
    [Export] private Marker3D ladderOffset;
    private Player _player;
    private float _interactionRad;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _interactionRad = Mathf.DegToRad(interactionAngle);
        this.BodyEntered += Ladder_BodyEntered;
        this.BodyExited += Ladder_BodyExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null)
        { return; }
        if (_player.PlayerState != PlayerStates.CLIMBING)
        {
            float angle = _player.GlobalTransform.Basis.Z.AngleTo(-GlobalTransform.Basis.Z);//_player.GlobalTransform.Basis.Z.Dot(GlobalTransform.Basis.Z);
            if (angle < _interactionRad)
            {
                if ((_player.PlayerWishDir.Y) > 0)
                {
                    EnterLadder(_player);
                }
            }
        }
    }

    private void Ladder_BodyExited(Node3D body)
    {
        if (body != _player)
        { return; }
        /*if ((body as Player).PlayerState == PlayerStates.CLIMBING)
        {*/
        ExitLadder();
        /*}
        _player = null;*/
    }

    private void Ladder_BodyEntered(Node3D body)
    {
        if ((body is Player) == false)
        { return; }
        _player = body as Player;
    }

    public void Interact(Player player)
    {
        GD.Print("Climb " + player);
        if (_player == player)
        {
            ExitLadder();
        }
        else
        {
            EnterLadder(player);
        }
    }

    private void EnterLadder(Player player)
    {
        GD.Print("Enter Ladder");
        _player = player;
        _player.GlobalPosition = new Vector3(ladderOffset.GlobalPosition.X, _player.GlobalPosition.Y, ladderOffset.GlobalPosition.Z);
        _player.SetPlayerState(PlayerStates.CLIMBING);
    }

    private void ExitLadder()
    {
        GD.Print("Exit Ladder");
        Player p = _player;
        _player = null;
        p.SetPlayerState(PlayerStates.STANDING, true);
    }
}