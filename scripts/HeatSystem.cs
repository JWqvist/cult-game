using Godot;

/// <summary>
/// Autoload singleton. Tracks heat level 0-5 from crimes.
/// Spawns PoliceCar when heat >= 2, despawns when heat < 1.
/// Heat decays 0.02/sec passively. ClearHeat() when player hides in compound.
/// </summary>
public partial class HeatSystem : Node
{
    public static HeatSystem Instance { get; private set; }

    public float HeatLevel { get; private set; } = 0f;
    public int WantedStars => Mathf.Clamp(Mathf.CeilToInt(HeatLevel / 1.67f), 0, 3);

    private const float DecayRate = 0.02f;
    private float _decayMultiplier = 1.0f;
    private const float SpawnThreshold = 2.0f;
    private const float DespawnThreshold = 1.0f;
    private const float BribeRange = 120f;
    private const float BribeCost = 50f;

    private PackedScene _policeCarScene;
    private CharacterBody2D _policeCar;
    private bool _policeSpawned = false;

    [Signal]
    public delegate void HeatChangedEventHandler();

    public override void _Ready()
    {
        Instance = this;
        _policeCarScene = GD.Load<PackedScene>("res://scenes/PoliceCar.tscn");
        GD.Print("[HeatSystem] Initialized.");
    }

    public override void _Process(double delta)
    {
        if (HeatLevel > 0f)
        {
            HeatLevel = Mathf.Max(0f, HeatLevel - DecayRate * _decayMultiplier * (float)delta);
            EmitSignal(SignalName.HeatChanged);
        }

        if (!_policeSpawned && HeatLevel >= SpawnThreshold)
            SpawnPoliceCar();
        else if (_policeSpawned && HeatLevel < DespawnThreshold)
            DespawnPoliceCar();

        // Bribe check: B key when near police
        if (_policeSpawned && _policeCar != null && IsInstanceValid(_policeCar) && Input.IsActionJustPressed("bribe"))
        {
            Node2D player = GetTree().GetFirstNodeInGroup("player") as Node2D;
            if (player != null && player.GlobalPosition.DistanceTo(_policeCar.GlobalPosition) <= BribeRange)
                Bribe();
        }
    }

    public void AddHeat(float amount)
    {
        int oldStars = WantedStars;
        HeatLevel = Mathf.Clamp(HeatLevel + amount, 0f, 5f);
        EmitSignal(SignalName.HeatChanged);
        GD.Print("[HeatSystem] Heat: ", HeatLevel.ToString("F2"), " Stars: ", WantedStars);

        int newStars = WantedStars;
        if (newStars > oldStars)
            ToastManager.Show("Heat! " + new string('\u2605', newStars), ToastManager.ColorDanger);
    }

    /// <summary>Set a multiplier on passive heat decay (Waco path: 0.5 = slower decay = more aggressive police).</summary>
    public void SetDecayMultiplier(float multiplier)
    {
        _decayMultiplier = Mathf.Max(0f, multiplier);
        GD.Print($"[HeatSystem] DecayMultiplier set to {_decayMultiplier:F2}");
    }

    public void ClearHeat()
    {
        HeatLevel = 0f;
        DespawnPoliceCar();
        EmitSignal(SignalName.HeatChanged);
        GD.Print("[HeatSystem] Heat cleared (compound).");
    }

    public void Bribe()
    {
        if (GameManager.Instance == null || GameManager.Instance.Money < BribeCost) return;
        GameManager.Instance.SpendMoney(BribeCost);
        HeatLevel = Mathf.Max(0f, HeatLevel - 1.0f);
        EmitSignal(SignalName.HeatChanged);
        GD.Print("[HeatSystem] Bribed! Heat now: ", HeatLevel.ToString("F2"));
    }

    private void SpawnPoliceCar()
    {
        if (_policeCarScene == null) return;
        Node world = GetTree().Root.GetNodeOrNull("World");
        if (world == null) return;

        _policeCar = (CharacterBody2D)_policeCarScene.Instantiate();
        _policeCar.GlobalPosition = GetRandomEdgePosition();
        world.AddChild(_policeCar);
        _policeSpawned = true;
        GD.Print("[HeatSystem] Police spawned at ", _policeCar.GlobalPosition);
    }

    private void DespawnPoliceCar()
    {
        if (_policeCar != null && IsInstanceValid(_policeCar))
            _policeCar.QueueFree();
        _policeCar = null;
        _policeSpawned = false;
        GD.Print("[HeatSystem] Police despawned.");
    }

    private Vector2 GetRandomEdgePosition()
    {
        int edge = (int)(GD.Randf() * 4);
        float t = GD.Randf();
        return edge switch
        {
            0 => new Vector2(Mathf.Lerp(-800f, 800f, t), -800f),
            1 => new Vector2(Mathf.Lerp(-800f, 800f, t), 800f),
            2 => new Vector2(-800f, Mathf.Lerp(-800f, 800f, t)),
            _ => new Vector2(800f, Mathf.Lerp(-800f, 800f, t)),
        };
    }
}
