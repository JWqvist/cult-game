using Godot;

/// <summary>
/// NPC with wandering AI state machine + recruitment mechanic.
/// States: Idle, Wander, Flee, Following (recruited).
/// </summary>
public partial class NPC : CharacterBody2D
{
    private enum State { Idle, Wander, Flee, Following }

    [Export] public float WalkSpeed { get; set; } = 50f;
    [Export] public float FleeSpeed { get; set; } = 120f;
    [Export] public float FollowSpeed { get; set; } = 90f;
    [Export] public float FleeRadius { get; set; } = 150f;
    [Export] public float StopFleeRadius { get; set; } = 280f;
    [Export] public float RecruitRange { get; set; } = 50f;
    [Export] public float Health = 100f;

    private State _state = State.Idle;
    private float _stateTimer = 0f;
    private Vector2 _wanderDirection;
    private Node2D _player;
    private Sprite2D _sprite;
    private bool _recruited = false;
    private int _followerIndex = 0;

    // Colors
    private static readonly Color NormalColor = new Color(0.2f, 0.8f, 0.3f, 1f);
    private static readonly Color RecruitedColor = new Color(0.8f, 0.2f, 0.8f, 1f);

    public bool IsRecruited => _recruited;

    public override void _Ready()
    {
        AddToGroup("npcs");
        _sprite = GetNode<Sprite2D>("Sprite2D");
        _stateTimer = GD.Randf() * 2f; // stagger
        if (_sprite != null)
            _sprite.Modulate = NormalColor;
    }

    public bool IsDead { get; private set; } = false;

    /// <summary>Item type this NPC drops on death (empty = no drop).</summary>
    [Export] public string DropItem { get; set; } = "";

    public virtual void TakeDamage(float amount)
    {
        if (IsDead) return;
        Health -= amount;
        FlashHit();
        if (Health <= 0f)
            Die();
    }

    /// <summary>Brief red tint to make a non-lethal hit visible (no animation assets needed).</summary>
    private void FlashHit()
    {
        if (_sprite == null || !IsInstanceValid(_sprite)) return;
        Color baseColor = _recruited ? RecruitedColor : NormalColor;
        _sprite.Modulate = new Color(1f, 0.4f, 0.4f, 1f);
        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate", baseColor, 0.18f);
    }

    private void Die()
    {
        IsDead = true;
        Velocity = Vector2.Zero;

        // Visual death state (ragdoll-lite): grey out, fall over, shrink slightly.
        if (_sprite != null && IsInstanceValid(_sprite))
        {
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenProperty(_sprite, "modulate", new Color(0.4f, 0.4f, 0.4f, 0.7f), 0.25f);
            tween.TweenProperty(_sprite, "rotation", Mathf.DegToRad(90f), 0.25f);
            tween.TweenProperty(_sprite, "scale", new Vector2(0.9f, 0.9f), 0.25f);
        }

        // Disable collision so the corpse doesn't block movement or future raycasts.
        var col = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (col != null)
            col.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);

        GD.Print("[NPC] ", Name, " died.");

        // Drop item pickup
        if (!string.IsNullOrEmpty(DropItem))
        {
            var pickupScene = GD.Load<PackedScene>("res://scenes/ItemPickup.tscn");
            if (pickupScene != null)
            {
                var pickup = pickupScene.Instantiate<ItemPickup>();
                pickup.ItemType = DropItem;
                pickup.GlobalPosition = GlobalPosition;
                GetParent().AddChild(pickup);
            }
        }

        // Remove from npcs group so combat doesn't target corpse
        RemoveFromGroup("npcs");

        // Disappear after a delay
        var timer = GetTree().CreateTimer(5.0);
        timer.Timeout += QueueFree;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsDead) return;
        float dt = (float)delta;

        if (_player == null)
        {
            Node node = GetTree().GetFirstNodeInGroup("player");
            if (node is Node2D n2d)
                _player = n2d;
            if (_player == null) return;
        }

        if (_recruited)
        {
            ProcessFollowing(dt);
            MoveAndSlide();
            return;
        }

        float distToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);

        // Check flee (only when heat >= 3)
        if (_state != State.Flee && distToPlayer < FleeRadius && GameManager.Instance != null && GameManager.Instance.HeatLevel >= 3)
        {
            _state = State.Flee;
        }
        else if (_state == State.Flee && (distToPlayer > StopFleeRadius || (GameManager.Instance != null && GameManager.Instance.HeatLevel < 3)))
        {
            _state = State.Idle;
            _stateTimer = 1f + GD.Randf() * 2f;
        }

        switch (_state)
        {
            case State.Idle:
                Velocity = Vector2.Zero;
                _stateTimer -= dt;
                if (_stateTimer <= 0f)
                {
                    // Transition to Wander
                    float angle = GD.Randf() * Mathf.Tau;
                    _wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    _stateTimer = 2f + GD.Randf() * 2f;
                    _state = State.Wander;
                }
                break;

            case State.Wander:
                Velocity = _wanderDirection * WalkSpeed;
                _stateTimer -= dt;
                if (_stateTimer <= 0f)
                {
                    _state = State.Idle;
                    _stateTimer = 1f + GD.Randf() * 2f;
                }
                break;

            case State.Flee:
                Vector2 away = (GlobalPosition - _player.GlobalPosition).Normalized();
                Velocity = away * FleeSpeed;
                break;

            case State.Following:
                ProcessFollowing(dt);
                break;
        }

        MoveAndSlide();
    }

    private void ProcessFollowing(float dt)
    {
        if (_player == null) return;

        // Follow in loose formation behind player
        float offsetDist = 40f + _followerIndex * 30f;
        float offsetAngle = _followerIndex * 0.8f; // spread them out
        Vector2 targetPos = _player.GlobalPosition + new Vector2(
            Mathf.Cos(offsetAngle) * offsetDist,
            Mathf.Sin(offsetAngle) * offsetDist
        );

        Vector2 toTarget = targetPos - GlobalPosition;
        float dist = toTarget.Length();

        if (dist > 15f)
        {
            Velocity = toTarget.Normalized() * Mathf.Min(FollowSpeed, dist * 2f);
        }
        else
        {
            Velocity = Vector2.Zero;
        }
    }

    /// <summary>
    /// Recruit this NPC into the cult.
    /// </summary>
    public void Recruit(int followerIndex)
    {
        if (_recruited) return;
        _recruited = true;
        _followerIndex = followerIndex;
        _state = State.Following;

        // Visual feedback — change color to cult purple
        if (_sprite != null)
            _sprite.Modulate = RecruitedColor;

        // Update GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.AddFollower();

        GD.Print("[NPC] Recruited! Follower index: ", _followerIndex);
    }

    /// <summary>
    /// Check if player is in range to recruit this NPC.
    /// </summary>
    public bool IsInRecruitRange(Vector2 playerPos)
    {
        return !_recruited && GlobalPosition.DistanceTo(playerPos) < RecruitRange;
    }
}
