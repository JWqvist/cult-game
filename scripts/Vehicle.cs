using Godot;

public partial class Vehicle : CharacterBody2D
{
    [Export] public float MaxSpeed = 200f;
    [Export] public float ReverseMaxSpeed = 80f;
    [Export] public float Acceleration = 150f;
    [Export] public float Friction = 100f;
    [Export] public float TurnSpeed = 2.2f;
    [Export] public float InteractRange = 80f;

    public bool IsOccupied { get; private set; } = false;

    private float _speed = 0f;

    public override void _Ready()
    {
        AddToGroup("vehicles");
        if (AssetManager.Instance != null)
        {
            GetNode<Sprite2D>("Sprite2D").Texture = AssetManager.Instance.GetCarTexture("red");
            GetNode<Sprite2D>("Sprite2D").Modulate = Colors.White;
        }
    }

    public void Enter()
    {
        IsOccupied = true;
    }

    public void Exit()
    {
        IsOccupied = false;
        _speed = 0f;
        Velocity = Vector2.Zero;
    }

    // Returns a world position beside the vehicle for the player to stand at.
    public Vector2 GetExitPosition()
    {
        Vector2 right = new Vector2(1f, 0f).Rotated(Rotation);
        return GlobalPosition + right * 45f;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsOccupied)
        {
            Velocity = Vector2.Zero;
            return;
        }

        float dt = (float)delta;

        // Steering: left/right rotate the vehicle
        float steer = 0f;
        if (Input.IsActionPressed("move_left"))  steer -= 1f;
        if (Input.IsActionPressed("move_right")) steer += 1f;

        // Throttle: up = forward, down = brake / reverse
        float throttle = 0f;
        if (Input.IsActionPressed("move_up"))   throttle =  1f;
        if (Input.IsActionPressed("move_down")) throttle = -1f;

        // Update longitudinal speed
        if (throttle != 0f)
        {
            _speed += throttle * Acceleration * dt;
            _speed = Mathf.Clamp(_speed, -ReverseMaxSpeed, MaxSpeed);
        }
        else
        {
            _speed = Mathf.MoveToward(_speed, 0f, Friction * dt);
        }

        // Rotate only when moving fast enough (prevents spinning on the spot)
        if (Mathf.Abs(_speed) > 8f)
        {
            float steerDir = Mathf.Sign(_speed);
            Rotation += steer * TurnSpeed * steerDir * dt;
        }

        // Forward is -Y in local space (Godot convention for upward sprites)
        Velocity = new Vector2(0f, -_speed).Rotated(Rotation);
        MoveAndSlide();
    }
}
