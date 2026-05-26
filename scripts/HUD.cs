using Godot;

/// <summary>
/// GTA-inspired HUD. Built entirely in code; the tscn only needs the CanvasLayer node.
///
/// Layout:
///   Top-left  — health bar + money
///   Top-right — followers + inner circle
///   Bottom-center — wanted stars (hidden when heat = 0)
///   Bottom-left — 3 inventory slots
///   Top-center  — active mission text
/// </summary>
public partial class HUD : CanvasLayer
{
    private static readonly Color Gold    = new Color(1.00f, 0.84f, 0.00f);
    private static readonly Color DarkBg  = new Color(0.00f, 0.00f, 0.00f, 0.60f);
    private static readonly Color HealthR = new Color(0.90f, 0.10f, 0.10f);
    private static readonly Color StarOn  = new Color(1.00f, 0.84f, 0.00f);
    private static readonly Color StarOff = new Color(0.35f, 0.35f, 0.35f);

    private Player _player;

    // Top-left
    private ProgressBar _healthBar;
    private Label _moneyLabel;

    // Top-right
    private Label _followersLabel;
    private Label _icLabel;

    // Bottom-center stars
    private PanelContainer _starsPanel;
    private Label[] _stars = new Label[3];

    // Bottom-left inventory
    private PanelContainer[] _slotPanels = new PanelContainer[3];
    private Label[]          _slotLabels = new Label[3];
    private StyleBoxFlat[]   _slotStyles = new StyleBoxFlat[3];

    // Mission (top-center)
    private Label _missionLabel;

    public override void _Ready()
    {
        Layer = 10;
        BuildTopLeft();
        BuildTopRight();
        BuildStars();
        BuildSlots();
        BuildMission();
    }

    public override void _Process(double delta)
    {
        if (_player == null)
        {
            if (GetTree().GetFirstNodeInGroup("player") is Player p)
                _player = p;
            return;
        }

        UpdateHealth();
        UpdateMoney();
        UpdateFollowers();
        UpdateStars();
        UpdateSlots();
        UpdateMission();
    }

    // ── Update methods ───────────────────────────────────────────────────────

    private void UpdateHealth()
    {
        _healthBar.Value = _player.Health;
    }

    private void UpdateMoney()
    {
        if (GameManager.Instance != null)
            _moneyLabel.Text = ((int)GameManager.Instance.Money).ToString("N0");
    }

    private void UpdateFollowers()
    {
        if (GameManager.Instance != null)
            _followersLabel.Text = "\u263B  " + GameManager.Instance.CultSize;

        if (InnerCircle.Instance != null)
        {
            int r = InnerCircle.Instance.RecruiterCount;
            int e = InnerCircle.Instance.EnforcerCount;
            int f = InnerCircle.Instance.FinancierCount;
            _icLabel.Text = $"IC  R:{r}  E:{e}  F:{f}";
        }
    }

    private void UpdateStars()
    {
        if (HeatSystem.Instance == null) return;
        int s = HeatSystem.Instance.WantedStars;
        _starsPanel.Visible = s > 0;
        for (int i = 0; i < 3; i++)
            _stars[i].AddThemeColorOverride("font_color", i < s ? StarOn : StarOff);
    }

    private void UpdateSlots()
    {
        var inv = _player.Inventory;
        if (inv == null) return;

        string[] names = { "Fists", inv.GetSlotLabel(1), inv.GetSlotLabel(2) };

        for (int i = 0; i < 3; i++)
        {
            _slotLabels[i].Text = (i + 1) + "\n" + (names[i] == "" ? "—" : names[i]);
            bool active = inv.CurrentSlot == i;
            _slotStyles[i].BgColor     = active ? new Color(0.28f, 0.22f, 0.00f, 0.85f) : DarkBg;
            _slotStyles[i].BorderColor = active ? Gold : new Color(0.45f, 0.45f, 0.45f);
            _slotStyles[i].BorderWidthLeft = _slotStyles[i].BorderWidthRight =
            _slotStyles[i].BorderWidthTop  = _slotStyles[i].BorderWidthBottom = active ? 2 : 1;
            _slotLabels[i].AddThemeColorOverride("font_color", active ? Gold : new Color(0.65f, 0.65f, 0.65f));
        }
    }

    private void UpdateMission()
    {
        if (MissionSystem.Instance == null) return;
        string s = MissionSystem.Instance.MissionStatusText;
        _missionLabel.Text    = s;
        _missionLabel.Visible = !string.IsNullOrEmpty(s);
    }

    // ── UI construction ──────────────────────────────────────────────────────

    private void BuildTopLeft()
    {
        var panel = MakePanel(10, 10, 245, 76);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 5);
        panel.AddChild(vbox);

        // Health row
        var healthRow = new HBoxContainer();
        healthRow.AddThemeConstantOverride("separation", 6);
        vbox.AddChild(healthRow);

        var heart = new Label();
        heart.Text = "\u2665";
        heart.AddThemeColorOverride("font_color", HealthR);
        heart.AddThemeFontSizeOverride("font_size", 18);
        healthRow.AddChild(heart);

        _healthBar = new ProgressBar();
        _healthBar.CustomMinimumSize = new Vector2(186, 18);
        _healthBar.MaxValue = 100.0;
        _healthBar.Value    = 100.0;
        _healthBar.ShowPercentage = false;

        var fillStyle = new StyleBoxFlat();
        fillStyle.BgColor = HealthR;
        _healthBar.AddThemeStyleboxOverride("fill", fillStyle);

        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.28f, 0.00f, 0.00f);
        _healthBar.AddThemeStyleboxOverride("background", bgStyle);
        healthRow.AddChild(_healthBar);

        // Money row
        var moneyRow = new HBoxContainer();
        moneyRow.AddThemeConstantOverride("separation", 3);
        vbox.AddChild(moneyRow);

        var dollar = new Label();
        dollar.Text = "$";
        dollar.AddThemeColorOverride("font_color", Gold);
        dollar.AddThemeFontSizeOverride("font_size", 22);
        moneyRow.AddChild(dollar);

        _moneyLabel = new Label();
        _moneyLabel.Text = "0";
        _moneyLabel.AddThemeColorOverride("font_color", Gold);
        _moneyLabel.AddThemeFontSizeOverride("font_size", 22);
        moneyRow.AddChild(_moneyLabel);
    }

    private void BuildTopRight()
    {
        var panel = MakePanel(1026, 10, 244, 70);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        panel.AddChild(vbox);

        _followersLabel = new Label();
        _followersLabel.Text = "\u263B  1";
        _followersLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.92f, 1.00f));
        _followersLabel.AddThemeFontSizeOverride("font_size", 15);
        vbox.AddChild(_followersLabel);

        _icLabel = new Label();
        _icLabel.Text = "IC  R:0  E:0  F:0";
        _icLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.65f, 0.65f));
        _icLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_icLabel);
    }

    private void BuildStars()
    {
        _starsPanel = MakePanel(540, 668, 200, 42);
        _starsPanel.Visible = false;
        AddChild(_starsPanel);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);
        _starsPanel.AddChild(hbox);

        for (int i = 0; i < 3; i++)
        {
            _stars[i] = new Label();
            _stars[i].Text = "\u2605";  // ★
            _stars[i].AddThemeColorOverride("font_color", StarOff);
            _stars[i].AddThemeFontSizeOverride("font_size", 26);
            hbox.AddChild(_stars[i]);
        }
    }

    private void BuildSlots()
    {
        var hbox = new HBoxContainer();
        hbox.OffsetLeft   = 10f;
        hbox.OffsetTop    = 638f;
        hbox.OffsetRight  = 220f;
        hbox.OffsetBottom = 710f;
        hbox.AddThemeConstantOverride("separation", 6);
        AddChild(hbox);

        for (int i = 0; i < 3; i++)
        {
            _slotStyles[i] = new StyleBoxFlat();
            _slotStyles[i].BgColor = DarkBg;
            _slotStyles[i].BorderColor = new Color(0.45f, 0.45f, 0.45f);
            _slotStyles[i].BorderWidthLeft = _slotStyles[i].BorderWidthRight =
            _slotStyles[i].BorderWidthTop  = _slotStyles[i].BorderWidthBottom = 1;
            _slotStyles[i].CornerRadiusTopLeft = _slotStyles[i].CornerRadiusTopRight =
            _slotStyles[i].CornerRadiusBottomLeft = _slotStyles[i].CornerRadiusBottomRight = 3;
            _slotStyles[i].ContentMarginLeft = _slotStyles[i].ContentMarginRight = 4f;
            _slotStyles[i].ContentMarginTop  = _slotStyles[i].ContentMarginBottom = 4f;

            _slotPanels[i] = new PanelContainer();
            _slotPanels[i].CustomMinimumSize = new Vector2(60, 52);
            _slotPanels[i].AddThemeStyleboxOverride("panel", _slotStyles[i]);
            hbox.AddChild(_slotPanels[i]);

            _slotLabels[i] = new Label();
            _slotLabels[i].Text = (i + 1) + "\n—";
            _slotLabels[i].HorizontalAlignment = HorizontalAlignment.Center;
            _slotLabels[i].AddThemeColorOverride("font_color", new Color(0.65f, 0.65f, 0.65f));
            _slotLabels[i].AddThemeFontSizeOverride("font_size", 11);
            _slotPanels[i].AddChild(_slotLabels[i]);
        }
    }

    private void BuildMission()
    {
        _missionLabel = new Label();
        _missionLabel.AnchorLeft  = 0.5f;
        _missionLabel.AnchorRight = 0.5f;
        _missionLabel.OffsetLeft  = -220f;
        _missionLabel.OffsetRight =  220f;
        _missionLabel.OffsetTop   =  28f;
        _missionLabel.OffsetBottom = 50f;
        _missionLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _missionLabel.AddThemeColorOverride("font_color", Gold);
        _missionLabel.Visible = false;
        AddChild(_missionLabel);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private PanelContainer MakePanel(float x, float y, float w, float h)
    {
        var p = new PanelContainer();
        p.OffsetLeft   = x;
        p.OffsetTop    = y;
        p.OffsetRight  = x + w;
        p.OffsetBottom = y + h;

        var s = new StyleBoxFlat();
        s.BgColor = DarkBg;
        s.CornerRadiusTopLeft = s.CornerRadiusTopRight = s.CornerRadiusBottomLeft = s.CornerRadiusBottomRight = 6;
        s.ContentMarginLeft = s.ContentMarginRight = 10f;
        s.ContentMarginTop  = s.ContentMarginBottom = 6f;
        p.AddThemeStyleboxOverride("panel", s);

        return p;
    }
}
