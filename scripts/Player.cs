using Godot;
using System;
using System.Runtime.InteropServices;

public partial class Player : CharacterBody3D
{
    private const float TERMINAL_VELOCITY = -53.645f;//120mph in m/s according to FAI SKYDIVING COMMISSION
    private const float MAX_SLOPE_ANGLE = 60f;
    private const float DPI_MULTIPLIER = 0.001f;//1000 DPI mouse

    #region exports
    [ExportGroup("Movement")]
    [Export] private float acceleration = 10;
    //[Export] private float deceleration = 7;
    [Export] private float movementSpeed = 3.6f;
    //[Export] private float airSpeed = 3f;
    [Export] private float sprintMultiplier = 2;
    [Export] private float maxStepHeight = 0.25f;
    [ExportGroup("Jumping")]
    [Export] private float jumpHeight = 1f;
    [Export] private int maxJumps = 1;
    [ExportGroup("Crouching")]
    [Export] private float crouchMultiplier = 0.75f;
    [Export] private float crouchChangeSpeed = 10;
    [Export] private float crouchHeight = 0.9f;//meters
    [ExportGroup("Camera")]
    [Export(PropertyHint.Range, "1,100,")] private int sensitivity = 40;
    [Export] private float cameraLeanInto = 7.5f;//degrees
    [Export] private float cameraVertSpeed = 20f;
    [Export] private float maxLookUp = 55, maxLookDown = -75;
    [ExportGroup("Physics")]
    [Export] private float Mass = 10;
    [Export] private float timeBeforeFalling = 0.1f;//coyote time
    [Export] private float jumpBufferTime = 0.1f;
    [Export] private Curve fallGravityCurve;
    //[Export] private float friction = 3.5f;
    [ExportGroup("Nodes")]
    [Export] private Node3D cameraHolder;
    [Export] private Camera3D camera;
    [Export] private RayCast3D headRay;
    [Export] private CollisionShape3D collisionBody;
    [Export] private Mantling mantlingNode;
    #endregion
    #region private fields
    private bool _canMove = true, _canLook = true;
    private bool _runningInput = false, _crouchInput = false, _jumpInput = false;
    private bool _isCrouching;
    private bool _justLanded = false, _isFalling = false, _wasOnFloor = true;
    private bool _jumpBuffer = false;
    private Vector2 _inputDir;//, _camInput;
    private Vector3 _velocity;
    private Vector3 _cameraSavedPos = Vector3.Inf;
    private int _jumpCount = 0;
    private float _standingHeight, _defaultCameraHeight;
    private float _crouchVal = 0, _tilt = 0;
    private float _coyoteTime = 0, _fallT = 0;
    private CylinderShape3D _collision;
    //private CapsuleShape3D _collision;
    #endregion
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    //==========GAME==========
    #region GAME
    public override void _Ready()
    {
        Input.UseAccumulatedInput = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _collision = (CylinderShape3D)collisionBody.Shape;
        //_collision = (CapsuleShape3D)collisionBody.Shape;
        _standingHeight = _collision.Height;
        _defaultCameraHeight = cameraHolder.Position.Y;//local, for global: camera.GlobalPosition.Y-this.GlobalPosition.Y
        Vector3 pos = headRay.Position;
        pos.Y = crouchHeight;
        headRay.Position = pos;
    }

    public override void _Process(double delta)
    {
        FixCameraRoll(delta);
        //_camInput = Vector2.Zero;
        if (!Vector3.Inf.IsEqualApprox(_cameraSavedPos))
        {
            Vector3 pos = camera.GlobalPosition;
            //float holderY = cameraHolder.GlobalPosition.Y;
            _cameraSavedPos = _cameraSavedPos.Lerp(cameraHolder.GlobalPosition, (float)delta * cameraVertSpeed);
            pos = _cameraSavedPos;
            camera.GlobalPosition = pos;
            //if (_cameraSavedVert == holderY)
            //{ _cameraSavedVert = float.NegativeInfinity; }
            //if (_cameraSavedVert < holderY + 0.0001f && _cameraSavedVert > holderY - 0.0001f)
            //{ _cameraSavedVert = holderY; }
        }
        if (_canMove == false)
        { return; }
        if (IsOnFloor() && _isFalling == true)
        { _justLanded = true; }
        if (IsOnFloor())
        {
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
        ChangeCrouchState(_crouchInput, (float)delta);
        if (_canMove == false)
        { return; }
        _velocity = this.Velocity;
        Vector3 direction = new Vector3(_inputDir.X, 0, _inputDir.Y).Normalized();
        direction = direction.Rotated(Vector3.Up, GlobalRotation.Y);
        //quake movement
        //if (IsOnFloor())
        //{ HandleGroundMovement((float)delta, direction); }
        //else
        //{ HandleAirMovement((float)delta, direction); }        
        HandleMovement((float)delta, direction);//comment this if using the quake movement
        HandleJump();
        //GD.Print("Velocity: " + _velocity.Length());
        //check steps
        if (!HandleStep((float)delta))
        {
            this.Velocity = _velocity;
            MoveAndSlide();
        }
        _wasOnFloor = IsOnFloor();
        //GD.Print("[physics]velocity:" + Velocity.ToString("0.0"));
    }
    #endregion
    //==========INPUT==========
    #region INPUT
    public override void _Input(InputEvent e)
    {
        if (e.IsEcho())
        { return; }
        if (_canMove == false)
        {
            _inputDir = Vector2.Zero;
            _runningInput = false;
            _crouchInput = false;
            return;
        }
        _inputDir = Input.GetVector("Right", "Left", "Backward", "Forward");
        _runningInput = Input.IsActionPressed("Run");
        _crouchInput = Input.IsActionPressed("Crouch");
    }
    public override void _UnhandledInput(InputEvent e)
    {
        if (_canLook == true && (e is InputEventMouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured))
        {
            Transform2D viewportTransform = GetTree().Root.GetFinalTransform();
            RotatePlayer(e.XformedBy(viewportTransform) as InputEventMouseMotion);
            GetViewport().SetInputAsHandled();
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
        motion *= DPI_MULTIPLIER * sensitivity;
        addPitch(motion.Y);
        addYaw(motion.X);
        clampPitch();
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
            if (IsApproxZero(X))
            { return; }
            cameraHolder.RotateObjectLocal(Vector3.Left, Mathf.DegToRad(X));
            cameraHolder.Orthonormalize();
        }
        void addYaw(float Y)
        {
            if (IsApproxZero(Y))
            { return; }
            this.RotateObjectLocal(Vector3.Down, Mathf.DegToRad(Y));
            this.Orthonormalize();
        }
        void clampPitch()
        {
            if (cameraHolder.Rotation.X > Mathf.DegToRad(maxLookDown) && cameraHolder.Rotation.X < Mathf.DegToRad(maxLookUp))
            { return; }
            Vector3 rot = cameraHolder.Rotation;
            rot.X = Mathf.Clamp(rot.X, Mathf.DegToRad(maxLookDown), Mathf.DegToRad(maxLookUp));
            cameraHolder.Rotation = rot;
            cameraHolder.Orthonormalize();
        }

    }
    private void TiltCamera(float tiltDegrees)
    {
        Vector3 rot = cameraHolder.RotationDegrees;
        rot.Z = tiltDegrees;
        cameraHolder.RotationDegrees = rot;
    }
    private void FixCameraRoll(double delta)
    {
        if (cameraHolder.RotationDegrees.Z != 0)
        {
            float tilt = cameraHolder.RotationDegrees.Z;
            tilt = Mathf.Lerp(tilt, 0, (float)delta * 20);
            if (tilt < 0.0001f && tilt > -0.0001f)
            { tilt = 0; }
            TiltCamera(-tilt);
        }
    }
    #endregion
    //==========MOVEMENT==========
    #region MOVEMENT
    private void HandleMovement(float delta, Vector3 direction)
    {
        direction *= GetMoveSpeed();
        direction.Y = this.Velocity.Y;
        _velocity = _velocity.Lerp(direction, delta * acceleration);
        HandleGravity(delta);
    }
    /*
    private void HandleGroundMovement(float delta, Vector3 direction)
    {
        float wishSpeed = GetMoveSpeed();
        float addSpeed, accelSpeed, currentSpeed;

        currentSpeed = _velocity.Dot(direction);
        addSpeed = wishSpeed - currentSpeed;
        if (addSpeed > 0)
        {
            accelSpeed = acceleration * delta * wishSpeed;
            if (accelSpeed > addSpeed)
            { accelSpeed = addSpeed; }
            _velocity += accelSpeed * direction;
        }
        //friction
        float speed = _velocity.Length();
        if (speed <= 0)
        {
            return;
        }
        float control = speed < deceleration ? deceleration : speed;
        float newSpeed = speed - delta * control * friction;
        if (newSpeed < 0)
        { newSpeed = 0; }
        newSpeed /= speed;
        _velocity *= newSpeed;

    }
    private void HandleAirMovement(float delta, Vector3 direction)
    {
        HandleGravity(delta);
        float speed = GetMoveSpeed();
        float curSpeed, cappedSpeed, addSpeed;

        curSpeed = _velocity.Dot(direction);
        cappedSpeed = Mathf.Min((speed * direction).Length(), airSpeed);
        addSpeed = cappedSpeed - curSpeed;
        if (addSpeed <= 0)
        { return; }
        float accel = acceleration * speed * delta;
        if (accel > addSpeed)
        { accel = addSpeed; }
        _velocity += accel * direction;
    }
    */
    private void HandleJump()
    {
        _jumpInput = Input.IsActionJustPressed("Jump");
        //GD.Print($"Jump:{_jumpInput} and onfloor:{IsOnFloor()} or falling:{_isFalling}");
        if ((_jumpInput || _jumpBuffer) && (IsOnFloor() || !_isFalling))//(IsOnFloor() || (!_wasOnFloor && !_isFalling)))
        {
            if (_jumpCount >= maxJumps)
            { return; }
            if (_jumpCount == 0)
            { StartJumpSound = true; }
            _jumpBuffer = false;
            _jumpCount += 1;
            _velocity.Y = CalcJumpForce();
        }
        else if (_jumpInput && (!IsOnFloor() || _isFalling))
        {
            _jumpBuffer = true;
            GetTree().CreateTimer(jumpBufferTime).Timeout += JumpBuffer_Timeout;
        }
    }
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
            stepHeight = RoundToDec(stepHeight, 3);
            //decimal decimalValue = Math.Round((decimal)stepHeight, 2);
            //if distance to step too high,        too low,       or step to high, don't move player there
            if (stepHeight > maxStepHeight || stepHeight <= 0.01f || (res.GetPosition() - this.GlobalPosition).Y > maxStepHeight)
            { return false; }
            Vector3 collisionPos = testStartPos.Origin + res.GetTravel();
            //GD.Print($"Trying to walk up something at angle: {Mathf.RadToDeg(Mathf.Acos(res.GetNormal().Dot(Vector3.Up)))} ({Mathf.Acos(res.GetNormal().Dot(Vector3.Up))})");
            if (Mathf.RadToDeg(Mathf.Acos(res.GetNormal().Dot(Vector3.Up))) >= MAX_SLOPE_ANGLE)//<--NOTE: this breaks when using a cylinder collider
            { return false; }
            //set camera position before global position
            _cameraSavedPos = camera.GlobalPosition;
            //set global position
            this.GlobalPosition = collisionPos;
            ApplyFloorSnap();//snap to floor to prevent floating
            return true;
        }
        return false;
    }
    private void HandleGravity(float delta)
    {
        if (!IsOnFloor())
        {//changing vertical, start for camera lerp and apply gravity
            //GD.Print($"velocity.Y <=0:{velocity.Y <= 0} |isFalling:{_isFalling}");
            if (_isFalling == false)
            {
                _coyoteTime += delta;
                if (_coyoteTime >= timeBeforeFalling)
                { _isFalling = true; }
                else
                { _isFalling = false; }
            }
            _cameraSavedPos = camera.GlobalPosition;
            //_velocity.Y -= _gravity * Mass * delta;
            if (_velocity.Y > 0)//jumping
            {
                _velocity.Y -= _gravity * (Mass) * delta;
            }
            else
            {
                _fallT += delta;
                _velocity.Y -= _gravity * (Mass * fallGravityCurve.Sample(_fallT)) * delta;
            }
            _velocity.Y = Mathf.Clamp(_velocity.Y, TERMINAL_VELOCITY, -TERMINAL_VELOCITY);
            if (Input.IsActionPressed("Jump") && mantlingNode.HandleMantle(delta, out Vector3 pos))
            {
                _cameraSavedPos = camera.GlobalPosition;
            }

            ApplyFloorSnap();//snap to floor to prevent floating
        }
        else
        {
            StartJumpSound = false;
            _jumpCount = 0;
            _isFalling = false;
            _coyoteTime = 0;
            _fallT = 0;
        }
    }
    private void ChangeCrouchState(bool newState, float delta)
    {
        bool state = ForceCrouch == false ? newState : true;
        float curCrouch;
        float curCam;
        if (state == true && (_isCrouching == false || (_isCrouching == true && _crouchVal < 1)))
        {
            _crouchVal += delta * crouchChangeSpeed;
            UpdateLerp();
            _collision.Height = curCrouch;
            Vector3 pos = collisionBody.Position;
            pos.Y = curCrouch * 0.5f;
            collisionBody.Position = pos;
            pos = cameraHolder.Position;
            pos.Y = curCam;
            cameraHolder.Position = pos;
            _isCrouching = _crouchVal >= 1;
            return;
        }
        if (state == false && (_isCrouching == true || (_isCrouching == false && _crouchVal > 0)))
        {
            headRay.ForceRaycastUpdate();
            GodotObject hit = headRay.GetCollider();
            if (hit is StaticBody3D || hit is CsgShape3D)//hit blocking
            { return; }
            _crouchVal -= delta * crouchChangeSpeed;
            UpdateLerp();
            Vector3 pos = collisionBody.Position;
            pos.Y = curCrouch * 0.5f;
            collisionBody.Position = pos;
            _collision.Height = curCrouch;
            pos = cameraHolder.Position;
            pos.Y = curCam;
            cameraHolder.Position = pos;
            _isCrouching = _crouchVal > 0;
            return;
        }

        void UpdateLerp()
        {
            _crouchVal = Mathf.Clamp(_crouchVal, 0, 1);
            curCrouch = Mathf.Lerp(_standingHeight, crouchHeight, _crouchVal);
            curCam = Mathf.Lerp(_defaultCameraHeight, _defaultCameraHeight - (_standingHeight - crouchHeight), _crouchVal);
        }
    }
    private float GetMoveSpeed()
    {
        if (_isCrouching == true)
        { return movementSpeed * crouchMultiplier; }
        if (_runningInput == true && _inputDir.Y >= 0)
        { return movementSpeed * sprintMultiplier; }
        return movementSpeed;
    }
    private float CalcJumpForce()
    {//initial_velocity^2 =  final_velocity^2 - 2*acceleration*displacement
        //Sqrt(2*Gravity*JumpHeight*Mass);//account for gravity applied to player
        float height = _isCrouching ? jumpHeight * 0.5f + 0.1f : jumpHeight + 0.1f;
        return Mathf.Sqrt(2 * (_gravity) * height * Mass);
    }
    #endregion
    //==========PUBLIC METHODS==========
    #region Public Methods
    public void ForcePosition(Vector3 position)
    {
        this.GlobalPosition = position;
    }
    public void ForceRotation(Vector3 rotation)
    {
        this.GlobalRotation = rotation;
    }
    public void ForceVelocity(Vector3 velocity)
    {
        this.Velocity = velocity;
    }
    #endregion
    //==========EVENTS==========
    #region EVENTS
    private void JumpBuffer_Timeout()
    {
        _jumpBuffer = false;
    }
    #endregion
    #region Properties
    public float MovementSpeed => movementSpeed;
    public float CrouchHeight => crouchHeight;
    public float StandingHeight => _standingHeight;
    public bool IsCrouching => _isCrouching;
    public bool JustLanded => _justLanded;
    public bool WasOnFloor => _wasOnFloor;
    public bool IsInAir => _isFalling || Velocity.Y > 0;
    public bool StartJumpSound { get; set; }
    public CollisionShape3D CollisionBody => collisionBody;
    public bool CanMove { get => _canMove; set => _canMove = value; }
    public bool CanLook { get => _canLook; set => _canLook = value; }
    public bool ForceCrouch { get; set; }
    #endregion
    #region ExtensionMethods
    private float ReMap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
    private float RoundToDec(float value, int digit)
    {
        float pow = Mathf.Pow(10, digit);
        return Mathf.Floor(value * pow) / pow;
    }
    private bool IsApproxZero(float value)
    {
        if (value < 0.0001f && value > -0.0001f)
        { return true; }
        return false;
    }
    #endregion
}
