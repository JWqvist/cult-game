using Godot;

public partial class Player : CharacterBody2D
{
    private const float WalkSpeed = 80f;
    private const float RunSpeed = 160f;

    private AnimationPlayer _animationPlayer;
    private Sprite2D _sprite;
    private string _lastDirection = "down";

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _sprite = GetNode<Sprite2D>("Sprite2D");
    }

    public override void _PhysicsProcess(double delta)
    {
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
        }
        else
        {
            Velocity = Vector2.Zero;
            PlayAnimation("idle_" + _lastDirection);
        }

        MoveAndSlide();
    }

    private void UpdateDirectionAndAnimation(Vector2 dir, bool running)
    {
        string animPrefix = running ? "run" : "walk";

        if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
        {
            if (dir.X > 0)
            {
                _lastDirection = "right";
                _sprite.FlipH = false;
                PlayAnimation(animPrefix + "_right");
            }
            else
            {
                _lastDirection = "left";
                _sprite.FlipH = false;
                PlayAnimation(animPrefix + "_left");
            }
        }
        else
        {
            if (dir.Y < 0)
            {
                _lastDirection = "up";
                _sprite.FlipH = false;
                PlayAnimation(animPrefix + "_up");
            }
            else
            {
                _lastDirection = "down";
                _sprite.FlipH = false;
                PlayAnimation(animPrefix + "_down");
            }
        }
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
