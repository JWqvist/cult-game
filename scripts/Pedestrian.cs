using Godot;

public partial class Pedestrian : NPC
{
    private enum State { IDLE, WALKING, FLEEING, FOLLOWING }

    private const float PedWalkSpeed = 60f;
    private const float PedFleeSpeed = 120f;
    private const float PedFleeRadius = 200f;
    private const float PedStopFleeRadius = 300f;
    private const float WanderRadius = 300f;
    private const float IdleTime = 2f;
    private const float ArrivalThreshold = 10f;

    public new bool IsRecruited { get; private set; } = false;

    private State _state = State.IDLE;
    private Vector2 _targetPosition;
    private float _idleTimer = 0f;
    private Node2D _player;
    private Vector2 _followOffset;

    private Sprite2D _sprite;

    public override void _Ready()
    {
        base._Ready(); // adds to "npcs" group
        AddToGroup("pedestrians");
        _targetPosition = GlobalPosition;
        _idleTimer = GD.Randf() * IdleTime; // stagger start times
        _sprite = GetNode<Sprite2D>("Sprite2D");

        if (AssetManager.Instance != null)
        {
            _sprite.Texture = AssetManager.Instance.GetNPCTexture("down", false);
            _sprite.Modulate = Colors.White;
        }
    }

    public void OnAttacked()
    {
        _state = State.FLEEING;
    }

    public void StartFollowing()
    {
        IsRecruited = true;
        _state = State.FOLLOWING;
        if (AssetManager.Instance != null && _sprite != null)
            _sprite.Texture = AssetManager.Instance.GetNPCTexture("down", true);
        float angle = GD.Randf() * Mathf.Tau;
        _followOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (30f + GD.Randf() * 40f);
    }

    public override void _PhysicsProcess(double delta)
    {
        // Dead pedestrians disable their AI (ragdoll-lite). Base NPC.Die()
        // handles the death visuals, item drop and cleanup timer.
        if (IsDead)
        {
            Velocity = Vector2.Zero;
            return;
        }

        float dt = (float)delta;

        // Find player reference lazily
        if (_player == null)
        {
            Node node = GetTree().GetFirstNodeInGroup("player");
            if (node is Node2D n2d)
                _player = n2d;
        }

        float distToPlayer = _player != null ? GlobalPosition.DistanceTo(_player.GlobalPosition) : float.MaxValue;

        // State transitions
        if (_state == State.FLEEING && distToPlayer > PedStopFleeRadius)
        {
            _state = State.IDLE;
            _idleTimer = IdleTime;
        }

        // State behaviour
        switch (_state)
        {
            case State.IDLE:
                Velocity = Vector2.Zero;
                _idleTimer -= dt;
                if (_idleTimer <= 0f)
                {
                    PickNewWanderTarget();
                    _state = State.WALKING;
                }
                break;

            case State.WALKING:
                Vector2 toTarget = _targetPosition - GlobalPosition;
                if (toTarget.Length() < ArrivalThreshold)
                {
                    Velocity = Vector2.Zero;
                    _state = State.IDLE;
                    _idleTimer = IdleTime + GD.Randf() * IdleTime;
                }
                else
                {
                    Velocity = toTarget.Normalized() * PedWalkSpeed;
                }
                break;

            case State.FLEEING:
                if (_player != null)
                {
                    Vector2 awayFromPlayer = (GlobalPosition - _player.GlobalPosition).Normalized();
                    Velocity = awayFromPlayer * PedFleeSpeed;
                }
                break;

            case State.FOLLOWING:
                if (_player != null)
                {
                    Vector2 followTarget = _player.GlobalPosition + _followOffset;
                    Vector2 toFollow = followTarget - GlobalPosition;
                    if (toFollow.Length() > ArrivalThreshold)
                        Velocity = toFollow.Normalized() * PedWalkSpeed;
                    else
                        Velocity = Vector2.Zero;
                }
                break;
        }

        MoveAndSlide();
    }

    private void PickNewWanderTarget()
    {
        float angle = GD.Randf() * Mathf.Tau;
        float dist = 50f + GD.Randf() * WanderRadius;
        _targetPosition = GlobalPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }
}
