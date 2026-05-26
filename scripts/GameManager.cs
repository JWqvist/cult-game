using Godot;

/// <summary>
/// Singleton autoload — tracks global player stats.
/// Access via: GameManager.Instance
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    // Player stats
    public float Money { get; private set; } = 0f;
    public int CultSize { get; private set; } = 1;  // starts with the player
    public int HeatLevel { get; private set; } = 0; // 0-5, police attention

    // Donations: $5 per follower per second (simplified day cycle)
    public float DonationsPerDay => CultSize * 5.0f;

    [Signal]
    public delegate void StatsChangedEventHandler();

    public override void _Ready()
    {
        Instance = this;
        GD.Print("[GameManager] Initialized. Money=", Money, " CultSize=", CultSize, " Heat=", HeatLevel);
    }

    public override void _Process(double delta)
    {
        // Followers donate $5/follower/second (simplified)
        Money += CultSize * 5.0f * (float)delta;
        EmitSignal(SignalName.StatsChanged);
    }

    public void AddMoney(float amount)
    {
        Money += amount;
        EmitSignal(SignalName.StatsChanged);
    }

    public void SpendMoney(float amount)
    {
        Money = Mathf.Max(0f, Money - amount);
        EmitSignal(SignalName.StatsChanged);
    }

    public void AddFollower()
    {
        CultSize++;
        EmitSignal(SignalName.StatsChanged);
        GD.Print("[GameManager] Cult size is now: ", CultSize);
    }

    public void RemoveFollower()
    {
        CultSize = Mathf.Max(1, CultSize - 1);
        EmitSignal(SignalName.StatsChanged);
    }

    public void IncreaseHeat(int amount = 1)
    {
        HeatLevel = Mathf.Clamp(HeatLevel + amount, 0, 5);
        EmitSignal(SignalName.StatsChanged);
        GD.Print("[GameManager] Heat level: ", HeatLevel);
    }

    public void DecreaseHeat(int amount = 1)
    {
        HeatLevel = Mathf.Clamp(HeatLevel - amount, 0, 5);
        EmitSignal(SignalName.StatsChanged);
    }
}
