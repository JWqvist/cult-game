using Godot;

public partial class NPC : CharacterBody2D
{
    [Export] public float Health = 100f;

    public override void _Ready()
    {
        AddToGroup("npcs");
    }

    public virtual void TakeDamage(float amount)
    {
        Health -= amount;
        if (Health <= 0f)
            QueueFree();
    }
}
