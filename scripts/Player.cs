using Godot;

public partial class Player : CharacterBody2D
{
    private const float WalkSpeed = 80f;
    private const float RunSpeed = 160f;

    public float Health = 100f;
    public Inventory Inventory { get; private set; }

    /// <summary>Called by NPCs, Police or other hazards to damage the player.</summary>
    public void TakeDamage(float amount)
    {
        if (EndgameSystem.Instance?.IsOver == true) return;
        Health = Mathf.Max(0f, Health - amount);
        GD.Print($"[Player] TakeDamage {amount:F0}  HP left: {Health:F0}");
        if (Health <= 0f)
            EndgameSystem.Instance?.TriggerPlayerDead();
    }

    private AnimationPlayer _animationPlayer;
    private Sprite2D _sprite;
    private string _lastDirection = "down";

    private Vehicle _currentVehicle = null;
    public bool IsInVehicle => _currentVehicle != null;

    public override void _Ready()
    {
        AddToGroup("player");
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        Inventory = GetNode<Inventory>("Inventory");

        if (AssetManager.Instance != null)
        {
            _sprite.Texture = AssetManager.Instance.GetPlayerTexture("down");
            _sprite.Modulate = Colors.White;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed("interact"))
        {
            if (_currentVehicle != null)
                ExitVehicle();
            else
                TryEnterVehicle(); // RecruitSystem (child node) handles NPC dialogue via _Process
        }
        else if (_currentVehicle != null && Input.IsActionJustPressed("melee_attack"))
        {
            ExitVehicle();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // While driving: follow the vehicle position so the Camera2D tracks it
        if (_currentVehicle != null)
        {
            GlobalPosition = _currentVehicle.GlobalPosition;
            Velocity = Vector2.Zero;
            return;
        }

        // Normal on-foot movement
        Vector2 inputDir = Vector2.Zero;

        if (Input.IsActionPressed("move_up")) inputDir.Y -= 1;
        if (Input.IsActionPressed("move_down")) inputDir.Y += 1;
        if (Input.IsActionPressed("move_left")) inputDir.X -= 1;
        if (Input.IsActionPressed("move_right")) inputDir.X += 1;

        bool running = Input.IsActionPressed("run");
        float speed = running ? RunSpeed : WalkSpeed;

        if (inputDir != Vector2.Zero)
        {
            inputDir = inputDir.Normalized();
            Velocity = inputDir * speed;
            UpdateDirectionAndAnimation(inputDir, running);
            if (_sprite != null)
                _sprite.Rotation = Mathf.Atan2(inputDir.Y, inputDir.X) + Mathf.Pi / 2f;
        }
        else
        {
            Velocity = Vector2.Zero;
            PlayAnimation("idle_" + _lastDirection);
        }

        MoveAndSlide();
    }

    private void TryEnterVehicle()
    {
        Godot.Collections.Array<Node> vehicles = GetTree().GetNodesInGroup("vehicles");
        Vehicle nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Node node in vehicles)
        {
            if (node is Vehicle vehicle && !vehicle.IsOccupied)
            {
                float dist = GlobalPosition.DistanceTo(vehicle.GlobalPosition);
                if (dist < vehicle.InteractRange && dist < nearestDist)
                {
                    nearest = vehicle;
                    nearestDist = dist;
                }
            }
        }

        if (nearest != null)
            EnterVehicle(nearest);
    }

    private void EnterVehicle(Vehicle vehicle)
    {
        _currentVehicle = vehicle;
        _currentVehicle.Enter();
        Visible = false;
        ToastManager.Show("Carjacked! WASD to drive, E/F to exit.", Colors.White);
    }

    private void ExitVehicle()
    {
        GlobalPosition = _currentVehicle.GetExitPosition();
        _currentVehicle.Exit();
        _currentVehicle = null;
        Visible = true;
        ToastManager.Show("Exited vehicle.", Colors.White);
    }

    private void UpdateDirectionAndAnimation(Vector2 dir, bool running)
    {
        string animPrefix = running ? "run" : "walk";
        string newDir;

        if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
        {
            newDir = dir.X > 0 ? "right" : "left";
        }
        else
        {
            newDir = dir.Y < 0 ? "up" : "down";
        }

        PlayAnimation(animPrefix + "_" + newDir);
        _lastDirection = newDir;
    }

    private void PlayAnimation(string animName)
    {
        if (_animationPlayer == null) return;

        // Fall back to walk_ if run_ animation doesn't exist yet
        if (animName.StartsWith("run_") && !_animationPlayer.HasAnimation(animName))
        {
            animName = "walk_" + animName.Substring(4);
        }

        // Fall back to idle_down if animation doesn't exist
        if (!_animationPlayer.HasAnimation(animName))
        {
            if (_animationPlayer.HasAnimation("idle_down"))
                animName = "idle_down";
            else
                return;
        }

        if (_animationPlayer.CurrentAnimation != animName)
            _animationPlayer.Play(animName);
    }
}
