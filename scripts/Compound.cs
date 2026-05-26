using Godot;

/// <summary>
/// Player's cult compound. Visual state changes based on CultSize thresholds.
/// fence: always visible
/// tent: visible when CultSize >= 5
/// building: visible when CultSize >= 15
/// Clears heat when player is inside (within HideRange).
/// </summary>
public partial class Compound : StaticBody2D
{
    private const float HideRange = 150f;

    private Node2D _fence;
    private Node2D _tent;
    private Node2D _building;
    private Node2D _player;

    public override void _Ready()
    {
        _fence = GetNode<Node2D>("fence");
        _tent = GetNode<Node2D>("tent");
        _building = GetNode<Node2D>("building");
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance == null) return;

        int cultSize = GameManager.Instance.CultSize;
        _fence.Visible = true;
        _tent.Visible = cultSize >= 5;
        _building.Visible = cultSize >= 15;

        // Lazily find player
        if (_player == null)
        {
            Node node = GetTree().GetFirstNodeInGroup("player");
            if (node is Node2D n2d)
                _player = n2d;
        }

        // Clear heat when player hides inside compound
        if (_player != null && HeatSystem.Instance != null && HeatSystem.Instance.HeatLevel > 0f)
        {
            if (GlobalPosition.DistanceTo(_player.GlobalPosition) <= HideRange)
                HeatSystem.Instance.ClearHeat();
        }
    }
}
