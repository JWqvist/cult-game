using Godot;

public partial class CombatSystem : Node
{
    private const float MeleeRange = 80f;
    private const float FistDamage = 25f;
    private const float MeleeWeaponDamage = 55f;
    private const float RangedDamage = 25f;
    private const float RayLength = 2000f;

    private CharacterBody2D _player;
    private Inventory _inventory;
    private Sprite2D _playerSprite;

    public override void _Ready()
    {
        _player = GetParent<CharacterBody2D>();
        _inventory = _player.GetNode<Inventory>("Inventory");
        _playerSprite = _player.GetNodeOrNull<Sprite2D>("Sprite2D");
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



    private const float FleeAlertRadius = 250f;

    private void DoMelee()
    {
        // Visible swing/punch feedback even without authored animation assets.
        PlayMeleeSwing();

        // Melee weapon (slot 1, equipped) hits harder than fists.
        bool meleeWeaponEquipped =
            _inventory != null &&
            _inventory.CurrentSlot == 1 &&
            _inventory.GetSlotLabel(1) == "MeleeWeapon";
        float damage = meleeWeaponEquipped ? MeleeWeaponDamage : FistDamage;

        NPC nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Node node in GetTree().GetNodesInGroup("npcs"))
        {
            if (node is NPC npc && IsInstanceValid(npc) && !npc.IsDead)
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
            nearest.TakeDamage(damage);
            RaiseHeat();
            AlertNearbyPedestrians(_player.GlobalPosition);
            string weapon = meleeWeaponEquipped ? "MeleeWeapon" : "Fists";
            GD.Print("[Combat] Melee (", weapon, ") hit: ", nearest.Name,
                     "  dmg: ", damage, "  remaining HP: ", nearest.Health);
        }
        else
        {
            GD.Print("[Combat] Melee swing — no target in range.");
        }
    }

    /// <summary>Quick scale/lunge pulse on the player sprite so a punch is visible.</summary>
    private void PlayMeleeSwing()
    {
        if (_playerSprite == null || !IsInstanceValid(_playerSprite)) return;
        Vector2 baseScale = _playerSprite.Scale;
        var tween = CreateTween();
        tween.TweenProperty(_playerSprite, "scale", baseScale * 1.25f, 0.06f);
        tween.TweenProperty(_playerSprite, "scale", baseScale, 0.10f);
    }

    private void DoRanged()
    {
        if (_inventory == null || !_inventory.IsGunEquipped())
        {
            return;
        }

        Vector2 from = _player.GlobalPosition;
        Vector2 mousePos = _player.GetGlobalMousePosition();
        Vector2 direction = (mousePos - from).Normalized();
        Vector2 to = from + direction * RayLength;

        var spaceState = _player.GetWorld2D().DirectSpaceState;
        var exclude = new Godot.Collections.Array<Rid> { _player.GetRid() };
        var query = PhysicsRayQueryParameters2D.Create(from, to, exclude: exclude);

        Vector2 tracerEnd = to;
        var result = spaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            tracerEnd = (Vector2)result["position"];
            GodotObject collider = result["collider"].AsGodotObject();
            if (collider is NPC hitNpc && IsInstanceValid(hitNpc) && !hitNpc.IsDead)
            {
                hitNpc.TakeDamage(RangedDamage);
                AlertNearbyPedestrians(_player.GlobalPosition);
                GD.Print("[Combat] Ranged hit: ", hitNpc.Name, "  remaining HP: ", hitNpc.Health);
            }
        }
        else
        {
            GD.Print("[Combat] Ranged shot — missed.");
        }

        RaiseHeat();
        SpawnTracer(from, tracerEnd);
    }

    /// <summary>Short-lived yellow tracer line so gunfire is visible.</summary>
    private void SpawnTracer(Vector2 from, Vector2 to)
    {
        Node parent = _player.GetParent();
        if (parent == null) return;

        var line = new Line2D();
        line.Width = 2f;
        line.DefaultColor = new Color(1f, 0.9f, 0.3f, 0.9f);
        line.ZIndex = 50;
        line.AddPoint(from);
        line.AddPoint(to);
        parent.AddChild(line);

        var tween = line.CreateTween();
        tween.TweenProperty(line, "modulate:a", 0f, 0.12f);
        tween.TweenCallback(Callable.From(line.QueueFree));
    }

    private void AlertNearbyPedestrians(Vector2 origin)
    {
        foreach (Node node in GetTree().GetNodesInGroup("pedestrians"))
        {
            if (node is Pedestrian ped && IsInstanceValid(ped))
            {
                if (origin.DistanceTo(ped.GlobalPosition) < FleeAlertRadius)
                    ped.OnAttacked();
            }
        }
    }

    /// <summary>
    /// Violence is a crime: feed the authoritative HeatSystem so police respond.
    /// Each attack adds a fractional amount; HeatSystem owns the 0-5 scale,
    /// police spawning and decay. (Previously this only bumped the legacy
    /// GameManager.HeatLevel counter, which never triggered a police response.)
    /// </summary>
    private const float HeatPerAttack = 0.4f;

    private void RaiseHeat()
    {
        HeatSystem.Instance?.AddHeat(HeatPerAttack);
    }
}
