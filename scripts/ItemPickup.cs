using Godot;

/// <summary>
/// Ground item the player walks over to collect. Placed in the world or
/// dropped by dead NPCs. Draws a simple colored marker so it is visible
/// without dedicated sprite assets (gun = orange, melee = cyan).
/// </summary>
public partial class ItemPickup : Area2D
{
    [Export] public string ItemType = "MeleeWeapon";

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Color color = ItemType == "Gun"
            ? new Color(1.0f, 0.55f, 0.1f)    // orange
            : new Color(0.2f, 0.8f, 0.9f);    // cyan

        // Filled marker with a dark outline so it reads on any background.
        DrawRect(new Rect2(-8, -8, 16, 16), color);
        DrawRect(new Rect2(-8, -8, 16, 16), new Color(0f, 0f, 0f, 0.8f), false, 2f);
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            player.Inventory.PickupItem(ItemType);
            ToastManager.Show("Picked up " + ItemType, Colors.White);
            QueueFree();
        }
    }
}
