using Godot;

/// <summary>
/// Police car NPC. Spawned by HeatSystem when heat >= 2.
/// Chases player at Speed=110. Despawned when heat drops below 1.
/// Arrest: after 1.5 s of contact, player loses 30% money,
/// teleports to spawn, and heat resets (which despawns this car).
/// </summary>
public partial class PoliceCar : CharacterBody2D
{
    private const float Speed = 110f;
    private const float ArrestContactTime = 1.5f;
    private const float ArrestMoneyLossRatio = 0.30f;
    private static readonly Vector2 SpawnPosition = new Vector2(640f, 360f);

    private Node2D _player;
    private Player _playerScript;
    private float _contactTime = 0f;
    private bool _arresting = false;

    public override void _Ready()
    {
        AddToGroup("police");
        if (AssetManager.Instance != null)
        {
            GetNode<Sprite2D>("Sprite2D").Texture = AssetManager.Instance.GetCarTexture("police");
            GetNode<Sprite2D>("Sprite2D").Modulate = Colors.White;
        }
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

        // Arrest on contact (within 40 px for ArrestContactTime seconds)
        if (!_arresting && GlobalPosition.DistanceTo(_player.GlobalPosition) < 40f)
        {
            _contactTime += (float)delta;
            if (_contactTime >= ArrestContactTime)
            {
                _arresting = true;
                CallDeferred(nameof(Arrest));
            }
        }
        else if (!_arresting)
        {
            _contactTime = 0f;
        }
    }

    private void Arrest()
    {
        if (_player == null || !IsInstanceValid(_player)) return;

        float loss = 0f;
        if (GameManager.Instance != null)
        {
            loss = GameManager.Instance.Money * ArrestMoneyLossRatio;
            GameManager.Instance.SpendMoney(loss);
        }

        // Teleport player to spawn
        _player.GlobalPosition = SpawnPosition;

        ToastManager.Show("ARRESTED! Lost $" + (int)loss, ToastManager.ColorDanger);
        GD.Print("[PoliceCar] Player arrested! Lost $", loss.ToString("F0"));

        // Clear heat — this also despawns this police car via HeatSystem
        HeatSystem.Instance?.ClearHeat();
    }
}
