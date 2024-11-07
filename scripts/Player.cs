using Godot;
using System;

public partial class Player : CharacterBody3D
{
    private const float TERMINAL_VELOCITY = -53.645f;//200mph in m/s according to FAI SKYDIVING COMMISSION
    private const float MAX_SLOP_ANGLE = 60f;

    [ExportGroup("Movement")]
    [Export] private float acceleration = 10;
    [Export] private float movementSpeed = 3.6f;
    [Export] private float sprintMultiplier = 2;
    [Export] private float maxStepHeight = 0.25f;
    [ExportGroup("Crouching")]
    [Export] private float crouchMultiplier = 0.75f;
    [Export] private float crouchChangeSpeed = 10;
    [Export] private float crouchHeight = 0.9f;//meters
    [ExportGroup("Camera")]
    [Export] private float sensitivity = 0.4f;
    [Export] private float cameraLeanInto = 7.5f;//degrees
    [Export] private float cameraVertSpeed = 10f;
    [Export] private float maxLookUp = 55, maxLookDown = -75;
    [ExportGroup("Physics")]
    [Export] private float Mass = 10;
    [Export] private float timeBeforeFalling = 0.1f;//coyote time
    [ExportGroup("Nodes")]
    [Export] private Node3D cameraHolder;
    [Export] private Camera3D camera;
    [Export] private RayCast3D headRay;
    [Export] private CollisionShape3D collisionBody;

    private bool _runningInput = false, _crouchInput = false;
    private bool _isCrouching;
    private bool _justLanded = false, _isFalling = false, _wasOnFloor = true;
    private Vector2 _inputDir, _camInput;
    private Vector3 velocity;
    private float _cameraSavedVert = float.NegativeInfinity;
    private float _defaultBodyHeight, _defaultCameraHeight;
    private float _crouchVal = 0, _tilt = 0;
    private float _coyoteTime = 0;
    private CylinderShape3D _collision;
    //private CapsuleShape3D _collision;
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();



    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _collision = (CylinderShape3D)collisionBody.Shape;
        //_collision = (CapsuleShape3D)collisionBody.Shape;
        _defaultBodyHeight = _collision.Height;
        _defaultCameraHeight = cameraHolder.Position.Y;//local, for global: camera.GlobalPosition.Y-this.GlobalPosition.Y
        Vector3 pos = headRay.Position;
        pos.Y = crouchHeight;
        headRay.Position = pos;
    }

    public override void _Process(double delta)
    {
        if (Velocity.LengthSquared() > movementSpeed * movementSpeed)
        {
            _tilt = ReMap(_camInput.X * sensitivity, -30, 30, -cameraLeanInto, cameraLeanInto);
            _tilt = Mathf.Clamp(_tilt, -cameraLeanInto, cameraLeanInto);
            if (_camInput != Vector2.Zero)
            { TiltCamera(-_tilt); }
            _camInput = Vector2.Zero;
        }
        else if (cameraHolder.RotationDegrees.Z != 0)
        {
            float tilt = cameraHolder.RotationDegrees.Z;
            tilt = Mathf.Lerp(tilt, 0, (float)delta * 20);
            if (tilt < 0.0001f && tilt > -0.0001f)
            { tilt = 0; }
            TiltCamera(-tilt);
        }
        if (!float.IsNegativeInfinity(_cameraSavedVert))
        {
            Vector3 pos = camera.GlobalPosition;
            float holderY = cameraHolder.GlobalPosition.Y;
            _cameraSavedVert = Mathf.Lerp(_cameraSavedVert, holderY, (float)delta * cameraVertSpeed);
            pos.Y = _cameraSavedVert;
            camera.GlobalPosition = pos;
            //if (_cameraSavedVert == holderY)
            //{ _cameraSavedVert = float.NegativeInfinity; }
            //if (_cameraSavedVert < holderY + 0.0001f && _cameraSavedVert > holderY - 0.0001f)
            //{ _cameraSavedVert = holderY; }
        }

        if (IsOnFloor() && _isFalling == true)
        { _justLanded = true; }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _PhysicsProcess(double delta)
    {
        _justLanded = false;
        ChangeCrouchState(_crouchInput, (float)delta);
        velocity = this.Velocity;
        Vector3 direction = new Vector3(_inputDir.X, 0, _inputDir.Y).Normalized();
        direction = direction.Rotated(Vector3.Up, GlobalRotation.Y) * DecideMoveSpeed();
        direction.Y = this.Velocity.Y;
        //Vector3 velocity = new Vector3(_currentSpeed * direction.X, this.Velocity.Y, _currentSpeed * direction.Z);
        velocity = velocity.Lerp(direction, (float)delta * acceleration);
        HandleGravity((float)delta);

        //check steps
        this.Velocity = velocity;
        if (!HandleStep((float)delta))
        { MoveAndSlide(); }
        _wasOnFloor = IsOnFloor();
    }

    public override void _Input(InputEvent e)
    {
        if (e.IsEcho())
        { return; }
        _inputDir = Input.GetVector("Right", "Left", "Backward", "Forward");
        _runningInput = Input.IsActionPressed("Run");
        _crouchInput = Input.IsActionPressed("Crouch");
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion)
        {
            _camInput = (e as InputEventMouseMotion).Relative;
            RotatePlayer(_camInput);
        }
    }

    private void RotatePlayer(Vector2 rotation)
    {
        cameraHolder.RotateX(Mathf.DegToRad(rotation.Y * sensitivity));
        this.RotateY(Mathf.DegToRad(-rotation.X * sensitivity));
        cameraHolder.RotationDegrees = new Vector3(Mathf.Clamp(cameraHolder.RotationDegrees.X, maxLookDown, maxLookUp), 180, cameraHolder.RotationDegrees.Z);
    }
    private void TiltCamera(float tiltDegrees)
    {
        Vector3 rot = cameraHolder.RotationDegrees;
        rot.Z = tiltDegrees;
        cameraHolder.RotationDegrees = rot;
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
            Vector3 CollisionPos = testStartPos.Origin + res.GetTravel();
            //GD.Print($"Trying to walk up something at angle: {Mathf.RadToDeg(Mathf.Acos(res.GetNormal().Dot(Vector3.Up)))} ({Mathf.Acos(res.GetNormal().Dot(Vector3.Up))})");
            if (Mathf.RadToDeg(Mathf.Acos(res.GetNormal().Dot(Vector3.Up))) >= MAX_SLOP_ANGLE)//<--NOTE: this breaks when using a cylinder collider
            { return false; }
            //set camera position before global position
            _cameraSavedVert = camera.GlobalPosition.Y;
            //set global position
            this.GlobalPosition = CollisionPos;
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
                {
                    _isFalling = true;
                    _coyoteTime = 0;
                }
                else
                { _isFalling = false; }
            }
            _cameraSavedVert = camera.GlobalPosition.Y;
            velocity.Y -= _gravity * Mass * delta;
            velocity.Y = Mathf.Clamp(velocity.Y, TERMINAL_VELOCITY, -TERMINAL_VELOCITY);
            ApplyFloorSnap();//snap to floor to prevent floating
        }
        else
        {
            _isFalling = false;
            _coyoteTime = 0;
        }
    }
    private void ChangeCrouchState(bool newState, float delta)
    {
        float curCrouch;
        float curCam;
        if (newState == true && (_isCrouching == false || (_isCrouching == true && _crouchVal < 1)))
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
        if (newState == false && (_isCrouching == true || (_isCrouching == false && _crouchVal > 0)))
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
            curCrouch = Mathf.Lerp(_defaultBodyHeight, crouchHeight, _crouchVal);
            curCam = Mathf.Lerp(_defaultCameraHeight, _defaultCameraHeight - (_defaultBodyHeight - crouchHeight), _crouchVal);
        }
    }
    private float DecideMoveSpeed()
    {
        if (_isCrouching == true)
        { return movementSpeed * crouchMultiplier; }
        if (_runningInput == true && _inputDir.Y >= 0)
        { return movementSpeed * sprintMultiplier; }
        return movementSpeed;
    }
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
    #endregion
    public float MovementSpeed => movementSpeed;
    public bool JustLanded => _justLanded;
    public bool WasOnFloor => _wasOnFloor;
    public bool IsInAir => _isFalling || velocity.Y > 0;
}
