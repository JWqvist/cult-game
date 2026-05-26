using Godot;

public partial class TrafficCar : CharacterBody2D
{
    [Export] public Vector2[] Waypoints = System.Array.Empty<Vector2>();
    [Export] public float Speed = 80f;

    private int _currentWaypoint = 0;
    private const float ArrivalThreshold = 20f;

    public override void _Ready()
    {
        AddToGroup("traffic");

        // If no waypoints set, park in place
        if (Waypoints == null || Waypoints.Length == 0)
        {
            Waypoints = new Vector2[] { GlobalPosition };
        }

        // Start at the nearest waypoint
        _currentWaypoint = GetNearestWaypointIndex();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Waypoints.Length == 0) return;

        Vector2 target = Waypoints[_currentWaypoint];
        Vector2 toTarget = target - GlobalPosition;

        if (toTarget.Length() < ArrivalThreshold)
        {
            _currentWaypoint = (_currentWaypoint + 1) % Waypoints.Length;
        }
        else
        {
            Vector2 dir = toTarget.Normalized();
            Velocity = dir * Speed;
            // Face direction of travel
            Rotation = dir.Angle() + Mathf.Pi / 2f;
        }

        MoveAndSlide();
    }

    private int GetNearestWaypointIndex()
    {
        int nearest = 0;
        float nearestDist = float.MaxValue;
        for (int i = 0; i < Waypoints.Length; i++)
        {
            float d = GlobalPosition.DistanceTo(Waypoints[i]);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = i;
            }
        }
        return nearest;
    }
}
