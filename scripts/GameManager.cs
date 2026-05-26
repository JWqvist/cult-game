using Godot;
using System.Collections.Generic;

/// <summary>
/// Singleton autoload — tracks global player stats.
/// Access via: GameManager.Instance
/// CultSize = 1 (player) + Followers.Count
/// </summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    // Player stats
    public float Money { get; private set; } = 0f;
    public int CultSize => 1 + Followers.Count;   // player + recruited followers
    public int HeatLevel { get; private set; } = 0; // 0-5, police attention

    // All recruited followers
    public List<Follower> Followers { get; private set; } = new List<Follower>();

    // Donations: $5 per cult member per second, boosted by Financiers
    public float DonationsPerDay => CultSize * 5.0f * (InnerCircle.Instance?.DonationMultiplier ?? 1f);

    [Signal]
    public delegate void StatsChangedEventHandler();

    public override void _Ready()
    {
        Instance = this;
        GD.Print("[GameManager] Initialized. Money=", Money, " CultSize=", CultSize, " Heat=", HeatLevel);
    }

    public override void _Process(double delta)
    {
        float multiplier = InnerCircle.Instance?.DonationMultiplier ?? 1f;
        Money += CultSize * 5.0f * multiplier * (float)delta;
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
        var follower = new Follower();
        Followers.Add(follower);
        EmitSignal(SignalName.StatsChanged);
        GD.Print("[GameManager] Recruited ", follower.Name, ". Cult size: ", CultSize);
    }

    public void RemoveFollower(Follower follower)
    {
        if (Followers.Remove(follower))
        {
            EmitSignal(SignalName.StatsChanged);
            GD.Print("[GameManager] Lost follower ", follower.Name, ". Cult size: ", CultSize);
        }
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
