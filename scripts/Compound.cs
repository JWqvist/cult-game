using Godot;

/// <summary>
/// Player's cult compound. Visual state changes based on CultSize thresholds.
/// fence: always visible
/// tent: visible when CultSize >= 5
/// building: visible when CultSize >= 15
/// </summary>
public partial class Compound : StaticBody2D
{
    private Node2D _fence;
    private Node2D _tent;
    private Node2D _building;

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
    }
}
