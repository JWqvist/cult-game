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

    // Legacy 0-5 heat accessor. HeatSystem is now the single source of truth
    // (float scale, police spawning, decay); this mirrors it as a rounded int
    // so older callers/readers stay consistent.
    public int HeatLevel => HeatSystem.Instance != null
        ? Mathf.Clamp(Mathf.RoundToInt(HeatSystem.Instance.HeatLevel), 0, 5)
        : 0;

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
        if (amount >= 50f)
            ToastManager.Show("+$" + (int)amount, ToastManager.ColorMoney);
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
        // Delegates to the authoritative HeatSystem so police respond to heat
        // raised through the legacy API.
        HeatSystem.Instance?.AddHeat(amount);
        EmitSignal(SignalName.StatsChanged);
        GD.Print("[GameManager] Heat level: ", HeatLevel);
    }

    public void DecreaseHeat(int amount = 1)
    {
        HeatSystem.Instance?.AddHeat(-amount);
        EmitSignal(SignalName.StatsChanged);
    }
}
