using Godot;

public partial class Pedestrian : NPC
{
    private enum State { IDLE, WALKING, FLEEING, FOLLOWING }

    private const float WalkSpeed = 60f;
    private const float FleeSpeed = 120f;
    private const float FleeRadius = 200f;
    private const float StopFleeRadius = 300f;
    private const float WanderRadius = 300f;
    private const float IdleTime = 2f;
    private const float ArrivalThreshold = 10f;

    private State _state = State.IDLE;
    private Vector2 _targetPosition;
    private float _idleTimer = 0f;
    private Node2D _player;
    private Vector2 _followOffset;

    public override void _Ready()
    {
        base._Ready(); // adds to "npcs" group
        AddToGroup("pedestrians");
        _targetPosition = GlobalPosition;
        _idleTimer = GD.Randf() * IdleTime; // stagger start times
    }

    public void StartFollowing()
    {
        _state = State.FOLLOWING;
        float angle = GD.Randf() * Mathf.Tau;
        _followOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (30f + GD.Randf() * 40f);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // Find player reference lazily
        if (_player == null)
        {
            Node node = GetTree().GetFirstNodeInGroup("player");
            if (node is Node2D n2d)
                _player = n2d;
        }

        float distToPlayer = _player != null ? GlobalPosition.DistanceTo(_player.GlobalPosition) : float.MaxValue;

        // State transitions (FOLLOWING overrides flee/wander)
        if (_state != State.FLEEING && _state != State.FOLLOWING && distToPlayer < FleeRadius)
        {
            _state = State.FLEEING;
        }
        else if (_state == State.FLEEING && distToPlayer > StopFleeRadius)
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
                    Velocity = toTarget.Normalized() * WalkSpeed;
                }
                break;

            case State.FLEEING:
                if (_player != null)
                {
                    Vector2 awayFromPlayer = (GlobalPosition - _player.GlobalPosition).Normalized();
                    Velocity = awayFromPlayer * FleeSpeed;
                }
                break;

            case State.FOLLOWING:
                if (_player != null)
                {
                    Vector2 followTarget = _player.GlobalPosition + _followOffset;
                    Vector2 toFollow = followTarget - GlobalPosition;
                    if (toFollow.Length() > ArrivalThreshold)
                        Velocity = toFollow.Normalized() * WalkSpeed;
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
