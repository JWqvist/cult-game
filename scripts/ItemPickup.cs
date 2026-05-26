using Godot;

public partial class ItemPickup : Area2D
{
    [Export] public string ItemType = "MeleeWeapon";

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            player.Inventory.PickupItem(ItemType);
            QueueFree();
        }
    }
}
