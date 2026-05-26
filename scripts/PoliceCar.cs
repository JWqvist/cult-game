using Godot;

/// <summary>
/// Police car NPC. Spawned by HeatSystem when heat >= 2.
/// Chases player at Speed=110. Despawned when heat drops below 1.
/// Deals 10 damage/s while in contact with the player.
/// </summary>
public partial class PoliceCar : CharacterBody2D
{
    private const float Speed = 110f;
    private const float DamagePerSecond = 10f;

    private Node2D _player;
    private Player _playerScript;

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
            {
                _player = n2d;
                _playerScript = n2d as Player;
            }
        }

        if (_player == null) return;

        Vector2 dir = (_player.GlobalPosition - GlobalPosition).Normalized();
        Velocity = dir * Speed;
        MoveAndSlide();

        // Damage player on contact (within 40 px)
        if (_playerScript != null && GlobalPosition.DistanceTo(_player.GlobalPosition) < 40f)
            _playerScript.TakeDamage(DamagePerSecond * (float)delta);
    }
}
