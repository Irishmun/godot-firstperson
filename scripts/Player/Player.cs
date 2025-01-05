using Godot;
using System;
using static Godot.TextServer;

public partial class Player : CharacterBody3D
{
    public static Player Instance;
    private const string ANIM_CROUCH = "Crouch";
    private const float TERMINAL_VELOCITY = -53.645f;//120mph in m/s according to FAI SKYDIVING COMMISSION
    private const float DPI_MULTIPLIER = 0.00125f;//800 DPI mouse
    private const float FOV_TIME = 0.25f;//800 DPI mouse
    private readonly Vector3 HORIZONTAL = new Vector3(1, 0, 1);
    #region exports
    [ExportGroup("Movement")]
    [Export] private float groundAcceleration = 10;
    [Export] private float airAcceleration = 2.5f;
    //[Export] private float deceleration = 7;
    [Export] private float movementSpeed = 4.8f;
    [Export] private float sprintSpeed = 5.75f;
    [Export] private float crouchSpeed = 3.6f;
    [Export] private float airSpeed = 1.2f;
    [Export] private float climbSpeed = 2f;
    [Export] private float maxStepHeight = 0.25f;
    [ExportGroup("Jumping")]
    [Export] private float jumpHeight = 1f;
    [Export] private int maxJumps = 1;
    [Export] private bool bunnyHop = false;
    //[ExportGroup("Crouching")]
    //[Export] private float crouchHeight = 0.9f;//meters
    [ExportGroup("Camera")]
    [Export(PropertyHint.Range, "1,100,")] private int sensitivity = 40;
    [Export] private float cameraLeanInto = 7.5f;//degrees
    [Export] private float cameraVertSpeed = 20f;
    [Export] private float maxLookUp = 55, maxLookDown = -75;
    [Export] private float fovIncrease = 5;
    [ExportGroup("Physics")]
    [Export] private float GravityMultiplier = 2;
    [Export] private float mass = 80;
    [Export] private float timeBeforeFalling = 0.1f;//coyote time
    [ExportGroup("Buffers")]
    [Export] private float jumpBufferTime = 0.1f;
    [Export] private float mantleBufferTime = 0.1f;
    //[Export] private float friction = 3.5f;

    #endregion
    #region private fields
    private PlayerStates _playerState = PlayerStates.STANDING;
    private bool _canMove = true, _canLook = true;
    private bool _runningInput = false, _crouchInput = false, _jumpInput = false;
    private bool _justLanded = false, _isFalling = false, _wasOnFloor = true;
    private bool _jumpBuffer = false, _mantleBuffer = false;
    private Vector2 _inputDir;//, _camInput;
    private Vector3 _velocity;
    private Vector3 _cameraSavedPos = Vector3.Inf;
    private Tween _tween;
    private int _jumpCount = 0;
    private float _baseFov, _sprintFov;
    private float _jumpForce;
    private float _standingHeight, _defaultCameraHeight;
    private float _crouchVal = 0, _tilt = 0;
    private float _coyoteTime = 0, _fallT = 0;
    //private CylinderShape3D _collision;
    //private CapsuleShape3D _collision;
    //[ExportGroup("Nodes")]
    private Node3D _cameraHolder;
    private Camera3D _camera;
    private ShapeCast3D _headRay;
    //private CollisionShape3D _collisionBody;
    private Mantling _mantling;
    private AnimationPlayer _animator;
    #endregion
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    //==========GAME==========
    #region GAME
    public override void _Ready()
    {
        Instance = this;
        Input.UseAccumulatedInput = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GetNodes();
        //_collision = (CapsuleShape3D)collisionBody.Shape;
        //_standingHeight = _collision.Height;
        _defaultCameraHeight = _cameraHolder.Position.Y;//local, for global: camera.GlobalPosition.Y-this.GlobalPosition.Y
        _baseFov = _camera.Fov;
        _sprintFov = _baseFov + fovIncrease;
        _jumpForce = CalcJumpForce();
        /*Vector3 pos = _headRay.Position;
        pos.Y = crouchHeight;
        _headRay.Position = pos;*/
    }
    public override void _Process(double delta)
    {
        //always fix camera roll
        //FixCameraRoll(delta);
        //_camInput = Vector2.Zero;
        //allways put player's camera to origin
        if (!Vector3.Inf.IsEqualApprox(_cameraSavedPos))
        {
            Vector3 pos = _camera.GlobalPosition;
            //float holderY = cameraHolder.GlobalPosition.Y;
            _cameraSavedPos = _cameraSavedPos.Lerp(_cameraHolder.GlobalPosition, (float)delta * cameraVertSpeed);
            pos = _cameraSavedPos;
            _camera.GlobalPosition = pos;
            //if (_cameraSavedVert == holderY)
            //{ _cameraSavedVert = float.NegativeInfinity; }
            //if (_cameraSavedVert < holderY + 0.0001f && _cameraSavedVert > holderY - 0.0001f)
            //{ _cameraSavedVert = holderY; }
        }
        //_camera.Fov = Mathf.Clamp(ReMap((Velocity * HORIZONTAL).Length(), movementSpeed, movementSpeed * sprintMultiplier, minFOV, minFOV * sprintMultiplier), minFOV, minFOV * sprintMultiplier);

        if (_canMove == false)
        { return; }
        if (IsOnFloor())
        {//TODO: possible duplicate in gravity
            if (_isFalling == true)
            { _justLanded = true; }
            StartJumpSound = false;
            _jumpCount = 0;
            _isFalling = false;
            _coyoteTime = 0;
            _fallT = 0;
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        _justLanded = false;
        //always adjust crouch state, might have been set when the player couldn't move
        if (_canMove == false)
        { return; }
        _velocity = this.Velocity;
        Vector3 wishDir = new Vector3(_inputDir.X, 0, _inputDir.Y);
        //GD.Print($"pre move velocity: {Velocity}");
        if (_playerState == PlayerStates.CLIMBING)
        { HandleClimbingMovement((float)delta, wishDir); }
        else if (_playerState == PlayerStates.ZIPLINING)
        { HandleZipliningMovement((float)delta, wishDir); }
        else
        {
            wishDir = wishDir.Normalized();
            wishDir = wishDir.Rotated(Vector3.Up, GlobalRotation.Y);
            HandleGroundedMovement((float)delta, wishDir);
        }
        HandleFOV();
    }
    #endregion
    //==========INPUT==========
    #region INPUT
    public override void _Input(InputEvent e)
    {
        //update input values if not an "Echo" AND if the player can move
        if (_canMove == false)
        {//player can't move, clear all inputs, allways
            _inputDir = Vector2.Zero;
            _runningInput = false;
            _crouchInput = false;
            return;
        }
        ForceInputCheck();
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if (_canLook == true && (e is InputEventMouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured))
        {
            //adjust mouse input to make game size irrelevant
            Transform2D viewportTransform = GetTree().Root.GetFinalTransform();
            //maybe use this instead? (e as InputEventMouseMotion).ScreenRelative;
            RotatePlayer(e.XformedBy(viewportTransform) as InputEventMouseMotion);
            GetViewport().SetInputAsHandled();//eat input
        }
        /*if (_handleInputs == false)
        { return; }*/
    }
    #endregion
    //==========VIEW==========
    #region VIEW
    private void RotatePlayer(InputEventMouseMotion rotation)
    {
        Vector2 motion = rotation.Relative;
        motion *= DPI_MULTIPLIER * sensitivity;//sensitivity based on mouse DPI (make a setting in game?)
        addPitch(motion.Y);
        addYaw(motion.X);
        clampPitch();
        //TODO: fix this
        //if (Velocity.LengthSquared() > movementSpeed * movementSpeed)
        //{
        //    _tilt = ReMap(yDelta, -30, 30, -cameraLeanInto, cameraLeanInto);
        //    _tilt = Mathf.Clamp(_tilt, -cameraLeanInto, cameraLeanInto);
        //    if (motion != Vector2.Zero)
        //    { TiltCamera(-_tilt); }
        //    GD.Print($"yDelta: {yDelta}| tilt: {_tilt}");
        //    motion = Vector2.Zero;
        //}

        void addPitch(float X)
        {
            if (X.IsApproxZero())
            { return; }
            _cameraHolder.RotateObjectLocal(Vector3.Left, Mathf.DegToRad(X));
            _cameraHolder.Orthonormalize();
        }
        void addYaw(float Y)
        {
            if (Y.IsApproxZero())
            { return; }
            this.RotateObjectLocal(Vector3.Down, Mathf.DegToRad(Y));
            this.Orthonormalize();
        }
        void clampPitch()
        {//clamp rotations to max values
            float lookDown, lookUp;
            lookDown = Mathf.DegToRad(maxLookDown);
            lookUp = Mathf.DegToRad(maxLookUp);
            if (_cameraHolder.Rotation.X > lookDown && _cameraHolder.Rotation.X < lookUp)
            { return; }
            Vector3 rot = _cameraHolder.Rotation;
            rot.X = Mathf.Clamp(rot.X, lookDown, lookUp);
            _cameraHolder.Rotation = rot;
            _cameraHolder.Orthonormalize();
        }

    }
    private void TiltCamera(float tiltDegrees)
    {
        Vector3 rot = _cameraHolder.RotationDegrees;
        rot.Z = tiltDegrees;
        _cameraHolder.RotationDegrees = rot;
    }
    private void FixCameraRoll(double delta)
    {
        if (_cameraHolder.RotationDegrees.Z != 0)
        {
            float tilt = _cameraHolder.RotationDegrees.Z;
            tilt = Mathf.Lerp(tilt, 0, (float)delta * 20);
            if (tilt < 0.0001f && tilt > -0.0001f)
            { tilt = 0; }
            TiltCamera(-tilt);
        }
    }
    private void HandleFOV()
    {
        if (_canMove == false)
        { return; }
        if (_playerState == PlayerStates.CLIMBING)
        {
            if (_camera.Fov == _sprintFov)
            { TweenFOV(_baseFov); }
            return;
        }
        if (_camera.Fov == _sprintFov)
        {//should be walking
            if (_wasOnFloor && IsOnFloor() &&
                (GetMoveSpeed() <= movementSpeed || _inputDir.Y.IsApproxZero()))
            {
                TweenFOV(_baseFov);
            }
        }
        else if (_camera.Fov == _baseFov)
        {//should be running/moving fast
            float sprintSquared = sprintSpeed * sprintSpeed;
            if ((GetMoveSpeed() > movementSpeed) ||//&& _inputDir.Y > 0) ||
                (this.Velocity * HORIZONTAL).LengthSquared() >= sprintSquared ||
                this.Velocity.Y > _jumpForce)
            {
                TweenFOV(_sprintFov);
            }
        }

        /* //TODO: make this work correctly >:(
         float normalVelocity = new Vector3(_velocity.X, _jumpForce, _velocity.Z).LengthSquared();
         //GD.Print($"_velocity:{_velocity.Length()} Funky:{new Vector3(_velocity.X, _jumpForce, _velocity.Z).Length()}");
         if (!_crouchInput && ((_runningInput && (_velocity * HORIZONTAL).LengthSquared() > movementSpeed * movementSpeed)
             || ((!IsOnFloor()) && _velocity.LengthSquared() > normalVelocity))
             && !_camera.Fov.Equals(_sprintFov))
         //&& (_velocity * HORIZONTAL).Length() > movementSpeed)
         {
             //GD.Print($"(running & moving:{_runningInput && (_velocity * HORIZONTAL).LengthSquared() > 0} || " +
             //    $"!onfloor & moving fast:{(!IsOnFloor() && _velocity.Length() > normalVelocity)}) && " +
             //    $"fov low:{!_camera.Fov.Equals(_sprintFov)}({_camera.Fov})");
             TweenFOV(_sprintFov);
         }
         else if ((_crouchInput || (!_runningInput && IsOnFloor()) || _inputDir == Vector2.Zero)
                 && !_camera.Fov.Equals(_baseFov))
         {
             //GD.Print($"(crouching:{_crouchInput} || not running on floor {!_runningInput && IsOnFloor()}) || no input:{_inputDir == Vector2.Zero})" +
             //    $" && fov high:{!_camera.Fov.Equals(_baseFov)}");
             TweenFOV(_baseFov);
         }*/
    }
    private void TweenFOV(float fov)
    {
        if (_tween != null && _tween.IsRunning() && _camera.Fov.Equals(fov))
        { return; }
        //GD.Print("tweening to:" + fov);
        _tween = GetTree().CreateTween();
        _tween.SetEase(Tween.EaseType.Out);
        _tween.SetTrans(Tween.TransitionType.Linear);
        _tween.Finished += Tween_Finished;
        //_tween.TweenProperty(this, "dotRadius", radius, time);
        _tween.TweenProperty(_camera, "fov", fov, FOV_TIME);
    }
    #endregion
    //==========MOVEMENT==========
    #region MOVEMENT
    private void HandleGroundedMovement(float delta, Vector3 wishDir)
    {
        //if vertical velocity was changed externally or after last frame's move
        //go to air instead
        if (IsOnFloor() && _velocity.Y <= 0)
        {
            HandleMovement((float)delta, wishDir);
        }
        else
        {
            HandleAirMovement((float)delta, wishDir);
            HandleGravity((float)delta);
        }
        HandleJump();
        //GD.Print("Velocity: " + _velocity.Length());
        //check steps
        HandleRigidBodies();
        this.Velocity = _velocity;
        if (!IsOnFloor() || !HandleStep((float)delta))
        {
            MoveAndSlide();
        }
        //GD.Print($"post move velocity: {Velocity}");

        _wasOnFloor = IsOnFloor();
        //GD.Print("[physics]velocity:" + Velocity.ToString("0.0"));
    }
    private void HandleClimbingMovement(float delta, Vector3 wishDir)
    {
        _velocity = Vector3.Zero;
        _velocity.Y = wishDir.Z * climbSpeed;
        this.Velocity = _velocity;
        if (_velocity.Y < 0 && (MoveAndCollide(_velocity * delta, true) != null || IsOnFloor()))
        {
            _playerState = PlayerStates.STANDING;
        }
        else
        {
            MoveAndSlide();
        }

        _wasOnFloor = true;
    }
    private void HandleZipliningMovement(float delta, Vector3 direction)
    {
        _wasOnFloor = true;
    }
    private void HandleMovement(float delta, Vector3 direction)
    {
        //multiply input direction by speed, keep whatever vertical velocity we have
        direction *= GetMoveSpeed();
        //velocity affecting movement
        Vector3 addSpeed = direction - _velocity;
        addSpeed *= delta * groundAcceleration;
        //addSpeed *= HORIZONTAL;//don't need this due to direction and velocity having no Y
        _velocity += addSpeed;
    }
    private void HandleAirMovement(float delta, Vector3 wishDir)
    {
        if (wishDir * HORIZONTAL == Vector3.Zero || _velocity.Dot(wishDir) > (wishDir * GetMoveSpeed()).Length())
        { return; }
        //wishDir *= GetMoveSpeed();
        //velocity affecting movement
        Vector3 addSpeed = (wishDir * GetMoveSpeed()) - _velocity;
        addSpeed *= delta * airAcceleration;
        addSpeed *= HORIZONTAL;
        _velocity += addSpeed;
    }
    private void HandleJump()
    {
        _jumpInput = bunnyHop ? Input.IsActionPressed(Keys.JUMP) : Input.IsActionJustPressed(Keys.JUMP);
        //GD.Print($"Jump:{_jumpInput} and onfloor:{IsOnFloor()} or falling:{_isFalling}");
        if ((_jumpInput || _jumpBuffer) && (IsOnFloor() || !_isFalling))//(IsOnFloor() || (!_wasOnFloor && !_isFalling)))
        {//check if we can jump any more than we already have
            if (_jumpCount >= maxJumps)
            { return; }
            if (_jumpCount == 0)//make sound only on first jump
            { StartJumpSound = true; }
            _jumpBuffer = false;
            _jumpCount += 1;
            _velocity.Y = _jumpForce;//CalcJumpForce();
            //GD.Print(_velocity.Y);
        }
        else if (_jumpInput && (!IsOnFloor() || _isFalling))
        {//buffer jump for if the player pressed the jump button before landing
            _jumpBuffer = true;
            GetTree().CreateTimer(jumpBufferTime).Timeout += JumpBuffer_Timeout;
        }
        if (_jumpInput)
        {
            _mantleBuffer = true;
            GetTree().CreateTimer(mantleBufferTime).Timeout += MantleBuffer_Timeout;
        }
    }
    /// <summary>Handles stepping up steps (stairs)</summary>
    /// <param name="delta">delta time</param>
    /// <returns>false if it can't step up the blockade, true otherwise</returns>
    private bool HandleStep(float delta) //velocity with minStep distance maybe?
    {
        //get where the player would have gone horizontaly        
        Vector3 expectedMoveMotion = this.Velocity * new Vector3(1, 0, 1) * delta;
        //get that position but above the possible step (double it for clearance just in case it's right on the edge)
        Transform3D testStartPos = this.GlobalTransform.Translated(expectedMoveMotion + new Vector3(0, maxStepHeight + 0.01f, 0));//*2 for clearance
        KinematicCollision3D res = new KinematicCollision3D();

        //check if the player would hit something, if so, check if it's an environment collider
        if (this.TestMove(testStartPos, new Vector3(0, -(maxStepHeight + 0.01f), 0), res) &&
            (res.GetCollider() is StaticBody3D || res.GetCollider() is CsgShape3D))
        {
            //check how high the collision point (step) is
            float stepHeight = (testStartPos.Origin + res.GetTravel() - this.GlobalPosition).Y;
            stepHeight = stepHeight.RoundToDec(3);
            //decimal decimalValue = Math.Round((decimal)stepHeight, 2);
            //if distance to step too high,        too low,       or step to high, don't move player there
            if (stepHeight > maxStepHeight || stepHeight <= 0.01f || (res.GetPosition() - this.GlobalPosition).Y > maxStepHeight)
            { return false; }
            Vector3 collisionPos = testStartPos.Origin + res.GetTravel();
            //GD.Print($"({Mathf.Acos(res.GetNormal().Dot(Vector3.Up)) >= this.FloorMaxAngle})Trying to walk up something at angle: {Mathf.RadToDeg(Mathf.Acos(res.GetNormal().Dot(Vector3.Up)))} ({Mathf.Acos(res.GetNormal().Dot(Vector3.Up))})");
            if (Mathf.Acos(res.GetNormal().Dot(Vector3.Up)) >= this.FloorMaxAngle)//<--NOTE: this breaks when using a cylinder collider
            { return false; }
            //set camera position before global position
            _cameraSavedPos = _camera.GlobalPosition;
            //set global position
            this.GlobalPosition = collisionPos;
            ApplyFloorSnap();//snap to floor to prevent floating
            return true;
        }
        return false;
    }
    private void HandleGravity(float delta)
    {
        if (IsOnFloor())
        { return; }
        //changing vertical, start for camera lerp and apply gravity
        //GD.Print($"velocity.Y <=0:{velocity.Y <= 0} |isFalling:{_isFalling}");
        if (_isFalling == false)
        {
            if (_coyoteTime >= timeBeforeFalling)
            { _isFalling = true; }
            else
            { _isFalling = false; }
            _coyoteTime += delta;
        }
        _cameraSavedPos = _camera.GlobalPosition;
        //_velocity.Y -= _gravity * Mass * delta;
        _velocity.Y -= _gravity * GravityMultiplier * delta;
        _velocity.Y = Mathf.Clamp(_velocity.Y, TERMINAL_VELOCITY, -TERMINAL_VELOCITY);
        if ((Input.IsActionPressed(Keys.JUMP) || _mantleBuffer) && (_velocity * HORIZONTAL).LengthSquared() > 0)
        {
            if (_mantling.HandleMantle())
            {
                _cameraSavedPos = _camera.GlobalPosition;
                _mantleBuffer = false;
            }
        }
        ApplyFloorSnap();//snap to floor to prevent floating
    }
    private void HandleCollisionInteraction(float delta)
    {
        KinematicCollision3D kin = MoveAndCollide(Velocity * delta, true);
        if (kin == null)
        { return; }
        GodotObject body = kin.GetCollider();
        if (body.HasMethod("OnCollide"))
        {
            body.Call("OnCollide", this, kin);
        }
    }
    private void ChangeCrouchState(bool newState)
    {
        //GD.Print($"newState:{newState} |playerState:{_playerState}");
        //change crouch based on newState OR if ForceCrouch is true
        if (_playerState != PlayerStates.CROUCHING && newState == true)
        {
            _animator.Play(ANIM_CROUCH, -1, crouchSpeed);
            _playerState = PlayerStates.CROUCHING;
        }
        else if (_playerState == PlayerStates.CROUCHING && newState == false)
        {
            _headRay.Enabled = true;
            _headRay.ForceShapecastUpdate();
            _headRay.Enabled = false;
            if (_headRay.GetCollisionCount() > 0)
            {//something's in the way, check if it should stop the player
                GodotObject hit = _headRay.GetCollider(0);
                if (hit is StaticBody3D || hit is CsgShape3D)//hit blocking
                { return; }
            }
            _animator.Play(ANIM_CROUCH, -1, -crouchSpeed, true);
            _playerState = PlayerStates.STANDING;
        }
    }
    private float GetMoveSpeed()
    {//decide movement speed based on if we're crouching, running or walking
        if (_playerState == PlayerStates.CROUCHING)
        { return crouchSpeed; }
        if (_runningInput && _inputDir.Y > 0)
        { return sprintSpeed; }
        return movementSpeed;
    }
    private void HandleRigidBodies()
    {
        for (int i = 0; i < this.GetSlideCollisionCount(); i++)
        {
            KinematicCollision3D col = this.GetSlideCollision(i);
            if ((col.GetCollider() is RigidBody3D) == false)
            { continue; }
            RigidBody3D collider = col.GetCollider() as RigidBody3D;
            Vector3 pushDir = -col.GetNormal();
            float pushVelocity = _velocity.Dot(pushDir) - collider.LinearVelocity.Dot(pushDir);
            pushVelocity = pushVelocity < 0 ? 0 : pushVelocity;
            float massRatio = Mass / collider.Mass;
            massRatio = massRatio > 1 ? 1 : massRatio;
            pushDir.Y = 0;
            float force = massRatio * 5;
            collider.ApplyImpulse(pushDir * pushVelocity * force, col.GetPosition() - collider.GlobalPosition);
        }
    }
    #endregion
    //==========PRIVATE METHODS==========
    #region PRIVATE METHODS
    private void GetNodes()
    {
        //_collisionBody = GetChildWithComponent<CollisionShape3D>();
        //_collision = (CylinderShape3D)_collisionBody.Shape;
        _cameraHolder = this.GetChildWithComponent<Node3D>(name: "CameraHolder");
        _camera = this.GetChildWithComponent<Camera3D>(_cameraHolder);
        _headRay = this.GetChildWithComponent<ShapeCast3D>();
        _mantling = this.GetChildWithComponent<Mantling>();
        _animator = this.GetChildWithComponent<AnimationPlayer>();
    }
    private float CalcJumpForce()
    {//initial_velocity^2 =  final_velocity^2 - 2*acceleration*displacement
     //Sqrt(2*Gravity*JumpHeight*Mass);//account for gravity applied to player
        float height = _playerState == PlayerStates.CROUCHING ? jumpHeight * 0.5f + 0.1f : jumpHeight + 0.1f;
        return Mathf.Sqrt(2 * _gravity * height * GravityMultiplier);
    }
    #endregion
    //==========PUBLIC METHODS==========
    #region PUBLIC METHODS
    /// <summary>Forcibly enable Crouch state</summary>
    public void ForceCrouch() => ChangeCrouchState(true);
    /// <summary>Overrides player's global position</summary>
    /// <param name="position">new position in Global space</param>
    public void OverridePosition(Vector3 position)
    {
        this.GlobalPosition = position;
    }
    /// <summary>Overrides player's global rotation</summary>
    /// <param name="rotation"></param>
    public void OverrideRotation(Vector3 rotation)
    {
        this.GlobalRotation = rotation;
    }
    /// <summary>Sets the player's velocity value</summary>
    /// <param name="velocity">new velocity value</param>
    public void OverrideVelocity(Vector3 velocity)
    {
        this.Velocity = velocity;
    }
    /// <summary>Overrides the current player state to the given state</summary>
    public void SetPlayerState(PlayerStates state, bool suggestion = false)
    {
        if (suggestion == false)
        {
            _playerState = state;
            return;
        }
        if (_crouchInput == true && state == PlayerStates.STANDING)
        {
            _playerState = PlayerStates.CROUCHING;
        }
        else
        {
            _playerState = state;
        }
    }
    /// <summary>Forcibly updates input values (direction, running, crouching)</summary>
    public void ForceInputCheck()
    {
        _inputDir = Input.GetVector(Keys.RIGHT, Keys.LEFT, Keys.BACKWARD, Keys.FORWARD);
        _runningInput = Input.IsActionPressed(Keys.RUN);
        _crouchInput = Input.IsActionPressed(Keys.CROUCH);
        ChangeCrouchState(_crouchInput);
    }
    #endregion
    //==========EVENTS==========
    #region EVENTS
    private void JumpBuffer_Timeout() => _jumpBuffer = false;
    private void MantleBuffer_Timeout() => _mantleBuffer = false;
    private void Tween_Finished()
    {
        _tween?.Kill();
        _tween = null;
    }
    #endregion
    #region Properties
    public float Mass => mass;
    public float MovementSpeed => movementSpeed;
    public float CrouchHeight => 0.9f;//get from animation somehow
    public float StandingHeight => 1.85f;
    public float FOV => _camera.Fov;
    public Vector3 HorizontalVelocity => Velocity * HORIZONTAL;
    public bool IsCrouching => _playerState == PlayerStates.CROUCHING;
    public bool JustLanded => _justLanded;
    public bool IsInAir => _isFalling || Velocity.Y > 0;
    public bool StartJumpSound { get; set; }
    public bool CanMove { get => _canMove; set => _canMove = value; }
    public PlayerStates PlayerState { get => _playerState; set => _playerState = value; }
    public Vector2 PlayerWishDir => _inputDir;
    #endregion
}

#region QUAKE MOVEMENT
/*
    [ExportGroup("Debug")]
    [Export] private bool useQuakeMovement = false;
    [ExportGroup("Quake")]
    [Export] private float friction = 6;
    [Export] private float quakeSpeed = 7;
    [Export] private float groundAccel = 14;
    [Export] private float groundDeAccel = 10;
    [Export] private float airAccel = 2;
    //QUAKE
    //private Vector3 wishDir = Vector3.Zero; -> wishDir
    //var playerVelocity = Vector3.ZERO -> _velocity
    private bool wishJump = false;
    //END QUAKE 

    private void QuakeGroundMovement(float delta, Vector3 direction)
    {
        QuakeFriction(delta);
        QuakeAccelerate(delta, direction, direction.LengthSquared() * GetMoveSpeed(), groundAccel);
    
        if (wishJump)
        {
            _velocity.Y = _jumpForce;//CalcJumpForce();
        }
    }
    private void QuakeAirMovement(float delta, Vector3 direction)
    {
        QuakeAccelerate(delta, direction, direction.LengthSquared() * GetMoveSpeed(), airAccel);
        HandleGravity(delta);
        /*
        float wishSpeed = GetMoveSpeed();
        float curSpeed, cappedSpeed, addSpeed;
    
        curSpeed = _velocity.Dot(direction);//(_velocity * HORIZONTAL).Length();
        cappedSpeed = Mathf.Min((wishSpeed * direction).Length(), movementSpeed);
        addSpeed = cappedSpeed - curSpeed;
        if (addSpeed <= 0)
        { return; }
        float accel = groundAcceleration * wishSpeed * delta;
        if (accel > addSpeed)
        { accel = addSpeed; }
        _velocity += accel * direction;
    }
    private void QuakeFriction(float delta)
    {
        float drop, lastSpeed, control, newSpeed;
        Vector3 vecCopy = _velocity;
        vecCopy.Y = 0;
        lastSpeed = vecCopy.Length();
    
        control = lastSpeed;
        if (lastSpeed < groundDeAccel)
        { control = groundDeAccel; }
        drop = control * friction * delta;
    
        newSpeed = lastSpeed - drop;
        if (newSpeed < 0)
        { newSpeed = 0; }
        if (lastSpeed > 0)
        { newSpeed /= lastSpeed; }
        _velocity.X *= newSpeed;
        _velocity.Z *= newSpeed;
    }
    private void QuakeAccelerate(float delta, Vector3 wishDir, float wishSpeed, float accel)
    {
        float currentSpeed, addSpeed, accelSpeed;
        currentSpeed = _velocity.Dot(wishDir);
        addSpeed = wishSpeed - currentSpeed;
        GD.Print(addSpeed);
        if (addSpeed <= 0)
        { return; }
        accelSpeed = accel * delta * wishSpeed;
        if (accelSpeed > accel)
        { accelSpeed = addSpeed; }
        _velocity += accelSpeed * wishDir;
    }
*/
#endregion