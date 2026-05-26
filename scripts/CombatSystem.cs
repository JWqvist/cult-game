using Godot;

public partial class CombatSystem : Node
{
    private const float MeleeRange = 80f;
    private const float MeleeDamage = 25f;
    private const float RangedDamage = 25f;
    private const float RayLength = 2000f;

    private CharacterBody2D _player;
    private Inventory _inventory;
    private float _heatAccumulator = 0f;

    public override void _Ready()
    {
        _player = GetParent<CharacterBody2D>();
        _inventory = _player.GetNode<Inventory>("Inventory");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("melee_attack"))
        {
            DoMelee();
        }
        else if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            DoRanged();
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("melee_attack"))
            DoMelee();
    }

    private void DoMelee()
    {
        NPC nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Node node in GetTree().GetNodesInGroup("npcs"))
        {
            if (node is NPC npc && IsInstanceValid(npc))
            {
                float dist = _player.GlobalPosition.DistanceTo(npc.GlobalPosition);
                if (dist < MeleeRange && dist < nearestDist)
                {
                    nearest = npc;
                    nearestDist = dist;
                }
            }
        }

        if (nearest != null)
        {
            nearest.TakeDamage(MeleeDamage);
            if (nearest is Pedestrian pedestrian)
                pedestrian.OnAttacked();
            RaiseHeat();
            GD.Print("[Combat] Melee hit: ", nearest.Name, " remaining HP: ", nearest.Health);
        }
    }

    private void DoRanged()
    {
        if (_inventory == null || !_inventory.IsGunEquipped()) return;

        Vector2 from = _player.GlobalPosition;
        Vector2 mousePos = _player.GetGlobalMousePosition();
        Vector2 direction = (mousePos - from).Normalized();
        Vector2 to = from + direction * RayLength;

        var spaceState = _player.GetWorld2D().DirectSpaceState;
        var exclude = new Godot.Collections.Array<Rid> { _player.GetRid() };
        var query = PhysicsRayQueryParameters2D.Create(from, to, exclude: exclude);

        var result = spaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            GodotObject collider = result["collider"].AsGodotObject();
            if (collider is NPC hitNpc && IsInstanceValid(hitNpc))
            {
                hitNpc.TakeDamage(RangedDamage);
                RaiseHeat();
                GD.Print("[Combat] Ranged hit: ", hitNpc.Name);
            }
        }
    }

    private void RaiseHeat()
    {
        _heatAccumulator += 0.5f;
        if (_heatAccumulator >= 1f)
        {
            GameManager.Instance?.IncreaseHeat(1);
            _heatAccumulator -= 1f;
        }
    }
}
