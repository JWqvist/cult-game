using Godot;

/// <summary>
/// Police car NPC. Spawned by HeatSystem when heat >= 2.
/// Chases player at Speed=110. Despawned when heat drops below 1.
/// </summary>
public partial class PoliceCar : CharacterBody2D
{
    private const float Speed = 110f;

    private Node2D _player;

    public override void _Ready()
    {
        AddToGroup("police");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null)
        {
            Node node = GetTree().GetFirstNodeInGroup("player");
            if (node is Node2D n2d)
                _player = n2d;
        }

        if (_player == null) return;

        Vector2 dir = (_player.GlobalPosition - GlobalPosition).Normalized();
        Velocity = dir * Speed;
        MoveAndSlide();
    }
}
