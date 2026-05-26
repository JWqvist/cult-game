using Godot;
using System.Collections.Generic;

/// <summary>
/// Autoload singleton. Manages the player's Inner Circle (up to 3 followers with roles).
/// Roles: Recruiter (auto-recruits 1/60s), Enforcer (passive), Financier (1.5x donations each).
/// Loyalty decays 0.5/60s. Below 20 loyalty → 1%/s defection chance.
/// Tab key toggles the Inner Circle UI panel.
/// </summary>
public partial class InnerCircle : Node
{
    public static InnerCircle Instance { get; private set; }

    public List<Follower> InnerCircleMembers { get; } = new List<Follower>();
    private const int MaxMembers = 3;

    // Timing
    private float _recruiterTimer = 0f;
    private const float RecruiterInterval = 60f;

    // Loyalty
    private const float LoyaltyDecayPerSec = 0.5f / 60f;
    private const float DefectionThreshold = 20f;
    private const float DefectionChancePerSec = 0.01f;

    // Role counts (computed)
    public int RecruiterCount => InnerCircleMembers.FindAll(f => f.Role == FollowerRole.Recruiter).Count;
    public int EnforcerCount  => InnerCircleMembers.FindAll(f => f.Role == FollowerRole.Enforcer).Count;
    public int FinancierCount => InnerCircleMembers.FindAll(f => f.Role == FollowerRole.Financier).Count;
    public float DonationMultiplier => Mathf.Pow(1.5f, FinancierCount);

    // UI
    private CanvasLayer _canvas;
    private Panel _panel;
    private VBoxContainer _followerList;
    private bool _panelOpen = false;

    public override void _Ready()
    {
        Instance = this;
        BuildUI();
        GD.Print("[InnerCircle] Initialized.");
    }

    public override void _Process(double delta)
    {
        if (GameManager.Instance == null) return;
        float dt = (float)delta;

        // Loyalty decay and defection check for every follower
        var followers = GameManager.Instance.Followers;
        for (int i = followers.Count - 1; i >= 0; i--)
        {
            var f = followers[i];
            f.Loyalty = Mathf.Max(0f, f.Loyalty - LoyaltyDecayPerSec * dt);

            if (f.Loyalty < DefectionThreshold && GD.Randf() < DefectionChancePerSec * dt)
                Defect(f);
        }

        // Recruiter auto-recruit
        if (RecruiterCount > 0)
        {
            _recruiterTimer += dt;
            if (_recruiterTimer >= RecruiterInterval)
            {
                _recruiterTimer = 0f;
                GameManager.Instance.AddFollower();
                GD.Print("[InnerCircle] Recruiter auto-recruited a follower.");
            }
        }
        else
        {
            _recruiterTimer = 0f;
        }
    }

    public override void _Input(InputEvent ev)
    {
        if (ev is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Tab)
        {
            TogglePanel();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>Assign a role to a follower, adding them to IC if needed.</summary>
    public void AssignRole(Follower f, FollowerRole role)
    {
        if (role != FollowerRole.None && !InnerCircleMembers.Contains(f))
        {
            if (InnerCircleMembers.Count >= MaxMembers)
            {
                GD.Print("[InnerCircle] IC is full (max 3).");
                return;
            }
            InnerCircleMembers.Add(f);
        }
        else if (role == FollowerRole.None && InnerCircleMembers.Contains(f))
        {
            InnerCircleMembers.Remove(f);
        }

        f.Role = role;
        GD.Print($"[InnerCircle] {f.Name} → role: {role}");
    }

    /// <summary>Cycle a follower's role: None→Recruiter→Enforcer→Financier→None.</summary>
    public void CycleRole(Follower f)
    {
        FollowerRole next = f.Role switch
        {
            FollowerRole.None      => FollowerRole.Recruiter,
            FollowerRole.Recruiter => FollowerRole.Enforcer,
            FollowerRole.Enforcer  => FollowerRole.Financier,
            FollowerRole.Financier => FollowerRole.None,
            _                      => FollowerRole.None,
        };

        // Capacity check before promoting into IC
        if (next != FollowerRole.None && !InnerCircleMembers.Contains(f) && InnerCircleMembers.Count >= MaxMembers)
        {
            GD.Print("[InnerCircle] IC full — cannot assign role.");
            return;
        }

        AssignRole(f, next);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void Defect(Follower f)
    {
        GD.Print($"[InnerCircle] {f.Name} defected! (loyalty {f.Loyalty:F1})");

        if (InnerCircleMembers.Contains(f))
            InnerCircleMembers.Remove(f);

        GameManager.Instance.RemoveFollower(f);
        HeatSystem.Instance?.AddHeat(0.3f);
    }

    private void TogglePanel()
    {
        _panelOpen = !_panelOpen;
        _panel.Visible = _panelOpen;
        if (_panelOpen)
            PopulateList();
    }

    private void BuildUI()
    {
        _canvas = new CanvasLayer();
        _canvas.Layer = 30;
        AddChild(_canvas);

        // Centered panel 500×400
        _panel = new Panel();
        _panel.AnchorLeft   = 0.5f;
        _panel.AnchorRight  = 0.5f;
        _panel.AnchorTop    = 0.5f;
        _panel.AnchorBottom = 0.5f;
        _panel.OffsetLeft   = -250f;
        _panel.OffsetRight  =  250f;
        _panel.OffsetTop    = -200f;
        _panel.OffsetBottom =  200f;
        _panel.Visible = false;
        _canvas.AddChild(_panel);

        // VBox filling the panel (with margin)
        var vbox = new VBoxContainer();
        vbox.AnchorLeft   = 0f;
        vbox.AnchorRight  = 1f;
        vbox.AnchorTop    = 0f;
        vbox.AnchorBottom = 1f;
        vbox.OffsetLeft   =  10f;
        vbox.OffsetRight  = -10f;
        vbox.OffsetTop    =  10f;
        vbox.OffsetBottom = -10f;
        _panel.AddChild(vbox);

        var title = new Label();
        title.Text = "=== INNER CIRCLE (Tab to close) ===";
        vbox.AddChild(title);

        var hint = new Label();
        hint.Text = "Click follower to cycle role. IC slots: 3 max.";
        vbox.AddChild(hint);

        vbox.AddChild(new HSeparator());

        var scroll = new ScrollContainer();
        scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(scroll);

        _followerList = new VBoxContainer();
        _followerList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_followerList);
    }

    private void PopulateList()
    {
        foreach (Node child in _followerList.GetChildren())
            child.QueueFree();

        if (GameManager.Instance == null) return;

        var followers = GameManager.Instance.Followers;
        if (followers.Count == 0)
        {
            var empty = new Label();
            empty.Text = "No followers recruited yet.";
            _followerList.AddChild(empty);
            return;
        }

        foreach (var follower in followers)
        {
            var f = follower; // capture for lambda
            bool inIC = InnerCircleMembers.Contains(f);
            string icTag = inIC ? " [IC]" : "";

            var btn = new Button();
            btn.Text = $"{f.Name}{icTag}  |  Loyalty: {f.Loyalty:F0}  |  Role: {f.Role}";
            btn.Pressed += () =>
            {
                CycleRole(f);
                PopulateList();
            };
            _followerList.AddChild(btn);
        }
    }
}
