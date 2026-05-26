using Godot;

public partial class Inventory : Node
{
    [Export] public int CurrentSlot = 0;

    // Slot 0: Fists (always), Slot 1: MeleeWeapon, Slot 2: Gun
    private readonly string[] _slots = new string[] { "Fists", "", "" };

    public string EquippedItem => _slots[CurrentSlot] != "" ? _slots[CurrentSlot] : "Fists";

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("slot_1"))
            CurrentSlot = 0;
        else if (@event.IsActionPressed("slot_2"))
            CurrentSlot = 1;
        else if (@event.IsActionPressed("slot_3"))
            CurrentSlot = 2;
    }

    public void PickupItem(string itemType)
    {
        if (itemType == "MeleeWeapon")
            _slots[1] = "MeleeWeapon";
        else if (itemType == "Gun")
            _slots[2] = "Gun";

        GD.Print("[Inventory] Picked up: ", itemType);
    }

    public bool IsGunEquipped() => CurrentSlot == 2 && _slots[2] == "Gun";
}
