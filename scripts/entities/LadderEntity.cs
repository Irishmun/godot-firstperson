using Godot;
using System;

public partial class LadderEntity : Area3D
{
    private const float EXIT_VELOCITY = 5;
    [Export] private float interactionAngle = 30f;
    [Export] private Marker3D ladderOffset;
    private Player _climbingPlayer;
    private float _interactionRad;
    private Node3D _ladderTop;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _interactionRad = Mathf.DegToRad(interactionAngle);
        _ladderTop = this.GetChildWithComponent<Marker3D>(name: "Top");
        this.BodyEntered += Ladder_BodyEntered;
        this.BodyExited += Ladder_BodyExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_climbingPlayer == null)
        { return; }
        if (_climbingPlayer.PlayerState != PlayerStates.CLIMBING)
        {
            float angle = _climbingPlayer.GlobalTransform.Basis.Z.AngleTo(-GlobalTransform.Basis.Z);//_player.GlobalTransform.Basis.Z.Dot(GlobalTransform.Basis.Z);
            if (angle < _interactionRad)
            {
                if ((_climbingPlayer.PlayerWishDir.Y) > 0)
                {
                    EnterLadder(_climbingPlayer);
                }
            }
        }
    }

    public override void _Input(InputEvent e)
    {
        if (_climbingPlayer == null)
        { return; }
        if (e.IsActionPressed(Keys.JUMP))
        {
            _climbingPlayer.SetPlayerState(PlayerStates.STANDING, true);
            Vector3 endVelocity = GlobalTransform.Basis.Z * EXIT_VELOCITY;
            endVelocity.Y += _climbingPlayer.JumpForce;
            _climbingPlayer.OverrideVelocity(endVelocity);
            _climbingPlayer.ForceInputCheck();
            _climbingPlayer = null;
        }
    }

    private void Ladder_BodyExited(Node3D body)
    {
        if (body != _climbingPlayer)
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
        _climbingPlayer = body as Player;
    }

    public void Interact(Player player)
    {
        GD.Print("Climb " + player);
        if (_climbingPlayer == player)
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
        _climbingPlayer = player;
        _climbingPlayer.OverridePosition(new Vector3(ladderOffset.GlobalPosition.X, _climbingPlayer.GlobalPosition.Y, ladderOffset.GlobalPosition.Z));
        _climbingPlayer.SetPlayerState(PlayerStates.CLIMBING);
    }

    private void ExitLadder()
    {
        Player p = _climbingPlayer;
        _climbingPlayer = null;
        if (p.GlobalPosition.Y > _ladderTop.GlobalPosition.Y)
        {
            Vector3 pos = p.GlobalPosition;
            pos += -GlobalTransform.Basis.Z.Normalized() * 0.1f;
            pos.Y = p.GlobalPosition.Y + 0.1f;
            p.OverridePosition(pos);
        }
        p.SetPlayerState(PlayerStates.STANDING, true);
    }
}