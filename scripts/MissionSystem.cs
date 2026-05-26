using Godot;
using System.Collections.Generic;

public enum ObjectiveType { RecruitN, EarnMoney, MugN, InnerCircleN, ReachHeatN }

public class Mission
{
    public string Description;
    public ObjectiveType Type;
    public int Target;
    public float Reward;
}

/// <summary>
/// Autoload singleton. Tracks one active mission at a time.
/// Objectives: RecruitN (followers), EarnMoney (total accumulated), MugN (NPCs killed).
/// On completion awards money and unlocks the next mission.
/// </summary>
public partial class MissionSystem : Node
{
    public static MissionSystem Instance { get; private set; }

    private List<Mission> _missions = new List<Mission>();
    private int _missionIndex = 0;
    public Mission ActiveMission => _missionIndex < _missions.Count ? _missions[_missionIndex] : null;

    /// <summary>Number of missions completed so far.</summary>
    public int CompletedMissions => _missionIndex;

    // Objective tracking
    private int _mugCount = 0;
    private float _totalEarned = 0f;
    private float _lastMoney = 0f;
    private int _maxHeatReached = 0;

    // "COMPLETE!" flash timer
    private float _completeTimer = 0f;
    private const float CompleteDuration = 3f;

    // UI
    private CanvasLayer _canvas;
    private Panel _completePanel;
    private Label _completeLabel;

    public string MissionStatusText
    {
        get
        {
            if (ActiveMission == null) return "No active mission";
            return $"Mission: {ActiveMission.Description}";
        }
    }

    public override void _Ready()
    {
        Instance = this;
        BuildMissions();
        BuildUI();
        GD.Print("[MissionSystem] Initialized. Active: ", ActiveMission?.Description);
    }

    public override void _Process(double delta)
    {
        if (_completeTimer > 0f)
        {
            _completeTimer -= (float)delta;
            if (_completeTimer <= 0f)
                _completePanel.Visible = false;
        }

        if (GameManager.Instance == null) return;

        // Accumulate total money earned (tracks all income sources passively)
        float currentMoney = GameManager.Instance.Money;
        if (currentMoney > _lastMoney)
            _totalEarned += currentMoney - _lastMoney;
        _lastMoney = currentMoney;

        // Track peak heat for ReachHeatN objectives
        if (HeatSystem.Instance != null)
        {
            int stars = HeatSystem.Instance.WantedStars;
            if (stars > _maxHeatReached)
                _maxHeatReached = stars;
        }

        CheckObjective();
    }

    /// <summary>Call this whenever the player kills/mugs an NPC.</summary>
    public void ReportMug()
    {
        _mugCount++;
        GD.Print($"[MissionSystem] Mug reported. Total: {_mugCount}");
        CheckObjective();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void CheckObjective()
    {
        if (ActiveMission == null) return;

        bool complete = ActiveMission.Type switch
        {
            ObjectiveType.RecruitN      => GameManager.Instance.Followers.Count >= ActiveMission.Target,
            ObjectiveType.EarnMoney     => _totalEarned >= (float)ActiveMission.Target,
            ObjectiveType.MugN          => _mugCount >= ActiveMission.Target,
            ObjectiveType.InnerCircleN  => InnerCircle.Instance != null &&
                                           InnerCircle.Instance.InnerCircleMembers.Count >= ActiveMission.Target,
            ObjectiveType.ReachHeatN    => _maxHeatReached >= ActiveMission.Target,
            _                           => false
        };

        if (complete)
            CompleteMission();
    }

    private void CompleteMission()
    {
        Mission m = ActiveMission;
        GD.Print($"[MissionSystem] Complete: {m.Description}  Reward: ${m.Reward}");

        GameManager.Instance.AddMoney(m.Reward);

        // Street Taxes gives +1 heat
        if (m.Type == ObjectiveType.MugN)
            HeatSystem.Instance?.AddHeat(1f);

        _missionIndex++;

        ShowCompletePopup(m);
    }

    private void ShowCompletePopup(Mission completed)
    {
        string nextLine = ActiveMission != null
            ? $"\nNext: {ActiveMission.Description}"
            : "\nAll missions complete!";

        _completeLabel.Text = $"MISSION COMPLETE!\n{completed.Description}\nReward: +${completed.Reward}{nextLine}";
        _completePanel.Visible = true;
        _completeTimer = CompleteDuration;
    }

    private void BuildMissions()
    {
        _missions.Add(new Mission
        {
            Description = "First Contact — Recruit 3 followers",
            Type        = ObjectiveType.RecruitN,
            Target      = 3,
            Reward      = 50f
        });
        _missions.Add(new Mission
        {
            Description = "Seed Money — Earn $200 total",
            Type        = ObjectiveType.EarnMoney,
            Target      = 200,
            Reward      = 100f
        });
        _missions.Add(new Mission
        {
            Description = "Street Taxes — Mug 5 people",
            Type        = ObjectiveType.MugN,
            Target      = 5,
            Reward      = 150f
        });
        // Sprint 9: extended mission chain
        _missions.Add(new Mission
        {
            Description = "Inner Sanctum — Fill 2 Inner Circle slots",
            Type        = ObjectiveType.InnerCircleN,
            Target      = 2,
            Reward      = 300f
        });
        _missions.Add(new Mission
        {
            Description = "Most Wanted — Reach 2 wanted stars",
            Type        = ObjectiveType.ReachHeatN,
            Target      = 2,
            Reward      = 400f
        });
        _missions.Add(new Mission
        {
            Description = "True Believers — Recruit 50 followers",
            Type        = ObjectiveType.RecruitN,
            Target      = 50,
            Reward      = 750f
        });
        _missions.Add(new Mission
        {
            Description = "War Chest — Accumulate $5,000",
            Type        = ObjectiveType.EarnMoney,
            Target      = 5000,
            Reward      = 1000f
        });
        _missions.Add(new Mission
        {
            Description = "The Purge — Mug 20 people",
            Type        = ObjectiveType.MugN,
            Target      = 20,
            Reward      = 500f
        });
    }

    private void BuildUI()
    {
        _canvas = new CanvasLayer();
        _canvas.Layer = 40;
        AddChild(_canvas);

        // Centered flash panel
        _completePanel = new Panel();
        _completePanel.AnchorLeft   = 0.5f;
        _completePanel.AnchorRight  = 0.5f;
        _completePanel.AnchorTop    = 0.3f;
        _completePanel.AnchorBottom = 0.3f;
        _completePanel.OffsetLeft   = -220f;
        _completePanel.OffsetRight  =  220f;
        _completePanel.OffsetTop    = -70f;
        _completePanel.OffsetBottom =  70f;
        _completePanel.Visible = false;
        _canvas.AddChild(_completePanel);

        _completeLabel = new Label();
        _completeLabel.AnchorLeft   = 0f;
        _completeLabel.AnchorRight  = 1f;
        _completeLabel.AnchorTop    = 0f;
        _completeLabel.AnchorBottom = 1f;
        _completeLabel.OffsetLeft   =  10f;
        _completeLabel.OffsetRight  = -10f;
        _completeLabel.OffsetTop    =  10f;
        _completeLabel.OffsetBottom = -10f;
        _completeLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        _completeLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _completePanel.AddChild(_completeLabel);
    }
}
